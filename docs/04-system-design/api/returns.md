# T3-14 — Hợp đồng API DT (Đổi trả)

> **Trạng thái:** DT-01 được triển khai trên nhánh `feature/dt-01` cho issue #148 theo bản rà T6-02 ngày 02/11/2026; các use case DT-02 trở đi bên dưới vẫn là đề xuất, chưa được triển khai trong thay đổi này. Hợp đồng DT-01: [returns-lookup.openapi.yaml](returns-lookup.openapi.yaml).

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
| DT-01 | `GET /returns/invoice-lookup?ma=...` | TN (`DOI_TRA`) | 200 | Đúng mã; gồm dòng hàng, số lượng còn trả và điều kiện |
| DT-01 | `GET /returns/invoice-lookup?soDienThoai=...&page=1&size=20` | TN (`DOI_TRA`) | 200 | Hóa đơn của mọi thu ngân, khớp số điện thoại và còn trong hạn |
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
- DT-01 dùng endpoint riêng `/returns/invoice-lookup` theo BR-DT-08; không nới quyền xem `/invoices` của POS-10/11. Không sao chép dữ liệu khách hàng hay viết API AUTH riêng.
- Lỗi, cookie phiên, CSRF và phân quyền giống Issue #4; không đưa token vào request body.

## 5. Đối chiếu bản rà T6-02 ngày 02/11/2026

1. DT-01 triển khai tuần 6 theo issue #148. DT-02 trở đi là bước tiếp theo, không tạo phiếu hoặc giả lập duyệt trong DT-01.
2. Chính sách: dòng có `hieu_luc_tu` gần nhất không sau thời điểm tra cứu; hòa thời điểm thì lấy `id` lớn hơn. Không có chính sách hiệu lực trả 409, không tự giả định cấu hình.
3. Hạn: hết ngày mua cộng N ngày lịch Việt Nam. Mua 10/11 lúc 21:00 với N=7 thì hết hạn sau ngày 17/11. Tra bằng mã vẫn hiện hóa đơn quá hạn để đánh dấu ngoại lệ; tra bằng điện thoại chỉ hiện hóa đơn còn trong hạn.
4. Số lượng đã trả/đang xử lý gồm phiếu `CHO_DUYET`, `DA_DUYET`, `HOAN_TAT`; bỏ `BI_TU_CHOI`. Giá thực trả đơn vị là `ct_hoa_don.don_gia - giam_don_vi`.
5. Cờ không đổi trả lấy từ danh mục tại lúc tra cứu. Quá hạn hoặc danh mục cấm là ngoại lệ cần AD duyệt ở DT-06. Hóa đơn đã hủy và dòng hết số lượng không thể bỏ qua bằng cờ ngoại lệ.
6. DT-01 chỉ đọc dữ liệu, không giữ chỗ hoặc ghi biến động kho/điểm. DT-02/03 phải kiểm tra lại quyền, trạng thái, chính sách và số lượng trong giao tác trước khi tạo phiếu.
7. Số điện thoại của khách được che trong kết quả trả cho TN theo T6-02 KH3. Mã và điện thoại được so khớp chính xác bằng tham số SQL.

> Phạm vi Sau MVP nêu trong tài liệu tuần 3 không thay thế kế hoạch tuần 6 của DT-01. Các quyết định T6-02 và mục C6/B7 trong danh sách thay đổi SRS v1.2 được dùng làm căn cứ; #1 tiếp tục chốt tài liệu SRS chính thức qua review.
