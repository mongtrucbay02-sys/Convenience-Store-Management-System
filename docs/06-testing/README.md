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
| [test-reports/2026-11-24-bc-kh-km-acceptance.md](test-reports/2026-11-24-bc-kh-km-acceptance.md) | Sửa #266, đối chiếu ma trận #288, giảm trừ đổi hàng/chi tiết/bộ lọc/ID64 và tài liệu KH/KM PostgreSQL | 819/819 PASS cục bộ, build/publish đạt; chưa CI/review/UAT cuối |
| [test-reports/2026-11-24-commit-preparation.md](test-reports/2026-11-24-commit-preparation.md) | Commit theo nhóm, kiểm tra source/secret/archive và provenance test | Commit cục bộ; build chạy lại; test trước được tái sử dụng; chưa push/CI |
| [test-reports/2026-11-24-postgresql-integration.md](test-reports/2026-11-24-postgresql-integration.md) | PostgreSQL-only, SM-01/02/03/06/07/08/10/13, RLS và publish | 763/763 PASS cục bộ trên working tree baseline 2e8d94b; CI commit mới NOT RUN |
| [`test-reports/2026-11-24-supabase-smoke.md`](test-reports/2026-11-24-supabase-smoke.md) | Kiểm tra workflow Supabase chỉ đọc và các ca thiếu cấu hình/schema | PostgreSQL riêng và Supabase thật đều đọc được 34/34 đối tượng; GitHub workflow chưa chạy |
| [`test-reports/2026-11-23-supabase-entities.md`](test-reports/2026-11-23-supabase-entities.md) | Catalog Supabase 31 bảng, SELECT chỉ đọc và hồi quy model MySQL | Ánh xạ đã kiểm tra; E2E PostgreSQL/staging chưa xác nhận |
| [`test-reports/2026-11-23-postgresql-runtime.md`](test-reports/2026-11-23-postgresql-runtime.md) | Chuyển SQL PostgreSQL, test ghi/HTTP/publish và trạng thái Supabase/staging | 759/759 Release đạt cục bộ; CI mới/staging chưa xác nhận |
| `test-plan.md` | Chiến lược: unit / integration / API / UI, môi trường, tiêu chí đạt | Chưa có |
| `test-cases/` | Test case thủ công theo module, mã `TC-<module>-xx`, liên kết mã chức năng | Chưa có |
| `automated-tests.md` | Mô tả bộ test tự động trong `tests/ConvenienceStore.Web.Tests/` và cách chạy `dotnet test` | Chưa có |
| [`ci-github-actions.md`](ci-github-actions.md) | CI .NET 10/PostgreSQL 17 riêng cho PR, kết quả TRX và cách bật check bắt buộc sau khi chạy thật | Đã viết workflow; chờ run GitHub/review |
| [`test-reports/`](test-reports/2026-11-22-integration-ui-e2e.md) | Kết quả từng đợt kiểm thử (`YYYY-MM-DD-<dot>.md`) | Có báo cáo tích hợp/UI ngày 22/11/2026, đang review |
| `defects.md` | Danh sách lỗi (hoặc liên kết tới GitHub Issues gắn nhãn bug) | Chưa có |

## Bộ test tự động hiện có
Nằm ở `tests/ConvenienceStore.Web.Tests/`: `DashboardServiceTests`, `OperationalReportsServiceTests`, `ReportExcelExporterTests` (BC), `PromotionServiceTests`, `PromotionManagementTests` (KM).

Đợt tích hợp ngày 22/11 bổ sung `PosPageAuthorizationTests` và `ReportPromotionMySqlRegressionTests`. Toàn bộ project test tại commit `9632942` có **697/697 ca đạt** trên MySQL 8.4.13 riêng; xem [báo cáo và giới hạn kiểm chứng](test-reports/2026-11-22-integration-ui-e2e.md). Kết quả này không thay thế nghiệm thu toàn bộ MVP, CI hoặc staging.

Ngày 23/11, commit `dc89ec5` thêm CI/gate TRX và ba test migration mở rộng; **700/700 test Release đạt cục bộ**. Xem [báo cáo CI/schema](test-reports/2026-11-23-ci-schema.md); chưa xác nhận workflow đã chạy trên GitHub.

Cùng ngày, commit `ce06dff` sửa đồng bộ hủy/quỹ, thêm chốt số dư, lưu ảnh KM và bảy test; **707/707 test, 41/41 kiểm tra HTTP đạt cục bộ**. Xem [báo cáo sửa quỹ/ảnh](test-reports/2026-11-23-cash-promotion.md). Điện thoại để sau; migration chưa được duyệt, CI GitHub/staging/review độc lập chưa xác nhận.

Tuần 8 bổ sung KM-05/06 và BC-09/10/11: 733/733 test đầy đủ đạt; sau thay đổi cuối 27/27 ca liên quan đạt; 53/53 HTTP và ảnh trình duyệt trên MySQL tạm. Xem [báo cáo tuần 8](test-reports/2026-11-23-week8.md) để phân biệt phiên bản đã kiểm thử; CI/staging/review vẫn chờ.

## Tiêu chí hoàn thành
- [ ] Mỗi mã chức năng MVP có ít nhất một test case.
- [ ] Mỗi API trong OpenAPI ở `04` được kiểm thử theo checklist trong tài liệu API.
- [ ] Không còn lỗi mức nghiêm trọng trước khi chuyển sang `07`.
