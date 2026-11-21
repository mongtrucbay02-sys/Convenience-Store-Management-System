> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

# UC-CAND-KHO-01 — Kiểm kê có phê duyệt (I3 candidate)

**Actors:** P03 lập, P01 duyệt. **Source:** SC-SIM-03; **FR:** FR-11, FR-12, FR-13; **State:** Draft / outside minimal MVP.

**Main flow:** (1) ghi tồn 18 → (2) đếm 17, đếm lại 17 → (3) ghi chênh −1 → (4) pending không đổi tồn → (5) duyệt/từ chối theo quyền, lưu dấu vết; chỉ xử lý tồn theo quyết định hợp lệ.  
**Alternatives:** không lệch, từ chối, chứng từ thiếu, tồn đã đổi sau snapshot (policy pending).  
**AC:** AC-08a, AC-09a. **Open:** snapshot, concurrent adjustments, quy tắc hạch toán chênh.
