# Ghi chú thiết kế T3-09 — Sản phẩm và Nhập hàng

## 1. Phạm vi và tình trạng đầu vào

T3-09 chỉ thiết kế CSDL cho hai module Sản phẩm/Danh mục (SP-01–SP-09) và Nhập hàng/Nhà cung cấp (NH-01–NH-09), cùng các điểm tích hợp Kho. Không triển khai controller/model/view, migration hay thay đổi database.

Khảo sát ngày 15/10/2026 xác nhận Git root là `Convenience-Store-Management-System`, branch là `feature/t3-09-product-purchase-erd`; repository chưa có file tracked và toàn bộ `docs/database/product-purchase/` đang untracked. Workspace không chứa:

- SRS hệ thống v1.0.1;
- đặc tả và tóm tắt 81 Use Case v1.1;
- bảng ưu tiên MoSCoW;
- SRS Chương 6/yêu cầu phi chức năng;
- ERD Kho;
- ERD Tài khoản/Vai trò;
- giao diện dịch vụ “Ghi nhận biến động kho”.

Nguồn khả dụng là nội dung giao việc ngày 13/10/2026 và ảnh ERD nháp kèm theo. Do thiếu tài liệu gốc, thiết kế này là đề xuất sẵn sàng review, chưa phải schema đã được nhóm chốt. Không thể đối chiếu từng tiêu chí SP-01–SP-09/NH-01–NH-09 hoặc SRS Chương 6; chỉ có thể kiểm tra với các ràng buộc được trích trong yêu cầu.

## 2. Các bảng và lý do tách

1. `categories`: chuẩn hóa danh mục một cấp; tránh lặp tên/chính sách danh mục ở sản phẩm.
2. `products`: dữ liệu chủ của sản phẩm, không chứa số lượng tồn và không lặp giá bán hiện hành.
3. `product_price_history`: mỗi thay đổi giá là một bản ghi bất biến có thời điểm hiệu lực, phục vụ truy vết.
4. `suppliers`: dữ liệu chủ của nhà cung cấp, độc lập với chứng từ.
5. `purchase_receipts`: phần đầu, nhà cung cấp, người thao tác và trạng thái của phiếu.
6. `purchase_receipt_items`: giải quan hệ N:M giữa phiếu và sản phẩm, giữ số lượng/giá vốn/snapshot của chứng từ.

Không tạo bảng kho, tồn kho, thẻ kho, lô hàng, hạn sử dụng, công nợ, mã vạch, chi nhánh hay phê duyệt vì nằm ngoài phạm vi hoặc đang chờ ERD của #1.

## 3. Quyết định thiết kế chính

### 3.1 Danh mục và sản phẩm

- Danh mục một cấp: một `category` có 0..N `products`; mỗi sản phẩm bắt buộc có đúng một danh mục.
- `sku` là mã duy nhất. Không thiết kế barcode.
- `unit` là đúng một đơn vị tính gốc, lưu trực tiếp trên sản phẩm.
- `minimum_stock` có `DEFAULT 5` và chỉ là ngưỡng cảnh báo, không phải tồn kho hiện hành.
- Dùng `ACTIVE/INACTIVE` và `is_active` để giữ tham chiếu lịch sử. Xóa dữ liệu chủ đã được tham chiếu dùng `RESTRICT`.
- `non_returnable` được giữ tại danh mục: SP-08 quy định cờ không đổi trả ở cấp danh mục. Không thêm bảng danh mục con.

### 3.2 Lịch sử giá bán

`products` không có cột `sale_price`; nguồn sự thật duy nhất là `product_price_history`. Giá hiện hành là bản ghi chưa hủy, có `effective_at` không lớn hơn thời điểm truy vấn và lớn nhất theo `(effective_at, id)`. `UNIQUE(product_id, effective_at)` loại trừ hai mức giá cùng thời điểm.

