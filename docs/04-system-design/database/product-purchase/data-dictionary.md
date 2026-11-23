# Từ điển dữ liệu — Sản phẩm và Nhập hàng

## 1. Trạng thái tài liệu và quy ước

Tài liệu này mô tả thiết kế logic cho MySQL 8.0 trong phạm vi T3-09. Workspace hiện không có SRS v1.0.1, đặc tả 81 Use Case, bảng MoSCoW, ERD Kho, ERD Tài khoản/Vai trò hoặc hợp đồng dịch vụ kho. Vì vậy:

- Các quy tắc được ghi là **Đã xác định** chỉ khi xuất hiện trong nội dung yêu cầu T3-09 do người giao việc cung cấp ngày 13/10/2026.
- Các mục **Đề xuất thiết kế** phải được #1/Lead/BA đối chiếu trước khi chuyển thành DDL.
- `created_by` và `confirmed_by` là khóa tham chiếu logic đang chờ tên bảng/cột và kiểu khóa từ ERD Tài khoản/Vai trò; chưa được khai báo quan hệ trong Mermaid.
- Không tạo bảng tồn kho hoặc thẻ kho trong tài liệu này.
- `BOOLEAN` trong MySQL là bí danh của `TINYINT(1)`. Mọi tiền tệ dùng số nguyên VND.
- Ký hiệu `UK` trong ERD là cột tham gia khóa duy nhất; khóa duy nhất ghép được mô tả riêng bên dưới.

Việc gắn Use Case chỉ ở mức dải mã module vì workspace không có tên và tiêu chí chấp nhận của từng Use Case. Không nên xem phần ánh xạ này là đối chiếu đầy đủ SP-01–SP-09/NH-01–NH-09.

## 2. `categories`

**Mục đích:** Lưu danh mục sản phẩm một cấp. Một danh mục có thể chưa có hoặc có nhiều sản phẩm; mỗi sản phẩm thuộc đúng một danh mục.

| Cột              | Kiểu MySQL        |  Null | Khóa/ràng buộc                                     | Mô tả                                                                             |
| ---------------- | ----------------- | ----: | -------------------------------------------------- | --------------------------------------------------------------------------------- |
| `id`             | `BIGINT UNSIGNED` | Không | PK, `AUTO_INCREMENT`                               | Định danh nội bộ.                                                                 |
| `name`           | `VARCHAR(100)`    | Không | UK                                                 | Tên danh mục duy nhất. Đề xuất so sánh theo collation không phân biệt hoa/thường. |
| `description`    | `TEXT`            |    Có | —                                                  | Mô tả tùy chọn.                                                                   |
| `is_active`      | `BOOLEAN`         | Không | `DEFAULT TRUE`, `CHECK (is_active IN (0,1))`       | Cho phép ngừng sử dụng thay vì xóa dữ liệu đã tham chiếu.                         |
| `non_returnable` | `BOOLEAN`         | Không | `DEFAULT FALSE`, `CHECK (non_returnable IN (0,1))` | Cờ không đổi trả ở cấp danh mục theo SP-08.                                       |

**Quan hệ và quy tắc:** `categories.id` ← `products.category_id` là 1:N. `ON UPDATE RESTRICT`, `ON DELETE RESTRICT`; danh mục đang được dùng được vô hiệu hóa bằng `is_active`. Chỉ mục `UK(name)` phục vụ tra cứu tên.

**Use Case:** hỗ trợ nhóm SP-01–SP-09 ở các thao tác danh mục có liên quan; cần tài liệu Use Case để gắn chính xác từng mã.

## 3. `products`

**Mục đích:** Lưu dữ liệu chủ của sản phẩm. Không lưu hay cho phép sửa tồn kho trực tiếp tại đây.

| Cột             | Kiểu MySQL        |  Null | Khóa/ràng buộc                                                | Mô tả                                                                        |
| --------------- | ----------------- | ----: | ------------------------------------------------------------- | ---------------------------------------------------------------------------- |
| `id`            | `BIGINT UNSIGNED` | Không | PK, `AUTO_INCREMENT`                                          | Định danh nội bộ.                                                            |
| `category_id`   | `BIGINT UNSIGNED` | Không | FK → `categories(id)`                                         | Danh mục một cấp của sản phẩm.                                               |
| `sku`           | `VARCHAR(50)`     | Không | UK                                                            | Mã sản phẩm duy nhất, bất biến sau khi đã xuất hiện trên chứng từ (đề xuất). |
| `name`          | `VARCHAR(200)`    | Không | —                                                             | Tên hiện hành của sản phẩm.                                                  |
| `unit`          | `VARCHAR(50)`     | Không | —                                                             | Một đơn vị tính gốc của sản phẩm.                                            |
| `image_url`     | `VARCHAR(2048)`   |    Có | —                                                             | URL ảnh tùy chọn; chính sách lưu trữ ảnh thuộc ngoài phạm vi.                |
| `description`   | `TEXT`            |    Có | —                                                             | Mô tả tùy chọn.                                                              |
| `minimum_stock` | `INT UNSIGNED`    | Không | `DEFAULT 5`, `CHECK (minimum_stock >= 0)`                     | Ngưỡng cảnh báo mặc định là 5; không phải số lượng tồn hiện hành.            |
| `status`        | `VARCHAR(20)`     | Không | `DEFAULT 'ACTIVE'`, `CHECK (status IN ('ACTIVE','INACTIVE'))` | Trạng thái kinh doanh.                                                       |

