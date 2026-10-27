# Design Notes – Hóa đơn / Đổi trả (Issue #9)

## Phạm vi
- INVOICES / INVOICE_DETAILS: hóa đơn và dòng sản phẩm bán ra.
- CASH_TRANSACTIONS: giao dịch thu tiền mặt hoặc hoàn tiền; `transaction_type` = `SALE`/`REFUND`.
- RETURNS / RETURN_DETAILS: phiếu trả hoặc đổi và từng dòng trả. `return_type` = `RETURN`/`EXCHANGE`.
- RETURN_POLICIES: chính sách đổi/trả theo thời gian hiệu lực.
- USERS, CUSTOMERS, PRODUCTS: **bảng tham chiếu dùng chung, không tạo trùng** với ERD của nhóm.

## Quy tắc nghiệp vụ
1. Số tiền lưu dạng `BIGINT UNSIGNED` (VND, không dùng số thực); tiền bán theo bội số 100 nếu SRS quy định.
2. `invoice_code`, `return_code` là mã nghiệp vụ duy nhất. Không xóa hóa đơn đã thanh toán; dùng `status`.
3. `quantity > 0`; `discount_amount <= quantity * unit_price`; `line_total = quantity * unit_price - discount_amount`.
4. Số lượng trả tích lũy của một `invoice_detail_id` không vượt số lượng bán (trừ phiếu trả bị hủy/từ chối).
5. Phiếu trả luôn trỏ về hóa đơn gốc. `refund_total` được tính từ các dòng trả, không tin giá trị do client gửi.
6. `CASH_TRANSACTIONS.return_id` chỉ có khi hoàn tiền. Một hóa đơn có thể có nhiều giao dịch (thu tiền/hoàn tiền).
7. Trạng thái `disposition` của hàng trả: `RESTOCK` hoặc `DISCARD`; khi nhập kho phải ghi nhận biến động tồn kho ở module Kho.
8. Mặc định chính sách trả trong 7 ngày, ngưỡng cần phê duyệt 500.000 VND: **cần đối chiếu SRS và xác nhận với nhóm**.
9. Ngoại lệ phải có `approved_by` và quyền Admin. Cập nhật hóa đơn/hoàn tiền/tồn kho phải dùng transaction để tránh ghi dữ liệu dở dang.
10. Đổi hàng cần tạo hóa đơn bán mới và liên kết phiếu đổi với hóa đơn mới; **đề xuất bổ sung `exchange_invoice_id` (FK INVOICES, nullable) cho RETURNS** nếu nhóm chốt luồng DT-03.

## Cần phối hợp
- Với người phụ trách sản phẩm/nhập hàng: dùng chính xác `PRODUCTS.id` và kiểu dữ liệu khóa của họ.
- Với người phụ trách khách hàng và tài khoản: xác nhận `CUSTOMERS.id`, `USERS.id`.
- Với POS: thống nhất mã hóa đơn, trạng thái thanh toán và giá chốt tại thời điểm bán.
- Với DT: xác nhận luồng phê duyệt, hoàn tiền, đổi hàng, chính sách hàng không được trả.

## Cấu trúc quan hệ
- USERS 1–N INVOICES; CUSTOMERS 1–N INVOICES (khách lẻ được null).
- INVOICES 1–N INVOICE_DETAILS; PRODUCTS 1–N INVOICE_DETAILS.
- INVOICES 1–N RETURNS; RETURNS 1–N RETURN_DETAILS.
- INVOICE_DETAILS 1–N RETURN_DETAILS.
- INVOICES 1–N CASH_TRANSACTIONS; RETURNS 1–N CASH_TRANSACTIONS (return_id nullable).

## Lưu ý triển khai
- ERD này là bản thiết kế độc lập, chưa phải migration/SQL DDL.
- Không thêm các bảng tham chiếu vào DB lần nữa nếu đã có từ module khác.
- Kiểu MySQL `BIGINT UNSIGNED` trong tài liệu có thể cần chuyển sang `BIGINT` nếu nhóm dùng SQL Server.
