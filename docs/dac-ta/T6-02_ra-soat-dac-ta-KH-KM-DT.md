# Rà soát đặc tả KH, KM, DT trước khi code (T6-02)

| | |
| --- | --- |
| Issue | #139 – T6-02 |
| Người rà | #1 Phạm Gia Thăng |
| Ngày | 02/11/2026 |
| Phạm vi | KH-01, KH-02, KH-03 (cho #5); KM-01 → KM-04 và POS-04 (cho #3, #5); DT-01 (cho #3) |
| Đối chiếu với | Đặc tả Use Case 18/10/2026; `docs/db/schema_v1.sql` (ERD v1); `docs/api/openapi-report-customer-promotion.yaml`; `docs/api/returns.md`; code trên `develop` (2cc1cf6) và `main` (aed862d) |
| Trạng thái | Chờ #3, #5 xác nhận trên PR. Các mục ghi "Chốt" dưới đây là đề xuất của #1; nếu không ai phản đối trước 04/11 thì áp dụng và đưa vào SRS v1.2 (T7-01) |

Kết luận chung: đặc tả đủ để code, không phải đổi cấu trúc CSDL. Có 15 điểm cần làm rõ; phần lớn là chi tiết tính toán mà đặc tả bỏ ngỏ. Không có điểm nào chặn việc bắt đầu code.

## 1. Khách hàng (KH-01 → KH-03) – #5

| # | Chỗ cần làm rõ | Căn cứ | Chốt |
| --- | --- | --- | --- |
| KH1 | KH-01 ghi "tìm theo số điện thoại, tên hoặc **mã thành viên**", nhưng PA-KH-01.A đã chọn số điện thoại làm định danh, ERD không có cột mã thành viên | `khach_hang` không có cột `ma` | Tìm theo số điện thoại hoặc tên. "Mã khách" là `id`, hiển thị dạng `KH000012` để tra cứu, không in thẻ. Sửa mô tả KH-01 trong đặc tả |
| KH2 | KH-01 hiển thị "tổng chi tiêu" và "lần mua gần nhất" nhưng không có cột lưu | ERD v1 | Tính khi truy vấn: tổng `tong_thanh_toan` của hóa đơn `DA_THANH_TOAN` trừ tổng `tien_hoan` của phiếu trả `HOAN_TAT`; lần mua gần nhất là `MAX(thanh_toan_luc)` của hóa đơn `DA_THANH_TOAN`. Dùng chỉ mục `idx_hoa_don_khach` sẵn có |
| KH3 | BR-KH-01 che số điện thoại "ở danh sách"; chưa nói màn hình chi tiết và tra cứu ở POS-03 | OpenAPI có `phoneIsMasked` | Thu ngân thấy số đã che ở danh sách, chi tiết và kết quả tra cứu; Admin thấy đủ. Ở POS-03 thu ngân đã gõ đủ số nên không cần hiện lại số đầy đủ |
| KH4 | KH-02 kiểm tra "10 số, bắt đầu bằng 0" | CHECK `ck_kh_sdt` cùng quy tắc | Giữ nguyên; máy chủ kiểm tra trước để trả lỗi 422 theo trường thay vì để CSDL báo lỗi |
| KH5 | KH-03 chưa nói hai người cùng sửa một khách | Cột `phien_ban` | Gửi kèm `phienBan`; lệch thì trả 409 như quy ước API mục 6 |
| KH6 | Khách đã ẩn (KH-04) có còn chặn trùng số điện thoại không | KH-04 xóa số điện thoại khi ẩn | Không chặn, vì số đã bị xóa (NULL). Khách cũ quay lại thì tạo khách mới |

## 2. Khuyến mãi (KM-01 → KM-04, POS-04) – #5, #3

| # | Chỗ cần làm rõ | Căn cứ | Chốt |
| --- | --- | --- | --- |
| KM1 | Module KM trên `main` lưu dữ liệu trong bộ nhớ (`InMemoryPromotionDataSource`), không dùng bảng `chuong_trinh_khuyen_mai` | Code `main` | #5 chuyển nguồn dữ liệu sang MySQL (bảng `chuong_trinh_khuyen_mai`, `khuyen_mai_san_pham`, view `v_chuong_trinh_khuyen_mai`, `v_luot_dung_khuyen_mai`) trước khi #3 làm POS-04. Đây là việc ưu tiên 1 trong bản đánh giá repo |
| KM2 | KM-02 E1 nói giá trị % trong khoảng (0, 100] nhưng không nói có số lẻ hay không | `gia_tri INT`, CHECK 1–100 | Chỉ số nguyên 1–100 |
| KM3 | "Mức giảm tối đa" của loại % tính trên mỗi đơn vị hay cả dòng | `ct_hoa_don.giam_don_vi` lưu theo đơn vị | Trên **mỗi đơn vị**, áp sau khi làm tròn xuống bội số 100 đ (BR-POS-05). Ví dụ: giá 25.000 đ, giảm 15%, tối đa 3.000 đ → 3.750 làm tròn 3.700 → áp trần còn 3.000 đ/đơn vị |
| KM4 | POS-04 bước 2 chọn chương trình giảm cao nhất; chưa nói khi bằng nhau | PA-KM-03.A | Bằng nhau thì chọn chương trình kết thúc sớm hơn, rồi `id` nhỏ hơn, để kết quả luôn giống nhau giữa các lần tính |
| KM5 | Giảm giá tính lúc giỏ thay đổi (PA-KM-02.A); chương trình có thể hết hạn hoặc bị tạm dừng trước khi thanh toán | BR-KM-05 | POS-07 tính lại khuyến mãi trong giao tác hoàn tất. Nếu khác số đang hiển thị thì không lưu, quay lại giỏ và báo thu ngân (cách xử lý giống POS-07 E1) |
| KM6 | Trạng thái lưu và trạng thái hiển thị | Bảng có 4 trạng thái lưu; view suy ra `DA_LEN_LICH`, `DANG_CHAY` | Giữ như ERD: chỉ lưu `NHAP`, `DA_KICH_HOAT`, `TAM_DUNG`, `DA_KET_THUC`; trạng thái hiển thị lấy từ view. Enum trong code `main` đã khớp |
| KM7 | BR-KM-03 "sau khi có lượt dùng" | `v_luot_dung_khuyen_mai` không tính hóa đơn đã hủy | Lượt dùng lấy từ view; hóa đơn bị hủy (POS-12) không còn tính lượt, nên chương trình có thể sửa lại được nếu mọi hóa đơn dùng nó đã hủy |

## 3. Đổi trả (DT-01) – #3

Mục này trả lời 5 câu hỏi để ngỏ ở `docs/api/returns.md` mục 5.

| # | Chỗ cần làm rõ | Căn cứ | Chốt |
| --- | --- | --- | --- |
| DT1 | "Trong 7 ngày kể từ ngày mua" tính thế nào; 7 có cố định không | Bảng `chinh_sach_doi_tra` (`so_ngay_doi_tra` mặc định 7) | Lấy chính sách đang hiệu lực (dòng `hieu_luc_tu` gần nhất ≤ thời điểm hiện tại). Hạn là hết ngày thứ N tính theo ngày lịch giờ Việt Nam: mua 10/11 lúc 21:00 thì trả được đến 23:59:59 ngày 17/11 |
| DT2 | Ngưỡng 500.000 đ áp theo phiếu hay hóa đơn | `nguong_tien_can_duyet`, cờ `can_duyet_vuot_nguong` trên phiếu | Theo **tổng tiền hoàn của phiếu** đang lập |
| DT3 | "Số lượng đã trả trước đó" có tính phiếu đang chờ duyệt không | BR-DT-02 | Tính cả phiếu `CHO_DUYET`, `DA_DUYET`, `HOAN_TAT`; không tính `BI_TU_CHOI`. Như vậy không lập được hai phiếu trả trùng cùng một món khi phiếu đầu đang chờ Admin |
| DT4 | BR-DT-08 cho thu ngân tra hóa đơn của mọi thu ngân, nhưng `returns.md` định dùng lại `GET /invoices` mà POS-11 đã giới hạn "chỉ hóa đơn của mình" (BR-POS-11) | Code `PosInvoiceAccess` trên `develop` | Thêm endpoint riêng cho DT-01, ví dụ `GET /returns/invoice-lookup?ma=...` hoặc `?soDienThoai=...`, cần quyền `DOI_TRA`. Chỉ trả hóa đơn khớp đúng mã, hoặc hóa đơn của khách đó còn trong hạn đổi trả. Không nới quyền của `/invoices` |
| DT5 | Giá hoàn khi hóa đơn có khuyến mãi | PA-DT-06.A | Giá thực trả mỗi đơn vị = `don_gia − giam_don_vi` trên `ct_hoa_don`. Không có khuyến mãi cấp hóa đơn (BR-KM-01) nên không phải phân bổ |
| DT6 | Danh mục "không đổi trả" | `danh_muc.khong_doi_tra` | Đọc cờ của danh mục sản phẩm tại thời điểm tra cứu |
| DT7 | DT-01 E2 "hóa đơn đã hủy" | `hoa_don.trang_thai = 'DA_HUY'` | Không hiện nút trả/đổi; ghi rõ lý do |

Phần gọi dịch vụ kho cho DT-05 và POS-12 (tuần 7) xem `docs/inventory/goi-dich-vu-kho-tra-hang-huy-hoa-don.md` (T-KHO-SV2).

## 4. Việc cần làm sau khi chốt

| Việc | Ai | Khi nào |
| --- | --- | --- |
| Sửa mô tả KH-01 (KH1) và thêm các quy tắc KM3, KM4, KM5, DT1–DT4 vào bản đặc tả và SRS v1.2 | #1 | Trong T7-01 |
| Chuyển nguồn dữ liệu KM sang MySQL (KM1) | #5 | Trước khi #3 làm POS-04 |
| Cập nhật `docs/api/returns.md` mục 5 theo bảng DT ở trên; thêm endpoint tra cứu cho DT-01 (DT4) | #3 | Khi làm DT-01 |
| Viết test case cho các ví dụ KM3, DT1, DT3 | #4 | Tuần 7 (T7-04) |
