# Phối hợp AUTH – POS-03 – POS-04 (cập nhật theo review)

- **AUTH:** #1 Thăng, Issue #5; cookie `cs_session`, header `X-XSRF-TOKEN`, vai trò/quyền từ phiên.
- **KH:** #5 Trung, T3-25; POS-03 tái sử dụng `GET /api/v1/customers`, chờ Trung xác nhận `keyword`, trường trả về và quy tắc khách chưa đăng ký.
- **KM:** #5 Trung, tầng 2; POS-04 không triển khai MVP, chưa chốt nguồn chính sách.
- **POS-03:** chỉ TN / `BAN_HANG`, `PUT /api/v1/carts/{id}/customer` với `khachHangId` và `phienBan`. POS kiểm tra ID phía máy chủ; không tự định nghĩa API AUTH hoặc KH.
- **POS-04:** chỉ TN / `BAN_HANG`, tầng 2; server quyết định tiền giảm.
- **Các điểm mở AUTH-01/06/09:** là phần AUTH do #1 Thăng quyết định; ý kiến POS không thay đổi đặc tả AUTH.

## Nội dung gốc và lịch sử phối hợp

# T3-14 — Thống nhất POS-03, POS-04 với AUTH/KH/KM (#5)

**Trạng thái: ĐỀ XUẤT CHỜ REVIEW.** Căn cứ Issue #4 (quy ước API), Issue #10 (POS/DT), các nhận xét AUTH-01/06/09 được cung cấp. **Không tự sửa API AUTH hoặc ERD** vì thuộc người phụ trách khác.

## 1. Phân định quyền sở hữu

| Chức năng | Chủ sở hữu | API / trách nhiệm |
|---|---|---|
| Đăng nhập, phiên, quyền | AUTH của #1 Thăng (Issue #5) / theo phân công nhóm | `POST /api/v1/auth/login`, cookie phiên 30 phút; `GET /api/v1/me`; xử lý `401`, `403` |
| Tra cứu khách hàng | KH #5 (theo bảng tài nguyên Issue #4) | `GET /api/v1/customers?keyword=...` **cần #5 xác nhận hỗ trợ SĐT** |
| Gắn khách vào đơn | POS #3 | `PUT /api/v1/carts/{id}/customer` nhận `khachHangId`, `phienBan` |
| Áp dụng KM cho giỏ | POS #3 | `POST /api/v1/carts/{id}/apply-promotions` nhận `phienBan`, server tự tính |
| Nguồn chính sách KM | Chưa chốt | `/promotions` là tầng 2 theo Issue #4; **không giả định #5 đã có API KM** |

**Lưu ý quan trọng:** `soDienThoai` của **tài khoản nhân viên AUTH-06** và `soDienThoai` của **khách hàng POS-03** là hai nghiệp vụ/bảng khác nhau. Không suy diễn việc số điện thoại tài khoản là bắt buộc thì số điện thoại khách hàng cũng bắt buộc.

## 2. Hợp đồng POS-03 — gắn khách hàng

1. Thu ngân đăng nhập một lần; browser tự gửi cookie phiên trong các request cùng domain; không gửi Bearer token, không lưu token trong localStorage.
2. Frontend tìm khách: `GET /api/v1/customers?keyword=0901234567&page=1&size=20`. #5 xác nhận tìm được theo số điện thoại, và DTO tối thiểu `{id, tenKhachHang, soDienThoai, trangThai}` (tên trường cần đối chiếu ERD).
3. Thu ngân chọn khách; frontend gửi `PUT /api/v1/carts/21/customer`, header `X-XSRF-TOKEN`, body `{ "khachHangId": 8, "phienBan": 3 }`. Bỏ khách: `khachHangId: null`.
4. Backend POS kiểm tra phiên, quyền TN/AD, giỏ thuộc phiên/thu ngân được phép thao tác, `phienBan`, khách tồn tại/hợp lệ; gắn khách và tính lại ưu đãi, trả giỏ mới `200`.
5. Lỗi: `401 HET_PHIEN` -> login; `403 PHAI_DOI_MAT_KHAU` -> đổi mật khẩu; `403 KHONG_DU_QUYEN` -> chặn; `404 KHONG_TIM_THAY` -> khách/giỏ không tồn tại; `409 XUNG_DOT_DU_LIEU` -> tải lại giỏ; `422 DU_LIEU_KHONG_HOP_LE` -> hiển thị lỗi trường.

**Cần #5 xác nhận:** tên trường DTO khách, lọc theo điện thoại, trạng thái hợp lệ, khách không có SĐT, có cho khách vãng lai không, và cập nhật điểm tại POS-07.

