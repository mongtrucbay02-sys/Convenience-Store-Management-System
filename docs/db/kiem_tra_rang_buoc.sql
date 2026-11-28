-- =====================================================================
-- kiem_tra_rang_buoc.sql – chạy MỘT LẦN bằng nút Run trong MySQL Workbench.
-- Thử 29 thao tác sai (giống phai_loi.sql); thao tác nào bị CSDL chặn là ĐẠT.
-- Điều kiện: CSDL đã chạy schema_v1.sql rồi kiem_thu_v1.sql. Chọn CSDL trước (USE ...).
-- Kết quả: bảng 29 dòng + dòng tổng kết; đạt khi 'Tổng' = 29/29.
-- Chạy lại nhiều lần được. Không tạo bảng hay thủ tục nào ở lại sau khi chạy.
-- =====================================================================
SET time_zone = '+07:00';

-- Chuẩn bị: phiếu xuất hủy dùng cho lệnh [26] (bỏ qua nếu đã có)
INSERT INTO phieu_xuat_huy (ma_phieu, nguoi_lap_id)
SELECT 'XH-20261018-001', 3 FROM DUAL
WHERE NOT EXISTS (SELECT 1 FROM phieu_xuat_huy WHERE ma_phieu = 'XH-20261018-001');

DROP TEMPORARY TABLE IF EXISTS kt_ket_qua;
CREATE TEMPORARY TABLE kt_ket_qua (so INT PRIMARY KEY, thao_tac TEXT, ket_qua VARCHAR(20), thong_bao_loi TEXT);
DROP PROCEDURE IF EXISTS kt_thu;

DELIMITER //
CREATE PROCEDURE kt_thu(IN p_so INT, IN p_lenh TEXT)
BEGIN
  DECLARE v_loi TEXT DEFAULT NULL;
  DECLARE CONTINUE HANDLER FOR SQLEXCEPTION
    GET DIAGNOSTICS CONDITION 1 v_loi = MESSAGE_TEXT;
  SET @kt_lenh = p_lenh;
  PREPARE kt_s FROM @kt_lenh;
  IF v_loi IS NULL THEN
    EXECUTE kt_s;
    DEALLOCATE PREPARE kt_s;
  END IF;
  INSERT INTO kt_ket_qua VALUES (p_so, p_lenh,
    IF(v_loi IS NULL, 'KHONG DAT', 'Dat (bi chan)'), v_loi);
END//
DELIMITER ;

