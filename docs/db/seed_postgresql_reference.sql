-- Reference data only. Review with database owner before applying to shared Supabase.
-- Never resets existing cash balance, policies, loyalty rules or user accounts.
BEGIN;
INSERT INTO smartmart.vai_tro(id,ma,ten) VALUES
  (1,'AD','Admin/Quản lý'),(2,'TN','Thu ngân'),(3,'NK','Nhân viên kho')
ON CONFLICT DO NOTHING;
DO $check_roles$
BEGIN
  IF (SELECT COUNT(*) FROM smartmart.vai_tro WHERE ma IN ('AD','TN','NK')) <> 3 THEN
    RAISE EXCEPTION 'Role IDs conflict with existing records; review data before seeding';
  END IF;
END $check_roles$;
INSERT INTO smartmart.quy_tac_tich_diem(so_tien_mot_diem,dang_bat,hieu_luc_tu)
SELECT 10000,TRUE,CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM smartmart.quy_tac_tich_diem);
INSERT INTO smartmart.chinh_sach_doi_tra(so_ngay_doi_tra,nguong_tien_can_duyet,hieu_luc_tu)
SELECT 7,500000,CURRENT_TIMESTAMP
WHERE NOT EXISTS (SELECT 1 FROM smartmart.chinh_sach_doi_tra);
INSERT INTO smartmart.quy_tien_mat(id,so_du,da_chot_so_du_dau)
VALUES(1,0,FALSE) ON CONFLICT(id) DO NOTHING;
COMMIT;
