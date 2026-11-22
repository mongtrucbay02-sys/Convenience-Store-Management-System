> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

# UC-CAND-NH-01 — Nhận hàng có chênh lệch

**Actors:** P03, P01 (nếu cần duyệt). **Source:** SC-SIM-02, D-01; **FR:** FR-07…10; **State:** Draft.

**Preconditions:** hàng/chứng từ được giao; có catalog.  
**Main flow:** (1) tạo phiếu → (2) ghi số trên chứng từ 12 → (3) đếm thực 10 → (4) lưu chênh −2 → (5) xin xác nhận theo chính sách → (6) duyệt số được chấp nhận 10 → (7) cộng tồn +10 đúng một lần.  
**Alternatives:** chờ duyệt, từ chối, nhận 0, hàng hỏng, đơn vị khác, retry xác nhận.  
**Postconditions:** số nhận/giấy tờ còn truy vết; pending không cộng hàng; phiếu được xác nhận có đúng một biến động.  
**AC:** AC-06a/b, AC-07a/b/c. **Open:** chính sách duyệt và quy đổi thùng/chai.
