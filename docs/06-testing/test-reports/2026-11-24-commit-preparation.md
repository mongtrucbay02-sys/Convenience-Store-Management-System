# Chuẩn bị commit tích hợp PostgreSQL-only — 24/11/2026

Đã chuẩn bị bốn commit cục bộ trên `integration/smartmart-postgresql-only` để review. Không push, merge PR, đổi `main`/`develop` hay sửa bất kỳ database nào trong lượt này. Không phát hiện lỗi source/test mới; chỉ bổ sung tài liệu CI và provenance bằng chứng.

## Baseline và trạng thái đầu lượt

- HEAD ban đầu và `origin/develop`: `2e8d94b35d1426fa168c42417cae831677133208`; merge-base khớp đúng SHA này. Đây là baseline local remote-tracking đã xác minh, không khẳng định GitHub không có commit mới hơn sau lần fetch trước.
- Working tree đầu lượt: **251 đường dẫn** với `--no-renames`, gồm 152 modified, 42 deleted và 57 untracked; index trống. Không có conflict/unmerged entry.
- `develop` cục bộ giữ `0a0bd208431ee67ad8de6121ef7e0236ae2b0589`; `main` giữ `fb625af51571175190e0db15f1b3b6ffd712900b`. Chỉ nhánh tích hợp nhận các commit mới.

## Commit và file đã đưa vào

| SHA | Nhóm | Số file (Git nhận diện rename) |
|---|---|---:|
| `f29ebb3d0247602fb7c06dd0edaf509b520e8eba` | runtime | 113 |
| `4ce95b311d4f3eb83adfae325fee989940eae333` | business | 21 |
| `b1c04234a7b14e8ba7349b5538a0f86be1bdbefd` | tests-ci | 66 |
| Commit chứa báo cáo này | docs | 17 |

Danh sách file đầy đủ, trạng thái rename và ánh xạ mọi đường dẫn bị xóa nằm trong [commit inventory JSON](2026-11-24-commit-preparation.files.json). SHA của commit tài liệu có thể lấy bằng `git log -1 --format=%H -- docs/06-testing/test-reports/2026-11-24-commit-preparation.md`; không nhúng tự tham chiếu SHA vào chính commit.

- **Runtime/migration:** Npgsql-only, DbContext/DI/SQL, signed bigint xuyên các module; baseline PostgreSQL, snapshot/resource SQL và hướng dẫn adoption; archive migration/mapping/interceptor/tool MySQL.
- **Security/business:** forced-password gate MVC/API, UTC pricing/microseconds, promotion version/CAS và transaction; tìm NCC, lọc/ngày/sort/pagination phiếu nhập, hidden version trong Razor forms.
- **Tests/CI:** port fixture/regression sang PostgreSQL, test schema/security/concurrency/purchasing; PostgreSQL 17 service, guard publish/TRX, TLS của smoke chỉ đọc.
- **Docs:** inventory/module, setup/deployment, báo cáo test, CI hiện hành và báo cáo chuẩn bị commit. Các bảng `working_tree_uncommitted: true`/HEAD trong summary test cũ mô tả thời điểm chạy test trước commit; được giữ để không thay provenance.

Bốn file có thay đổi đan xen được chia hunk trong Git index: `EfCategoryMutationRepository`, `EfProductCreationRepository`, `ProductReadService`, `PromotionUsageStore`. Nội dung cuối working tree không bị thay đổi khi chia commit. Bằng chứng test áp dụng cho cả bộ commit cuối, không phải từng commit trung gian.

## File đầy đủ, archive và bảo toàn chức năng

- Hash byte của toàn bộ **546 file** trong manifest lần test trước khớp, không thiếu/khác source. Trong đó **545 file** có nội dung tương đương trong Git sau chuẩn hóa CRLF/LF; một `ConvenienceStore.Web.csproj.user` chỉ chứa thiết lập IDE `ActiveDebugProfile`, bị ignore theo `*.user`, cố ý không commit.
- Mọi source/test/migration cần thiết có trong Git. Inventory cũ bao phủ đủ 172 đường dẫn source/test ứng viên; inventory commit bổ sung ba đường dẫn tài liệu mới/chỉnh thêm trong lượt này.
- Tất cả **42 đường dẫn bị xóa** có bản PostgreSQL thay thế hoặc archive. **17 cặp archive** khớp nguyên nội dung baseline sau chuẩn hóa newline; Git hiển thị R100 cho các migration cũ chuyển chỗ. Snapshot MySQL được lưu bản riêng, snapshot active thay bằng PostgreSQL.
- `DashboardService.cs` và `IDashboardDataSource.cs` chỉ đổi newline, không đổi hành vi.
- Không xóa module nghiệp vụ; đối chiếu trước đó giữ 68 Razor Views, 47 controllers và 99 static assets, route/HTTP attributes không đổi. Lượt này không sửa thêm source/UI; kiểm tra giao diện/UAT đầy đủ vẫn chưa chạy.

## Rà soát secret, dữ liệu và migration

