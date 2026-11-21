# Giai đoạn 2 — Product Definition · Smart Mart

> **Trục chính:** [01 Khảo sát](../01-survey/) → **02 Định nghĩa sản phẩm** → [03 Yêu cầu](../03-requirements/) → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu · [Mục lục tài liệu](../README.md)

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Toàn bộ kết quả được xây dựng từ hồ sơ khảo sát giả lập Giai đoạn 1 trong PR #227. Không trình bày đây là yêu cầu đã được khách hàng thật chấp thuận.

**Document ID:** SM-PD-README-001 · **Version:** 0.1 · **Status:** DRAFT / INTERNAL PROPOSAL / NOT FIELD VALIDATED.  
**Nguồn chuẩn để kế thừa:** [`Giai đoạn 1 – Handoff`](../01-survey/analysis/transition-to-product-definition.md), [`Problem Statement`](../01-survey/analysis/problem-statement.md), [`Source Traceability`](../01-survey/analysis/source-traceability.md).  
**Căn cứ bản nguồn:** hồ sơ Giai đoạn 1 trong PR #227 (nhánh `docs/restructure-00-10`).

## Mục tiêu
Xác định sản phẩm Smart Mart dành cho ai, giải quyết vấn đề nào, những tính năng ứng viên, và phạm vi MVP có thể đề xuất cho **một cửa hàng tiện lợi độc lập**. Không chuyển giả thuyết thành yêu cầu đã xác minh.

## Hồ sơ
| Tệp | Nội dung | Trạng thái |
|---|---|---|
| [product-vision.md](product-vision.md) | Tầm nhìn, giá trị, ranh giới và khác biệt | Draft |
| [personas-and-roles.md](personas-and-roles.md) | 3 proto-personas và role boundaries | Draft |
| [scenarios.md](scenarios.md) | 5 scenarios, liên kết As-is và PS | Draft |
| [feature-candidates.md](feature-candidates.md) | Feature candidates, MoSCoW đề xuất, truy vết | Draft |
| [scope.md](scope.md) | Scope, MVP proposal, exclusions, release layers | Draft |
| [success-metrics.md](success-metrics.md) | Baselines chưa biết, phương pháp đo và acceptance gate | Draft |

## Quy tắc xuyên suốt
1. `INT/OBS → F-SIM → P-SIM → PS → N-SIM → FC-SIM → SC-SIM → MVP proposal`.
2. Các mã `N-SIM-01…08` là **nhu cầu ứng viên**, không phải SRS đã Approved.
3. Những module có trong repository nhưng **không** được Giai đoạn 1 hỗ trợ phải dùng nguồn `EXISTING PROJECT / TO VALIDATE`, không tự gán `N-SIM`.
4. PS-05 (xác minh chuyển khoản) **OPEN**; không mặc định kết nối ngân hàng.
5. Mức ưu tiên ở đây là **internal proposal**, cần Product Owner/đại diện cửa hàng và nhóm chốt.
6. Giai đoạn 3 mới đặc tả user stories chi tiết, acceptance criteria, NFR, business rules và SRS.

## Gate trước khi tuyên bố hoàn thành thực tế
- [ ] Có phỏng vấn/quan sát người dùng thật và bằng chứng được phép sử dụng.
- [ ] Các As-is, Findings và Problem Statement thực được xác minh.
- [ ] Có review Product Vision và xác nhận loại nhu cầu, ưu tiên, ranh giới MVP.
- [ ] Làm rõ POS→Inventory, receive→Inventory, kiểm kê/approval và xác minh chuyển khoản.
- [ ] Đồng bộ scope với Issue/PR/implementation thực tế, có người chịu trách nhiệm phê duyệt.

**Kết luận:** Đầy đủ để học tập và lập kế hoạch; chưa vượt cổng phê duyệt khách hàng thực tế.
