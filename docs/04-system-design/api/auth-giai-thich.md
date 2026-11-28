# Giải thích hợp đồng API module Xác thực và phân quyền (AUTH)

| | |
|---|---|
| **Đi kèm** | `docs/api/openapi-auth.yaml` (OpenAPI 3.0.3) |
| **Issue** | T3-05 – OpenAPI module AUTH |
| **Người viết** | #1 Phạm Gia Thăng · 16/10/2026 |
| **Căn cứ** | Đặc tả Use Case 13/10 (AUTH-01 → AUTH-10, PA-AUTH-01 → 05, mục 13 ma trận quyền); ERD Tài khoản-Vai trò v1 (18/10/2026); `API_CONVENTIONS.md` v1.0; ADR-001 |
| **Cập nhật** | 20/10/2026 · #1: ghi kết quả 13 điểm ở mục 8 (cột "Kết quả"); 4 mã lỗi mới đã đăng ký vào `API_CONVENTIONS.md` mục 8.4; bộ mã quyền được dẫn ở `API_CONVENTIONS.md` mục 9.4; thêm mục 9.1 tài khoản Admin đầu tiên (#1 làm); cập nhật mục 10. Các kết quả chưa ghi "Đã chốt" đang chờ nhóm xác nhận |

File này giải thích **mọi thành phần và mọi mã** xuất hiện trong `openapi-auth.yaml`, để các thành viên đọc hiểu được file YAML, viết OpenAPI cho module của mình theo cùng cách, và biết những điểm còn cần nhóm chốt.

---

## Mục lục

