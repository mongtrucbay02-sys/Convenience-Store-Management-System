# 05 — Hiện thực

> **Trục chính:** 01 Khảo sát → 02 Định nghĩa sản phẩm → 03 Yêu cầu → 04 Thiết kế → **05 Hiện thực** → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu

## Mục đích
Tài liệu đi kèm mã nguồn: **quy ước viết code, cách dựng môi trường phát triển, ghi chú hiện thực từng module**. Bản thân mã nguồn nằm ở `src/`; thư mục này giải thích nó.

## Đầu vào → Đầu ra
- **Đầu vào:** thiết kế trong `04-system-design/`.
- **Đầu ra cho `06`:** mã nguồn đã build được, ghi chú module để người kiểm thử biết phạm vi đã hiện thực.

## Tài liệu hiện có

| Tài liệu | Mô tả |
|---|---|
| [`backend-conventions.md`](backend-conventions.md) | Quy ước phát triển Backend (công nghệ, kiến trúc, đặt tên, xử lý lỗi…) |
| [`setup/mysql-reports-setup.md`](setup/mysql-reports-setup.md) | Cấu hình chuỗi kết nối MySQL (Aiven) để chạy Báo cáo và Khuyến mãi — trước đây là `MYSQL-REPORTS-SETUP.md` ở gốc repo |
| [`attachments/Login.zip`](attachments/Login.zip) | Gói mã nguồn chức năng Đăng nhập (Views/Account, `_AdminLayout`, `auth-login-setup.md`) — chưa gộp vào `src/` |

## Cấu trúc đề xuất

```
05-implementation/
├── backend-conventions.md
├── frontend-conventions.md     (chưa có)
├── setup/                      Hướng dẫn dựng môi trường dev, cấu hình CSDL
├── modules/                    Ghi chú hiện thực theo module: products.md, promotions.md, reports.md…
└── attachments/                Gói mã/tài liệu đính kèm chưa gộp
```

## Việc nên làm tiếp
- Gộp nội dung `attachments/Login.zip` vào `src/` và chuyển `auth-login-setup.md` trong đó sang `setup/` hoặc `modules/auth.md`.
- Viết `modules/<module>.md` cho các module đã có code: Products, Promotions, Reports, POS, Returns.

## Tiêu chí hoàn thành
- [ ] Người mới clone repo dựng được môi trường chỉ bằng tài liệu trong `setup/`.
- [ ] Không có mật khẩu/chuỗi kết nối thật trong tài liệu hoặc `appsettings*.json`.
