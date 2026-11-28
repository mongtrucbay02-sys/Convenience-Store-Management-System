# Smart Mart PostgreSQL-only — thay đổi tích hợp 24/11/2026

> Bằng chứng tích hợp/test dưới đây ghi nhận snapshot trước khi commit. Trạng thái commit và kiểm tra bổ sung được ghi ở [báo cáo chuẩn bị GitHub](../06-testing/test-reports/2026-11-24-commit-preparation.md); không suy ra có lần chạy test mới.

Nhánh làm việc `integration/smartmart-postgresql-only`, baseline cuối `origin/develop@2e8d94b35d1426fa168c42417cae831677133208`. PR #282 đã được merge upstream trong lúc làm việc; đã đối chiếu tới head cuối `53a886c702b8ce1cc271c36b173a7cf1c66f4630`. Không sửa `main`/`develop` cục bộ, không commit/push/merge PR trong phiên này.

## Tích hợp có kiểm soát

1. Chụp HEAD, working tree, merge-base, 17/2 unique commits và diff với PR #282 tại `5a400938…`; 180 đường dẫn nguồn không trùng các thay đổi riêng của develop. `git apply --check` thành công trước áp patch.
2. Giữ các tài liệu/ADR mới trên develop và code tuần 8 đã có; không lấy cả snapshot cũ đè repository.
3. Đối chiếu cập nhật upstream: thêm bảy file smoke chỉ đọc của `53a886c…`. Fetch không prune; sau kiểm tra ancestry/index, cập nhật riêng nhánh tích hợp tiến tới `2e8d94b…`, giữ nguyên hash 825 working files. `main` và `develop` không đổi.
4. Thực hiện cleanup/fix trong cùng ứng dụng/context. Các file `MySql*` đổi tên `PostgreSql*`; những đường dẫn Git hiển thị deleted đều có bản renamed hoặc archive, đã kiểm tra bằng script.
5. Port có chọn lọc hành vi từ PR #201/#197 vào controllers/view hiện tại, không import `ApplicationDbContext` hoặc một bộ backend NH thứ hai. Không đóng PR hoặc xóa source branches.

## Thay đổi theo module

| Module | Hành vi/thay đổi | File chính |
|---|---|---|
| Data / DI | Chỉ `UseNpgsql`, PostgreSQL 17; một context; bỏ provider switch, mapping/converter/migration/time-zone MySQL khỏi assembly | `Program.cs`, `ConvenienceStore.Web.csproj`, `Data/CuaHangDbContext*.cs`, `Shared/Data/PostgreSqlEnv.cs`, `Shared/Data/SqlDialect.cs` |
| Migrations / security | Baseline EF native mới, snapshot, SQL embed bất biến; history `public`; 31 bảng RLS, grants riêng backend, revoke public/anon/authenticated; ba view invoker | `Data/Migrations/20261124050401_PostgreSqlBaseline.cs`, `.Designer.cs`, `CuaHangDbContextModelSnapshot.cs`, `Sql/PostgreSqlBaseline.sql`, `docs/db/schema_postgresql.sql` |
| AUTH | Global forced-password filter bảo vệ MVC và API; giữ exception đổi mật khẩu/logout; ID tài khoản/session long; SQL native UTC/expiry/idle, tìm kiếm và trùng tên/email không phụ thuộc collation MySQL | `Modules/Auth/Security/KiemTraDoiMatKhauFilter.cs`, `NguoiDungHienTai.cs`, `Services/DichVuXacThuc.cs`, `DichVuTaiKhoan.cs`, `KhoiTaoAdminDauTien.cs` |
| Products | So sánh instant UTC với clock DB sau khóa; giá tức thời không bị cắt giây gây lỗi; giữ trần giá SRS; chống trùng mã/tên danh mục qua fold và khóa giao dịch | `ProductPricingService.cs`, `EfProductPricingRepository.cs`, `EfProductCreationRepository.cs`, `EfCategoryMutationRepository.cs`, `EfProductReadRepository.cs` |
| Promotions | DTO/form/version cho edit/status/delete; CAS và tăng version; EF token; khóa hàng trước kiểm tra lại usage; transaction head + product links; stale/usage conflict không ghi đè | `PromotionModels.cs`, `PromotionFormModel.cs`, `PromotionService.cs`, `PostgreSqlPromotionDataSource.cs`, `PromotionUsageStore.cs`, `IPromotion*.cs`, các controllers và ba views KM |
| POS / Customers | ID native long xuyên catalog/giỏ/checkout/hóa đơn/hủy; số dư điểm long; không ép về uint; ngày mã HD từ timestamp đổi sang Việt Nam | `Services/Pos/PostgreSqlPos*.cs`, `PosCartService.cs`, `PosHeldOrderService.cs`, `PosInvoiceCancellationService.cs`, `Models/Pos/*.cs`, `CustomerPoints.cs`, `Shared/Api/GioVietNam.cs` |
| Inventory / Returns | Đồng bộ chữ ký ID long, đọc DB long và bỏ cast ID về int tại kho/phiếu/lookup/refund; giữ giới hạn số lượng và nghiệp vụ DT hiện có | `IDichVuKho.cs`, `DichVuKho.cs`, `Models/*.cs`, `ReturnOrderService.cs`, `PostgreSqlInvoiceLookupStore.cs`, `ReturnPointsGateway.cs`, `CashLedgerService.cs` |
| Purchasing | NCC/date inclusive/status validation; ngày giảm dần + ID giảm dần; pagination giữ mọi bộ lọc; search NCC/phiếu fold tiếng Việt; status lạ trả lỗi | `PurchaseReceiptsController.cs`, `SuppliersController.cs`, `PurchaseReceiptService.cs`, `Views/Purchasing/Index.cshtml` |
| Shared / Reports | Bỏ mọi nhánh SQL MySQL, dùng Npgsql/SQLSTATE/constraint name; chuẩn hóa tham số thời gian cho sync/async; fold cả ký tự hoa tiếng Việt trên locale C; rename adapters báo cáo | `Shared/Data/*.cs`, `Data/PostgreSqlTimeInterceptor.cs`, `Modules/Reports/Services/PostgreSql*.cs`, `WeekEightReportDataSource.cs`, `DashboardController.cs` |
| CI / publish | Chỉ PostgreSQL 17 service; full test + strict TRX; publish và guard không MySQL/secrets/dev config; smoke Supabase chỉ đọc giữ TLS chặt khi người vận hành cấu hình | `.github/workflows/ci.yml`, `supabase-smoke.yml`, `scripts/ci/verify-postgresql-runtime.ps1`, `tools/SupabaseSmoke/Program.cs` |
| Tests / setup | Fixture Npgsql tạo DB loopback riêng, business regression port; thêm UTC/CAS/RLS/bigint/date/NH HTTP tests; templates dùng private role, không password thật | `tests/ConvenienceStore.Web.Tests/PostgreSql/*.cs`, `HaTang/CsdlPostgreSqlTam.cs`, các test `*PostgreSql*`, `.env*.example`, `docs/db/POSTGRESQL-MIGRATIONS.md` |

