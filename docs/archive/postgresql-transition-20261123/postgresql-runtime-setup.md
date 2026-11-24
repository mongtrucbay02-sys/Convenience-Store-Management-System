# Chạy SmartMart bằng PostgreSQL / Supabase

Trạng thái 23/11/2026: đã chuyển SQL nghiệp vụ, kiểm thử Release trên PostgreSQL 17.12 riêng và MySQL 8.4.13 riêng. Chưa có URL staging được ghi nhận trong repo/GitHub.

## Cấu hình

Cài .NET SDK 10. Project đã tham chiếu Npgsql; không cần cài MySQL để kết nối Supabase. Trong `src/ConvenienceStore.Web`, điền file `.env` theo `.env.postgresql.example`:

```dotenv
Database__Provider=PostgreSql
ConnectionStrings__CuaHang="Host=aws-1-ap-northeast-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.YOUR_PROJECT_REF;Password=YOUR_DATABASE_PASSWORD;SSL Mode=Require;"
```

Dùng Database password của Supabase và project ref của nhóm. Không dùng mật khẩu tài khoản web. Không commit `.env`; `.env` phải nằm cạnh csproj, ngoài wwwroot. Biến môi trường có ưu tiên cao hơn `.env`; xóa biến cũ trong PowerShell nếu muốn dùng file. Ứng dụng tự đặt schema `smartmart` và múi giờ phiên `Asia/Ho_Chi_Minh` cho Npgsql.

Nếu có nhiều checkout, chỉ chỉnh cấu hình của checkout đang chạy. Những file đọc kết nối còn tên `MySql*` được giữ để tương thích với code/test cũ, nhưng chọn provider/kết nối qua `SqlDialect`, không ép Npgsql qua MySQL driver.

## Dữ liệu nền và migration

- Supabase đã có 31 bảng: **không chạy lại** `docs/db/schema_postgresql.sql` lên CSDL đó. Script này là DDL tham chiếu dựng CSDL rỗng/test, có 3 hàm, 17 trigger và 3 view lấy từ catalog thật. Đây không phải migration nâng cấp, và không sao chép toàn bộ thiết lập RLS/grant của Supabase.
- `docs/db/seed_postgresql_reference.sql` chuẩn bị ba vai trò, quy tắc điểm, chính sách trả và dòng quỹ 1. Nhóm CSDL kiểm tra trước khi chạy trên CSDL chung. Script không tạo user/mật khẩu mẫu; chạy lại không ghi đè quỹ, chính sách hay quy tắc hiện có. Đã kiểm tra chạy lặp trên database tạm.
- Migration C# hiện có dành cho MySQL. Guard tiếp tục chặn `Migrate` khi dùng Npgsql. Cần nhóm CSDL duyệt baseline PostgreSQL riêng nếu muốn quản lý migration qua EF.
- Không tắt RLS hay cấp quyền anon/authenticated cho ứng dụng. Tài khoản DB chỉ cấu hình tại backend; quyền DB/RLS thực tế trên Supabase cần nhóm CSDL review riêng.

Nếu chưa có Admin, sau khi nhóm chuẩn bị vai trò AD, đặt trong cùng cửa sổ PowerShell:

```powershell
$env:AdminDauTien__TenDangNhap = "trung_admin_test"
$secure = Read-Host "Mật khẩu tạm Admin mới" -AsSecureString
$env:AdminDauTien__MatKhauTam = [System.Net.NetworkCredential]::new("", $secure).Password
$secure.Dispose()
dotnet run --project .\src\ConvenienceStore.Web\ConvenienceStore.Web.csproj --no-launch-profile --urls http://127.0.0.1:5292
```

Chỉ tạo Admin khi CSDL chưa có tài khoản vai trò AD. Đổi biến môi trường không đặt lại mật khẩu user đã tồn tại. Mở `/login.html`, đăng nhập rồi đổi mật khẩu tạm khi được yêu cầu. Admin quản lý tài khoản Thu ngân qua chức năng AUTH, không tạo user bằng mật khẩu băm tự đoán.

## Các đường dẫn kiểm thử

Admin: `/Dashboard`, `/Customers`, `/Products`, `/Categories`, `/NhaCungCap`, `/NhapHang`, `/Kho`, `/Kho/ChungTu`, `/CashLedger`, `/Reports/Reports/Dashboard`.
Thu ngân: `/Pos`, `/Customers?attachToCart=true`, `/Products`. Các báo cáo/quỹ/tài khoản quản trị bị chặn theo quyền; Admin không được cấp quyền POS của Thu ngân.

Admin kiểm đếm và chốt số dư mở quỹ tại `/CashLedger` trước khi thực hiện hoàn tiền dựa trên quỹ. Không đặt số dư giả để vượt kiểm tra `QUY_KHONG_DU`.

## Test riêng

Không trỏ biến test vào Supabase/Aiven Dev. Test PostgreSQL mới từ chối host ngoài loopback và tự tạo/xóa `smartmart_pgtest_<GUID>`.

```powershell
$env:ConnectionStrings__CuaHangPostgreSqlTest = "Host=127.0.0.1;Port=5432;Database=postgres;Username=postgres;Password=TEST_ONLY;Pooling=false;"
dotnet test .\tests\ConvenienceStore.Web.Tests\ConvenienceStore.Web.Tests.csproj --filter "Database=PostgreSql"
```

Chạy đủ bộ cần thêm `ConnectionStrings__CuaHangTest` cho MySQL test riêng. Workflow `.github/workflows/ci.yml` dựng hai service PostgreSQL 17/MySQL 8.4 cho từng run; không dùng DB cloud của nhóm.

Kết quả và giới hạn: [báo cáo chuyển SQL](../../06-testing/test-reports/2026-11-23-postgresql-runtime.md).
