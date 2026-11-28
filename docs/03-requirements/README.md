# 03 — Requirements Engineering · Smart Mart

> **Trục chính:** [01 Khảo sát](../01-survey/) → [02 Định nghĩa sản phẩm](../02-product-definition/) → **03 Yêu cầu** → [04 Thiết kế](../04-system-design/) → 05 Hiện thực → [06 Kiểm thử](../06-testing/) → 07 Triển khai → 08 Nghiệm thu · [Mục lục tài liệu](../README.md)

> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

**Status:** DRAFT v0.1 · **Scope:** Một cửa hàng độc lập · **Source:** PR #227 (01), PR #228 HEAD `b7f66aa` (02) · **Owner/approval:** TBD.

## Đọc theo thứ tự
| Tệp | Mục đích |
|---|---|
| [../06-testing/test-reports/2026-11-24-postgresql-integration.md](../06-testing/test-reports/2026-11-24-postgresql-integration.md) | Bằng chứng tích hợp PostgreSQL-only và các gate DT/SRS còn mở | Local verified; không tự gán Approved cho SRS |
| [postgresql-runtime-change-20261123.md](postgresql-runtime-change-20261123.md) | Bổ sung kỹ thuật Supabase/UTC/CI, đã hiện thực cục bộ; chờ nhóm duyệt |
| [srs.md](srs.md) | SRS bản nháp, phạm vi và phân loại |
| [functional-requirements.md](functional-requirements.md) | FR-01…FR-15, điều kiện và nguồn |
| [user-stories.md](user-stories.md) | US-01…US-12, vai trò/giá trị |
| [acceptance-criteria.md](acceptance-criteria.md) | AC theo Given–When–Then; cả positive/negative |
| [business-rules.md](business-rules.md) | BR-01…BR-10, nhấn mạnh D-01 *proposed* |
| [non-functional-requirements.md](non-functional-requirements.md) | NFR candidate, phép đo và TBD |
| [use-cases/](use-cases/) | 4 luồng use case ứng viên; không thay thế bộ 81 UC v1.1 ngoài repo |
| [requirements-traceability.md](requirements-traceability.md) | PS/N-SIM → FC → US/FR/AC → dự kiến test |
| [moscow-priority.md](moscow-priority.md) | Ưu tiên *proposed*, chưa được ký duyệt |
| [open-decisions.md](open-decisions.md) | Quyết định mở, gate và phụ thuộc |
| [change-log.md](change-log.md) | Lịch sử bản thảo; không tự nhận đã review |
| [migration-srs-review-2026-11-23.md](migration-srs-review-2026-11-23.md) | Đề xuất cập nhật SRS theo migration runtime; phân biệt bản nháp và phần thiếu cần nhóm duyệt |
| [cash-promotion-change-proposal-20261123.md](cash-promotion-change-proposal-20261123.md) | CP-01–04: đồng bộ hủy/quỹ, chốt số dư và ảnh khuyến mãi; chờ duyệt |
| [week8-bc-km-clarifications.md](week8-bc-km-clarifications.md) | Làm rõ KM-05/06 và BC-09..11 theo issue tuần 8; chờ nhóm xác nhận |

## Đầu vào chính
- [Product Vision](../02-product-definition/product-vision.md), [Feature Candidates](../02-product-definition/feature-candidates.md), [Scope D-01](../02-product-definition/scope.md), [Scenarios](../02-product-definition/scenarios.md), [Success Metrics](../02-product-definition/success-metrics.md).
- [Problem Statement](../01-survey/analysis/problem-statement.md), [Handoff](../01-survey/analysis/transition-to-product-definition.md).

## Quy tắc
1. Các mã FR/US/AC/BR ở đây **không** được đồng nhất tự động với mã SP-01, POS-03… hoặc 81 Use Cases v1.1 từ bộ SRS bên ngoài.
2. MVP *proposed* = I0 + I1 + I2; I3 sau MVP tối thiểu. D-01 chỉ là *đề xuất nội bộ*.
3. Không khẳng định kết quả test, KPI thực tế, hiệu năng, luật thuế/hóa đơn hay ngân hàng khi chưa xác minh.
4. KH/KM/DT là chức năng dự án có thể có nguồn độc lập; **không gán N-SIM giả**.

## Gate trước khi Approved
- [ ] Nhóm xác nhận G2-01…05, chọn MVP và quyền duyệt; [ ] đối chiếu SRS v1.0.1, 81 UC v1.1, MoSCoW/NFR gốc được nêu là ngoài repo; [ ] review nguồn người dùng thật nếu trình bày như khảo sát thực; [ ] rà mã/trace; [ ] kiểm thử sau khi triển khai.
