# API Contract – Inventory (KHO)

**Issue:** #50 – T3-10
**Module:** Kho và tồn kho
**Phiên bản:** Draft v0.1
**Quy ước:** Issue #4 – Quy ước chung viết API
**Nguồn nghiệp vụ:** ERD Kho và dịch vụ ghi nhận biến động kho của #1

## 1. Bảng tổng hợp endpoint

| Method | Endpoint                                   | Use Case | Quyền   | HTTP | Ghi chú                        |
| ------ | ------------------------------------------ | -------- | ------- | ---- | ------------------------------ |
| GET    | `/api/v1/inventory`                        | KHO-01   | PENDING | 200  | Xem tồn kho hiện tại           |
| GET    | `/api/v1/inventory/{productId}/stock-card` | KHO-02   | PENDING | 200  | Lịch sử biến động              |
| POST   | `/api/v1/stocktakes`                       | KHO-03   | NK      | 201  | Lập phiếu kiểm kê              |
| PUT    | `/api/v1/stocktakes/{id}`                  | KHO-03   | NK      | 200  | Sửa phiếu nháp, đề xuất        |
| POST   | `/api/v1/stocktakes/{id}/submit`           | KHO-03   | NK      | 200  | Gửi duyệt                      |
| POST   | `/api/v1/stocktakes/{id}/approve`          | KHO-04   | AD      | 200  | Duyệt và điều chỉnh tồn        |
| POST   | `/api/v1/stocktakes/{id}/reject`           | KHO-04   | AD      | 200  | Từ chối kiểm kê                |
| POST   | `/api/v1/disposals`                        | KHO-05   | NK      | 201  | Sau MVP                        |
| PUT    | `/api/v1/inventory/{productId}/threshold`  | KHO-06   | PENDING | 200  | Cập nhật ngưỡng, URL đề xuất   |
| GET    | `/api/v1/inventory/alerts`                 | KHO-07   | PENDING | 200  | Cảnh báo tồn thấp, URL đề xuất |
| POST   | `/api/v1/disposals/{id}/approve`           | KHO-08   | AD      | 200  | Sau MVP                        |
| POST   | `/api/v1/disposals/{id}/reject`            | KHO-08   | AD      | 200  | Sau MVP                        |

Lưu ý:

- Quyền chưa có căn cứ xác nhận được đánh dấu PENDING.
- URL mới cần Lead duyệt và bổ sung vào quy ước API.
- KHO-05 và KHO-08 là phần sau MVP theo bảng tài nguyên của Issue #4; chưa đặc tả chi tiết.
- Các endpoint bổ sung cho kiểm kê phải đối chiếu trạng thái trong ERD.

## 2. Quy ước chung

- API prefix: `/api/v1`.
- Đường dẫn tiếng Anh, số nhiều và kebab-case.
- JSON camelCase, tiếng Việt không dấu.
- Enum là chuỗi chữ hoa không dấu.
- Danh sách trả `{items, total, page, size}`.
- Tạo tài nguyên trả 201 và header Location.
- Chuyển trạng thái dùng POST, thành công trả 200.
- PUT cập nhật phải kiểm soát sửa đồng thời theo quy ước.
- Không sử dụng PATCH.
- Lỗi trả `{code, message, details, traceId}`.
- Xác thực bằng cookie phiên HttpOnly.
- POST, PUT, DELETE cần header `X-XSRF-TOKEN`.
- Không ghi trực tiếp số lượng tồn từ API bên ngoài.

## 3. Các Use Case

### KHO-01 – Xem tồn kho hiện tại

**GET** `/api/v1/inventory`

Trả danh sách sản phẩm cùng số lượng tồn hiện tại.

Query dự kiến:

- `page`, `size`
- `keyword`
- `danhMucId`
- `sort`

Response minh họa:

```json
{
  "items": [
    {
      "sanPhamId": 15,
      "tonKho": 48,
      "nguongToiThieu": 5
    }
  ],
  "total": 1,
  "page": 1,
  "size": 20
}
```

PENDING:

- Phân quyền xem giá vốn.
- Bộ lọc chính thức theo SRS.
- DTO sau khi hợp nhất ERD.

