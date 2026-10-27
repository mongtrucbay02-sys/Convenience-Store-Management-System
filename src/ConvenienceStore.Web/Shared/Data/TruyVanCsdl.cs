using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace ConvenienceStore.Web.Shared.Data;

public static class TruyVanCsdl
{
    public static async Task<DbCommand> TaoLenhAsync(
        this DbContext db, string sql, CancellationToken ct, params (string Ten, object? GiaTri)[] thamSo)
    {
        await db.Database.OpenConnectionAsync(ct);
        var lenh = db.Database.GetDbConnection().CreateCommand();
        lenh.CommandText = sql;
        lenh.Transaction = db.Database.CurrentTransaction?.GetDbTransaction();
        foreach (var (ten, giaTri) in thamSo)
        {
            var p = lenh.CreateParameter();
            p.ParameterName = ten;
            p.Value = giaTri ?? DBNull.Value;
            lenh.Parameters.Add(p);
        }

        return lenh;
    }

    public static async Task<int> ChayAsync(
        this DbContext db, string sql, CancellationToken ct, params (string Ten, object? GiaTri)[] thamSo)
    {
        await using var lenh = await db.TaoLenhAsync(sql, ct, thamSo);
        return await lenh.ExecuteNonQueryAsync(ct);
    }

    public static async Task<object?> DocMotGiaTriAsync(
        this DbContext db, string sql, CancellationToken ct, params (string Ten, object? GiaTri)[] thamSo)
    {
        await using var lenh = await db.TaoLenhAsync(sql, ct, thamSo);
        var ketQua = await lenh.ExecuteScalarAsync(ct);
        return ketQua is DBNull ? null : ketQua;
    }

    public static async Task<List<T>> DocDanhSachAsync<T>(
        this DbContext db, string sql, Func<DbDataReader, T> chuyenDoi, CancellationToken ct,
        params (string Ten, object? GiaTri)[] thamSo)
    {
        await using var lenh = await db.TaoLenhAsync(sql, ct, thamSo);
        await using var doc = await lenh.ExecuteReaderAsync(ct);
        var danhSach = new List<T>();
        while (await doc.ReadAsync(ct))
            danhSach.Add(chuyenDoi(doc));
        return danhSach;
    }

    public static async Task<T?> DocMotDongAsync<T>(
        this DbContext db, string sql, Func<DbDataReader, T> chuyenDoi, CancellationToken ct,
        params (string Ten, object? GiaTri)[] thamSo)
        where T : class
    {
        var danhSach = await db.DocDanhSachAsync(sql, chuyenDoi, ct, thamSo);
        return danhSach.Count == 0 ? null : danhSach[0];
    }

    public static (string DanhSachThamSo, (string Ten, object? GiaTri)[] ThamSo) TaoThamSoIn(
        string tienTo, IEnumerable<int> giaTri)
    {
        var thamSo = giaTri.Select((g, i) => ($"@{tienTo}{i}", (object?)g)).ToArray();
        return (string.Join(", ", thamSo.Select(t => t.Item1)), thamSo);
    }

    public static bool LaLoiTrungKhoa(DbException loi, string tenKhoa) =>
        loi.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase)
        && loi.Message.Contains(tenKhoa, StringComparison.OrdinalIgnoreCase);
}
