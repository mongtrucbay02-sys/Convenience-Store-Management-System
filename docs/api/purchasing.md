# API Contract – Purchasing (NH)

**Issue:** #50 – T3-10
**Module:** Nhập hàng và nhà cung cấp
**Phiên bản:** Draft v0.1
**Quy ước:** Issue #4 – Quy ước chung viết API

## 1. Bảng tổng hợp endpoint

| Method | Endpoint                                 | Use Case | Quyền   | HTTP | Ghi chú                        |
| ------ | ---------------------------------------- | -------- | ------- | ---- | ------------------------------ |
| GET    | `/api/v1/suppliers`                      | NH-01    | AD, NK  | 200  | Danh sách, tìm kiếm NCC        |
| POST   | `/api/v1/suppliers`                      | NH-02    | AD, NK  | 201  | Thêm NCC                       |
| PUT    | `/api/v1/suppliers/{id}`                 | NH-02    | AD, NK  | 200  | Sửa NCC; có phienBan           |
| POST   | `/api/v1/suppliers/{id}/deactivate`      | NH-03    | AD      | 200  | Ngừng hợp tác, đề xuất         |
| DELETE | `/api/v1/suppliers/{id}`                 | NH-03    | AD      | 204  | Xóa thật khi hợp lệ            |
| POST   | `/api/v1/purchase-orders`                | NH-04    | PENDING | 201  | Tạo phiếu nhập nháp            |
| PUT    | `/api/v1/purchase-orders/{id}`           | NH-05    | PENDING | 200  | Sửa phiếu nhập nháp            |
| POST   | `/api/v1/purchase-orders/{id}/cancel`    | NH-05    | PENDING | 200  | Hủy phiếu nhập nháp            |
| POST   | `/api/v1/purchase-orders/{id}/confirm`   | NH-06    | PENDING | 200  | Xác nhận nhập kho              |
| GET    | `/api/v1/purchase-orders`                | NH-07    | PENDING | 200  | Tra cứu phiếu nhập             |
| GET    | `/api/v1/purchase-orders/{id}`           | NH-08    | PENDING | 200  | Xem chi tiết phiếu             |
| GET    | `/api/v1/suppliers/{id}/purchase-orders` | NH-09    | PENDING | 200  | Lịch sử nhập theo NCC; đề xuất |

Các endpoint chưa có trong bảng tài nguyên Issue #4 phải được Lead xác nhận trước khi chốt.

## 2. Quy ước chung

- Prefix API: `/api/v1`.
- Endpoint tiếng Anh, số nhiều, kebab-case.
- JSON camelCase, tiếng Việt không dấu.
- Danh sách trả `{items, total, page, size}`.
- POST tạo mới trả 201 Created kèm Location.
- PUT trả 200 OK, gửi toàn bộ trường được phép sửa.
- Chuyển trạng thái dùng POST.
- Xóa thật dùng DELETE và trả 204.
- Không sử dụng PATCH.
- Lỗi trả `{code, message, details, traceId}`.
- Cookie phiên HttpOnly; các request ghi dữ liệu gửi X-XSRF-TOKEN.
- Phiếu nhập chỉ cập nhật tồn kho thông qua dịch vụ Kho trong cùng transaction.

## 3. Use Case NH-01 đến NH-09

### NH-01 – Xem danh sách nhà cung cấp

**GET** `/api/v1/suppliers`
**Quyền:** AD, NK
**Ưu tiên:** Must

- Tìm kiếm theo tên hoặc số điện thoại, lọc trạng thái.
- Hiển thị mã, tên, người liên hệ, điện thoại, địa chỉ và trạng thái.
- Trả danh sách phân trang.
- Không có kết quả: HTTP 200 với items rỗng.

### NH-02 – Thêm/sửa nhà cung cấp

**POST** `/api/v1/suppliers`
**PUT** `/api/v1/suppliers/{id}`
**Quyền:** AD, NK
**Ưu tiên:** Must

