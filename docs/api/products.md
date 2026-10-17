# API Contract – Products (SP)

**Issue:** #50 – T3-10
**Module:** Sản phẩm và danh mục
**Phiên bản:** Draft v0.1
**Quy ước:** Issue #4 – Quy ước chung viết API

## 1. Bảng tổng hợp endpoint

| Method | Endpoint                           | Use Case     | Quyền      | HTTP | Ghi chú                             |
| ------ | ---------------------------------- | ------------ | ---------- | ---- | ----------------------------------- |
| GET    | `/api/v1/products`                 | SP-01        | AD, TN, NK | 200  | TN chỉ xem sản phẩm đang kinh doanh |
| GET    | `/api/v1/products/{id}`            | SP-02        | AD, TN, NK | 200  | Giá vốn chỉ AD được xem             |
| POST   | `/api/v1/products`                 | SP-03        | AD, NK     | 201  | Tạo sản phẩm, tồn ban đầu 0         |
| PUT    | `/api/v1/products/{id}`            | SP-04        | AD, NK     | 200  | Không sửa giá bán; có `phienBan`    |
| GET    | `/api/v1/products/{id}/prices`     | SP-02, SP-05 | AD         | 200  | Xem lịch sử giá                     |
| POST   | `/api/v1/products/{id}/prices`     | SP-05        | AD         | 201  | Tạo mức giá mới                     |
| POST   | `/api/v1/products/{id}/deactivate` | SP-06        | AD         | 200  | Ngừng kinh doanh                    |
| POST   | `/api/v1/products/{id}/activate`   | SP-06        | AD         | 200  | Mở bán lại                          |
| DELETE | `/api/v1/products/{id}`            | SP-06        | AD         | 204  | Chỉ xóa thật khi hợp lệ             |
| GET    | `/api/v1/categories`               | SP-07        | AD, TN, NK | 200  | Danh sách danh mục                  |
| POST   | `/api/v1/categories`               | SP-08        | AD         | 201  | Thêm danh mục                       |
| PUT    | `/api/v1/categories/{id}`          | SP-08        | AD         | 200  | Sửa danh mục; có `phienBan`         |
| POST   | `/api/v1/categories/{id}/hide`     | SP-09        | AD         | 200  | Ẩn danh mục                         |
| DELETE | `/api/v1/categories/{id}`          | SP-09        | AD         | 204  | Xóa danh mục hợp lệ                 |

Các endpoint chuyển trạng thái là đề xuất, cần Lead review. SP-05 còn cần đặc tả thao tác hủy giá bán đã lên lịch.

## 2. Quy ước API

- Mọi endpoint bắt đầu bằng `/api/v1`.
- Đường dẫn tiếng Anh, số nhiều, kebab-case.
- Trường JSON dùng camelCase, tiếng Việt không dấu.
- Enum viết chữ hoa, không dấu.
- GET danh sách trả `{items, total, page, size}`.
- GET chi tiết trả đối tượng trực tiếp.
- POST tạo bản ghi trả 201 và header Location.
- PUT cập nhật trả 200 và dùng `phienBan` khi cần kiểm soát sửa đồng thời.
- DELETE xóa thật trả 204 No Content.
- Chuyển trạng thái dùng POST.
- Lỗi trả `{code, message, details, traceId}`.
- Xác thực bằng cookie phiên HttpOnly.
- POST, PUT, DELETE cần header `X-XSRF-TOKEN`.
- Không dùng PATCH.
- Không cập nhật tồn kho trực tiếp qua API sản phẩm.

## 3. Đặc tả sơ bộ Use Case

### SP-01 – Xem, tìm kiếm, lọc danh sách sản phẩm

**Method:** GET
**Endpoint:** `/api/v1/products`
**Quyền:** AD, TN, NK

Query parameters:

- `page`: mặc định 1.
- `size`: mặc định 20, tối đa 100.
- `keyword`: tìm theo tên hoặc mã.
- `danhMucId`: lọc danh mục.
- `trangThai`: lọc trạng thái.
- `giaBanTu`, `giaBanDen`: khoảng giá bán.
- `sort`: sắp xếp.