Không có file `artifacts/`, `bin/`, `obj/`, TRX, database cluster, dump/backup, `.env` cá nhân hoặc `*.user` được stage. Quét nội dung ứng viên và Git index không thấy private key, token GitHub/Supabase, JWT hoặc AWS key. Các vị trí password/connection string đã đối chiếu là placeholder, phép gán biến hoặc fixture/credential container CI riêng; không có mật khẩu Supabase. Email/điện thoại trong test là dữ liệu giả lập, không có export bản ghi khách hàng/nhân viên thật. Không chép raw TRX/log/cluster vào Git; chúng tiếp tục nằm trong artifacts bị ignore.

[POSTGRESQL-MIGRATIONS.md](../../db/POSTGRESQL-MIGRATIONS.md) và [runtime setup](../../05-implementation/setup/postgresql-runtime-setup.md) ghi rõ **cấm áp dụng initial baseline lên Supabase hiện hữu**. SQL nhúng bằng byte với `docs/db/schema_postgresql.sql`; tập tên 31 bảng khớp catalog contract Supabase được chụp trong repository. So sánh này không chứng minh catalog live, nội dung trigger, policy hay credential hiện tại trùng hoàn toàn. Không truy cập/ghi database trong lượt này. Ứng dụng không tự chạy migration; `Down` baseline bị vô hiệu hóa.

Adoption vẫn là blocker: owner phải xác minh backup, catalog đầy đủ, RLS/grants/role backend thực, TLS/pooler, cách ghi nhận migration history và incremental differences. Không đánh dấu baseline đã áp dụng chỉ vì trùng 31 tên bảng.

## Kết quả và bằng chứng

| Hạng mục | Kết quả | Phạm vi bằng chứng |
|---|---|---|
| Nhánh/HEAD/merge-base/baseline | PASS | Local target branch; baseline `2e8d94b`; main/develop không đổi |
| Source/test/migration đầy đủ | PASS | 546 hash khớp; 545 file source/config/test cần commit đã có; file IDE riêng loại trừ |
| Deleted/renamed/archive | PASS | 42 disposition; 17 archive nguyên nội dung sau chuẩn hóa newline |
| Secret/PII/generated-file review | PASS | Quét và review vị trí cụ thể; chỉ placeholder/fixture/CI credential riêng |
| `git diff --check`, staged và baseline-to-stack | PASS | Kiểm tra trong lượt chuẩn bị và trước commit |
| Build Release | PASS — chạy lại | `dotnet build ConvenienceStoreManagement.slnx -c Release --no-restore -v quiet`: 0 warning, 0 error; 2.22 giây |
| Full test PostgreSQL 17.11 | PASS — tái sử dụng | 763/763, 0 failed/skipped từ lần chạy trước; **không chạy lại test trong lượt này** |
| Test database mới / migration / seed / reset | NOT RUN | Không sửa database theo giới hạn lượt này |
| GitHub CI của các commit mới | NOT RUN | Chưa push; không suy ra CI từ test cục bộ |
| Supabase live adoption/runtime role | NOT RUN / BLOCKER | Catalog contract là reference đã chụp; cần owner/staging riêng |
| UAT/đủ màn hình/signoff DT/SRS | NOT RUN / BLOCKER | Quyết định scope và nghiệm thu còn thiếu |

Test cũ chạy từ `2026-11-24T17:16:49.4716833+07:00` đến `17:19:45.9812641+07:00`. TRX cục bộ: `artifacts/postgresql-integration/release-validation.trx`, SHA-256 `15fa3c51e09a04d856df2036921cf0173a3bd6a80ed4e351e9984411acc0dc01`. Manifest source trước commit: `02c2fad6433dc56886de6fa657d97b9de33b76407c5ecc58a1a9ab64ea2c4fad`. Build mới: `artifacts/postgresql-integration/commit-review/build-release.log`. [Báo cáo test và summary hash](2026-11-24-postgresql-integration.md) là bản lưu bằng chứng đã biên tập cho Git; raw artifacts không commit.

## Readiness và blocker còn lại

Bộ commit sẵn sàng review cục bộ và chuẩn bị PR khi người dùng cho phép push. Chưa đủ điều kiện merge/release: SM-04 (DT approval/exchange scope), SM-05/M0 (backup/adoption/role backend shared), SM-09 (owner/ruleset), SM-12 (release main), SM-14 (SRS/UAT/signoff) và SM-15 (SDK/image/lockfile tái lập) vẫn mở. Cần CI trên đúng commit cuối. Giới hạn số JSON/JavaScript trên `Number.MAX_SAFE_INTEGER` chưa được kiểm chứng; không mở rộng hợp đồng ID/giá khi chưa có quyết định.

Sau commit tài liệu, kiểm tra lại branch, `git status`, log, thống kê baseline-to-HEAD và file bị ignore. Working tree theo dõi phải sạch; artifacts và cấu hình cá nhân vẫn ở máy, ngoài commit. Không có thao tác push, merge PR hoặc sửa database được thực hiện.