- Tên và số điện thoại là bắt buộc.
- Số điện thoại không được trùng nhà cung cấp khác.
- Thông tin có thể gồm tên, người liên hệ, email, địa chỉ, mã số thuế và ghi chú.
- PUT phải kiểm soát sửa đồng thời bằng `phienBan`.
- POST trả 201 và Location; PUT trả 200.

### NH-03 – Ngừng hợp tác/xóa nhà cung cấp

**Quyền:** AD
**Ưu tiên:** Could

- Nhà cung cấp đã có phiếu nhập không được xóa cứng.
- Nếu chưa có phiếu nhập, có thể xóa thật.
- Nếu còn phiếu nhập nháp/chờ xử lý, phải kiểm tra trước khi ngừng hợp tác.
- PENDING: Chốt endpoint thay đổi trạng thái.

### NH-04 – Tạo phiếu nhập nháp

**POST** `/api/v1/purchase-orders`

- Tạo phiếu nhập ở trạng thái nháp.
- Có nhà cung cấp và các dòng sản phẩm, số lượng, đơn giá nhập.
- Chưa thay đổi tồn kho khi lưu nháp.
- PENDING: Quyền, DTO, validation và trạng thái chính xác theo SRS.

### NH-05 – Sửa/hủy phiếu nhập nháp

**PUT** `/api/v1/purchase-orders/{id}`
**POST** `/api/v1/purchase-orders/{id}/cancel`

- Chỉ sửa hoặc hủy phiếu khi đang ở trạng thái cho phép.
- Không thay đổi tồn kho.
- PENDING: Trạng thái hợp lệ, phân quyền và thông báo lỗi.

### NH-06 – Xác nhận nhập kho

**POST** `/api/v1/purchase-orders/{id}/confirm`

- Kiểm tra phiếu có thể xác nhận.
- Gọi dịch vụ ghi nhận biến động kho do #1 phụ trách.
- Lưu chứng từ và biến động kho trong cùng transaction.
- Nếu ghi nhận tồn kho thất bại, rollback và giữ nguyên trạng thái phiếu.
- Chặn xác nhận trùng khi gửi request nhiều lần.
- PENDING: Quyền, hợp đồng Input/Output của dịch vụ Kho và mã lỗi nghiệp vụ.

### NH-07 – Tra cứu danh sách phiếu nhập

**GET** `/api/v1/purchase-orders`

- Hỗ trợ tìm kiếm, lọc và phân trang.
- PENDING: Danh sách bộ lọc được SRS cho phép.

### NH-08 – Xem chi tiết/in phiếu nhập

**GET** `/api/v1/purchase-orders/{id}`

- Trả thông tin phiếu nhập và các dòng chi tiết.
- Chứng từ đã xác nhận phải bảo toàn dữ liệu giao dịch.
- Việc in có thể thực hiện từ giao diện dựa trên dữ liệu GET.
- PENDING: Quy định in và dữ liệu thể hiện trên mẫu phiếu.

### NH-09 – Xem lịch sử nhập theo nhà cung cấp

**GET** `/api/v1/suppliers/{id}/purchase-orders`

- Xem lịch sử phiếu nhập của một nhà cung cấp.
- PENDING: Xác nhận URL, quyền và bộ lọc thời gian.

## 4. Các quyết định chờ xác nhận

- PENDING-01: Ma trận quyền NH-04 đến NH-09.
- PENDING-02: DTO phiếu nhập và dòng chi tiết theo ERD hợp nhất.
- PENDING-03: Trạng thái và vòng đời phiếu nhập.
- PENDING-04: Hợp đồng dịch vụ ghi nhận biến động kho của #1.
- PENDING-05: Xử lý giao dịch đồng thời và xác nhận trùng.
- PENDING-06: Phạm vi MVP của NH-03 và các chức năng liên quan.
- PENDING-07: URL lịch sử nhập theo nhà cung cấp.
- PENDING-08: Request/Response JSON và bảng lỗi đầy đủ cho các endpoint MVP.

## 5. Tài liệu tham chiếu

- Issue #4 – Quy ước chung viết API.
- Issue #50 – OpenAPI SP/NH/KHO.
- Issue #65–67 – Đặc tả Nhà cung cấp.
- PR #73 – ERD Sản phẩm/Nhập hàng.
- Tài liệu dịch vụ Kho của #1.

