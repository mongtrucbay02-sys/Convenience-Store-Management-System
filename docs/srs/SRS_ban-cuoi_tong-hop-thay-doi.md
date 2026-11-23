# SRS bản cuối – tổng hợp thay đổi so với SRS v1.0.3 (T9-02, issue #257)

Soạn 23/11/2026 · #1 Phạm Gia Thăng. Gộp mọi thay đổi đã ghi trong repo để sửa **một lần** tệp Word SRS v1.0.3 (thư mục tài liệu chung của nhóm, tuần 3) thành SRS bản cuối.

Nguồn: `docs/srs/SRS_v1.1_danh-sach-thay-doi.md` (PR #231), `docs/srs/SRS_v1.2_danh-sach-thay-doi.md`, `docs/03-requirements/cash-promotion-change-proposal-20261123.md` (PR #232), ADR-002 (PR #231).

Cột "Trạng thái": **Đã cài đặt** = có code trên `develop`/PR #232; **Xn** = đề xuất chốt mã Xn đăng ở issue #257 ngày 23/11, **hạn phản hồi 20:00 ngày 25/11/2026** – quá hạn không ai phản đối thì ghi "Đã chốt" trong SRS; **Đã chốt** = đã có quyết định.

## 1. Thay đổi bắt buộc (SRS v1.0.3 đang ghi khác sản phẩm)

| Mã | Mục SRS | Sửa thành | Nguồn | Trạng thái |
| --- | --- | --- | --- | --- |
| C1 | 2.4, 2.5, 7.2 vấn đề 3 | Giao diện ASP.NET Core MVC (Razor) + Bootstrap, jQuery; trang đăng nhập HTML gọi REST. Tham chiếu ADR-002 | v1.2 | Đã cài đặt; X1 |
| C2 | 2.4 (triển khai) | Ghi đúng hiện trạng triển khai tại thời điểm nộp (cục bộ hay trực tuyến) | v1.2 | Chờ #4 (#242) |
| C3 / S5 | 3.4 AUTH-07 | AUTH-07 chỉ sửa họ tên, SĐT, email; đổi vai trò qua AUTH-10 | v1.1, v1.2 | Đã cài đặt |
| C4 | KH-01 | Tìm theo SĐT hoặc tên; mã khách là id nội bộ dạng `KH000012` | v1.2 | X2 |
| C5 | 6.x, POS-09 | Đơn hủy lưu trong `gio_hang` (`DA_HUY`), không bảng riêng | v1.2 | Đã cài đặt |
| C6 | 4.x, 7.1 | Endpoint tra cứu hóa đơn cho DT-01 dùng quyền `DOI_TRA` | v1.2 | X2 |
| S1 | 1.1, bảng module | KHO-01, 06, 07 thuộc #2; dự phòng #1 ↔ #2 (thực tế phần NH/KHO trên `develop` do #5 làm, PR #222) | v1.1 | Cập nhật theo thực tế |
| S4 | 2.4 (môi trường) | Môi trường CSDL và CI: ghi đúng phiên bản MySQL nhóm chốt (ADR 9.7.2, CI 8.4) | v1.1 | X5: chốt MySQL 8.4 |

## 2. Quy tắc bổ sung đã chốt khi lập trình

| Mã | Mục SRS | Nội dung | Trạng thái |
| --- | --- | --- | --- |
| S2 | 2.4, 6 | `schema_v1.sql` chạy trên MySQL 9.7.2 (20/10) | Đã kiểm chứng |
| S3 | AUTH, 6 | Admin đầu tiên tạo khi khởi động từ biến môi trường, bắt buộc đổi mật khẩu | Đã cài đặt |
| S6–S9 | AUTH-01 → 03, 06, 10, NFR-BM | Phiên `phien_dang_nhap`, cookie `cs_session` 30 phút; CSRF `X-XSRF-TOKEN`; 27 mã quyền; `403 PHAI_DOI_MAT_KHAU` | Đã cài đặt |
| S10–S11 | AUTH-08, 09 | Khóa hủy mọi phiên, không khóa Admin cuối; mở khóa gỡ cả khóa tạm; đặt lại mật khẩu trả một lần, bắt buộc đổi | Đã cài đặt |
| B1 | KH-01 | Tổng chi tiêu = hóa đơn đã thanh toán − tiền hoàn phiếu trả hoàn tất | X2 |
| B2 | BR-KH-01 | Thu ngân thấy SĐT đã che; Admin thấy đủ | X2 |
| B3 → B6 | KM-02, POS-04, POS-07 | Giá trị % nguyên 1–100; giảm tối đa theo đơn vị; hòa thì chọn chương trình kết thúc sớm hơn; tính lại khuyến mãi khi hoàn tất | X2 |
| B7, B8 | DT-01, DT-02 | Hạn đổi trả theo chính sách hiệu lực, giờ Việt Nam; ngưỡng 500.000 đ trên tổng tiền hoàn | X2 |
| B9 | Dịch vụ kho | `TRA_HANG`, `HUY_HOA_DON`; giá vốn bình quân làm tròn 2 chữ số | Đã cài đặt |
| CP-01 | POS-12 | Hủy hóa đơn tiền mặt ghi chi quỹ = tiền khách đưa − tiền thừa, cùng giao tác với hoàn kho, thu hồi điểm, lịch sử hoàn | Đã cài đặt (PR #232); X3 |
| CP-02 | POS-12 | Mỗi hóa đơn chỉ hủy/hoàn một lần; lịch sử chi không sửa/xóa | Đã cài đặt; X3 |
| CP-03 | (mới) Quỹ tiền mặt | Admin chốt số dư mở quỹ một lần bằng số thực đếm; hệ thống lưu trước/sau/chênh lệch | Đã cài đặt; X3 |
| CP-04 | KM-02, KM-03 | Ảnh khuyến mãi: URL HTTP/HTTPS ≤ 2048 ký tự, lưu xuống CSDL | Đã cài đặt; X3 |
| R3 | POS-12 | Hủy hóa đơn bị từ chối (`QUY_KHONG_DU`) khi quỹ không đủ, kể cả khi chưa chốt số dư mở | X4: giữ quy tắc; Admin chốt số dư mở trước demo; ghi SRS, HDSD |

## 3. Use case bị cắt hoặc chưa làm

Đề xuất chốt X7, X8, X9 ở issue #257 (hạn 25/11). Use case cắt được SRS ghi "Không thuộc bản nộp" kèm lý do thiếu thời gian.

| Use case | Issue | MoSCoW | Quyết định |
| --- | --- | --- | --- |
| DT-03 Đổi hàng | #238 | Could | Cắt (X7) |
| DT-06 Duyệt đổi trả ngoại lệ | #239 | Could | Cắt (X7) |
| DT-07 Danh sách phiếu đổi trả | #237 | Should | Làm nếu xong trước 27/11, không thì cắt (X8) |
| DT-08 Cấu hình chính sách đổi trả | #253 | Could | Cắt (X7) |
| BC-09, BC-10, BC-11 | #248, #249, #250 | Could | Cắt (X7) |
| KM-05, KM-06 | #247, #246 | Could | Cắt (X7) |
| KH-07 Cấu hình quy tắc tích điểm | #189 | Could | Cắt (X7) |
| NH-09 Lịch sử nhập theo NCC | #146 | Could | Cắt (X7) |
| AI-01 Gợi ý số lượng nhập | (chưa có issue) | – | Cắt, giữ thiết kế ADR-001 mục 3.11 (X9) |

## 4. Yêu cầu phi chức năng cần ghi đúng hiện trạng

| Mã | Nội dung | Việc cần làm |
| --- | --- | --- |
| NFR-BM-09 | Bản triển khai trực tuyến dùng HTTPS | Nếu không triển khai trực tuyến, đổi thành "chạy cục bộ" và ghi lý do |
| NFR-HN-01 → 05 | Thời gian phản hồi, 5 người dùng đồng thời | Ghi kết quả đo của #4 hoặc "chưa kiểm chứng" |

## 5. Ba điểm AUTH từ issue #5 (đã chốt 23/11, `docs/quan-ly/quyet-dinh-AUTH-01-06-09.md`, PR #271)

| Vấn đề | Trạng thái |
| --- | --- |
| AUTH-06: SĐT tài khoản bắt buộc hay không | **Đã chốt 23/11:** không bắt buộc; sửa đặc tả AUTH-06 bước 2 |
| AUTH-09: đặt lại mật khẩu khi đang khóa tạm có gỡ khóa tạm không | **Đã chốt 23/11:** gỡ khóa tạm, bộ đếm về 0; khóa do Admin vẫn phải mở trước (code ở PR #271); sửa E1 đặc tả AUTH-09 |
| AUTH-01: thứ tự kiểm tra khóa tạm và mật khẩu | **Đã chốt 23/11:** khóa tạm kiểm trước mật khẩu; sửa bước 4–5 đặc tả AUTH-01, đóng vấn đề mở số 8 |

## 6. Việc còn lại để ra SRS bản cuối

1. Sau 20:00 ngày 25/11, đọc phản hồi ở issue #257: mục nào không bị phản đối thì ghi "Đã chốt"; mục bị phản đối thì sửa theo ý kiến hoặc trưởng nhóm quyết định.
2. Ghi danh sách mục đã chốt vào issue #257.
3. #1 sửa tệp Word theo mục 1–5, ghi phiên bản "bản cuối", thêm dòng lịch sử phiên bản; xuất PDF, đưa vào `docs/` qua PR.
