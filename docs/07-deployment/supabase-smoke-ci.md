# Kiểm tra Supabase thật trong GitHub Actions

Workflow `.github/workflows/supabase-smoke.yml` có tên **Supabase smoke test**. Đọc tối đa một dòng từ mỗi đối tượng ánh xạ EF: 31 bảng và 3 view trong schema `smartmart`. Không ghi giá trị dòng vào log/artifact, chỉ ghi tên đối tượng và số dòng đã đọc (0 hoặc 1).

## Cấu hình

1. Repo GitHub → Settings → Secrets and variables → Actions → New repository secret.
2. Tên: `SUPABASE_DB_CONNECTION`.
3. Giá trị: chuỗi kết nối Npgsql của Supabase, dạng `Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=VerifyFull;Root Certificate=artifacts/supabase-ca.pem;`. Đây không phải URL `postgresql://...`, API key hay khóa Supabase Auth.
4. Tải CA chính thức dạng PEM từ Database Settings của project Supabase. Tạo repository secret `SUPABASE_DB_CA_CERT` chứa nguyên văn PEM (không phải đường dẫn, không phải private key). Workflow tạo `artifacts/supabase-ca.pem` với quyền hạn chế, kiểm tra cú pháp PEM bằng OpenSSL và xóa file sau khi chạy. Secret `SUPABASE_DB_CONNECTION` phải dùng `SSL Mode=VerifyFull;Root Certificate=artifacts/supabase-ca.pem;`. Không dùng Trust Server Certificate=true, không fallback. Không đưa credential hoặc CA vào Git. Khi chạy cục bộ, dùng đường dẫn CA thực tại máy.
5. Dùng đúng role PostgreSQL `smartmart_backend` (username pooler có thể kèm project ref). Smoke xác minh cả `current_user` và `session_user` trong chính transaction read-only trước mọi probe; từ chối SUPERUSER, BYPASSRLS, CREATEROLE, CREATEDB hoặc row_security=off. Không mở RLS hoặc cấp policy công khai để làm test xanh.
6. Nhánh tích hợp hiện tại là `develop`. Workflow tự chạy khi push `main`, `develop` hoặc nhánh nguồn cũ `feature/supabase-entities-20261123` và có thay đổi thuộc paths đã khai báo. Push nhánh feature/PR khác không tự kích hoạt live Smoke; chỉ dispatch nhánh đã được review và phê duyệt. Không tự sửa nhánh mặc định, trigger hoặc merge để chạy smoke.
7. Nút Actions → Supabase smoke test → Run workflow xuất hiện sau khi workflow nằm trong nhánh mặc định. Tham khảo [GitHub workflow_dispatch](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow).

Thiếu secret hoặc không đọc được bất kỳ đối tượng nào sẽ báo thất bại, không bỏ qua để báo xanh. Workflow không chạy theo sự kiện PR và không cấp secret cho code PR bên ngoài. Nó chạy code trên những nhánh push đã liệt kê; chỉ cho người tin cậy quyền push và bảo vệ các nhánh đó.

## Phạm vi và giới hạn