## 3. Hợp đồng POS-04 — áp dụng khuyến mãi

1. Frontend gửi `POST /api/v1/carts/21/apply-promotions`, header `X-XSRF-TOKEN`, body `{ "phienBan": 4 }`; **không** gửi số tiền giảm tự tính.
2. Backend POS lấy giỏ/khách/chính sách KM từ nguồn được thống nhất, kiểm tra điều kiện và tự tính. Mỗi dòng không giảm quá thành tiền dòng, tổng không âm.
3. `200` trả giỏ đã cập nhật gồm `chiTiet`, `tongTienHang`, `tienGiam`, `thanhTien`, `khuyenMaiApDung`, `phienBan`.
4. Khi thay đổi khách, hàng hoặc số lượng và khi checkout, server phải tính lại KM theo quy tắc được chốt. Nếu không có KM hợp lệ: `khuyenMaiApDung: []`, `tienGiam: 0`.
5. Lỗi `409 XUNG_DOT_DU_LIEU`, `409 DA_DUOC_XU_LY` nếu giỏ đã hoàn tất, `422 DU_LIEU_KHONG_HOP_LE` nếu request không hợp lệ.

**Cần #1/#5 chốt:** ai sở hữu bảng/chính sách KM; có cộng dồn không; cách chọn KM tốt nhất và hòa điểm; tính theo từng dòng hay cả hóa đơn; thời điểm tính lại; API/service dùng để đọc KM. `/promotions` đang được Issue #4 xếp tầng 2.

## 4. Ba vấn đề AUTH được nêu trong review (không thuộc quyền tự chốt của POS)

| Vấn đề | Điều đang mâu thuẫn / chưa chốt | Đề xuất để nhóm duyệt | Tác động đến POS |
|---|---|---|---|
| AUTH-06 số điện thoại tài khoản | Đặc tả ngầm hiểu bắt buộc; ERD/YAML cho phép null | **Ưu tiên giữ nullable theo ERD/YAML**, chỉnh AUTH-06 nói rõ `soDienThoai` không bắt buộc; nếu nghiệp vụ bắt buộc thì sửa ERD và YAML ngược lại | POS-03 dùng khách hàng, không phụ thuộc trường SĐT của tài khoản |
| AUTH-09 đặt lại mật khẩu khi khóa tạm | E1 chỉ nêu Admin khóa | **Đề xuất** reset thành công thì xóa khóa tạm do sai mật khẩu, đưa bộ đếm sai về 0; không tự gỡ khóa do Admin | Thu ngân đăng nhập lại được sau khi Admin reset, không cần đợi khóa tạm; phải có kiểm soát quyền và audit |
| AUTH-01 thứ tự kiểm tra khóa tạm | YAML kiểm tra khóa tạm trước mật khẩu, đặc tả bước 4–5 khác | **Đề xuất** kiểm tra tài khoản/khóa tạm trước xác thực mật khẩu; trả lỗi công khai không làm lộ trạng thái tài khoản theo quyết định AUTH | POS chỉ dựa mã lỗi đăng nhập/phiên được chốt; không tự xử lý khóa tài khoản |

Ba đề xuất này **chưa phải quyết định chính thức**. Người phụ trách AUTH/BA phải thống nhất rồi sửa SRS, đặc tả AUTH, ERD và YAML tương ứng trong PR của họ.

## 5. Hành vi frontend thống nhất với AUTH

- Cookie phiên: `HttpOnly; Secure; SameSite=Strict`, 30 phút không hoạt động; browser tự gửi cookie cùng domain.
- `POST/PUT/DELETE` gửi header `X-XSRF-TOKEN` lấy từ cookie `XSRF-TOKEN` (khác cookie phiên HttpOnly).
- `401 HET_PHIEN`: chuyển login và giữ return URL; `403 PHAI_DOI_MAT_KHAU`: trang đổi mật khẩu; `403 KHONG_DU_QUYEN`: thông báo không đủ quyền.
- Không tự tính giá cuối cùng, tồn kho, khuyến mãi ở frontend; server quyết định.

## 6. Checklist xác nhận

- [ ] #5 xác nhận contract GET /customers và DTO khách cho POS-03.
- [ ] #1/#5 xác nhận nguồn chính sách và cách tính KM cho POS-04.
- [ ] #1 xác nhận lưu giỏ server (`/carts`) và enum trạng thái.
- [ ] AUTH owner chốt ba điểm AUTH-01/06/09, đồng bộ SRS/ERD/YAML.
- [ ] #1 xác nhận quyền AD/TN theo ma trận SRS, DT là sau MVP.
- [ ] Review file `docs/api/sales.md`, `docs/api/returns.md`, YAML; tạo PR base `develop`.
