using System.Data.Common;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Promotions.Models;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Modules.Promotions.Services;

/// <summary>Persistence for KM-01..04 using chuong_trinh_khuyen_mai and the schema's join/view tables.</summary>
public sealed class MySqlPromotionDataSource(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor) : IPromotionDataSource
{
    private static readonly TimeSpan VietnamOffset = TimeSpan.FromHours(7);
    private readonly ApplicationDbContext _db = db;
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;

    public async Task<IReadOnlyList<PromotionRecord>> GetAllAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT p.id,p.ma,p.ten,p.mo_ta,p.loai_giam,p.gia_tri,p.giam_toi_da,p.bat_dau_luc,p.ket_thuc_luc,
                   p.chi_thanh_vien,p.trang_thai,COALESCE(u.usage_count,0) usage_count,
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
            Long(r,"gia_tri"), NullableLong(r,"giam_toi_da"), VietnamDate(r,"bat_dau_luc"), VietnamDate(r,"ket_thuc_luc"),
            Bool(r,"chi_thanh_vien"), Text(r,"trang_thai"), Int(r,"usage_count"),
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
            await using var tx=await connection.BeginTransactionAsync(ct);
            try
            {
                var creatorId = GetCreatorId();
                var createParams = Params(record with { Code = "KM-PENDING" });
                createParams["@creator"] = creatorId;
                await ExecuteAsync(connection,tx,"""
                    INSERT INTO chuong_trinh_khuyen_mai
                      (ma,ten,mo_ta,loai_giam,gia_tri,giam_toi_da,bat_dau_luc,ket_thuc_luc,chi_thanh_vien,trang_thai,nguoi_tao_id,phien_ban,ngay_tao,ngay_cap_nhat)
                    VALUES (@code,@name,@description,@type,@value,@max,@start,@end,@members,@status,@creator,1,NOW(),NOW())
                    """, ct, createParams);
                var id=Convert.ToInt64(await ScalarAsync(connection,tx,"SELECT LAST_INSERT_ID()",ct),CultureInfo.InvariantCulture);
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
            await using var tx=await connection.BeginTransactionAsync(ct);
            try
            {
                var affected=await ExecuteAsync(connection,tx,"""
                    UPDATE chuong_trinh_khuyen_mai SET ten=@name,mo_ta=@description,loai_giam=@type,gia_tri=@value,
                      giam_toi_da=@max,bat_dau_luc=@start,ket_thuc_luc=@end,chi_thanh_vien=@members,trang_thai=@status,
                      phien_ban=phien_ban+1,ngay_cap_nhat=NOW() WHERE id=@id
                    """,ct,Params(record));
                if(affected==0) { await tx.RollbackAsync(ct); return false; }
                await ReplaceProductsAsync(connection,tx,record.Id,record.Products,ct);
                await tx.CommitAsync(ct); return true;
            }
            catch { await tx.RollbackAsync(ct); throw; }
        }
        finally { if(close) await connection.CloseAsync(); }
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
        ["@id"]=r.Id,["@code"]=r.Code,["@name"]=r.Name,["@description"]=r.Description,
        ["@type"]=r.DiscountType==DiscountType.Percent?"PHAN_TRAM":"SO_TIEN",["@value"]=r.DiscountValue,
        ["@max"]=r.MaxDiscountAmount,["@start"]=DbDate(r.StartAt),["@end"]=DbDate(r.EndAt),
        ["@members"]=r.MembersOnly,["@status"]=r.Status switch {PromotionStatus.Draft=>"NHAP",PromotionStatus.Active=>"DA_KICH_HOAT",PromotionStatus.Paused=>"TAM_DUNG",_=>"DA_KET_THUC"}
    };
    private static PromotionRecord ToRecord(PromotionRow r,IReadOnlyList<ProductRefDto> products)=>new(
        r.Id,r.Code,r.Name,r.Description,r.Type=="PHAN_TRAM"?DiscountType.Percent:DiscountType.FixedAmount,
        r.Value,r.MaxDiscount,products,r.StartAt,r.EndAt,r.Members,r.Status switch {"NHAP"=>PromotionStatus.Draft,"DA_KICH_HOAT"=>PromotionStatus.Active,"TAM_DUNG"=>PromotionStatus.Paused,_=>PromotionStatus.Ended},r.UsageCount);
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
        DateTimeOffset StartAt,DateTimeOffset EndAt,bool Members,string Status,int UsageCount,ProductRefDto? Product);

    private async Task<IReadOnlyList<T>> QueryAsync<T>(string sql,DateTimeOffset? unused,Func<DbDataReader,T> map,CancellationToken ct)
    {
        var c=_db.Database.GetDbConnection();var close=c.State!=System.Data.ConnectionState.Open;if(close)await c.OpenAsync(ct);
        try { await using var cmd=c.CreateCommand();cmd.CommandText=sql;await using var r=await cmd.ExecuteReaderAsync(ct);var list=new List<T>();while(await r.ReadAsync(ct))list.Add(map(r));return list; }
        finally { if(close)await c.CloseAsync(); }
    }
    private static async Task<int> ExecuteAsync(DbConnection c,DbTransaction tx,string sql,CancellationToken ct,Dictionary<string,object?>? values=null)
    { await using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=sql;Add(cmd,values);return await cmd.ExecuteNonQueryAsync(ct); }
    private static async Task<object?> ScalarAsync(DbConnection c,DbTransaction tx,string sql,CancellationToken ct)
    { await using var cmd=c.CreateCommand();cmd.Transaction=tx;cmd.CommandText=sql;return await cmd.ExecuteScalarAsync(ct); }
    private static void Add(DbCommand c,Dictionary<string,object?>? values){if(values is null)return;foreach(var pair in values){var p=c.CreateParameter();p.ParameterName=pair.Key;p.Value=pair.Value??DBNull.Value;c.Parameters.Add(p);}}
    private static DateTime DbDate(DateTimeOffset d)=>DateTime.SpecifyKind(d.ToOffset(VietnamOffset).DateTime,DateTimeKind.Unspecified);
    private static DateTimeOffset VietnamDate(DbDataReader r,string n)=>new(DateTime.SpecifyKind(Convert.ToDateTime(r[n],CultureInfo.InvariantCulture),DateTimeKind.Unspecified),VietnamOffset);
    private static long Long(DbDataReader r,string n)=>r[n] is DBNull?0:Convert.ToInt64(r[n],CultureInfo.InvariantCulture);
    private static long? NullableLong(DbDataReader r,string n)=>r[n] is DBNull?null:Convert.ToInt64(r[n],CultureInfo.InvariantCulture);
    private static int Int(DbDataReader r,string n)=>r[n] is DBNull?0:Convert.ToInt32(r[n],CultureInfo.InvariantCulture);
    private static bool Bool(DbDataReader r,string n)=>r[n] is not DBNull&&Convert.ToBoolean(r[n],CultureInfo.InvariantCulture);
    private static string Text(DbDataReader r,string n)=>r[n] is DBNull?"":Convert.ToString(r[n],CultureInfo.InvariantCulture)??"";
    private static string? NullableText(DbDataReader r,string n)=>r[n] is DBNull?null:Text(r,n);
}

