# SRS v1.2: danh sách thay đổi so với v1.0.3 (T7-01)

| | |
| --- | --- |
| Issue | #170 – T7-01 |
| Người soạn | #1 Phạm Gia Thăng · bản nháp 02/11/2026 |
| Bản gốc | SRS v1.0.3 (18/10/2026, tệp Word trong thư mục tài liệu chung của nhóm) |
| Cách dùng | Mỗi dòng là một chỗ phải sửa trong tệp Word SRS. Khi sửa xong, ghi "Đã sửa" ở cột cuối. Hạn chốt v1.2: 13/11/2026 |

Nguồn thay đổi: lệch đặc tả ghi trong PR, rà soát T6-02, quyết định của #1 khi duyệt PR, kết quả chạy thử trên MySQL 9.7.2 (02/11), và bản đánh giá repo ngày 01/11.

## 1. Thay đổi bắt buộc (SRS đang ghi khác với sản phẩm thật)

| # | Mục SRS | v1.0.3 ghi | v1.2 sửa thành | Căn cứ | Trạng thái |
| --- | --- | --- | --- | --- | --- |
| C1 | 2.4 (Giao diện), 2.5, 7.2 vấn đề 3 | React + Vite + Ant Design, Recharts; bản build phục vụ cùng domain | ASP.NET Core MVC (Razor view) + Bootstrap, jQuery; một số trang dùng HTML tĩnh gọi REST `/api/v1` (đăng nhập). Ghi lý do đổi và tham chiếu ADR-002 | Code `develop`; cần viết ADR-002 | Chờ ADR-002 |
| C2 | 2.4 (Triển khai) | Bản staging từ tuần 6 trên Docker | Ghi đúng hiện trạng: chưa có Docker image và staging trực tuyến; demo chạy cục bộ trên MySQL 9.7.2. Kế hoạch triển khai Render + Aiven chuyển sang tuần 8 | Bản đánh giá repo; T6-03 | |
| C3 | 3.4 AUTH-07 | Admin sửa họ tên, số điện thoại, email và vai trò | AUTH-07 chỉ sửa họ tên, số điện thoại, email; đổi vai trò qua AUTH-10 (BR-AUTH-03). E1 về Admin cuối cùng chuyển sang AUTH-10 | Bản sửa 20/10; code AUTH-07, AUTH-10 | |
| C4 | 3.x KH-01 | Tìm theo "số điện thoại, tên hoặc mã thành viên" | Tìm theo số điện thoại hoặc tên; mã khách là id nội bộ hiển thị `KH000012` | T6-02 mục KH1 | Chờ #5 xác nhận |
| C5 | 6.x (ERD), POS-09 | (không nêu) | Đơn hủy lưu trong `gio_hang` (`DA_HUY`, lý do, giá trị, thời điểm); không có bảng riêng | Nhật ký duyệt CSDL, PR #129 | |
| C6 | 4.x giao tiếp, 7.1 | Danh sách endpoint theo OpenAPI tuần 3 | Thêm endpoint tra cứu hóa đơn cho DT-01 cần quyền `DOI_TRA` (không dùng lại `/invoices`) | T6-02 mục DT4 | Chờ #3 xác nhận |

## 2. Bổ sung quy tắc (SRS chưa nói, đã chốt khi code)

| # | Mục SRS | Nội dung bổ sung | Căn cứ |
| --- | --- | --- | --- |
| B1 | KH-01 | Tổng chi tiêu = tổng tiền hóa đơn đã thanh toán − tổng tiền hoàn của phiếu trả hoàn tất; lần mua gần nhất tính từ hóa đơn đã thanh toán | T6-02 KH2 |
| B2 | BR-KH-01 | Thu ngân thấy số điện thoại đã che ở danh sách, chi tiết và kết quả tra cứu; Admin thấy đủ | T6-02 KH3 |
| B3 | KM-02 | Giá trị % là số nguyên 1–100 | T6-02 KM2 |
| B4 | POS-04, BR-POS-05 | Mức giảm tối đa của loại % tính trên mỗi đơn vị, áp sau khi làm tròn xuống bội số 100 đ | T6-02 KM3 |
| B5 | POS-04 bước 2 | Hai chương trình giảm bằng nhau: chọn chương trình kết thúc sớm hơn, rồi mã nhỏ hơn | T6-02 KM4 |
| B6 | POS-07 | Tính lại khuyến mãi trong giao tác hoàn tất; khác số đang hiển thị thì quay lại giỏ và báo thu ngân | T6-02 KM5 |
| B7 | DT-01, BR-DT-02 | Hạn đổi trả lấy từ chính sách đang hiệu lực, tính đến hết ngày thứ N theo giờ Việt Nam; số lượng đã trả tính cả phiếu chờ duyệt | T6-02 DT1, DT3 |
| B8 | DT-02, PA-DT-02.C | Ngưỡng 500.000 đ so với tổng tiền hoàn của phiếu | T6-02 DT2 |
| B9 | Dịch vụ kho (6.x, tài liệu dịch vụ kho 1.2) | `TRA_HANG`, `HUY_HOA_DON` đã cài đặt; giá vốn bình quân theo giá vốn dòng hóa đơn, làm tròn nửa lên 2 chữ số | T-KHO-SV2, PR đóng #140 |
| B10 | 2.4 (CSDL) | Đã chạy schema, migration và 164 test trên MySQL 9.7.2; tài khoản kết nối dùng `caching_sha2_password` | Chạy thử 02/11 |

## 3. Mục chương 7.2 (vấn đề mở) cần cập nhật

| Vấn đề | Trạng thái v1.0.3 | v1.2 |
| --- | --- | --- |
| 3 (giao diện) | Đóng: React | Mở lại rồi đóng theo C1 (Razor MVC), kèm ADR-002 |
| 10 (khảo sát hiện trạng) | Chưa ai viết | Ghi người viết và ngày xong; nếu chưa xong thì giữ "Mở" |
| Mới: CI | (không có) | Mở: chưa có GitHub Actions; #4 đưa `ci.yml` vào repo (bản đánh giá, ưu tiên 2) |
| Mới: nhánh `main` và `develop` lệch nhau | (không có) | Mở: gộp `main` vào `develop` rồi ngược lại trước BTQT #2 |

## 4. Các đề xuất trong "Danh sách nội dung đề xuất SRS v1.0.3"

Tài liệu đó liệt kê 42 mục do #1 điền thay và chờ người phụ trách duyệt. Khi ra v1.2, mỗi mục chỉ được ghi "Đã chốt" nếu người phụ trách đã xác nhận (bình luận trên issue hoặc PR). Mục nào chưa ai xác nhận thì ghi "Đề xuất" như cũ, không tự đổi thành "Đã chốt".

## 5. Việc còn lại để ra SRS v1.2

1. #1 sửa tệp Word theo mục 1–3, đổi số phiên bản thành 1.2 và thêm dòng lịch sử phiên bản.
2. Viết ADR-002 (C1) và đưa vào `docs/adr/`.
3. #3, #5 xác nhận C4, C6 và mục 2 trên PR này.
4. Xuất SRS v1.2 sang PDF, đưa vào `docs/srs/` qua PR.
