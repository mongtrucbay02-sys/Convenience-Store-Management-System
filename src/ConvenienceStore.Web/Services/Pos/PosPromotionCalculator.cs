using System.Data.Common;

namespace ConvenienceStore.Web.Services.Pos;

public sealed record PosPromotion(int Id, int ProductId, string Name, string Type,
    long Value, long? Cap, bool MembersOnly, DateTime EndAt);

public static class PosPromotionCalculator
{
    public static long Discount(long price, PosPromotion promotion)
    {
        if (price <= 0) return 0;
        decimal amount = promotion.Type switch
        {
            "PHAN_TRAM" => decimal.Floor((decimal)price * promotion.Value / 100 / 100) * 100,
            "SO_TIEN" => promotion.Value,
            _ => 0
        };
        if (promotion.Type == "PHAN_TRAM" && promotion.Cap is > 0)
            amount = Math.Min(amount, promotion.Cap.Value);
        return (long)Math.Clamp(amount, 0, price);
    }

    public static PosPromotion? Best(IEnumerable<PosPromotion> promotions, int productId,
        long price, bool member) => promotions
        .Where(p => p.ProductId == productId && (!p.MembersOnly || member) && Discount(price, p) > 0)
        .OrderByDescending(p => Discount(price, p)).ThenBy(p => p.EndAt).ThenBy(p => p.Id)
        .FirstOrDefault();

    // Caller owns the cart lock and transaction. Shared by cart refresh and POS-07.
    public static async Task<List<PosPromotion>> ReadAsync(DbConnection connection,
        DbTransaction transaction, CancellationToken ct = default)
    {
        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = """
            SELECT p.id, s.san_pham_id, p.ten, p.loai_giam, p.gia_tri,
                   p.giam_toi_da, p.chi_thanh_vien, p.ket_thuc_luc
            FROM chuong_trinh_khuyen_mai p
            JOIN khuyen_mai_san_pham s ON s.chuong_trinh_id = p.id
            WHERE p.trang_thai = 'DA_KICH_HOAT'
              AND p.bat_dau_luc <= NOW() AND p.ket_thuc_luc > NOW()
            ORDER BY p.id, s.san_pham_id
            FOR SHARE
            """;
        var result = new List<PosPromotion>();
        await using var reader = await command.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add(new(Convert.ToInt32(reader[0]), Convert.ToInt32(reader[1]),
                reader.GetString(2), reader.GetString(3), Convert.ToInt64(reader[4]),
                reader.IsDBNull(5) ? null : Convert.ToInt64(reader[5]),
                Convert.ToBoolean(reader[6]), reader.GetDateTime(7)));
        return result;
    }
}
