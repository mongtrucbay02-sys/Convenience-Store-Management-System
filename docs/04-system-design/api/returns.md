# T3-14 — Hợp đồng API DT (Đổi trả)

> **Trạng thái:** Đề xuất để review; chưa phải API đã triển khai. Căn cứ: Issue #10 (phạm vi), Issue #4 (quy ước chung, phiên bản 1.0 ngày 16/10/2026). Đường dẫn/DTO chưa có trong Issue #4 được đánh dấu **ĐỀ XUẤT** và cần người phụ trách duyệt. Không tạo API AUTH thay cho #1 Thăng (Issue #5); module KH/KM do #5 Trung phụ trách.

## Quy ước áp dụng

- REST + JSON UTF-8; prefix `/api/v1`; URL tiếng Anh số nhiều/kebab-case; không dùng PATCH.
- JSON camelCase tiếng Việt không dấu; `id` số nguyên, tiền VND số nguyên, thời điểm ISO 8601 `+07:00`, enum `CHU_HOA_KHONG_DAU`.
- Bản ghi đơn trả thẳng object; danh sách `{ "items": [], "total": 0, "page": 1, "size": 20 }`.
- Thành công: GET/PUT/POST chuyển trạng thái 200; POST tạo 201 + Location; DELETE xóa thật 204.
- Lỗi: `{ "code": "...", "message": "...", "details": null, "traceId": "..." }`; 400 cú pháp, 401 chưa đăng nhập/hết phiên, 403 không quyền, 404 không tìm thấy, 409 xung đột, 422 sai nghiệp vụ, 500 lỗi máy chủ.
- Cookie phiên `HttpOnly; Secure; SameSite=Strict`; hết hạn sau 30 phút không hoạt động; không dùng localStorage cho token. POST/PUT/DELETE bắt buộc `X-XSRF-TOKEN` từ cookie `XSRF-TOKEN`.
- Quyền theo ma trận SRS và phản hồi review: `TN` thu ngân, `AD` quản lý, `NK` kho. POS-01..09 chỉ TN (`BAN_HANG`); POS-10/11 TN xem của mình (`HOA_DON_XEM_CUA_MINH`), AD xem tất cả (`HOA_DON_XEM_TAT_CA`); POS-12 chỉ AD (`HOA_DON_HUY`). DT-01..05 TN (`DOI_TRA`), DT-06/08 AD (`DOI_TRA_DUYET_CAU_HINH`), DT-07 TN/AD (đề xuất của #1).
- Dữ liệu `phienBan` cần thiết khi bản ghi có nhiều người cùng sửa; nếu sai trả `409 XUNG_DOT_DU_LIEU`. Không cho tạo chứng từ trùng do gửi lại.
- Mọi thay đổi tồn kho phải gọi dịch vụ biến động kho trong **cùng giao tác CSDL** với lưu chứng từ; lỗi phải rollback toàn bộ.

## 1. Phạm vi

Issue #10 giao DT-01 đến DT-08, nhưng Issue #4 đánh dấu `/returns` **Sau MVP**. Vì vậy tài liệu này ghi danh sách endpoint và quy tắc nghiệp vụ để thống nhất trước; chưa coi là cam kết triển khai tuần 3. **ĐỀ XUẤT:** tài liệu đặt `docs/api/returns.md` (cần #1 xác nhận tên file vì Issue #4 chưa liệt kê `returns.md`).

## 2. Danh sách endpoint (đề xuất, sau MVP)

| UC | HTTP + đường dẫn (bỏ `/api/v1`) | Quyền đề xuất | Thành công | Ghi chú |
|---|---|---|---|---|
| DT-01 | `GET /invoices?keyword=...` | TN (`DOI_TRA`) | 200 | Tra hóa đơn gốc bằng mã; tái sử dụng POS |
| DT-01 | `GET /invoices/{id}` | TN (`DOI_TRA`) | 200 | Lấy dòng và số lượng đã trả |
| DT-02 | `POST /returns` | TN (`DOI_TRA`) | 201 | Tạo phiếu trả; Location `/api/v1/returns/{id}` |
| DT-03 | `POST /returns/{id}/exchange` | TN (`DOI_TRA`) | 200 | Đổi hàng, liên kết hóa đơn mới |
| DT-04 | `POST /returns/{id}/refund` | TN (`DOI_TRA`) | 200 | Ghi nhận hoàn tiền mặt |
| DT-05 | `POST /returns/{id}/process-stock` | TN (`DOI_TRA`) | 200 | Nhập lại hoặc xuất hủy qua dịch vụ kho |
| DT-06 | `POST /returns/{id}/approve` | AD (`DOI_TRA_DUYET_CAU_HINH`) | 200 | Duyệt ngoại lệ; cần thêm `/reject` nếu SRS yêu cầu |
| DT-07 | `GET /returns` | TN/AD (`DOI_TRA` / `DOI_TRA_DUYET_CAU_HINH`) | 200 | `{items,total,page,size}` |
| DT-07 | `GET /returns/{id}` | TN/AD (`DOI_TRA` / `DOI_TRA_DUYET_CAU_HINH`) | 200 | Chi tiết phiếu |
| DT-07 | `GET /returns/{id}/print` | TN/AD (`DOI_TRA` / `DOI_TRA_DUYET_CAU_HINH`) | 200 | **ĐỀ XUẤT** dữ liệu in |
| DT-08 | `GET /return-policies` | AD (`DOI_TRA_DUYET_CAU_HINH`) | 200 | Chính sách đổi trả, tên tài nguyên mới cần #1 duyệt |
| DT-08 | `PUT /return-policies/{id}` | AD (`DOI_TRA_DUYET_CAU_HINH`) | 200 | Cập nhật kèm `phienBan` |

## 3. Dữ liệu ví dụ (đề xuất để #1/#6 xác nhận)

### DT-02 `POST /api/v1/returns`

```json
{
  "hoaDonGocId": 90,
  "chiTiet": [{ "hoaDonChiTietId": 301, "soLuong": 1, "lyDo": "Hàng lỗi" }],
  "ghiChu": null
}
```

`201 Created`, Location `/api/v1/returns/12`:

```json
{
  "id": 12, "ma": "TH-20261016-001", "hoaDonGocId": 90,
  "chiTiet": [{ "hoaDonChiTietId": 301, "soLuong": 1, "soTienDuKienHoan": 8000 }],
  "tongTienHoan": 8000, "trangThai": "MOI_TAO", "phienBan": 1,
  "thoiDiemTao": "2026-10-16T09:00:00+07:00"
}
```

`MOI_TAO` là enum **đề xuất**, chưa xác nhận với ERD. Máy chủ kiểm tra hóa đơn gốc, thời hạn, số lượng trả lũy kế và giá trị hoàn theo quy tắc đã chốt. Lỗi: 404 `KHONG_TIM_THAY`; 409 `DA_DUOC_XU_LY`; 422 `DU_LIEU_KHONG_HOP_LE`. Nếu cần mã lỗi riêng, phải đăng ký Issue #4 mục 8.4 trước.

### DT-03 `POST /api/v1/returns/{id}/exchange`

```json
{ "sanPhamMoi": [{ "sanPhamId": 22, "soLuong": 1 }], "phienBan": 1 }
```

Máy chủ kiểm tra giá trị hàng mới không thấp hơn hàng trả theo quy tắc DT-03; tạo hóa đơn mới và biến động kho trong cùng giao tác. Cần chốt cách ghi nhận phần chênh lệch tiền.

### DT-04 `POST /api/v1/returns/{id}/refund`

```json
{ "hinhThucHoan": "TIEN_MAT", "phienBan": 2 }
```

Số tiền hoàn do máy chủ tính; chống hoàn hai lần (`409 DA_DUOC_XU_LY`); lưu giao dịch tiền mặt.

### DT-05 `POST /api/v1/returns/{id}/process-stock`

```json
{ "xuLy": "NHAP_LAI", "phienBan": 3 }
```

`NHAP_LAI`/`XUAT_HUY` là giá trị **đề xuất** cần chốt. Mọi thay đổi tồn qua dịch vụ kho, cùng transaction với phiếu trả; không ghi trực tiếp tồn kho.

### DT-06 `POST /api/v1/returns/{id}/approve`

```json
{ "lyDo": "Duyệt ngoại lệ quá hạn", "phienBan": 4 }
```

Chỉ AD; kiểm tra trạng thái `CHO_DUYET` (nếu enum này được ERD DT chốt). Trả `409 DA_DUOC_XU_LY` nếu phiếu đã duyệt/từ chối.

### DT-08 `PUT /api/v1/return-policies/{id}`

```json
{ "soNgayChoPhep": 7, "nguongCanDuyet": 500000, "phienBan": 1 }
```

`200 OK`: trả chính sách đã cập nhật, `phienBan` mới. Các giá trị 7 ngày và 500.000 VND là mặc định theo yêu cầu DT đã trao đổi, cần đối chiếu bản SRS/ERD chính thức trước khi triển khai.

## 4. Quy tắc nghiệp vụ và phối hợp

- Bắt buộc hóa đơn gốc; số lượng trả không vượt `soLuongDaMua - soLuongDaTraTruoc`.
- Áp dụng hạn đổi trả mặc định 7 ngày; ngoại lệ và ngưỡng phê duyệt 500.000 VND cần xác nhận cách tính với #1/#6.
- Không hoàn tiền hoặc nhập kho hai lần khi mạng gửi lại; dùng kiểm tra trạng thái + `phienBan`.
- Khi hoàn tiền, đổi hàng hoặc nhập lại tồn, ghi chứng từ và biến động liên quan trong cùng transaction.
- DT tái sử dụng `/invoices` của POS; không sao chép dữ liệu khách hàng hay viết API AUTH riêng.
- Lỗi, cookie phiên, CSRF và phân quyền giống Issue #4; không đưa token vào request body.

## 5. Cần trưởng nhóm xác nhận

1. DT-01 đến DT-08 có cần chi tiết ngay tuần 3 hay chỉ bảng do `/returns` là Sau MVP?
2. Tên bảng/enum DT theo ERD đã hợp nhất; ai có quyền duyệt ngoại lệ?
3. Thời hạn 7 ngày tính từ ngày mua hay thời điểm thanh toán; ngưỡng 500.000 áp dụng theo phiếu hay hóa đơn?
4. Cách xử lý hoàn tiền khi hóa đơn đã giảm giá, tích điểm, hoặc đã trả một phần.
5. Tên tài nguyên `/return-policies` và endpoint in có cần bổ sung vào Issue #4 không?

> Theo phản hồi #1 Thăng: toàn bộ DT thuộc **Sau MVP**; chỉ cần bảng endpoint, DTO chi tiết chưa bắt buộc. DT-07 TN/AD là đề xuất phân quyền của #1, chờ thống nhất ma trận.