**Quan hệ và quy tắc:** `category_id` dùng `ON UPDATE RESTRICT`, `ON DELETE RESTRICT`. Sản phẩm là phía 1 của lịch sử giá và dòng phiếu nhập; hai FK con đều `RESTRICT` khi xóa sản phẩm nhằm giữ lịch sử. Chỉ mục đề xuất: `UK(sku)`, `IX(category_id, status)`, `IX(name)`.

Giá bán hiện hành không lặp trong bảng này. Nó được lấy từ bản ghi `product_price_history` chưa hủy có `effective_at <= CURRENT_TIMESTAMP(6)` lớn nhất (nếu hòa thì lấy `id` lớn nhất). Cách này chọn một nguồn sự thật; truy vấn/transaction cập nhật giá phải tuân thủ quy tắc ở mục 4.

**Use Case:** nhóm SP-01–SP-09; NH-01–NH-09 chỉ đọc/chọn sản phẩm khi lập phiếu. Cần đặc tả gốc để ánh xạ từng mã.

## 4. `product_price_history`

**Mục đích:** Lưu lịch sử giá bán và thời điểm hiệu lực; không lưu giá vốn.

| Cột            | Kiểu MySQL        |  Null | Khóa/ràng buộc                                                                                 | Mô tả                                                                      |
| -------------- | ----------------- | ----: | ---------------------------------------------------------------------------------------------- | -------------------------------------------------------------------------- |
| `id`           | `BIGINT UNSIGNED` | Không | PK, `AUTO_INCREMENT`                                                                           | Định danh bản ghi giá.                                                     |
| `product_id`   | `BIGINT UNSIGNED` | Không | FK → `products(id)`                                                                            | Sản phẩm được định giá.                                                    |
| `sale_price`   | `BIGINT UNSIGNED` | Không | `CHECK (sale_price > 0 AND MOD(sale_price,100)=0)`                                             | Giá bán nguyên VND, dương và là bội số 100 đồng.                           |
| `effective_at` | `DATETIME(6)`     | Không | Thành phần UK ghép                                                                             | Thời điểm giá bắt đầu hiệu lực.                                            |
| `created_by`   | `BIGINT UNSIGNED` | Không | FK ngoài phạm vi, đang chờ                                                                     | Người tạo giá; phải khớp khóa tài khoản khi hợp nhất ERD.                  |
| `created_at`   | `DATETIME(6)`     | Không | `DEFAULT CURRENT_TIMESTAMP(6)`                                                                 | Thời điểm ghi nhận.                                                        |
| `cancelled_at` | `DATETIME(6)`     |    Có | `CHECK (cancelled_at IS NULL OR (cancelled_at >= created_at AND cancelled_at < effective_at))` | Thời điểm hủy một mức giá khi mức giá chưa có hiệu lực; không xóa lịch sử. |

**Quan hệ và quy tắc:** FK `product_id` dùng `ON UPDATE RESTRICT`, `ON DELETE RESTRICT`. Đề xuất `UNIQUE(product_id, effective_at)` để không có hai giá cùng thời điểm; `IX(product_id, cancelled_at, effective_at, id)` phục vụ lấy giá hiện hành.

Mỗi sản phẩm phải có giá ban đầu trong cùng transaction tạo sản phẩm. Nhân viên kho được tạo giá ban đầu; chỉ Admin được tạo lần thay đổi sau — đây là kiểm tra phân quyền ở application/service layer. Khi thêm giá, service khóa logic theo sản phẩm và từ chối trùng `effective_at`; không cập nhật hoặc xóa bản ghi giá đã có.

