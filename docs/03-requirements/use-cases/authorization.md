> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

# UC-CAND-AUTH-01 — Kiểm soát thao tác nhạy cảm

**Actors:** P01/P02/P03 theo matrix chưa approved. **Source:** N-SIM-06, PS-04; **FR:** FR-13; **State:** Draft.

**Main flow:** (1) người dùng đã đăng nhập gửi thao tác → (2) kiểm quyền đối với hành động/đối tượng → (3) cho phép/từ chối → (4) ghi audit theo policy.  
**Examples:** thu ngân tự sửa giá: từ chối; chủ duyệt phiếu: cần quyền hợp lệ.  
**AC:** AC-10a/b. **Open:** người kiêm nhiệm, ủy quyền, scope theo ca, retention audit, role matrix chi tiết.
