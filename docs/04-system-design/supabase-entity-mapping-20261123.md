Implementation update 2026-11-24: one PostgreSQL-only CuaHangDbContext now replaces the transitional provider switch. MySQL migrations/adapters are archived or renamed outside the release lineage; the migration guard has been replaced by an offline-generated PostgreSQL baseline/snapshot. The mapping/catalog evidence below remains a dated reference. Existing Supabase adoption and runtime-role verification still require M0 review; see [current migration contract](../db/POSTGRESQL-MIGRATIONS.md).

---

# Ánh xạ EF Core cho Supabase — 23/11/2026

## Phạm vi

Nhánh `feature/supabase-entities-20261123`, một `CuaHangDbContext`, schema `smartmart`.
Đối chiếu trực tiếp catalog PostgreSQL 17 trên Supabase ở chế độ chỉ đọc, không suy luận chỉ từ tài liệu MD/SQL.
Có 31 bảng, 307 cột, 31 khóa chính, 62 khóa ngoại, 89 CHECK, 17 trigger và 3 view.

`Data/CuaHangDbContext.PostgreSql.cs` ánh xạ bảng/cột, khóa, chỉ mục, identity, default và cột sinh.
`Data/CuaHangDbContext.PostgreSql.Constraints.cs` lưu metadata CHECK/trigger theo catalog.
`Data/Entities` có 30 entity bảng và 3 entity view. Bảng nối `khuyen_mai_san_pham` dùng shared entity Dictionary theo mẫu EF sẵn có: tổng cộng vẫn đủ 31 bảng, không cần thêm class trùng.
Ba entity bổ sung là `QuyTienMat`, `GiaoDichTienMat`, `HoanTienHoaDon`.
Có thêm `PhienDangNhap.HetHanLuc` và quan hệ `GiaoDichTienMat.QuyTienMatId` theo Supabase thật.

## Kiểu dữ liệu và tương thích

- bigint → long; smallint → short; date → DateOnly; timestamptz → DateTime UTC.
- Consumer Customers, Products, Purchasing, Inventory đổi kiểu khóa/giá trị phù hợp. DTO ngày sinh chuyển DateTime/DateOnly tại biên giao diện.
- Năm cột sinh do CSDL tính, không gửi trong INSERT/UPDATE.
- Giữ concurrency token phiên bản danh mục và logic tăng phiên bản sẵn có.
- Không ánh xạ hoặc quản lý các schema hệ thống Supabase auth/storage/realtime, không thay bằng Supabase Auth.
- Mô hình MySQL cũ giữ riêng trong cùng context; bộ chuyển kiểu giữ kiểu lưu unsigned của schema MySQL. Snapshot MySQL cũ là mốc đối chiếu kiểu lưu, không dùng làm snapshot PostgreSQL.
- Giữ giới hạn giá bán API hiện có (uint.MaxValue); việc mở rộng chính sách giá cần quyết định riêng. Số dư điểm dùng bigint có kiểm tra tràn số/không âm.

## Cấu hình cục bộ

Sao chép `src/ConvenienceStore.Web/.env.example` thành `.env` cùng thư mục, thay project ref/password bằng giá trị riêng. Không commit `.env`.

```dotenv
Database__Provider=PostgreSql
ConnectionStrings__CuaHang="Host=HOST_POOLER;Port=5432;Database=postgres;Username=postgres.PROJECT_REF;Password=DATABASE_PASSWORD;SSL Mode=Require;Search Path=smartmart;"
```

Thứ tự ưu tiên: biến môi trường → .env → IConfiguration. Nếu không chỉ định provider, vẫn dùng MySql để không tự đổi đích CSDL của nhóm.
Không áp interceptor SET time_zone của MySQL cho Npgsql. Gửi DateTime UTC khi ghi timestamptz; chuyển UTC+7 ở biên nghiệp vụ/giao diện khi chuyển các service.

## Migration và giới hạn hiện tại

Các migration đang có dành cho MySQL. `PostgreSqlMigrationGuard` chặn Migrate/MigrateAsync/GenerateScript/HasPendingModelChanges khi dùng Npgsql, trước khi kết nối server. Cần baseline PostgreSQL được nhóm CSDL duyệt riêng.
Metadata trigger/CHECK không phải script triển khai đầy đủ: hàm trigger, view, RLS và quyền truy cập vẫn do bộ DDL PostgreSQL đã duyệt quản lý. Chưa dùng EnsureCreated hay áp migration lên Supabase.

