# Đối chiếu migration, ERD và SRS — 23/11/2026

> **Trạng thái:** Đề xuất / chờ nhóm duyệt. Không phải biên bản phê duyệt CSDL.
>
> **Phụ trách:** #5 Nguyễn Tiến Trung · **Người duyệt:** Chưa có xác nhận độc lập của người phụ trách CSDL/POS/Đổi trả.
>
> **Phạm vi mã nền:** `35836a3`, bao gồm các migration đã có từ trước; lượt này không sửa migration lịch sử hoặc triển khai CSDL chung.
>
> **Có dùng AI:** OpenAI Codex, AI-TRUNG-004 trong [nhật ký](../../../09-ai-usage/ai-usage-log.md).

## 1. Nguồn đối chiếu

| Nguồn | Vai trò / giới hạn |
|---|---|
| [`schema_v1.sql`](../../../db/schema_v1.sql), `SchemaV1.cs`, `InitialCreate` | Baseline vật lý: 28 bảng nghiệp vụ, 3 view, 10 trigger, 53 CHECK |
| [`Pos12CancelInvoice`](../../../../src/ConvenienceStore.Web/Data/Migrations/20261112160000_Pos12CancelInvoice.cs) | Thêm audit hoàn tiền do hủy hóa đơn, 2 trigger |
| [`CashLedger`](../../../../src/ConvenienceStore.Web/Data/Migrations/20261112170000_CashLedger.cs) | Thêm số dư quỹ và sổ giao dịch tiền mặt |
| [`CuaHangDbContext`](../../../../src/ConvenienceStore.Web/Data/CuaHangDbContext.cs), model snapshot | ORM ánh xạ baseline; ba bảng mới hiện do SQL migration/dịch vụ quản lý, chưa có entity tương ứng |
| [ERD Hóa đơn/Đổi trả](../invoice-returns/), [ERD Sản phẩm/Nhập hàng](../product-purchase/) | Dùng tên mô hình tiếng Anh và một số kiểu khác baseline; chưa có ánh xạ đầy đủ sang tên/cột vật lý Việt Nam |
| [SRS v1.2 change list](../../../srs/SRS_v1.2_danh-sach-thay-doi.md), [SRS ứng viên mới](../../../03-requirements/srs.md) | Đều chưa phải bằng chứng SRS đã được nhóm duyệt; Word SRS gốc được nhắc đến là ngoài repo |
| [Nhật ký duyệt CSDL](../../../quan-ly/nhat-ky-duyet-thay-doi-csdl.md) | Có quy trình duyệt; không tự ghi thêm approval cho các migration này |

## 2. Chênh lệch cụ thể

Metadata MySQL 8.4.13 từ database tạm sau khi áp đủ ba migration: **31 bảng nghiệp vụ, 302 cột bảng nghiệp vụ, 3 view, 12 trigger, 56 CHECK, 61 FK**. Không tính `__EFMigrationsHistory` vào bảng nghiệp vụ. EF relational model có 28 bảng; ba bảng chưa ánh xạ đúng là ba bảng mở rộng bên dưới. Xem [runtime-summary.json](runtime-summary.json).

| Mục | Đã có trong runtime | Thiếu/khác ở tài liệu hoặc ORM | Việc cần nhóm duyệt |
|---|---|---|---|
| Pos12CancelInvoice | `hoan_tien_hoa_don`, 2 FK, UNIQUE theo hóa đơn, trigger cấm sửa/xóa | Không có trong baseline v1; ERD hiện dùng khái niệm `CASH_TRANSACTIONS`, không thể coi là cùng một bảng vật lý | Đưa audit POS-12 và quan hệ 0..1 vào ERD/từ điển/SRS |
| CashLedger | `quy_tien_mat`, `giao_dich_tien_mat`, thêm 3 FK và 3 CHECK | Không có trong baseline v1 và model snapshot | Chốt bảng quỹ/sổ, quy tắc số dư, quyền ghi và cách đối soát |
| ORM | Một `CuaHangDbContext` vận hành | Ba bảng bổ sung không nằm trong EF relational model | Chọn tiếp tục sở hữu DDL bằng SQL migration hay ánh xạ bổ sung có kiểm soát; không tạo DbContext thứ hai |
| Phạm vi test schema cũ | Test baseline xác nhận 28 bảng/10 trigger | Test đó chủ động loại ba bảng mới và trigger bổ sung, nên không phải kiểm chứng toàn bộ 31 bảng | Bổ sung test riêng cho schema mở rộng, Up/Down và trigger mới |
| ERD tiếng Anh | `INVOICES`, `RETURNS`, `CASH_TRANSACTIONS`… là mô hình cũ | Tên, kiểu ID và cấu trúc không khớp nguyên văn `hoa_don`, `phieu_doi_tra` và các bảng quỹ thực tế | Xem đây là ERD khái niệm; lập bản vật lý có tên đúng, không scaffold thêm bảng tiếng Anh |
| FR-07 của SRS ứng viên | `ct_phieu_nhap.so_luong` hiện có một số lượng | Chưa có cột số chứng từ/số thực nhận/chênh lệch theo từng dòng như FR-07 đề xuất | Duyệt lại scope trước khi yêu cầu migration mới; không tự coi đề xuất là chức năng đã hoàn thành |
| Ảnh KM | DTO/form có `ImageUrl` | Schema/adapter chưa lưu ảnh | Tách việc hoàn thiện ảnh KM; chưa thêm cột/migration trong bản đối chiếu này |

## 3. Các quyết định còn mở

