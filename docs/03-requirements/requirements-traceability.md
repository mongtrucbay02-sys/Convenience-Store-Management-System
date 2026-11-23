# Requirements Traceability – 01 → 02 → 03 → 06

> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

**Nguồn khảo sát:** [source-traceability](../01-survey/analysis/source-traceability.md) → [handoff](../01-survey/analysis/transition-to-product-definition.md). Test cases ngoài bản này chưa được tạo/chạy.

| Need | PS / constraint | Feature | User Story | FR | AC | Test mapping |
|---|---|---|---|---|---|---|
| N-SIM-01 | PS-02 | FC-SIM-01,04 | US-01,02 | FR-01,02,03 | AC-01a/b,02a/b | TODO (`06-testing`) |
| N-SIM-02 | PS-01 | FC-SIM-02 | US-06 | FR-07 | AC-06a/b | TODO (`06-testing`) |
| N-SIM-03 | PS-01 | FC-SIM-03 | US-03,07 | FR-04,08,09,10 | AC-03a/b/c,07a/b/c | TODO (`06-testing`) |
| N-SIM-04 | PS-01,04 | FC-SIM-05 | US-08,09 | FR-11,12 | AC-08a,09a | TODO (`06-testing`) |
| N-SIM-05 | PS-02,04 | FC-SIM-04 | US-04,05 | FR-05,06 | AC-04a,05a | TODO (`06-testing`) |
| N-SIM-06 | PS-04 | FC-SIM-06 | US-10 | FR-13 | AC-10a/b | TODO (`06-testing`) |
| N-SIM-07 | PS-03 | FC-SIM-07 | US-11 | FR-14 | AC-11a | TODO (`06-testing`) |
| N-SIM-08 | CON-01 | FC-SIM-08 | US-12 | FR-15 | AC-12a | TODO (`06-testing`) |

## Coverage & exclusions
- 8/8 N-SIM có ít nhất một FC, US, FR và AC candidate.
- PS-05 là **open question**, không có N-SIM đã duyệt cho bank API.
- KH/KM/DT: có thể có yêu cầu từ tài liệu ngoài luồng khảo sát; chỉ thêm khi xác định nguồn thật và ma trận mã cũ.
- NFR-01…10 là đề xuất đo/kiểm tra, không có baseline đã xác minh.
- Chưa gắn test IDs hay status PASS; sẽ đưa vào `docs/06-testing/` sau review.
