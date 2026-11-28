# Duyệt CSDL bổ sung – migration `PostgreSqlBaseline` và đối chiếu Supabase (28/11/2026)

| | |
| --- | --- |
| Người duyệt | #1 Phạm Gia Thăng (người duyệt CSDL; phần tài liệu CSDL theo phân công của #5) |
| Đối tượng | `src/ConvenienceStore.Web/Data/Migrations/20261124050401_PostgreSqlBaseline.cs` và `Sql/PostgreSqlBaseline.sql` (1.046 dòng), gộp qua PR #286 ngày 24/11 |
| Commit đối chiếu | `develop` `cc20336` |
| Supabase | project `convenience-store` (`cgspynsairbgvawfuhbu`), schema `smartmart`. Đọc qua dashboard ngày 28/11, không chạy SQL, không sửa gì |
| Liên quan | ADR-003; biên bản T8-02 mục 7; PR #300 (security contract, #293) |

## 1. Đối chiếu 5 tiêu chí

| Tiêu chí | Kết quả | Căn cứ |
| --- | --- | --- |
| 1. Sinh cho `CuaHangDbContext`, đúng thư mục, có `Up()`/`Down()` | **Đạt có ghi chú.** `Up()` chạy tệp SQL nhúng; `Down()` cố ý ném `NotSupportedException` ("khôi phục từ bản sao lưu đã duyệt") | Migration baseline không có trạng thái trước để quay về; chấp nhận được nếu quy trình sao lưu và khôi phục có thật (`docs/07-deployment/backup-restore.md` ở PR #304 – chưa diễn tập) |
| 2. Không sửa hay xóa migration đã gộp | **Ngoại lệ có căn cứ.** Commit `f29ebb3` (#286) xóa 6 migration MySQL, thay bằng một baseline | Đổi nền tảng CSDL theo ADR-003. Migration MySQL không chạy được trên PostgreSQL; lịch sử vẫn còn trong git và tag bản lùi (chưa gắn) |
| 3. Khớp ERD v1 + phần bổ sung đã duyệt | **Đạt.** 31 bảng = 28 bảng ERD v1 + `hoan_tien_hoa_don`, `quy_tien_mat`, `giao_dich_tien_mat` (CP-01 → CP-03, đã chốt X3); 62 FK; 3 view | SQL baseline: 31 `CREATE TABLE`, 62 tham chiếu FK, 3 view. Tài liệu đối chiếu `docs/04-system-design/database/reconciliation-20261123/` |
| 4. Không phá trigger và CHECK | **Đạt.** 17 trigger: 6 chặn sửa/xóa sổ lịch sử, 1 bảo vệ hóa đơn, 10 cập nhật `ngay_cap_nhat`. 89 CHECK (ENUM MySQL chuyển thành `varchar + CHECK`) | `supabase-schema-contract.json`: 203 ràng buộc = 31 PK + 62 FK + 21 UNIQUE + 89 CHECK; test `Model_MatchesReadOnlySupabaseCatalog` |
| 5. `dotnet test` đạt trên CSDL đích (từ 23/11: PostgreSQL 17) | **Đạt.** CI lượt `cc20336`: 877/877; #1 chạy lại cục bộ ngày 27/11 trên PostgreSQL 17.12 (Docker): 877/877 | Log CI; tệp TRX cục bộ |

**Kết luận: duyệt bổ sung** migration `PostgreSqlBaseline` về **cấu trúc**, với hai ghi chú:
- (a) Phần cấp quyền trong baseline (`GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES` cho `smartmart_backend`) **rộng hơn** Supabase thật. Phần quyền theo PR #300; không dùng baseline làm căn cứ quyền.
- (b) #286 được gộp trước lượt duyệt này (gộp với một người duyệt code). Ghi nhận là sai lệch quy trình.

## 2. Đối chiếu Supabase thật ngày 28/11 (chỉ đọc)

| Hạng mục | Supabase 28/11 | Bản chụp trong repo | Kết quả |
| --- | --- | --- | --- |
| Bảng và view | 31 bảng + 3 view | `supabase-schema-contract.json` (23/11): 31 + 3 | Khớp |
| Số cột từng bảng | 31/31 bảng | Như trên | Khớp từng bảng |
| Trigger | 17, đúng tên | 17 | Khớp |
| RLS và policy | 31/31 bảng bật RLS, mỗi bảng một policy `smartmart_backend_trusted_server` (ALL → `smartmart_backend`) | `observed-contract.json` (26/11, PR #300) | Khớp |
| Vai trò `smartmart_backend` | Còn tồn tại, 0 kết nối lúc xem | Như trên | Khớp |
| Dữ liệu | **Đã có** (ước tính): 25 hóa đơn, 46 dòng hóa đơn, 75 dòng thẻ kho, 32 giao dịch quỹ, 18 sản phẩm, 15 phiếu nhập, 2 tài khoản | Ngày 23/11 trống | **Mới, chưa rõ nguồn** |

Ràng buộc (PK/FK/UNIQUE/CHECK) không xem được qua dashboard nếu không chạy SQL. Căn cứ cho phần này là `supabase-schema-contract.json`, được CI kiểm với mô hình EF mỗi PR, cùng smoke test đọc 31 bảng và 3 view ngày 26/11.

## 3. Việc cần làm

1. #2 ghi rõ dữ liệu trên Supabase do ai, bằng cách nào ghi vào (ứng dụng với `smartmart_backend`, seed bằng `postgres`, hay SQL tay). Nếu do ứng dụng ghi thì đó là bằng chứng ghi cho #293.
2. Sao lưu Supabase trước khi áp bất kỳ migration nào ở PR #300 (B08), và trước buổi demo 04/12.
3. #5 gắn tag bản lùi MySQL (commit trước #282), như ADR-003 mục 4 bước 1, nếu nhóm còn muốn giữ đường lùi.
4. Tài liệu CSDL dùng làm căn cứ từ nay:
   - **Cấu trúc:** `supabase-schema-contract.json` cùng `PostgreSqlBaseline.sql`.
   - **Quyền:** security contract ở PR #300.
   - ERD v1 và `schema_v1.sql` (MySQL) chỉ còn là lịch sử.
