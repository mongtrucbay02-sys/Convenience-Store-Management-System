# Business Rules – Candidate Register

> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

**Lưu ý:** D-01 trong Giai đoạn 2 là quyết định *đề xuất* (pending G2-03/04), không phải rule đã được duyệt.

| Rule | Phát biểu đề xuất | Nguồn | Gate |
|---|---|---|---|
| BR-01 | Một sản phẩm chỉ được bán khi hợp lệ và được lựa chọn rõ | FC-SIM-01, SC-SIM-01 | G2-02 |
| BR-02 | Tổng bán dựa vào giá×số lượng đã chọn; phương thức giá/chiết khấu chưa quyết định | SC-SIM-01, M-03 | G2-02 |
| BR-03 | Theo D-01: bán hoàn tất trừ đúng lượng khỏi tồn chính thức; chưa hoàn tất thì không trừ | D-01, M-11 | G2-03/04 |
| BR-04 | Một giao dịch bán hoàn tất không được tạo hai biến động tồn chỉ do retry | Hệ quả thiết kế đề xuất từ D-01 | G2-04 |
| BR-05 | Phiếu nhận cần giữ riêng số giao, số thực nhận, chênh | N-SIM-02, SC-SIM-02 | G2-02 |
| BR-06 | Theo D-01: chỉ nhận hợp lệ đã xác nhận mới cộng tồn, theo lượng được chấp nhận, không cộng trùng | D-01, N-SIM-03 | G2-03/04 |
| BR-07 | Kiểm kê pending chưa sửa tồn chính thức | SC-SIM-03, N-SIM-04 | G2-04 |
| BR-08 | Thao tác đổi giá/hủy đã thanh toán/duyệt điều chỉnh cần quyền và lịch sử | PS-04, N-SIM-06 | G2-04 |
| BR-09 | In lại hóa đơn không làm phát sinh giao dịch bán mới | SC-SIM-05 | G2-02 |
| BR-10 | Không coi ảnh chuyển khoản là xác nhận tiền đã tới; quy trình xác minh giữ OPEN | PS-05, INT-002 | G2-04 |

## Open Business Policies
Chưa có căn cứ chốt chính sách tồn âm; hoàn/hủy sau bán; snapshot kiểm kê; quy đổi thùng/chai; thuế/hoá đơn pháp lý; quy tắc giá, giảm giá và khuyến mãi; quyền nhân viên kiêm nhiệm. Không tự mặc định các chính sách này.
