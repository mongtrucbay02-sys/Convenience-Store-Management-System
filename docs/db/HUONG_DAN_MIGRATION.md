# Hướng dẫn tạo migration đầu tiên (InitialCreate) từ ERD v1

Ngày 19/10/2026 · Thang Pham (#1)

## Ý tưởng

Migration đầu tiên phải tạo ra **đúng** CSDL như `schema_v1.sql`: 28 bảng, 3 view, 10 trigger, 53 CHECK, khóa ngoại ghép, cột tính toán và dữ liệu khởi tạo. EF Core không tự sinh được trigger, view và nhiều CHECK, nên dùng cách "baseline":

1. EF Core sinh **entity C#** từ một CSDL đã chạy `schema_v1.sql` (scaffold). Code của mọi người dùng các entity này.
2. Tạo migration `InitialCreate` như bình thường để EF có **model snapshot**.
3. Thay phần thân `Up()` / `Down()` bằng các câu lệnh trong `SchemaV1.cs`, tức chính nội dung `schema_v1.sql`.

Kết quả: CSDL dựng bằng migration giống hệt `schema_v1.sql`. Các migration sau này EF sinh bình thường, so với snapshot.

## Các file đi kèm

| File | Đặt ở đâu trong repo | Ghi chú |
|---|---|---|
| `SchemaV1.cs` | `src/ConvenienceStore.Web/Data/Migrations/` | Sinh tự động từ `schema_v1.sql`, không sửa tay |
| `schema_v1.sql`, `kiem_thu_v1.sql`, `kiem_tra_rang_buoc.sql` | `docs/db/` | Bản thiết kế và bộ kiểm tra |

Đã kiểm tra: `SchemaV1.cs` biên dịch được với .NET 10. Chạy các câu lệnh trong `SchemaV1.Up` lên CSDL trống cho ra cấu trúc giống hệt `schema_v1.sql` (so bằng mysqldump), và `kiem_tra_rang_buoc.sql` đạt 29/29. `SchemaV1.Down` xóa sạch, không còn bảng nào. **Chưa kiểm tra** được bước scaffold và `dotnet ef` thật, vì máy kiểm tra không tải được gói NuGet; bước 6 dưới đây là phần kiểm tra đó.

## Các bước (trong khung máy chủ của #2)

Chạy trong thư mục `src/ConvenienceStore.Web`.

**1. Thêm gói NuGet**

```powershell
dotnet add package MySql.EntityFrameworkCore
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Ghi phiên bản thực tế vào bảng mục 2.2 của ADR-001.

**2. Chuẩn bị CSDL nguồn để scaffold**

Trong Workbench: tạo CSDL `cuahang_scaffold` (utf8mb4, utf8mb4_0900_ai_ci) rồi chạy `schema_v1.sql` vào đó. Không chạy dữ liệu mẫu.

**3. Sinh entity**

Đặt mật khẩu vào biến môi trường để không ghi thẳng vào lệnh:

```powershell
$env:MYSQL_PWD = "mat-khau-root"
dotnet ef dbcontext scaffold "server=localhost;port=3306;database=cuahang_scaffold;user=root;password=$env:MYSQL_PWD" MySql.EntityFrameworkCore `
  --output-dir Data/Entities --context-dir Data --context CuaHangDbContext --no-onconfiguring --force
```

Kiểm tra nhanh:

- Có 28 lớp entity, ví dụ `TaiKhoan`, `SanPham`, `HoaDon`.
- 3 view, nếu được sinh, phải có `.ToView("v_...")` trong `OnModelCreating`, để migration không tạo bảng cho chúng. Nếu provider không sinh view thì không sao, vì view đã nằm trong `SchemaV1`.
- Không commit chuỗi kết nối hay mật khẩu (ADR-001, mục 3.8).

**4. Đăng ký DbContext trong `Program.cs`**

```csharp
builder.Services.AddDbContext<CuaHangDbContext>(o =>
    o.UseMySQL(builder.Configuration.GetConnectionString("CuaHang")!)
     .AddInterceptors(new GioVietNamInterceptor()));
```

`GioVietNamInterceptor` là một `DbConnectionInterceptor`, chạy `SET time_zone = '+07:00'` mỗi khi mở kết nối (ADR-001). Chuỗi kết nối để trong `appsettings.Development.json` hoặc biến môi trường `ConnectionStrings__CuaHang`.

**5. Tạo migration rồi thay phần thân**

```powershell
dotnet ef migrations add InitialCreate --output-dir Data/Migrations
```

Chép `SchemaV1.cs` vào `Data/Migrations/`. Mở file `..._InitialCreate.cs` vừa sinh, xóa toàn bộ phần thân EF tự sinh trong `Up` và `Down`, rồi thay bằng:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    foreach (var sql in SchemaV1.Up)
        migrationBuilder.Sql(sql);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    foreach (var sql in SchemaV1.Down)
        migrationBuilder.Sql(sql);
}
```

**Giữ nguyên** hai file `..._InitialCreate.Designer.cs` và `CuaHangDbContextModelSnapshot.cs`.

**6. Kiểm tra trên CSDL trống**

```powershell
dotnet ef database update --connection "server=localhost;port=3306;database=cuahang_dev;user=root;password=$env:MYSQL_PWD"
```

Nếu `cuahang_dev` chưa có, tạo trống trước bằng Workbench. Sau đó, trong Workbench, chạy `kiem_thu_v1.sql` rồi `kiem_tra_rang_buoc.sql` trên `cuahang_dev`. **Đạt khi 29/29.**

Tiếp theo chạy `dotnet ef migrations add KiemTraKhongLech`. Migration sinh ra phải **trống** (Up/Down không có lệnh). Nếu có lệnh, model và CSDL đang lệch: xóa migration thử đó (`dotnet ef migrations remove`) và báo #1.

**7. Mở pull request**

PR gồm: gói NuGet, `Data/Entities`, `CuaHangDbContext`, `Data/Migrations` (3 file + `SchemaV1.cs`), `Program.cs`. #1 duyệt (T4-R1). #4 đưa bước 6 vào CI.

## Quy tắc cho các migration sau

- Thay đổi CSDL = sửa entity rồi `dotnet ef migrations add <TenThayDoi>`. Mỗi PR có migration phải được #1 duyệt.
- CHECK, trigger, view mới: thêm bằng `migrationBuilder.Sql(...)` trong migration đó.
- Cập nhật cùng lúc `schema_v1.sql` và ERD, để tài liệu khớp CSDL thật (SRS bản cuối, T9-01).
- Không sửa `SchemaV1.cs` và migration `InitialCreate` sau khi đã gộp vào `main`.

## Lỗi hay gặp

| Lỗi | Nguyên nhân | Cách xử lý |
|---|---|---|
| 1419 khi tạo trigger | Binary log bật, tài khoản không có quyền SUPER | Docker/CI: chạy MySQL với `--disable-log-bin` (ADR-001). Máy cá nhân dùng root thì không gặp. Aiven: kiểm tra khi dựng staging |
| 1050 Table already exists | Chạy `database update` lên CSDL đã có bảng (ví dụ CSDL đã chạy `schema_v1.sql` bằng tay) | Dùng CSDL trống cho migration; CSDL đã chạy tay chỉ dùng để scaffold |
| Migration lỗi giữa chừng | MySQL không rollback được lệnh DDL | Xóa CSDL đó, tạo lại trống, chạy lại |
| `dotnet ef` không nhận | Thiếu gói `Microsoft.EntityFrameworkCore.Design` hoặc chưa mở lại Terminal | Thêm gói (bước 1), mở lại Terminal |
