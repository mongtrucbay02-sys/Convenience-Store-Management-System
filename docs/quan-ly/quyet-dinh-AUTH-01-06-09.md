# Quyết định ba điểm mở của AUTH (issue #5)

| | |
| --- | --- |
| Ngày | 23/11/2026 |
| Người quyết định | #1 Phạm Gia Thăng (phụ trách AUTH) – giao Claude Code đề xuất, ghi ở nhật ký AI |
| Căn cứ | Comment issue #5 (16/10/2026); `docs/04-system-design/api/PHOI_HOP_AUTH_POS03_POS04.md` mục 4; ERD Tài khoản – Vai trò v1; `openapi-auth.yaml`; code `Modules/Auth` trên `develop` |

## AUTH-06 – Số điện thoại khi tạo tài khoản

**Quyết định:** số điện thoại **không bắt buộc**; nếu nhập thì phải đúng định dạng 10 chữ số bắt đầu bằng 0 và không trùng tài khoản khác.

**Lý do:** ERD cho phép `tai_khoan.so_dien_thoai` NULL, OpenAPI khai `nullable: true`, code (`QuyTacTaiKhoan.SoDienThoaiHopLe`) chấp nhận null. Số điện thoại tài khoản nhân viên không dùng trong nghiệp vụ nào bắt buộc (khác với số điện thoại khách hàng ở POS-03).

**Việc cần làm:** sửa đặc tả AUTH-06 bước 2 ghi rõ "Số điện thoại (tùy chọn)". Không đổi code.

## AUTH-01 – Thứ tự kiểm tra khi đăng nhập

**Quyết định:** kiểm tra **khóa tạm trước**, không kiểm mật khẩu khi đang khóa tạm; sau khi mật khẩu đúng mới kiểm khóa do Admin (`DA_KHOA`).

**Lý do:** khi đang khóa tạm mà vẫn kiểm mật khẩu thì người dò mật khẩu vẫn biết được mật khẩu đúng hay sai, làm mất tác dụng của khóa 15 phút sau 5 lần sai. Khóa do Admin chỉ báo cho người đã nhập đúng mật khẩu, nên người ngoài không biết tài khoản nào đang bị khóa. Code `DichVuXacThuc.DangNhapAsync` đang làm đúng thứ tự này.

**Việc cần làm:** sửa bước 4–5 đặc tả AUTH-01 theo thứ tự trên (đóng vấn đề mở số 8 của SRS). Không đổi code.

## AUTH-09 – Đặt lại mật khẩu khi tài khoản đang khóa tạm

**Quyết định:** Admin đặt lại mật khẩu thì **gỡ khóa tạm do sai mật khẩu và đưa bộ đếm sai về 0**. Tài khoản bị Admin khóa (`DA_KHOA`) vẫn phải mở khóa (AUTH-08) trước khi đặt lại.

**Lý do:** nhân viên quên mật khẩu thường đã nhập sai nhiều lần; đã có mật khẩu mới mà vẫn phải chờ 15 phút là vô lý. Đặt lại mật khẩu là thao tác của Admin đã xác thực, có ghi lịch sử và hủy mọi phiên cũ, nên gỡ khóa tạm không làm yếu bảo mật. Khóa do Admin là quyết định quản lý nên không tự gỡ.

**Việc cần làm:** code `DichVuTaiKhoan.DatLaiMatKhauAsync` cập nhật `khoa_tam_den = NULL, so_lan_sai = 0` (PR này), test `DatLai_DangKhoaTamDoSaiMatKhau_GoKhoaTam_BoDemVe0_DangNhapDuocNgay`; sửa ngoại lệ E1 của đặc tả AUTH-09.
