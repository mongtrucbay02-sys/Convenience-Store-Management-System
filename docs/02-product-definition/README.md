# 02 — Định nghĩa sản phẩm

> **Trục chính:** 01 Khảo sát → **02 Định nghĩa sản phẩm** → 03 Yêu cầu → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu

## Mục đích
Chuyển kết quả khảo sát thành **định nghĩa sản phẩm**: xây cái gì, cho ai, giải quyết vấn đề nào, và phạm vi đến đâu (MVP vs. tầng 2).

## Đầu vào → Đầu ra
- **Đầu vào:** `01-survey/survey-findings.md` (mã `KS-xx`).
- **Đầu ra cho `03`:** tầm nhìn, nhóm người dùng/vai trò, danh sách module và ranh giới phạm vi.

## Nội dung cần có

| Tài liệu | Mô tả | Trạng thái |
|---|---|---|
| `product-vision.md` | Tầm nhìn, vấn đề cần giải quyết, giá trị mang lại | Chưa có |
| `personas-and-roles.md` | Chân dung người dùng và vai trò hệ thống (quản lý, thu ngân, nhân viên kho…) | Chưa có |
| `scope.md` | Danh sách module SP, NH, KHO, KH, KM, POS, DT, BC, AUTH; phân định **MVP** và **tầng 2 / sau MVP** | Chưa có |
| `success-metrics.md` | Tiêu chí đánh giá sản phẩm thành công (dùng lại ở `08`) | Chưa có |

> Một số quyết định phạm vi đã được nêu rải rác trong tài liệu thiết kế (ví dụ: Đổi trả được đánh dấu *Sau MVP* theo Issue #4; Khuyến mãi POS-04 thuộc tầng 2). Khi viết `scope.md`, nên gom các quyết định này về đây làm nguồn chính.

## Tiêu chí hoàn thành
- [ ] Mỗi module trong `scope.md` liên kết tới ít nhất một mã `KS-xx`.
- [ ] Ranh giới MVP được trưởng nhóm duyệt.
