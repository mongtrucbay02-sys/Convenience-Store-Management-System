# 00 — Quản lý dự án

> Khu vực **xuyên suốt**: không thuộc một giai đoạn cụ thể mà điều phối toàn bộ trục `01` → `08`.

## Mục đích
Lưu mọi tài liệu giúp nhóm biết **ai làm gì, khi nào, theo quy ước nào**, và giúp người ngoài (giảng viên, người review) theo dõi tiến độ.

## Nội dung cần có

| Tài liệu | Mô tả | Trạng thái |
|---|---|---|
| `project-charter.md` | Mục tiêu, phạm vi tổng, thành viên và vai trò (#1…#6), kênh liên lạc | Chưa có |
| `plan.md` | Kế hoạch theo tuần/sprint, mốc bàn giao từng giai đoạn `01`–`08` | Chưa có |
| `task-assignment.md` | Bảng phân công task (mã T…-…), người dự phòng; liên kết Issue GitHub | Chưa có |
| `conventions.md` | Quy ước làm việc: nhánh Git, PR, review, đặt tên — có thể dẫn tới Issue #4 (quy ước API) | Chưa có |
| `risk-register.md` | Danh sách rủi ro, mức độ, phương án xử lý | Chưa có |
| [`traceability-matrix.md`](traceability-matrix.md) | Ma trận truy vết yêu cầu → thiết kế → code → test → nghiệm thu | Khung sẵn |
| `meetings/` | Biên bản họp, đặt tên `YYYY-MM-DD-<chu-de>.md` | Chưa có |
| `status-reports/` | Báo cáo tiến độ định kỳ | Chưa có |
| [`templates/`](templates/) | Mẫu tài liệu dùng chung | Có sẵn |

## Liên kết với các khu vực khác
- Mốc của từng giai đoạn trong `plan.md` tương ứng với tiêu chí hoàn thành trong README của `01`–`08`.
- `10-final-report` lấy phần "Quản lý dự án" từ thư mục này.
- Mẫu Issue và PR nằm ở `.github/` (ISSUE_TEMPLATE, pull_request_template).

## Tiêu chí cập nhật
- [ ] Kế hoạch được cập nhật khi một giai đoạn trễ mốc.
- [ ] Mỗi buổi họp có biên bản trong `meetings/`.
- [ ] Ma trận truy vết được cập nhật khi có yêu cầu mới hoặc thay đổi.
