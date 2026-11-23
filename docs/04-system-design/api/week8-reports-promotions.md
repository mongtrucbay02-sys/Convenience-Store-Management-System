# Hợp đồng bổ sung tuần 8 — #246–250

Trạng thái: **Đang review, chờ #4 kiểm tra hợp đồng**, 23/11/2026. Kế thừa AUTH cookie `cs_session`, lỗi chung và múi giờ Việt Nam của repo. Actor tất cả endpoint bên dưới: **AD**. TN/NK bị từ chối 403, chưa đăng nhập 401. Không thay quyền cũ của KM-01.

Bản máy đọc: [OpenAPI 3 JSON](week8-reports-promotions.openapi.json). Đây là phần bổ sung riêng cho endpoint tuần 8, không tự thay thế toàn bộ đặc tả API các module khác. Trang MVC mới cũng chặn tài khoản đang buộc đổi mật khẩu tạm.

| HTTP | Đường dẫn | Kết quả |
|---|---|---|
| GET | `/api/promotions/{id}/usage` | `PromotionHistory`: id/code/name, uses, totalDiscount; mỗi use có promotionId/name, invoiceId/code, paidAt, cashierName, customerName, discount, invoiceNet |
| DELETE | `/api/promotions/{id}` | 204 nếu xóa; 404 không tồn tại; 409 đã có tham chiếu / phát sinh tham chiếu đồng thời |
| GET | `/api/reports/returns` | BC-09, cùng DTO `OperationalReportResponse` |
| GET | `/api/reports/customers` | BC-10 |
| GET | `/api/reports/promotions` | BC-11 |

DELETE cần header XSRF của AUTH được tính từ phiên hiện tại, như các API ghi khác. Không truyền role qua header tự đặt. Giao diện MVC có trang xác nhận và POST antiforgery; GET không xóa dữ liệu.

Các báo cáo dùng `Period` (`today`, `7days`/`last7days`, `thisMonth`, `lastMonth`, `custom`), `From`, `To`. Khoảng hai đầu bao gồm ngày chọn; truy vấn `[00:00 From, 00:00 To+1)` theo UTC+7. E1/E2 trả API 400; E3 trả 200 với `message="Không có dữ liệu trong khoảng đã chọn"`, rows/chart rỗng. BC-09 thêm `ReturnReason` (LOI_SAN_PHAM/HET_HAN/KHONG_DUNG_NHU_CAU/KHAC), `CashierId`; BC-11 thêm `PromotionId`. Id không dương hoặc enum lọc sai trả 400.

MVC: `/Reports/Reports/Returns`, `/Customers`, `/Promotions` với tiền tố `/Reports/Reports`; chi tiết dùng `Detail?code=BC-09&key=reason:KHAC`, `BC-10&key=customer:{id}`, `BC-11&key=promotion:{id}`. Xuất Excel dùng `Export?code=BC-09` (tương tự 10/11) và toàn bộ bộ lọc hiện tại. Không có dữ liệu hoặc bộ lọc lỗi thì không tạo file Excel rỗng.

## Định nghĩa triển khai, cần nhóm xác nhận

- **BC-09** chỉ lấy phiếu HOAN_TAT và hóa đơn gốc còn DA_THANH_TOAN. Dòng trả lấy tên/giá thực trả từ snapshot. Phân biệt **giá trị hàng trả** và **tiền mặt hoàn**: đổi hàng có thể hoàn tiền mặt 0. Tiền hoàn header phân bổ theo giá trị dòng, lấy phần dư ở dòng cuối để tổng khớp tuyệt đối. Một phiếu nhiều lý do được đếm ở từng lý do; tổng phiếu tính distinct.
- Tỷ lệ trả = giá trị hàng trả sau bộ lọc / doanh thu sau KM toàn cửa hàng trong kỳ; mẫu số 0 hiển thị 0%. Chỉ số doanh thu toàn kỳ dùng toàn cửa hàng; không nhầm là doanh thu riêng một lý do. Nếu nhóm muốn mẫu số theo thu ngân hoặc cohort hóa đơn gốc, cần chốt và sửa test tương ứng.
- **BC-10** khách mới theo `khach_hang.ngay_tao`; khách có mua đếm distinct khách trên hóa đơn paid trong kỳ; tỷ lệ đơn gắn khách tính trên mọi hóa đơn paid. Chi tiêu thuần = bán sau KM trừ giá trị hàng trả hoàn tất trong kỳ của khách, kể cả bán kỳ trước. Số điểm lấy từ sự kiện ledger trong kỳ: TICH_DIEM, THU_HOI_*; DIEU_CHINH hiện riêng, không gộp thành tích điểm. Thu hồi hủy vẫn là sự kiện thực tế dù hóa đơn đã hủy, không che mất lịch sử điểm.
- **BC-11** dùng cùng view với KM-06. Một hóa đơn có hai CT có một lượt ở mỗi CT. Doanh thu mỗi dòng là doanh thu đơn có CT đó; các dòng có thể chồng nhau. Tổng doanh thu gom các hóa đơn không trùng, không cộng thẳng doanh thu từng CT. Giá trị TB đơn là tổng sau KM tại lúc bán / số đơn của nhóm; hàng trả kỳ này giảm riêng vào doanh thu thuần kỳ này. Không suy đoán tăng doanh thu do KM hay ROI nhân quả từ các số liệu mô tả.
- Khi lọc một CT, tổng giảm của CT chỉ lấy dòng thuộc CT đó; doanh thu đơn có CT vẫn là toàn bộ đơn, đã trừ mọi KM trên đơn. Phiếu trả của đơn kỳ trước vẫn hiện trong báo cáo kỳ trả.
- **KM-05** thực hiện luồng xóa chương trình chưa dùng của issue, đồng thời giữ Kết thúc như lựa chọn thay thế. Phần PA-KM-05.A trong issue có cách diễn đạt mâu thuẫn luồng chính; cần nhóm xác nhận cách hiểu này trước khi nghiệm thu.

Không thêm bảng hoặc migration tuần 8. Hợp đồng này bổ sung các endpoint mới; đặc tả OpenAPI trong repo vẫn cần người phụ trách #4 đối chiếu và duyệt. Không đánh dấu #4 đã test hoặc staging đã chạy khi chỉ có test cục bộ.
