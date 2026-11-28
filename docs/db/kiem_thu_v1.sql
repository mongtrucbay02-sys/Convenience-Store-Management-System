-- Kiểm thử nhanh schema_v1.sql: luồng dữ liệu mẫu + các ràng buộc phải chặn.
-- Mỗi lệnh "PHAI LOI" được chạy riêng bằng script kiem_thu.sh để kiểm tra có lỗi.
SET NAMES utf8mb4; SET time_zone = '+07:00';

INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id, bat_buoc_doi_mk)
VALUES ('admin', 'AQAAAAIAAYagAAAAE...', 'Quản trị', 1, TRUE),
       ('thungan1', 'x', 'Thu ngân 1', 2, FALSE),
       ('kho1', 'x', 'Kho 1', 3, FALSE);

INSERT INTO danh_muc (ten, khong_doi_tra) VALUES ('Sữa và chế phẩm', FALSE), ('Đồ ăn tươi', TRUE);
INSERT INTO bo_dem_chung_tu (tien_to, ky, so_cuoi) VALUES ('SP','',0);
UPDATE bo_dem_chung_tu SET so_cuoi = so_cuoi + 1 WHERE tien_to='SP' AND ky='';
INSERT INTO san_pham (ma, ten_san_pham, danh_muc_id, don_vi_tinh, nguoi_tao_id)
VALUES ('SP000001', 'Sữa tươi Vinamilk 180ml', 1, 'Hộp', 3), ('SP000002', 'Bánh mì kẹp', 2, 'Cái', 3);
INSERT INTO ton_kho (san_pham_id) VALUES (1), (2);
INSERT INTO lich_su_gia (san_pham_id, gia_ban, hieu_luc_tu, nguoi_tao_id) VALUES
  (1, 8000, NOW() - INTERVAL 1 DAY, 3), (2, 15000, NOW() - INTERVAL 1 DAY, 3),
  (1, 8500, NOW() + INTERVAL 3 DAY, 1);

-- Tìm kiếm không dấu (BR-SP-01)
SELECT 'BR-SP-01' kiem_tra, ma, ten_san_pham FROM san_pham WHERE ten_san_pham LIKE '%sua tuoi%';
SELECT 'BR-SP-11' kiem_tra, v.* FROM v_gia_ban_hien_hanh v;

INSERT INTO nha_cung_cap (ma, ten, so_dien_thoai) VALUES ('NCC0001', 'Công ty Sữa', '02812345678');
INSERT INTO phieu_nhap (ma_phieu, nha_cung_cap_id, ngay_nhap, nguoi_lap_id) VALUES ('PN-20261018-001', 1, CURDATE(), 3);
INSERT INTO ct_phieu_nhap (phieu_nhap_id, san_pham_id, ten_san_pham, don_vi_tinh, so_luong, gia_nhap)
VALUES (1, 1, 'Sữa tươi Vinamilk 180ml', 'Hộp', 24, 6500), (1, 2, 'Bánh mì kẹp', 'Cái', 10, 9000);
UPDATE phieu_nhap SET tong_tien = (SELECT SUM(thanh_tien) FROM ct_phieu_nhap WHERE phieu_nhap_id = 1),
       trang_thai='DA_NHAP_KHO', nguoi_xac_nhan_id=3, xac_nhan_luc=NOW(), phien_ban = phien_ban + 1 WHERE id = 1;
UPDATE ton_kho SET so_luong_ton = 24, gia_von_binh_quan = 6500 WHERE san_pham_id = 1;
UPDATE ton_kho SET so_luong_ton = 10, gia_von_binh_quan = 9000 WHERE san_pham_id = 2;
INSERT INTO the_kho (san_pham_id, loai_bien_dong, ma_chung_tu, chung_tu_id, so_luong_thay_doi, ton_sau, gia_von_ap_dung, gia_von_sau, nguoi_thuc_hien_id)
VALUES (1,'NHAP_HANG','PN-20261018-001',1,24,24,6500,6500,3), (2,'NHAP_HANG','PN-20261018-001',1,10,10,9000,9000,3);
SELECT 'phieu_nhap' kiem_tra, ma_phieu, tong_tien, trang_thai FROM phieu_nhap;

INSERT INTO khach_hang (ho_ten, so_dien_thoai, nguoi_tao_id) VALUES ('Nguyễn Văn A', '0901234567', 2);
INSERT INTO chuong_trinh_khuyen_mai (ma, ten, loai_giam, gia_tri, giam_toi_da, bat_dau_luc, ket_thuc_luc, chi_thanh_vien, trang_thai, nguoi_tao_id)
VALUES ('KM0001', 'Giảm 10% sữa', 'PHAN_TRAM', 10, NULL, NOW() - INTERVAL 1 DAY, NOW() + INTERVAL 7 DAY, TRUE, 'DA_KICH_HOAT', 1),
       ('KM0002', 'Sắp chạy', 'SO_TIEN', 1000, NULL, NOW() + INTERVAL 1 DAY, NOW() + INTERVAL 7 DAY, FALSE, 'DA_KICH_HOAT', 1);
