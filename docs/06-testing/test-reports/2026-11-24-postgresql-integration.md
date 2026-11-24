# Báo cáo tích hợp PostgreSQL-only — 24/11/2026

> Bằng chứng tích hợp/test dưới đây ghi nhận snapshot trước khi commit. Trạng thái commit và kiểm tra bổ sung được ghi ở [báo cáo chuẩn bị GitHub](2026-11-24-commit-preparation.md); không suy ra có lần chạy test mới.

Kết quả cục bộ: **763/763 PASS, 0 FAIL, 0 skipped**; build Release **0 warning, 0 error**. Một ứng dụng .NET 10 MVC/Razor, một `CuaHangDbContext`, runtime EF Core/Npgsql duy nhất. Sẵn sàng review thay đổi; chưa đủ gate triển khai/nghiệm thu Supabase.

## Baseline và phạm vi bằng chứng

- Nhánh: `integration/smartmart-postgresql-only`. HEAD/baseline cuối: `2e8d94b35d1426fa168c42417cae831677133208`, khớp `origin/develop` đã fetch/xác minh.
- Lúc bắt đầu: HEAD và develop `0a0bd208431ee67ad8de6121ef7e0236ae2b0589`, working tree sạch. Merge-base với PR #282 cũ: `b2ed284f746b01de2333ce0ff7c0b9ce6a7ff8f5`; hai phía có 17/2 commit riêng, không trùng đường dẫn thay đổi. Patch 180 đường dẫn đã qua `git apply --check` trước khi áp dụng.
- Trong lúc thực hiện, GitHub đã merge PR #282 lúc `2026-11-24T09:30:42Z`, head cuối `53a886c702b8ce1cc271c36b173a7cf1c66f4630`. Bảy file bổ sung smoke chỉ đọc được đối chiếu và tích hợp. Nhánh nguồn không còn trên remote; không prune/xóa nhánh từ phiên này.
- Chỉ nhánh tích hợp được cập nhật tiến tới baseline mới. Hash 825 file làm việc không đổi trong thao tác cập nhật metadata; `main` và `develop` cục bộ giữ nguyên. Không tạo commit, push, merge/đóng PR hoặc xóa remote branch.
- Kết quả test thuộc **working tree chưa commit**, không phải chứng nhận CI của commit nền. Hash tập source/config/test: `02c2fad6433dc56886de6fa657d97b9de33b76407c5ecc58a1a9ab64ea2c4fad` (546 file). Binary ứng dụng trong thư mục build, test, publish và smoke tool có cùng SHA-256; xem [summary máy đọc](2026-11-24-postgresql-integration.summary.json).

Môi trường thực đo: .NET SDK **10.0.401**; EF Core/Relational **10.0.12**; Npgsql và Npgsql EF provider **10.0.3**; PostgreSQL **17.11** native Windows tại `127.0.0.1:55439`, data directory riêng dưới `artifacts/postgresql-integration/pgdata`; cluster đã dừng sau kiểm tra. Không dùng PostgreSQL/Supabase chung để chạy fixture, migration, seed, reset hoặc test ghi.

## Bảng kiểm chứng