Quy tắc:

- Tìm kiếm không phân biệt chữ hoa/chữ thường và dấu.
- TN chỉ xem sản phẩm đang kinh doanh.
- `giaVon` trả null đối với TN và NK.
- Không tìm thấy kết quả: HTTP 200 và `items` rỗng.

### SP-02 – Xem chi tiết sản phẩm

**Method:** GET
**Endpoint:** `/api/v1/products/{id}`
**Quyền:** AD, TN, NK

- Trả thông tin sản phẩm, danh mục, giá hiệu lực, tồn kho, ngưỡng, mô tả, ảnh và trạng thái.
- Giá vốn chỉ hiển thị cho AD.
- Sản phẩm không tồn tại: HTTP 404 `KHONG_TIM_THAY`.
- Lịch sử giá cần thể hiện theo phân quyền và quy tắc SP-05.

### SP-03 – Thêm sản phẩm

**Method:** POST
**Endpoint:** `/api/v1/products`
**Quyền:** AD, NK
**Success:** 201 Created và Location.

Request dự kiến:

```json
{
  "ma": null,
  "tenSanPham": "Sữa tươi Vinamilk 180ml",
  "danhMucId": 2,
  "donViTinh": "Hộp",
  "giaBan": 8000,
  "nguongToiThieu": 5,
  "moTa": null
}
```

Quy tắc:

- `tenSanPham`, `danhMucId`, `donViTinh`, `giaBan` bắt buộc.
- Mã không trùng, có thể để null để máy chủ sinh.
- Giá bán lớn hơn 0, bội số 100 đồng.
- Ngưỡng tồn mặc định 5.
- Tạo tồn ban đầu 0 thông qua dịch vụ kho.
- Tạo bản ghi lịch sử giá đầu tiên.
- Ảnh gửi riêng, JPG/PNG, không quá 2 MB.

Lỗi:

- `409 TRUNG_MA_SAN_PHAM`: mã đã tồn tại.
- `422 DU_LIEU_KHONG_HOP_LE`: thiếu trường hoặc giá sai.
- `403 KHONG_DU_QUYEN`: vai trò không hợp lệ.

### SP-04 – Sửa thông tin sản phẩm

**Method:** PUT
**Endpoint:** `/api/v1/products/{id}`
**Quyền:** AD, NK
**Success:** 200 OK.

- Sửa thông tin, không sửa giá bán.
- Request phải có `phienBan`.
- Sai phiên bản: `409 XUNG_DOT_DU_LIEU`.
- Không cho đổi đơn vị tính nếu đã có tồn hoặc giao dịch: `409 KHONG_DOI_DON_VI_TINH`.
- Hóa đơn và chứng từ cũ giữ dữ liệu đã lưu tại thời điểm giao dịch.

### SP-05 – Cập nhật giá bán

**Method:** POST
**Endpoint:** `/api/v1/products/{id}/prices`
**Quyền:** AD

- Giá mới là số nguyên dương, bội số 100.
- Hỗ trợ hiệu lực ngay hoặc trong tương lai.
- Thời điểm hiệu lực không được ở quá khứ.
- Lưu lịch sử giá mới, không ghi đè lịch sử cũ.
- Nếu giá thấp hơn giá vốn, cần cảnh báo và xác nhận lại.
- Giá hiện hành được xác định từ lịch sử giá tại thời điểm truy vấn.
- Đơn đang bán giữ giá tại lúc thêm vào giỏ.
- PENDING: endpoint hủy giá đã lên lịch nhưng chưa có hiệu lực.

### SP-06 – Ngừng kinh doanh/xóa sản phẩm

**Quyền:** AD
**Ưu tiên:** Should

- Nếu đã phát sinh giao dịch, không được xóa cứng.
- Nếu chưa có giao dịch, có thể xóa thật khi thỏa điều kiện.
- Khi ngừng kinh doanh, sản phẩm không được xuất hiện trong danh sách chọn ở POS và phiếu nhập mới.
- Hỗ trợ mở bán lại.
- Cảnh báo khi còn tồn hoặc đang nằm trong phiếu nhập nháp, đơn tạm giữ.
- PENDING: xác nhận endpoint và điều kiện chuyển trạng thái.