1. [Tài liệu hiện có đã đủ để viết OpenAPI chưa](#1-tài-liệu-hiện-có-đã-đủ-để-viết-openapi-chưa)
2. [Cách mở và xem file](#2-cách-mở-và-xem-file)
3. [Đọc cấu trúc file YAML](#3-đọc-cấu-trúc-file-yaml)
4. [Danh sách endpoint](#4-danh-sách-endpoint)
5. [Giải thích từng endpoint](#5-giải-thích-từng-endpoint)
6. [Từ điển mã](#6-từ-điển-mã)
7. [Ánh xạ trường JSON và cột CSDL](#7-ánh-xạ-trường-json-và-cột-csdl)
8. [Các quyết định thiết kế và điểm cần nhóm chốt](#8-các-quyết-định-thiết-kế-và-điểm-cần-nhóm-chốt)
9. [Gợi ý khi lập trình bằng ASP.NET Core](#9-gợi-ý-khi-lập-trình-bằng-aspnet-core)
10. [Việc tiếp theo](#10-việc-tiếp-theo)

---

## 1. Tài liệu hiện có đã đủ để viết OpenAPI chưa

**Đủ cho module AUTH**, vì có đủ 4 nguồn mà một bản OpenAPI cần:

| OpenAPI cần | Lấy từ |
|---|---|
| Có những endpoint nào, phương thức gì, đường dẫn gì | Bảng endpoint nháp (Tài liệu dịch vụ kho 8.4) + quy ước đặt đường dẫn (`API_CONVENTIONS` mục 2, 3) |
| Gửi lên trường gì, trả về trường gì, kiểu dữ liệu, ràng buộc | Luồng chính của đặc tả use case + từ điển dữ liệu ERD bảng `tai_khoan`, `vai_tro` |
| Lỗi nào, mã HTTP nào, câu thông báo nào | Ngoại lệ (E1, E2…) của đặc tả + bảng mã lỗi (`API_CONVENTIONS` mục 8) |
| Ai được gọi | Ma trận actor – use case (Đặc tả mục 13) + quy ước xác thực (`API_CONVENTIONS` mục 9) |

**Những gì tài liệu chưa nói, mình phải tự đề xuất** (chi tiết ở mục 8): thứ tự kiểm tra khóa tạm khi đăng nhập (vấn đề mở số 8 trong SRS), định dạng tên đăng nhập và số điện thoại, bộ mã quyền, cột `phien_ban` cho bảng `tai_khoan`, và 4 mã lỗi mới.

Với các module khác (SP, NH, POS, BC, KH), tài liệu cũng đủ theo cùng cách, **miễn là ERD phần của module đó đã xong**. ERD cho biết tên trường và kiểu dữ liệu; thiếu ERD thì OpenAPI sẽ phải đoán.

---

## 2. Cách mở và xem file

| Cách | Làm thế nào | Khi nào dùng |
|---|---|---|
| **Swagger Editor** (trên web) | Mở `editor.swagger.io`, dán toàn bộ nội dung file vào khung bên trái | Xem nhanh, bên phải hiện giao diện tài liệu có thể bấm vào từng endpoint |
| **VS Code** | Cài tiện ích "OpenAPI (Swagger) Editor" hoặc "Swagger Viewer", mở file và bấm xem trước | Khi sửa file, có báo lỗi cú pháp ngay |
| **Trong dự án** | Khi máy chủ chạy, mở `/swagger` | Sau khi code xong; đây là bản chính thức |

File đã được kiểm tra bằng công cụ `openapi-spec-validator`: **hợp lệ theo chuẩn OpenAPI 3.0.3**, gồm 11 đường dẫn và 14 thao tác.

---

## 3. Đọc cấu trúc file YAML

### 3.1. Cú pháp YAML cơ bản

| Ký hiệu | Ý nghĩa | Ví dụ trong file |
|---|---|---|
| `khóa: giá trị` | Một cặp tên – giá trị | `operationId: dangNhap` |
| Thụt lề (2 dấu cách) | Thể hiện cấp cha – con. **Sai thụt lề là sai cả file**; không dùng phím Tab | `paths:` → `/auth/login:` → `post:` |
| `- ` đầu dòng | Một phần tử trong danh sách | `- $ref: '#/components/parameters/page'` |
| `[a, b, c]` | Danh sách viết trên một dòng | `enum: [AD, TN, NK]` |
| `{ a: 1, b: 2 }` | Đối tượng viết trên một dòng | `schema: { type: string }` |
| `\|` sau dấu hai chấm | Đoạn văn nhiều dòng, giữ nguyên xuống dòng | `description: \|` |
| `'...'` | Chuỗi đặt trong nháy đơn, bắt buộc khi giá trị dễ bị hiểu nhầm thành số hoặc có ký tự đặc biệt | `'200'`, `'0912345678'`, `'#/components/...'` |
| `#` | Chú thích, máy bỏ qua | `# ---------- AUTH-01` |

Mã HTTP như `'200'` phải để trong nháy, vì OpenAPI yêu cầu tên của phản hồi là chuỗi. Số điện thoại `'0912345678'` cũng vậy, nếu không YAML hiểu thành số và mất số 0 đầu.

### 3.2. Các phần chính của file

```
openapi      ← phiên bản chuẩn OpenAPI
info         ← tên, phiên bản, mô tả chung của API
servers      ← địa chỉ gốc của API
tags         ← nhóm các endpoint để hiển thị
security     ← cách xác thực mặc định
paths        ← danh sách endpoint (phần chính)
components   ← các phần dùng chung, được tham chiếu bằng $ref
```

| Khóa | Giải thích | Giá trị trong file AUTH |
|---|---|---|
| `openapi` | Phiên bản chuẩn OpenAPI mà file tuân theo. Chọn 3.0.3 vì Swagger của ASP.NET Core và hầu hết công cụ đọc tốt | `3.0.3` |
| `info.title`, `info.version` | Tên API và phiên bản của **tài liệu này** (không phải phiên bản `/api/v1`) | `1.0.0` |
| `info.description` | Mô tả chung: trạng thái, ưu tiên MoSCoW, cách xác thực, ký hiệu vai trò | |
| `servers.url` | Phần đầu của mọi đường dẫn. `/auth/login` thực tế là `/api/v1/auth/login` | `/api/v1` |
| `tags` | Nhóm endpoint khi hiển thị trên Swagger. File AUTH có 3 nhóm: Phiên đăng nhập, Hồ sơ cá nhân, Quản trị tài khoản | |
| `security` (cấp gốc) | Mặc định mọi endpoint **cần đăng nhập** bằng cookie. Endpoint nào khác thì khai báo lại | `- cookieAuth: []` |

### 3.3. Một endpoint gồm những gì

Lấy `POST /accounts` (AUTH-06) làm ví dụ:

| Khóa | Giải thích |
|---|---|
| `/accounts:` → `post:` | Đường dẫn và phương thức HTTP |
| `tags` | Endpoint thuộc nhóm nào trên Swagger |
| `operationId` | Tên duy nhất của thao tác, viết camelCase tiếng Việt không dấu (`taoTaiKhoan`). Công cụ sinh mã giao diện dùng tên này làm tên hàm |
| `summary` | Một dòng tiêu đề, **bắt đầu bằng mã use case** (`AUTH-06 Tạo tài khoản`) |
| `description` | Quyền, mức ưu tiên, căn cứ (use case, BR, PA) và cách xử lý đặc biệt |
| `security` | Ghi đè mặc định. `- cookieAuth: []` cùng `xsrfHeader: []` **trên cùng một gạch đầu dòng** nghĩa là cần **cả hai** (cookie phiên **và** header chống CSRF) |
| `parameters` | Tham số trên đường dẫn (`in: path`, ví dụ `{id}`) hoặc trên query string (`in: query`, ví dụ `?page=1`) |
| `requestBody` | Dữ liệu JSON gửi lên: `required` (bắt buộc có thân), `content.application/json.schema` (cấu trúc), `example` (ví dụ) |
| `responses` | Mỗi mã HTTP có thể trả về: `description` (khi nào), `headers` (header trả kèm), `content` (cấu trúc JSON), `example`/`examples` (ví dụ) |

### 3.4. `$ref` – tham chiếu đến phần dùng chung

`$ref: '#/components/schemas/TaiKhoan'` nghĩa là "lấy định nghĩa ở `components` → `schemas` → `TaiKhoan`". Dấu `#` là "trong chính file này". Nhờ đó cấu trúc `TaiKhoan` chỉ viết một lần nhưng dùng ở 7 endpoint; sửa một chỗ là đổi tất cả.

### 3.5. Phần `components`

| Mục | Chứa gì | Trong file AUTH |
|---|---|---|
| `securitySchemes` | Các cách xác thực | `cookieAuth` (cookie `cs_session`), `xsrfHeader` (header `X-XSRF-TOKEN`) |
| `parameters` | Tham số dùng lại nhiều lần | `page`, `size`, `idTaiKhoan` |
| `responses` | Phản hồi lỗi dùng lại nhiều lần | `ChuaDangNhap` (401), `KhongDuQuyen` (403), `KhongTimThay` (404), `XungDotHoacTrung` (409), `DuLieuKhongHopLe` (422), `LoiMayChu` (500) |
| `schemas` | Cấu trúc dữ liệu (đối tượng JSON, giá trị liệt kê) | 24 schema, giải thích ở mục 6.6 |

### 3.6. Các từ khóa mô tả dữ liệu (trong `schema`)

| Từ khóa | Ý nghĩa | Ví dụ trong file |
|---|---|---|
| `type` | Kiểu dữ liệu: `string`, `integer`, `number`, `boolean`, `array`, `object` | `type: integer` |
| `format` | Định dạng chi tiết hơn của kiểu | `int64` (số nguyên lớn), `date-time` (thời điểm ISO 8601), `email`, `password` (ô nhập bị che trên Swagger) |
| `required` | Danh sách trường **bắt buộc phải có mặt** trong JSON | `required: [tenDangNhap, matKhau]` |
| `properties` | Danh sách các trường của một đối tượng | |
| `nullable: true` | Trường được phép mang giá trị `null` | `email`, `soDienThoai` |
| `enum` | Chỉ nhận một trong các giá trị liệt kê | `enum: [HOAT_DONG, DA_KHOA]` |
| `minLength`, `maxLength` | Độ dài chuỗi tối thiểu, tối đa | `maxLength: 50` cho tên đăng nhập (khớp `VARCHAR(50)`) |
| `minimum` | Giá trị số tối thiểu | `minimum: 1` cho `page` |
| `default` | Giá trị dùng khi không gửi | `default: 20` cho `size` |
| `pattern` | Biểu thức chính quy (regex) chuỗi phải khớp | Xem bảng 3.7 |
| `items` | Kiểu của từng phần tử trong mảng | `items: { $ref: '#/components/schemas/MaQuyen' }` |
| `oneOf` | Giá trị khớp **một trong** các cấu trúc liệt kê | `details` của lỗi: mảng lỗi theo trường **hoặc** đối tượng |
| `additionalProperties: true` | Đối tượng được có thêm trường tùy ý | `details` của lỗi khóa tạm có `conLaiPhut`, `khoaTamDen` |
| `example`, `examples` | Ví dụ minh họa (một hoặc nhiều ví dụ có tên). Không ảnh hưởng hoạt động, chỉ để đọc | |
| `description` | Giải thích cho người đọc | |

### 3.7. Các biểu thức chính quy (pattern)

| Pattern | Dùng cho | Giải nghĩa |
|---|---|---|
| `^(?=.*[A-Za-z])(?=.*\d).{8,128}$` | Mật khẩu mới, mật khẩu tạm (BR-AUTH-04) | `(?=.*[A-Za-z])`: có ít nhất 1 chữ cái · `(?=.*\d)`: có ít nhất 1 chữ số · `.{8,128}`: dài 8–128 ký tự |
| `^[A-Za-z0-9._]{3,50}$` | Tên đăng nhập (đề xuất) | Chỉ gồm chữ không dấu, số, dấu chấm, gạch dưới; dài 3–50 |
| `^0\d{9}$` | Số điện thoại (đề xuất) | Bắt đầu bằng số 0, theo sau 9 chữ số; tổng 10 số |

`^` là đầu chuỗi, `$` là cuối chuỗi. Điều kiện "khác mật khẩu hiện tại" không viết được bằng pattern; máy chủ kiểm tra trong code.

---

## 4. Danh sách endpoint

Đường dẫn đầy đủ thêm `/api/v1` phía trước.

| # | Phương thức và đường dẫn | Use case | Quyền | Ưu tiên | Thành công |
|---|---|---|---|---|---|
| 1 | `POST /auth/login` | AUTH-01 | Công khai | Must | 200 |
| 2 | `POST /auth/logout` | AUTH-02 | AD, TN, NK | Must | 204 |
| 3 | `POST /auth/change-password` | AUTH-03 | AD, TN, NK | Must | 204 |
| 4 | `GET /me` | AUTH-04 (xem) | AD, TN, NK | MVP | 200 |
| 5 | `PUT /me` | AUTH-04 (sửa) | AD, TN, NK | Could – Sau MVP | 200 |
| 6 | `GET /accounts` | AUTH-05 | AD | Must | 200 |
| 7 | `POST /accounts` | AUTH-06 (+ AUTH-10) | AD | Must | 201 |
| 8 | `GET /accounts/{id}` | Hỗ trợ AUTH-07 → 10 | AD | Must | 200 |
| 9 | `PUT /accounts/{id}` | AUTH-07 | AD | Should – tầng 2 | 200 |
| 10 | `POST /accounts/{id}/lock` | AUTH-08 | AD | Should – MVP | 200 |
| 11 | `POST /accounts/{id}/unlock` | AUTH-08 | AD | Should – MVP | 200 |
| 12 | `POST /accounts/{id}/reset-password` | AUTH-09 | AD | Should – tầng 2 | 200 |
| 13 | `PUT /accounts/{id}/role` | AUTH-10 | AD | Must | 200 |
| 14 | `GET /roles` | AUTH-10 | AD | Must | 200 |

**Tuần 4 code trước:** #1, #6, #13, #14 (nền phân quyền cho cả nhóm), sau đó #2, #3, #7, #4, #6.

---

## 5. Giải thích từng endpoint

### 5.1. `POST /auth/login` – Đăng nhập (AUTH-01)

Gửi `{ tenDangNhap, matKhau }`. Máy chủ xử lý theo thứ tự:

```
1. Thiếu trường ............................................ 422 DU_LIEU_KHONG_HOP_LE   (E1)
2. Không có tên đăng nhập .................................. 401 SAI_THONG_TIN_DANG_NHAP (E2)
3. Đang khóa tạm (khoa_tam_den > bây giờ) .................. 423 TAI_KHOAN_KHOA_TAM      (E3)
      → không kiểm tra mật khẩu, không tăng bộ đếm
4. Sai mật khẩu → so_lan_sai + 1
      chưa đủ 5 ............................................ 401 SAI_THONG_TIN_DANG_NHAP (E2)
      đủ 5 → khoa_tam_den = bây giờ + 15 phút ............. 423 TAI_KHOAN_KHOA_TAM      (BR-AUTH-11)
5. Đúng mật khẩu nhưng trang_thai = DA_KHOA ................ 423 TAI_KHOAN_BI_KHOA       (E4)
6. Thành công: so_lan_sai = 0, xóa khoa_tam_den, ghi lan_dang_nhap_cuoi,
   tạo dòng phien_dang_nhap, đặt cookie cs_session + XSRF-TOKEN ... 200
```

Phản hồi 200 gồm thông tin người dùng, danh sách quyền, cờ `batBuocDoiMatKhau` và `manHinhChinh` (AD → `DASHBOARD`, TN → `POS`, NK → `KHO`) để giao diện chuyển đúng màn hình.

Không có tên đăng nhập và sai mật khẩu đều trả **cùng một câu** "Tên đăng nhập hoặc mật khẩu không đúng" (E2), để người ngoài không dò được tên đăng nhập nào có thật.

### 5.2. `POST /auth/logout` – Đăng xuất (AUTH-02)

Hủy phiên ở máy chủ (điền `huy_luc` của dòng `phien_dang_nhap`), xóa hai cookie, trả `204` không có nội dung. Luồng A1 (thu ngân còn đơn đang bán) là việc của giao diện: hỏi xác nhận, gọi API hủy đơn của module POS (POS-09), rồi mới gọi đăng xuất.

### 5.3. `POST /auth/change-password` – Đổi mật khẩu (AUTH-03)

Gửi `{ matKhauHienTai, matKhauMoi, nhapLaiMatKhauMoi }`.
- Đang bị bắt buộc đổi (lần đầu đăng nhập, vừa được Admin đặt lại – A1): `matKhauHienTai` gửi `null`.
- Sai mật khẩu hiện tại → `422 SAI_MAT_KHAU_HIEN_TAI` (E1).
- Mật khẩu mới không đạt chính sách hoặc trùng mật khẩu cũ (E2), hai lần nhập không khớp (E3) → `422 DU_LIEU_KHONG_HOP_LE` kèm lỗi từng ô.
- Thành công: lưu bản băm mới, tắt cờ bắt buộc đổi, trả `204`.

### 5.4. `GET /me` và `PUT /me` – Hồ sơ cá nhân (AUTH-04)

- `GET /me`: giao diện gọi **mỗi lần tải trang** để biết ai đang đăng nhập, vai trò và danh sách quyền (dựng menu). Vì vậy thuộc MVP dù AUTH-04 là Could.
- `PUT /me`: chỉ sửa `hoTen`, `soDienThoai`, `email` (BR-AUTH-05). Thuộc phần Could, làm sau MVP.
- Người dùng luôn lấy từ phiên, không có tham số `id`.

### 5.5. `GET /accounts` – Danh sách tài khoản (AUTH-05)

Tham số: `keyword` (tìm trong tên đăng nhập, họ tên), `vaiTro`, `trangThai`, `sort`, `page`, `size`. Trả dạng `{ items, total, page, size }`. Không có kết quả vẫn trả `200` với `items` rỗng; câu "Không tìm thấy tài khoản phù hợp" (E1) do giao diện hiển thị.

### 5.6. `POST /accounts` – Tạo tài khoản (AUTH-06 gồm AUTH-10)

Gửi tên đăng nhập, họ tên, số điện thoại, email (tùy chọn), mật khẩu tạm, vai trò. Tài khoản mới `HOAT_DONG`, `batBuocDoiMatKhau = true`. Trả `201` kèm header `Location`. Trùng tên đăng nhập → `409 TRUNG_TEN_DANG_NHAP` (E1); trùng số điện thoại hoặc email → `409 TRUNG_DU_LIEU`; sai định dạng → `422` (E2).

### 5.7. `GET /accounts/{id}` – Xem một tài khoản

Không có use case riêng; cần để mở biểu mẫu sửa, khóa, đặt lại mật khẩu, gán vai trò.

### 5.8. `PUT /accounts/{id}` – Sửa tài khoản (AUTH-07)

Sửa họ tên, số điện thoại, email; gửi kèm `phienBan`. **Không gồm vai trò** – đổi vai trò dùng endpoint #13 (lý do ở mục 8). `phienBan` không khớp → `409 XUNG_DOT_DU_LIEU`.

### 5.9. `POST /accounts/{id}/lock` và `/unlock` – Khóa, mở khóa (AUTH-08)

- **Khóa:** gửi `{ lyDo }` (bắt buộc). Đặt `DA_KHOA`, hủy mọi phiên của tài khoản đó. Từ chối khi tự khóa mình (`KHONG_THAO_TAC_CHINH_MINH`), khóa Admin hoạt động cuối cùng (`MAT_ADMIN_CUOI_CUNG`), hoặc tài khoản đã khóa sẵn (`DA_DUOC_XU_LY`).
- **Mở khóa:** không cần thân yêu cầu. Gỡ **cả hai loại khóa**: khóa của Admin và khóa tạm do sai mật khẩu (A1); đặt bộ đếm sai về 0. Không bị khóa loại nào → `409 DA_DUOC_XU_LY`.

### 5.10. `POST /accounts/{id}/reset-password` – Đặt lại mật khẩu (AUTH-09)

Gửi `{ matKhauTam }` hoặc để trống cho máy chủ tự sinh. Lưu bản băm, bật cờ bắt buộc đổi, hủy phiên cũ. **Mật khẩu tạm chỉ trả về một lần** trong phản hồi; kèm header `Cache-Control: no-store` để trình duyệt không lưu. Tài khoản đang bị Admin khóa → `409 TAI_KHOAN_DANG_KHOA` (E1).

### 5.11. `PUT /accounts/{id}/role` và `GET /roles` – Vai trò (AUTH-10)

- `PUT .../role`: gửi `{ vaiTro }`. Có hiệu lực từ yêu cầu kế tiếp của tài khoản đó, vì máy chủ đọc vai trò từ CSDL theo phiên ở mỗi yêu cầu. Làm hệ thống mất Admin cuối cùng → `409 MAT_ADMIN_CUOI_CUNG`.
- `GET /roles`: chỉ đọc 3 vai trò và danh sách quyền cài sẵn (PA-AUTH-04.A).

---

## 6. Từ điển mã

### 6.1. Mã trạng thái HTTP

| Mã | Tên | Ý nghĩa trong module AUTH |
|---|---|---|
| `200` | OK | Thành công, có dữ liệu trả về |
| `201` | Created | Đã tạo tài khoản mới; header `Location` chỉ tới tài khoản đó |
| `204` | No Content | Thành công, không có dữ liệu trả về (đăng xuất, đổi mật khẩu) |
| `401` | Unauthorized | Chưa đăng nhập, hết phiên, hoặc đăng nhập sai |
| `403` | Forbidden | Đã đăng nhập nhưng không có quyền, hoặc đang bị bắt buộc đổi mật khẩu |
| `404` | Not Found | Không có tài khoản với `id` đó |
| `409` | Conflict | Xung đột với dữ liệu hoặc trạng thái hiện tại (trùng, mất Admin cuối, đã xử lý…) |
| `422` | Unprocessable Entity | Dữ liệu sai quy tắc (thiếu trường, sai định dạng, mật khẩu yếu…) |
| `423` | Locked | Tài khoản đang bị khóa (tạm hoặc bởi Admin) |
| `500` | Internal Server Error | Lỗi máy chủ, lỗi CSDL (AUTH-01 E5) |

### 6.2. Mã lỗi

**Mã chung** (đã có trong `API_CONVENTIONS` mục 8.2):

| Mã | HTTP | Gặp ở endpoint | Câu thông báo |
|---|---|---|---|
| `CHUA_DANG_NHAP` | 401 | Mọi endpoint trừ đăng nhập | Vui lòng đăng nhập. |
| `HET_PHIEN` | 401 | Mọi endpoint trừ đăng nhập | Phiên làm việc đã hết hạn, vui lòng đăng nhập lại. |
| `SAI_THONG_TIN_DANG_NHAP` | 401 | #1 | Tên đăng nhập hoặc mật khẩu không đúng. |
| `KHONG_DU_QUYEN` | 403 | #6 → #14 khi không phải AD | Bạn không có quyền thực hiện thao tác này. |
| `PHAI_DOI_MAT_KHAU` | 403 | Mọi endpoint trừ #2, #3, #4 khi cờ bắt buộc đổi đang bật | Vui lòng đổi mật khẩu trước khi tiếp tục. |
| `KHONG_TIM_THAY` | 404 | #8 → #13 | Không tìm thấy tài khoản. |
| `XUNG_DOT_DU_LIEU` | 409 | #5, #9 | Dữ liệu đã được người khác thay đổi, vui lòng tải lại. |
| `DA_DUOC_XU_LY` | 409 | #10, #11 | Tài khoản đã ở trạng thái này. |
| `TRUNG_DU_LIEU` | 409 | #5, #7, #9 | Số điện thoại (hoặc email) đã được dùng cho tài khoản khác. |
| `DU_LIEU_KHONG_HOP_LE` | 422 | Mọi endpoint có dữ liệu gửi lên | Dữ liệu chưa hợp lệ, vui lòng kiểm tra lại. |
| `TAI_KHOAN_BI_KHOA` | 423 | #1 | Tài khoản đã bị khóa, vui lòng liên hệ quản lý. |
| `LOI_MAY_CHU` | 500 | Mọi endpoint | Hệ thống đang gặp sự cố, vui lòng thử lại sau. |

**Mã riêng module AUTH:**

| Mã | HTTP | Endpoint | Căn cứ | Trạng thái |
|---|---|---|---|---|
| `TRUNG_TEN_DANG_NHAP` | 409 | #7 | AUTH-06 E1, BR-AUTH-06 | Đã đăng ký |
| `MAT_ADMIN_CUOI_CUNG` | 409 | #10, #13 | AUTH-08 E1, AUTH-10 E1, BR-AUTH-08 (E1 cũ của AUTH-07 đã chuyển sang AUTH-10, xem mục 8 điểm 3) | Đã đăng ký |
| `TAI_KHOAN_KHOA_TAM` | 423 | #1 | AUTH-01 E3, BR-AUTH-11 | Đã đăng ký 20/10 |
| `SAI_MAT_KHAU_HIEN_TAI` | 422 | #3 | AUTH-03 E1 | Đã đăng ký 20/10 |
| `KHONG_THAO_TAC_CHINH_MINH` | 409 | #10 | AUTH-08 E1 ("khóa chính mình") | Đã đăng ký 20/10 |
| `TAI_KHOAN_DANG_KHOA` | 409 | #12 | AUTH-09 E1 | Đã đăng ký 20/10 |

4 mã mới đã được thêm vào bảng mục 8.4 của `API_CONVENTIONS.md` (20/10/2026); đưa lên repo trong cùng PR với file OpenAPI.

**Vì sao tách `TAI_KHOAN_KHOA_TAM` và `TAI_KHOAN_BI_KHOA`:** giao diện xử lý khác nhau. Khóa tạm thì hiện số phút còn lại (lấy từ `details.conLaiPhut`) và cho thử lại sau; bị Admin khóa thì chỉ hướng dẫn liên hệ quản lý.

### 6.3. Giá trị liệt kê (enum)

| Schema | Giá trị | Ý nghĩa | Cột CSDL |
|---|---|---|---|
| `MaVaiTro` | `AD` | Admin/Quản lý | `vai_tro.ma` |
| | `TN` | Thu ngân | |
| | `NK` | Nhân viên kho | |
| `TrangThaiTaiKhoan` | `HOAT_DONG` | Đang hoạt động | `tai_khoan.trang_thai` |
| | `DA_KHOA` | Bị Admin khóa (AUTH-08) | |
| `ManHinhChinh` | `DASHBOARD` | Màn hình chính của AD | Không lưu CSDL, suy ra từ vai trò |
| | `POS` | Màn hình chính của TN | |
| | `KHO` | Màn hình chính của NK | |

Khóa tạm do sai mật khẩu **không** là một giá trị trạng thái; nó thể hiện bằng `khoaTamDen` có giá trị lớn hơn thời điểm hiện tại.

### 6.4. Mã quyền (`MaQuyen`)

Theo PA-AUTH-04.A, máy chủ kiểm tra theo **quyền**, không theo tên vai trò, để sau này nâng lên vai trò tùy chỉnh không phải sửa nghiệp vụ. Mỗi mã quyền tương ứng một dòng của ma trận Đặc tả mục 13. **Đây là đề xuất của #1**, đã được dẫn vào `API_CONVENTIONS.md` mục 9.4 (20/10/2026); trạng thái: chờ xác nhận (hạn 12h 20/10/2026). Mọi module dùng các mã này để gắn quyền vào API.

| Mã quyền | Use case | AD | TN | NK |
|---|---|:-:|:-:|:-:|
| `TAI_KHOAN_CA_NHAN` | AUTH-01 → 04 | ✓ | ✓ | ✓ |
| `QUAN_TRI_TAI_KHOAN` | AUTH-05 → 10 | ✓ | | |
| `SAN_PHAM_XEM` | SP-01, 02, 07 | ✓ | ✓ | ✓ |
| `SAN_PHAM_SUA` | SP-03, 04 | ✓ | | ✓ |
| `SAN_PHAM_GIA_DANH_MUC` | SP-05, 06, 08, 09 | ✓ | | |
| `TON_KHO_XEM` | KHO-01 | ✓ | ✓ (chỉ xem) | ✓ |
| `THE_KHO_CANH_BAO_XEM` | KHO-02, 07 | ✓ | | ✓ |
| `NGUONG_TON_SUA` | KHO-06 | ✓ | | |
| `KIEM_KE_XUAT_HUY_LAP` | KHO-03, 05 | | | ✓ |
| `KIEM_KE_XUAT_HUY_DUYET` | KHO-04, 08 | ✓ | | |
| `NHA_CUNG_CAP_QUAN_LY` | NH-01, 02 | ✓ | | ✓ |
| `NHA_CUNG_CAP_NGUNG` | NH-03 | ✓ | | |
| `PHIEU_NHAP_LAP` | NH-04, 05 | | | ✓ |
| `PHIEU_NHAP_XAC_NHAN` | NH-06 | ✓ | | ✓ |
| `PHIEU_NHAP_XEM` | NH-07, 08 | ✓ | | ✓ |
| `NHAP_HANG_LICH_SU_NCC` | NH-09 | ✓ | | |
| `BAN_HANG` | POS-01 → 09 | | ✓ | |
| `HOA_DON_XEM_TAT_CA` | POS-10, 11 | ✓ | | |
| `HOA_DON_XEM_CUA_MINH` | POS-10, 11 (chỉ hóa đơn mình lập) | | ✓ | |
| `HOA_DON_HUY` | POS-12 | ✓ | | |
| `BAO_CAO_XEM` | BC-01 → 12 | ✓ | | |
| `KHACH_HANG_QUAN_LY` | KH-01, 02, 03, 05 | ✓ | ✓ | |
| `KHACH_HANG_QUAN_TRI` | KH-04, 07, 08 | ✓ | | |
| `KHUYEN_MAI_XEM` | KM-01 | ✓ | ✓ (chỉ chương trình đang chạy) | |
| `KHUYEN_MAI_QUAN_LY` | KM-02 → 06 | ✓ | | |
| `DOI_TRA` | DT-01 → 05 | | ✓ | |
| `DOI_TRA_DUYET_CAU_HINH` | DT-06, 08 | ✓ | | |

Ghi chú: KH-06 (tích điểm) và POS-04 (áp khuyến mãi) do hệ thống tự làm trong luồng bán, không cần quyền riêng. DT-07 chưa có trong ma trận mục 13; đề xuất gắn `DOI_TRA` cho TN và `DOI_TRA_DUYET_CAU_HINH` cho AD, chờ nhóm xác nhận.

### 6.5. Mã tham chiếu tài liệu

| Ký hiệu | Nghĩa | Ví dụ |
|---|---|---|
| `AUTH-xx` | Mã use case module Xác thực và phân quyền | AUTH-01 Đăng nhập |
| `E1, E2…` | Ngoại lệ thứ n trong đặc tả use case | AUTH-01 E3: tài khoản đang khóa tạm |
| `A1, A2…` | Luồng thay thế thứ n | AUTH-03 A1: đổi mật khẩu bắt buộc |
| `BR-AUTH-xx` | Quy tắc nghiệp vụ module AUTH | BR-AUTH-04: chính sách mật khẩu |
| `PA-AUTH-xx.Y` | Phương án vận hành số xx, phương án Y được chọn | PA-AUTH-02.B: khóa tạm 15 phút, tự mở |
| `T3-05` | Mã issue trên GitHub | T3-05 OpenAPI module AUTH |

### 6.6. Tên các schema

| Schema | Nghĩa | Dùng ở |
|---|---|---|
| `Loi` | Phản hồi lỗi chung | Mọi phản hồi lỗi |
| `LoiTruong` | Một lỗi của một ô nhập | `Loi.details` |
| `MaVaiTro`, `TrangThaiTaiKhoan`, `ManHinhChinh`, `MaQuyen` | Giá trị liệt kê | Mục 6.3, 6.4 |
| `VaiTroTomTat` | Vai trò dạng ngắn `{ ma, ten }` | Trong `TaiKhoan`, `HoSoCaNhan` |
| `NguoiDungDangNhap` | Thông tin tối thiểu về người đang đăng nhập | Phản hồi đăng nhập |
| `YeuCauDangNhap` / `KetQuaDangNhap` | Thân yêu cầu / phản hồi của đăng nhập | #1 |
| `YeuCauDoiMatKhau` | Thân yêu cầu đổi mật khẩu | #3 |
| `HoSoCaNhan` / `YeuCauCapNhatHoSo` | Hồ sơ cá nhân / thân yêu cầu sửa hồ sơ | #4, #5 |
| `SoDienThoai` | Kiểu số điện thoại dùng chung (có pattern) | Hồ sơ, tạo và sửa tài khoản |
| `TaiKhoan` | Tài khoản đầy đủ như Admin thấy | #7 → #13 |
| `TrangTaiKhoan` | Một trang danh sách tài khoản `{ items, total, page, size }` | #6 |
| `YeuCauTaoTaiKhoan`, `YeuCauSuaTaiKhoan`, `YeuCauKhoaTaiKhoan`, `YeuCauDatLaiMatKhau`, `YeuCauGanVaiTro` | Thân yêu cầu của từng thao tác | #7, #9, #10, #12, #13 |
| `KetQuaDatLaiMatKhau` | Phản hồi có mật khẩu tạm | #12 |
| `VaiTro` / `DanhSachVaiTro` | Vai trò kèm quyền / danh sách vai trò | #14 |

Quy ước đặt tên schema: **PascalCase tiếng Việt không dấu**; thân yêu cầu bắt đầu bằng `YeuCau…`, phản hồi đặc biệt bắt đầu bằng `KetQua…`. Các module khác đặt tên theo cùng cách.

### 6.7. Cookie và header

| Tên | Loại | Ai đặt | Ý nghĩa |
|---|---|---|---|
| `cs_session` | Cookie | Máy chủ, khi đăng nhập | Chuỗi phiên ngẫu nhiên. `HttpOnly` (JavaScript không đọc được), `Secure` (chỉ gửi qua HTTPS), `SameSite=Strict` (không gửi từ trang web khác). Máy chủ lưu bản băm SHA-256 trong `phien_dang_nhap.token_bam` |
| `XSRF-TOKEN` | Cookie | Máy chủ, khi đăng nhập | Chuỗi chống CSRF. **Không** HttpOnly để giao diện đọc được và gửi lại |
| `X-XSRF-TOKEN` | Header yêu cầu | Giao diện | Giá trị lấy từ cookie `XSRF-TOKEN`; bắt buộc với POST/PUT/DELETE trừ đăng nhập |
| `Location` | Header phản hồi | Máy chủ | Đường dẫn tới tài nguyên vừa tạo (`201`) |
| `Cache-Control: no-store` | Header phản hồi | Máy chủ | Không cho lưu phản hồi chứa mật khẩu tạm |
| `Set-Cookie` | Header phản hồi | Máy chủ | Đặt hoặc xóa cookie |

---

## 7. Ánh xạ trường JSON và cột CSDL

| Trường JSON | Cột CSDL | Ghi chú |
|---|---|---|
| `id` | `tai_khoan.id` | |
| `tenDangNhap` | `ten_dang_nhap` | |
| *(không bao giờ trả về)* | `mat_khau_bam` | |
| `hoTen` | `ho_ten` | |
| `soDienThoai` | `so_dien_thoai` | |
| `email` | `email` | |
| `vaiTro.ma`, `vaiTro.ten` | `vai_tro.ma`, `vai_tro.ten` qua `vai_tro_id` | |
| `trangThai` | `trang_thai` | |
| `lyDoKhoa` | `ly_do_khoa` | |
| `batBuocDoiMatKhau` | `bat_buoc_doi_mk` | Tên cột viết tắt `mk`; tên trường JSON viết đủ theo quy ước "không viết tắt khó hiểu" |
| *(không trả về)* | `so_lan_sai` | Thông tin nội bộ chống dò mật khẩu |
| `khoaTamDen` | `khoa_tam_den` | |
| `lanDangNhapCuoi` | `lan_dang_nhap_cuoi` | |
| `ngayTao`, `ngayCapNhat` | `ngay_tao`, `ngay_cap_nhat` | Cột `DATETIME` không có múi giờ; API trả kèm `+07:00` |
| `phienBan` | `phien_ban` | Đã thêm vào ERD Tài khoản-Vai trò v1 (18/10/2026) |
| `quyen` | Không lưu CSDL | Khai báo trong mã nguồn theo vai trò (BR-AUTH-10) |

---

## 8. Các quyết định thiết kế và điểm cần nhóm chốt

Cột "Kết quả" ghi ngày 20/10/2026. Kết quả nào chưa ghi "Đã chốt" là kết quả đề xuất, chờ nhóm xác nhận trong issue đồng bộ (hạn 12h 20/10/2026).

| # | Điểm | Đề xuất trong file | Lý do | Cần ai chốt | Kết quả (20/10/2026) |
|---|---|---|---|---|---|
| 1 | Thứ tự kiểm tra khóa tạm khi đăng nhập (**vấn đề mở số 8** SRS 7.2) | Kiểm tra khóa tạm **trước** khi kiểm tra mật khẩu | Nếu kiểm tra mật khẩu trước, kẻ dò vẫn thử được mật khẩu trong lúc bị khóa. Cần sửa bước 4–5 của AUTH-01 cho khớp | Cả nhóm; #1 sửa đặc tả | Đã sửa Đặc tả Use Case 18/10 và SRS v1.0.3 (AUTH-01 bước 4–5); ERD Tài khoản-Vai trò v1 sửa cho khớp ngày 20/10. Mục A10 của Danh sách đề xuất. Chờ xác nhận (hạn 12h 20/10/2026) |
| 2 | Lần sai thứ 5 trả mã gì | `423 TAI_KHOAN_KHOA_TAM` (không phải 401) | Người dùng biết ngay bị khóa 15 phút, tránh thử tiếp vô ích | Cả nhóm | Giữ đề xuất: lần sai thứ 5 trả `423 TAI_KHOAN_KHOA_TAM`; mã đã đăng ký ở `API_CONVENTIONS` 8.4. Chờ xác nhận (hạn 12h 20/10/2026) |
| 3 | `PUT /accounts/{id}` có đổi vai trò không | **Không**; đổi vai trò chỉ qua `PUT /accounts/{id}/role` | Quy tắc "còn ít nhất một Admin" chỉ kiểm tra ở một chỗ. AUTH-07 trong đặc tả có nhắc vai trò; cần ghi chú "đổi vai trò thực hiện qua AUTH-10" | #1 | Giữ đề xuất. AUTH-07 chỉ sửa họ tên, số điện thoại, email; đổi vai trò qua AUTH-10; ngoại lệ "tự hạ vai trò khi là Admin cuối cùng" chuyển sang AUTH-10. Đã sửa Đặc tả Use Case 18/10 và SRS v1.0.3 ngày 20/10. Chờ xác nhận (hạn 12h 20/10/2026) |
| 4 | Cột `phien_ban` cho bảng `tai_khoan` | Thêm cột `phien_ban INT NOT NULL DEFAULT 1` | Theo `API_CONVENTIONS` mục 10.1 (phát hiện hai Admin cùng sửa). **Đã chốt 18/10: thêm `phien_ban INT UNSIGNED NOT NULL DEFAULT 1` vào ERD v1** | #1 (ERD) | **Đã chốt 18/10** (ERD v1: `phien_ban INT UNSIGNED NOT NULL DEFAULT 1`) |
| 5 | Định dạng tên đăng nhập | `^[A-Za-z0-9._]{3,50}$` | Đặc tả chưa quy định; tránh dấu cách, ký tự có dấu | Cả nhóm | Giữ đề xuất; đã có CHECK `ck_tai_khoan_ten` trong `schema_v1.sql`. Chờ xác nhận (hạn 12h 20/10/2026) |
| 6 | Định dạng số điện thoại | `^0\d{9}$` (10 số, bắt đầu bằng 0) | Đặc tả chưa quy định; dùng chung cho module Khách hàng | #1, #5 | Giữ đề xuất; đã có CHECK `ck_tai_khoan_sdt`, `VARCHAR(10)`, dùng chung với Khách hàng (mục B4.2 Danh sách đề xuất). Chờ xác nhận (hạn 12h 20/10/2026) |
| 7 | Bộ mã quyền | 27 mã ở mục 6.4 | PA-AUTH-04.A yêu cầu kiểm tra theo quyền; mọi module cần tên quyền để gắn vào API | Cả nhóm | Giữ 27 mã ở mục 6.4; DT-07 gắn `DOI_TRA` (TN) và `DOI_TRA_DUYET_CAU_HINH` (AD); dẫn vào `API_CONVENTIONS` 9.4. Chờ xác nhận (hạn 12h 20/10/2026) |
| 8 | 4 mã lỗi mới | Mục 6.2 | Giao diện cần phân biệt các trường hợp | #1 cập nhật `API_CONVENTIONS` | **Đã làm 20/10**: 4 mã đã đăng ký ở `API_CONVENTIONS` 8.4 |
| 9 | Mở khóa gộp hai loại khóa | `/unlock` gỡ cả khóa Admin và khóa tạm | Theo AUTH-08 A1; giao diện chỉ cần một nút | #1 | Giữ đề xuất: `/unlock` gỡ cả khóa Admin và khóa tạm, bộ đếm sai về 0. Chờ xác nhận (hạn 12h 20/10/2026) |
| 10 | Thêm `GET /accounts/{id}` | Có | Đặc tả không có use case "xem một tài khoản" nhưng biểu mẫu sửa cần dữ liệu hiện tại | #1 | Giữ đề xuất: có `GET /accounts/{id}` (quyền `QUAN_TRI_TAI_KHOAN`). Chờ xác nhận (hạn 12h 20/10/2026) |
| 11 | Thuật toán băm mật khẩu | `PasswordHasher` của ASP.NET Core (PBKDF2) | BR-AUTH-01 ghi "bcrypt hoặc tương đương" nên vẫn đúng; **Đã sửa trong ERD Tài khoản-Vai trò v1 (18/10)** | #1 (ERD) | **Đã chốt 18/10** (ERD Tài khoản-Vai trò v1) |
| 12 | Đổi mật khẩu có hủy các phiên khác không | Không (đặc tả không yêu cầu) | Nếu muốn an toàn hơn: hủy mọi phiên khác, giữ phiên hiện tại. Không bắt buộc cho MVP | #1 | Giữ đề xuất: không hủy phiên khác trong MVP. Chờ xác nhận (hạn 12h 20/10/2026) |
| 13 | Tên cookie | `cs_session`, `XSRF-TOKEN` | Ngắn, không lộ công nghệ máy chủ | #2 (khung máy chủ) | Giữ `cs_session`, `XSRF-TOKEN` (đã dùng trong OpenAPI, SRS 4.3 – mục A11); #2 áp dụng khi dựng khung máy chủ. Chờ xác nhận (hạn 12h 20/10/2026) |

---

## 9. Gợi ý khi lập trình bằng ASP.NET Core

Phần này chỉ để hình dung; cách viết cụ thể do #2 chốt trong README quy ước code máy chủ (T3-12).

| Trong OpenAPI | Trong code C# |
|---|---|
| `tags` (3 nhóm) | 3 controller trong `Features/Auth/`: `AuthController` (`/auth`), `MeController` (`/me`), `AccountsController` (`/accounts`, `/roles`) |
| `operationId` | Tên hàm trong controller, ví dụ `DangNhap`, `TaoTaiKhoan` |
| `summary` | Thuộc tính `[EndpointSummary("AUTH-06 Tạo tài khoản")]` hoặc chú thích XML |
| `security: []` | `[AllowAnonymous]` |
| `cookieAuth` | Cookie Authentication trong `Program.cs`, kiểm tra phiên với bảng `phien_dang_nhap` |
| `xsrfHeader` | Antiforgery của ASP.NET Core, cấu hình đọc header `X-XSRF-TOKEN` |
| Quyền AD/TN/NK, mã quyền | Mỗi mã quyền là một **policy**: `[Authorize(Policy = "QUAN_TRI_TAI_KHOAN")]`. Bảng vai trò → quyền khai báo một lần trong `Features/Auth/BangQuyen.cs` |
| `schemas` `YeuCau…`, `KetQua…`, `TaiKhoan` | Các lớp DTO (record) trong `Features/Auth/Dtos/`; **không** trả thẳng entity EF Core |
| `required`, `maxLength`, `pattern` | Thuộc tính kiểm tra dữ liệu như `[Required]`, `[MaxLength(50)]`, `[RegularExpression(...)]` |
| `Loi`, mã lỗi | Lớp ngoại lệ nghiệp vụ dùng chung trong `Common/` (ví dụ `throw new LoiNghiepVu("TRUNG_TEN_DANG_NHAP", 409, "Tên đăng nhập đã tồn tại.")`), một middleware chuyển thành JSON `Loi` |
| `PHAI_DOI_MAT_KHAU` | Một bộ lọc chạy trước mọi endpoint, trừ 3 endpoint được phép |

Ví dụ hình dạng một endpoint:

```csharp
[ApiController]
[Route("api/v1/accounts")]
[Tags("AUTH – Quản trị tài khoản")]
public class AccountsController : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = MaQuyen.QuanTriTaiKhoan)]
    [EndpointSummary("AUTH-06 Tạo tài khoản")]
    [ProducesResponseType<TaiKhoanDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<LoiDto>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> TaoTaiKhoan(YeuCauTaoTaiKhoan yeuCau)
    {
        var taiKhoan = await _dichVuTaiKhoan.TaoAsync(yeuCau);
        return CreatedAtAction(nameof(XemTaiKhoan), new { id = taiKhoan.Id }, taiKhoan);
    }
}
```

Khi các thuộc tính `ProducesResponseType`, `Authorize`, `Tags` được khai báo đúng, Swagger sinh ra sẽ gần giống file YAML này. Đó là cách kiểm tra code có khớp hợp đồng hay không.

### 9.1. Tài khoản Admin đầu tiên (#1 làm, bổ sung 20/10/2026)

CSDL mới dựng bằng migration chỉ có 3 vai trò, chưa có tài khoản nào, nên không ai đăng nhập được để tạo tài khoản (AUTH-06). Mật khẩu phải băm bằng `PasswordHasher` nên không viết sẵn bằng SQL được (`schema_v1.sql` mục 9c). **#1 viết phần này trong task T4-16** (Bảng Task, bổ sung 20/10/2026; T-AUTH-01 phụ thuộc task này), vì cần nó để test AUTH-01.

| Nội dung | Cách làm |
|---|---|
| Vị trí | `Features/Auth/KhoiTaoAdminDauTien.cs`, gọi một lần trong `Program.cs` sau khi ứng dụng khởi động và migration đã chạy |
| Khi nào tạo | Chỉ khi CSDL **chưa có tài khoản nào thuộc vai trò AD**. Đã có thì bỏ qua, nên chạy lại nhiều lần không tạo trùng |
| Dữ liệu | Tên đăng nhập và mật khẩu tạm đọc từ biến môi trường `AdminDauTien__TenDangNhap`, `AdminDauTien__MatKhauTam`; họ tên mặc định "Quản trị viên"; vai trò AD; `trang_thai = HOAT_DONG` |
| Mật khẩu | Băm bằng `PasswordHasher`; mật khẩu tạm phải đạt BR-AUTH-04; `bat_buoc_doi_mk = TRUE`, nên lần đăng nhập đầu phải đổi mật khẩu (AUTH-01 A1) |
| Thiếu biến môi trường | Không tạo, ghi cảnh báo vào log; ứng dụng vẫn chạy |
| Bảo mật | Không commit mật khẩu; `.env.example` chỉ có tên biến, để trống giá trị (ADR-001 mục 3.8) |
| Liên quan | Script dữ liệu mẫu của #4 dùng Admin này, không tự tạo Admin thứ hai. Bộ test API của #4 và test dịch vụ kho tự tạo tài khoản riêng trong CSDL test, không phụ thuộc phần này |

---

## 10. Việc tiếp theo

- [ ] Nhóm xác nhận kết quả 13 điểm ở mục 8 (đã ghi kết quả đề xuất ngày 20/10); ghi kết quả cuối vào comment issue T3-05
- [x] Đăng ký 4 mã lỗi mới vào `API_CONVENTIONS.md` mục 8.4 (20/10/2026)
- [x] Dẫn bảng mã quyền (mục 6.4) vào `API_CONVENTIONS.md` mục 9.4 (20/10/2026); bảng gốc giữ ở mục 6.4 file này
- [x] Sửa ERD: thêm cột `phien_ban`, sửa ghi chú "bcrypt" (ERD Tài khoản-Vai trò v1, 18/10/2026; định dạng tên đăng nhập và số điện thoại ở điểm 5, 6 cũng đã đưa vào CHECK của CSDL)
- [x] Sửa đặc tả AUTH-01 bước 4–5 (điểm 1, ngày 18/10) và AUTH-07 (điểm 3, ngày 20/10) trong Đặc tả Use Case 18/10 và SRS v1.0.3; vấn đề mở số 8 ghi đã đóng trong SRS 7.2, chờ nhóm xác nhận mục A10
- [ ] #1 viết chương trình khởi tạo Admin đầu tiên (mục 9.1) – task T4-16
- [ ] Đưa `openapi-auth.yaml` và file giải thích này lên repo trong `docs/api/` qua một PR, ghi `Closes #<số issue T3-05>`
- [ ] #2, #3, #5 dùng file này làm mẫu khi viết OpenAPI cho module của mình (T3-10, T3-14, T3-25)
