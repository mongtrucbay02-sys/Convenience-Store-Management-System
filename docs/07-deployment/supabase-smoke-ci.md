# Kiểm tra Supabase thật trong GitHub Actions

Workflow `.github/workflows/supabase-smoke.yml` có tên **Supabase smoke test**. Đọc tối đa một dòng từ mỗi đối tượng ánh xạ EF: 31 bảng và 3 view trong schema `smartmart`. Không ghi giá trị dòng vào log/artifact, chỉ ghi tên đối tượng và số dòng đã đọc (0 hoặc 1).

## Cấu hình

1. Repo GitHub → Settings → Secrets and variables → Actions → New repository secret.
2. Tên: `SUPABASE_DB_CONNECTION`.
3. Giá trị: chuỗi kết nối Npgsql của Supabase, dạng `Host=...;Port=5432;Database=postgres;Username=...;Password=...;SSL Mode=Require;`. Đây không phải URL `postgresql://...`, API key hay khóa Supabase Auth.
4. Dùng tài khoản backend với quyền đọc schema phù hợp. Khuyến nghị nhóm CSDL cấp một vai trò chỉ đọc theo chính sách RLS đã duyệt. Không mở RLS hoặc cấp policy công khai để làm test xanh.
5. Push workflow và công cụ lên nhánh `feature/supabase-entities-20261123`. Lần push thay đổi các đường dẫn đã khai báo sẽ chạy workflow, không cần merge `main` trước. Workflow cũng chạy trên push vào `develop`/`main` khi các đường dẫn liên quan đổi.
6. Nút Actions → Supabase smoke test → Run workflow xuất hiện sau khi workflow nằm trong nhánh mặc định. Tham khảo [GitHub workflow_dispatch](https://docs.github.com/en/actions/how-tos/manage-workflow-runs/manually-run-a-workflow).

Thiếu secret hoặc không đọc được bất kỳ đối tượng nào sẽ báo thất bại, không bỏ qua để báo xanh. Workflow không chạy theo sự kiện PR và không cấp secret cho code PR bên ngoài. Nó chạy code trên những nhánh push đã liệt kê; chỉ cho người tin cậy quyền push và bảo vệ các nhánh đó.

## Phạm vi và giới hạn

- Kết nối Npgsql, schema và khả năng thực thi SELECT/materialize qua `CuaHangDbContext` trên 34 đối tượng thật.
- Bắt buộc `SET TRANSACTION READ ONLY` rồi kiểm tra `SHOW transaction_read_only=on`; không chỉ dựa vào startup option vì pooler có thể bỏ qua. Toàn bộ SELECT nằm trong cùng transaction, rollback sau kiểm tra.
- Không khởi động ứng dụng, tạo tài khoản, đăng nhập, seed hoặc chạy migration/EnsureCreated. Không gọi các fixture tạo/xóa database của bộ test tích hợp.
- Không xác nhận luồng ghi kho–hóa đơn–quỹ, giao diện, đăng nhập hoặc staging. Các ca ghi vẫn chạy trên PostgreSQL riêng ở workflow CI chính. Muốn kiểm thử ghi trên cloud cần CSDL test riêng được nhóm duyệt.
- Bảng rỗng hoặc bị RLS che có thể đọc được 0 dòng; PASS không chứng minh dữ liệu đầy đủ hoặc quyền ghi của ứng dụng.
- Artifact `supabase-readonly-*` chứa kết quả và tên đối tượng lỗi, không chứa mật khẩu/chuỗi kết nối/giá trị dữ liệu. Khi lỗi chỉ hiện loại lỗi và SQLSTATE để tránh rò rỉ thông tin.

## Chạy tại máy

Đặt biến môi trường `SUPABASE_DB_CONNECTION` từ cấu hình riêng ngoài Git rồi chạy:

```powershell
dotnet run --project .\tools\SupabaseSmoke\SupabaseSmoke.csproj -- artifacts/supabase-smoke/result.json
```

Không dùng `ConnectionStrings__CuaHangPostgreSqlTest` hoặc `ConnectionStrings__CuaHangTest` trỏ vào Supabase dùng chung. Chưa push/configure secret thì workflow chưa chạy trên GitHub.
