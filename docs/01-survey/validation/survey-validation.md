# Bước 4 — Findings Validation and Open Questions

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Tất cả nguồn phỏng vấn và quan sát là tình huống AI dựng. Mã E-SIM chỉ trỏ tới nội dung giả lập, không phải bằng chứng hiện trường, lời chứng hay kết quả nghiệm thu. Không đưa các kết luận dưới đây vào báo cáo như đã khảo sát cửa hàng thật.


**ID:** SM-SUR-VAL-001 · **Status:** NOT FIELD VALIDATED.  
**Simulated dialogue confirmation:** [Survey Execution Record](../survey-execution-record.md) chỉ là sự đồng thuận *trong kịch bản*, không thay cho xác nhận của cửa hàng thật.

| Topic | Cần hỏi/kiểm thực địa | Source hiện có | Real validation |
|---|---|---|---|
| Quy trình POS | Công cụ nào; mã lỗi có quét lại/tìm tên được; thời gian xử lý bao lâu | INT-002, OBS-001 | NOT DONE |
| Tiền mặt/chuyển khoản | Ai xác minh tiền đã nhận; khi mất Internet xử lý thế nào | INT-002 CAS-04/10; U-02-03 | NOT DONE |
| Phiếu nhập | Các trường chứng từ; số thực nhận; ai duyệt thiếu/thừa/hỏng | INT-003, OBS-002 | NOT DONE |
| Tồn kho | Khi nào trừ/cộng; kiểm kê bao lâu; quy tắc chuyển trạng thái, sửa phiếu | INT-001, INT-003, OBS-003 | NOT DONE |
| Đơn vị tính | Quy đổi thùng/chai; giới hạn kiểm đếm | INT-003 WH-13; U-03-02 | NOT DONE |
| Quyền | Đổi giá, hủy hóa đơn, duyệt chênh lệch; người kiêm nhiệm | INT-001 OWN-12, INT-002 | NOT DONE |
| Báo cáo | Có export chi tiết, đối soát hoàn tiền không; thời gian chốt | INT-001 OWN-09/11 | NOT DONE |
| Thiết bị & ngân sách | Máy in/quét, trình duyệt điện thoại, Internet và chi phí | INT-001 OWN-17, INT-002 CAS-11 | NOT DONE |

## Biểu mẫu xác nhận thực tế (để trống)

| Item ID | Người xác minh | Ngày | Kết luận / chỉnh sửa | Evidence ID | Status |
|---|---|---|---|---|---|
| F-SIM-001 | | | | | NOT DONE |
| F-SIM-002 | | | | | NOT DONE |
| F-SIM-004 | | | | | NOT DONE |
| F-SIM-005 | | | | | NOT DONE |

## Kết luận

- Đã thực hành phân tích dữ liệu mô phỏng và dựng As-is trong phạm vi các nguồn.
- **Chưa đạt điều kiện nghiệm thu khảo sát thực địa**; không dùng kết quả làm sự thật về một cửa hàng có thật.
- Khi có khảo sát thật: lập mã `INT-REAL-*` / `OBS-REAL-*` riêng, không đổi nhãn SIMULATED và không sửa đổi lịch sử làm như dữ liệu thật.
