# SMART MART — GIAI ĐOẠN 1: KHẢO SÁT VÀ XÁC ĐỊNH VẤN ĐỀ

> **Trục chính:** **01 Khảo sát** → [02 Định nghĩa sản phẩm](../02-product-definition/) → 03 Yêu cầu → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu · [Mục lục tài liệu](../README.md) · Ghi nhận AI: [`09-ai-usage/ai-usage-log.md`](../09-ai-usage/ai-usage-log.md) (AI-001)

> **SIMULATED / DỮ LIỆU GIẢ LẬP.** Toàn bộ lời phỏng vấn, quan sát và các findings đều được xây dựng cho tình huống học tập; **không phải khảo sát thực tế**. Không đưa vào báo cáo như chứng cứ từ cửa hàng thật. Các file này là bộ hồ sơ mô phỏng được biên tập từ Bước 1–5.

**Phạm vi:** Một cửa hàng tiện lợi độc lập, ba vai trò: Chủ cửa hàng, Thu ngân, Nhân viên kho.  
**Phương pháp:** Phỏng vấn trực tiếp + quan sát (được **giả lập** trong hồ sơ).  
**Trạng thái:** Hoàn thành tình huống luyện tập; **NOT FIELD VALIDATED / NOT APPROVED**.

## Mục lục theo bước

| Bước | Nội dung chính | Hồ sơ |
|---|---|---|
| **1 — Phạm vi** | Xác định cửa hàng, vai trò, ranh giới | [Survey Scope](survey-scope.md) |
| **2 — Kế hoạch** | Mục tiêu, phương pháp, câu hỏi, bảo vệ thông tin | [Survey Plan](survey-plan.md), [Questionnaire](survey-questionnaire.md) |
| **3 — Thực hiện giả lập** | Ba cuộc phỏng vấn và ba hoạt động quan sát | [Execution Record](survey-execution-record.md), [Evidence Register](evidence-register.md), [INT-001](interviews/records/INT-001-owner.md), [INT-002](interviews/records/INT-002-cashier.md), [INT-003](interviews/records/INT-003-warehouse.md), [OBS-001](observations/records/OBS-001-sales.md), [OBS-002](observations/records/OBS-002-purchasing.md), [OBS-003](observations/records/OBS-003-inventory.md) |
| **4 — Phân tích** | 9 findings; 4 As-is; problem candidates và traceability | [Findings](analysis/survey-findings.md), [As-is Sales](analysis/as-is-sales.md), [As-is Purchasing](analysis/as-is-purchasing.md), [As-is Inventory](analysis/as-is-inventory.md), [As-is Daily Close](analysis/as-is-daily-close.md), [Problem Analysis](analysis/problem-analysis.md), [Source Traceability](analysis/source-traceability.md) |
| **5 — Phát biểu bài toán** | Problem Statement và chuyển giao nhu cầu ứng viên | [Problem Statement](analysis/problem-statement.md), [Handoff](analysis/transition-to-product-definition.md), [Review Gate](validation/problem-statement-review.md) |

**Các biểu mẫu dùng lại:** [Interview Template](interviews/interview-template.md) · [Observation Template](observations/observation-template.md) · [Observation Checklist](observations/observation-checklist.md)  
**Kiểm soát giả định:** [Simulation Assumptions](evidence/simulation-assumptions.md) · [Survey Validation](validation/survey-validation.md)

## Dữ liệu minh họa thống nhất

- **Bán hàng:** 2 chai × 12.000đ + 1 snack × 9.000đ = **33.000đ**; nhận **50.000đ**, trả **17.000đ**. Quét mã có lần không thành công, có thao tác tìm theo tên.
- **Nhập hàng:** phiếu ghi **12** chai; thực nhận **10**; chênh lệch **−2**; chỉ ghi nhận số thực nhận được chấp nhận trong tình huống.
- **Kiểm kê:** sổ/bảng tính ghi **18**; kiểm đếm **17**; chênh lệch **−1**; **chưa duyệt điều chỉnh** khi chưa đối chiếu chứng từ.
- **Đối soát cuối ngày:** chỉ có phỏng vấn giả lập; **không có quan sát trực tiếp tương ứng**, kể cả trong kịch bản.

Không suy diễn số lượng nhân viên thực tế, tỷ lệ lỗi, tổn thất, thời gian tiết kiệm, ngân sách, số giao dịch/ngày hoặc hiệu quả phần mềm.

## Trình tự truy vết

`INT/OBS (Bước 3) → F-SIM (Bước 4) → P-SIM (Problem Analysis) → PS (Bước 5) → N-SIM (Ứng viên nhu cầu cho Giai đoạn 2)`.

**Phân biệt:** `F-SIM` là phát hiện *trong kịch bản*, `P-SIM` là vấn đề phân tích *trong kịch bản*, `N-SIM` là nhu cầu ứng viên, **không phải** yêu cầu SRS đã được phê duyệt.

## Quy trình chuyển thành hồ sơ khảo sát thực tế

1. Xác nhận cửa hàng và người tham gia, thông báo mục đích và xin phép.
2. Thực hiện phỏng vấn/quan sát thật; không sửa đè dữ liệu giả lập thành dữ liệu thật nếu mất truy vết.
3. Tạo mã biên bản mới, ẩn danh, lưu bằng chứng với quyền truy cập phù hợp.
4. Lập Findings và As-is thực tế, đánh dấu Unverified cho nội dung chưa đối chiếu.
5. Review Problem Statement với đại diện cửa hàng trước khi chốt Product Vision, MVP và SRS.

Không đưa dữ liệu khách hàng, mật khẩu, ảnh/ghi âm chưa được phép công bố lên GitHub công khai.
