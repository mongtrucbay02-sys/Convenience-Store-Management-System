# 03 — Yêu cầu

> **Trục chính:** 01 Khảo sát → 02 Định nghĩa sản phẩm → **03 Yêu cầu** → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu

## Mục đích
Đặc tả **hệ thống phải làm được gì**: yêu cầu chức năng, phi chức năng, Use Case và quy tắc nghiệp vụ. Đây là căn cứ để thiết kế (`04`), viết test (`06`) và nghiệm thu (`08`).

## Đầu vào → Đầu ra
- **Đầu vào:** `02-product-definition/scope.md`, `personas-and-roles.md`.
- **Đầu ra cho `04`:** SRS và Use Case có mã chức năng (SP-01, POS-03, BC-12…), có mức ưu tiên MoSCoW.

## Nội dung cần có

| Tài liệu | Mô tả | Trạng thái |
|---|---|---|
| `srs.md` (hoặc `srs-v1.0.1.pdf`) | Đặc tả yêu cầu phần mềm của toàn hệ thống | **Đã có ngoài repo — cần đưa vào** |
| `use-cases/` | Đặc tả Use Case theo module, ví dụ `use-cases/pos.md` | **Đã có ngoài repo (81 Use Case v1.1) — cần đưa vào** |
| `moscow-priority.md` | Bảng ưu tiên MoSCoW cho từng mã chức năng | **Đã có ngoài repo — cần đưa vào** |
| `non-functional-requirements.md` | Yêu cầu phi chức năng (SRS Chương 6): hiệu năng, bảo mật, múi giờ UTC+7… | **Đã có ngoài repo — cần đưa vào** |
| `business-rules.md` | Quy tắc nghiệp vụ dùng chung (giá, tồn kho, đổi trả, khuyến mãi) | Chưa có |
| `change-log.md` | Lịch sử thay đổi yêu cầu và ảnh hưởng tới `04`/`06` | Chưa có |

> Các tài liệu đánh dấu "đã có ngoài repo" được ghi nhận trong `04-system-design/database/product-purchase/design-notes.md` là đầu vào nhưng chưa nằm trong repository. Đưa chúng vào đây giúp các tài liệu thiết kế trích dẫn được trực tiếp.

## Tiêu chí hoàn thành
- [ ] Mỗi yêu cầu có mã, mức ưu tiên và nguồn khảo sát (`KS-xx`).
- [ ] Ma trận truy vết ở `00-project-management/traceability-matrix.md` có cột Yêu cầu đầy đủ.
- [ ] Khi PR làm lệch đặc tả, cập nhật tài liệu trong thư mục này (theo checklist PR).
