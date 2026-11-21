# 01 — Khảo sát (Giai đoạn 1)

> **Trục chính:** **01 Khảo sát** → 02 Định nghĩa sản phẩm → 03 Yêu cầu → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu

> ⚠️ **SIMULATED / DỮ LIỆU GIẢ LẬP.** Toàn bộ phỏng vấn, quan sát, findings và vấn đề hiện có trong thư mục này là kịch bản do AI dựng để luyện tập, **chưa phải khảo sát thực địa**. Mọi mã đều mang hậu tố `SIM` và cấp bằng chứng `S0`. Không trình bày như kết luận đã xác minh. Xem ghi nhận tại [`09-ai-usage/ai-usage-log.md`](../09-ai-usage/ai-usage-log.md).

## Mục đích
Ghi lại cửa hàng tiện lợi **đang vận hành thế nào** và **vấn đề/nhu cầu** thực tế, kết thúc bằng một **Problem Statement** làm căn cứ cho `02-product-definition`.

**Phạm vi khảo sát:** một cửa hàng tiện lợi độc lập (Smart Mart); ba vai trò Chủ cửa hàng (P01), Thu ngân (P02), Nhân viên kho (P03).

## Cấu trúc thư mục

```text
01-survey/
├── README.md                     ← tổng quan Giai đoạn 1 (tệp này)
├── README-step-5.md              Hướng dẫn riêng Bước 5
├── interviews/                   Bước 3 — INT-001…003           (chưa có trong repo)
├── observations/                 Bước 3 — OBS-001…003           (chưa có trong repo)
├── analysis/
│   ├── survey-findings.md        Bước 4 — F-SIM-001…009        (chưa có trong repo)
│   ├── problem-analysis.md       Bước 4 — P-SIM-01…08          (chưa có trong repo)
│   ├── source-traceability.md    Bước 4                        (chưa có trong repo)
│   ├── as-is-*.md                Bước 4 — sales, purchasing, inventory, daily-close (chưa có)
│   ├── problem-statement.md                  Bước 5
│   ├── problem-context-and-stakeholders.md   Bước 5
│   ├── problem-evidence-matrix.md            Bước 5
│   ├── problem-objectives-and-boundaries.md  Bước 5
│   ├── source-traceability-reference.md      Bước 5
│   └── transition-to-product-definition.md   Bước 5 → bàn giao cho 02
└── validation/
    ├── survey-validation.md      Bước 4                        (chưa có trong repo)
    └── problem-statement-review.md           Bước 5 — cổng duyệt
```

## Các bước của Giai đoạn 1

| Bước | Nội dung | Đầu ra | Trạng thái trong repo |
|---|---|---|---|
| 1–2 | Chuẩn bị khảo sát | — | Chưa có |
| 3 | Phỏng vấn và quan sát | `INT-001…003`, `OBS-001…003` | **Thiếu** — đang được tham chiếu |
| 4 | Tổng hợp findings, phân tích vấn đề, As-is | `F-SIM-xxx`, `P-SIM-xx`, `AS-SIM-xx`, `N-SIM-xx` | **Thiếu** — đang được tham chiếu |
| 5 | Problem Statement | `PS-01…05`, `CON-01`, `O-01…05`, `RV-01…08` | Có (giả lập, Draft 0.1) |

> Các tệp Bước 5 đang liên kết tới tệp Bước 3–4. Những liên kết này sẽ hoạt động khi bổ sung các tệp còn thiếu vào đúng đường dẫn ở trên.

## Hệ thống mã truy vết

```text
INT / OBS (Bước 3) → F-SIM (finding) → P-SIM (problem) → PS (problem statement) → N-SIM (need candidate) → 02-product-definition
```

| Tiền tố | Ý nghĩa | Ví dụ |
|---|---|---|
| `INT-xxx` / `OBS-xxx` | Phỏng vấn / quan sát | `INT-002 CAS-04`, `OBS-003:2–6` |
| `AS-SIM-xx` | Quy trình hiện trạng (As-is) | `AS-SIM-02` nhận hàng |
| `F-SIM-xxx` | Phát hiện khảo sát | `F-SIM-004` |
| `P-SIM-xx` | Vấn đề ứng viên | `P-SIM-03` |
| `PS-xx` / `CON-xx` | Phát biểu bài toán / ràng buộc | `PS-01a`, `CON-01` |
| `O-xx` | Mục tiêu nghiên cứu | `O-03` |
| `N-SIM-xx` | Nhu cầu ứng viên chuyển sang `02` | `N-SIM-06` |
| `RV-xx` | Câu hỏi xác minh với cửa hàng thật | `RV-07` |

Khi có khảo sát thật, tạo mã mới **không có `SIM`** và giữ nguyên hồ sơ giả lập để so sánh.

## Bàn giao cho `02-product-definition`
Đầu vào cho Giai đoạn 2: [`analysis/transition-to-product-definition.md`](analysis/transition-to-product-definition.md) — 8 nhu cầu ứng viên `N-SIM-01…08`.

**Lưu ý:** PS-05 (xác minh tiền chuyển khoản) chưa được chốt thành giải pháp.

## Tiêu chí hoàn thành
- [x] Có Problem Statement và ma trận Problem–Evidence (bản giả lập).
- [ ] Bổ sung hồ sơ Bước 3–4 vào repo để mọi liên kết hoạt động.
- [ ] Trả lời các câu hỏi `RV-01…08` với cửa hàng thật.
- [ ] Người đại diện cửa hàng duyệt Problem Statement. Cổng hiện tại: **chỉ dùng để luyện tập; khảo sát thật đang bị chặn.**