- Kết nối Npgsql, schema và khả năng thực thi SELECT/materialize qua `CuaHangDbContext` trên 34 đối tượng thật.
- Remote bắt buộc `VerifyFull` và file CA hiện hữu; giữ nguyên đường dẫn CA cho Npgsql. Npgsql 10 ưu tiên GSS theo mặc định, vì vậy tool tắt GSS cho remote để bắt buộc TLS. Localhost/loopback được giữ cấu hình TLS test cục bộ.
- Bắt buộc `SET TRANSACTION READ ONLY` rồi kiểm tra `SHOW transaction_read_only=on`; không chỉ dựa vào startup option vì pooler có thể bỏ qua. Toàn bộ SELECT nằm trong cùng transaction, rollback sau kiểm tra.
- Artifact bổ sung `roleVerified`, `currentUser`, `sessionUser`, `isSuperuser`, `bypassRls`, `canCreateRole`, `canCreateDatabase`, `rowSecurity`. Tên role ngoài allowlist được che bằng null; khi chưa lấy được bằng chứng, roleVerified=false và các thuộc tính chưa biết là null. Role guard áp dụng cả local: dùng role test cùng tên, không dùng postgres. Đây không phải kiểm chứng toàn bộ RLS policy/GRANT.
- Không khởi động ứng dụng, tạo tài khoản, đăng nhập, seed hoặc chạy migration/EnsureCreated. Không gọi các fixture tạo/xóa database của bộ test tích hợp.
- Không xác nhận luồng ghi kho–hóa đơn–quỹ, giao diện, đăng nhập hoặc staging. Các ca ghi vẫn chạy trên PostgreSQL riêng ở workflow CI chính. Muốn kiểm thử ghi trên cloud cần CSDL test riêng được nhóm duyệt.
- Bảng rỗng hoặc bị RLS che có thể đọc được 0 dòng; PASS không chứng minh dữ liệu đầy đủ hoặc quyền ghi của ứng dụng.
- Báo cáo được tạo với `FileMode.CreateNew`: không ghi đè bằng chứng đã có, và từ chối đường dẫn trùng trước khi kết nối. Chọn một tên mới cho mỗi lần chạy.
- Artifact `supabase-readonly-*` chứa kết quả và tên đối tượng lỗi, không chứa mật khẩu/chuỗi kết nối/giá trị dữ liệu. Khi lỗi chỉ hiện loại lỗi và SQLSTATE để tránh rò rỉ thông tin.

## Kiểm thử tích hợp role trên PostgreSQL 17 cô lập

`SupabaseRuntimeRoleIntegrationTests` dùng service PostgreSQL 17 riêng trong SmartMart CI, khác cổng với service kiểm thử nghiệp vụ. Biến `ConnectionStrings__CuaHangPostgreSqlRoleTest` chỉ trỏ tới instance test chuyên dụng, host 127.0.0.1, không chứa dữ liệu dự án. Fixture từ chối host khác, thiếu biến, trùng cổng với service nghiệp vụ, cluster đã có database ngoài postgres/template hoặc đã có role smartmart_backend. Fixture tạo role/database test, chạy baseline và xóa đối tượng do fixture tạo khi kết thúc. Không trỏ biến này tới Supabase hoặc PostgreSQL dùng chung.

Các ca kiểm thử chạy truy vấn catalog thực tế qua Npgsql: role hợp lệ và artifact 34/34, role sai, SUPERUSER, BYPASSRLS, CREATEROLE, CREATEDB, row_security off, current_user khác session_user và transaction không chỉ đọc. Các ca nâng quyền chỉ diễn ra trong cluster test riêng. Live Smoke vẫn cần xác minh riêng trên kết nối Supabase được phê duyệt.

## Chạy tại máy

Đặt biến môi trường `SUPABASE_DB_CONNECTION` từ cấu hình riêng ngoài Git rồi chạy:

```powershell
$report = "artifacts/supabase-smoke/result-$(Get-Date -Format yyyyMMdd-HHmmssfff).json"
dotnet run --project .\tools\SupabaseSmoke\SupabaseSmoke.csproj -c Release -- $report
```

Không dùng `ConnectionStrings__CuaHangPostgreSqlTest` hoặc `ConnectionStrings__CuaHangTest` trỏ vào Supabase dùng chung. Chưa push/configure secret thì workflow chưa chạy trên GitHub.

## Bằng chứng VerifyFull hiện tại

Xem [báo cáo TLS/EF ngày 24/11](../06-testing/test-reports/2026-11-24-supabase-smoke-verifyfull.md). Bằng chứng smoke cũ chỉ chứng minh kết nối/read-only tại thời điểm chạy, không chứng minh `VerifyFull` vì báo cáo cũ thiếu trường TLS. Hướng dẫn phiên này thay thế ví dụ `Require` trước đó.

Npgsql [phân biệt Require và VerifyFull, xác minh CA/hostname](https://www.npgsql.org/doc/security.html) và hỗ trợ [Root Certificate](https://www.npgsql.org/doc/connection-string-parameters.html). Changelog Supabase đã được rà soát; không có thay đổi liên quan buộc phải sửa schema cho công việc này.
