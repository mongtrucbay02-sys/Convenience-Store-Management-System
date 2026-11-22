# Convenience Store Management System

Hệ thống quản lý cửa hàng tiện lợi — ASP.NET Core MVC (`src/ConvenienceStore.Web`), test tại `tests/ConvenienceStore.Web.Tests`.

## Tài liệu
Toàn bộ tài liệu dự án nằm trong [`docs/`](docs/README.md), tổ chức theo vòng đời `00` → `10`:

`01 Khảo sát → 02 Định nghĩa sản phẩm → 03 Yêu cầu → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu`, cùng `00 Quản lý dự án` và `09 Sử dụng AI` xuyên suốt, tổng hợp vào `10 Báo cáo cuối kỳ`.

## Chạy thử
Cấu hình MySQL cho Báo cáo và Khuyến mãi: [`docs/05-implementation/setup/mysql-reports-setup.md`](docs/05-implementation/setup/mysql-reports-setup.md).

```bash
dotnet run --project src/ConvenienceStore.Web
dotnet test
```