| Kiểm tra | Kết quả | Bằng chứng / giới hạn |
|---|---|---|
| Build solution Release | PASS | `artifacts/postgresql-integration/build-current.log`: 0 warning/error |
| Toàn bộ suite trên PostgreSQL 17 riêng | PASS | `release-validation.trx`: 763 executed/passed, failed=0, notExecuted=0; đã chạy `verify-trx.ps1` |
| SM-01 bắt buộc đổi mật khẩu | PASS | HTTP MVC GET/POST và API GET/POST bị chặn; không tạo khách hàng; logout/đổi mật khẩu được phép; sau đổi mật khẩu vào nghiệp vụ được |
| SM-02 đổi giá UTC | PASS | Giá tức thời giữ độ chính xác DB; quá khứ `+07` bị từ chối; tương lai và instant `Z/+07` đối chiếu đúng; clock DB đọc sau khóa |
| SM-03 khuyến mãi | PASS | Hai editor cùng version chỉ một thắng; stale status/delete bị từ chối; EF cũng có token/version; lượt dùng checkout commit khi editor chờ khóa chặn sửa hạn chế; lỗi FK rollback cả version/head/liên kết |
| SM-06/07 | PASS | ID và số dư >32-bit qua catalog/giỏ/checkout/hóa đơn/hủy; mốc `16:59:59Z`, `17:00:00Z` và `+07` tạo ngày chứng từ Việt Nam |
| SM-08/13 Purchasing | PASS | 14 ca mới: NCC/ngày inclusive/status, thứ tự ngày+ID, phân trang giữ bộ lọc, dữ liệu sai, tìm tiếng Việt, AD/NK/TN/forced/anonymous |
| Model/catalog/baseline | PASS | 31 bảng, 307 cột, 62 FK, 89 CHECK, 17 trigger, 3 view; snapshot không có pending model changes; fresh migration áp dụng được trên DB riêng |
| RLS, grants, invoker view | PASS | 31 bảng RLS; 3 view `security_invoker`; backend role qua `SET ROLE` không superuser/BYPASSRLS/owner đọc được và CRUD+sequence được trên DB riêng; role không tin cậy không đọc xuyên bảng/view; probe CRUD rollback toàn bộ |
| Giữ frontend/routes | PASS | Còn đủ 68 Razor Views, 47 controllers, 99 static assets; không đổi các attribute route/HTTP; chỉ 4 view sửa cho version/bộ lọc; các trang thật được kiểm tra HTTP |
| Publish PostgreSQL-only | PASS | `publish-verification.log`: Npgsql-only, EF thống nhất; không MySQL/Pomelo DLL/dependency, `.env*` hoặc `appsettings.Development.json`; binary khớp binary đã test |
| Sinh SQL migration offline | PASS | `postgresql-baseline.sql` 53,446 byte; factory thiết kế localhost inert; không kết nối/apply Supabase |
| Smoke tool mới từ PR #282 | PASS | DB riêng: 34/34 đối tượng, transaction read-only; thiếu config exit 2; thiếu schema exit 1/42P01; không báo xanh giả |
| Đăng nhập psql/Npgsql bằng role runtime trên shared Supabase | NOT RUN | Chưa có cấu hình runtime/credential được xác minh trong phiên; connector/owner hoặc log lịch sử không thay thế cùng role triển khai |
| CI trên commit cuối chứa các sửa này | NOT RUN | Chưa commit/push theo giới hạn công việc; không dùng 759/759 của SHA cũ làm bằng chứng mới |
| UAT/staging, duyệt SRS/DT, kiểm tra UI trực quan đầy đủ | NOT RUN | Chưa có môi trường/signoff quyết định scope; không suy ra từ unit/integration test |

Các ca smoke âm trả lỗi đúng là PASS của phép kiểm tra, không cộng vào 763. Không cộng 34/34 filtered hoặc 14/14 Purchasing vào tổng: **763 là một lần chạy full suite riêng**. Các lượt trung gian FAIL được giữ trong artifacts để truy vết, không dùng làm trạng thái cuối.

## Test thay đổi và khả năng so sánh