Giá bán dùng `BIGINT UNSIGNED`, `> 0` và `MOD(sale_price,100)=0`, đúng quy tắc số nguyên VND và bội số 100. `created_by` phục vụ audit/phân quyền nhưng FK thật chờ ERD Tài khoản. Sản phẩm và giá ban đầu phải được tạo trong cùng transaction. Các lần thay đổi sau chỉ Admin được tạo; đây là authorization ở service, không thể diễn đạt chỉ bằng FK/CHECK.

Không cập nhật/xóa bản ghi lịch sử. `cancelled_at` chỉ được ghi khi mức giá chưa có hiệu lực (`CURRENT_TIMESTAMP(6) < effective_at`), nhằm loại mức giá tương lai khỏi tập giá hiệu lực mà vẫn giữ audit. Service lấy thời gian từ server và kiểm tra điều kiện tại thời điểm hủy; `CHECK (cancelled_at < effective_at)` là lớp bảo vệ bổ sung ở MySQL.

### 3.3 Snapshot chứng từ

Ngoài `product_name_snapshot` trong ảnh nháp, thiết kế đề xuất thêm `product_sku_snapshot` và `unit_snapshot`. Ba trường này bảo toàn mã nhận diện, tên và ý nghĩa của số lượng nếu dữ liệu chủ đổi sau khi lập phiếu. Snapshot do server sao chép khi thêm dòng, không phải bản sao tùy ý do client nhập. `product_id` vẫn được giữ để truy vấn và tích hợp kho.

### 3.4 Tiền và tổng tiền

`unit_cost`, `line_total`, `total_amount` là số nguyên VND bằng `BIGINT UNSIGNED`. Theo NH-04, `unit_cost >= 0`; `quantity > 0`. `line_total` là generated stored column `quantity * unit_cost`, tránh lệch thành tiền dòng. `total_amount` được vật hóa theo sơ đồ đầu vào để đọc chứng từ nhanh nhưng là dữ liệu dẫn xuất: service luôn tính từ tổng các dòng và cập nhật trong cùng transaction. MySQL `CHECK` không thể đảm bảo tổng liên bảng.

Không lưu giá vốn hiện hành trong sáu bảng vì nơi lưu chính thức thuộc ERD Kho chưa được cung cấp. Dòng phiếu giữ giá nhập lịch sử, không phải “giá vốn hiện hành”.

## 4. Vòng đời phiếu nhập

```text
DRAFT ──xác nhận nguyên tử──> RECEIVED
  │
  └────────hủy──────────────> CANCELLED
```

- `DRAFT`: có thể sửa phần đầu/dòng; chưa thay đổi tồn kho.
- `RECEIVED`: đã xác nhận nhập kho; có người/thời điểm xác nhận; bất biến, không được sửa hoặc hủy.
- `CANCELLED`: chỉ xuất phát từ `DRAFT`, có lý do; không thay đổi tồn kho.
- Sai sót sau `RECEIVED` đi qua nghiệp vụ kiểm kê, không đảo/xóa phiếu.

Một phiếu bắt buộc thuộc một nhà cung cấp và khi xác nhận phải có ít nhất một dòng. `UNIQUE(receipt_id, product_id)` khiến mỗi sản phẩm chỉ xuất hiện một lần; thao tác thêm trùng phải cộng vào dòng hiện có. `UNIQUE(supplier_id, supplier_document_no)` là đề xuất chống nhập trùng chứng từ của nhà cung cấp khi số chứng từ được cung cấp.

### Chống xác nhận hai lần

Trong transaction, service khóa phiếu và dùng chuyển trạng thái có điều kiện từ `DRAFT` sang `RECEIVED`. Chỉ transaction cập nhật đúng một dòng được tiếp tục; transaction còn lại nhận 0 dòng và thất bại nghiệp vụ. Có thể triển khai bằng `SELECT ... FOR UPDATE` kết hợp `UPDATE ... WHERE status='DRAFT'`, hoặc optimistic concurrency tương đương. Cấm endpoint xác nhận gửi lại sự kiện kho sau khi trạng thái không còn `DRAFT`.

## 5. Tích hợp NH-06 với Kho

