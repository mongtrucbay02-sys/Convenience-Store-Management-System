using System.Data.Common;
namespace ConvenienceStore.Web.Services.Pos;

public static class HuyGioDangBan
{
    // Caller owns the transaction; cart mutations serialize on the parent row.
    public static async Task HuyAsync(DbConnection connection, DbTransaction transaction,
        int cashierId, CancellationToken ct = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = "SELECT id FROM gio_hang WHERE thu_ngan_id = @t AND trang_thai = 'DANG_BAN' FOR UPDATE";
        var parameter = command.CreateParameter();
        parameter.ParameterName = "@t";
        parameter.Value = cashierId;
        command.Parameters.Add(parameter);
        var id = await command.ExecuteScalarAsync(ct);
        if (id is null or DBNull) return;
        command.Parameters.Clear();
        parameter.ParameterName = "@g";
        parameter.Value = id;
        command.Parameters.Add(parameter);
        command.CommandText = """
            UPDATE gio_hang
            SET trang_thai = 'DA_HUY',
                huy_luc = NOW(),
                ly_do_huy = 'THU_NGAN_HUY',
                gia_tri_khi_huy = COALESCE((
                    SELECT CAST(SUM(so_luong * (don_gia - giam_don_vi)) AS UNSIGNED)
                    FROM ct_gio_hang WHERE gio_hang_id = @g), 0)
            WHERE id = @g AND trang_thai = 'DANG_BAN'
            AND EXISTS (SELECT 1 FROM ct_gio_hang WHERE gio_hang_id = @g)
            """;
        await command.ExecuteNonQueryAsync(ct);
    }
}