- Test nghiệp vụ MySQL được chuyển sang `CsdlPostgreSqlTam`/Npgsql và SQL PostgreSQL, giữ assertion nghiệp vụ; fixture chỉ nhận loopback, tạo/xóa tên DB ngẫu nhiên của chính nó. Không giữ MySQL service hay provider dự phòng.
- 22 ca chỉ đặc thù schema/lịch sử nâng cấp MySQL được lưu ở `docs/archive/mysql-tests`; không còn thuộc runtime test phát hành. Guard migration MySQL và temporary unsigned key test được thay bằng kiểm tra baseline/snapshot native PostgreSQL. Không giả vờ toàn bộ 759 ca cũ vẫn nguyên trạng.
- Bằng chứng thay thế: `PostgreSql/SupabaseModelMappingTests.cs`, `PostgreSql/PostgreSqlSchemaTests.cs`, các kiểm thử generated columns/transaction nghiệp vụ được port; thêm regression trong `IntegrationRegressionTests.cs` và `PurchasingParityTests.cs`.
- Khoản tiền, số lượng và delta điểm vẫn theo giới hạn nghiệp vụ/schema hiện có; trần giá 4,294,967,295 được giữ theo SRS, tách khỏi kiểu lưu bigint. Không tự mở rộng chính sách giá.
- Không tuyên bố đã kiểm chứng toàn dải Int64 ở JavaScript/JSON hoặc mọi màn hình bằng trình duyệt. Test ID/số dư lớn thực hiện quanh 5 tỷ; hợp đồng số JSON của client được giữ nguyên. Nếu yêu cầu vượt `Number.MAX_SAFE_INTEGER`, cần quyết định hợp đồng ID chuỗi và test UI tương ứng.

## Findings còn lại và readiness

| Finding | Trạng thái cuối | Điều kiện còn thiếu |
|---|---|---|
| SM-01/02/03 | FIXED / VERIFIED local | CI/review trên commit chứa thay đổi trước tích hợp chính thức |
| SM-06/07/08/10/13 | FIXED / VERIFIED trong phạm vi trên | Shared-role adoption/UAT là gate riêng; không đồng nhất với test admin cục bộ |
| SM-04 | OPEN / BLOCKER scope | DT-03/06: còn `CHO_DUYET` và trang đổi hàng giữ chỗ; Issue #283 M0 chưa quyết định duyệt/từ chối/đổi hàng thuộc phạm vi nào. Giữ nguyên module và dữ liệu |
| SM-05 | OPEN / BLOCKER deployment | Backup/baseline shared, cùng credential `smartmart_backend`, non-owner/NOBYPASSRLS, grants/policies, TLS/pooler và staging riêng chưa được xác minh |
| SM-09 | OPEN / NOT RUN | Ruleset/required CI/review là việc của owner; không thay cấu hình GitHub hoặc main trong phiên này |
| SM-11 | UPDATED | Báo cáo mới gắn HEAD + hash working tree + TRX thực; tài liệu cũ được ghi rõ lịch sử/superseded, không tự nhận CI mới đã chạy |
| SM-12 | OUT OF SCOPE / NOT RUN | `main` không được sửa/triển khai; release PR và smoke main chỉ sau gate tích hợp |
| SM-14 | OPEN / BLOCKER acceptance | SRS/AC/signoff và scope DT/NH chưa được duyệt; không tự ghi Approved hoặc coi survey mô phỏng là khảo sát thật |
| SM-15 | OPEN / NOT RUN reproducible release | Đã ghi chính xác SDK/server/package thực; chưa khóa toàn bộ SDK/image digest/lockfile cho release tái lập |

M1 và các phần port NH có bằng chứng cục bộ đã sẵn sàng review. Chưa đủ điều kiện merge/release hay tuyên bố nghiệm thu hoàn chỉnh: còn M0, CI exact commit, quyết định DT/SRS và UAT. Không tự chạy migration/seed/reset trên Supabase hiện có để làm các gate này xanh.

Danh sách module/file, cách tích hợp và rủi ro chức năng: [implementation inventory](../../05-implementation/postgresql-only-integration-20261124.md). Quy trình baseline: [PostgreSQL migrations](../../db/POSTGRESQL-MIGRATIONS.md). Log/TRX/SQL đầy đủ ở `artifacts/postgresql-integration` (ignored, cục bộ); summary JSON bên cạnh báo cáo giữ số liệu và hash cho review.
