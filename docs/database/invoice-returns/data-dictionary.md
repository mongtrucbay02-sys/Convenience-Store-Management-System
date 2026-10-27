# Data Dictionary – ERD Hóa đơn / Đổi trả

> Thiết kế đề xuất cho Issue #9; chưa phải schema đã được nhóm duyệt. Kiểu dữ liệu tham chiếu MySQL theo ERD sản phẩm/nhập hàng trong ảnh.

## USERS

| Cột | Kiểu dữ liệu | Ràng buộc |
|---|---|---|
| `id` | `BIGINT UNSIGNED` | PK |
| `username` | `VARCHAR(100)` | UK |

## CUSTOMERS

| Cột | Kiểu dữ liệu | Ràng buộc |
|---|---|---|
| `id` | `BIGINT UNSIGNED` | PK |
| `phone` | `VARCHAR(30)` | UK |

## PRODUCTS

| Cột | Kiểu dữ liệu | Ràng buộc |
|---|---|---|
| `id` | `BIGINT UNSIGNED` | PK |
| `name` | `VARCHAR(200)` | — |

## INVOICES

| Cột | Kiểu dữ liệu | Ràng buộc |
|---|---|---|
| `id` | `BIGINT UNSIGNED` | PK |
| `invoice_code` | `VARCHAR(50)` | UK |
| `cashier_id` | `BIGINT UNSIGNED` | FK USERS |
| `customer_id` | `BIGINT UNSIGNED NULL` | FK CUSTOMERS |
| `subtotal` | `BIGINT UNSIGNED` | — |
| `discount_total` | `BIGINT UNSIGNED` | — |
| `total_amount` | `BIGINT UNSIGNED` | — |
| `status` | `VARCHAR(20)` | — |
| `created_at` | `DATETIME(6)` | — |

## INVOICE_DETAILS

| Cột | Kiểu dữ liệu | Ràng buộc |
|---|---|---|
| `id` | `BIGINT UNSIGNED` | PK |
| `invoice_id` | `BIGINT UNSIGNED` | FK INVOICES |
| `product_id` | `BIGINT UNSIGNED` | FK PRODUCTS |
| `quantity` | `INT UNSIGNED` | — |
| `unit_price` | `BIGINT UNSIGNED` | — |
| `discount_amount` | `BIGINT UNSIGNED` | — |
| `line_total` | `BIGINT UNSIGNED` | — |

## CASH_TRANSACTIONS

| Cột | Kiểu dữ liệu | Ràng buộc |
|---|---|---|
| `id` | `BIGINT UNSIGNED` | PK |
| `invoice_id` | `BIGINT UNSIGNED` | FK INVOICES |
| `return_id` | `BIGINT UNSIGNED NULL` | FK RETURNS |
| `transaction_type` | `VARCHAR(20)` | — |
| `amount` | `BIGINT UNSIGNED` | — |
| `cash_received` | `BIGINT UNSIGNED NULL` | — |
| `change_amount` | `BIGINT UNSIGNED NULL` | — |
| `created_at` | `DATETIME(6)` | — |

## RETURNS

| Cột | Kiểu dữ liệu | Ràng buộc |
|---|---|---|
| `id` | `BIGINT UNSIGNED` | PK |
| `return_code` | `VARCHAR(50)` | UK |
| `invoice_id` | `BIGINT UNSIGNED` | FK INVOICES |
| `processed_by` | `BIGINT UNSIGNED` | FK USERS |
| `approved_by` | `BIGINT UNSIGNED NULL` | FK USERS |
| `return_type` | `VARCHAR(20)` | — |
| `status` | `VARCHAR(20)` | — |
| `reason` | `TEXT` | — |
| `refund_total` | `BIGINT UNSIGNED` | — |
| `created_at` | `DATETIME(6)` | — |

## RETURN_DETAILS

| Cột | Kiểu dữ liệu | Ràng buộc |
|---|---|---|
| `id` | `BIGINT UNSIGNED` | PK |
| `return_id` | `BIGINT UNSIGNED` | FK RETURNS |
| `invoice_detail_id` | `BIGINT UNSIGNED` | FK INVOICE_DETAILS |
| `quantity` | `INT UNSIGNED` | — |
| `refund_unit_price` | `BIGINT UNSIGNED` | — |
| `refund_amount` | `BIGINT UNSIGNED` | — |
| `disposition` | `VARCHAR(20)` | — |

## RETURN_POLICIES

| Cột | Kiểu dữ liệu | Ràng buộc |
|---|---|---|
| `id` | `BIGINT UNSIGNED` | PK |
| `name` | `VARCHAR(100)` | — |
| `return_window_days` | `INT UNSIGNED` | — |
| `approval_threshold` | `BIGINT UNSIGNED` | — |
| `is_active` | `BOOLEAN` | — |
| `effective_from` | `DATETIME(6)` | — |
