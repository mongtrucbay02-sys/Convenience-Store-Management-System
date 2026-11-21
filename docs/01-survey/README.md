# 01 — Khảo sát

> **Trục chính:** **01 Khảo sát** → 02 Định nghĩa sản phẩm → 03 Yêu cầu → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu

## Mục đích
Ghi lại cửa hàng tiện lợi **đang vận hành thế nào** trước khi có hệ thống, và **vấn đề/nhu cầu** thực tế. Đây là căn cứ gốc cho mọi yêu cầu ở `03`.

## Đầu vào → Đầu ra
- **Đầu vào:** đề bài môn học, buổi phỏng vấn/quan sát cửa hàng, tài liệu tham khảo các hệ thống tương tự.
- **Đầu ra cho `02`:** danh sách vấn đề và nhu cầu đã đánh mã `KS-xx`.

## Nội dung cần có

| Tài liệu | Mô tả | Trạng thái |
|---|---|---|
| `survey-plan.md` | Đối tượng, phương pháp (phỏng vấn, quan sát, bảng hỏi), lịch khảo sát | Chưa có |
| `interviews/` | Ghi chép từng buổi phỏng vấn (`YYYY-MM-DD-<doi-tuong>.md`) | Chưa có |
| `as-is-processes.md` | Quy trình hiện trạng: bán hàng, nhập hàng, kiểm kho, đổi trả, khuyến mãi, báo cáo | Chưa có |
| `competitor-analysis.md` | So sánh phần mềm quản lý cửa hàng hiện có trên thị trường | Chưa có |
| `survey-findings.md` | Tổng hợp phát hiện, mỗi phát hiện một mã `KS-xx` | Chưa có |

## Tiêu chí hoàn thành
- [ ] Mỗi nhóm nghiệp vụ (SP, NH, KHO, KH, KM, POS, DT, BC, AUTH) có ít nhất một phát hiện khảo sát.
- [ ] Phát hiện được đánh mã `KS-xx` để `03` trích dẫn.
- [ ] Được trưởng nhóm duyệt.
