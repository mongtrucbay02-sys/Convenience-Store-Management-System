using Npgsql;

namespace ConvenienceStore.Web.Tests.PostgreSql;

// All mutations are confined to the disposable observed database. Deliberate
// policy/grant changes are fault-injection controls, not the AS-IS contract.
[Collection("BackendRoleContract")]
[Trait("Database", "PostgreSql")]
public sealed class ObservedSecurityBehaviorPostgreSqlTests(SmokeRolePostgreSqlFixture fixture)
{
    private async Task InObservedDatabase(Func<NpgsqlConnection, Task> test)
    {
        var baseline = await fixture.ReadBaselineAclFingerprintAsync();
        await fixture.PrepareObservedDatabaseAsync();
        try
        {
            await using var connection = new NpgsqlConnection(fixture.ObservedBackendConnection);
            await connection.OpenAsync();
            Assert.Equal("smartmart_backend", await Scalar<string>(connection, "SELECT current_user::text"));
            Assert.Equal("smartmart_backend", await Scalar<string>(connection, "SELECT session_user::text"));
            Assert.Equal("on", await Scalar<string>(connection, "SHOW row_security"));
            await test(connection);
        }
        finally
        {
            await fixture.DropObservedDatabaseAsync();
            await fixture.AssertSafeRoleAsync();
            Assert.Equal(baseline, await fixture.ReadBaselineAclFingerprintAsync());
        }
    }

    private async Task AdminSql(string sql)
    {
        // Retain the fixture's guarded loopback admin endpoint; change only the
        // database to the owned observed profile, never to the baseline.
        var settings = new NpgsqlConnectionStringBuilder(fixture.AdminConnection)
        {
            Database = new NpgsqlConnectionStringBuilder(fixture.ObservedBackendConnection).Database
        };
        Assert.NotEqual(new NpgsqlConnectionStringBuilder(fixture.AdminConnection).Database, settings.Database);
        await using var admin = new NpgsqlConnection(settings.ConnectionString);
        await admin.OpenAsync();
        await Execute(admin, sql);
    }

