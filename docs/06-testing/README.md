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
| [`ci-github-actions.md`](ci-github-actions.md) | CI .NET 10/MySQL riêng cho PR, kết quả TRX và cách bật check bắt buộc sau khi chạy thật | Đã viết workflow; chờ run GitHub/review |
| [`test-reports/`](test-reports/2026-11-22-integration-ui-e2e.md) | Kết quả từng đợt kiểm thử (`YYYY-MM-DD-<dot>.md`) | Có báo cáo tích hợp/UI ngày 22/11/2026, đang review |
| `defects.md` | Danh sách lỗi (hoặc liên kết tới GitHub Issues gắn nhãn bug) | Chưa có |

## Bộ test tự động hiện có
Nằm ở `tests/ConvenienceStore.Web.Tests/`: `DashboardServiceTests`, `OperationalReportsServiceTests`, `ReportExcelExporterTests` (BC), `PromotionServiceTests`, `PromotionManagementTests` (KM).

Đợt tích hợp ngày 22/11 bổ sung `PosPageAuthorizationTests` và `ReportPromotionMySqlRegressionTests`. Toàn bộ project test tại commit `9632942` có **697/697 ca đạt** trên MySQL 8.4.13 riêng; xem [báo cáo và giới hạn kiểm chứng](test-reports/2026-11-22-integration-ui-e2e.md). Kết quả này không thay thế nghiệm thu toàn bộ MVP, CI hoặc staging.

Ngày 23/11, commit `dc89ec5` thêm CI/gate TRX và ba test migration mở rộng; **700/700 test Release đạt cục bộ**. Xem [báo cáo CI/schema](test-reports/2026-11-23-ci-schema.md); chưa xác nhận workflow đã chạy trên GitHub.

## Tiêu chí hoàn thành
- [ ] Mỗi mã chức năng MVP có ít nhất một test case.
- [ ] Mỗi API trong OpenAPI ở `04` được kiểm thử theo checklist trong tài liệu API.
- [ ] Không còn lỗi mức nghiêm trọng trước khi chuyển sang `07`.
