# Quy ước chung viết API

| | |
|---|---|
| **Dự án** | Hệ thống quản lý cửa hàng tiện lợi – Nhóm 7, INFO2005 |
| **Issue** | T3-04 – Quy ước chung cho hợp đồng API |
| **Trạng thái** | Đề xuất – chờ #2, #3, #5 xác nhận |
| **Phiên bản** | 1.0 · 16/10/2026 · #1 Phạm Gia Thăng |
| **Áp dụng cho** | Mọi endpoint trong `src/ConvenienceStore.Web`; mọi file `docs/api/<module>.md` |
| **Căn cứ** | ADR-001 (tech stack); Tài liệu dịch vụ kho bản 1.2 mục 3.5, 7, 8.3; ERD toàn hệ thống v1; SRS chương 3 và 6 |
| **Cập nhật** | 18/10/2026 · #1: đồng bộ giá trị liệt kê và tên trường thời điểm với ERD v1 (mục 4.2, 5, 6.1); thêm tài nguyên `/return-policies`, `/loyalty-rules`; danh sách endpoint của 81 use case ở SRS v1.0.3 mục 7.1<br>20/10/2026 · #1: đăng ký 4 mã lỗi mới của AUTH (mục 8.4); sửa căn cứ của `MAT_ADMIN_CUOI_CUNG` theo AUTH-07 mới; dẫn bộ mã quyền (mục 9.4). Chờ nhóm xác nhận |

> Đọc file này **trước khi** viết danh sách endpoint cho module của mình (T3-05, T3-10, T3-14, T3-25) và trước khi code API. Muốn đổi quy ước: mở issue, #1 duyệt, cập nhật file này qua PR.

---

## Mục lục