### KHO-02 – Xem lịch sử biến động kho

**GET** `/api/v1/inventory/{productId}/stock-card`

Query:

- `page`, `size`
- `tuNgay`, `denNgay`
- `sort` nếu được hỗ trợ

Response minh họa:

```json
{
  "items": [
    {
      "id": 1,
      "sanPhamId": 15,
      "loaiBienDong": "NHAP_HANG",
      "maChungTu": "PN-20261017-001",
      "chungTuId": 10,
      "soLuongThayDoi": 20,
      "tonSau": 48,
      "thoiDiem": "2026-10-17T08:30:00+07:00"
    }
  ],
  "total": 1,
  "page": 1,
  "size": 20
}
```

Các loại biến động trong ERD:

- NHAP_HANG
- BAN_HANG
- HUY_HOA_DON
- TRA_HANG
- KIEM_KE
- XUAT_HUY

Thẻ kho chỉ được thêm thông qua nghiệp vụ, không cho phép sửa hoặc xóa lịch sử.

PENDING: Trường giá vốn và phân quyền hiển thị.

### KHO-03 – Tạo phiếu kiểm kê

**POST** `/api/v1/stocktakes`

**Quyền theo ERD:** NK

Request minh họa:

```json
{
  "phamVi": "CHON_SAN_PHAM",
  "danhMucId": null,
  "ghiChu": "Kiểm kê đầu ca",
  "chiTiet": [
    {
      "sanPhamId": 15,
      "soLuongThucTe": 45,
      "lyDoChenhLech": "Thiếu 3 hộp"
    }
  ]
}
```

Response 201 Created:

- Header Location trỏ đến phiếu mới.
- Body là đối tượng phiếu kiểm kê.
- Trạng thái ban đầu: `NHAP`.

Phạm vi theo ERD:

- `TOAN_BO`
- `DANH_MUC`
- `CHON_SAN_PHAM`

Quy tắc:

- Một phiếu có từ một dòng kiểm kê trở lên.
- `soLuongThucTe` có thể null khi chưa đếm xong.
- Không thay đổi tồn kho khi mới tạo phiếu nháp.
- Cần gửi phiếu sang trạng thái `CHO_DUYET`.

PENDING: DTO, điều kiện sửa nháp, các validation theo SRS.

### KHO-04 – Duyệt và điều chỉnh tồn

**POST** `/api/v1/stocktakes/{id}/approve`

**Quyền theo ERD:** AD

Quy tắc:

- Chỉ duyệt phiếu đang ở trạng thái `CHO_DUYET`.
- Tính chênh lệch dựa trên tồn thực tế so với tồn tại thời điểm duyệt.
- Gọi dịch vụ Kho để ghi nhận thay đổi tồn.
- Ghi lịch sử thẻ kho và cập nhật chứng từ trong cùng transaction.
- Nếu nghiệp vụ thất bại thì rollback.

Trạng thái sau duyệt thành công: `DA_DIEU_CHINH`.

**POST** `/api/v1/stocktakes/{id}/reject`

Từ chối phiếu:

- Chuyển trạng thái thành `BI_TU_CHOI`.
- Lưu lý do từ chối.
- Không thay đổi tồn kho.

Nếu chứng từ đã được xử lý:

- HTTP 409
- `DA_DUOC_XU_LY`

PENDING: Request JSON và các điều kiện từ chối theo đặc tả dịch vụ Kho.

### KHO-05 – Lập phiếu xuất hủy

**POST** `/api/v1/disposals`

**Phạm vi:** Sau MVP theo Issue #4.

Theo ERD:

- NK lập phiếu.
- Dòng phiếu gồm sản phẩm, số lượng, lý do.
- Lý do gồm `HONG`, `HET_HAN`, `MAT`, `KHAC`.
- Không giảm tồn khi mới lập phiếu.

### KHO-06 – Thiết lập ngưỡng tồn tối thiểu

**PUT** `/api/v1/inventory/{productId}/threshold`

URL đề xuất, cần xác nhận.

Request minh họa:

```json
{
  "nguongToiThieu": 5
}
```

Quy tắc theo ERD:

