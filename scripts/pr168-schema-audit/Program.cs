using System.Text.Json;
using ConvenienceStore.Web.Tests.HaTang;
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

var fixture = new CsdlMySqlTam();
await fixture.InitializeAsync();
try
{
    await using var context = fixture.TaoDbContext();
    var columns = context.Model.GetRelationalModel().Tables.SelectMany(t => t.Columns.Select(c => new {
        Key = t.Name + "." + c.Name, c.StoreType, c.IsNullable, c.DefaultValueSql,
        Generated = c.PropertyMappings.First().Property.ValueGenerated.ToString()
    })).ToDictionary(c => c.Key);
    await using var connection = await fixture.MoKetNoiAsync();
    await using var command = new MySqlCommand("""
        SELECT TABLE_NAME,COLUMN_NAME,COLUMN_TYPE,IS_NULLABLE,COLUMN_DEFAULT,EXTRA,GENERATION_EXPRESSION
        FROM information_schema.COLUMNS WHERE TABLE_SCHEMA=DATABASE() AND TABLE_NAME <> '__EFMigrationsHistory'
        ORDER BY TABLE_NAME,ORDINAL_POSITION
        """, connection);
    await using var reader = await command.ExecuteReaderAsync();
    var typeMismatches = new List<object>();
    var nullabilityMismatches = new List<object>();
    var defaultRepresentations = new List<object>();
    var computed = new List<object>();
    var inspected = 0;
    while (await reader.ReadAsync())
    {
        var key = reader.GetString(0) + "." + reader.GetString(1);
        if (!columns.TryGetValue(key, out var mapped)) continue; // views audited separately
        inspected++;
        var actualType = reader.GetString(2);
        var nullable = reader.GetString(3) == "YES";
        var defaultSql = reader.IsDBNull(4) ? null : reader.GetString(4);
        var extra = reader.GetString(5);
        var expression = reader.IsDBNull(6) ? "" : reader.GetString(6);
        if (expression.Length > 0) computed.Add(new { key, mapped.Generated, expression });
        if (!string.Equals(mapped.StoreType, actualType, StringComparison.OrdinalIgnoreCase))
            typeMismatches.Add(new { key, model = mapped.StoreType, database = actualType });
        if (mapped.IsNullable != nullable)
            nullabilityMismatches.Add(new { key, model = mapped.IsNullable, database = nullable });
        // Literal representations only; this is not a semantic default comparison.
        if (mapped.DefaultValueSql != defaultSql)
            defaultRepresentations.Add(new { key, model = mapped.DefaultValueSql, database = defaultSql, extra });
    }
    await reader.DisposeAsync();
    await using var versionCommand = new MySqlCommand("SELECT VERSION()", connection);
    var version = Convert.ToString(await versionCommand.ExecuteScalarAsync());
    await fixture.ChayAsync("""
        INSERT INTO tai_khoan (ten_dang_nhap,mat_khau_bam,ho_ten,vai_tro_id)
        SELECT 'schema_probe','test-only-hash','Schema Probe',id FROM vai_tro WHERE ma='AD';
        INSERT INTO danh_muc (ten) VALUES ('schema_probe');
        INSERT INTO san_pham (ma,ten_san_pham,danh_muc_id,don_vi_tinh,nguoi_tao_id)
        SELECT 'SP-PROBE','Probe',d.id,'cai',t.id FROM danh_muc d CROSS JOIN tai_khoan t
        WHERE d.ten='schema_probe' AND t.ten_dang_nhap='schema_probe';
        INSERT INTO nha_cung_cap (ma,ten,so_dien_thoai) VALUES ('NCC-PROBE','Probe','0901234567');
        INSERT INTO phieu_nhap (ma_phieu,nha_cung_cap_id,ngay_nhap,nguoi_lap_id)
        SELECT 'PN-PROBE',n.id,'2026-11-14',t.id FROM nha_cung_cap n CROSS JOIN tai_khoan t
        WHERE n.ma='NCC-PROBE' AND t.ten_dang_nhap='schema_probe';
        """);
    var productId = Convert.ToUInt32(await fixture.DocMotGiaTriAsync("SELECT id FROM san_pham WHERE ma='SP-PROBE'"));
    var receiptId = Convert.ToUInt32(await fixture.DocMotGiaTriAsync("SELECT id FROM phieu_nhap WHERE ma_phieu='PN-PROBE'"));
    context.CtPhieuNhaps.Add(new ConvenienceStore.Web.Data.Entities.CtPhieuNhap {
        PhieuNhapId = receiptId, SanPhamId = productId, TenSanPham = "Probe", DonViTinh = "cai", SoLuong = 2, GiaNhap = 1000
    });
    object computedInsert;
    try {
        await context.SaveChangesAsync();
        computedInsert = new { outcome = "succeeded", value = context.CtPhieuNhaps.Local.Single().ThanhTien };
    } catch (DbUpdateException exception) when (exception.InnerException is MySqlException mysql) {
        computedInsert = new { outcome = "failed", mysqlError = mysql.Number, mysql.Message };
    }
    var report = new { serverVersion = version, mappedTableColumns = columns.Count, inspected,
        typeMismatches, nullabilityMismatches, defaultRepresentations,
        computedColumns = computed, computedColumnInsertProbe = computedInsert,
        tokens = context.Model.GetEntityTypes().SelectMany(e => e.GetProperties()
            .Where(p => p.Name == "PhienBan" && e.GetTableName() != null)
            .Select(p => new { table = e.GetTableName(), p.IsConcurrencyToken })).ToArray() };
    Directory.CreateDirectory("docs/tests/evidence/pr168");
    await File.WriteAllTextAsync("docs/tests/evidence/pr168/column-audit.json", JsonSerializer.Serialize(report, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Version={version}; mapped={columns.Count}; inspected={inspected}; type mismatches={typeMismatches.Count}; nullability mismatches={nullabilityMismatches.Count}; default representations={defaultRepresentations.Count}; computed={computed.Count}");
}
finally { await fixture.DisposeAsync(); }
