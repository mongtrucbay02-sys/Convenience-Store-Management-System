# Open Decisions / Approval Gate – 03

> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

| ID | Câu hỏi chưa chốt | Liên hệ | Tác động | Owner/decision |
|---|---|---|---|---|
| OD-01 | G2-01…05 và D-01 được ai duyệt, ngày nào? | 02/scope | MVP & FR-04,09 | PENDING |
| OD-02 | Bán thiếu tồn: từ chối, cho đặt trước, hay xử lý khác? | FR-04 | API/test | PENDING |
| OD-03 | Một sổ tồn chính thức: giao dịch, retry, rollback và truy vết lỗi? | FR-04,09,10 | Kiến trúc/test tích hợp | PENDING |
| OD-04 | Ai duyệt phiếu thường/ngoại lệ; đơn vị tính thùng/chai? | FR-07,08 | NH & KHO | PENDING |
| OD-05 | Quy tắc hủy/hoàn tiền/khôi phục tồn sau bán? | PS-04 / DT | POS/DT | PENDING |
| OD-06 | Trạng thái chuyển khoản xác minh bởi ai và phương pháp nào? | PS-05 | POS/BC; không mặc định API ngân hàng | OPEN |
| OD-07 | Quyền thao tác khi người kiêm nhiều vai trò, retention audit? | FR-13 | Auth/security | PENDING |
| OD-08 | Thiết bị, Internet, tốc độ, locale/timezone, ngân sách? | NFR-01…10 | Target/NFR | UNKNOWN |
| OD-09 | Đưa vào/đối chiếu SRS v1.0.1, 81 UC v1.1, MoSCoW, NFR gốc ngoài repo? Lưu ý: nhánh `develop` đã có `docs/srs/SRS_v1.2_danh-sach-thay-doi.md` (thay đổi từ SRS v1.0.3 lên v1.2) và `docs/dac-ta/T6-02_ra-soat-dac-ta-KH-KM-DT.md` — cần xác định phiên bản SRS hiện hành (v1.0.1, v1.0.3 hay v1.2) | docs/03 README trước; nhánh `develop` | Tránh trùng mã/yêu cầu, lệch phiên bản | OPEN |
| OD-10 | KH/KM/DT trong repo có requirement source/decision nào? | 02/feature-candidates | Scope change | OPEN |

Khi quyết định: ghi người, ngày, evidence và PR; **không đánh Approved** từ suy luận hay từ việc code tồn tại.
