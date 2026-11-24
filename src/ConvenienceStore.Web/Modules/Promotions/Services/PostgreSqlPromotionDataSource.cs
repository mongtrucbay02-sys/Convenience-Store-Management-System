using ConvenienceStore.Web.Shared.Data;
using System.Data.Common;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Promotions.Models;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

/// <summary>Persistence for KM-01..04 using chuong_trinh_khuyen_mai and the schema's join/view tables.</summary>
public sealed class PostgreSqlPromotionDataSource(CuaHangDbContext db, IHttpContextAccessor httpContextAccessor) : IPromotionDataSource
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private readonly CuaHangDbContext _db = db;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<IReadOnlyList<PromotionRecord>> GetAllAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT p.id,p.ma,p.ten,p.mo_ta,p.anh_url,p.loai_giam,p.gia_tri,p.giam_toi_da,p.bat_dau_luc,p.ket_thuc_luc,
                   p.chi_thanh_vien,p.trang_thai,p.phien_ban,COALESCE(u.usage_count,0) usage_count,
                   k.san_pham_id product_id,COALESCE(s.ma,'') product_sku,COALESCE(s.ten_san_pham,'') product_name
            FROM chuong_trinh_khuyen_mai p
            LEFT JOIN (SELECT chuong_trinh_id,COUNT(DISTINCT hoa_don_id) usage_count
                       FROM v_luot_dung_khuyen_mai GROUP BY chuong_trinh_id) u ON u.chuong_trinh_id=p.id
            LEFT JOIN khuyen_mai_san_pham k ON k.chuong_trinh_id=p.id
            LEFT JOIN san_pham s ON s.id=k.san_pham_id
            ORDER BY p.id,k.san_pham_id
            """;
        var rows = await QueryAsync(sql, null, r => new PromotionRow(
            Long(r,"id"), Text(r,"ma"), Text(r,"ten"), NullableText(r,"mo_ta"), Text(r,"loai_giam"),
            Long(r,"gia_tri"), NullableLong(r,"giam_toi_da") is 0 ? null : NullableLong(r,"giam_toi_da"), VietnamDate(r,"bat_dau_luc"), VietnamDate(r,"ket_thuc_luc"),
            Bool(r,"chi_thanh_vien"), Text(r,"trang_thai"), Int(r,"usage_count"), NullableText(r,"anh_url"), Long(r,"phien_ban"),
            r["product_id"] is DBNull ? null : new ProductRefDto(Long(r,"product_id"),Text(r,"product_sku"),Text(r,"product_name"))), ct);
        return rows.GroupBy(r=>r.Id).Select(g=>ToRecord(g.First(),g.Where(x=>x.Product is not null).Select(x=>x.Product!).ToArray())).ToArray();
    }

    public async Task<PromotionRecord?> GetByIdAsync(long id, CancellationToken ct) =>
        (await GetAllAsync(ct)).FirstOrDefault(p=>p.Id==id);

    public async Task<PromotionRecord> CreateAsync(PromotionRecord record, CancellationToken ct)
    {
        var connection=_db.Database.GetDbConnection(); var close=connection.State!=System.Data.ConnectionState.Open;
        if(close) await connection.OpenAsync(ct);
        try
        {
            await using var tx=await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
            try
            {
                var creatorId = GetCreatorId();
                var createParams = Params(record with { Code = "KM-PENDING" });
                createParams["@creator"] = creatorId;
                var id = await SqlDialect.InsertIdAsync(connection,tx,"""
                    INSERT INTO chuong_trinh_khuyen_mai
                      (ma,ten,mo_ta,anh_url,loai_giam,gia_tri,giam_toi_da,bat_dau_luc,ket_thuc_luc,chi_thanh_vien,trang_thai,nguoi_tao_id,phien_ban,ngay_tao,ngay_cap_nhat)
                    VALUES (@code,@name,@description,@image,@type,@value,@max,@start,@end,@members,@status,@creator,1,NOW(),NOW())
                    """, ct, createParams);
                await ExecuteAsync(connection,tx,"UPDATE chuong_trinh_khuyen_mai SET ma=@code WHERE id=@id",ct,
                    new Dictionary<string,object?>{{"@code",Code(id)},{"@id",id}});
                await ReplaceProductsAsync(connection,tx,id,record.Products,ct);
                await tx.CommitAsync(ct);
                return (await GetByIdAsync(id,ct))!;
            }
            catch { await tx.RollbackAsync(ct); throw; }
        }
        finally { if(close) await connection.CloseAsync(); }
    }

    public async Task<bool> UpdateAsync(PromotionRecord record, CancellationToken ct)
    {
        var connection=_db.Database.GetDbConnection(); var close=connection.State!=System.Data.ConnectionState.Open;
        if(close) await connection.OpenAsync(ct);
        try
        {
            await using var tx=await connection.BeginTransactionAsync(System.Data.IsolationLevel.ReadCommitted,ct);
            try
            {
                // Checkout holds FOR SHARE on this same promotion row until its
                // invoice and usage are committed. Acquire the incompatible lock
                // first, then inspect usage with a fresh READ COMMITTED snapshot.
                if (!await CanUpdateAsync(connection,tx,record,ct))
                { await tx.RollbackAsync(ct); return false; }
                var affected=await ExecuteAsync(connection,tx,"""
                    UPDATE chuong_trinh_khuyen_mai SET ten=@name,mo_ta=@description,anh_url=@image,loai_giam=@type,gia_tri=@value,
                      giam_toi_da=@max,bat_dau_luc=@start,ket_thuc_luc=@end,chi_thanh_vien=@members,trang_thai=@status,
                      phien_ban=phien_ban+1,ngay_cap_nhat=NOW() WHERE id=@id AND phien_ban=@version
                    """,ct,Params(record));
                if(affected==0) { await tx.RollbackAsync(ct); return false; }
                await ReplaceProductsAsync(connection,tx,record.Id,record.Products,ct);
                await tx.CommitAsync(ct); return true;
            }
            catch { await tx.RollbackAsync(ct); throw; }
        }
        finally { if(close) await connection.CloseAsync(); }
    }

    private static async Task<bool> CanUpdateAsync(DbConnection c, DbTransaction tx, PromotionRecord proposed, CancellationToken ct)
    {
        await using (var rowLock=SqlDialect.Command("SELECT id FROM chuong_trinh_khuyen_mai WHERE id=@id FOR UPDATE",c,tx))
        {
            rowLock.AddValue("@id",proposed.Id);
            if(await rowLock.ExecuteScalarAsync(ct) is null) return false;
        }
        bool restrictedChanged;
        await using (var command = SqlDialect.Command("""
            SELECT phien_ban,loai_giam,gia_tri,giam_toi_da,bat_dau_luc,ket_thuc_luc,
                   chi_thanh_vien,trang_thai,clock_timestamp() AS current_time
            FROM chuong_trinh_khuyen_mai WHERE id=@id
            """,c,tx))
        {
            command.AddValue("@id",proposed.Id);
            await using var r=await command.ExecuteReaderAsync(ct);
            if (!await r.ReadAsync(ct) || Long(r,"phien_ban") != proposed.Version) return false;
            var status=Text(r,"trang_thai") switch {
                "NHAP"=>PromotionStatus.Draft,"DA_KICH_HOAT"=>PromotionStatus.Active,
                "TAM_DUNG"=>PromotionStatus.Paused,_=>PromotionStatus.Ended };
            if (PromotionService.GetDisplayStatus(status,VietnamDate(r,"bat_dau_luc"),VietnamDate(r,"ket_thuc_luc"),VietnamDate(r,"current_time"))
                == PromotionDisplayStatus.Ended) return false;
            restrictedChanged = Text(r,"loai_giam") != (proposed.DiscountType==DiscountType.Percent?"PHAN_TRAM":"SO_TIEN")
                || Long(r,"gia_tri") != proposed.DiscountValue
                || (NullableLong(r,"giam_toi_da") is 0 ? null : NullableLong(r,"giam_toi_da")) != proposed.MaxDiscountAmount
                || VietnamDate(r,"bat_dau_luc") != proposed.StartAt
                || Bool(r,"chi_thanh_vien") != proposed.MembersOnly;
        }
        var currentIds=new List<long>();
        await using (var command=SqlDialect.Command("SELECT san_pham_id FROM khuyen_mai_san_pham WHERE chuong_trinh_id=@id ORDER BY san_pham_id",c,tx))
        {
            command.AddValue("@id",proposed.Id);
            await using var r=await command.ExecuteReaderAsync(ct);
            while(await r.ReadAsync(ct)) currentIds.Add(r.GetInt64(0));
        }
        restrictedChanged |= !currentIds.SequenceEqual(proposed.Products.Select(p=>p.Id).OrderBy(id=>id));
        if (!restrictedChanged) return true;
        await using var usage=SqlDialect.Command("SELECT EXISTS(SELECT 1 FROM v_luot_dung_khuyen_mai WHERE chuong_trinh_id=@id)",c,tx);
        usage.AddValue("@id",proposed.Id);
        return !Convert.ToBoolean(await usage.ExecuteScalarAsync(ct));
    }

    private static async Task ReplaceProductsAsync(DbConnection c,DbTransaction tx,long id,IReadOnlyList<ProductRefDto> products,CancellationToken ct)
    {
        await ExecuteAsync(c,tx,"DELETE FROM khuyen_mai_san_pham WHERE chuong_trinh_id=@id",ct,new Dictionary<string,object?>{{"@id",id}});
        foreach(var product in products)
            await ExecuteAsync(c,tx,"INSERT INTO khuyen_mai_san_pham(chuong_trinh_id,san_pham_id) VALUES(@promotion,@product)",ct,
                new Dictionary<string,object?>{{"@promotion",id},{"@product",product.Id}});
    }

    private static Dictionary<string,object?> Params(PromotionRecord r)=>new()
    {
        ["@id"]=r.Id,["@version"]=r.Version,["@code"]=r.Code,["@name"]=r.Name,["@description"]=r.Description,["@image"]=r.ImageUrl,
        ["@type"]=r.DiscountType==DiscountType.Percent?"PHAN_TRAM":"SO_TIEN",["@value"]=r.DiscountValue,
        ["@max"]=r.MaxDiscountAmount,["@start"]=DbDate(r.StartAt),["@end"]=DbDate(r.EndAt),
        ["@members"]=r.MembersOnly,["@status"]=r.Status switch {PromotionStatus.Draft=>"NHAP",PromotionStatus.Active=>"DA_KICH_HOAT",PromotionStatus.Paused=>"TAM_DUNG",_=>"DA_KET_THUC"}
    };
    private static PromotionRecord ToRecord(PromotionRow r,IReadOnlyList<ProductRefDto> products)=>new(
        r.Id,r.Code,r.Name,r.Description,r.Type=="PHAN_TRAM"?DiscountType.Percent:DiscountType.FixedAmount,
        r.Value,r.MaxDiscount,products,r.StartAt,r.EndAt,r.Members,r.Status switch {"NHAP"=>PromotionStatus.Draft,"DA_KICH_HOAT"=>PromotionStatus.Active,"TAM_DUNG"=>PromotionStatus.Paused,_=>PromotionStatus.Ended},r.UsageCount,r.ImageUrl,r.Version);
    private static string Code(long id)=>$"KM-{id:00000000}";
    private long GetCreatorId()
    {
        var value = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? _httpContextAccessor.HttpContext?.User.FindFirstValue("sub");
        return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var id) && id > 0
            ? id
            : throw new InvalidOperationException("Không xác định được mã tài khoản Admin để lưu người tạo khuyến mãi.");
    }
    private sealed record PromotionRow(long Id,string Code,string Name,string? Description,string Type,long Value,long? MaxDiscount,
        DateTimeOffset StartAt,DateTimeOffset EndAt,bool Members,string Status,int UsageCount,string? ImageUrl,long Version,ProductRefDto? Product);

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string sql,DateTimeOffset? unused,Func<DbDataReader,T> map,CancellationToken ct)
    {
        var c=_db.Database.GetDbConnection();var close=c.State!=System.Data.ConnectionState.Open;if(close)await c.OpenAsync(ct);
        try { await using var cmd=c.CreateCommand();cmd.CommandText=sql;await using var r=await cmd.ExecuteReaderAsync(ct);var list=new List<T>();while(await r.ReadAsync(ct))list.Add(map(r));return list; }
        finally { if(close)await c.CloseAsync(); }
    }
    private static async Task<int> ExecuteAsync(DbConnection c,DbTransaction tx,string sql,CancellationToken ct,Dictionary<string,object?>? values=null)
    { await using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=sql;Add(cmd,values);return await cmd.ExecuteNonQueryAsync(ct); }
    private static void Add(DbCommand c,Dictionary<string,object?>? values){if(values is null)return;foreach(var pair in values){var p=c.CreateParameter();p.ParameterName=pair.Key;p.Value=SqlDialect.Normalize(c.Connection!,pair.Value)??DBNull.Value;c.Parameters.Add(p);}}
    private static DateTime DbDate(DateTimeOffset d)=>DateTime.SpecifyKind(d.ToOffset(VietnamOffset).DateTime,DateTimeKind.Unspecified);
    private static DateTimeOffset VietnamDate(DbDataReader r,string n)=>ConvenienceStore.Web.Shared.Api.GioVietNam.ThanhOffset(Convert.ToDateTime(r[n],CultureInfo.InvariantCulture));
    private static long Long(DbDataReader r,string n)=>r[n] is DBNull?0:Convert.ToInt64(r[n],CultureInfo.InvariantCulture);
    private static long? NullableLong(DbDataReader r,string n)=>r[n] is DBNull?null:Convert.ToInt64(r[n],CultureInfo.InvariantCulture);
    private static int Int(DbDataReader r,string n)=>r[n] is DBNull?0:Convert.ToInt32(r[n],CultureInfo.InvariantCulture);
    private static bool Bool(DbDataReader r,string n)=>r[n] is not DBNull&&Convert.ToBoolean(r[n],CultureInfo.InvariantCulture);
    private static string Text(DbDataReader r,string n)=>r[n] is DBNull?"":Convert.ToString(r[n],CultureInfo.InvariantCulture)??"";
    private static string? NullableText(DbDataReader r,string n)=>r[n] is DBNull?null:Text(r,n);
}

/// <summary>Live catalog search used by promotion forms and product-id validation.</summary>
public sealed class PostgreSqlProductLookup(CuaHangDbContext db) : IProductLookup
{
    private readonly CuaHangDbContext _db=db;
    public async Task<ProductPage> SearchAsync(string? keyword,int page,int pageSize,CancellationToken ct)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,50);
        var c=_db.Database.GetDbConnection();var close=c.State!=System.Data.ConnectionState.Open;if(close)await c.OpenAsync(ct);
        try
        {
            var term=string.IsNullOrWhiteSpace(keyword)?null:"%"+VietnameseSearch.Fold(keyword.Trim())+"%";
            await using var count=c.CreateCommand();count.CommandText="SELECT COUNT(*) FROM san_pham WHERE (@term IS NULL OR "+VietnameseSearch.Column("ma")+" LIKE @term OR "+VietnameseSearch.Column("ten_san_pham")+" LIKE @term)";
            AddParam(count,"@term",term);var total=Convert.ToInt32(await count.ExecuteScalarAsync(ct),CultureInfo.InvariantCulture);
            await using var cmd=c.CreateCommand();cmd.CommandText="SELECT id,ma,ten_san_pham FROM san_pham WHERE (@term IS NULL OR "+VietnameseSearch.Column("ma")+" LIKE @term OR "+VietnameseSearch.Column("ten_san_pham")+" LIKE @term) ORDER BY ten_san_pham,id LIMIT @take OFFSET @skip";
            AddParam(cmd,"@term",term);AddParam(cmd,"@take",pageSize);AddParam(cmd,"@skip",(page-1)*pageSize);
            await using var r=await cmd.ExecuteReaderAsync(ct);var products=new List<ProductRefDto>();while(await r.ReadAsync(ct))products.Add(new ProductRefDto(Convert.ToInt64(r["id"],CultureInfo.InvariantCulture),Convert.ToString(r["ma"],CultureInfo.InvariantCulture)??"",Convert.ToString(r["ten_san_pham"],CultureInfo.InvariantCulture)??""));
            return new ProductPage(products,page,pageSize,total,(int)Math.Ceiling(total/(double)pageSize));
        }
        finally {if(close)await c.CloseAsync();}
    }
    public async Task<IReadOnlyList<ProductRefDto>> GetByIdsAsync(IReadOnlyCollection<long> ids,CancellationToken ct)
    {
        if(ids.Count==0)return [];
        var c=_db.Database.GetDbConnection();var close=c.State!=System.Data.ConnectionState.Open;if(close)await c.OpenAsync(ct);
        try
        {
            await using var cmd=c.CreateCommand();var names=ids.Distinct().Select((_,i)=>"@p"+i).ToArray();cmd.CommandText=$"SELECT id,ma,ten_san_pham FROM san_pham WHERE id IN ({string.Join(',',names)})";
            var i=0;foreach(var id in ids.Distinct())AddParam(cmd,names[i++],id);
            await using var r=await cmd.ExecuteReaderAsync(ct);var found=new Dictionary<long,ProductRefDto>();while(await r.ReadAsync(ct)){var p=new ProductRefDto(Convert.ToInt64(r["id"],CultureInfo.InvariantCulture),Convert.ToString(r["ma"],CultureInfo.InvariantCulture)??"",Convert.ToString(r["ten_san_pham"],CultureInfo.InvariantCulture)??"");found[p.Id]=p;}
            return ids.Distinct().Where(found.ContainsKey).Select(id=>found[id]).ToArray();
        }
        finally {if(close)await c.CloseAsync();}
    }
    private static void AddParam(DbCommand c,string name,object? value) => c.AddValue(name,value);
}