INSERT INTO khuyen_mai_san_pham VALUES (1, 1);
SELECT 'BR-KM-07' kiem_tra, ma, trang_thai, trang_thai_hien_thi FROM v_chuong_trinh_khuyen_mai;

-- Bán: 3 hộp sữa giảm 10% (800/hộp), 1 bánh mì; khách đưa 50.000
INSERT INTO gio_hang (thu_ngan_id, khach_hang_id) VALUES (2, 1);
INSERT INTO ct_gio_hang (gio_hang_id, san_pham_id, so_luong, don_gia, khuyen_mai_id, giam_don_vi) VALUES (1,1,3,8000,1,800), (1,2,1,15000,NULL,0);
INSERT INTO hoa_don (ma_hoa_don, gio_hang_id, thu_ngan_id, khach_hang_id, thanh_toan_luc, tong_tien_hang, tong_giam, tong_thanh_toan, tien_khach_dua, tien_thua, so_tien_mot_diem, diem_cong)
VALUES ('HD-20261018-0001', 1, 2, 1, NOW(), 39000, 2400, 36600, 50000, 13400, 10000, 3);
INSERT INTO ct_hoa_don (hoa_don_id, san_pham_id, ten_san_pham, don_vi_tinh, so_luong, don_gia, khuyen_mai_id, giam_don_vi, gia_von)
VALUES (1,1,'Sữa tươi Vinamilk 180ml','Hộp',3,8000,1,800,6500), (1,2,'Bánh mì kẹp','Cái',1,15000,NULL,0,9000);
UPDATE gio_hang SET trang_thai='DA_HOAN_TAT', hoan_tat_luc=NOW() WHERE id=1;
UPDATE khach_hang SET diem_hien_co = diem_hien_co + 3 WHERE id = 1;
INSERT INTO lich_su_diem (khach_hang_id, loai, so_diem, diem_sau, hoa_don_id, nguoi_thuc_hien_id) VALUES (1,'TICH_DIEM',3,3,1,2);
SELECT 'ct_hoa_don' kiem_tra, san_pham_id, thanh_tien FROM ct_hoa_don;
SELECT 'KM-06' kiem_tra, v.* FROM v_luot_dung_khuyen_mai v;

-- Đơn mới sau khi hoàn tất: được phép (đơn cũ không còn DANG_BAN)
INSERT INTO gio_hang (thu_ngan_id) VALUES (2);

-- Trả 1 hộp sữa, nhập lại kho; hoàn 7.200
INSERT INTO phieu_doi_tra (ma_phieu, loai, hoa_don_goc_id, khach_hang_id, chinh_sach_id, trang_thai, gia_tri_hang_tra, tien_hoan, diem_thu_hoi, thu_ngan_id, hoan_tat_luc)
VALUES ('TH-20261018-001','TRA_HANG',1,1,1,'HOAN_TAT',7200,7200,1,2,NOW());
INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id, san_pham_id, hoa_don_id, so_luong, don_gia_thuc_tra, ly_do, cach_xu_ly, gia_von)
VALUES (1,1,1,1,7200,'KHONG_DUNG_NHU_CAU','NHAP_LAI',6500);
SELECT 'ct_phieu_doi_tra' kiem_tra, thanh_tien, gia_tri_huy FROM ct_phieu_doi_tra;

-- Phiếu đổi trả cần duyệt (vượt ngưỡng) đang chờ: hợp lệ
INSERT INTO phieu_doi_tra (ma_phieu, loai, hoa_don_goc_id, chinh_sach_id, trang_thai, can_duyet_vuot_nguong, gia_tri_hang_tra, tien_hoan, thu_ngan_id)
VALUES ('TH-20261018-002','TRA_HANG',1,1,'CHO_DUYET',TRUE,7200,7200,2);

-- Hủy hóa đơn: được phép chuyển DA_THANH_TOAN -> DA_HUY
-- giỏ số 2 (đơn trống tạo ở trên) được bán rồi hủy hóa đơn
INSERT INTO ct_gio_hang (gio_hang_id, san_pham_id, so_luong, don_gia) VALUES (2, 2, 1, 15000);
INSERT INTO hoa_don (ma_hoa_don, gio_hang_id, thu_ngan_id, thanh_toan_luc, tong_tien_hang, tong_thanh_toan, tien_khach_dua, tien_thua)
VALUES ('HD-20261018-0002', 2, 2, NOW(), 15000, 15000, 20000, 5000);
INSERT INTO ct_hoa_don (hoa_don_id, san_pham_id, ten_san_pham, don_vi_tinh, so_luong, don_gia, gia_von) VALUES (2,2,'Bánh mì kẹp','Cái',1,15000,9000);
UPDATE gio_hang SET trang_thai='DA_HOAN_TAT', hoan_tat_luc=NOW() WHERE id=2;
UPDATE hoa_don SET trang_thai='DA_HUY', nguoi_huy_id=1, huy_luc=NOW(), ly_do_huy='Nhập nhầm' WHERE id=2;
SELECT 'POS-12' kiem_tra, ma_hoa_don, trang_thai FROM hoa_don;

-- Thu ngân mở đơn mới (đơn đang bán duy nhất)
INSERT INTO gio_hang (thu_ngan_id) VALUES (2);
