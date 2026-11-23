> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

# UC-CAND-POS-01 — Bán hàng tiền mặt và ghi tồn

**Actor:** P02; **Source:** SC-SIM-01, D-01; **FR:** FR-01…05; **State:** Draft.

**Preconditions (proposed):** đăng nhập đủ quyền; có sản phẩm; tồn có thể kiểm tra.  
**Main flow:** (1) tìm mã/tên → (2) thêm hàng → (3) tính tổng → (4) nhận tiền mặt và xác nhận → (5) ghi giao dịch hoàn tất + trừ tồn theo D-01 đúng một lần → (6) tra hóa đơn.  
**Alternatives:** không tìm thấy sản phẩm; sản phẩm ngừng bán; hủy trước khi thanh toán; retry; không đủ tồn (policy pending); lỗi đồng bộ tồn phải ngăn công bố trạng thái hoàn tất nhất quán giả.  
**Postconditions:** một giao dịch hoàn tất, biến động tồn tương ứng; hoặc không có thay đổi một phần trái chính sách.  
**AC:** AC-01a/b, AC-02a/b, AC-03a/b/c, AC-04a.  
**Open:** xử lý tồn âm/hủy/hoàn, atomicity và payment reconciliation.
