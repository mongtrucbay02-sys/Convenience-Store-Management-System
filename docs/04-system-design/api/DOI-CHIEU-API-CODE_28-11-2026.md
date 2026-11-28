# Đối chiếu tài liệu API với code (28/11/2026)

Rà soát 28/11/2026 · #1 Phạm Gia Thăng (phần tài liệu API theo phân công của #5). Code đối chiếu: `develop` `cc20336`, các controller có `[Route("api/...")]`.

**Kết luận chung:**
- Ứng dụng dùng **Razor MVC** (ADR-002), nên phần lớn chức năng chạy bằng trang và form, không qua REST.
- REST chỉ hiện thực cho AUTH, danh mục, tra cứu/in hóa đơn, đổi trả, báo cáo và khuyến mãi.
- Nhiều tệp API trong thư mục này là **hợp đồng thiết kế giai đoạn 3** (khi còn định làm SPA React), không phải mô tả hệ thống hiện tại.

## 1. Các API có trong code (36 thao tác)

| Nhóm | Đường dẫn | Thao tác | Tài liệu |
| --- | --- | --- | --- |
| AUTH | `/api/v1/auth/login`, `logout`, `change-password` | POST | `openapi-auth.yaml` ✅ |
| AUTH | `/api/v1/accounts` | GET, POST | `openapi-auth.yaml` ✅ |
| AUTH | `/api/v1/accounts/{id}` | GET, PUT | `openapi-auth.yaml` ✅ |
| AUTH | `/api/v1/accounts/{id}/lock`, `unlock`, `reset-password` | POST | `openapi-auth.yaml` ✅ |
| AUTH | `/api/v1/accounts/{id}/role` | PUT | `openapi-auth.yaml` ✅ |
| AUTH | `/api/v1/roles` | GET | `openapi-auth.yaml` ✅ |
| SP | `/api/v1/categories` | GET | `sp-07-categories.md` ✅ |
| POS | `/api/v1/invoices/{id}`, `/api/v1/invoices/{id}/print` | GET | `pos-dt.openapi.yaml` ⚠️ mô tả khác (thiết kế giỏ hàng REST) |
| DT | `/api/v1/returns/invoice-lookup` | GET | `returns-lookup.openapi.yaml` ✅ |
| DT | `/api/v1/returns/invoices/{invoiceId}`, `/api/v1/returns/{id}` | GET | ❌ chưa có tài liệu |
| DT | `/api/v1/returns/preview`, `/api/v1/returns` | POST | `returns.md` mô tả một phần |
| BC | `/api/reports/dashboard` | GET | ❌ (`openapi-report-customer-promotion.yaml` ghi `/reports/dashboard`) |
| BC | `/api/reports/revenue`, `products`, `categories`, `cashiers`, `inventory`, `purchases`, `gross-profit` | GET | ❌ (thiết kế cũ ghi `/reports/revenue-by-time`, `revenue-by-product`…) |
| BC | `/api/reports/returns`, `customers`, `promotions` | GET | `week8-reports-promotions.openapi.json` ✅ |
| KM | `/api/promotions/{id}` | GET | `week8-reports-promotions.openapi.json` ✅ |
| KM | `/api/promotions` (GET, POST), `/api/promotions/{id}` (PATCH), `/api/promotions/{id}/status` (PUT) | | ❌ (thiết kế cũ không có tiền tố `/api`) |

Lưu ý: AUTH, SP, POS, DT dùng tiền tố `/api/v1`; BC và KM dùng `/api` (không có `v1`), lệch quy ước `API_CONVENTIONS.md`.

## 2. Tài liệu mô tả API không có trong code

| Tệp | Nội dung không hiện thực bằng REST | Thực tế trong ứng dụng |
| --- | --- | --- |
| `openapi-auth.yaml` | `GET /me`, `PUT /me` (AUTH-04) | **AUTH-04 chưa hiện thực**: issue #110 (mức Could, giao #6) đóng ngày 04/11 chỉ kèm tệp giao diện, không có code |
| `products.md` | `/api/v1/products` (6 thao tác) | SP-01 → 06 làm bằng trang Razor MVC |
| `pos-dt.openapi.yaml`, `sales.md` | Giỏ hàng (`/api/v1/carts…`), danh sách hóa đơn, chính sách đổi trả, duyệt/đổi/hoàn đổi trả | POS và phần lớn DT làm bằng trang Razor MVC (ADR-002); `POS08-mvc-contract.md` mô tả đúng POS-08 |
| `openapi-report-customer-promotion.yaml` | `/customers…`, `/loyalty-settings…`, `/promotions/evaluate`, tên báo cáo cũ | Khách hàng làm bằng MVC; KH-07 (`loyalty-settings`) đã cắt; báo cáo đổi tên như mục 1 |
| `week8-reports-promotions.openapi.json` | `DELETE /api/promotions/{id}`, `GET /api/promotions/{id}/usage` | KM-05, KM-06 làm bằng trang MVC |

## 3. Sửa trong lượt rà soát này

1. **Khôi phục `openapi-report-customer-promotion.yaml`.** Tệp bị hỏng thành dữ liệu nhị phân trong commit gộp `12b6415` (22/11). Đã khôi phục từ bản đúng trên `main` (bản cuối của #5 ngày 23/10).
2. **`openapi-auth.yaml`:** ghi "chưa hiện thực" cho `GET/PUT /me` (AUTH-04) và kết quả đối chiếu 12/14 thao tác.

## 4. Đề nghị (không tự sửa – thuộc chủ tài liệu)

| Việc | Người |
| --- | --- |
| Ghi đầu `pos-dt.openapi.yaml`, `sales.md`, `products.md`: "thiết kế giai đoạn 3; POS, SP hiện thực bằng MVC" | #3, #2 |
| Viết OpenAPI cho các API BC, KM ở mục 1 đang thiếu, hoặc ghi đầu tệp thiết kế cũ là đã thay | #5 |
| Bổ sung tài liệu `GET /api/v1/returns/invoices/{id}`, `GET /api/v1/returns/{id}` | #3 |
| AUTH-04: nhóm chốt ghi "không thuộc bản nộp" trong SRS hay làm bổ sung | #5 quyết định; #1 sửa SRS |
| Thống nhất tiền tố `/api/v1` cho BC, KM (sau bảo vệ, vì đổi đường dẫn ảnh hưởng giao diện) | #5 |
