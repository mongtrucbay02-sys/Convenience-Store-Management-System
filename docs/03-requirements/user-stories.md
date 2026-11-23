# User Stories – Smart Mart v0.1

> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

Các stories dùng mẫu **As a / I want / So that**; actor là proto-persona giả lập, không có xác nhận người dùng thật.

| Story | As a | I want | So that | FR | Scenario |
|---|---|---|---|---|---|
| US-01 | P02 | tìm hàng theo mã hoặc tên | tiếp tục bán khi mã không đọc | FR-01 | SC-SIM-01 |
| US-02 | P02 | thêm hàng, xem tổng và thu tiền mặt | hoàn thành giao dịch đúng số tiền | FR-02,03 | SC-SIM-01 |
| US-03 | P01 | tồn chính thức phản ánh giao dịch bán hoàn tất | tránh chênh giữa bán và tồn | FR-04,10 | SC-SIM-01 |
| US-04 | P02 | tìm giao dịch/hóa đơn đã lưu | phục vụ tra soát khi cần | FR-05 | SC-SIM-05 |
| US-05 | P02 | in lại bản sao hóa đơn | khắc phục máy in trục trặc mà không bán lần hai | FR-06 | SC-SIM-05 |
| US-06 | P03 | nhập số giao, thực nhận, chênh lệch | có số liệu nhận hàng rõ ràng | FR-07 | SC-SIM-02 |
| US-07 | P01/P03 | xác nhận nhận hàng hợp lệ | chỉ cộng tồn theo hàng được chấp nhận | FR-08,09,10 | SC-SIM-02 |
| US-08 | P03 | ghi kết quả kiểm kê và chênh lệch | báo chủ xem xét trước điều chỉnh | FR-11 | SC-SIM-03 |
| US-09 | P01 | xem và quyết định đề xuất điều chỉnh | giữ rõ trách nhiệm và lịch sử | FR-12,13 | SC-SIM-03 |
| US-10 | P01 | kiểm soát quyền thao tác nhạy cảm | hạn chế sửa đổi không được phép | FR-13 | SC-SIM-02,03,05 |
| US-11 | P01 | tra cứu giao dịch theo ngày và đối soát | xem doanh thu cần kiểm tra | FR-14 | SC-SIM-04 |
| US-12 | P03 | thao tác dễ đọc trên thiết bị được lựa chọn | giảm ghi nhầm khi kiểm kho | FR-15 | SC-SIM-02,03 |

## Definition of Ready (proposed)
Story cần FC/Need, điều kiện thành công/thất bại, quyền, trạng thái dữ liệu, AC và quyết định mở được ghi nhận. **G2-01…05 chưa approved** nên các story hiện tại vẫn `Draft`.
