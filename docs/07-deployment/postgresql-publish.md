Current integration update 2026-11-24: Release is now PostgreSQL-only. Run `scripts/ci/verify-postgresql-runtime.ps1` against publish output; MySQL packages and development configuration must be absent. Follow [baseline adoption and runtime-role requirements](../db/POSTGRESQL-MIGRATIONS.md). The evidence below is dated 2026-11-23 and does not certify the current working tree or a deployment.

---

# Publish và bàn giao PostgreSQL

Trạng thái 23/11/2026: gói publish Release đã chạy thử đăng nhập, đổi mật khẩu tạm và báo cáo trên PostgreSQL test riêng. Chưa triển khai cloud/staging.

## Kiểm tra staging hiện có

Đã kiểm tra GitHub API của repository: 0 Environments và 0 Deployments. Không tìm thấy URL staging hoặc workflow deploy trong repo. Kết quả này không xác nhận các triển khai thủ công ngoài GitHub; cần URL và quyền truy cập môi trường đó để nghiệm thu.

CI là kiểm thử tự động; CI xanh không tự tạo staging. Workflow hiện tại chỉ build/test và upload TRX.

## Publish

Từ thư mục repository:

```powershell
dotnet restore .\ConvenienceStoreManagement.slnx
dotnet publish .\src\ConvenienceStore.Web\ConvenienceStore.Web.csproj -c Release -o .\artifacts\publish
```

Gói publish không chứa `.env*` hay `appsettings.Development.json`; không đóng gói mật khẩu máy lập trình. Đặt biến môi trường trên máy chủ:

```dotenv
Database__Provider=PostgreSql
ConnectionStrings__CuaHang=Host=HOST_POOLER;Port=5432;Database=postgres;Username=BACKEND_USER;Password=DATABASE_PASSWORD;SSL Mode=Require;
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://127.0.0.1:5292
```

Hoặc tự tạo `.env` ngoài wwwroot tại working directory của ứng dụng, chỉ cho tài khoản chạy dịch vụ đọc. Không đưa file đó vào Git hay gói chia sẻ. Xem [cấu hình Supabase](../05-implementation/setup/postgresql-runtime-setup.md).

Chạy từ thư mục publish bằng `dotnet ConvenienceStore.Web.dll`. Máy chủ cần .NET 10 Runtime. Để truy cập từ mạng ngoài, nhóm triển khai cần cấu hình domain/reverse proxy HTTPS, firewall và process manager phù hợp; chưa có các giá trị đó trong repo để tự triển khai. Không mở cổng DB trực tiếp cho trình duyệt.

## Nghiệm thu và rollback

1. Ghi commit triển khai, URL và môi trường/CSDL; sao lưu trước bất kỳ thao tác nâng cấp CSDL chung nào.
2. Dùng tài khoản kiểm thử được nhóm duyệt để kiểm tra đăng nhập/đổi mật khẩu, AD/TN, bán hàng, điểm, quỹ, nhập/xuất kho và báo cáo trên CSDL staging riêng.
3. Kiểm tra các ca bị từ chối (phân quyền, CSRF, tồn không đủ, lặp thanh toán, lặp hoàn tiền).
4. Nếu lỗi ứng dụng: quay lại gói publish/config trước đó. Không xóa schema hoặc chạy migration MySQL để rollback PostgreSQL. Hoàn nguyên dữ liệu chỉ theo kế hoạch/backup do nhóm CSDL duyệt.

Không ghi nhận staging đạt cho tới khi có URL chạy thật và biên bản kết quả.