    private static async Task<T> Scalar<T>(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction = null)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return (T)(await command.ExecuteScalarAsync())!;
    }

    private static async Task<int> Execute(NpgsqlConnection connection, string sql, NpgsqlTransaction? transaction = null)
    {
        await using var command = new NpgsqlCommand(sql, connection, transaction);
        return await command.ExecuteNonQueryAsync();
    }

    private static async Task Denied(NpgsqlConnection connection, string sql, string state, string? message = null)
    {
        var error = await Assert.ThrowsAsync<PostgresException>(async () => { await Execute(connection, sql); });
        Assert.Equal(state, error.SqlState);
        if (message is not null) Assert.Contains(message, error.MessageText);
    }

    [Fact]
    public Task RealBackend_ObservedRls_VisibilityAndWithCheck_AreDistinctFromAcl() =>
        InObservedDatabase(async connection =>
        {
            await Execute(connection, """
                INSERT INTO smartmart.danh_muc(id,ten) VALUES (9001,'QA visible'),(9002,'QA hidden')
                """);
            Assert.Equal(2L, await Scalar<long>(connection, "SELECT count(*) FROM smartmart.danh_muc WHERE id IN (9001,9002)"));
            // The observed policy trusts the runtime role; it has no per-user row filter.
            // Inject a restrictive policy solely to prove PostgreSQL enforces RLS with ACL still granted.
            await AdminSql("""
                CREATE POLICY qa_restrictive_probe ON smartmart.danh_muc
                AS RESTRICTIVE FOR ALL TO smartmart_backend
                USING (id <> 9002) WITH CHECK (ten NOT LIKE 'QA forbidden%')
                """);
            Assert.True(await Scalar<bool>(connection, "SELECT has_table_privilege(current_user,'smartmart.danh_muc','INSERT')"));
            Assert.True(await Scalar<bool>(connection, "SELECT has_table_privilege(current_user,'smartmart.danh_muc','UPDATE')"));
            Assert.Equal(1L, await Scalar<long>(connection, "SELECT count(*) FROM smartmart.danh_muc WHERE id IN (9001,9002)"));
            Assert.Equal(0, await Execute(connection, "UPDATE smartmart.danh_muc SET ten='QA changed' WHERE id=9002"));
            Assert.Equal(0, await Execute(connection, "DELETE FROM smartmart.danh_muc WHERE id=9002"));
            await Denied(connection, "INSERT INTO smartmart.danh_muc(id,ten) VALUES (9003,'QA forbidden insert')",
                "42501", "row-level security");
            await Denied(connection, "UPDATE smartmart.danh_muc SET ten='QA forbidden update' WHERE id=9001",
                "42501", "row-level security");
            Assert.Equal("QA visible", await Scalar<string>(connection, "SELECT ten FROM smartmart.danh_muc WHERE id=9001"));
            Assert.Equal(0L, await Scalar<long>(connection, "SELECT count(*) FROM smartmart.danh_muc WHERE id=9003"));
            await AdminSql("DROP POLICY qa_restrictive_probe ON smartmart.danh_muc");
            Assert.Equal("QA hidden", await Scalar<string>(connection, "SELECT ten FROM smartmart.danh_muc WHERE id=9002"));
            Assert.False(await Scalar<bool>(connection, "SELECT has_table_privilege(current_user,'smartmart.vai_tro','DELETE')"));
            await Denied(connection, "DELETE FROM smartmart.vai_tro WHERE id=1", "42501", "permission denied");
        });

    [Fact]
    public Task RealBackend_SequenceAndFunctionPermissions_AndUpdatedAtTrigger() =>
        InObservedDatabase(async connection =>
        {
            var first = await Scalar<long>(connection, "SELECT nextval('smartmart.danh_muc_id_seq')");
            Assert.Equal(first, await Scalar<long>(connection, "SELECT currval('smartmart.danh_muc_id_seq')"));
            await Denied(connection, "SELECT setval('smartmart.danh_muc_id_seq',1)", "42501", "permission denied");
            Assert.True(await Scalar<long>(connection, "SELECT nextval('smartmart.danh_muc_id_seq')") > first);
            foreach (var function in new[] { "fn_guard_hoa_don", "fn_prevent_history_mutation", "fn_updated_at" })
            {
                Assert.False(await Scalar<bool>(connection, $"SELECT has_function_privilege(current_user,'smartmart.{function}()','EXECUTE')"));
                await Denied(connection, $"SELECT smartmart.{function}()", "42501", "permission denied");
            }
            await Execute(connection, "INSERT INTO smartmart.danh_muc(id,ten,ngay_cap_nhat) VALUES (9001,'QA timestamp','2000-01-01Z')");
            Assert.Equal(1, await Execute(connection, "UPDATE smartmart.danh_muc SET ten='QA updated' WHERE id=9001"));
            Assert.True(await Scalar<bool>(connection, "SELECT ngay_cap_nhat > '2000-01-01Z'::timestamptz FROM smartmart.danh_muc WHERE id=9001"));
        });

    private static readonly string[] HistoryTables =
    [
        "ct_hoa_don", "ct_phieu_doi_tra", "giao_dich_tien_mat",
        "hoan_tien_hoa_don", "lich_su_diem", "the_kho"
    ];

    [Fact]
    public Task RealBackend_ExactTriggerInventory_MatchesBaselineAttachments() =>
        InObservedDatabase(async connection =>
        {
            var expected = HistoryTables.Select(t => $"{t}|tr_{t}_append_only|fn_prevent_history_mutation|27|O")
                .Append("hoa_don|tr_hoa_don_guard|fn_guard_hoa_don|27|O")
                .Concat(new[] { "chuong_trinh_khuyen_mai", "danh_muc", "gio_hang", "khach_hang", "nha_cung_cap",
                    "phieu_nhap", "quy_tien_mat", "san_pham", "tai_khoan", "ton_kho" }
                    .Select(t => $"{t}|trg_{t}_updated|fn_updated_at|19|O"))
                .OrderBy(x => x, StringComparer.Ordinal).ToArray();
            Assert.Equal(17, expected.Length);
            var actual = await Scalar<string[]>(connection, """
                SELECT ARRAY(SELECT concat_ws('|', c.relname,t.tgname,p.proname,t.tgtype,t.tgenabled)
                    FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid
                    JOIN pg_namespace n ON n.oid=c.relnamespace
                    JOIN pg_proc p ON p.oid=t.tgfoid
                    JOIN pg_namespace pn ON pn.oid=p.pronamespace
                    WHERE n.nspname='smartmart' AND NOT t.tgisinternal AND pn.nspname='smartmart'
                    AND t.tgqual IS NULL AND t.tgnargs=0)
                """);
            Assert.Equal(expected, actual.OrderBy(x => x, StringComparer.Ordinal).ToArray());
            Assert.Equal(17L, await Scalar<long>(connection, """
                SELECT count(*) FROM pg_trigger t JOIN pg_class c ON c.oid=t.tgrelid
                JOIN pg_namespace n ON n.oid=c.relnamespace
                WHERE n.nspname='smartmart' AND NOT t.tgisinternal
                """));
        });

    // Historical synthetic records only; does not exercise or re-enable loyalty business flows.
    private static Task<int> SeedHistory(NpgsqlConnection connection) => Execute(connection, """
        INSERT INTO smartmart.tai_khoan(id,ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id)
            VALUES (9001,'qa_history','synthetic-not-a-password','QA',1);
        INSERT INTO smartmart.danh_muc(id,ten) VALUES (9001,'QA history');
        INSERT INTO smartmart.san_pham(id,ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id)
            VALUES (9001,'QA9001','QA product',9001,'cai',9001);
        INSERT INTO smartmart.khach_hang(id,ho_ten,so_dien_thoai,nguoi_tao_id)
            VALUES (9001,'QA customer','0900009001',9001);
        INSERT INTO smartmart.gio_hang(id,thu_ngan_id,trang_thai,hoan_tat_luc)
            VALUES (9001,9001,'DA_HOAN_TAT',now());
        INSERT INTO smartmart.hoa_don(id,ma_hoa_don,gio_hang_id,thu_ngan_id,thanh_toan_luc,
            tong_tien_hang,tong_thanh_toan,tien_khach_dua,tien_thua)
            VALUES (9001,'QAHD9001',9001,9001,now(),1000,1000,1000,0);
        INSERT INTO smartmart.ct_hoa_don(hoa_don_id,san_pham_id,ten_san_pham,don_vi_tinh,so_luong,don_gia,gia_von)
            VALUES (9001,9001,'QA product','cai',1,1000,500);
        INSERT INTO smartmart.chinh_sach_doi_tra(id,hieu_luc_tu) VALUES (9001,now());
        INSERT INTO smartmart.phieu_doi_tra(id,ma_phieu,loai,hoa_don_goc_id,chinh_sach_id,trang_thai,
            gia_tri_hang_tra,tien_hoan,thu_ngan_id,hoan_tat_luc)
            VALUES (9001,'QADT9001','TRA_HANG',9001,9001,'HOAN_TAT',1000,1000,9001,now());
        INSERT INTO smartmart.ct_phieu_doi_tra(phieu_doi_tra_id,san_pham_id,hoa_don_id,so_luong,
            don_gia_thuc_tra,ly_do,cach_xu_ly,gia_von)
            VALUES (9001,9001,9001,1,1000,'LOI_SAN_PHAM','NHAP_LAI',500);
        INSERT INTO smartmart.quy_tien_mat(id,so_du) VALUES (1,1000) ON CONFLICT (id) DO NOTHING;
        INSERT INTO smartmart.giao_dich_tien_mat(id,quy_tien_mat_id,loai,hoa_don_id,so_tien,
            so_du_truoc,so_du_sau,nguoi_thuc_hien_id)
            VALUES (9001,1,'THU_BAN_HANG',9001,1000,0,1000,9001);
        INSERT INTO smartmart.hoan_tien_hoa_don(id,hoa_don_id,so_tien_hoan,ly_do,nguoi_thuc_hien_id)
            VALUES (9001,9001,1000,'QA',9001);
        INSERT INTO smartmart.lich_su_diem(id,khach_hang_id,loai,so_diem,diem_sau,ly_do,nguoi_thuc_hien_id)
            VALUES (9001,9001,'DIEU_CHINH',1,1,'QA historical record',9001);
        INSERT INTO smartmart.the_kho(id,san_pham_id,loai_bien_dong,ma_chung_tu,chung_tu_id,
            so_luong_thay_doi,ton_sau,gia_von_ap_dung,gia_von_sau,nguoi_thuc_hien_id)
            VALUES (9001,9001,'BAN_HANG','QAHD9001',9001,-1,0,500,500,9001);
        """);

    [Fact]
    public Task RealBackend_HistoryAclAndAppendOnlyTriggers_RejectMutationIndependently() =>
        InObservedDatabase(async connection =>
        {
            await SeedHistory(connection);
            foreach (var table in HistoryTables)
            {
                var key = table switch
                {
                    "ct_hoa_don" => "hoa_don_id",
                    "ct_phieu_doi_tra" => "phieu_doi_tra_id",
                    _ => "id"
                };
                var target = $"smartmart.{table}";
                var before = await Scalar<string>(connection, $"SELECT to_jsonb(t)::text FROM {target} t WHERE {key}=9001");
                var update = $"UPDATE {target} SET {key}={key} WHERE {key}=9001";
                var delete = $"DELETE FROM {target} WHERE {key}=9001";
                await Denied(connection, update, "42501", "permission denied");
                await Denied(connection, delete, "42501", "permission denied");
                // Fault-injection only: allow the statement to reach the existing trigger.
                await AdminSql($"GRANT UPDATE, DELETE ON {target} TO smartmart_backend");
                try
                {
                    Assert.True(await Scalar<bool>(connection, $"SELECT has_table_privilege(current_user,'{target}','UPDATE')"));
                    Assert.True(await Scalar<bool>(connection, $"SELECT has_table_privilege(current_user,'{target}','DELETE')"));
                    await Denied(connection, update, "23514", $"{table} is append-only: UPDATE forbidden");
                    await Denied(connection, delete, "23514", $"{table} is append-only: DELETE forbidden");
                }
                finally
                {
                    await AdminSql($"REVOKE UPDATE, DELETE ON {target} FROM smartmart_backend");
                }
                Assert.Equal(before, await Scalar<string>(connection, $"SELECT to_jsonb(t)::text FROM {target} t WHERE {key}=9001"));
                Assert.False(await Scalar<bool>(connection, $"SELECT has_table_privilege(current_user,'{target}','UPDATE')"));
                Assert.False(await Scalar<bool>(connection, $"SELECT has_table_privilege(current_user,'{target}','DELETE')"));
            }
        });

    [Fact]
    public Task RealBackend_InvoiceGuard_ProtectsFinancialSnapshotAndRollback() =>
        InObservedDatabase(async connection =>
        {
            await SeedHistory(connection);
            const string snapshot = """
                SELECT (to_jsonb(h) - ARRAY['trang_thai','nguoi_huy_id','huy_luc','ly_do_huy']::text[])::text
                FROM smartmart.hoa_don h WHERE id=9001
                """;
            var before = await Scalar<string>(connection, snapshot);
            const string cancel = """
                UPDATE smartmart.hoa_don SET trang_thai='DA_HUY',nguoi_huy_id=9001,
                    huy_luc=now(),ly_do_huy='QA cancellation' WHERE id=9001
                """;
            await Denied(connection, "UPDATE smartmart.hoa_don SET tong_tien_hang=2000 WHERE id=9001",
                "23514", "immutable financial snapshot");
            await Denied(connection, """
                UPDATE smartmart.hoa_don SET trang_thai='DA_HUY',nguoi_huy_id=9001,
                    huy_luc=now(),ly_do_huy='QA',tong_tien_hang=2000,tong_thanh_toan=2000,
                    tien_khach_dua=2000 WHERE id=9001
                """, "23514", "immutable financial snapshot");
            await Denied(connection, "DELETE FROM smartmart.hoa_don WHERE id=9001", "42501", "permission denied");
            await AdminSql("GRANT DELETE ON smartmart.hoa_don TO smartmart_backend");
            try
            {
                await Denied(connection, "DELETE FROM smartmart.hoa_don WHERE id=9001", "23514", "hoa_don cannot be deleted");
            }
            finally { await AdminSql("REVOKE DELETE ON smartmart.hoa_don FROM smartmart_backend"); }

            await using (var transaction = await connection.BeginTransactionAsync())
            {
                Assert.Equal(1, await Execute(connection, cancel, transaction));
                Assert.Equal("DA_HUY", await Scalar<string>(connection, "SELECT trang_thai FROM smartmart.hoa_don WHERE id=9001", transaction));
                Assert.Equal(before, await Scalar<string>(connection, snapshot, transaction));
                await transaction.RollbackAsync();
            }
            Assert.Equal("DA_THANH_TOAN", await Scalar<string>(connection, "SELECT trang_thai FROM smartmart.hoa_don WHERE id=9001"));
            Assert.True(await Scalar<bool>(connection, "SELECT nguoi_huy_id IS NULL AND huy_luc IS NULL AND ly_do_huy IS NULL FROM smartmart.hoa_don WHERE id=9001"));
            Assert.Equal(before, await Scalar<string>(connection, snapshot));
            Assert.Equal(1, await Execute(connection, cancel));
            await Denied(connection, cancel, "23514", "immutable financial snapshot");
            await Denied(connection, "UPDATE smartmart.hoa_don SET trang_thai='DA_THANH_TOAN' WHERE id=9001",
                "23514", "immutable financial snapshot");
            Assert.Equal("DA_HUY", await Scalar<string>(connection, "SELECT trang_thai FROM smartmart.hoa_don WHERE id=9001"));
            Assert.Equal(before, await Scalar<string>(connection, snapshot));
        });
}