1. [Nguyên tắc chung](#1-nguyên-tắc-chung)
2. [Đường dẫn (URL)](#2-đường-dẫn-url)
3. [Phương thức HTTP và mã thành công](#3-phương-thức-http-và-mã-thành-công)
4. [Tên trường JSON](#4-tên-trường-json)
5. [Kiểu dữ liệu](#5-kiểu-dữ-liệu)
6. [Phản hồi thành công](#6-phản-hồi-thành-công)
7. [Phân trang, tìm kiếm, lọc, sắp xếp](#7-phân-trang-tìm-kiếm-lọc-sắp-xếp)
8. [Phản hồi lỗi](#8-phản-hồi-lỗi)
9. [Xác thực và phân quyền](#9-xác-thực-và-phân-quyền)
10. [Sửa đồng thời và xử lý trùng](#10-sửa-đồng-thời-và-xử-lý-trùng)
11. [Nghiệp vụ thay đổi tồn kho](#11-nghiệp-vụ-thay-đổi-tồn-kho)
12. [Viết tài liệu API cho module](#12-viết-tài-liệu-api-cho-module)
13. [Thay đổi API sau khi đã chốt](#13-thay-đổi-api-sau-khi-đã-chốt)
14. [Checklist review PR có API](#14-checklist-review-pr-có-api)
15. [Thay đổi so với bản nháp trong tài liệu dịch vụ kho](#15-thay-đổi-so-với-bản-nháp-trong-tài-liệu-dịch-vụ-kho)

---

## 1. Nguyên tắc chung

- API theo kiểu **REST**, dữ liệu **JSON** mã hóa **UTF-8**.
- Mọi endpoint bắt đầu bằng **`/api/v1`**.
- Giao diện React và API chạy **cùng domain**; xác thực bằng **cookie phiên** (mục 9).
- **Máy chủ là nơi quyết định**: kiểm tra quyền, kiểm tra dữ liệu, tính tiền, tính tồn đều làm ở máy chủ. Giao diện có kiểm tra cũng chỉ để tiện người dùng, không thay cho máy chủ.
- **Một ngôn ngữ nghiệp vụ**: tên trường, mã trạng thái, mã lỗi dùng tiếng Việt không dấu giống CSDL và tài liệu; chỉ đường dẫn dùng tiếng Anh (lý do ở mục 2.1).

---

## 2. Đường dẫn (URL)

### 2.1. Quy tắc

| Quy tắc | Đúng | Sai |
|---|---|---|
| Danh từ **tiếng Anh, số nhiều**, chữ thường | `/products` | `/product`, `/san-pham`, `/getProducts` |
| Nhiều từ nối bằng **gạch ngang** (kebab-case) | `/purchase-orders` | `/purchaseOrders`, `/purchase_orders` |
| Một bản ghi: thêm `/{id}` | `/products/15` | `/products?id=15` |
| Lồng **tối đa 1 cấp**, chỉ khi bản ghi con không tồn tại độc lập | `/products/15/price-history` | `/categories/2/products/15/prices` |
| Không đưa động từ vào đường dẫn, trừ **hành động chuyển trạng thái** (mục 2.3) | `DELETE /carts/7/items/3` | `POST /deleteCartItem` |

**Vì sao đường dẫn tiếng Anh còn trường JSON tiếng Việt?** Bản nháp endpoint AUTH và KHO đã viết bằng tiếng Anh; đường dẫn tiếng Anh ngắn, theo chuẩn REST phổ biến và dễ đọc trên Swagger. Trường JSON thì khớp 1–1 với tên cột CSDL (`ten_san_pham` → `tenSanPham`), nên không phải đặt tên hai lần và không lệch với ERD, SRS.

### 2.2. Bảng tên tài nguyên

Dùng đúng các tên dưới đây. Cần tên mới: thêm vào bảng qua PR.

| Đường dẫn | Nghĩa | Module | Người sở hữu |
|---|---|---|---|
| `/auth` | Đăng nhập, đăng xuất, đổi mật khẩu | AUTH | #1 |
| `/me` | Hồ sơ người đang đăng nhập | AUTH | #1 (#6 AUTH-04) |
| `/accounts` | Tài khoản | AUTH | #1 |
| `/roles` | Vai trò | AUTH | #1 |
| `/categories` | Danh mục | SP | #2 |
| `/products` | Sản phẩm | SP | #2 |
| `/products/{id}/prices` | Lịch sử giá bán | SP | #2 |
| `/suppliers` | Nhà cung cấp | NH | #2 |
| `/purchase-orders` | Phiếu nhập | NH | #2 |
| `/inventory` | Tồn kho, thẻ kho, ngưỡng, cảnh báo | KHO | #1 |
| `/stocktakes` | Phiếu kiểm kê | KHO | #1 (tầng 2) |
| `/disposals` | Phiếu xuất hủy | KHO | Sau MVP |
| `/carts` | Đơn đang bán (giỏ hàng) | POS | #3 |
| `/invoices` | Hóa đơn | POS | #3 |
| `/customers` | Khách hàng | KH | #5 |
| `/promotions` | Chương trình khuyến mãi | KM | Tầng 2 |
| `/returns` | Phiếu đổi trả | DT | Sau MVP |
| `/return-policies` | Chính sách đổi trả (DT-08) | DT | Sau MVP (#6) |
| `/loyalty-rules` | Quy tắc tích điểm (KH-07) | KH | Sau MVP (#6) |
| `/reports` | Báo cáo, dashboard | BC | #5 |
| `/ai` | Tính năng AI (AI-01) | AI | #5 |

> Giỏ hàng **lưu ở máy chủ** qua `/carts` (đã chốt trong review T3-14; bảng `gio_hang`, `ct_gio_hang` ở ERD Hóa đơn/Đổi trả v1). `itemId` của dòng giỏ là `sanPhamId`.

### 2.3. Hành động chuyển trạng thái

Việc không phải thêm, sửa, xóa thuần túy (duyệt, từ chối, khóa, xác nhận…) viết thành **`POST /{tài-nguyên}/{id}/{động-từ}`**, động từ tiếng Anh, kebab-case:

| Ví dụ | Use case |
|---|---|
| `POST /purchase-orders/{id}/confirm` | NH-06 Xác nhận nhập kho |
| `POST /accounts/{id}/lock`, `.../unlock` | AUTH-08 |
| `POST /accounts/{id}/reset-password` | AUTH-09 |
| `POST /stocktakes/{id}/submit`, `.../approve`, `.../reject` | KHO-03, KHO-04 |
| `POST /invoices/{id}/cancel` | POS-12 |

### 2.4. Báo cáo và dữ liệu tổng hợp

Báo cáo là tài nguyên chỉ đọc: `GET /reports/{tên-báo-cáo}` với tham số lọc trên query string.

```
GET /api/v1/reports/dashboard
GET /api/v1/reports/revenue?tuNgay=2026-10-01&denNgay=2026-10-31&nhomTheo=NGAY
GET /api/v1/reports/top-products?tuNgay=2026-10-01&denNgay=2026-10-31&soLuong=10
```

---

## 3. Phương thức HTTP và mã thành công

| Thao tác | Phương thức | Mã thành công | Thân phản hồi |
|---|---|---|---|
| Lấy danh sách | `GET /products` | `200 OK` | Danh sách có phân trang (mục 6.2) |
| Lấy một bản ghi | `GET /products/{id}` | `200 OK` | Bản ghi |
| Tạo mới | `POST /products` | `201 Created` + header `Location: /api/v1/products/{id}` | Bản ghi vừa tạo |
| Cập nhật | `PUT /products/{id}` | `200 OK` | Bản ghi sau khi sửa |
| Xóa | `DELETE /products/{id}` | `204 No Content` | Không có |
| Hành động chuyển trạng thái | `POST /.../{id}/{động-từ}` | `200 OK` | Bản ghi sau khi chuyển trạng thái |
| Hành động không trả dữ liệu (đăng xuất…) | `POST` | `204 No Content` | Không có |

- **Không dùng `PATCH`.** `PUT` gửi **toàn bộ các trường được phép sửa** của use case đó; trường không được phép sửa (mã sản phẩm đã có giao dịch, giá bán ở SP-04…) bị bỏ qua hoặc báo lỗi 422.
- **"Xóa mềm"** (ngừng kinh doanh, ẩn, khóa) là hành động chuyển trạng thái, không dùng `DELETE`. `DELETE` chỉ dùng khi thực sự xóa bản ghi (ví dụ xóa dòng giỏ hàng, xóa sản phẩm chưa có giao dịch theo PA-SP-01.B).
- `GET` **không bao giờ thay đổi dữ liệu**.

---

## 4. Tên trường JSON

### 4.1. Quy tắc

| Quy tắc | Ví dụ |
|---|---|
| **camelCase, tiếng Việt không dấu**, chuyển từ tên cột CSDL | `ten_san_pham` → `tenSanPham` |
| Khóa chính luôn là `id` | `"id": 15` |
| Khóa ngoại: tên bảng + `Id` | `sanPhamId`, `danhMucId`, `nhaCungCapId` |
| Đối tượng lồng kèm theo khóa ngoại khi giao diện cần hiển thị tên | `"danhMuc": { "id": 2, "ten": "Đồ uống" }` |
| Danh sách con: danh từ số ít + "Chi tiết" hoặc tên rõ nghĩa | `"chiTiet": [ ... ]` |
| Trường đúng/sai (boolean): bắt đầu bằng động từ hoặc trạng thái | `batBuocDoiMatKhau`, `khongDoiTra`, `dangHieuLuc` |
| Không viết tắt khó hiểu | `soLuong` (không `sl`), `nhaCungCap` (không `ncc`) |

Từ đã quen trong tài liệu thì dùng thống nhất: `giaBan`, `giaVon`, `soLuong`, `tonKho`, `nguongToiThieu`, `donViTinh`, `trangThai`, `ghiChu`, `lyDo`, `ngayTao`, `ngayCapNhat`, `nguoiTaoId`. Thời điểm tạo, sửa của bản ghi dùng `ngayTao`, `ngayCapNhat` (cột `ngay_tao`, `ngay_cap_nhat`); thời điểm của một sự kiện dùng `<việc>Luc` (`duyetLuc`, `huyLuc`, `thanhToanLuc`), khớp tên cột ERD v1.

### 4.2. Giá trị liệt kê (enum)

- Viết **CHỮ_HOA_KHÔNG_DẤU, nối gạch dưới**, giống hệt giá trị ENUM trong CSDL.
- Gửi và nhận dưới dạng **chuỗi**, không dùng số.

```json
{ "trangThai": "CHO_DUYET" }
{ "loaiBienDong": "NHAP_HANG" }
{ "vaiTro": "AD" }
```

Giá trị đã chốt trong ERD v1 (18/10/2026) – dùng đúng các tên này trong API:

| Trường | Giá trị |
|---|---|
| Vai trò | `AD`, `TN`, `NK` |
| Trạng thái tài khoản | `HOAT_DONG`, `DA_KHOA` |
| Trạng thái danh mục | `HOAT_DONG`, `DA_AN` |
| Trạng thái sản phẩm | `DANG_KINH_DOANH`, `NGUNG_KINH_DOANH` |
| Trạng thái nhà cung cấp | `HOAT_DONG`, `NGUNG_HOP_TAC` |
| Trạng thái phiếu nhập | `NHAP`, `DA_NHAP_KHO`, `DA_HUY` |
| Loại biến động kho | `NHAP_HANG`, `BAN_HANG`, `HUY_HOA_DON`, `TRA_HANG`, `KIEM_KE`, `XUAT_HUY` |
| Phiếu kiểm kê | phạm vi `TOAN_BO`, `DANH_MUC`, `CHON_SAN_PHAM`; trạng thái `NHAP`, `CHO_DUYET`, `BI_TU_CHOI`, `DA_DIEU_CHINH` |
| Phiếu xuất hủy | trạng thái `NHAP`, `CHO_DUYET`, `BI_TU_CHOI`, `DA_XUAT_HUY`; lý do `HONG`, `HET_HAN`, `MAT`, `KHAC` |
| Giỏ hàng | trạng thái `DANG_BAN`, `TAM_GIU`, `DA_HOAN_TAT`, `DA_HUY`; lý do hủy `THU_NGAN_HUY`, `HET_HAN_TAM_GIU` |
| Hóa đơn | `DA_THANH_TOAN`, `DA_HUY` |
| Phiếu đổi trả | loại `TRA_HANG`, `DOI_HANG`; trạng thái `CHO_DUYET`, `DA_DUYET`, `HOAN_TAT`, `BI_TU_CHOI`; lý do `LOI_SAN_PHAM`, `HET_HAN`, `KHONG_DUNG_NHU_CAU`, `KHAC`; cách xử lý `NHAP_LAI`, `HUY` |
| Khách hàng | trạng thái `HOAT_DONG`, `DA_AN`; giới tính `NAM`, `NU`, `KHAC` |
| Lịch sử điểm | `TICH_DIEM`, `THU_HOI_TRA_HANG`, `THU_HOI_HUY_HOA_DON`, `DIEU_CHINH` |
| Chương trình khuyến mãi | loại giảm `PHAN_TRAM`, `SO_TIEN`; trạng thái lưu `NHAP`, `DA_KICH_HOAT`, `TAM_DUNG`, `DA_KET_THUC`; trạng thái hiển thị (trả trong API) `NHAP`, `DA_LEN_LICH`, `DANG_CHAY`, `TAM_DUNG`, `DA_KET_THUC` |

Cần giá trị mới: thêm vào ERD (qua migration, #1 duyệt) và tài liệu API của module trong cùng PR.

---

## 5. Kiểu dữ liệu

| Loại dữ liệu | Kiểu JSON | Quy ước | Ví dụ |
|---|---|---|---|
| Tiền bán, tiền thu, tiền thừa, tiền hoàn | số nguyên | VND, không có phần thập phân (BR-POS-02); giá bán là bội số của 100 (BR-SP-05) | `"giaBan": 15000` |
| Giá vốn | số | 2 chữ số thập phân, làm tròn nửa lên (đã chốt, Tài liệu dịch vụ kho bản 1.2 mục 7 điểm 2) | `"giaVon": 10285.71` |
| Số lượng, tồn kho, điểm | số nguyên | Không âm, trừ trường hợp chênh lệch kiểm kê | `"soLuong": 3` |
| Thời điểm | chuỗi | **ISO 8601 có múi giờ +07:00** (BR-BC-03) | `"ngayTao": "2026-10-16T08:30:00+07:00"` |
| Ngày (không có giờ) | chuỗi | `YYYY-MM-DD` | `"tuNgay": "2026-10-01"` |
| Mã chứng từ | chuỗi | `<LOẠI>-YYYYMMDD-nnn` | `"ma": "PN-20261016-001"` |
| Mã định danh | số nguyên | `id` tự tăng | `"id": 15` |
| Không có giá trị | `null` | **Luôn trả trường**, giá trị `null`; không bỏ trường khỏi JSON | `"ghiChu": null` |

Tiền tố mã chứng từ: `PN` phiếu nhập · `HD` hóa đơn · `TH` phiếu trả hàng · `KK` phiếu kiểm kê · `XH` phiếu xuất hủy.

> Định dạng tiền "15.000 đ", ngày "16/10/2026" là việc của **giao diện**. API luôn trả số và chuỗi ISO như bảng trên.

---

## 6. Phản hồi thành công

### 6.1. Một bản ghi

Trả **thẳng đối tượng**, không bọc thêm lớp `data`:

```json
{
  "id": 15,
  "ma": "SP000015",
  "tenSanPham": "Sữa tươi Vinamilk 180ml",
  "danhMuc": { "id": 2, "ten": "Sữa và chế phẩm" },
  "donViTinh": "Hộp",
  "giaBan": 8000,
  "giaVon": 6450.00,
  "tonKho": 48,
  "nguongToiThieu": 5,
  "trangThai": "DANG_KINH_DOANH",
  "phienBan": 3,
  "ngayCapNhat": "2026-10-16T08:30:00+07:00"
}
```

### 6.2. Danh sách

**Mọi** endpoint trả danh sách dùng cùng một dạng, kể cả danh sách nhỏ không cần phân trang:

```json
{
  "items": [ { "id": 15, "tenSanPham": "..." } ],
  "total": 132,
  "page": 1,
  "size": 20
}
```

| Trường | Ý nghĩa |
|---|---|
| `items` | Các bản ghi của trang hiện tại |
| `total` | Tổng số bản ghi khớp bộ lọc (không phải số bản ghi trong trang) |
| `page`, `size` | Trang và cỡ trang đã dùng |

Danh sách nhỏ không phân trang (vai trò, danh mục cho ô chọn…): trả toàn bộ trong `items`, `page = 1`, `size = total`.

> Tên `items, total, page, size` là tên kỹ thuật dùng chung, giữ tiếng Anh như bản nháp đã chốt.

---

## 7. Phân trang, tìm kiếm, lọc, sắp xếp

| Tham số | Ý nghĩa | Mặc định | Ví dụ |
|---|---|---|---|
| `page` | Số trang, bắt đầu từ 1 | `1` | `?page=2` |
| `size` | Số bản ghi mỗi trang, tối đa 100 | `20` | `?size=50` |
| `keyword` | Từ khóa tìm kiếm, không phân biệt hoa thường và dấu (BR-SP-01) | – | `?keyword=sua` |
| `sort` | Trường sắp xếp; thêm `-` phía trước để giảm dần; nhiều trường cách nhau dấu phẩy | Tùy endpoint, ghi trong tài liệu | `?sort=-ngayTao,tenSanPham` |
| `<tênTrường>` | Lọc bằng đúng tên trường JSON | – | `?danhMucId=2&trangThai=DANG_KINH_DOANH` |
| `tuNgay`, `denNgay` | Khoảng thời gian, **tính cả hai đầu**, theo giờ +07:00 | – | `?tuNgay=2026-10-01&denNgay=2026-10-31` |
| `<tênTrường>Tu`, `<tênTrường>Den` | Khoảng giá trị số | – | `?giaBanTu=5000&giaBanDen=20000` |

- `size` > 100 thì máy chủ tự hạ xuống 100, không báo lỗi.
- Tham số lọc không hợp lệ (trường không tồn tại, giá trị enum sai) → `422 DU_LIEU_KHONG_HOP_LE`.
- Endpoint nào hỗ trợ tham số nào phải ghi rõ trong tài liệu API của module.

---

## 8. Phản hồi lỗi

### 8.1. Dạng chung

Mọi lỗi, ở mọi module, đều trả cùng một dạng:

```json
{
  "code": "TON_KHONG_DU",
  "message": "Sữa tươi Vinamilk 180ml chỉ còn 2 sản phẩm.",
  "details": [
    { "sanPhamId": 15, "tonHienTai": 2, "soLuongYeuCau": 3 }
  ],
  "traceId": "0HN7F2K8V3Q1A:00000004"
}
```

| Trường | Bắt buộc | Ý nghĩa |
|---|---|---|
| `code` | Có | Mã lỗi cố định, CHỮ_HOA_KHÔNG_DẤU. **Giao diện dựa vào `code` để xử lý**, không so sánh chuỗi `message` |
| `message` | Có | Câu tiếng Việt có dấu, hiển thị được ngay cho người dùng, đúng câu chữ trong phần ngoại lệ của đặc tả use case |
| `details` | Không | Thông tin thêm: danh sách lỗi theo trường (mục 8.3) hoặc dữ liệu riêng của lỗi; `null` nếu không có |
| `traceId` | Có | Mã truy vết để tìm trong log máy chủ khi báo lỗi |

### 8.2. Mã HTTP

| Mã | Khi nào | Mã lỗi chung |
|---|---|---|
| `400 Bad Request` | JSON sai cú pháp, sai kiểu dữ liệu (gửi chữ vào trường số) | `YEU_CAU_KHONG_HOP_LE` |
| `401 Unauthorized` | Chưa đăng nhập, hết phiên 30 phút; đăng nhập sai thông tin | `CHUA_DANG_NHAP`, `HET_PHIEN`, `SAI_THONG_TIN_DANG_NHAP` |
| `403 Forbidden` | Đã đăng nhập nhưng vai trò không có quyền; đang bị bắt buộc đổi mật khẩu | `KHONG_DU_QUYEN`, `PHAI_DOI_MAT_KHAU` |
| `404 Not Found` | Không có bản ghi với `id` đó | `KHONG_TIM_THAY` |
| `409 Conflict` | Xung đột với trạng thái hiện tại: trùng dữ liệu duy nhất, tồn không đủ, phiếu đã được người khác xử lý, dữ liệu đã bị người khác sửa | `XUNG_DOT_DU_LIEU`, `DA_DUOC_XU_LY`, `TRUNG_DU_LIEU` + mã riêng của module |
| `422 Unprocessable Entity` | Dữ liệu đúng kiểu nhưng sai quy tắc nghiệp vụ: thiếu trường bắt buộc, giá ≤ 0, không là bội số 100… | `DU_LIEU_KHONG_HOP_LE` + mã riêng của module |
| `423 Locked` | Tài khoản đang bị khóa tạm hoặc bị Admin khóa | `TAI_KHOAN_BI_KHOA` |
| `500 Internal Server Error` | Lỗi không lường trước, lỗi CSDL | `LOI_MAY_CHU` |

**Phân biệt 400 và 422:** máy chủ không đọc được yêu cầu → 400; đọc được nhưng dữ liệu vi phạm quy tắc → 422.

**Lỗi 500:** `message` luôn là "Hệ thống đang gặp sự cố, vui lòng thử lại sau." **Không bao giờ** trả thông báo lỗi gốc, câu SQL hay stack trace ra ngoài; chi tiết chỉ ghi vào log kèm `traceId`.

### 8.3. Lỗi kiểm tra dữ liệu theo trường

Khi có nhiều trường sai, trả **tất cả** trong một lần để giao diện báo lỗi tại từng ô:

```json
{
  "code": "DU_LIEU_KHONG_HOP_LE",
  "message": "Dữ liệu chưa hợp lệ, vui lòng kiểm tra lại.",
  "details": [
    { "truong": "giaBan", "loi": "Giá bán phải lớn hơn 0 và là bội số của 100 đồng." },
    { "truong": "tenSanPham", "loi": "Vui lòng nhập tên sản phẩm." }
  ],
  "traceId": "..."
}
```

`truong` dùng đúng tên trường JSON; trường lồng viết theo đường dẫn: `chiTiet[2].soLuong`.

### 8.4. Mã lỗi riêng của module

- Mỗi module đặt mã lỗi riêng cho các ngoại lệ trong đặc tả use case, **đăng ký vào bảng dưới đây qua PR** để không trùng.
- Ưu tiên dùng mã chung (mục 8.2); chỉ tạo mã riêng khi giao diện cần xử lý khác đi.

| Mã | HTTP | Module | Use case / căn cứ | Ý nghĩa |
|---|---|---|---|---|
| `TON_KHONG_DU` | 409 | KHO | Dịch vụ kho 3.5; POS-07 E1 | Tồn sau biến động < 0 |
| `SAN_PHAM_KHONG_TON_TAI` | 422 | KHO | Dịch vụ kho 3.5 | Không có bản ghi tồn kho của sản phẩm |
| `TRUNG_TEN_DANG_NHAP` | 409 | AUTH | AUTH-06 | Tên đăng nhập đã tồn tại |
| `MAT_ADMIN_CUOI_CUNG` | 409 | AUTH | AUTH-08 E1, AUTH-10 E1 (BR-AUTH-08) | Thao tác làm hệ thống không còn Admin nào hoạt động |
| `TAI_KHOAN_KHOA_TAM` | 423 | AUTH | AUTH-01 E3, BR-AUTH-11 | Tài khoản đang khóa tạm do sai mật khẩu; `details` có `conLaiPhut`, `khoaTamDen` |
| `SAI_MAT_KHAU_HIEN_TAI` | 422 | AUTH | AUTH-03 E1 | Mật khẩu hiện tại không đúng |
| `KHONG_THAO_TAC_CHINH_MINH` | 409 | AUTH | AUTH-08 E1 | Admin tự khóa tài khoản của chính mình |
| `TAI_KHOAN_DANG_KHOA` | 409 | AUTH | AUTH-09 E1 | Đặt lại mật khẩu cho tài khoản đang bị Admin khóa |
| `TRUNG_MA_SAN_PHAM` | 409 | SP | SP-03 E1 | Mã sản phẩm đã tồn tại |
| `KHONG_DOI_DON_VI_TINH` | 409 | SP | SP-04 E2 (BR-SP-06) | Đổi đơn vị tính khi sản phẩm đã có tồn hoặc giao dịch |
| *(module thêm vào đây)* | | | | |

---

## 9. Xác thực và phân quyền

### 9.1. Phiên đăng nhập

- `POST /api/v1/auth/login` thành công → máy chủ đặt **cookie phiên** `HttpOnly; Secure; SameSite=Strict`, lưu phiên ở bảng `phien_dang_nhap`.
- Trình duyệt tự gửi cookie theo mọi yêu cầu cùng domain; **giao diện không cần và không được** tự lưu token trong `localStorage`.
- Phiên hết hạn sau **30 phút không hoạt động**; mỗi yêu cầu hợp lệ gia hạn lại 30 phút.
- Hết phiên → `401 HET_PHIEN`. Giao diện chuyển về trang đăng nhập và giữ lại đường dẫn đang mở để quay lại.
- `POST /api/v1/auth/logout` → hủy phiên ở máy chủ, xóa cookie, trả `204`.
- Tài khoản bị khóa hoặc đổi vai trò: các phiên đang mở của tài khoản đó bị hủy hoặc áp dụng quyền mới **từ yêu cầu kế tiếp** (AUTH-08, AUTH-10).

### 9.2. Bắt buộc đổi mật khẩu

Khi `batBuocDoiMatKhau = true` (tài khoản mới, vừa được đặt lại mật khẩu): mọi API trả `403 PHAI_DOI_MAT_KHAU`, **trừ** `POST /auth/change-password`, `POST /auth/logout` và `GET /me`.

### 9.3. Chống CSRF

Mọi yêu cầu `POST`, `PUT`, `DELETE` phải gửi header **`X-XSRF-TOKEN`**, lấy từ cookie `XSRF-TOKEN` máy chủ cấp khi đăng nhập. Hàm gọi API dùng chung trong `src/client/src/api/` tự gắn header này; không ai phải viết lại.

### 9.4. Phân quyền

- Quyền kiểm tra **ở máy chủ cho mọi endpoint** (BR-AUTH-03), theo ma trận actor – use case (SRS 3.14).
- Ký hiệu vai trò trong tài liệu: **AD** Admin/Quản lý · **TN** Thu ngân · **NK** Nhân viên kho · **Công khai** (không cần đăng nhập, chỉ có `/auth/login`).
- Ẩn dữ liệu theo vai trò, ví dụ giá vốn chỉ AD xem (BR-SP-02): máy chủ **trả `null`** cho trường đó với vai trò không được xem; ghi rõ trong cột Ghi chú của tài liệu API.
- Chỉ được xem dữ liệu của chính mình (ví dụ hồ sơ cá nhân): lấy người dùng từ phiên, **không** nhận `id` người dùng từ giao diện.
- **Mã quyền:** máy chủ kiểm tra theo mã quyền (PA-AUTH-04.A), không theo tên vai trò. Dùng bộ 27 mã ở `auth-giai-thich.md` mục 6.4, khai báo một lần trong `Features/Auth/BangQuyen.cs`, gắn vào endpoint bằng `[Authorize(Policy = MaQuyen.<Ten>)]`. Module nào cần mã quyền mới thì mở issue, #1 bổ sung vào bảng đó. Bộ mã đang ở trạng thái chờ xác nhận (hạn 12h 20/10/2026).

---

## 10. Sửa đồng thời và xử lý trùng

### 10.1. Hai người cùng sửa một bản ghi

Áp dụng cho bản ghi có thể bị nhiều người sửa (sản phẩm SP-04 E1, danh mục, nhà cung cấp, khách hàng…):

- Bản ghi trả về có trường **`phienBan`** (số nguyên, tăng 1 mỗi lần lưu).
- `PUT` phải gửi kèm `phienBan` đang có. Nếu khác giá trị hiện tại trong CSDL → `409 XUNG_DOT_DU_LIEU`, message "Dữ liệu đã được người khác thay đổi, vui lòng tải lại." Giao diện tải lại bản ghi.

### 10.2. Chứng từ đã được xử lý

Duyệt, từ chối, xác nhận, hủy một chứng từ: câu lệnh cập nhật kèm điều kiện trạng thái hiện tại (`... WHERE id = ? AND trang_thai = 'CHO_DUYET'`). Không có dòng nào được cập nhật → `409 DA_DUOC_XU_LY` (KHO-04 E1, KHO-08 E2).

### 10.3. Bấm gửi hai lần

Thao tác tạo chứng từ (hoàn tất đơn POS-07, xác nhận nhập kho NH-06…) **không được tạo hai chứng từ** khi người dùng bấm hai lần hoặc mạng gửi lại:

- Giao diện: khóa nút ngay khi bấm, mở lại khi có phản hồi.
- Máy chủ: kiểm tra trạng thái như mục 10.2 (phiếu nhập đã xác nhận, đơn đã hoàn tất thì trả `409 DA_DUOC_XU_LY`).

---

## 11. Nghiệp vụ thay đổi tồn kho

- **Không có endpoint nào ghi trực tiếp vào bảng tồn kho** (BR-SP-04). Mọi thay đổi tồn đi qua dịch vụ "Ghi nhận biến động kho" của #1.
- Endpoint tạo chứng từ (hóa đơn, phiếu nhập, kiểm kê…) gọi dịch vụ kho **trong cùng giao tác CSDL** với việc lưu chứng từ. Dịch vụ báo lỗi → hủy toàn bộ giao tác (rollback), trả lỗi theo bảng mục 3.5 của tài liệu dịch vụ kho, chứng từ giữ trạng thái cũ.
- `GET /inventory` và các báo cáo chỉ đọc dữ liệu tồn.

---

## 12. Viết tài liệu API cho module

### 12.1. File và phạm vi

- Mỗi người viết **`docs/api/<module>.md`** cho module mình sở hữu: `auth.md`, `products.md`, `purchasing.md`, `inventory.md`, `sales.md`, `customers.md`, `reports.md`, `ai.md`.
- **Chỉ viết chi tiết cho use case trong MVP.** Use case tầng 2 và "Sau MVP" chỉ cần một dòng trong bảng, cột Ghi chú ghi "Sau MVP".
- Khi code xong, **Swagger (`/swagger`) là bản chính thức**; file `.md` giữ vai trò hợp đồng ban đầu và giải thích nghiệp vụ. Nếu hai bên lệch nhau: sửa file `.md` theo code trong cùng PR.

### 12.2. Bảng tổng hợp endpoint

Đầu mỗi file có bảng (giống mẫu Tài liệu dịch vụ kho mục 8.4):

| Phương thức và đường dẫn | Use case | Quyền | Mã thành công | Mã lỗi riêng | Ghi chú |
|---|---|---|---|---|---|
| `GET /products` | SP-01 | AD, TN, NK | 200 | – | TN chỉ thấy sản phẩm đang kinh doanh; `giaVon` = null với TN, NK |
| `POST /products` | SP-03 | AD, NK | 201 | `TRUNG_MA_SAN_PHAM` | Tạo kèm bản ghi giá đầu tiên |

### 12.3. Chi tiết từng endpoint MVP

Mỗi endpoint trong MVP có một mục theo mẫu:

````markdown
### POST /api/v1/products – Thêm sản phẩm (SP-03)

**Quyền:** AD, NK
**Quy tắc:** BR-SP-03, BR-SP-04, BR-SP-05, BR-SP-10

**Yêu cầu**
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

| Trường | Bắt buộc | Ràng buộc |
|---|---|---|
| `ma` | Không | Để `null` thì máy chủ tự sinh; tối đa 20 ký tự; không trùng |
| `tenSanPham` | Có | 1–200 ký tự |
| `danhMucId` | Có | Danh mục đang hoạt động |
| `donViTinh` | Có | 1–20 ký tự |
| `giaBan` | Có | Số nguyên > 0, bội số của 100 |
| `nguongToiThieu` | Không | Số nguyên ≥ 0, mặc định 5 |

**Phản hồi `201 Created`**: bản ghi sản phẩm (mục 6.1), `tonKho = 0`.

**Lỗi**
| HTTP | Mã | Khi nào (đặc tả) |
|---|---|---|
| 409 | `TRUNG_MA_SAN_PHAM` | E1 – Mã sản phẩm đã tồn tại |
| 422 | `DU_LIEU_KHONG_HOP_LE` | E2 – Thiếu trường, giá không hợp lệ |
| 403 | `KHONG_DU_QUYEN` | Vai trò TN |
````

> Ảnh sản phẩm (SP-03 E3) gửi riêng qua `POST /products/{id}/image` dạng `multipart/form-data`, JPG/PNG ≤ 2 MB; không nhúng ảnh vào JSON.

### 12.4. Trong code

- Mỗi module một nhóm controller, gắn **tag Swagger theo mã module** (`SP`, `NH`, `POS`…).
- `summary` của endpoint bắt đầu bằng mã use case: `"SP-03 Thêm sản phẩm"`.
- Không trả thẳng entity của EF Core ra ngoài; dùng **DTO** riêng cho yêu cầu và phản hồi (tránh lộ trường như mật khẩu băm, và tránh vòng lặp khi tuần tự hóa JSON).
- Xử lý lỗi, phân trang, định dạng lỗi viết **một lần** trong `Common/` (#2 làm trong T3-11); module chỉ ném lỗi nghiệp vụ kèm mã, không tự dựng JSON lỗi.

---

## 13. Thay đổi API sau khi đã chốt

- **Thay đổi không phá vỡ** (thêm endpoint mới, thêm trường mới vào phản hồi, thêm tham số lọc không bắt buộc): làm trong PR như bình thường, cập nhật tài liệu module.
- **Thay đổi phá vỡ** (đổi tên hoặc bỏ trường, đổi kiểu dữ liệu, đổi đường dẫn, thêm trường bắt buộc vào yêu cầu, đổi ý nghĩa mã lỗi):
  1. Mở issue nhãn `thay đổi yêu cầu`, nêu lý do và người bị ảnh hưởng.
  2. Người dùng API đó (thường là người làm giao diện hoặc module gọi sang) xác nhận trong issue.
  3. #1 duyệt; sửa tài liệu và code trong cùng PR, PR ghi `Closes #<issue>`.
- Đồ án chỉ có `v1`; **không** tạo `/api/v2`.

---

## 14. Checklist review PR có API

Người review đánh dấu trong PR:

- [ ] Đường dẫn đúng bảng tên tài nguyên (mục 2.2); hành động viết dạng `POST /{id}/{động-từ}`
- [ ] Phương thức và mã thành công đúng mục 3 (`201` + `Location` khi tạo, `204` khi xóa)
- [ ] Tên trường camelCase không dấu, enum chữ hoa, tiền là số nguyên, thời gian có `+07:00`
- [ ] Danh sách trả `{ items, total, page, size }`
- [ ] Lỗi đúng dạng `{ code, message, details, traceId }`; mã lỗi mới đã đăng ký ở mục 8.4; message khớp đặc tả
- [ ] Có kiểm tra quyền ở máy chủ, đúng ma trận actor; trường bị ẩn theo vai trò trả `null`
- [ ] Bản ghi sửa được có `phienBan`; chứng từ chuyển trạng thái có điều kiện trạng thái
- [ ] Thay đổi tồn kho chỉ qua dịch vụ kho, trong cùng giao tác
- [ ] Không trả entity EF Core, không lộ mật khẩu băm, không lộ lỗi gốc ở 500
- [ ] Swagger hiển thị đúng; `docs/api/<module>.md` đã cập nhật
- [ ] Có test API cho luồng chính và ít nhất một ngoại lệ (với use case Must)

---

## 15. Thay đổi so với bản nháp trong tài liệu dịch vụ kho

Bản nháp ở Tài liệu dịch vụ kho v1.1 mục 8.3–8.5 vẫn đúng tinh thần; các điểm dưới đây được chốt hoặc sửa. #1 cập nhật bảng endpoint AUTH, KHO cho khớp.

| Điểm | Bản nháp | Chốt trong quy ước này |
|---|---|---|
| Đường dẫn tiếng Anh hay tiếng Việt | Đang để mở | **Tiếng Anh**, số nhiều, kebab-case (mục 2) |
| Tham số lọc `GET /accounts` | `keyword, role, status` | `keyword, vaiTro, trangThai` (lọc bằng đúng tên trường) |
| Tham số thời gian thẻ kho `GET /inventory/{productId}/stock-card` | `from, to` | `tuNgay, denNgay` |
| Đường dẫn thẻ kho | `/inventory/{productId}/stock-card` | Giữ nguyên |
| Xác thực | "token, phiên 30 phút" (issue T3-04) | **Cookie phiên HttpOnly**, không dùng token (ADR-001) |
| Lỗi | `{ code, message, details }` | Thêm `traceId` (mục 8.1) |
| Lỗi 400 | Chưa có | Thêm cho yêu cầu sai cú pháp (mục 8.2) |
| Lỗi bắt buộc đổi mật khẩu | Chưa có | `403 PHAI_DOI_MAT_KHAU` (mục 9.2) |
| Sửa đồng thời | Ghi trong lưu ý SP-04 | Trường `phienBan` + `409 XUNG_DOT_DU_LIEU` cho mọi bản ghi sửa được (mục 10.1) |
