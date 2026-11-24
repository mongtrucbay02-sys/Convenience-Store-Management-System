# ERD Khách hàng và Khuyến mãi — phần thiết kế của #251

Trạng thái: **Đang review**, 23/11/2026. Căn cứ bảng hiện có của `CuaHangDbContext` / `SchemaV1` và migration `PromotionImage` ở nhánh nền. Tuần 8 không tạo bảng hoặc DbContext mới.

```mermaid
erDiagram
    tai_khoan ||--o{ khach_hang : nguoi_tao_id
    tai_khoan |o--o{ quy_tac_tich_diem : nguoi_tao_id
    tai_khoan ||--o{ lich_su_diem : nguoi_thuc_hien_id
    tai_khoan ||--o{ chuong_trinh_khuyen_mai : nguoi_tao_id
    khach_hang |o--o{ gio_hang : khach_hang_id
    khach_hang |o--o{ hoa_don : khach_hang_id
    khach_hang |o--o{ phieu_doi_tra : khach_hang_id
    khach_hang ||--o{ lich_su_diem : khach_hang_id
    hoa_don |o--o{ lich_su_diem : hoa_don_id
    phieu_doi_tra |o--o{ lich_su_diem : phieu_doi_tra_id
    chuong_trinh_khuyen_mai ||--o{ khuyen_mai_san_pham : chuong_trinh_id
    san_pham ||--o{ khuyen_mai_san_pham : san_pham_id
    chuong_trinh_khuyen_mai |o--o{ ct_hoa_don : khuyen_mai_id
    chuong_trinh_khuyen_mai |o--o{ ct_gio_hang : khuyen_mai_id
    khach_hang {
        uint id PK
        varchar ho_ten "nullable khi ẩn"
        varchar so_dien_thoai UK "10 số, nullable khi ẩn"
        uint diem_hien_co
        enum trang_thai
        uint nguoi_tao_id FK
    }
    quy_tac_tich_diem {
        uint id PK
        uint so_tien_mot_diem
        boolean dang_bat
        datetime hieu_luc_tu
        uint nguoi_tao_id FK
    }
    lich_su_diem {
        ulong id PK
        uint khach_hang_id FK
        enum loai
        int so_diem
        uint diem_sau
        uint hoa_don_id FK
        uint phieu_doi_tra_id FK
        uint nguoi_thuc_hien_id FK
        datetime thoi_diem
    }
    chuong_trinh_khuyen_mai {
        uint id PK
        varchar ma UK
        enum loai_giam
        uint gia_tri
        uint giam_toi_da "nullable"
        datetime bat_dau_luc
        datetime ket_thuc_luc
        boolean chi_thanh_vien
        enum trang_thai
        varchar anh_url "nullable, migration nhánh nền"
        uint nguoi_tao_id FK
    }
    khuyen_mai_san_pham {
        uint chuong_trinh_id PK,FK
        uint san_pham_id PK,FK
    }
```

| Nhóm | Quy tắc liên quan |
|---|---|
| Khách hàng | Khách hoạt động có họ tên và SĐT duy nhất đúng 10 số bắt đầu 0. Ẩn khách xóa thông tin cá nhân, giữ mã tham chiếu giao dịch. Báo cáo Admin dùng nhãn `Khách đã ẩn #id` khi cần. |
| Điểm | Lịch sử chỉ thêm, có loại tích/thu hồi trả/thu hồi hủy/điều chỉnh. Thanh toán và cập nhật điểm cùng transaction. `hoa_don.so_tien_mot_diem` lưu giá trị quy tắc tại lúc bán, **không có FK giả định** từ hóa đơn tới id quy tắc. |
| KM | Sản phẩm–chương trình là N–N qua khóa chính ghép. Dòng hóa đơn lưu chương trình và giảm giá tại lúc bán; không tính báo cáo bằng giá/chính sách hiện tại. |
| Lượt dùng | `v_luot_dung_khuyen_mai` gom theo chương trình + hóa đơn đã thanh toán; hóa đơn hủy không được tính. Đây là view đọc, không là bảng giao dịch mới. |
| Xóa KM | Chặn cả tham chiếu trong giỏ và hóa đơn mọi trạng thái; việc không còn lượt hợp lệ không đồng nghĩa chưa từng có tham chiếu. Xóa liên kết sản phẩm rồi chương trình trong cùng transaction; FK bảo vệ cuộc đua thêm tham chiếu. |

Sơ đồ chọn các cột và quan hệ phục vụ phần KH/KM, không thay thế ERD toàn hệ thống. Xem `docs/db/schema_v1.sql`, entity và các migration để biết toàn bộ cột/CHECK/FK. Migration ảnh ở nhánh nền còn cần người phụ trách CSDL duyệt; tài liệu này không tự cấp approval.
