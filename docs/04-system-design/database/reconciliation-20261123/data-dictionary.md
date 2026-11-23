# Từ điển bổ sung theo migration thực tế

**Trạng thái: Đề xuất / chờ duyệt.** Chỉ ghi nhận DDL hiện có, chưa xác nhận nhóm đã phê duyệt thiết kế. Xem [báo cáo đối chiếu](README.md).

## `hoan_tien_hoa_don` — Pos12CancelInvoice

| Cột | Kiểu / bắt buộc | Ý nghĩa / ràng buộc |
|---|---|---|
| `id` | BIGINT UNSIGNED, NOT NULL, AUTO_INCREMENT | PK |
| `hoa_don_id` | INT UNSIGNED, NOT NULL | FK → `hoa_don.id`, UNIQUE: tối đa một bản ghi hoàn do hủy cho một hóa đơn |
| `so_tien_hoan` | BIGINT UNSIGNED, NOT NULL | Số tiền hoàn do hủy; DDL chưa có CHECK buộc > 0 |
| `phuong_thuc` | ENUM('TIEN_MAT'), NOT NULL, mặc định TIEN_MAT | Chưa có phương thức ngân hàng |
| `diem_da_thu_hoi` | INT UNSIGNED, NOT NULL, mặc định 0 | Điểm đã thu hồi thực tế |
| `diem_chua_thu_hoi` | INT UNSIGNED, NOT NULL, mặc định 0 | Điểm còn thiếu do số dư khách không đủ |
| `ly_do` | VARCHAR(255), NOT NULL | Dịch vụ kiểm tra lý do không rỗng; DDL không có CHECK độ dài tối thiểu |
| `nguoi_thuc_hien_id` | INT UNSIGNED, NOT NULL | FK → `tai_khoan.id`; quyền Admin kiểm tra ở dịch vụ |
| `thoi_diem` | DATETIME, NOT NULL, mặc định CURRENT_TIMESTAMP | Có index; quy ước phiên giao dịch UTC+7 |

Hai trigger `trg_hoan_tien_hd_khong_sua`, `trg_hoan_tien_hd_khong_xoa` chặn UPDATE/DELETE. Không suy ra từ đó rằng mọi bảng sổ quỹ khác cũng bất biến.

## `quy_tien_mat` — CashLedger

| Cột | Kiểu / bắt buộc | Ý nghĩa / ràng buộc |
|---|---|---|
| `id` | INT UNSIGNED, NOT NULL | PK; migration tạo dòng id=1; code dùng dòng này, DDL không buộc bảng chỉ có một dòng |
| `so_du` | BIGINT UNSIGNED, NOT NULL, mặc định 0 | CHECK `ck_quy_tien_mat_so_du` >= 0 |
| `ngay_cap_nhat` | DATETIME, NOT NULL | CURRENT_TIMESTAMP, ON UPDATE CURRENT_TIMESTAMP |

Không có FK từ `giao_dich_tien_mat` sang bảng này; quan hệ sổ quỹ là quy ước của dịch vụ, không vẽ thành FK trong ERD vật lý.

## `giao_dich_tien_mat` — CashLedger

| Cột | Kiểu / bắt buộc | Ý nghĩa / ràng buộc |
|---|---|---|
| `id` | BIGINT UNSIGNED, NOT NULL, AUTO_INCREMENT | PK |
| `loai` | ENUM('THU_BAN_HANG','CHI_HOAN_TRA'), NOT NULL | Hai loại hiện có; chưa có loại chi do hủy hóa đơn POS-12 |
| `hoa_don_id` | INT UNSIGNED, NULL | FK → `hoa_don.id`, index; không UNIQUE |
| `phieu_doi_tra_id` | INT UNSIGNED, NULL | FK → `phieu_doi_tra.id`, index; không UNIQUE |
| `so_tien` | BIGINT UNSIGNED, NOT NULL | CHECK > 0 |
| `so_du_truoc` | BIGINT UNSIGNED, NOT NULL | Chưa có CHECK phương trình cộng/trừ theo loại |
| `so_du_sau` | BIGINT UNSIGNED, NOT NULL | Chưa có CHECK phương trình cộng/trừ theo loại |
| `nguoi_thuc_hien_id` | INT UNSIGNED, NOT NULL | FK → `tai_khoan.id`, index |
| `thoi_diem` | DATETIME, NOT NULL, mặc định CURRENT_TIMESTAMP | Quy ước phiên UTC+7 |

CHECK `ck_gdtm_lien_ket`: thu bán hàng phải có hóa đơn, không có phiếu trả; chi hoàn trả phải có phiếu trả, không có hóa đơn. Chống lặp hiện do dịch vụ khóa quỹ và kiểm tra bản ghi, không phải UNIQUE của DDL. Migration không thêm trigger cấm sửa/xóa sổ quỹ.