Danh sách tất cả đường dẫn thay đổi và bản renamed/archive: [file inventory JSON](postgresql-integration-files-20261124.json). Snapshot source/hash/binary và kết quả: [báo cáo kiểm chứng](../06-testing/test-reports/2026-11-24-postgresql-integration.md).

## Bảo toàn giao diện và tính năng

- Còn đủ **68 Razor Views, 47 controllers, 99 static assets** so với baseline PR #282 mới nhất; không mất file. Các attribute Route/HTTP không đổi.
- Chỉ bốn view chỉnh: `Promotions/Form`, `Promotions/Index`, `Promotions/Delete` mang version; `Purchasing/Index` thêm bộ lọc/pagination. Không tạo SPA/frontend thứ hai.
- Giữ flow AUTH, Products, Purchasing, Inventory, POS, Customers, Promotions, Returns, Reports, cash ledger và tuần 8. Không xóa luồng DT chưa đủ quyết định; trang đổi hàng/phiếu cần duyệt vẫn là finding mở.
- Chưa thực hiện refactor di chuyển toàn bộ POS theo #280; giữ đường dẫn hiện hành để tránh xung đột rename hàng loạt. Đây là HOLD trong Issue #283, không phải mất chức năng.
- `docs/archive/mysql-runtime` giữ migration/mapping/interceptor MySQL cũ; `mysql-tests` giữ schema/upgrade tests đặc thù cũ; `mysql-tools` giữ tool audit #168. Tất cả nằm ngoài compile/runtime/CI phát hành.

## Gate và thao tác tiếp theo

763/763 test PostgreSQL riêng và publish guard PASS là bằng chứng kỹ thuật cục bộ. Còn cần review/commit theo quy trình của owner, CI trên đúng commit chứa sửa, M0 backup/catalog/adoption/role shared, quyết định DT/SRS và UAT. Không tự merge, push hoặc triển khai từ báo cáo này.

Baseline mới chỉ áp cho DB rỗng/riêng. Không chạy lại trên Supabase đang có dữ liệu; cần DB owner đối chiếu schema, trigger bodies, index, RLS, grants và ghi nhận adoption có review. Kiểm thử ghi phải dùng staging/DB test riêng; không dùng Supabase chung làm fixture. Giới hạn giá, delta điểm, số lượng và contract JSON hiện có được giữ khi chưa có quyết định thay đổi.