Chỉ mức giá **chưa có hiệu lực** mới được hủy. Service phải kiểm tra `CURRENT_TIMESTAMP(6) < effective_at`, lấy thời gian hủy từ server và ghi `cancelled_at`; không nhận thời gian hủy tùy ý từ client. `CHECK (cancelled_at < effective_at)` hỗ trợ bảo vệ dữ liệu, nhưng riêng nó không chứng minh thao tác được thực hiện trước thời điểm hiệu lực nếu người ghi có thể backdate dữ liệu.

**Use Case:** nhóm SP-01–SP-09 ở chức năng tạo sản phẩm/tra cứu/cập nhật giá; cần đặc tả để xác định mã chính xác.

## 5. `suppliers`

**Mục đích:** Lưu dữ liệu chủ của nhà cung cấp.

| Cột              | Kiểu MySQL        |  Null | Khóa/ràng buộc                                                | Mô tả                                                                     |
| ---------------- | ----------------- | ----: | ------------------------------------------------------------- | ------------------------------------------------------------------------- |
| `id`             | `BIGINT UNSIGNED` | Không | PK, `AUTO_INCREMENT`                                          | Định danh nội bộ.                                                         |
| `supplier_code`  | `VARCHAR(50)`     | Không | UK                                                            | Mã nhà cung cấp duy nhất.                                                 |
| `name`           | `VARCHAR(200)`    | Không | —                                                             | Tên nhà cung cấp.                                                         |
| `contact_person` | `VARCHAR(150)`    |    Có | —                                                             | Người liên hệ.                                                            |
| `phone`          | `VARCHAR(30)`     | Không | UK (đề xuất từ sơ đồ đầu vào)                                 | Số điện thoại ở dạng chuỗi. Cần #1 xác nhận có thật sự duy nhất/bắt buộc. |
| `email`          | `VARCHAR(254)`    |    Có | —                                                             | Email liên hệ; kiểm tra cú pháp tại application layer.                    |
| `address`        | `VARCHAR(500)`    |    Có | —                                                             | Địa chỉ.                                                                  |
| `tax_code`       | `VARCHAR(50)`     |    Có | —                                                             | Mã số thuế; tính duy nhất chưa được chốt.                                 |
| `notes`          | `TEXT`            |    Có | —                                                             | Ghi chú nội bộ.                                                           |
| `status`         | `VARCHAR(20)`     | Không | `DEFAULT 'ACTIVE'`, `CHECK (status IN ('ACTIVE','INACTIVE'))` | Dùng vô hiệu hóa thay vì xóa nhà cung cấp đã giao dịch.                   |

**Quan hệ và quy tắc:** Một nhà cung cấp có 0..N phiếu nhập; mỗi phiếu có đúng một nhà cung cấp. FK con dùng `ON UPDATE RESTRICT`, `ON DELETE RESTRICT`. Nhà cung cấp đã phát sinh phiếu không được xóa cứng; đề xuất không xóa cứng mọi nhà cung cấp mà chuyển `INACTIVE`. Chỉ mục: `UK(supplier_code)`, `UK(phone)`, `IX(name)`, `IX(status)`.

**Use Case:** nhóm NH-01–NH-09 ở các thao tác nhà cung cấp; cần đặc tả để gắn từng mã.

## 6. `purchase_receipts`

**Mục đích:** Lưu phần đầu phiếu nhập và vòng đời Nháp → Đã nhập kho hoặc Nháp → Đã hủy.

| Cột                    | Kiểu MySQL        |  Null | Khóa/ràng buộc                                                          | Mô tả                                                        |
| ---------------------- | ----------------- | ----: | ----------------------------------------------------------------------- | ------------------------------------------------------------ |
| `id`                   | `BIGINT UNSIGNED` | Không | PK, `AUTO_INCREMENT`                                                    | Định danh phiếu.                                             |
| `receipt_code`         | `VARCHAR(50)`     | Không | UK                                                                      | Mã phiếu duy nhất.                                           |
| `supplier_id`          | `BIGINT UNSIGNED` | Không | FK → `suppliers(id)`                                                    | Nhà cung cấp của phiếu.                                      |
| `created_by`           | `BIGINT UNSIGNED` | Không | FK ngoài phạm vi, đang chờ                                              | Người lập phiếu.                                             |
| `confirmed_by`         | `BIGINT UNSIGNED` |    Có | FK ngoài phạm vi, đang chờ                                              | Người xác nhận nhập kho.                                     |
| `supplier_document_no` | `VARCHAR(100)`    |    Có | Thành phần UK ghép đề xuất                                              | Số chứng từ phía nhà cung cấp.                               |
| `status`               | `VARCHAR(20)`     | Không | `DEFAULT 'DRAFT'`, `CHECK (status IN ('DRAFT','RECEIVED','CANCELLED'))` | Trạng thái vòng đời.                                         |
| `total_amount`         | `BIGINT UNSIGNED` | Không | `DEFAULT 0`, `CHECK (total_amount >= 0)`                                | Tổng tiền vật hóa để truy vấn/chứng từ; phải bằng tổng dòng. |
| `created_at`           | `DATETIME(6)`     | Không | `DEFAULT CURRENT_TIMESTAMP(6)`                                          | Thời điểm lập.                                               |
| `confirmed_at`         | `DATETIME(6)`     |    Có | —                                                                       | Thời điểm chuyển sang `RECEIVED`.                            |
| `cancel_reason`        | `TEXT`            |    Có | —                                                                       | Lý do hủy phiếu nháp.                                        |