Đợt tiếp theo đã chuyển SQL nghiệp vụ AUTH/POS/KH/NH/KHO/DT/KM/BC sang nhánh PostgreSQL, giữ MySQL tương thích. INSERT RETURNING, upsert, khóa dòng, lỗi SQLSTATE, default 0/false, hạn phiên và UTC+7 đã được kiểm thử trên PostgreSQL riêng; xem [báo cáo runtime](../06-testing/test-reports/2026-11-23-postgresql-runtime.md). Các tên adapter MySql* còn giữ để tương thích API nội bộ, không ép dùng MySQL driver.

## Bằng chứng kiểm tra

- Catalog thật được chụp chỉ đọc; fixture kiểm tra không chứa mật khẩu hay dữ liệu giao dịch.
- Đã SELECT AsNoTracking/Take(1) thành công qua EF trên toàn bộ 31 bảng + 3 view. Log chỉ ghi số dòng, không ghi giá trị người dùng. Không thử INSERT/UPDATE trên Supabase dùng chung.
- Test `PostgreSql/SupabaseModelMappingTests` đối chiếu model với catalog, kiểu dữ liệu, cột sinh, và chặn migration MySQL; chạy không cần CSDL.
- Kết quả hồi quy/build cuối cùng ghi trong [báo cáo kiểm thử](../06-testing/test-reports/2026-11-23-supabase-entities.md) sau khi chạy xong.

Đã xác nhận luồng ghi và HTTP trên PostgreSQL localhost riêng. Chưa nghiệm thu staging/cloud hoặc quyền RLS của role backend ít quyền trên Supabase; không ghi dữ liệu Supabase/Aiven chung.

## Danh sách đối chiếu entity–bảng/view

| Đối tượng smartmart | Loại | Entity C# |
|---|---|---|
| `bo_dem_chung_tu` | Table | `BoDemChungTu` |
| `chinh_sach_doi_tra` | Table | `ChinhSachDoiTra` |
| `chuong_trinh_khuyen_mai` | Table | `ChuongTrinhKhuyenMai` |
| `ct_gio_hang` | Table | `CtGioHang` |
| `ct_hoa_don` | Table | `CtHoaDon` |
| `ct_kiem_ke` | Table | `CtKiemKe` |
| `ct_phieu_doi_tra` | Table | `CtPhieuDoiTra` |
| `ct_phieu_nhap` | Table | `CtPhieuNhap` |
| `ct_xuat_huy` | Table | `CtXuatHuy` |
| `danh_muc` | Table | `DanhMuc` |
| `giao_dich_tien_mat` | Table | `GiaoDichTienMat` |
| `gio_hang` | Table | `GioHang` |
| `hoa_don` | Table | `HoaDon` |
| `hoan_tien_hoa_don` | Table | `HoanTienHoaDon` |
| `khach_hang` | Table | `KhachHang` |
| `khuyen_mai_san_pham` | Table | `Shared entity KhuyenMaiSanPham (Dictionary)` |
| `lich_su_diem` | Table | `LichSuDiem` |
| `lich_su_gia` | Table | `LichSuGium` |
| `nha_cung_cap` | Table | `NhaCungCap` |
| `phien_dang_nhap` | Table | `PhienDangNhap` |
| `phieu_doi_tra` | Table | `PhieuDoiTra` |
| `phieu_kiem_ke` | Table | `PhieuKiemKe` |
| `phieu_nhap` | Table | `PhieuNhap` |
| `phieu_xuat_huy` | Table | `PhieuXuatHuy` |
| `quy_tac_tich_diem` | Table | `QuyTacTichDiem` |
| `quy_tien_mat` | Table | `QuyTienMat` |
| `san_pham` | Table | `SanPham` |
| `tai_khoan` | Table | `TaiKhoan` |
| `the_kho` | Table | `TheKho` |
| `ton_kho` | Table | `TonKho` |
| `v_chuong_trinh_khuyen_mai` | View | `VChuongTrinhKhuyenMai` |
| `v_gia_ban_hien_hanh` | View | `VGiaBanHienHanh` |
| `v_luot_dung_khuyen_mai` | View | `VLuotDungKhuyenMai` |
| `vai_tro` | Table | `VaiTro` |
