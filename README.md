# Convenience Store Management System

Hệ thống quản lý cửa hàng tiện lợi — ASP.NET Core MVC (`src/ConvenienceStore.Web`), test tại `tests/ConvenienceStore.Web.Tests`.

## Tài liệu
Toàn bộ tài liệu dự án nằm trong [`docs/`](docs/README.md), tổ chức theo vòng đời `00` → `10`:

`01 Khảo sát → 02 Định nghĩa sản phẩm → 03 Yêu cầu → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu`, cùng `00 Quản lý dự án` và `09 Sử dụng AI` xuyên suốt, tổng hợp vào `10 Báo cáo cuối kỳ`.

Quy trình thay đổi yêu cầu và danh sách người dự phòng: [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Chạy thử
Runtime: .NET 10 MVC/Razor, EF Core/Npgsql, PostgreSQL 17. See [configuration and migrations](docs/db/POSTGRESQL-MIGRATIONS.md). Cấu hình kết nối: tạo `src/ConvenienceStore.Web/.env` theo `.env.example` ([`CONTRIBUTING.md`](CONTRIBUTING.md) mục 4.2).

```bash
dotnet build
dotnet run --project src/ConvenienceStore.Web
```

Test tự động chạy trên CI: mỗi PR, GitHub Actions dựng PostgreSQL 17 tạm, chạy toàn bộ test rồi xóa. Không cần chạy `dotnet test` trên máy và không bao giờ trỏ test vào Supabase dùng chung.