/// <summary>Live catalog search used by promotion forms and product-id validation.</summary>
public sealed class MySqlProductLookup(ApplicationDbContext db) : IProductLookup
{
    private readonly ApplicationDbContext _db=db;
    public async Task<ProductPage> SearchAsync(string? keyword,int page,int pageSize,CancellationToken ct)
    {
        page=Math.Max(1,page);pageSize=Math.Clamp(pageSize,1,50);
        var c=_db.Database.GetDbConnection();var close=c.State!=System.Data.ConnectionState.Open;if(close)await c.OpenAsync(ct);
        try
        {
            var term=string.IsNullOrWhiteSpace(keyword)?null:"%"+keyword.Trim()+"%";
            await using var count=c.CreateCommand();count.CommandText="SELECT COUNT(*) FROM san_pham WHERE (@term IS NULL OR ma LIKE @term OR ten_san_pham LIKE @term)";
            AddParam(count,"@term",term);var total=Convert.ToInt32(await count.ExecuteScalarAsync(ct),CultureInfo.InvariantCulture);
            await using var cmd=c.CreateCommand();cmd.CommandText="SELECT id,ma,ten_san_pham FROM san_pham WHERE (@term IS NULL OR ma LIKE @term OR ten_san_pham LIKE @term) ORDER BY ten_san_pham,id LIMIT @take OFFSET @skip";
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
    private static void AddParam(DbCommand c,string name,object? value){var p=c.CreateParameter();p.ParameterName=name;p.Value=value??DBNull.Value;c.Parameters.Add(p);}
}

