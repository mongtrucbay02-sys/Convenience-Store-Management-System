# 06 — Kiểm thử

> **Trục chính:** 01 Khảo sát → 02 Định nghĩa sản phẩm → 03 Yêu cầu → 04 Thiết kế → 05 Hiện thực → **06 Kiểm thử** → 07 Triển khai → 08 Nghiệm thu

## Mục đích
Chứng minh hệ thống **làm đúng yêu cầu** ở `03` và **đúng thiết kế** ở `04`, trước khi triển khai.

## Đầu vào → Đầu ra
- **Đầu vào:** yêu cầu (`03`), hợp đồng API và ERD (`04`), mã nguồn (`05`, `src/`).
- **Đầu ra cho `07`:** báo cáo kết quả kiểm thử và danh sách lỗi còn tồn đọng, làm điều kiện cho phép triển khai.

## Nội dung cần có

| Tài liệu | Mô tả | Trạng thái |
|---|---|---|
| `test-plan.md` | Chiến lược: unit / integration / API / UI, môi trường, tiêu chí đạt | Chưa có |
| `test-cases/` | Test case thủ công theo module, mã `TC-<module>-xx`, liên kết mã chức năng | Chưa có |
| `automated-tests.md` | Mô tả bộ test tự động trong `tests/ConvenienceStore.Web.Tests/` và cách chạy `dotnet test` | Chưa có |
| `test-reports/` | Kết quả từng đợt kiểm thử (`YYYY-MM-DD-<dot>.md`) | Chưa có |
| `defects.md` | Danh sách lỗi (hoặc liên kết tới GitHub Issues gắn nhãn bug) | Chưa có |

## Bộ test tự động hiện có
Nằm ở `tests/ConvenienceStore.Web.Tests/`: `DashboardServiceTests`, `OperationalReportsServiceTests`, `ReportExcelExporterTests` (BC), `PromotionServiceTests`, `PromotionManagementTests` (KM).

## Tiêu chí hoàn thành
- [ ] Mỗi mã chức năng MVP có ít nhất một test case.
- [ ] Mỗi API trong OpenAPI ở `04` được kiểm thử theo checklist trong tài liệu API.
- [ ] Không còn lỗi mức nghiêm trọng trước khi chuyển sang `07`.