### SP-07 – Xem danh sách danh mục

**Method:** GET
**Endpoint:** `/api/v1/categories`
**Quyền:** AD, TN, NK
**Success:** 200 OK.

- Danh mục một cấp.
- Hiển thị tên, số sản phẩm và trạng thái.
- Danh sách trả `{items, total, page, size}`.

### SP-08 – Thêm/sửa danh mục

**Methods:** POST, PUT
**Endpoints:** `/api/v1/categories`, `/api/v1/categories/{id}`
**Quyền:** AD

- Nhập tên, mô tả, cờ `khongDoiTra`.
- Không cho phép tên danh mục trùng.
- PUT gửi `phienBan` để xử lý sửa đồng thời.
- POST trả 201, PUT trả 200.

### SP-09 – Xóa/ẩn danh mục

**Quyền:** AD
**Ưu tiên:** Could

- Chỉ xóa thật danh mục khi không còn sản phẩm.
- Nếu còn sản phẩm, đề nghị chuyển sang danh mục khác hoặc ẩn.
- Không cho xóa danh mục còn sản phẩm đang kinh doanh.
- PENDING: xác nhận điều kiện ẩn và endpoint chuyển trạng thái.

## 4. Pending Decisions

- PENDING-01: Hợp nhất ERD Sản phẩm và ERD Kho.
- PENDING-02: Chốt nơi lưu và quản lý `nguongToiThieu`.
- PENDING-03: Endpoint hủy giá bán đã lên lịch.
- PENDING-04: Điều kiện xóa thật và ngừng kinh doanh sản phẩm.
- PENDING-05: Xác nhận phạm vi MVP của các Use Case Should/Could.
- PENDING-06: Đối chiếu DTO với tên cột trong ERD hợp nhất.
- PENDING-07: Bổ sung request/response và lỗi chi tiết cho từng endpoint MVP trước khi mở PR chính thức.

## 5. Tài liệu tham chiếu

- Issue #4 – Quy ước chung viết API.
- Issue #50 – OpenAPI SP/NH/KHO.
- Issue #56–64 – Các Use Case Sản phẩm.
- PR #73 – ERD Sản phẩm/Nhập hàng.
- ERD phần Kho của #1.

## 6. Request/Response mẫu cho các endpoint MVP

### 6.1. SP-01 – Response danh sách sản phẩm

**GET** `/api/v1/products?page=1&size=20&keyword=sua`

HTTP 200 OK:

```json
{
  "items": [
    {
      "id": 15,
      "ma": "SP000015",
      "tenSanPham": "Sữa tươi Vinamilk 180ml",
      "danhMuc": {
        "id": 2,
        "ten": "Sữa và chế phẩm"
      },
      "donViTinh": "Hộp",
      "giaBan": 8000,
      "giaVon": null,
      "tonKho": 48,
      "nguongToiThieu": 5,
      "trangThai": "DANG_KINH_DOANH",
      "phienBan": 3
    }
  ],
  "total": 1,
  "page": 1,
  "size": 20
}
```

**Lưu ý:** Ví dụ phản hồi dành cho TN/NK, vì `giaVon` là `null`. AD có thể được trả giá vốn.

Không có dữ liệu phù hợp:

```json
{
  "items": [],
  "total": 0,
  "page": 1,
  "size": 20
}
```

### 6.2. SP-03 – Response tạo sản phẩm

**POST** `/api/v1/products`

HTTP 201 Created

Header:

`Location: /api/v1/products/15`

```json
{
  "id": 15,
  "ma": "SP000015",
  "tenSanPham": "Sữa tươi Vinamilk 180ml",
  "danhMuc": {
    "id": 2,
    "ten": "Sữa và chế phẩm"
  },
  "donViTinh": "Hộp",
  "giaBan": 8000,
  "giaVon": 0,
  "tonKho": 0,
  "nguongToiThieu": 5,
  "trangThai": "DANG_KINH_DOANH",
  "phienBan": 1
}
```

