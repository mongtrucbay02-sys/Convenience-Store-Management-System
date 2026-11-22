# 04 — Thiết kế hệ thống

> **Trục chính:** 01 Khảo sát → 02 Định nghĩa sản phẩm → 03 Yêu cầu → **04 Thiết kế** → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → 08 Nghiệm thu

## Mục đích
Mô tả **hệ thống được thiết kế ra sao** để đáp ứng yêu cầu ở `03`: kiến trúc, cơ sở dữ liệu, hợp đồng API và giao diện.

## Đầu vào → Đầu ra
- **Đầu vào:** SRS, Use Case, MoSCoW, NFR trong `03-requirements/`.
- **Đầu ra cho `05`:** ERD + từ điển dữ liệu, OpenAPI, bản thiết kế UI mà lập trình viên làm theo.

## Cấu trúc thư mục

```
04-system-design/
├── architecture/        Kiến trúc tổng thể, sơ đồ thành phần, quyết định thiết kế (ADR)
├── database/            ERD, từ điển dữ liệu, ghi chú thiết kế — mỗi nhóm module một thư mục con
├── api/                 Hợp đồng API (OpenAPI + đặc tả Markdown)
└── ui/                  Thiết kế giao diện (Figma/SVG)
```

## Tài liệu hiện có

| Tài liệu | Mô tả | Module |
|---|---|---|
| [`database/product-purchase/`](database/product-purchase/) | T3-09 — ERD, từ điển dữ liệu, ghi chú thiết kế Sản phẩm & Nhập hàng | SP, NH, KHO |
| [`database/invoice-returns/`](database/invoice-returns/) | Issue #9 — ERD (drawio/mmd/dot/svg), từ điển dữ liệu Hóa đơn & Đổi trả | POS, DT |
| [`database/reconciliation-20261123/`](database/reconciliation-20261123/) | Đối chiếu ba migration với schema/ERD/SRS; ERD bổ sung, từ điển và DDL tham chiếu **chờ duyệt** | POS, DT, BC, CSDL |
| [`api/pos-dt.openapi.yaml`](api/pos-dt.openapi.yaml) | T3-14 — OpenAPI POS và Đổi trả | POS, DT |
| [`api/openapi-report-customer-promotion.yaml`](api/openapi-report-customer-promotion.yaml) | OpenAPI Báo cáo, Khách hàng, Khuyến mãi | BC, KH, KM |
| [`api/sales.md`](api/sales.md) | T3-14 — Bản gộp hợp đồng API POS + DT + phối hợp AUTH | POS, DT |
| [`api/returns.md`](api/returns.md) | T3-14 — Hợp đồng API Đổi trả | DT |
| [`api/PHOI_HOP_AUTH_POS03_POS04.md`](api/PHOI_HOP_AUTH_POS03_POS04.md) | Thống nhất POS-03/POS-04 với AUTH/KH/KM | POS, AUTH, KH, KM |
| [`ui/QuickMart_UI.zip`](ui/QuickMart_UI.zip) | Bộ SVG giao diện Figma (bản gốc + thư mục `Nang_Cap`) | Toàn hệ thống |

## Việc nên làm tiếp
- Thêm `architecture/overview.md` mô tả kiến trúc ASP.NET Core MVC theo module (`src/ConvenienceStore.Web/Modules/*`) và nguồn dữ liệu MySQL.
- `api/sales.md` hiện là bản gộp: chép lại nguyên văn `pos-dt.openapi.yaml` (không nằm trong khối code), `returns.md`, hợp đồng POS và `PHOI_HOP_AUTH_POS03_POS04.md`. Nên tách phần hợp đồng POS thành `api/pos.md` và bỏ phần trùng, để mỗi nội dung chỉ có một bản chính.
- Cân nhắc giải nén `ui/QuickMart_UI.zip` thành thư mục `ui/` để xem trực tiếp trên GitHub và review được thay đổi.
- Thêm `database/` cho các module còn thiếu (KH, KM, BC, AUTH) nếu có ERD riêng.

## Tiêu chí hoàn thành
- [ ] Mỗi mã chức năng MVP trong `03` có thiết kế tương ứng (bảng, endpoint, màn hình).
- [ ] Thay đổi CSDL đi kèm migration và được #1 duyệt (theo checklist PR).
