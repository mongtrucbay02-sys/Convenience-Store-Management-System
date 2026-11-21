# Bối cảnh và Stakeholder Analysis cho Problem Statement

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Các cuộc phỏng vấn, quan sát, findings và vấn đề đều là kịch bản AI dựng. Tài liệu này chỉ phục vụ luyện tập. Không trình bày như kết luận đã được xác minh tại cửa hàng thật.


**Document ID:** SM-SUR-PS-CTX-001 · **Status:** SIMULATED / DRAFT

## Bối cảnh
Phạm vi được người yêu cầu dự án chốt: **một cửa hàng tiện lợi độc lập**. Ba vai trò nghiên cứu: **Chủ cửa hàng (Store Owner), Thu ngân (Cashier), Nhân viên kho (Warehouse Staff)**. Đây là phạm vi của bài tập, chưa phải mẫu khảo sát đã tiếp cận ngoài đời.

## Stakeholder — hoạt động và quan tâm (theo tình huống AI đóng vai)

| Vai trò | Hoạt động kịch bản | Điều quan tâm trong kịch bản | Nguồn |
|---|---|---|---|
| P01 Chủ cửa hàng | Đối chiếu doanh thu, giám sát biến động tồn, xét thao tác nhạy cảm | Tồn chính xác, tra cứu hóa đơn, báo cáo cuối ngày, chi phí vận hành | INT-001 OWN-07/09/12/16/17 |
| P02 Thu ngân | Tra cứu sản phẩm, lập hóa đơn, thanh toán và xử lý lỗi | Tìm tên khi mã không đọc, xác minh tiền nhận, in lại hóa đơn | INT-002 CAS-02/04/05/06/14; OBS-001 |
| P03 Nhân viên kho | Nhận hàng, ghi số thực nhận, kiểm kê, xin xử lý chênh lệch | Đơn vị tính, so sánh số liệu, lịch sử điều chỉnh, dễ thao tác | INT-003 WH-02/04/09/15/17; OBS-002/003 |

## As-is và trạng thái nguồn
- `AS-SIM-01`: bán hàng tại quầy (OBS-001); một giao dịch mẫu đúng **33.000đ**.
- `AS-SIM-02`: nhận hàng (OBS-002); phiếu **12** chai, thực nhận **10** chai; ghi nhận **10** theo quyết định vai chủ.
- `AS-SIM-03`: kiểm kê (OBS-003); tồn trên bảng tính **18**, đếm **17**, chưa duyệt điều chỉnh và không rõ nguyên nhân.
- `AS-SIM-04`: đối soát doanh thu cuối ngày, **chỉ tái dựng từ phỏng vấn**, không có observation tương ứng.

## Điều kiện/giới hạn cần hỏi lại
- Có thực sự một cửa hàng, số nhân sự kiêm nhiệm thế nào?
- POS, máy quét, máy in, bảng tính, kết nối mạng hiện có ra sao?
- Quy tắc xử lý thanh toán chuyển khoản, hủy hóa đơn và điều chỉnh giá là gì?
- Chi phí đầu tư/bảo trì chấp nhận được; thiết bị nào cần hỗ trợ?

## Rủi ro thiên lệch
Ba vai người dùng và vai quan sát đều do AI mô phỏng; chúng không phải các nguồn xác nhận độc lập. Không tính 'hai nguồn đồng ý' là cross-validation thực địa.
