# Success Metrics & Evaluation Plan — Smart Mart

> **SIMULATED / BASELINES UNKNOWN.** Không có số liệu doanh thu, thời gian, tỷ lệ lỗi thực tế từ Giai đoạn 1. Những thước đo dưới đây là đề xuất thiết kế phép đánh giá, **không phải thành tích của sản phẩm**.

**ID:** SM-PD-METRICS-001 · **Status:** DRAFT.

## 1. Nguyên tắc đo
- Phân biệt **business outcome** (chỉ có thể đo với cửa hàng thực) và **acceptance evidence** (có thể kiểm thử bằng dữ liệu test).
- Thời gian/tỷ lệ/khả năng sử dụng cần **baseline trước** và cùng điều kiện test sau; không tự đặt chỉ tiêu thay chủ cửa hàng.
- Giữ case mô phỏng 33.000đ; 12/10; 18/17 để xây dữ liệu test, không gọi đó là dữ liệu hiệu quả vận hành.

## 2. Measurement plan
| Metric ID | Metric | Phương pháp đo | Baseline | Target | Source |
|---|---|---|---|---|---|
| M-01 | Tỉ lệ tìm được sản phẩm khi barcode không đọc | Số case tìm thành công / tổng case thử với dataset công bố | UNKNOWN | TBD | FC-SIM-01; SC-SIM-01 |
| M-02 | Thời gian xử lý giao dịch bán cơ bản | Đo từ chọn hàng đến ghi nhận hoàn tất, cùng số món và thiết bị | UNKNOWN | TBD | FC-SIM-04 |
| M-03 | Tính đúng tổng tiền | Kiểm tra phép tính độc lập và các case exception | Chưa có | 100% **test cases đã chấp thuận** | SC-SIM-01 |
| M-04 | Ghi tồn sau nhập thực nhận | Test expected delta vs recorded delta trên giao dịch được chấp thuận | Chưa có | Không ghi +12 khi chấp thuận +10 | SC-SIM-02 |
| M-05 | Không chỉnh tồn trước duyệt kiểm kê | Kiểm thử chuyển trạng thái pending→approved/rejected | Chưa có | Tồn không đổi khi pending | SC-SIM-03 |
| M-06 | Kiểm soát role và audit | Negative/positive tests theo permission matrix được phê duyệt | Chưa có | Pass các case được duyệt | FC-SIM-06 |
| M-07 | Khả năng tra cứu giao dịch | Test tra cứu theo mã/ngày và bản sao hóa đơn không tạo giao dịch mới | Chưa có | Pass các case được duyệt | SC-SIM-05 |
| M-08 | Đối soát doanh thu | So dữ liệu test đã chốt và các giao dịch đủ điều kiện | UNKNOWN | TBD | SC-SIM-04 |
| M-09 | Dễ dùng trên thiết bị mục tiêu | Task walkthrough với vai trò được chấp thuận | UNKNOWN | TBD | N-SIM-08 |
| M-10 | Requirement coverage của MVP | US/AC được liên kết với test và demo evidence | Chưa có | 100% AC **MVP đã phê duyệt có trạng thái** | Scope/03 |

`100%` trong M-03 và M-10 là **tiêu chí kiểm tra độ bao phủ/độ đúng trên tập test đã phê duyệt**, không khẳng định phần mềm đạt 100% ở môi trường thực.

## 3. Đánh giá theo luồng mẫu
1. **POS:** 2×12.000 + 9.000 = 33.000; 50.000−33.000=17.000, tra được mã giao dịch.
2. **Receiving:** phiếu 12, thực nhận 10; thông tin chênh -2 được lưu; chỉ cập nhật tồn theo số đã xác nhận.
3. **Inventory:** tồn 18, đếm 17; pending chưa thay đổi tồn; sau quyết định hợp lệ mới xử lý theo policy.
4. **Security:** thu ngân không tự ý đổi giá/hủy hóa đơn đã trả, nếu policy cuối cùng xác nhận điều này.

## 4. Thông tin cần thu thập trước khi đặt target thực
- Cửa hàng thực, danh mục SKU, thiết bị barcode/POS/máy in, mô hình Internet.
- Số lượt thử, phương pháp chọn case, khoảng thời gian quan sát; tần suất nghiệp vụ thực.
- Hình thức tiền mặt/chuyển khoản, chính sách thu/hoàn tiền.
- Quy tắc tạo adjustment từ kiểm kê, phê duyệt và logging.
- Người duyệt ưu tiên và tiêu chí thành công.

## 5. Evidence map
- `03-requirements/`: User Stories, Acceptance Criteria, NFR, SRS.
- `06-testing/`: Test Plan, Test Cases, Unit/Integration/E2E Evidence.
- `08-validation/`: MVP Acceptance Report, Requirement Coverage, user feedback thật (nếu có).

**Status:** Design of evaluation complete for practice; business baselines and approval unresolved.