## 6. Request/Response mẫu – Nhà cung cấp và Nhập hàng

Các JSON dưới đây là ví dụ thiết kế, không phải dữ liệu thực tế từ CSDL. Các trường chưa được xác nhận qua ERD/SRS giữ trạng thái Draft.

### 6.1. NH-01 – Danh sách nhà cung cấp

**GET** `/api/v1/suppliers?page=1&size=20&keyword=vinamilk`

**Quyền:** AD, NK

HTTP 200 OK:

```json
{
  "items": [
    {
      "id": 3,
      "ma": "NCC000003",
      "tenNhaCungCap": "Công ty Sữa Việt Nam",
      "nguoiLienHe": "Nguyễn Văn A",
      "soDienThoai": "0900000000",
      "diaChi": "Hà Nội",
      "trangThai": "HOAT_DONG",
      "phienBan": 1
    }
  ],
  "total": 1,
  "page": 1,
  "size": 20
}
```

Không có kết quả: HTTP 200 với `items: []`, `total: 0`.

PENDING: Tên trường, enum và cấu trúc dữ liệu cần đối chiếu ERD hợp nhất.

### 6.2. NH-02 – Thêm nhà cung cấp

**POST** `/api/v1/suppliers`

**Quyền:** AD, NK

Request:

```json
{
  "tenNhaCungCap": "Công ty Sữa Việt Nam",
  "nguoiLienHe": "Nguyễn Văn A",
  "soDienThoai": "0900000000",
  "email": "example@example.com",
  "diaChi": "Hà Nội",
  "maSoThue": null,
  "ghiChu": null
}
```

HTTP 201 Created:

`Location: /api/v1/suppliers/3`

Trả đối tượng nhà cung cấp vừa tạo.

**PUT** `/api/v1/suppliers/3`

Request:

```json
{
  "tenNhaCungCap": "Công ty Sữa Việt Nam",
  "nguoiLienHe": "Nguyễn Văn B",
  "soDienThoai": "0900000000",
  "email": "example@example.com",
  "diaChi": "Hà Nội",
  "maSoThue": null,
  "ghiChu": null,
  "phienBan": 1
}
```

HTTP 200 OK: trả đối tượng đã cập nhật với `phienBan` tăng.

Validation:

- Tên nhà cung cấp và số điện thoại bắt buộc.
- Số điện thoại không được trùng nhà cung cấp khác.
- Email, nếu có, phải đúng định dạng.
- PUT phải kiểm tra `phienBan`.

Lỗi:

- `409 XUNG_DOT_DU_LIEU`: dữ liệu đã được người khác sửa.
- `409 TRUNG_DU_LIEU`: số điện thoại trùng.
- `422 DU_LIEU_KHONG_HOP_LE`: thiếu hoặc sai dữ liệu.

PENDING: Giới hạn độ dài các trường theo ERD/SRS.

### 6.3. NH-04 – Tạo phiếu nhập nháp

**POST** `/api/v1/purchase-orders`

Request minh họa:

```json
{
  "nhaCungCapId": 3,
  "ghiChu": "Nhập hàng đầu tuần",
  "chiTiet": [
    {
      "sanPhamId": 15,
      "soLuong": 20,
      "giaNhap": 6500.0
    }
  ]
}
```

HTTP 201 Created:

`Location: /api/v1/purchase-orders/10`

Response minh họa:

```json
{
  "id": 10,
  "ma": "PN-20261017-001",
  "nhaCungCapId": 3,
  "trangThai": "NHAP",
  "chiTiet": [
    {
      "sanPhamId": 15,
      "soLuong": 20,
      "giaNhap": 6500.0
    }
  ],
  "ghiChu": "Nhập hàng đầu tuần"
}
```

Lưu phiếu nháp không làm thay đổi tồn kho.

PENDING: Tên trường `giaNhap`/`donGiaNhap`, trạng thái chính thức và quyền thực hiện.

### 6.4. NH-05 – Sửa hoặc hủy phiếu nhập nháp