CALL kt_thu(1, 'UPDATE the_kho SET ton_sau = 99 WHERE id = 1');
CALL kt_thu(2, 'DELETE FROM the_kho WHERE id = 1');
CALL kt_thu(3, 'UPDATE ton_kho SET so_luong_ton = -1 WHERE san_pham_id = 1');
CALL kt_thu(4, 'INSERT INTO the_kho (san_pham_id, loai_bien_dong, ma_chung_tu, chung_tu_id, so_luong_thay_doi, ton_sau, gia_von_ap_dung, gia_von_sau, nguoi_thuc_hien_id) VALUES (1,''BAN_HANG'',''HD-x'',1,5,10,1,1,2)');
CALL kt_thu(5, 'INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id) VALUES (''ADMIN'', ''x'', ''Trùng hoa thường'', 1)');
CALL kt_thu(6, 'INSERT INTO tai_khoan (ten_dang_nhap, mat_khau_bam, ho_ten, vai_tro_id) VALUES (''có dấu'', ''x'', ''Sai định dạng'', 1)');
CALL kt_thu(7, 'UPDATE tai_khoan SET trang_thai = ''DA_KHOA'' WHERE id = 2');
CALL kt_thu(8, 'INSERT INTO lich_su_gia (san_pham_id, gia_ban, hieu_luc_tu, nguoi_tao_id) VALUES (1, 8050, NOW(), 1)');
CALL kt_thu(9, 'INSERT INTO gio_hang (thu_ngan_id) VALUES (2)');
CALL kt_thu(10, 'INSERT INTO ct_gio_hang (gio_hang_id, san_pham_id, so_luong, don_gia, giam_don_vi) VALUES (2, 1, 1, 8000, 500)');
CALL kt_thu(11, 'UPDATE hoa_don SET tong_giam = 0 WHERE id = 1');
CALL kt_thu(12, 'UPDATE hoa_don SET trang_thai = ''DA_THANH_TOAN'' WHERE id = 2');
CALL kt_thu(13, 'DELETE FROM hoa_don WHERE id = 1');
CALL kt_thu(14, 'UPDATE ct_hoa_don SET so_luong = 1 WHERE hoa_don_id = 1');
CALL kt_thu(15, 'INSERT INTO hoa_don (ma_hoa_don, gio_hang_id, thu_ngan_id, thanh_toan_luc, tong_tien_hang, tong_thanh_toan, tien_khach_dua, tien_thua) VALUES (''HD-x'', 3, 2, NOW(), 15000, 15000, 10000, 0)');
CALL kt_thu(16, 'INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id, san_pham_id, hoa_don_id, so_luong, don_gia_thuc_tra, ly_do, cach_xu_ly, gia_von) VALUES (1, 2, 2, 1, 1, ''KHAC'', ''HUY'', 1)');
CALL kt_thu(17, 'INSERT INTO phieu_doi_tra (ma_phieu, loai, hoa_don_goc_id, chinh_sach_id, trang_thai, gia_tri_hang_tra, tien_hoan, thu_ngan_id) VALUES (''TH-x'',''TRA_HANG'',1,1,''CHO_DUYET'',100,100,2)');
CALL kt_thu(18, 'UPDATE phieu_doi_tra SET trang_thai = ''HOAN_TAT'', hoan_tat_luc = NOW() WHERE id = 2');
CALL kt_thu(19, 'UPDATE khach_hang SET diem_hien_co = diem_hien_co - 100 WHERE id = 1');
CALL kt_thu(20, 'UPDATE lich_su_diem SET so_diem = 5 WHERE id = 1');
CALL kt_thu(21, 'INSERT INTO khach_hang (ho_ten, so_dien_thoai, nguoi_tao_id) VALUES (''Trùng số'', ''0901234567'', 2)');
CALL kt_thu(22, 'INSERT INTO chuong_trinh_khuyen_mai (ma, ten, loai_giam, gia_tri, bat_dau_luc, ket_thuc_luc, nguoi_tao_id) VALUES (''KM-x'',''Sai'', ''PHAN_TRAM'', 150, NOW(), NOW() + INTERVAL 1 DAY, 1)');
CALL kt_thu(23, 'INSERT INTO chuong_trinh_khuyen_mai (ma, ten, loai_giam, gia_tri, bat_dau_luc, ket_thuc_luc, nguoi_tao_id) VALUES (''KM-y'',''Sai'', ''SO_TIEN'', 1000, NOW(), NOW() - INTERVAL 1 DAY, 1)');
CALL kt_thu(24, 'INSERT INTO phieu_kiem_ke (ma_phieu, pham_vi, nguoi_lap_id) VALUES (''KK-x'', ''DANH_MUC'', 3)');
CALL kt_thu(25, 'INSERT INTO phieu_nhap (ma_phieu, nha_cung_cap_id, ngay_nhap, nguoi_lap_id, trang_thai) VALUES (''PN-x'', 1, CURDATE(), 3, ''DA_HUY'')');
CALL kt_thu(26, 'INSERT INTO ct_xuat_huy (phieu_xuat_huy_id, san_pham_id, so_luong, ly_do) VALUES (1, 1, 1, ''KHAC'')');
CALL kt_thu(27, 'INSERT INTO danh_muc (ten) VALUES (''sua va che pham'')');
CALL kt_thu(28, 'INSERT INTO hoa_don (ma_hoa_don, thu_ngan_id, thanh_toan_luc, tong_tien_hang, tong_thanh_toan, tien_khach_dua, tien_thua) VALUES (''HD-y'', 2, NOW(), 15000, 15000, 20000, 5000)');
CALL kt_thu(29, 'INSERT INTO ct_phieu_doi_tra (phieu_doi_tra_id, san_pham_id, hoa_don_id, so_luong, don_gia_thuc_tra, ly_do, cach_xu_ly, gia_von) VALUES (2, 2, 2, 1, 15000, ''LOI_SAN_PHAM'', ''HUY'', 9000)');

SELECT so AS `#`, ket_qua AS `Kết quả`, thong_bao_loi AS `Lỗi CSDL trả về`, thao_tac AS `Thao tác sai` FROM kt_ket_qua ORDER BY so;
SELECT CONCAT(SUM(ket_qua <> 'KHONG DAT'), '/', COUNT(*)) AS `Tổng bị chặn`,
       IF(SUM(ket_qua = 'KHONG DAT') = 0, 'ĐẠT', 'CÓ THAO TÁC SAI LỌT QUA') AS `Kết luận` FROM kt_ket_qua;

DROP PROCEDURE kt_thu;
DROP TEMPORARY TABLE kt_ket_qua;
