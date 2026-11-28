-- phai_loi_tung_lenh.sql: bản dùng trong VS Code / Workbench của phai_loi.sql.
-- Điều kiện: CSDL vừa chạy schema_v1.sql rồi kiem_thu_v1.sql (chưa chạy gì thêm).
-- Mỗi lệnh có dấu ; riêng. KẾT QUẢ ĐÚNG: lệnh chuẩn bị thành công, cả 29 lệnh kiểm tra đều báo lỗi.
-- Nếu công cụ dừng ở lỗi đầu tiên: đặt con trỏ vào từng lệnh và chạy riêng từng lệnh.

-- Chuẩn bị (phải THÀNH CÔNG):
SET time_zone = '+07:00';
INSERT INTO phieu_xuat_huy (ma_phieu, nguoi_lap_id) VALUES ('XH-20261018-001', 3);

-- [01] phải báo lỗi
UPDATE the_kho SET ton_sau = 99 WHERE id = 1;
-- [02] phải báo lỗi
DELETE FROM the_kho WHERE id = 1;
-- [03] phải báo lỗi
UPDATE ton_kho SET so_luong_ton = -1 WHERE san_pham_id = 1;
-- [04] phải báo lỗi
INSERT INTO the_kho (san_pham_id, loai_bien_dong, ma_chung_tu, chung_tu_id, so_luong_thay_doi, ton_sau, gia_von_ap_dung, gia_von_sau, nguoi_thuc_hien_id) VALUES (1,'BAN_HANG','HD-x',1,5,10,1,1,2);
-- [05] phải báo lỗi
INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id) VALUES ('ADMIN', 'x', 'Trùng hoa thường', 1);
-- [06] phải báo lỗi
INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id) VALUES ('có dấu', 'x', 'Sai định dạng', 1);
-- [07] phải báo lỗi
UPDATE tai_khoan SET trang_thai = 'DA_KHOA' WHERE id = 2;
-- [08] phải báo lỗi
INSERT INTO lich_su_gia (san_pham_id, gia_ban, hieu_luc_tu, nguoi_tao_id) VALUES (1, 8050, NOW(), 1);
-- [09] phải báo lỗi
INSERT INTO gio_hang (thu_ngan_id) VALUES (2);
-- [10] phải báo lỗi
INSERT INTO ct_gio_hang (gio_hang_id, san_pham_id, so_luong, don_gia, giam_don_vi) VALUES (2, 1, 1, 8000, 500);
-- [11] phải báo lỗi
UPDATE hoa_don SET tong_giam = 0 WHERE id = 1;
-- [12] phải báo lỗi
UPDATE hoa_don SET trang_thai = 'DA_THANH_TOAN' WHERE id = 2;
-- [13] phải báo lỗi
DELETE FROM hoa_don WHERE id = 1;
-- [14] phải báo lỗi
UPDATE ct_hoa_don SET so_luong = 1 WHERE hoa_don_id = 1;
-- [15] phải báo lỗi
INSERT INTO hoa_don (ma_hoa_don, gio_hang_id, thu_ngan_id, thanh_toan_luc, tong_tien_hang, tong_thanh_toan, tien_khach_dua, tien_thua) VALUES ('HD-x', 3, 2, NOW(), 15000, 15000, 10000, 0);
-- [16] phải báo lỗi
INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id, san_pham_id, hoa_don_id, so_luong, don_gia_thuc_tra, ly_do, cach_xu_ly, gia_von) VALUES (1, 2, 2, 1, 1, 'KHAC', 'HUY', 1);
-- [17] phải báo lỗi
INSERT INTO phieu_doi_tra (ma_phieu, loai, hoa_don_goc_id, chinh_sach_id, trang_thai, gia_tri_hang_tra, tien_hoan, thu_ngan_id) VALUES ('TH-x','TRA_HANG',1,1,'CHO_DUYET',100,100,2);
-- [18] phải báo lỗi
UPDATE phieu_doi_tra SET trang_thai = 'HOAN_TAT', hoan_tat_luc = NOW() WHERE id = 2;
-- [19] phải báo lỗi
UPDATE khach_hang SET diem_hien_co = diem_hien_co - 100 WHERE id = 1;
-- [20] phải báo lỗi
UPDATE lich_su_diem SET so_diem = 5 WHERE id = 1;
-- [21] phải báo lỗi
INSERT INTO khach_hang (ho_ten, so_dien_thoai, nguoi_tao_id) VALUES ('Trùng số', '0901234567', 2);
-- [22] phải báo lỗi
INSERT INTO chuong_trinh_khuyen_mai (ma, ten, loai_giam, gia_tri, bat_dau_luc, ket_thuc_luc, nguoi_tao_id) VALUES ('KM-x','Sai', 'PHAN_TRAM', 150, NOW(), NOW() + INTERVAL 1 DAY, 1);
-- [23] phải báo lỗi
INSERT INTO chuong_trinh_khuyen_mai (ma, ten, loai_giam, gia_tri, bat_dau_luc, ket_thuc_luc, nguoi_tao_id) VALUES ('KM-y','Sai', 'SO_TIEN', 1000, NOW(), NOW() - INTERVAL 1 DAY, 1);
-- [24] phải báo lỗi
INSERT INTO phieu_kiem_ke (ma_phieu, pham_vi, nguoi_lap_id) VALUES ('KK-x', 'DANH_MUC', 3);
-- [25] phải báo lỗi
INSERT INTO phieu_nhap (ma_phieu, nha_cung_cap_id, ngay_nhap, nguoi_lap_id, trang_thai) VALUES ('PN-x', 1, CURDATE(), 3, 'DA_HUY');
-- [26] phải báo lỗi
INSERT INTO ct_xuat_huy (phieu_xuat_huy_id, san_pham_id, so_luong, ly_do) VALUES (1, 1, 1, 'KHAC');
-- [27] phải báo lỗi
INSERT INTO danh_muc (ten) VALUES ('sua va che pham');
-- [28] phải báo lỗi
INSERT INTO hoa_don (ma_hoa_don, thu_ngan_id, thanh_toan_luc, tong_tien_hang, tong_thanh_toan, tien_khach_dua, tien_thua) VALUES ('HD-y', 2, NOW(), 15000, 15000, 20000, 5000);
-- [29] phải báo lỗi
INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id, san_pham_id, hoa_don_id, so_luong, don_gia_thuc_tra, ly_do, cach_xu_ly, gia_von) VALUES (2, 2, 2, 1, 15000, 'LOI_SAN_PHAM', 'HUY', 9000);
