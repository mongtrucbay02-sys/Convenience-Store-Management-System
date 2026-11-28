# Nhật ký duyệt thay đổi CSDL (T4-09, T5-R1, T6-R1, T7-R1)

Theo Kế hoạch v3 mục 1.4: mọi thay đổi cấu trúc CSDL phải đi qua tệp migration trong pull request và được #1 duyệt; không ai sửa trực tiếp CSDL dùng chung. Migration nằm ở `src/ConvenienceStore.Web/Data/Migrations/`. CODEOWNERS trước đây ghi nhầm `/src/ConvenienceStore.Web/Migrations/` nên PR có migration không tự gán #1; PR này sửa lại đường dẫn.

Nguồn chuẩn của cấu trúc CSDL: `docs/db/schema_v1.sql` (ERD v1, 18/10/2026) và migration `InitialCreate` trên `develop`, sinh bằng `CuaHangDbContext` (scaffold từ CSDL, ADR-001 mục 3.8). Thay đổi mới viết thành migration tiếp theo của `CuaHangDbContext`, không tạo DbContext thứ hai.

## Lượt rà tuần 4 (19/10 → 25/10/2026) – ghi bổ sung ngày 22/11/2026 (#31, T4-09)

Tuần 4 không có lượt rà nào được ghi lại đúng hạn. Bảng dưới đây rà lại theo lịch sử pull request.

| PR | Tác giả | Nhánh đích | Thay đổi CSDL | Kết luận | Ghi chú |
| --- | --- | --- | --- | --- | --- |
| #129 [T4-27] POS-09 | #3 Hoài | `develop` | Tệp SQL rời `docs/database/migrations/20261025_pos09_cancelled_orders.sql` (tạo 25/10) | Không được rà trong tuần 4; rà ngày 02/11: **không duyệt** | Xem lượt rà 02/11. PR vẫn đang mở |

