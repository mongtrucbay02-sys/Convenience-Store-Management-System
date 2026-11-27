# T8-02 – Duyệt thay đổi CSDL tuần 8 (issue #234)

| | |
| --- | --- |
| Người duyệt | #1 Phạm Gia Thăng (người duyệt CSDL theo Kế hoạch v3 mục 1.4) |
| Ngày rà | 23/11/2026 |
| Phạm vi | PR #232 (`fix/integrate-main-develop-20261122` → `develop`, #5 Trung) và PR #270 (sửa lỗi review, nhắm vào nhánh của #232) |
| Tiêu chí | 5 tiêu chí trong `docs/quan-ly/nhat-ky-duyet-thay-doi-csdl.md` |
| Trạng thái | Kết luận đề xuất "duyệt có điều kiện"; điều kiện X3, X4 đang chờ nhóm xác nhận ở issue #257 (hạn 25/11). X5 thay bằng ADR-003 (chuyển sang PostgreSQL/Supabase, chốt 23/11) – xem mục 6 |

## 1. Migration trong phạm vi

| Migration | Nguồn | Thay đổi |
| --- | --- | --- |
| `20261112160000_Pos12CancelInvoice` | PR #217 (#3 Hoài, gộp 15/11) | Bảng `hoan_tien_hoa_don`, 2 trigger cấm sửa/xóa |
| `20261112170000_CashLedger` | Commit `ea4ecee` (#3 Hoài, đẩy thẳng vào `develop` 17/11) | Bảng `quy_tien_mat`, `giao_dich_tien_mat` |
| `20261123090000_CashLedgerReconciliation` | PR #232 (`ce06dff`), sửa thêm ở PR #270 | Cột `da_chot_so_du_dau`; loại giao dịch `CHI_HUY_HOA_DON`, `CHOT_SO_DU_DAU`; cột `ghi_chu`; UNIQUE theo sự kiện; 2 trigger cấm sửa/xóa sổ quỹ |
| `20261123091000_PromotionImage` | PR #232 (`ce06dff`) | Cột `chuong_trinh_khuyen_mai.anh_url` VARCHAR(2048) NULL |

## 2. Đối chiếu 5 tiêu chí

| Tiêu chí | Pos12CancelInvoice | CashLedger | CashLedgerReconciliation | PromotionImage |
| --- | --- | --- | --- | --- |
| 1. Sinh cho `CuaHangDbContext`, đúng thư mục, có `Up()`/`Down()` | Đạt | Đạt | Đạt | Đạt; model snapshot đã cập nhật |
| 2. Không sửa migration đã gộp, mọi thay đổi là migration mới | Đạt | Đạt | Đạt (không sửa `CashLedger`) | Đạt |
| 3. Khớp ERD v1 hoặc có issue "thay đổi yêu cầu" được đồng ý; cập nhật tài liệu schema | **Chưa đạt**: bảng ngoài ERD v1; không có issue "thay đổi yêu cầu" | **Chưa đạt**: như bên cạnh | **Chưa đạt**: như bên cạnh | **Chưa đạt**: như bên cạnh |
| 4. Không phá trigger và CHECK hiện có | Đạt (test cấu trúc 28 bảng/10 trigger vẫn đạt) | Đạt | Đạt; thêm test riêng cho bảng mở rộng, rollback, chạy lại | Đạt |
| 5. `dotnet test` đạt trên MySQL 9.7.2 | **Chưa đạt**: CI chạy MySQL 8.4 (707/707 đạt) | như bên cạnh | như bên cạnh | như bên cạnh |

Về tiêu chí 3: #232 đã có bản đối chiếu `docs/04-system-design/database/reconciliation-20261123/` (từ điển dữ liệu, `schema-additions.sql`, ERD bổ sung) và đề xuất SRS `docs/03-requirements/cash-promotion-change-proposal-20261123.md` (CP-01 → CP-04). Đây là sản phẩm đủ để duyệt, nhưng còn thiếu bước "nhóm đồng ý" theo `CONTRIBUTING.md` (PR #230).

## 3. Lỗi phát hiện khi review và tình trạng

| Mã | Nội dung | Tình trạng |
| --- | --- | --- |
| R1 | `CashLedgerReconciliation` thêm cột trước khi kiểm tra dữ liệu trùng; khi có giao dịch trùng thì migration dừng giữa chừng và không chạy lại được | Đã sửa ở PR #270: kiểm tra trùng chạy trước mọi ALTER; có test hạ CSDL, chèn trùng, chạy lại |
| R2 | Trang Quỹ tiền mặt hiển thị chênh lệch chốt số dư không có dấu | Đã sửa ở PR #270 (CSDL vẫn lưu độ lớn vì cột UNSIGNED) |
| R3 | POS-12 trả `QUY_KHONG_DU` khi quỹ chưa chốt số dư mở (quỹ chỉ tính tiền thu từ 17/11) | **Chờ nhóm quyết định** quy tắc nghiệp vụ; nếu giữ, ghi vào SRS và HDSD, và chốt số dư mở trước buổi demo |
| R4 | CI dùng MySQL 8.4, ADR-001 ghi 9.7.2 | **Đã xử lý 23/11:** nhóm chốt chuyển sang PostgreSQL 17/Supabase (ADR-003); MySQL 8.4 chỉ còn là bản lùi |

Các quyết định DB-R01 → DB-R05 trong tài liệu đối chiếu của #5 (chi quỹ khi hủy hóa đơn, chống ghi lặp, sổ bất biến) đã được `ce06dff` cài đặt và có test MySQL. DB-R02 (số dư mở) chuyển thành thao tác Admin chốt một lần. DB-R06 (baseline v1 + delta hay schema v2) vẫn mở.

## 4. Kết luận đề xuất

**Duyệt có điều kiện** cho cả 4 migration, với điều kiện:

1. PR #270 được gộp vào nhánh của #232 và CI đạt.
2. Nhóm xác nhận đề xuất CP-01 → CP-04 (X3) và quy tắc R3 (X4) ở issue #257; khi đó tiêu chí 3 coi là đạt qua tài liệu đối chiếu, không sửa `schema_v1.sql` lịch sử (DB-R06: giữ v1 làm baseline, công bố v1 + delta).
3. ~~Nhóm chốt phiên bản MySQL (X5, đề xuất 8.4) và sửa ADR-001 cho khớp.~~ Thay bằng ADR-003 (23/11): CSDL đích là PostgreSQL 17/Supabase; ADR-001 đã ghi chú.
4. Không áp các migration này lên CSDL MySQL dùng chung (Aiven) trước khi đủ điều kiện 1, 2; khi áp thì sao lưu trước và Admin chốt số dư mở ngay sau khi nâng cấp.

## 5. Ghi chú

- Commit `ea4ecee` đẩy thẳng vào `develop` không qua PR; nhánh `develop` chưa được bảo vệ. Đã báo trong danh sách lỗi gửi #3 và #5.
- Bảng task v3 (Excel, ngoài repo) không cập nhật được qua PR; trạng thái task theo dõi bằng issue. Danh sách use case cắt đề xuất ở issue #257 (X7 → X9, hạn phản hồi 25/11).

## 6. Cập nhật sau quyết định chuyển CSDL (23/11/2026)

- Nhóm chốt PostgreSQL 17 trên Supabase ([ADR-003](../adr/ADR-003-chuyen-csdl-postgresql-supabase.md)). Biên bản này duyệt 4 migration **MySQL**; kết luận trên áp dụng cho bản MySQL hiện chạy và cho tag bản lùi.
- Nội dung của 4 migration đã có trong schema `smartmart` do #2 Yến dựng trên Supabase:
  - Bảng `hoan_tien_hoa_don`, `quy_tien_mat`, `giao_dich_tien_mat`.
  - Cột `da_chot_so_du_dau`.
  - Cột `anh_url` VARCHAR(2048).
- Khi có ánh xạ hoặc migration Npgsql, #1 duyệt lại theo 5 tiêu chí; tiêu chí 5 đổi thành "`dotnet test` đạt trên PostgreSQL 17" (xem `nhat-ky-duyet-thay-doi-csdl.md`).

## 7. Cập nhật 27/11/2026

- PR #286 (24/11) thay toàn bộ migration MySQL bằng một migration `20261124050401_PostgreSqlBaseline`. Bốn migration trong biên bản này **không còn chạy** trên bản hiện tại; kết luận "duyệt có điều kiện" chỉ còn giá trị lịch sử.
- X3, X4 đã chốt ở #257 (không có phản đối trước 25/11). X10 không còn áp dụng.
- `PostgreSqlBaseline` chưa có biên bản duyệt theo 5 tiêu chí. Việc cần làm: đối chiếu với ERD v1 + phần bổ sung (tiêu chí 3), trigger/CHECK (tiêu chí 4), test trên PostgreSQL 17 (tiêu chí 5: đạt, CI `cc20336` 877/877).