**Quan hệ và quy tắc:** FK `supplier_id` dùng `RESTRICT`. `UNIQUE(supplier_id, supplier_document_no)` đề xuất chống nhập trùng chứng từ khi có số (MySQL vẫn cho phép nhiều `NULL`). Chỉ mục: `UK(receipt_code)`, `IX(supplier_id, created_at)`, `IX(status, created_at)`, `IX(created_by, created_at)` sau khi chốt tài khoản.

Các điều kiện liên cột sau phải được service kiểm tra trong transaction (MySQL `CHECK` không truy vấn bảng con):

- `DRAFT`: `confirmed_by`, `confirmed_at`, `cancel_reason` đều `NULL`; có thể sửa dòng, chưa tác động kho.
- `RECEIVED`: `confirmed_by`, `confirmed_at` khác `NULL`, `cancel_reason IS NULL`, có ít nhất một dòng; tổng phiếu bằng tổng dòng; không cho sửa/hủy.
- `CANCELLED`: chỉ chuyển từ `DRAFT`, `cancel_reason` khác rỗng, hai trường xác nhận là `NULL`; không tác động kho.
- Chống xác nhận hai lần bằng cập nhật có điều kiện `UPDATE ... SET status='RECEIVED', ... WHERE id=? AND status='DRAFT'`; chỉ tiếp tục khi đúng một dòng bị cập nhật. Phải khóa phiếu/dòng, ghi biến động kho, cập nhật giá vốn bình quân gia quyền và hoàn tất chuyển trạng thái trong một transaction nguyên tử.

`total_amount` là dữ liệu dẫn xuất được vật hóa theo sơ đồ đầu vào. Service phải tính lại từ các dòng, không nhận giá trị do client gửi. Việc dùng trigger hay chỉ transaction service cần thống nhất trong kiến trúc chung.

**Use Case:** nhóm NH-01–NH-09, đặc biệt NH-06 theo nội dung yêu cầu là xác nhận nhập và gọi nghiệp vụ ghi biến động kho; cần đặc tả đầy đủ để đối chiếu các mã còn lại.

## 7. `purchase_receipt_items`

**Mục đích:** Bảng kết hợp giải quan hệ N:M giữa phiếu nhập và sản phẩm, đồng thời giữ dữ liệu chứng từ tại thời điểm lập.

| Cột                     | Kiểu MySQL        |  Null | Khóa/ràng buộc                                      | Mô tả                                                                                 |
| ----------------------- | ----------------- | ----: | --------------------------------------------------- | ------------------------------------------------------------------------------------- |
| `id`                    | `BIGINT UNSIGNED` | Không | PK, `AUTO_INCREMENT`                                | Định danh dòng.                                                                       |
| `receipt_id`            | `BIGINT UNSIGNED` | Không | FK → `purchase_receipts(id)`, thành phần UK ghép    | Phiếu chứa dòng.                                                                      |
| `product_id`            | `BIGINT UNSIGNED` | Không | FK → `products(id)`, thành phần UK ghép             | Sản phẩm được nhập.                                                                   |
| `product_sku_snapshot`  | `VARCHAR(50)`     | Không | —                                                   | Mã sản phẩm tại lúc thêm dòng. Cột bổ sung để chứng từ không đổi khi dữ liệu chủ đổi. |
| `product_name_snapshot` | `VARCHAR(200)`    | Không | —                                                   | Tên sản phẩm tại lúc thêm dòng.                                                       |
| `unit_snapshot`         | `VARCHAR(50)`     | Không | —                                                   | Đơn vị gốc tại lúc thêm dòng. Cột bổ sung để diễn giải đúng số lượng lịch sử.         |
| `quantity`              | `INT UNSIGNED`    | Không | `CHECK (quantity > 0)`                              | Số lượng nhập, số nguyên dương.                                                       |
| `unit_cost`             | `BIGINT UNSIGNED` | Không | `CHECK (unit_cost >= 0)`                            | Giá nhập đơn vị nguyên VND, không âm theo NH-04. Quy tắc bội số chưa được chốt.       |
| `line_total`            | `BIGINT UNSIGNED` | Không | `GENERATED ALWAYS AS (quantity * unit_cost) STORED` | Thành tiền nhất quán ở mức CSDL.                                                      |

