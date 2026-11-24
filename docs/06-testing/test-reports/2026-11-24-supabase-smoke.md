# Kiểm tra Supabase smoke workflow — 24/11/2026

## Thay đổi

- `.github/workflows/supabase-smoke.yml`: job Supabase riêng dùng repository secret `SUPABASE_DB_CONNECTION`; không sửa workflow test MySQL/PostgreSQL dùng một lần.
- `tools/SupabaseSmoke`: chương trình .NET 10 tham chiếu model của ứng dụng, đọc tối đa một dòng từ mỗi bảng/view trong transaction PostgreSQL bắt buộc chỉ đọc. Không khởi động ứng dụng hoặc gọi initializer AUTH, seed, migration.
- Cách cấu hình và giới hạn: [Supabase smoke CI](../../07-deployment/supabase-smoke-ci.md).

## Kết quả đã thực hiện

| Ca | Kết quả |
|---|---|
| Build Debug công cụ và project web tham chiếu | 0 lỗi, 0 cảnh báo |
| Không có `SUPABASE_DB_CONNECTION` | Exit 2, báo thiếu secret, không kết nối |
| PostgreSQL thử riêng, schema đầy đủ | PASS: 31 bảng + 3 view, 34 SELECT qua EF; `transaction_read_only=on` |
| PostgreSQL thử riêng, thiếu schema | Exit 1, SQLSTATE 42P01, báo đối tượng thiếu; không báo PASS |
| Supabase thật, cấu hình cũ | Exit 1, SQLSTATE **28P01** (thông tin xác thực bị từ chối). Chưa mở transaction hoặc đọc dữ liệu |
| Supabase thật, sau cập nhật cấu hình riêng ngoài Git | **PASS: 31 bảng + 3 view, 34 SELECT qua EF, transaction_read_only=on; không ghi dữ liệu** |

Log/artifact chỉ ghi tên đối tượng, số dòng đọc 0/1, trạng thái chỉ đọc và loại lỗi/SQLSTATE. Không xuất giá trị dữ liệu, password, connection string hoặc stack trace. Kết nối thử riêng ở loopback, không trỏ fixture tạo/xóa CSDL vào cloud.

## Còn chờ

- Cấu hình cục bộ đã kết nối được; repository secret `SUPABASE_DB_CONNECTION` đã được tạo trên GitHub ngày 24/11/2026, không lưu giá trị trong repo.
- Push workflow/tool và xác nhận GitHub Actions chạy thực tế. Chưa có kết quả CI của workflow mới.
- Chưa xác nhận các luồng ghi trên cloud, đăng nhập/UI hoặc staging. Workflow chỉ đọc không thay thế nghiệm thu nghiệp vụ.

Tệp chạy và báo cáo cục bộ nằm ngoài Git trong `outputs/supabase-smoke-20261124/`. Không thêm cấu hình cá nhân vào PR.
