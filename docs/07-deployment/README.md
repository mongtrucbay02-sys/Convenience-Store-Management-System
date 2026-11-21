# 07 — Triển khai

> **Trục chính:** 01 Khảo sát → 02 Định nghĩa sản phẩm → 03 Yêu cầu → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → **07 Triển khai** → 08 Nghiệm thu

## Mục đích
Hướng dẫn **đưa hệ thống lên môi trường chạy** (demo/staging/production) và ghi nhận từng lần phát hành.

## Đầu vào → Đầu ra
- **Đầu vào:** bản build đã qua kiểm thử (`06`), cấu hình CSDL (`05-implementation/setup/`).
- **Đầu ra cho `08`:** hệ thống đang chạy, địa chỉ truy cập, tài khoản demo để nghiệm thu.

## Nội dung cần có

| Tài liệu | Mô tả | Trạng thái |
|---|---|---|
| `environments.md` | Danh sách môi trường, địa chỉ, CSDL (Aiven MySQL `cuahang_codefirst_dev`…) — **không ghi mật khẩu** | Chưa có |
| `deployment-guide.md` | Các bước build, publish ASP.NET Core, áp migration, cấu hình biến môi trường | Chưa có |
| `configuration.md` | Danh sách biến cấu hình (ví dụ `ConnectionStrings__DefaultConnection`) và ý nghĩa | Chưa có |
| `release-notes/` | Ghi chú phát hành theo phiên bản (`vX.Y.Z.md`) | Chưa có |
| `rollback.md` | Cách quay lại phiên bản trước khi triển khai lỗi | Chưa có |

> Cấu hình kết nối MySQL hiện được mô tả ở [`05-implementation/setup/mysql-reports-setup.md`](../05-implementation/setup/mysql-reports-setup.md); `configuration.md` nên dẫn tới đó thay vì chép lại.

## Tiêu chí hoàn thành
- [ ] Một thành viên chưa tham gia triển khai làm theo `deployment-guide.md` và chạy được hệ thống.
- [ ] Mỗi lần phát hành có release notes.