Bảng task v3 (tệp Excel đính kèm issue #2, #118) không được cập nhật trong tuần 4. Bảng này không nằm trong repo; từ đây trạng thái task theo dõi bằng issue GitHub.

## Lượt rà tuần 5 (26/10 → 01/11/2026) – ghi bổ sung ngày 22/11/2026 (#88, T5-R1)

| PR | Tác giả | Nhánh đích | Thay đổi CSDL | Kết luận | Ghi chú |
| --- | --- | --- | --- | --- | --- |
| #131 [T3-11] Chuyển khung máy chủ sang MySQL 9.7.2 | #1 Thăng | `develop` (gộp 27/10) | `CuaHangDbContext`, migration `InitialCreate` và `SchemaV1`, `docs/db/schema_v1.sql` và các script kiểm thử | Gộp **không có người duyệt** | Người viết cũng là người duyệt CSDL, nên không có duyệt độc lập. Cấu trúc khớp ERD v1 (đối chiếu lại ở lượt 02/11 khi so với #168: 28 bảng, 3 view, 10 trigger, 53 CHECK) |
| #132, #134, #135, #136, #137, #138 | #1 Thăng | `develop` (gộp 27/10) | Chứa lại nguyên các tệp CSDL của #131 (các PR xếp chồng), không thêm migration mới | Gộp **không có người duyệt** | Không đổi cấu trúc CSDL so với #131 |
| #130 Code First baseline | #2 Yến | `main` | `ApplicationDbContext` riêng, 3 migration "SchemaProbe" | Không được rà trong tuần 5; rà ngày 02/11: **không duyệt** | Xem lượt rà 02/11 |
| #168 Complete Code First Schema | #2 Yến | `main` | Như #130, đủ các bảng | Không được rà trong tuần 5; rà ngày 02/11: **không duyệt** | Xem lượt rà 02/11. PR vẫn đang mở |

Kết luận tuần 5: các thay đổi CSDL đã gộp trong tuần đều do #1 viết và tự gộp, không có người duyệt thứ hai. Bảng task v3 không được cập nhật.

## Lượt rà 02/11/2026 (tuần 6)

Rà toàn bộ pull request đang mở có tệp migration, `.sql`, DbContext hoặc entity.

| PR | Tác giả | Nhánh đích | Thay đổi CSDL | Kết luận | Lý do và việc cần làm |
| --- | --- | --- | --- | --- | --- |
| #129 [T4-27] POS-09 hủy đơn đang bán | #3 Hoài | `develop` | Thêm bảng `pos_cancelled_orders` bằng tệp SQL rời | **Không duyệt; đề nghị đóng** | ERD v1 đã lưu đơn hủy trong `gio_hang` (`trang_thai = 'DA_HUY'`, lý do, giá trị, thời điểm hủy). POS-09 trên `develop` đã dùng cách này và chạy đúng khi thử 01/11. Bảng mới sẽ trùng dữ liệu, không có khóa ngoại, và không đi qua migration. PR đang xung đột với `develop` |
| #130 Feature/db core codefirst baseline | #2 Yến | `main` | `ApplicationDbContext` code-first riêng, 3 migration "SchemaProbe" | **Không duyệt** | Tạo DbContext thứ hai song song với `CuaHangDbContext`; hai bộ migration sẽ dựng hai CSDL khác nhau. Đã được thay bằng #168 |
| #168 [DB] Complete Code First Schema | #2 Yến | `main` | Như #130, mở rộng đủ các bảng (116 tệp, khoảng 44.000 dòng) | **Không duyệt**; giữ lại phần test | Cùng lý do với #130. Đề nghị: chuyển code SP-07 (#169) và test tích hợp sang `CuaHangDbContext` trên `develop`; nếu thấy ERD v1 thiếu cột hay ràng buộc nào thì mở issue nhãn "thay đổi yêu cầu", #1 thêm migration mới |
| #169 [SP-07] Category List API | #2 Yến | nhánh của #168 | Phụ thuộc DbContext của #168 | Chờ chuyển nhánh | Code nghiệp vụ dùng được; chỉ cần đổi sang `CuaHangDbContext` và nhắm `develop` |
| #194, #195 Báo cáo BC-02 → BC-12 | #5 Trung | `main` | Không có tệp CSDL | Không thuộc phạm vi duyệt CSDL | Lưu ý: nhắm `main` trong khi AUTH, kho, POS ở `develop`; nên nhắm `develop` để không làm hai nhánh lệch thêm (lỗi 5 trong bản đánh giá) |
| #196, #197 SP-08, NH-01 (Draft) | #2 Yến | nhánh code-first | Không có tệp migration mới | Chờ | Sẽ cần chuyển nhánh như #169 |

Kết quả: không có thay đổi CSDL nào được gộp mà chưa duyệt. Không có migration mới nào cần thêm vào `CuaHangDbContext` trong tuần 6. T-KHO-SV2 (#140) không đổi cấu trúc CSDL.

Bổ sung ngày 22/11/2026: lượt rà trên bỏ sót **#204** `[DB] Optimistic concurrency cho DanhMuc` (#2 Yến, sửa `CuaHangDbContext` và thêm test), tạo và gộp ngay trong ngày 02/11, không có người duyệt. Thay đổi chỉ cấu hình `IsConcurrencyToken()`, không đổi cấu trúc bảng.

## Lượt rà tuần 7 (07/11 → 13/11/2026) – ghi bổ sung ngày 22/11/2026 (#172, T7-R1)

| Ngày | PR | Tác giả | Thay đổi CSDL | Kết luận | Ghi chú |
| --- | --- | --- | --- | --- | --- |
| 10/11 | #207 `[DB,Fix] Enforce DanhMuc optimistic concurrency` | #2 Yến | `CuaHangDbContext` (token concurrency), test tích hợp | Gộp không có người duyệt; không đổi cấu trúc bảng | Bằng chứng test trong PR (6/6) chạy trên MySQL 8.4, chưa phải MySQL 9.7.2 như tiêu chí 5 |
| 12/11 | #217 `feat(pos-12)` | #3 Hoài | Migration mới `20261112160000_Pos12CancelInvoice`: bảng `hoan_tien_hoa_don` và 2 trigger | Gộp 15/11, chỉ #5 duyệt, **chưa qua duyệt CSDL** | Đạt tiêu chí 1, 2 (có `Up()`/`Down()`, sinh cho `CuaHangDbContext`). **Chưa đạt tiêu chí 3**: bảng không có trong ERD v1, `docs/db/schema_v1.sql` không được cập nhật, không có issue "thay đổi yêu cầu" |

## Bàn giao (14/11 → 22/11/2026)

Ghi lại để trưởng nhóm mới (#5 Trung) tiếp tục rà.

| Ngày | PR / commit | Tác giả | Thay đổi CSDL | Hiện trạng | Ghi chú |
| --- | --- | --- | --- | --- | --- |
| 14/11 | #219 Hợp nhất có chọn lọc lược đồ PR #168 | #2 Yến | `CuaHangDbContext`, test cấu trúc | Gộp 15/11, #5 duyệt | Chưa có lượt duyệt CSDL ghi nhận |
| 14/11 | #220 Ánh xạ cột sinh tự động | #2 Yến | `CuaHangDbContextModelSnapshot.cs` | Gộp 15/11, #5 duyệt | Chưa có lượt duyệt CSDL ghi nhận |
| 17/11 | Commit `ea4ecee` "fix: synchronize return cash ledger and tests" | #3 Hoài | Migration mới `20261112170000_CashLedger`: bảng `quy_tien_mat`, `giao_dich_tien_mat` | **Đẩy thẳng vào `develop`, không qua pull request** | Có `Up()`/`Down()`. **Chưa đạt tiêu chí 3**: bảng không có trong ERD v1, `schema_v1.sql` không được cập nhật, không có issue "thay đổi yêu cầu" |
| — | #129, #168 | #3 Hoài, #2 Yến | Xem lượt rà 02/11 | Vẫn đang mở dù đã kết luận không duyệt | |

## Mẫu ghi cho các lượt sau

| Ngày | PR | Tác giả | Thay đổi CSDL | Kết luận | Ghi chú |
| --- | --- | --- | --- | --- | --- |
| | | | | | |

Tiêu chí duyệt một migration:

1. Sinh từ `CuaHangDbContext`, đặt trong `src/ConvenienceStore.Web/Data/Migrations/`, có cả `Up()` và `Down()`.
2. Không sửa hay xóa migration đã gộp; mọi thay đổi là migration mới.
3. Khớp ERD v1 hoặc có issue "thay đổi yêu cầu" đã được #1 đồng ý; cập nhật `docs/db/schema_v1.sql` trong cùng PR.
4. Không phá trigger và CHECK hiện có (chạy `docs/db/kiem_thu.sh`: 29/29 thao tác sai vẫn bị chặn).
5. `dotnet test` đạt trên MySQL 9.7.2. **Từ 23/11/2026** (ADR-003): đạt trên PostgreSQL 17; migration MySQL chỉ còn áp dụng cho bản lùi.

## Bổ sung đối chiếu 23/11/2026 — chưa phải approval

#5 Trung chuẩn bị [bộ đối chiếu migration/ERD/SRS](../04-system-design/database/reconciliation-20261123/README.md) cho `Pos12CancelInvoice` và `CashLedger`, gồm ERD vật lý bổ sung, từ điển, DDL tham chiếu và các quyết định còn mở. Không sửa migration lịch sử hoặc áp CSDL chung. Phiên bản MySQL có bằng chứng hiện tại là 8.4.13; workflow CI chọn nhánh 8.4, cần nhóm chốt lại ma trận phiên bản thay vì mặc định đã kiểm chứng 9.7.2. Mục này ghi nhận sản phẩm đối chiếu, **không xác nhận người phụ trách CSDL đã duyệt**.

Đợt sửa tiếp theo tại commit `ce06dff` đề xuất hai migration `CashLedgerReconciliation` và `PromotionImage`: [thiết kế và hướng dẫn nâng cấp](../04-system-design/database/cash-promotion-20261123.md), [đề xuất SRS](../03-requirements/cash-promotion-change-proposal-20261123.md), [kết quả kiểm thử](../06-testing/test-reports/2026-11-23-cash-promotion.md). **Đang chờ #1 duyệt theo #234**; chưa ghi Aiven chung hoặc cập nhật trạng thái Approved của schema/SRS.

## Đổi nền tảng CSDL 23/11/2026

| Ngày | Nguồn | Người | Thay đổi CSDL | Kết luận | Ghi chú |
| --- | --- | --- | --- | --- | --- |
| 23/11 | [ADR-003](../adr/ADR-003-chuyen-csdl-postgresql-supabase.md); báo cáo kiểm kê Supabase của #2 | #2 Yến dựng schema; nhóm chốt; #1 ghi | Schema `smartmart` trên Supabase (PostgreSQL 17.12): 31 bảng, 62 FK, 89 CHECK, 17 trigger, 3 view, RLS 31/31 | **Nhóm chốt dùng làm CSDL đích**. Chưa duyệt theo 5 tiêu chí vì ứng dụng chưa chạy trên schema này | DDL hiện chỉ nằm trên Supabase, cần đưa vào repo qua PR. Duyệt lại khi có ánh xạ hoặc migration Npgsql và CI PostgreSQL |
