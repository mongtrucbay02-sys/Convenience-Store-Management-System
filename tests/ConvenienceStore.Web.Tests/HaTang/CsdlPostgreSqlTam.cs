using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace ConvenienceStore.Web.Tests.HaTang;

// Every fixture owns a randomly named database on an explicitly supplied loopback server.
public class CsdlPostgreSqlTam : IAsyncLifetime
{
    public const string TenBienMoiTruong = "ConnectionStrings__CuaHangPostgreSqlTest";
    private string _chuoiMayChu = "";
    public string TenCsdl { get; } = "smartmart_test_" + Guid.NewGuid().ToString("N");
    public string ChuoiKetNoi { get; private set; } = "";
    public DbContextOptions<CuaHangDbContext> TuyChonDbContext { get; private set; } = null!;
    public async Task InitializeAsync()
    {
        var value=Environment.GetEnvironmentVariable(TenBienMoiTruong)
            ?? throw new InvalidOperationException($"Missing {TenBienMoiTruong}; isolated loopback PostgreSQL 17 is required.");
        var server=new NpgsqlConnectionStringBuilder(value) { Database="postgres",Pooling=false };
        if(server.Host is not ("127.0.0.1" or "localhost" or "::1"))
            throw new InvalidOperationException("Refuse test writes outside loopback.");
        _chuoiMayChu=server.ConnectionString;
        await using(var c=new NpgsqlConnection(_chuoiMayChu)) {
            await c.OpenAsync();
            await using var version=new NpgsqlCommand("SHOW server_version_num",c);
            if(Convert.ToInt32(await version.ExecuteScalarAsync())/10000!=17)
                throw new InvalidOperationException("PostgreSQL 17 is required.");
            await using(var roles=new NpgsqlCommand("DO $r$ BEGIN IF NOT EXISTS(SELECT 1 FROM pg_roles WHERE rolname='smartmart_backend') THEN CREATE ROLE smartmart_backend NOLOGIN NOSUPERUSER NOBYPASSRLS; END IF; EXCEPTION WHEN duplicate_object THEN NULL; END $r$",c)) await roles.ExecuteNonQueryAsync();
            await using var create=new NpgsqlCommand("CREATE DATABASE \""+TenCsdl+"\"",c);
            await create.ExecuteNonQueryAsync();
        }
        server.Database=TenCsdl;server.SearchPath="smartmart";server.Timezone="Asia/Ho_Chi_Minh";
        ChuoiKetNoi=server.ConnectionString;
        TuyChonDbContext=new DbContextOptionsBuilder<CuaHangDbContext>()
            .UseNpgsql(ChuoiKetNoi,pg=>pg.SetPostgresVersion(17,0)).Options;
        await using(var db=TaoDbContext()) await db.Database.MigrateAsync();
        await ChayAsync("""
            INSERT INTO vai_tro(id,ma,ten) VALUES(1,'AD','Admin'),(2,'TN','Thu ngân'),(3,'NK','Nhân viên kho');
            INSERT INTO quy_tac_tich_diem(so_tien_mot_diem,dang_bat,hieu_luc_tu) VALUES(10000,TRUE,'2026-01-01T00:00:00+07');
            INSERT INTO chinh_sach_doi_tra(so_ngay_doi_tra,nguong_tien_can_duyet,hieu_luc_tu) VALUES(7,500000,'2026-01-01T00:00:00+07');
            INSERT INTO quy_tien_mat(id,so_du,da_chot_so_du_dau) VALUES(1,0,FALSE);
            """);
        await NapDuLieuNenAsync();
    }
    public async Task DisposeAsync()
    {
        if(_chuoiMayChu.Length==0 || Environment.GetEnvironmentVariable("GIU_CSDL_TEST")=="1")return;
        await using var c=new NpgsqlConnection(_chuoiMayChu);await c.OpenAsync();
        await using var drop=new NpgsqlCommand("DROP DATABASE IF EXISTS \""+TenCsdl+"\" WITH (FORCE)",c);
        await drop.ExecuteNonQueryAsync();
    }
    protected virtual Task NapDuLieuNenAsync()=>Task.CompletedTask;
    public CuaHangDbContext TaoDbContext()=>new(TuyChonDbContext);
    public async Task<NpgsqlConnection> MoKetNoiAsync(){var c=new NpgsqlConnection(ChuoiKetNoi);await c.OpenAsync();return c;}
    public async Task<int> ChayAsync(string sql,params (string Ten,object? GiaTri)[] args)
    {await using var c=await MoKetNoiAsync();await using var cmd=TaoLenh(c,sql,args);return await cmd.ExecuteNonQueryAsync();}
    public async Task<object?> DocMotGiaTriAsync(string sql,params (string Ten,object? GiaTri)[] args)
    {await using var c=await MoKetNoiAsync();await using var cmd=TaoLenh(c,sql,args);var result=await cmd.ExecuteScalarAsync();return result is DBNull?null:result;}
    public static NpgsqlCommand TaoLenh(NpgsqlConnection c,string sql,params (string Ten,object? GiaTri)[] args)
    {var cmd=new NpgsqlCommand(sql,c);foreach(var(name,value)in args)cmd.AddValue(name,value);return cmd;}
}