- Ngưỡng là số nguyên không âm.
- Mặc định bằng 5.
- Ngưỡng bằng 0 nghĩa là không cảnh báo.

PENDING:

- Quyền cập nhật.
- Có cần `phienBan` cho bản ghi tồn kho không.
- URL và response chính thức.

### KHO-07 – Xem cảnh báo hàng sắp hết

**GET** `/api/v1/inventory/alerts`

URL đề xuất, cần xác nhận.

Response theo định dạng danh sách:
`{items, total, page, size}`.

Cảnh báo dựa trên tồn hiện tại và ngưỡng tối thiểu.

PENDING:

- Điều kiện chính xác để xác định cảnh báo.
- Phân quyền và bộ lọc.
- DTO hiển thị.

### KHO-08 – Duyệt phiếu xuất hủy

**POST** `/api/v1/disposals/{id}/approve`

**Phạm vi:** Sau MVP theo Issue #4.

Theo ERD:

- AD duyệt phiếu.
- Chỉ duyệt khi ở trạng thái `CHO_DUYET`.
- Kiểm tra tồn trước khi xuất hủy.
- Ghi biến động kho qua dịch vụ Kho trong cùng transaction.
- Trạng thái cuối là `DA_XUAT_HUY`.

Khi từ chối, trạng thái là `BI_TU_CHOI`.

## 4. Mã lỗi

| HTTP | Code                   | Ý nghĩa                           |
| ---- | ---------------------- | --------------------------------- |
| 400  | YEU_CAU_KHONG_HOP_LE   | JSON sai cú pháp hoặc kiểu        |
| 401  | CHUA_DANG_NHAP         | Chưa đăng nhập                    |
| 401  | HET_PHIEN              | Hết phiên                         |
| 403  | KHONG_DU_QUYEN         | Không có quyền                    |
| 404  | KHONG_TIM_THAY         | Không tìm thấy tài nguyên         |
| 409  | TON_KHONG_DU           | Tồn sau biến động không hợp lệ    |
| 409  | DA_DUOC_XU_LY          | Chứng từ đã được xử lý            |
| 409  | XUNG_DOT_DU_LIEU       | Dữ liệu đã bị thay đổi            |
| 422  | SAN_PHAM_KHONG_TON_TAI | Không có bản ghi tồn kho sản phẩm |
| 422  | DU_LIEU_KHONG_HOP_LE   | Sai quy tắc nghiệp vụ             |
| 500  | LOI_MAY_CHU            | Lỗi máy chủ                       |

Mọi response lỗi tuân thủ:
`{code, message, details, traceId}`.

## 5. Quy tắc tích hợp

- Bảng `ton_kho` lưu số lượng, giá vốn bình quân và ngưỡng tồn.
- Bảng `the_kho` lưu lịch sử biến động.
- Không cho phép các module SP, NH, POS sửa trực tiếp `ton_kho`.
- NH-06 phải gọi dịch vụ ghi nhận biến động kho.
- KHO-04 và KHO-08 cũng sử dụng dịch vụ này.
- Chứng từ và biến động kho phải được lưu nhất quán trong transaction.

## 6. Pending Decisions

- PENDING-01: Quyền chi tiết KHO-01, KHO-02, KHO-06, KHO-07.
- PENDING-02: Hợp đồng dịch vụ Kho đã được Lead xác nhận.
- PENDING-03: DTO và mapping từ ERD hợp nhất.
- PENDING-04: Endpoint thay đổi ngưỡng tồn.
- PENDING-05: Endpoint cảnh báo tồn thấp.
- PENDING-06: Request/Response chi tiết cho các endpoint MVP.
- PENDING-07: Ranh giới MVP và tầng 2 của KHO-03/KHO-04.
- PENDING-08: Thống nhất kiểu dữ liệu và migrations sau ADR-001.

## 7. Tài liệu tham chiếu

- Issue #4 – Quy ước chung viết API.
- Issue #50 – OpenAPI SP/NH/KHO.
- ERD Kho của #1.
- Tài liệu dịch vụ ghi nhận biến động kho.
- SRS hệ thống quản lý cửa hàng tiện lợi.
