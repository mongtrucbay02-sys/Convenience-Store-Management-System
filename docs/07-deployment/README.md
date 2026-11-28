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
| [../db/POSTGRESQL-MIGRATIONS.md](../db/POSTGRESQL-MIGRATIONS.md) | Baseline PostgreSQL-only, private backend role và adoption schema hiện có | Publish guard PASS cục bộ; M0/shared/staging chưa nghiệm thu |
| [`supabase-smoke-ci.md`](supabase-smoke-ci.md) | Workflow kiểm tra kết nối và SELECT qua EF trên Supabase thật, cấu hình GitHub Secret | Supabase thật đọc được 31 bảng + 3 view; đã cấu hình secret, chờ chạy GitHub |
| [`postgresql-publish.md`](postgresql-publish.md) | Publish PostgreSQL, kiểm tra staging, cấu hình backend và rollback | Publish đã chạy cục bộ; chưa có URL staging |
| `environments.md` | Danh sách môi trường, địa chỉ, CSDL (Aiven MySQL `cuahang_codefirst_dev`…) — **không ghi mật khẩu** | Chưa có |
| `deployment-guide.md` | Các bước build, publish ASP.NET Core, áp migration, cấu hình biến môi trường | Chưa có |
| `configuration.md` | Danh sách biến cấu hình (ví dụ `ConnectionStrings__DefaultConnection`) và ý nghĩa | Chưa có |
| `release-notes/` | Ghi chú phát hành theo phiên bản (`vX.Y.Z.md`) | Chưa có |
| `rollback.md` | Cách quay lại phiên bản trước khi triển khai lỗi | Chưa có |

> Cấu hình kết nối PostgreSQL/Supabase: xem `CONTRIBUTING.md` mục 4.2 và [`05-implementation/setup/postgresql-runtime-setup.md`](../05-implementation/setup/postgresql-runtime-setup.md). Hướng dẫn MySQL cũ đã lưu trữ ở `docs/archive/mysql-runtime/`.

## Tiêu chí hoàn thành
- [ ] Một thành viên chưa tham gia triển khai làm theo `deployment-guide.md` và chạy được hệ thống.
- [ ] Mỗi lần phát hành có release notes.