| Mã | Quan sát từ mã/DDL | Đề xuất để duyệt | Trạng thái |
|---|---|---|---|
| DB-R01 | POS-12 ghi `hoan_tien_hoa_don`; `PosInvoiceCancellationService` hiện không gọi `CashLedgerService` hoặc cập nhật sổ quỹ. Enum sổ chỉ có thu bán/chi phiếu trả | Nếu quỹ là nguồn đối soát tiền mặt, bổ sung luồng chi do hủy hóa đơn trong cùng transaction; có thể cần loại giao dịch/FK/CHECK mới bằng migration tiếp theo. Cần test bán → hủy → đối soát quỹ | **Cần xử lý trước nghiệm thu sổ quỹ**; kết luận từ rà mã, chưa tái hiện riêng luồng hủy trên trình duyệt trong lượt này |
| DB-R02 | CashLedger khởi tạo quỹ bằng 0, không backfill tiền mặt của hóa đơn cũ | Chốt ngày bắt đầu sổ/số dư mở quỹ trên DB đã có dữ liệu; không tự suy ra quỹ thực tế bằng tổng doanh thu | Chờ người phụ trách nghiệp vụ và CSDL |
| DB-R03 | Chống ghi lặp ở dịch vụ bằng khóa quỹ/kiểm tra, không có UNIQUE cho mỗi sự kiện thu/hoàn | Chốt ràng buộc idempotency và quan hệ với chi do hủy trước khi thiết kế migration mới | Chờ duyệt |
| DB-R04 | Hai bảng quỹ không có trigger cấm UPDATE/DELETE; chỉ bảng audit POS-12 có | Chốt retention/bất biến sổ tiền mặt theo SRS; không ghi trong tài liệu rằng toàn bộ sổ đã bất biến | Chờ duyệt |
| DB-R05 | `quy_tien_mat` seed id=1; code dùng id=1, DDL không ràng buộc singleton và không có FK sổ → quỹ | Chốt phạm vi một quỹ/một cửa hàng; ERD không vẽ FK chưa tồn tại | Chờ duyệt |
| DB-R06 | Baseline v1 và snapshot vẫn mang phạm vi ban đầu | Đề xuất giữ v1 làm baseline lịch sử, công bố bản tham chiếu runtime = v1 + hai delta, hoặc phát hành schema v2 sau duyệt. Không sửa InitialCreate/SchemaV1 đã phát hành | Chờ người phụ trách CSDL chốt quy ước |
| DB-R07 | ADR/SRS cũ ghi MySQL 9.7.2; bằng chứng hiện tại là 8.4.13 | Chốt ma trận CI/Aiven ở nhánh 8.4; chưa xác nhận test 9.7.2 | Chờ nhóm |

Các vấn đề DB-R01/02 chưa được sửa nghiệp vụ trong PR tài liệu/CI này. CI xanh cũng không tự chứng minh những quy tắc chưa có test đã đúng.

## 4. Bộ cập nhật đã chuẩn bị

- [ERD bổ sung vật lý](erd-additions.mmd): ba bảng mới và năm FK thực; `quy_tien_mat` đứng riêng vì chưa có FK tới sổ.
- [Từ điển dữ liệu](data-dictionary.md): 21 cột, index/UNIQUE, CHECK, trigger và giới hạn.
- [DDL tham chiếu bổ sung](schema-additions.sql): sao đúng sáu câu lệnh `Up()` của hai migration, dùng để review; không phải script triển khai lên DB đã migrate.
- [Danh sách SRS cần cập nhật](../../../03-requirements/migration-srs-review-2026-11-23.md): nội dung đề xuất và người/quyết định cần chốt.
- `runtime-summary.json`: số liệu metadata lấy từ database tạm dựng bằng migration; được tạo trong lượt kiểm chứng cục bộ, không phải bằng chứng approval.

Không sửa `docs/db/schema_v1.sql` vì các test baseline và migration lịch sử vẫn dùng nó. Bản tham chiếu mở rộng đã được chuẩn bị riêng để người duyệt quyết định cách phát hành, tránh biến chỉnh tài liệu thành thay đổi baseline âm thầm.

## 5. Kiểm chứng kỹ thuật

Thêm [MigrationExtensionMySqlTests](../../../../tests/ConvenienceStore.Web.Tests/Integration/Database/Schema/MigrationExtensionMySqlTests.cs): kiểm tra ba bảng/FK/CHECK/trigger mới, rollback hai migration về InitialCreate rồi áp lại trên database test riêng, và thử UPDATE/DELETE audit POS-12 bị từ chối.

Kết quả lượt chạy và hash bằng chứng được ghi trong [báo cáo CI/schema](../../../06-testing/test-reports/2026-11-23-ci-schema.md). Kiểm tra Down chỉ thực hiện trên dữ liệu tạm; Down xóa bảng hoàn tiền/sổ quỹ, không phải kế hoạch rollback an toàn cho dữ liệu nghiệp vụ thật.

## 6. Checklist gửi nhóm duyệt

- [ ] Chốt SRS/ERD phiên bản nào là nguồn được duyệt; cung cấp Word/PDF gốc nếu đối chiếu đầy đủ.
- [ ] Người phụ trách CSDL duyệt ba bảng, năm FK, ba CHECK và hai trigger bổ sung.
- [ ] POS/Đổi trả/BC chốt DB-R01/02 và cách đối soát khi hủy hóa đơn.
- [ ] Chốt ownership SQL migration/EF và quy ước schema v2 hoặc delta tham chiếu.
- [ ] Sau quyết định, mở migration mới nếu cần, cập nhật SRS/ERD và chạy lại regression.
- [ ] Review độc lập, CI GitHub thật và staging trước nghiệm thu; không đóng các mục này chỉ vì đã có tài liệu đề xuất.
