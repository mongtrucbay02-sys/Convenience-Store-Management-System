using ConvenienceStore.Web.Tests.HaTang;
using ConvenienceStore.Web.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Npgsql;
namespace ConvenienceStore.Web.Tests.PostgreSql;

public sealed class PostgreSqlSchemaTests(CsdlPostgreSqlTam database) : IClassFixture<CsdlPostgreSqlTam>
{
    private async Task<long> Count(string sql)=>Convert.ToInt64(await database.DocMotGiaTriAsync(sql));
    [Fact]
    public async Task Baseline_Creates31Tables89Checks62ForeignKeys17Triggers3InvokerViews()
    {
        Assert.Equal(31,await Count("SELECT COUNT(*) FROM pg_tables WHERE schemaname='smartmart'"));
        Assert.Equal(89,await Count("SELECT COUNT(*) FROM pg_constraint WHERE connamespace='smartmart'::regnamespace AND contype='c'"));
        Assert.Equal(62,await Count("SELECT COUNT(*) FROM pg_constraint WHERE connamespace='smartmart'::regnamespace AND contype='f'"));
        Assert.Equal(17,await Count("SELECT COUNT(*) FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid JOIN pg_namespace n ON n.oid=c.relnamespace WHERE n.nspname='smartmart' AND NOT t.tgisinternal"));
        Assert.Equal(3,await Count("SELECT COUNT(*) FROM pg_class WHERE relnamespace='smartmart'::regnamespace AND relkind='v' AND 'security_invoker=true'=ANY(reloptions)"));
        await using var db=database.TaoDbContext();
        Assert.Equal(new[] { "20261124050401_PostgreSqlBaseline", "20261127030000_ExchangePlanSnapshots" },
            await db.Database.GetAppliedMigrationsAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
    }

    [Fact]
    public async Task SM10_RlsBackendHasAccess_UntrustedRoleCannotReadThroughTableOrView()
    {
        Assert.Equal(31,await Count("SELECT COUNT(*) FROM pg_class WHERE relnamespace='smartmart'::regnamespace AND relkind='r' AND relrowsecurity"));
        Assert.Equal(0,await Count("SELECT COUNT(*) FROM pg_roles WHERE rolname='smartmart_backend' AND (rolsuper OR rolbypassrls)"));
        var role="smartmart_untrusted_"+Guid.NewGuid().ToString("N");
        await using var c=await database.MoKetNoiAsync();
        await using var command=c.CreateCommand();
        try
        {
            command.CommandText="SET ROLE smartmart_backend; SELECT COUNT(*) FROM vai_tro";
            Assert.Equal(3L,Convert.ToInt64(await command.ExecuteScalarAsync()));
            command.CommandText="RESET ROLE; CREATE ROLE "+role+" NOLOGIN NOSUPERUSER NOBYPASSRLS; GRANT USAGE ON SCHEMA smartmart TO "+role+"; GRANT SELECT ON smartmart.chuong_trinh_khuyen_mai,smartmart.v_chuong_trinh_khuyen_mai TO "+role;
            await command.ExecuteNonQueryAsync();
            await database.ChayAsync("INSERT INTO tai_khoan(ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id) VALUES('rls_actor','unused','RLS actor',1)");
            await database.ChayAsync("INSERT INTO chuong_trinh_khuyen_mai(ma,ten,loai_giam,gia_tri,bat_dau_luc,ket_thuc_luc,nguoi_tao_id) SELECT 'RLS','RLS program','PHAN_TRAM',10,NOW(),NOW()+INTERVAL '1 day',id FROM tai_khoan WHERE ten_dang_nhap='rls_actor'");
            Assert.Equal(1,await Count("SELECT COUNT(*) FROM chuong_trinh_khuyen_mai"));
            command.CommandText="SET ROLE "+role+"; SELECT COUNT(*) FROM chuong_trinh_khuyen_mai";
            Assert.Equal(0L,Convert.ToInt64(await command.ExecuteScalarAsync()));
            command.CommandText="SELECT COUNT(*) FROM v_chuong_trinh_khuyen_mai";
            Assert.Equal(0L,Convert.ToInt64(await command.ExecuteScalarAsync()));
        }
        finally
        {
            command.CommandText="RESET ROLE; DROP OWNED BY "+role+"; DROP ROLE "+role;
            await command.ExecuteNonQueryAsync();
        }
    }
}
