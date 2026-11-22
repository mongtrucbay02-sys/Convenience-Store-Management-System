# ADR-002: Giao diện dùng ASP.NET Core MVC (Razor view) thay cho React + Vite + Ant Design

| | |
|---|---|
| **Dự án** | Hệ thống quản lý cửa hàng tiện lợi – Nhóm 7, học phần INFO2005 |
| **Trạng thái** | **Đề xuất** – ghi nhận hiện trạng, chờ trưởng nhóm và cả nhóm xác nhận |
| **Ngày** | 22/11/2026 |
| **Người soạn** | #1 Phạm Gia Thăng |
| **Thay thế** | ADR-001 mục 2 (dòng "Giao diện", "Biểu đồ") và mục 3.4 |
| **Liên quan** | `docs/srs/SRS_v1.2_danh-sach-thay-doi.md` mục C1 và mục 3 (vấn đề mở 3); issue #11; PR #119 |

## 1. Bối cảnh

ADR-001 (15/10/2026) chọn **React + Vite + Ant Design** cho giao diện, bản build React được ASP.NET Core phục vụ như tệp tĩnh. SRS v1.0.3 mục 2.4 và 2.5 ghi theo lựa chọn này và đóng vấn đề mở số 3.

Hiện trạng trên nhánh `develop` ngày 22/11/2026:

- Không có project React: không có `package.json`, cấu hình Vite hay tệp `.tsx`/`.jsx`.
- Giao diện là **Razor view của ASP.NET Core MVC**: 65 tệp `.cshtml` trong `src/ConvenienceStore.Web`, dùng Bootstrap và jQuery trong `wwwroot/lib`, CSS riêng trong `wwwroot/css`.
- Hai trang HTML tĩnh `wwwroot/login.html` và `wwwroot/doi-mat-khau.html` gọi REST `/api/v1/auth/...` bằng JavaScript (`wwwroot/js/login.js`, `auth-rieng.js`).
- Khung giao diện Razor do #3 dựng (issue #11, 17/10/2026), gộp qua PR #119 (27/10/2026). Các module POS, DT, SP, NH, KHO, KH, KM, BC đều được làm trên khung này.

## 2. Quyết định

Ghi nhận **ASP.NET Core MVC với Razor view + Bootstrap + jQuery** là công nghệ giao diện của sản phẩm. Các trang đăng nhập và đổi mật khẩu giữ dạng HTML tĩnh gọi REST `/api/v1`. Không làm phần giao diện React trong phạm vi học phần.

## 3. Lý do (theo dữ kiện ghi nhận được)

1. Khung giao diện Razor đã chạy và được nhóm dùng trước khi có dòng code React nào.
2. Toàn bộ chức năng đã lập trình đều dựa trên Razor view; chuyển sang React lúc này là làm lại toàn bộ phần giao diện.
3. Giao diện và API vẫn cùng domain, nên cơ chế xác thực bằng cookie HttpOnly và chống CSRF của ADR-001 mục 3.5 vẫn giữ nguyên.

Tài liệu không ghi nhận được buổi thảo luận hay quyết định chính thức nào của nhóm về việc đổi công nghệ. Mục này cần cả nhóm xác nhận.

## 4. Hệ quả

- SRS mục 2.4, 2.5 và vấn đề mở 3 phải sửa theo mục C1 của danh sách thay đổi SRS v1.2.
- ADR-001 mục 3.4, sơ đồ thành phần (nút "React + Ant Design") và mục 3.10 (build giao diện React, Docker image gồm bản build React) không còn đúng.
- Biểu đồ báo cáo không dùng Recharts. `wwwroot/lib` chỉ có Bootstrap, jQuery, jQuery Validation; repo không có thư viện biểu đồ bên ngoài.
- Phần kiểm quyền vẫn đặt ở máy chủ (BR-AUTH-03); Razor view chỉ ẩn menu theo vai trò.

## 5. Xác nhận

| Thành viên | Xác nhận |
|---|---|
| #5 Nguyễn Tiến Trung (trưởng nhóm từ 22/11/2026) | |
| #2 Phan Hải Yến | |
| #3 Nguyễn Thị Thu Hoài | |
| #4 Tạ Quang Huy | |
| #6 Cao Huyền Trang | |
