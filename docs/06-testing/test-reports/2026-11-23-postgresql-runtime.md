# Chuyển SQL nghiệp vụ sang PostgreSQL — 23/11/2026

Trạng thái: đã hiện thực và kiểm thử cục bộ; chưa commit/push phần runtime, chưa xác nhận GitHub CI của revision này và chưa nghiệm thu staging. Nền nhánh là commit `498daf5` (ánh xạ entity đã push); thay đổi runtime nằm trên cùng nhánh `feature/supabase-entities-20261123`.

## Kết quả thực chạy

| Kiểm tra | Kết quả | Phạm vi |
|---|---|---|
| Build Release (.NET 10) | 0 lỗi, 0 cảnh báo | Ứng dụng và project test |
| Toàn bộ test Release | **759/759 đạt**, 0 lỗi, 0 bỏ qua; 3 phút 45 giây | MySQL 8.4.13 và PostgreSQL 17.12 riêng tại localhost |
| Test PostgreSQL mới | **17/17 đạt**, nằm trong cùng run 759 | Không cộng số filtered run để tăng tổng |
| Publish | Thành công; 293 tệp; không chứa .env* hoặc appsettings.Development.json | Không đóng gói mật khẩu cục bộ |
| Chạy gói publish thật | login.html 200; tạo Admin đầu tiên/đăng nhập 200; đổi mật khẩu tạm 204; báo cáo 200 | Database PostgreSQL localhost mới, xóa sau test |
| Dữ liệu nền | Seed chạy lặp giữ nguyên quỹ đã chốt; 3 vai trò, số dư thử 123400 vẫn giữ nguyên | Database localhost tạm; không seed Supabase |
| Kết nối Supabase từ .env | Schema smartmart, transaction_read_only=on; 3 vai trò, 0 Admin, 0 sản phẩm | SELECT tổng hợp, không ghi dữ liệu nhóm |
| Staging | GitHub API: 0 Environment, 0 Deployment; repo không ghi URL staging | Không xác nhận các triển khai thủ công ngoài GitHub |

## Những luồng PostgreSQL đã kiểm tra

- AUTH: tạo tài khoản, đăng nhập, đổi mật khẩu tạm, đăng xuất, khóa sau 5 lần sai, đặt lại mật khẩu gỡ khóa; phiên ghi/gia hạn `het_han_luc`, hết hạn ngay cả khi lần hoạt động cuối còn mới.
- Khách hàng: thêm/sửa/tìm kiếm, che số điện thoại Thu ngân, chống trùng, điều chỉnh điểm không âm, ẩn khách và giữ lịch sử.
- POS: tạo giỏ cạnh tranh, gắn khách/áp khuyến mãi, giữ/gọi lại/hủy đơn; hoàn tất hóa đơn cùng tồn kho, điểm và quỹ. Hai thu ngân bán món cuối: đúng một hóa đơn thành công.
- Nhập/kho: xác nhận phiếu nhập không lặp, bình quân giá vốn; kiểm kê/xuất hủy chỉ đổi tồn sau duyệt.
- Đổi trả/hủy hóa đơn: phục hồi kho, thu hồi điểm, chi quỹ và chống hoàn tiền lặp; giữ snapshot hóa đơn bất biến.
- KM/BC: khuyến mãi thành viên và bản sao giá trên hóa đơn; truy vấn BC-02..11; biên nửa đêm UTC+7; tìm kiếm tiếng Việt không phân biệt dấu/hoa thường qua EF và SQL POS/KM.
- HTTP chạy từ Program.cs thật: không đăng nhập/AD/TN; CSRF; trang quản lý, POS đúng vai trò, 10 API báo cáo; tạo sản phẩm qua API.

## Thay đổi kỹ thuật

`SqlDialect` chọn cú pháp theo provider, tham số hóa dữ liệu và chuẩn hóa timestamp. PostgreSQL dùng `INSERT ... RETURNING id`, `ON CONFLICT`, `IS NOT DISTINCT FROM`, khoảng thời gian và phạm vi khóa hợp lệ cho PostgreSQL. Không dùng `lastval()` có thể bị trigger làm lệch sequence. POS sử dụng DbConnection/DbTransaction; xử lý lỗi unique/FK theo SQLSTATE.

Giữ một CuaHangDbContext và nhánh MySQL tương thích. Bổ sung default 0/false mà scaffold bỏ qua; test đối chiếu default với catalog. Timestamp PostgreSQL dùng UTC ở lưu trữ và UTC+7 ở biên nghiệp vụ/giao diện. Tìm kiếm dùng lower/translate, không cần thêm extension/function vào Supabase.

Tên các adapter `MySql*` được giữ để tương thích mã gọi/test cũ; không có nghĩa chúng ép dùng MySQL driver khi provider là PostgreSql.

## CI, schema và giới hạn

Workflow `.github/workflows/ci.yml` đã thêm service `postgres:17-alpine` bên cạnh MySQL, truyền biến test riêng và giữ gate TRX không bỏ qua test. Workflow mới chưa được push/chạy trên GitHub; kết quả 759/759 ở trên là chạy Release cục bộ.

DDL `docs/db/schema_postgresql.sql` dựng CSDL rỗng/test từ model cùng 3 hàm/17 trigger/3 view đọc từ Supabase. Không chạy lại lên schema đã có. Script không thay thế quản lý RLS/grant/baseline của nhóm CSDL. Các migration EF lịch sử vẫn dành cho MySQL; guard tiếp tục chặn áp chúng lên Npgsql. Không thêm bảng/cột mới trên Supabase trong đợt này.

Test PostgreSQL chỉ chấp nhận loopback, tự tạo/xóa smartmart_pgtest_GUID; gói publish cũng được thử trên database localhost riêng. Không chạy INSERT/UPDATE/DELETE, seed, migration hoặc tắt RLS trên Supabase/Aiven của nhóm. Chưa nghiệm thu quyền DB/RLS của role backend ít quyền hoặc staging cloud.

## Bàn giao

- [Cách cấu hình/chạy PostgreSQL](../../05-implementation/setup/postgresql-runtime-setup.md).
- [Publish và staging](../../07-deployment/postgresql-publish.md).
- [Bổ sung kỹ thuật SRS](../../03-requirements/postgresql-runtime-change-20261123.md), chờ nhóm review.
- Bằng chứng cục bộ: outputs/postgres-runtime-20261123/full-regression/test-results/tests.trx, build-release.log, publish-smoke.json, seedcheck.log, supabase-readiness.json.
