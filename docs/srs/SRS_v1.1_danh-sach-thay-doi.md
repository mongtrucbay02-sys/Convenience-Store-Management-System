# SRS v1.1: danh sách thay đổi phát sinh trong sprint 1–2 (T5-02)

| | |
| --- | --- |
| Issue | #87 – T5-02 |
| Người soạn | #1 Phạm Gia Thăng · ghi bổ sung ngày 22/11/2026 (hạn gốc 30/10/2026) |
| Bản gốc | SRS v1.0.3 (18/10/2026, tệp Word đính kèm issue #7, #8, #118) |
| Phạm vi | Thay đổi có ngày trong sprint 1–2 (19/10 → 01/11/2026), có nguồn kiểm chứng được trên GitHub hoặc trong tài liệu đính kèm issue |
| Quan hệ với v1.2 | `SRS_v1.2_danh-sach-thay-doi.md` (02/11/2026) so với v1.0.3 và đã gồm mục C3 dưới đây. Khi sửa tệp Word, áp dụng tài liệu này trước rồi đến danh sách v1.2 |

## Lịch sử phiên bản SRS

| Phiên bản | Ngày | Người sửa | Nội dung |
| --- | --- | --- | --- |
| v1.0.1 | 13/10/2026 | #1 | Bản đầy đủ đầu tiên theo nhóm 6 người (đính kèm issue #2, #7) |
| v1.0.3 | 18/10/2026 | #1 | Bản nộp bài tập quá trình #1 (issue #7, #8). 42 mục do #1 điền thay, ghi trong "Danh sách nội dung đề xuất SRS v1.0.3" (issue #82) |
| v1.1 | 22/11/2026 (danh sách thay đổi) | #1 | Tài liệu này. **Tệp Word SRS chưa được sửa theo danh sách** |
| v1.2 | 02/11/2026 (danh sách thay đổi) | #1 | `SRS_v1.2_danh-sach-thay-doi.md`. Tệp Word chưa được sửa theo danh sách |

## 1. Thay đổi từ các tài liệu sửa ngày 20/10/2026 (issue #118)

| # | Mục SRS | v1.0.3 ghi | v1.1 sửa thành | Căn cứ | Trạng thái |
| --- | --- | --- | --- | --- | --- |
| S1 | 1.1 (phân công), bảng module | KHO-01, 06, 07 đề xuất chuyển cho #1 | KHO-01, 06, 07 do #2 lập trình theo issue đã gán (#89, #90, #91). Dự phòng giữa #1 và #2 khi một người không làm tiếp được | ERD Kho v1 (sửa 20/10) mục 8 điểm 10; ADR-001 dòng "Cập nhật" 20/10 | Chờ nhóm xác nhận (ghi trong cả hai nguồn) |
| S2 | 2.4 (CSDL), 6 | `schema_v1.sql` đã chạy trên MySQL 8.0.46, cần chạy lại trên 9.7.2 | `schema_v1.sql` chạy thành công trên MySQL 9.7.2 (#1 chạy, 20/10/2026) | ERD Kho v1, lịch sử phiên bản 20/10 | Đã kiểm chứng |
| S3 | 3.x AUTH, 6 (dữ liệu khởi tạo) | Không nêu cách có tài khoản Admin đầu tiên | Khi khởi động mà CSDL chưa có Admin, chương trình khởi tạo tạo Admin đầu tiên từ biến môi trường (mật khẩu băm, không ghi vào repo), đặt `bat_buoc_doi_mk = TRUE` | ADR-001 mục 3.8 (20/10); T4-16; PR #132 | Đã cài đặt trên `develop` |
| S4 | 2.4 (môi trường) | Một CSDL dùng chung cho môi trường phát triển | Mỗi thành viên chạy `mysql:9.7.2` bằng Docker Compose (`--default-time-zone=+07:00 --disable-log-bin`), có `.env.example` và migration tự động; CI dùng MySQL 9.7.2 dạng service container | Bảng Task v3 sửa 20/10 (T3-25, T4-02) | Chưa thực hiện: repo có `.env.example`, chưa có tệp Docker Compose và workflow CI |
| S5 | 3.4 AUTH-07 | Admin sửa họ tên, số điện thoại, email và vai trò | AUTH-07 chỉ sửa họ tên, số điện thoại, email; đổi vai trò qua AUTH-10 | Đặc tả use case sửa 20/10; trùng mục C3 của danh sách v1.2 | Đã cài đặt (PR #135, #137) |

## 2. Quy tắc chốt khi lập trình trong sprint 1–2 (SRS chưa nêu)

| # | Mục SRS | Nội dung bổ sung | Căn cứ |
| --- | --- | --- | --- |
| S6 | AUTH-01, AUTH-02, NFR-BM | Phiên lưu ở bảng `phien_dang_nhap`; cookie `cs_session` (HttpOnly, SameSite=Strict), hết hạn sau 30 phút không thao tác; vai trò đọc lại từ CSDL ở mỗi yêu cầu | PR #135 (gộp 27/10) |
| S7 | NFR-BM, 4.x giao tiếp | Chống CSRF: cookie `XSRF-TOKEN` và header `X-XSRF-TOKEN` bắt buộc cho POST/PUT/DELETE | PR #135 |
| S8 | AUTH-10, ma trận phân quyền | 27 mã quyền, mỗi mã là một policy (`MaQuyen`), module khác kiểm quyền theo mã | PR #135 |
| S9 | AUTH-03, AUTH-06 | Khi tài khoản còn cờ bắt buộc đổi mật khẩu, mọi API trừ đổi mật khẩu và đăng xuất trả `403 PHAI_DOI_MAT_KHAU` | PR #135 |
| S10 | AUTH-08 | Khóa tài khoản hủy mọi phiên đang mở; không cho Admin tự khóa mình và không khóa Admin đang hoạt động cuối cùng. Mở khóa gỡ cả khóa do Admin và khóa tạm, đưa bộ đếm sai mật khẩu về 0 | PR #137 (gộp 27/10) |
| S11 | AUTH-09 | Đặt lại mật khẩu: hệ thống tự sinh hoặc Admin nhập; mật khẩu chỉ trả về một lần (`Cache-Control: no-store`); bật cờ bắt buộc đổi; hủy các phiên cũ | PR #137 |

## 3. Vấn đề mở còn lại từ sprint 1–2

Ba điểm nêu trong issue #5 (16/10/2026) chưa có quyết định ghi nhận:

| Vấn đề | Nội dung | Trạng thái |
| --- | --- | --- |
| AUTH-06 | Số điện thoại có bắt buộc khi tạo tài khoản không (đặc tả ngầm hiểu bắt buộc, ERD và OpenAPI cho phép null) | Mở |
| AUTH-09 | Đặt lại mật khẩu khi tài khoản đang bị khóa tạm có gỡ khóa tạm không (PR #137 ghi là "điểm chờ nhóm chốt") | Mở |
| AUTH-01 | Thứ tự kiểm tra khóa tạm và mật khẩu khi đăng nhập (OpenAPI kiểm khóa tạm trước, đặc tả bước 4–5 ngược lại; vấn đề mở số 8 trong SRS) | Mở |

## 4. Việc còn lại để ra SRS v1.1

1. Sửa tệp Word SRS theo mục 1–2, ghi số phiên bản 1.1 và thêm dòng lịch sử phiên bản.
2. Các thành viên liên quan xác nhận S1 (#2) và mục 3 (người phụ trách AUTH và trưởng nhóm) bằng comment trên PR hoặc issue #87.
3. Mục nào chưa được xác nhận vẫn ghi "Đề xuất", không ghi "Đã chốt".
