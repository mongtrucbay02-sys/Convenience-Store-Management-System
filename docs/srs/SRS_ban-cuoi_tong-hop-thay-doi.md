# SRS bản cuối – tổng hợp thay đổi so với SRS v1.0.3 (T9-02, issue #257)

Soạn 23/11/2026, cập nhật 27/11/2026 · #1 Phạm Gia Thăng. Gộp mọi thay đổi đã ghi trong repo để sửa **một lần** tệp Word SRS v1.0.3 (thư mục tài liệu chung của nhóm, tuần 3) thành SRS bản cuối.

Nguồn:
- `docs/srs/SRS_v1.1_danh-sach-thay-doi.md` (PR #231) và `docs/srs/SRS_v1.2_danh-sach-thay-doi.md`.
- `docs/03-requirements/cash-promotion-change-proposal-20261123.md` (PR #232).
- ADR-002 (PR #231) và ADR-003 (PR #281).
- Kết quả X1 → X10 đăng ở issue #257 ngày 27/11.

Đối chiếu code tại `develop` `cc20336`; CI 877/877.

Cột "Trạng thái":
- **Đã chốt**: có quyết định. Gồm các mục X1 → X10 không bị phản đối trước 20:00 ngày 25/11.
- **Đã cài đặt**: có code trên `develop`.
- **Chờ quyết định**: đã hỏi nhóm ở #257, chưa có trả lời. SRS chưa sửa mục này.

## 1. Thay đổi bắt buộc (SRS v1.0.3 đang ghi khác sản phẩm)

| Mã | Mục SRS | Sửa thành | Nguồn | Trạng thái |
| --- | --- | --- | --- | --- |
| C1 | 2.4, 2.5, 7.2 vấn đề 3 | Giao diện ASP.NET Core MVC (Razor) + Bootstrap, jQuery; trang đăng nhập HTML gọi REST. Tham chiếu ADR-002 | v1.2 | Đã cài đặt; **Đã chốt** (X1) |
| C2 | 2.4 (triển khai) | Không có bản triển khai web trực tuyến: #242 (deploy HTTPS) đóng "not planned" ngày 24/11. Ứng dụng chạy bằng `dotnet run`, kết nối CSDL qua biến môi trường | v1.2; #242 | Ghi theo hiện trạng |
| C3 / S5 | 3.4 AUTH-07 | AUTH-07 chỉ sửa họ tên, SĐT, email; đổi vai trò qua AUTH-10 | v1.1, v1.2 | Đã cài đặt |
| C4 | KH-01 | Tìm theo SĐT hoặc tên; mã khách là id nội bộ dạng `KH000012` | v1.2 | **Đã chốt** (X2) |
| C5 | 6.x, POS-09 | Đơn hủy lưu trong `gio_hang` (`DA_HUY`), không bảng riêng | v1.2 | Đã cài đặt |
| C6 | 4.x, 7.1 | Endpoint tra cứu hóa đơn cho DT-01 dùng quyền `DOI_TRA` | v1.2 | **Đã chốt** (X2) |
| S1 | 1.1, bảng module | KHO-01, 06, 07 thuộc #2; dự phòng #1 ↔ #2 (thực tế phần NH/KHO trên `develop` do #5 làm, PR #222) | v1.1 | Cập nhật theo thực tế |
| S4 | 2.4, 2.5, 6 (môi trường, ràng buộc, CSDL) | **PostgreSQL 17**, schema `smartmart`, truy cập bằng EF Core 10 + Npgsql; không còn MySQL. CI dựng PostgreSQL 17 tạm cho mỗi PR. CSDL dùng chung là Supabase. Backend chạy bằng vai trò `smartmart_backend` (RLS, không BYPASSRLS), không dùng Supabase Auth | ADR-003; #286 | **Đã chốt** (ADR-003, thay X5); đã chuyển xong 24/11 (#286) |

## 2. Quy tắc bổ sung đã chốt khi lập trình

| Mã | Mục SRS | Nội dung | Trạng thái |
| --- | --- | --- | --- |
| S2 | 2.4, 6 | Lược đồ: migration `PostgreSqlBaseline` (#286) trên PostgreSQL 17: 31 bảng, 3 view. `schema_v1.sql` (MySQL, 18/10) giữ làm lịch sử | Đã cài đặt. Smoke đọc trên Supabase đạt; **ghi trên Supabase chưa kiểm chứng (#293)** |
| S3 | AUTH, 6 | Admin đầu tiên tạo khi khởi động từ biến môi trường, bắt buộc đổi mật khẩu | Đã cài đặt |
| S6–S9 | AUTH-01 → 03, 06, 10, NFR-BM | Phiên `phien_dang_nhap`, cookie `cs_session` 30 phút; CSRF `X-XSRF-TOKEN`; 27 mã quyền; `403 PHAI_DOI_MAT_KHAU` | Đã cài đặt |
| S10–S11 | AUTH-08, 09 | Khóa hủy mọi phiên, không khóa Admin cuối; mở khóa gỡ cả khóa tạm; đặt lại mật khẩu trả một lần, bắt buộc đổi | Đã cài đặt |
| B1 | KH-01 | Tổng chi tiêu = hóa đơn đã thanh toán − tiền hoàn phiếu trả hoàn tất | **Đã chốt** (X2) |
| B2 | BR-KH-01 | Thu ngân thấy SĐT đã che; Admin thấy đủ | **Đã chốt** (X2) |
| B3 → B6 | KM-02, POS-04, POS-07 | Giá trị % nguyên 1–100; giảm tối đa theo đơn vị; hòa thì chọn chương trình kết thúc sớm hơn; tính lại khuyến mãi khi hoàn tất | **Đã chốt** (X2) |
| B7, B8 | DT-01, DT-02 | Hạn đổi trả theo chính sách hiệu lực, giờ Việt Nam; ngưỡng 500.000 đ trên tổng tiền hoàn | **Đã chốt** (X2) |
| B9 | Dịch vụ kho | `TRA_HANG`, `HUY_HOA_DON`; giá vốn bình quân làm tròn 2 chữ số | Đã cài đặt |
| CP-01 | POS-12 | Hủy hóa đơn tiền mặt ghi chi quỹ = tiền khách đưa − tiền thừa, cùng giao tác với hoàn kho, thu hồi điểm, lịch sử hoàn | Đã cài đặt; **Đã chốt** (X3) |
| CP-02 | POS-12 | Mỗi hóa đơn chỉ hủy/hoàn một lần; lịch sử chi không sửa/xóa | Đã cài đặt; **Đã chốt** (X3) |
| CP-03 | (mới) Quỹ tiền mặt | Admin chốt số dư mở quỹ một lần bằng số thực đếm; hệ thống lưu trước/sau/chênh lệch | Đã cài đặt; **Đã chốt** (X3) |
| CP-04 | KM-02, KM-03 | Ảnh khuyến mãi: URL HTTP/HTTPS ≤ 2048 ký tự, lưu xuống CSDL | Đã cài đặt; **Đã chốt** (X3) |
| R3 | POS-12 | Hủy hóa đơn bị từ chối (`QUY_KHONG_DU`) khi quỹ không đủ, kể cả khi chưa chốt số dư mở. Admin chốt số dư mở trước demo | Đã cài đặt; **Đã chốt** (X4) |

## 3. Phạm vi use case

Use case cắt được SRS ghi "Không thuộc bản nộp" kèm lý do (thiếu thời gian, mức Could).

| Use case | Issue | MoSCoW | Code trên `develop` | Quyết định |
| --- | --- | --- | --- | --- |
| BC-09, BC-10, BC-11 | #248 → #250 | Could | Có (#275, #289) | **Giữ** (#5 rút khỏi danh sách cắt, 23/11) |
| KM-05, KM-06 | #247, #246 | Could | Có (#275, #289) | **Giữ** (#5, 23/11) |
| DT-07 Danh sách phiếu đổi trả | #237 | Should | Có (#287) | **Giữ** (X8) |
| NH-09 Lịch sử nhập theo NCC | #146 | Could | Có (#294, 26/11) | Đề xuất **giữ** (X7 ngày 23/11 ghi cắt, nhưng code đã gộp sau đó) – **chờ #5 xác nhận** |
| DT-03 Đổi hàng | #238 | Could | Chưa; PR #291 bị yêu cầu sửa | **Chờ quyết định** (#3, #5): giữ nếu #291 gộp kịp mốc nhóm chọn, không thì cắt |
| DT-06 Duyệt đổi trả ngoại lệ | #239 | Could | Chưa; PR #291 | **Chờ quyết định**, như DT-03 |
| DT-08 Cấu hình chính sách đổi trả | #253 | Could | Không (đổi trả vẫn dùng bảng `chinh_sach_doi_tra`, chỉ thiếu màn hình chỉnh) | **Cắt** (X7; #253 đóng "not planned" 26/11) |
| KH-07 Cấu hình quy tắc tích điểm | #189 | Could | Không | **Cắt** (X7) |
| AI-01 Gợi ý số lượng nhập | – | – | Không | **Cắt**, giữ thiết kế ở ADR-001 mục 3.11 (X9) |

Tham khảo: [báo cáo module tuần 8](../05-implementation/week8-bc-kh-km-report.md), [NH-09](../05-implementation/NH09.md), [ma trận nghiệm thu cuối](../06-testing/test-reports/final-acceptance-matrix.md).

## 4. Yêu cầu phi chức năng cần ghi đúng hiện trạng

| Mã | Nội dung | Ghi vào SRS cuối |
| --- | --- | --- |
| NFR-BM-09 | Bản triển khai trực tuyến dùng HTTPS | Không có bản triển khai trực tuyến (#242 "not planned"). Kết nối tới Supabase bắt buộc TLS `VerifyFull` (#286, #298) |
| NFR-HN-01 → 05 | Thời gian phản hồi, 5 người dùng đồng thời | **Chưa kiểm chứng** (chưa có kết quả đo trong repo; #176 tối ưu truy vấn còn mở) |
| NFR kiểm thử | Test tự động | 877 test trên PostgreSQL 17 cô lập, chạy trong CI cho mỗi PR (lượt `cc20336`: 877/877); chạy lại cục bộ ngày 27/11 cũng 877/877 |

## 5. Ba điểm AUTH từ issue #5 (đã chốt 23/11, `docs/quan-ly/quyet-dinh-AUTH-01-06-09.md`, PR #271)

| Vấn đề | Trạng thái |
| --- | --- |
| AUTH-06: SĐT tài khoản bắt buộc hay không | **Đã chốt 23/11:** không bắt buộc; sửa đặc tả AUTH-06 bước 2 |
| AUTH-09: đặt lại mật khẩu khi đang khóa tạm có gỡ khóa tạm không | **Đã chốt 23/11:** gỡ khóa tạm, bộ đếm về 0; khóa do Admin vẫn phải mở trước; sửa E1 đặc tả AUTH-09 |
| AUTH-01: thứ tự kiểm tra khóa tạm và mật khẩu | **Đã chốt 23/11:** khóa tạm kiểm trước mật khẩu; sửa bước 4–5 đặc tả AUTH-01, đóng vấn đề mở số 8 |

## 6. Việc còn lại để ra SRS bản cuối

1. ~~Đọc phản hồi X1 → X10 sau 20:00 25/11~~: không có phản đối; kết quả đã đăng ở #257 ngày 27/11.
2. Chờ #3, #5 trả lời về NH-09, DT-03, DT-06 (#257). Có câu trả lời thì sửa mục 3 và tệp Word.
3. #1 sửa tệp Word theo mục 1–5, ghi phiên bản "bản cuối", thêm dòng lịch sử phiên bản; xuất PDF, đưa vào `docs/srs/` qua PR.
