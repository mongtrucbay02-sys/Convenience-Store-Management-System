# Tái tích hợp schema của PR #168

Ngày kiểm kê: 14/11/2026 (Asia/Saigon). Đây là bản tích hợp chọn lọc để review độc lập, không phải merge nguyên PR nguồn.

## Nguồn và phạm vi

- `develop`: `7e501094807b8bf20413900ff4f13e5d334a156a` (đã có #204, #207).
- `main`: `aed862d6097501338e67889fddfe720bec1b83bb`.
- [PR #168](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/168): `2554bf1d58f8abaf0b8569f7355f5f30193e88c0`, vẫn mở; base là `main`, không phải `develop`.
- [PR #130](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/130): đã đóng, không merge, phạm vi được thay thế.
- Đã fetch `main`, `develop`, head #168/#197/#201/#204/#207; đọc discussion và inline review của #168. Không cherry-pick.
- [inventory.md](inventory.md) liệt kê đủ 144 file nguồn: KEEP nguyên trạng 0, ADAPT 6, SKIP 138. Đây là số đóng góp nguồn, không phải số file đích. Chỉ 4 đường dẫn nguồn tồn tại cùng tên trên `develop`; các entity/configuration còn lại có implementation tương đương ở vị trí khác.

| Thành phần | #168 | develop | Quyết định | Lý do |
|---|---|---|---|---|
| DbContext | ApplicationDbContext | CuaHangDbContext | ADAPT một phần / SKIP context | Chỉ thích nghi guard thay đổi nghiệp vụ cho DanhMuc; không thêm context hoặc interceptor tăng phiên bản thứ hai |
| Entity | 28 class trong Modules | 27 class bảng + 3 class view, shared-type join | SKIP | Cùng 28 bảng, khác kiểu CLR/navigation; không ghi đè entity dùng bởi AUTH/POS/KHO |
| Configuration | 28 configuration class | Fluent mapping trong CuaHangDbContext | SKIP / blocker | Đã có PK/FK/index/type/nullability; token và generated metadata còn thiếu, cần port riêng có compatibility test |
| Migration | 23 migration + 23 designer + snapshot | InitialCreate + SchemaV1 + snapshot | SKIP | Baseline hiện hành đã tạo đủ DDL; không có DDL mới cần REGENERATE |
| SchemaStructureTests | DB cố định, 23 migration, 2 trigger | Kiểm tra kho và DanhMuc riêng | ADAPT | Dùng CsdlMySqlTam, kiểm tra toàn bộ tên đối tượng và lineage hiện hành, không skip ngầm |
| Mapping tests | Chưa có kiểm tra context thống nhất | 2 metadata tests của #207 | KEEP tests cũ, bổ sung | Một context, 28 table/3 view, DDL baseline, không pending model change |
| Integration tests | Schema + SP-07/08/09 | AUTH/POS/KHO/customer/returns | ADAPT schema; SKIP SP | SP là tính năng cần port độc lập, không thuộc diff CSDL này |
| Documentation | Báo cáo Code First/Aiven cũ | ADR, hướng dẫn migration, báo cáo #207 | UPDATE tại đây | Giữ truy vết, không sao chép PASS lịch sử thành kết quả hiện tại |

## Kiến trúc và schema ownership

`CuaHangDbContext` là context runtime duy nhất. `Program.cs` đăng ký Oracle `MySql.EntityFrameworkCore` 10.0.12, `UseMySQL(ConnectionStrings:CuaHang)` và `GioVietNamInterceptor` (+07:00). Không đổi DI/provider/package. Các module còn dùng MySql.Data/MySqlConnector hoặc SQL trực tiếp; đó không phải DbContext thứ hai.

Chỉ có [ADR-001](../../adr/ADR-001-tech-stack.md) trong thư mục ADR, không tìm thấy ADR mới hơn thay thế. ADR xác định monolith, migration chung và môi trường MySQL; mục 3.8 không nêu tên CuaHangDbContext theo nghĩa đen. Lựa chọn context được thể hiện cụ thể qua code `develop`, PR #131 và review #168, phù hợp yêu cầu tích hợp.

| Đối tượng | SQL tham chiếu | EF model runtime | Migration hiện hành | DB test sau Migrate |
|---|---:|---:|---:|---:|
| Bảng nghiệp vụ | 28 | 28 (có join khuyen_mai_san_pham) | 28 | 28 |
| Bảng lịch sử EF | 0 | Không là entity nghiệp vụ | EF tự quản lý | 1 |
| View | 3 | 3 mapping keyless; mapping không tự tạo view | 3 câu SQL | 3 |
| Trigger | 10 | Không mô hình hóa trong EF | 10 câu SQL | 10 |
| CHECK | 53 | 0 CHECK được khai báo trong model | 53 trong SQL tạo bảng | 53, ENFORCED=YES |
| Lịch sử migration | Không áp dụng | 1 migration được phát hiện | InitialCreate | 1 applied, 0 pending |

Nguồn DDL là `docs/db/schema_v1.sql`, được đóng gói trong `Data/Migrations/SchemaV1.cs` và gọi từ `20261026120030_InitialCreate`. View, trigger, CHECK và seed do SQL bên trong migration sở hữu, không phải script triển khai song song. Không chạy SQL tham chiếu lần nữa sau migration. SQL kiểm tra ở `docs/db/` không phải migration mới. Hướng dẫn scaffold ban đầu và ERD bản nháp được giữ như tài liệu lịch sử; chúng không thay thế baseline đã commit.

Kết quả trên được xác minh với MySQL Docker **8.4.13**, image digest `sha256:a81d043930e51745522bd5c430961cf8286f0a23f0a9a2a6e0108f7ea71b0902`, loopback `127.0.0.1:33308`. Phiên bản 9.7.2 ghi trong ADR và môi trường Aiven/production: **BLOCKED — NOT VERIFIED** trong lần này. Không suy rộng kết quả 8.4.13 sang các server đó.

### Khởi tạo và migration

1. Dùng MySQL test server độc lập; không dùng production/shared Aiven. Tạo database trống riêng, utf8mb4/utf8mb4_0900_ai_ci; cấu hình `ConnectionStrings__CuaHang` qua secret/environment.
2. Trên môi trường dev/test mới, dùng cờ MySQL `--default-time-zone=+07:00 --disable-log-bin` theo ADR. Không tự đổi cấu hình server dùng chung. Nếu binary log bật, chủ CSDL phải xác minh quyền tạo trigger và chính sách `log_bin_trust_function_creators`; không tự cấp SUPER hoặc thay biến global.
3. Chạy `dotnet ef database update --project src/ConvenienceStore.Web --context CuaHangDbContext` trên database trống đã xác nhận. Không chạy cả `schema_v1.sql` và migration vào cùng database.
4. Với database có dữ liệu/lịch sử khác: dừng, kiểm kê và xin quyết định baseline từ owner CSDL; không reset, drop, đánh dấu lịch sử hoặc chạy Down.
5. Thay đổi DDL tương lai phải sinh migration từ CuaHangDbContext, đọc SQL diff và cập nhật SQL/ERD trong cùng PR; #1 review. Không sửa baseline đã có, không nhập 23 migration của #168.

Test fixture `CsdlMySqlTam` nhận `ConnectionStrings__CuaHangTest`, tạo `cuahang_test_<random>` rồi Migrate, và chỉ xóa database đó sau test. Tên database trong chuỗi đầu vào bị bỏ qua. Do fixture có quyền CREATE/DROP DATABASE, biến này phải trỏ server test độc lập. Không dùng `EnsureCreated` vì sẽ bỏ qua DDL SQL của baseline.

## Mapping và blocker đã xác minh

Kiểm tra trực tiếp toàn bộ 281 cột bảng: tên, store type (gồm unsigned/ENUM/precision/scale), nullability đều khớp model. PK, FK, các cột FK ghép, delete rule, unique/index khớp. NO ACTION/RESTRICT được chuẩn hóa tương đương trên MySQL; alternate key nội bộ EF được kiểm tra bằng unique index vật lý cùng cột, không đòi tên synthetic `AK_...` tồn tại.

41 câu CREATE trong SQL tham chiếu được so với script sinh từ migration (bỏ comment và khoảng trắng); giữ cả default, biểu thức generated, nội dung trigger/view/CHECK. Đây là so sánh văn bản baseline, không thay thế behavioral test cho từng CHECK/trigger/view.

[column-audit.json](../../tests/evidence/pr168/column-audit.json) ghi 48 khác biệt biểu diễn default (ví dụ EF `'1'` và information_schema `1`, hoặc CLR zero không có DefaultValueSql). Không có khác biệt type/nullability. Không kết luận mọi default EF đều tương đương chỉ từ phép đếm này.

**B1 — Generated-column metadata còn thiếu:** `ct_hoa_don.thanh_tien`, `ct_phieu_doi_tra.thanh_tien`, `ct_phieu_doi_tra.gia_tri_huy`, `ct_phieu_nhap.thanh_tien`, `gio_hang.dang_ban_cua_thu_ngan` là STORED generated trong DB nhưng model ghi `ValueGenerated.Never`. Probe thêm chi tiết phiếu nhập bằng CuaHangDbContext đã thất bại **MySQL 3105** ở `ct_phieu_nhap.thanh_tien`. Đây là lỗi có sẵn trong mapping develop, không sửa trong PR này vì đang được module sử dụng. Cần mapping generated riêng + tests insert/read/update cho từng module + kiểm tra snapshot/migration không ghi đè DDL. Không đổi contract ThanhTien/DateTime hoặc tự tái tạo cột để sửa nhanh.

**B2 — Concurrency chưa đủ toàn hệ thống:** nguồn có token cho 10 bảng. Develop có token cho DanhMuc và ChuongTrinhKhuyenMai; tự tăng ở EF chỉ DanhMuc. Tám bảng `tai_khoan`, `san_pham`, `nha_cung_cap`, `phieu_nhap`, `phieu_kiem_ke`, `phieu_xuat_huy`, `khach_hang`, `gio_hang` chưa là EF concurrency token. Các module đang có SQL cập nhật/khóa dòng riêng. Không áp dụng vòng lặp tăng generic của #168 trước khi xác minh từng write path và hợp đồng conflict. NhaCungCap là dependency cần giải quyết trước khi port phần mutation/discontinue hiện xuất hiện trong head #197.

## Bảo toàn #204/#207

Không đổi token của DanhMuc hoặc Promotions. Cả SaveChanges và SaveChangesAsync tiếp tục gọi một hàm tăng phiên bản duy nhất; phiên bản mới tính từ OriginalValues, nên conflict/retry không tăng lặp. Thích nghi guard của #168: chỉ tăng khi có thuộc tính nghiệp vụ IsModified; thay đổi riêng PhienBan được đưa về original, không ghi phiên bản tùy ý. No-op tracked (không đổi hoặc đổi rồi trả về giá trị cũ trước DetectChanges) không tăng. Không so CurrentValues với OriginalValues để loại write intent: detached Update có originals local bằng current nhưng vẫn phải tăng version. Caller đánh dấu cột nghiệp vụ Modified được xem là yêu cầu ghi, dù giá trị local bằng nhau; không thể kết luận đó là no-op vật lý mà không đọc database. Không đổi schema/dữ liệu hay thêm migration.

Các test MySQL giữ 6 case của #207 và thêm 9 case: explicit Modified sync/async, detached update và stale writer sync/async, tracked đổi rồi trả về giá trị cũ sync/async, thay đổi riêng version sync/async, stale sync và retry. Test hai context xác minh exception và dữ liệu của bên thắng còn nguyên; reload/retry giữ cả thay đổi trước đó. Context ném DbUpdateConcurrencyException, caller cần chuyển thành conflict theo API; middleware hiện không có handler generic cho mọi EF entity. Không tuyên bố đã kiểm thử mọi transaction cạnh tranh hoặc mọi raw SQL writer.

## Tác động #197 / #201 và dependency khác

| PR | HEAD kiểm kê | Dependency thực tế | Kế hoạch sau tích hợp |
|---|---|---|---|
| #197 NH-01 | 359756abbd0b7d30d0cbeb0cac9fb5f5d2fda15b | EfSupplierListRepository nhận ApplicationDbContext, NhaCungCaps; HEAD hiện còn chứa NH-02/NH-03 và test mutation/discontinue | Chỉ chọn commit NH-01; đổi context, dùng Data.Entities.NhaCungCap; projection read-only phù hợp cùng thuộc tính; giữ LanNhapGanNhat null theo contract hiện tại. Không port mutation/discontinue trước B2; bỏ đăng ký ApplicationDbContext trong DI; chạy query/authorization tests riêng |
| #201 NH-07 | b97558b0343b151b63f4eb782c24acee186a8a5e | EfReceiptListRepository nhận ApplicationDbContext, PhieuNhaps/NhaCungCap/NguoiLap; NgayNhap nguồn DateOnly | Đổi context; giữ runtime DateTime ánh xạ date, chuyển biên lọc DateOnly sang DateTime ở query, chuyển kết quả về DateOnly sau materialization hoặc projection đã được provider test. Xác minh inclusive dates, sort, joins, trạng thái, DTO và AUTH AD/NK trước endpoint |

Hai PR đều đang base `feature/db-codefirst-remaining-schema` tại d03a30c; không sửa base/head/code hoặc merge trong nhiệm vụ này. Supplier list và receipt list có thể port sang CuaHangDbContext nhưng chưa được chạy compatibility tests ở đây. Không cần thêm DbSet hay bảng mới cho hai truy vấn. ChiTietPhieuNhaps bên nguồn tương ứng CtPhieuNhaps ở develop; LichSuGias tương ứng LichSuGia/LichSuGium; KhuyenMaiSanPham đã là shared join, không tạo entity bảng trùng.

Các dependency khác: review #168 nêu #169 (SP-07), #196 (SP-08). HEAD #168 chứa thêm SP-08/09 và tests, nên phạm vi nguồn không chỉ là database. Tìm PR mở theo base branch xác nhận #197/#201; đó không phải bằng chứng mọi feature stack khác đã được chuyển hết. SQL POS/KHO và AUTH trên develop phụ thuộc baseline #131, không phụ thuộc runtime ApplicationDbContext. PR này không thay đổi DTO/repository API hiện hành.

## Kiểm thử và rollback

Xem [báo cáo thực thi](../../tests/pr168-consolidation-verification.md). Bộ schema không chứa hard-coded database cũ, không đòi 23 migration và không trả PASS bằng cách skip khi thiếu MySQL.

Lệnh kiểm tra focused (sau restore/build):

```powershell
dotnet test ConvenienceStoreManagement.slnx --no-build --filter 'FullyQualifiedName~SchemaStructureTests'
dotnet test ConvenienceStoreManagement.slnx --no-build --filter 'FullyQualifiedName~SchemaMappingTests'
dotnet test ConvenienceStoreManagement.slnx --no-build --filter 'FullyQualifiedName~DanhMucConcurrency'
dotnet run --project scripts/pr168-schema-audit/SchemaAudit.csproj
```

Audit console không nằm trong solution runtime; tái sử dụng fixture test, tạo database tạm riêng, ghi JSON vào `docs/tests/evidence/pr168/column-audit.json`. Probe generated-column ghi lỗi dự kiến vào báo cáo, **không phải test PASS về khả năng insert**. Không chứa credential hoặc tự đọc cấu hình production. Chạy từ root repository, với ConnectionStrings__CuaHangTest trên server test riêng.

Rollback PR: revert commit code/test/tài liệu qua PR thông thường. Không có migration mới nên không chạy database downgrade/Down, không drop hoặc restore production. Revert guard sẽ khôi phục hành vi no-op cũ và cần ghi nhận tác động đó.

Chưa đủ điều kiện nghiệm thu toàn bộ #168 khi B1/B2 còn mở. Chỉ đề xuất đóng #168 sau khi PR tích hợp đã merge, mọi phần cần giữ (kể cả feature bị để lại) đã đối chiếu đầy đủ và reviewer xác nhận. Không tự đóng #168/#130/#197/#201, không tự approve/merge.
