using ConvenienceStore.Web.Models.Pos;
namespace ConvenienceStore.Web.Services.Pos;

public interface IPosCatalog
{
    IReadOnlyList<PosProduct> Search(string? keyword);
    PosProduct? Find(int id);
}
// Dữ liệu mẫu chỉ phục vụ demo tuần 4. Thay bằng repository SQL khi tích hợp.
public sealed class DemoPosCatalog : IPosCatalog
{
    private static readonly List<PosProduct> Products = new()
    {
        new() { Id=1, Code="SP001", Name="Nước suối 500ml", Price=5000, Stock=80 },
        new() { Id=2, Code="SP002", Name="Mì Hảo Hảo", Price=4500, Stock=120 },
        new() { Id=3, Code="SP003", Name="Sữa tươi 180ml", Price=9000, Stock=50 },
        new() { Id=4, Code="SP004", Name="Bánh quy", Price=18000, Stock=35 },
        new() { Id=5, Code="SP005", Name="Coca Cola lon", Price=12000, Stock=65 },
        new() { Id=6, Code="SP006", Name="Khăn giấy", Price=10000, Stock=40 }
    };
    public IReadOnlyList<PosProduct> Search(string? keyword) =>
        string.IsNullOrWhiteSpace(keyword) ? Products : Products.Where(x =>
            x.Name.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase) ||
            x.Code.Contains(keyword.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
    public PosProduct? Find(int id) => Products.FirstOrDefault(x => x.Id == id);
}
