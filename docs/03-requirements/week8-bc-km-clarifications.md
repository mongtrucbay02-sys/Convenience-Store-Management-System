# Làm rõ yêu cầu tuần 8 BC/KM — #246–252

Ngày 23/11/2026. Trạng thái: **Đề xuất / chờ nhóm review**, không sửa đè đặc tả đã duyệt. Không tự đổi ưu tiên Could thành Must hoặc tự cắt chức năng trong đợt triển khai này.

| Nội dung cần làm rõ | Cách triển khai để kiểm thử | Cần xác nhận |
|---|---|---|
| KM-05 vừa nói Kết thúc thay xóa, vừa có luồng xóa | Thực hiện luồng xác nhận xóa chương trình tạo nhầm chưa tham chiếu; giữ nút Kết thúc cho chương trình đã dùng | #1 / điều phối và người review #2 xác nhận cách hiểu |
| KM-06 bỏ lượt hóa đơn hủy, nhưng KM-05 bảo vệ lịch sử | Lượt dùng hợp lệ từ view paid; điều kiện xóa kiểm tra thêm tất cả dòng hóa đơn/giỏ bất kể trạng thái | Không hiểu usage=0 thành được phép xóa mọi trường hợp |
| BC-09 có phiếu đổi với tiền mặt hoàn 0 | Hiển thị riêng giá trị hàng trả và tiền mặt hoàn; doanh thu giảm bằng giá trị hàng trả, không tạo tiền mặt hoàn giả | Thống nhất với DT-03 trước nghiệm thu tích hợp |
| Đồng nhất BC-02–05/08 với BC-09–11 sau chuyển PostgreSQL | Bản sửa #266 ngày 24/11 đọc `ct_phieu_doi_tra.thanh_tien` vào giảm trừ doanh thu theo ngày hoàn tất, gồm phiếu đổi. `tien_hoan` vẫn là chỉ số tiền mặt riêng của BC-09/quỹ; không phân bổ tiền mặt này thay cho giá trị hàng trả. Chi tiết BC-02–05 hiển thị cả bán và trả, kể cả hóa đơn gốc ngoài kỳ. | Làm rõ cùng quy tắc hàng trên; chờ reviewer nghiệp vụ xác nhận, chưa coi là SRS được duyệt |
| Phần dư phân bổ giảm giá theo bộ lọc | BC-03/04/08 và chi tiết dùng cùng phân bổ từng dòng: làm tròn xuống, phần dư vào dòng cuối, sau đó mới lọc. Không tính lại tỷ lệ trên tập sản phẩm sau lọc. | Đối chiếu regression 45/46 đồng; giữ tổng giảm trừ bằng giảm giá hóa đơn |
| BC-09 mẫu số tỷ lệ trả chưa định nghĩa | Giá trị hàng trả theo bộ lọc / doanh thu sau KM toàn cửa hàng cùng kỳ; mẫu số 0 hiển thị 0%; nhãn UI nêu rõ | Nếu muốn theo thu ngân/cohort hóa đơn gốc cần điều chỉnh thống nhất |
| Một phiếu có nhiều lý do | Đếm distinct phiếu tại tổng; mỗi lý do có thể cùng đếm phiếu đó; chỉ số sản phẩm trả nhiều nhất gom toàn bộ lý do sau lọc | Không cộng thẳng số phiếu từng nhóm để ra tổng |
| BC-10 tích/thu hồi/điều chỉnh | Sự kiện điểm theo ngày ledger; chỉ TICH_DIEM là cộng, THU_HOI_* là thu hồi; DIEU_CHINH riêng. Hóa đơn hủy không tính doanh số nhưng không che mất sự kiện thu hồi điểm thực tế | Không sửa lịch sử để làm báo cáo khớp giả tạo |
| BC-11 một đơn nhiều CT | Mỗi CT có lượt riêng, doanh thu dòng là của các đơn chứa CT; tổng doanh thu lấy union hóa đơn, không cộng dòng chồng nhau | Người dùng cần đọc chú thích khi so dòng với tổng |
| BC-11 giá trị TB đơn | Tổng sau KM tại lúc bán / số đơn nhóm; giá trị hàng trả kỳ này giảm riêng vào doanh thu thuần kỳ này | Không diễn giải như ROI hoặc doanh thu tăng thêm do CT |
| Chưa đổi mật khẩu tạm | Chặn trang báo cáo MVC và KM-05/06 cho tài khoản buộc đổi mật khẩu; API giữ kiểm tra AUTH chung | Không cấp thêm quyền cho TN/NK để mở báo cáo |

Đặc tả API đầy đủ hơn: [Markdown](../04-system-design/api/week8-reports-promotions.md), [OpenAPI 3 JSON](../04-system-design/api/week8-reports-promotions.openapi.json). Đã viết thành hợp đồng để người review có thể đối chiếu; chưa đánh dấu #4 đã nghiệm thu hợp đồng.

Bố cục điện thoại chưa nghiệm thu và hoãn theo người dùng. Đây là trạng thái công việc giao diện, không tự tuyên bố bỏ yêu cầu responsive của cả nhóm.