Luồng đề xuất trong một transaction nguyên tử:

1. Bắt đầu transaction và khóa phiếu `DRAFT` cùng các dòng.
2. Kiểm tra quyền, nhà cung cấp, ít nhất một dòng, số lượng/giá vốn, snapshot và tổng tiền.
3. Chuyển trạng thái có điều kiện để giành quyền xác nhận duy nhất.
4. Với từng dòng, gọi giao diện nội bộ “Ghi nhận biến động kho” với định danh nguồn ổn định gồm loại nguồn + `receipt_id`/`receipt_item_id`, `product_id`, số lượng tăng, `unit_cost`, thời điểm và người thực hiện.
5. Thành phần Kho tăng tồn và cập nhật giá vốn bình quân gia quyền: `(tồn cũ * giá vốn cũ + lượng nhập * đơn giá nhập) / (tồn cũ + lượng nhập)`, theo quy tắc làm tròn do #1 chốt.
6. Ghi `confirmed_by`, `confirmed_at`, hoàn tất `RECEIVED`, rồi commit. Bất kỳ lỗi nào phải rollback toàn bộ.

Luồng này chỉ nguyên tử nếu module Kho dùng cùng MySQL database/transaction và giao diện cho phép truyền cùng connection/transaction (hoặc được thực thi trong cùng application transaction). Nếu là dịch vụ/process độc lập thì không thể tuyên bố ACID xuyên dịch vụ; nhóm phải chốt outbox/idempotency/saga. Khóa idempotency phía Kho vẫn cần để chống ghi lặp khi retry. Tên bảng/cột kho, nơi lưu giá vốn, kiểu dữ liệu, quy tắc làm tròn và chữ ký giao diện đều đang chờ bàn giao.

## 6. Quy tắc ở CSDL và ở service

**CSDL đảm bảo được:** PK, FK nội phạm vi, duy nhất mã, duy nhất một sản phẩm trên phiếu, miền trạng thái, `quantity > 0`, `unit_cost >= 0`, giá bán dương và là bội số 100, quan hệ thời gian `cancelled_at < effective_at`, thành tiền dòng, và các quy tắc xóa/cập nhật FK.

**Service phải đảm bảo:** phân quyền theo vai trò; chuỗi không rỗng/định dạng; tạo sản phẩm kèm giá đầu; chỉ Admin thay đổi giá sau đó; chỉ hủy giá chưa hiệu lực bằng thời gian server; giá hiện hành duy nhất theo thời gian; snapshot tin cậy; tổng phiếu bằng tổng dòng; điều kiện phụ thuộc trạng thái; chỉ sửa nháp; ít nhất một dòng khi xác nhận; chuyển trạng thái hợp lệ; chống xác nhận lặp; atomicity với Kho; giá vốn bình quân; và không xóa nhà cung cấp đã giao dịch.

Trigger có thể siết bất biến trạng thái/tổng tiền nhưng chưa được đưa vào thiết kế vì nhóm chưa cung cấp quy ước migration/trigger. Nếu nhiều ứng dụng cùng ghi DB, #1 nên cân nhắc stored procedure/trigger hoặc thu hồi quyền ghi trực tiếp để tránh bỏ qua service.

## 7. Quy tắc FK đề xuất

| FK                                  | Khi cập nhật khóa | Khi xóa cha                           | Lý do                                                                                              |
| ----------------------------------- | ----------------- | ------------------------------------- | -------------------------------------------------------------------------------------------------- |
| `products.category_id`              | `RESTRICT`        | `RESTRICT`                            | Dữ liệu chủ không đổi PK/xóa khi đang dùng.                                                        |
| `product_price_history.product_id`  | `RESTRICT`        | `RESTRICT`                            | Giữ audit giá.                                                                                     |
| `purchase_receipts.supplier_id`     | `RESTRICT`        | `RESTRICT`                            | Giữ lịch sử giao dịch.                                                                             |
| `purchase_receipt_items.receipt_id` | `RESTRICT`        | `RESTRICT` (đề xuất, chờ Lead review) | Tránh xóa cha làm mất dòng chứng từ; chính sách xóa phiếu nháp phải được chốt cùng quyết định này. |
| `purchase_receipt_items.product_id` | `RESTRICT`        | `RESTRICT`                            | Giữ lịch sử chứng từ và tích hợp kho.                                                              |
| Các FK tài khoản                    | Chờ #1            | Chờ #1                                | Chưa có ERD đích.                                                                                  |

