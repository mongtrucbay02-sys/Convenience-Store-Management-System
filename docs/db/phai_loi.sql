UPDATE the_kho SET ton_sau = 99 WHERE id = 1
DELETE FROM the_kho WHERE id = 1
UPDATE ton_kho SET so_luong_ton = -1 WHERE san_pham_id = 1
INSERT INTO the_kho (san_pham_id, loai_bien_dong, ma_chung_tu, chung_tu_id, so_luong_thay_doi, ton_sau, gia_von_ap_dung, gia_von_sau, nguoi_thuc_hien_id) VALUES (1,'BAN_HANG','HD-x',1,5,10,1,1,2)
INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id) VALUES ('ADMIN', 'x', 'Trùng hoa thường', 1)
INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id) VALUES ('có dấu', 'x', 'Sai định dạng', 1)
UPDATE tai_khoan SET trang_thai = 'DA_KHOA' WHERE id = 2
INSERT INTO lich_su_gia (san_pham_id, gia_ban, hieu_luc_tu, nguoi_tao_id) VALUES (1, 8050, NOW(), 1)
INSERT INTO gio_hang (thu_ngan_id) VALUES (2)
INSERT INTO ct_gio_hang (gio_hang_id, san_pham_id, so_luong, don_gia, giam_don_vi) VALUES (2, 1, 1, 8000, 500)
UPDATE hoa_don SET tong_giam = 0 WHERE id = 1
UPDATE hoa_don SET trang_thai = 'DA_THANH_TOAN' WHERE id = 2
DELETE FROM hoa_don WHERE id = 1
UPDATE ct_hoa_don SET so_luong = 1 WHERE hoa_don_id = 1
INSERT INTO hoa_don (ma_hoa_don, gio_hang_id, thu_ngan_id, thanh_toan_luc, tong_tien_hang, tong_thanh_toan, tien_khach_dua, tien_thua) VALUES ('HD-x', 3, 2, NOW(), 15000, 15000, 10000, 0)
INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id, san_pham_id, hoa_don_id, so_luong, don_gia_thuc_tra, ly_do, cach_xu_ly, gia_von) VALUES (1, 2, 2, 1, 1, 'KHAC', 'HUY', 1)
INSERT INTO phieu_doi_tra (ma_phieu, loai, hoa_don_goc_id, chinh_sach_id, trang_thai, gia_tri_hang_tra, tien_hoan, thu_ngan_id) VALUES ('TH-x','TRA_HANG',1,1,'CHO_DUYET',100,100,2)
UPDATE phieu_doi_tra SET trang_thai = 'HOAN_TAT', hoan_tat_luc = NOW() WHERE id = 2
UPDATE khach_hang SET diem_hien_co = diem_hien_co - 100 WHERE id = 1
UPDATE lich_su_diem SET so_diem = 5 WHERE id = 1
INSERT INTO khach_hang (ho_ten, so_dien_thoai, nguoi_tao_id) VALUES ('Trùng số', '0901234567', 2)
INSERT INTO chuong_trinh_khuyen_mai (ma, ten, loai_giam, gia_tri, bat_dau_luc, ket_thuc_luc, nguoi_tao_id) VALUES ('KM-x','Sai', 'PHAN_TRAM', 150, NOW(), NOW() + INTERVAL 1 DAY, 1)
INSERT INTO chuong_trinh_khuyen_mai (ma, ten, loai_giam, gia_tri, bat_dau_luc, ket_thuc_luc, nguoi_tao_id) VALUES ('KM-y','Sai', 'SO_TIEN', 1000, NOW(), NOW() - INTERVAL 1 DAY, 1)
INSERT INTO phieu_kiem_ke (ma_phieu, pham_vi, nguoi_lap_id) VALUES ('KK-x', 'DANH_MUC', 3)
INSERT INTO phieu_nhap (ma_phieu, nha_cung_cap_id, ngay_nhap, nguoi_lap_id, trang_thai) VALUES ('PN-x', 1, CURDATE(), 3, 'DA_HUY')
INSERT INTO ct_xuat_huy (phieu_xuat_huy_id, san_pham_id, so_luong, ly_do) VALUES (1, 1, 1, 'KHAC')
INSERT INTO danh_muc (ten) VALUES ('sua va che pham')
INSERT INTO hoa_don (ma_hoa_don, thu_ngan_id, thanh_toan_luc, tong_tien_hang, tong_thanh_toan, tien_khach_dua, tien_thua) VALUES ('HD-y', 2, NOW(), 15000, 15000, 20000, 5000)
INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id, san_pham_id, hoa_don_id, so_luong, don_gia_thuc_tra, ly_do, cach_xu_ly, gia_von) VALUES (2, 2, 2, 1, 15000, 'LOI_SAN_PHAM', 'HUY', 9000)
