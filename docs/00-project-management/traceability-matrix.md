# Ma trận truy vết

> **Trạng thái:** Nháp · **Phụ trách:** Trưởng nhóm · **Cập nhật:** —
> **Căn cứ:** `docs/01-survey`, `docs/03-requirements`, `docs/04-system-design`, `src/`, `tests/`, `docs/08-validation`

Mỗi dòng là một chức năng/yêu cầu. Điền đường dẫn tương đối tới tài liệu hoặc mã nguồn. Ô để trống nghĩa là chưa có — chính là việc còn thiếu cần làm.

| Mã chức năng | Mô tả ngắn | Ưu tiên (MoSCoW) | Nguồn khảo sát (01: PS / N-SIM) | Yêu cầu / UC (03) | Thiết kế (04) | Code (05 → `src/`) | Test (06 → `tests/`) | Nghiệm thu (08) | Trạng thái |
|---|---|---|---|---|---|---|---|---|---|
| SP-01…SP-09 | Sản phẩm / Danh mục | | | | `04-system-design/database/product-purchase/` | `src/ConvenienceStore.Web/Modules/Products/` | | | |
| NH-01…NH-09 | Nhập hàng / Nhà cung cấp | | | | `04-system-design/database/product-purchase/` | | | | |
| POS-01…POS-04 | Bán hàng tại quầy | | | | `04-system-design/api/pos-dt.openapi.yaml`, `04-system-design/api/PHOI_HOP_AUTH_POS03_POS04.md` | `src/ConvenienceStore.Web/Controllers/PosController.cs` | | | |
| DT-01…DT-08 | Đổi trả | | | | `04-system-design/api/returns.md`, `04-system-design/database/invoice-returns/` | `src/ConvenienceStore.Web/Controllers/ReturnsController.cs` | | | |
| KM-01…KM-04 | Khuyến mãi | | | | `04-system-design/api/openapi-report-customer-promotion.yaml` | `src/ConvenienceStore.Web/Modules/Promotions/` | `tests/ConvenienceStore.Web.Tests/PromotionServiceTests.cs`, `PromotionManagementTests.cs` | | |
| BC-01…BC-12 | Báo cáo / Dashboard | | | | `04-system-design/api/openapi-report-customer-promotion.yaml` | `src/ConvenienceStore.Web/Modules/Reports/` | `tests/ConvenienceStore.Web.Tests/DashboardServiceTests.cs`, `OperationalReportsServiceTests.cs`, `ReportExcelExporterTests.cs` | | |
| KH-… | Khách hàng | | | | `04-system-design/api/openapi-report-customer-promotion.yaml` | | | | |
| AUTH-… | Đăng nhập / phân quyền | | | | | `05-implementation/attachments/Login.zip` (bản vá chưa gộp) | | | |

> Các dòng trên được điền sẵn từ những gì đang có trong repo; cột Ưu tiên, Khảo sát, Yêu cầu và Nghiệm thu cần nhóm bổ sung khi tài liệu `01`, `03`, `08` được đưa vào.