Không khuyến nghị `ON UPDATE CASCADE` cho khóa định danh kỹ thuật; PK phải bất biến.

## 8. Điểm chưa chốt với #1

1. Tên bảng/cột, kiểu PK và chính sách xóa của Tài khoản/Vai trò cho `created_by`, `confirmed_by`.
2. ERD Kho: bảng giữ tồn/giá vốn, kiểu tiền/số lượng, khóa và cơ chế concurrency.
3. Chữ ký, transaction boundary và idempotency contract của dịch vụ ghi biến động kho NH-06.
4. Quy tắc làm tròn giá vốn bình quân gia quyền.
5. Tên/trạng thái chuẩn (tiếng Anh hay mã số) dùng chung trong toàn hệ thống.
6. Xử lý trường hợp không còn giá hiệu lực sau khi hủy một mức giá tương lai.
7. `suppliers.phone` có bắt buộc và duy nhất không; `tax_code` có cần duy nhất không.
8. Có bắt buộc/duy nhất số chứng từ nhà cung cấp hay không.
9. Lead phê duyệt đề xuất `ON DELETE RESTRICT` cho `purchase_receipt_items.receipt_id` và chốt phiếu `DRAFT` được xóa cứng hay chỉ hủy mềm.
10. Giá nhập có phải bội số 100 không; số lượng có bao giờ là thập phân không.
11. Quy tắc sinh mã `sku`, `supplier_code`, `receipt_code`, timezone và độ chính xác thời gian.
12. Có dùng trigger/stored procedure để bảo vệ trạng thái và tổng tiền khi có nhiều writer không.
13. Tên và tiêu chí đầy đủ của các Use Case còn lại để hoàn thiện traceability ngoài SP-08, NH-04 và NH-06.

## 9. Giả định và rủi ro

- Hệ thống chỉ có một cửa hàng, một loại tồn cho mỗi sản phẩm và số lượng nhập là số nguyên. Nếu ERD Kho dùng location/warehouse hoặc số lượng thập phân, khóa và kiểu số phải đổi.
- Collation dự kiến không phân biệt hoa/thường; vì thế UK mã/tên cũng không phân biệt hoa/thường. Phải chốt charset/collation toàn hệ thống.
- `BIGINT UNSIGNED` đủ cho tiền VND, nhưng phép nhân generated column và tổng nhiều dòng vẫn cần kiểm tra tràn ở service.
- Chỉ dựa vào application để bảo vệ trạng thái là rủi ro nếu có writer khác hoặc quyền SQL trực tiếp.
- Không có tài liệu gốc nên cardinality, nullable và uniqueness ngoài các ràng buộc nêu rõ có thể thay đổi sau review.

## 10. Công việc khi hợp nhất ERD

1. Đối chiếu kiểu và tên khóa với ERD Tài khoản/Kho; thêm các FK ngoài phạm vi và quan hệ Mermaid tương ứng.
2. Thay mô tả tích hợp NH-06 bằng chữ ký dịch vụ thực tế; chốt transaction/idempotency.
3. Đối chiếu từng Use Case/SRS Chương 6/MoSCoW và cập nhật ma trận traceability.
4. Chốt các mục trong phần 8; xóa cột/constraint đề xuất không được duyệt.
5. Tạo DDL/migration ở task được phân công riêng; kiểm thử constraint và hai transaction xác nhận đồng thời.
6. Đồng bộ tên/kiểu/trạng thái/index với quy ước chung, rồi render lại Mermaid/draw.io.