**Quan hệ và quy tắc:** `UNIQUE(receipt_id, product_id)` bắt buộc một sản phẩm chỉ có một dòng trong cùng phiếu; service cộng dồn khi người dùng thêm lại. Đề xuất đổi FK `receipt_id` sang `ON DELETE RESTRICT` để tránh thao tác xóa cha làm mất các dòng lịch sử ngoài ý muốn; đây là quyết định thiết kế cần Lead phê duyệt cùng chính sách xóa phiếu `DRAFT`. FK `product_id` dùng `ON DELETE RESTRICT`. Chỉ mục riêng `IX(product_id)` cần cho chiều truy vấn sản phẩm → lịch sử nhập; tiền tố `receipt_id` đã được bao phủ bởi UK ghép.

Snapshot phải được sao chép từ `products` bởi server, không nhận tùy ý từ client. Dòng chỉ được thêm/sửa/xóa khi phiếu là `DRAFT`.

**Use Case:** nhóm NH-01–NH-09 ở lập/xem/xác nhận phiếu; liên quan nhóm SP khi tra cứu sản phẩm. Cần đặc tả để gắn mã chính xác.

## 8. Ràng buộc cần application/service layer thực thi

1. Chuẩn hóa/trim mã, tên, đơn vị, điện thoại; kiểm tra chuỗi không rỗng và độ dài ký tự.
2. Kiểm tra định dạng email/URL và quyền của người thao tác.
3. Tạo sản phẩm cùng giá ban đầu; chỉ Admin thêm giá sau đó; chỉ hủy giá chưa hiệu lực; không cập nhật/xóa lịch sử giá; xác định duy nhất giá hiện hành.
4. Không cho nhập/sửa tồn từ chức năng sản phẩm.
5. Chỉ sửa dòng và tổng tiền của phiếu `DRAFT`; snapshot lấy từ server; cộng dồn sản phẩm trùng.
6. Kiểm tra state transition, các trường phụ thuộc trạng thái và ít nhất một dòng trước khi xác nhận.
7. Khóa/chống tranh chấp và xác nhận đúng một lần; toàn bộ xác nhận + ghi kho + cập nhật giá vốn phải nguyên tử.
8. Tính `total_amount = SUM(line_total)` và kiểm tra tràn số; CSDL không thể dùng `CHECK` liên bảng cho quy tắc này.
9. Cấm sửa/hủy phiếu `RECEIVED`; sai sót sau xác nhận đi qua nghiệp vụ kiểm kê.
10. Không xóa cứng nhà cung cấp đã phát sinh phiếu; ưu tiên chuyển trạng thái.

## 9. Chỉ mục tổng hợp đề xuất

| Bảng                     | Chỉ mục/khóa                                                                                                                    | Mục đích                                          |
| ------------------------ | ------------------------------------------------------------------------------------------------------------------------------- | ------------------------------------------------- |
| `categories`             | `UNIQUE(name)`                                                                                                                  | Duy nhất và tra cứu tên.                          |
| `products`               | `UNIQUE(sku)`, `INDEX(category_id,status)`, `INDEX(name)`                                                                       | Tìm SKU; lọc danh mục/trạng thái; tìm tên.        |
| `product_price_history`  | `UNIQUE(product_id,effective_at)`, `INDEX(product_id,cancelled_at,effective_at,id)`                                             | Không trùng thời hiệu; lấy giá hiện hành/lịch sử. |
| `suppliers`              | `UNIQUE(supplier_code)`, `UNIQUE(phone)`, `INDEX(name)`, `INDEX(status)`                                                        | Tra cứu và lọc nhà cung cấp.                      |
| `purchase_receipts`      | `UNIQUE(receipt_code)`, `UNIQUE(supplier_id,supplier_document_no)`, `INDEX(status,created_at)`, `INDEX(supplier_id,created_at)` | Chống trùng và lập danh sách phiếu.               |
| `purchase_receipt_items` | `UNIQUE(receipt_id,product_id)`, `INDEX(product_id)`                                                                            | Một dòng/sản phẩm/phiếu và tra lịch sử nhập.      |

Chỉ mục FK thực tế cần kiểm tra lại với DDL hợp nhất; InnoDB có thể tái sử dụng tiền tố trái của UK/index đã có.