**PUT** `/api/v1/purchase-orders/10`

Request minh họa:

```json
{
  "nhaCungCapId": 3,
  "ghiChu": "Điều chỉnh số lượng nhập",
  "chiTiet": [
    {
      "sanPhamId": 15,
      "soLuong": 25,
      "giaNhap": 6500.0
    }
  ],
  "phienBan": 1
}
```

HTTP 200 OK: trả phiếu nhập sau khi cập nhật.

**POST** `/api/v1/purchase-orders/10/cancel`

HTTP 200 OK: trả phiếu với trạng thái đã hủy.

Không sửa hoặc hủy phiếu đã xác nhận.

PENDING: Xác nhận cơ chế `phienBan` và mã trạng thái theo tài liệu NH.

### 6.5. NH-06 – Xác nhận nhập kho

**POST** `/api/v1/purchase-orders/10/confirm`

Chỉ xác nhận phiếu đủ điều kiện.

Luồng xử lý dự kiến:

1. Kiểm tra quyền và trạng thái phiếu.
2. Kiểm tra các dòng sản phẩm, số lượng, giá nhập.
3. Bắt đầu transaction.
4. Xác nhận chứng từ và gọi dịch vụ Kho của #1.
5. Ghi nhận biến động kho.
6. Commit nếu tất cả thành công; rollback khi có lỗi.

HTTP 200 OK: trả đối tượng phiếu đã được xác nhận.

Trường hợp xác nhận lại phiếu đã xử lý:

HTTP 409 Conflict:

```json
{
  "code": "DA_DUOC_XU_LY",
  "message": "Phiếu nhập đã được xử lý, vui lòng tải lại.",
  "details": null,
  "traceId": "example-trace-id"
}
```

PENDING: Quyền thực hiện, trạng thái cuối và chữ ký chính thức của dịch vụ Kho.

### 6.6. NH-07 – Tra cứu phiếu nhập

**GET** `/api/v1/purchase-orders?page=1&size=20`

Response phải dùng:

```json
{
  "items": [],
  "total": 0,
  "page": 1,
  "size": 20
}
```

PENDING: Bộ lọc theo nhà cung cấp, trạng thái và thời gian.

### 6.7. NH-08 – Chi tiết phiếu nhập

**GET** `/api/v1/purchase-orders/10`

HTTP 200 OK: trả thông tin chứng từ và toàn bộ các dòng chi tiết.

HTTP 404 `KHONG_TIM_THAY` nếu phiếu không tồn tại.

Chức năng in có thể sử dụng dữ liệu API để dựng giao diện in.

### 6.8. NH-09 – Lịch sử nhập theo nhà cung cấp

**GET** `/api/v1/suppliers/3/purchase-orders`

HTTP 200 OK: danh sách phân trang theo quy ước.

PENDING: Đường dẫn này cần đăng ký/xác nhận theo bảng tài nguyên của Issue #4.

## 7. Mã lỗi chung

| HTTP | Error Code           | Ý nghĩa                    |
| ---- | -------------------- | -------------------------- |
| 400  | YEU_CAU_KHONG_HOP_LE | JSON sai cú pháp hoặc kiểu |
| 401  | CHUA_DANG_NHAP       | Chưa đăng nhập             |
| 401  | HET_PHIEN            | Phiên đăng nhập hết hạn    |
| 403  | KHONG_DU_QUYEN       | Vai trò không được phép    |
| 404  | KHONG_TIM_THAY       | Không tìm thấy bản ghi     |
| 409  | TRUNG_DU_LIEU        | Dữ liệu duy nhất bị trùng  |
| 409  | XUNG_DOT_DU_LIEU     | Xung đột phiên bản         |
| 409  | DA_DUOC_XU_LY        | Chứng từ đã được xử lý     |
| 422  | DU_LIEU_KHONG_HOP_LE | Dữ liệu vi phạm nghiệp vụ  |
| 500  | LOI_MAY_CHU          | Lỗi máy chủ                |

Mọi response lỗi sử dụng cấu trúc `{code, message, details, traceId}`.