Đây là dữ liệu minh họa hợp đồng, không phải dữ liệu lấy từ database thật.

### 6.3. SP-04 – Request cập nhật sản phẩm

**PUT** `/api/v1/products/15`

```json
{
  "tenSanPham": "Sữa tươi Vinamilk 180ml ít đường",
  "danhMucId": 2,
  "donViTinh": "Hộp",
  "nguongToiThieu": 5,
  "moTa": "Sữa tươi ít đường",
  "phienBan": 3
}
```

HTTP 200 OK: trả đối tượng sản phẩm sau khi cập nhật với `phienBan` đã tăng.

Nếu `phienBan` không còn đúng, trả:

HTTP 409 Conflict

```json
{
  "code": "XUNG_DOT_DU_LIEU",
  "message": "Dữ liệu đã được người khác thay đổi, vui lòng tải lại.",
  "details": null,
  "traceId": "example-trace-id"
}
```

### 6.4. SP-05 – Request cập nhật giá bán

**POST** `/api/v1/products/15/prices`

```json
{
  "giaBan": 9000,
  "thoiDiemHieuLuc": "2026-10-20T08:00:00+07:00"
}
```

HTTP 201 Created: trả bản ghi giá mới được tạo.

Quy tắc:

- Chỉ AD được cập nhật giá.
- Giá phải lớn hơn 0 và là bội số của 100.
- Thời điểm hiệu lực không được ở quá khứ.
- Nếu thấp hơn giá vốn, phải có bước xác nhận lại theo SP-05 A1.
- PENDING: chi tiết hợp đồng xác nhận giá dưới giá vốn.
- PENDING: endpoint hủy giá đã lên lịch.

### 6.5. SP-07 – Response danh sách danh mục

**GET** `/api/v1/categories`

```json
{
  "items": [
    {
      "id": 2,
      "ten": "Sữa và chế phẩm",
      "khongDoiTra": false,
      "soSanPham": 12,
      "trangThai": "HOAT_DONG",
      "phienBan": 1
    }
  ],
  "total": 1,
  "page": 1,
  "size": 1
}
```

### 6.6. SP-08 – Request thêm danh mục

**POST** `/api/v1/categories`

```json
{
  "ten": "Sữa và chế phẩm",
  "moTa": "Các sản phẩm từ sữa",
  "khongDoiTra": false
}
```

HTTP 201 Created: trả bản ghi danh mục vừa tạo và header Location.

**PUT** `/api/v1/categories/2`

```json
{
  "ten": "Sữa và các chế phẩm",
  "moTa": "Các sản phẩm từ sữa",
  "khongDoiTra": false,
  "phienBan": 1
}
```

HTTP 200 OK: trả danh mục sau cập nhật.

## 7. Quy tắc lỗi chung

| HTTP | Error Code            | Trường hợp                             |
| ---- | --------------------- | -------------------------------------- |
| 400  | YEU_CAU_KHONG_HOP_LE  | JSON sai cú pháp hoặc sai kiểu dữ liệu |
| 401  | CHUA_DANG_NHAP        | Chưa xác thực                          |
| 401  | HET_PHIEN             | Phiên đăng nhập hết hạn                |
| 403  | KHONG_DU_QUYEN        | Không có quyền                         |
| 404  | KHONG_TIM_THAY        | Không có tài nguyên                    |
| 409  | XUNG_DOT_DU_LIEU      | Phiên bản đã thay đổi                  |
| 409  | TRUNG_MA_SAN_PHAM     | Mã sản phẩm trùng                      |
| 409  | KHONG_DOI_DON_VI_TINH | Đơn vị tính không được đổi             |
| 422  | DU_LIEU_KHONG_HOP_LE  | Vi phạm quy tắc nghiệp vụ              |
| 500  | LOI_MAY_CHU           | Lỗi máy chủ không lường trước          |

Mọi lỗi phải trả đúng `{code, message, details, traceId}`.
