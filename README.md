# Convenience Store Management System

Hệ thống quản lý cửa hàng tiện lợi — ASP.NET Core MVC (`src/ConvenienceStore.Web`), test tại `tests/ConvenienceStore.Web.Tests`.

## Tài liệu
Toàn bộ tài liệu dự án nằm trong [`docs/`](docs/README.md), tổ chức theo vòng đời `00` → `10`:

`01 Khảo sát → 02 Định nghĩa sản phẩm → 03 Yêu cầu → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu`, cùng `00 Quản lý dự án` và `09 Sử dụng AI` xuyên suốt, tổng hợp vào `10 Báo cáo cuối kỳ`.

Quy trình thay đổi yêu cầu và danh sách người dự phòng: [`CONTRIBUTING.md`](CONTRIBUTING.md).

## Chạy thử
Runtime: .NET 10 MVC/Razor, EF Core/Npgsql, PostgreSQL 17. See [configuration and migrations](docs/db/POSTGRESQL-MIGRATIONS.md). Tests require an isolated loopback PostgreSQL 17 connection via `ConnectionStrings__CuaHangPostgreSqlTest`. Never use shared Supabase for write tests.

```bash
dotnet run --project src/ConvenienceStore.Web
dotnet test
```
