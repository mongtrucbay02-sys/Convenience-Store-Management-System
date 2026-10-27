-- =====================================================================
-- CSDL Hệ thống quản lý cửa hàng tiện lợi - Nhóm 7
-- ERD v1 (18/10/2026) - toàn hệ thống, 28 bảng, 3 view, 10 trigger
--
-- Hệ quản trị chuẩn: MySQL 9.7.2 (LTS). Chỉ dùng tính năng có ở cả MySQL 8.4
-- để chạy được trên Aiven (Aiven hiện chỉ có 8.4). InnoDB, utf8mb4,
-- utf8mb4_0900_ai_ci. Giờ lưu là giờ Việt Nam (+07:00).
--
-- Thứ tự chạy theo phụ thuộc khóa ngoại:
--   0. Dùng chung     bo_dem_chung_tu
--   1. Tài khoản      vai_tro, tai_khoan, phien_dang_nhap
--   2. Sản phẩm       danh_muc, san_pham, lich_su_gia
--   3. Nhập hàng      nha_cung_cap, phieu_nhap, ct_phieu_nhap
--   4. Kho            ton_kho, the_kho, phieu_kiem_ke, ct_kiem_ke,
--                     phieu_xuat_huy, ct_xuat_huy
--   5. Khách hàng, KM khach_hang, quy_tac_tich_diem,
--                     chuong_trinh_khuyen_mai, khuyen_mai_san_pham
--   6. Hóa đơn        gio_hang, ct_gio_hang, hoa_don, ct_hoa_don
--   7. Đổi trả        chinh_sach_doi_tra, phieu_doi_tra, ct_phieu_doi_tra
--   8. Điểm           lich_su_diem
--   9. View, trigger, dữ liệu khởi tạo
--
-- Trong dự án, bảng được tạo bằng EF Core migration. Tệp này là bản tham
-- chiếu: migration sinh ra phải cho cùng cấu trúc. Trigger và view đưa vào
-- migration bằng migrationBuilder.Sql(...).
-- =====================================================================

SET NAMES utf8mb4 COLLATE utf8mb4_0900_ai_ci;
SET time_zone = '+07:00';

-- ---------------------------------------------------------------------
-- 0. DÙNG CHUNG
-- ---------------------------------------------------------------------

-- Bộ đếm sinh mã: PN/HD/TH/KK/XH theo ngày (ky = YYYYMMDD);
-- SP/NCC/KM không theo ngày (ky = '').
CREATE TABLE bo_dem_chung_tu (
  tien_to   VARCHAR(3)   NOT NULL,
  ky        CHAR(8)      NOT NULL DEFAULT '',
  so_cuoi   INT UNSIGNED NOT NULL DEFAULT 0,
  PRIMARY KEY (tien_to, ky),
  CONSTRAINT ck_bo_dem_tien_to CHECK (tien_to IN ('PN','HD','TH','KK','XH','SP','NCC','KM'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------
-- 1. TÀI KHOẢN VÀ VAI TRÒ (#1)
-- ---------------------------------------------------------------------

CREATE TABLE vai_tro (
  id   TINYINT UNSIGNED NOT NULL,
  ma   CHAR(2)          NOT NULL,
  ten  VARCHAR(50)      NOT NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_vai_tro_ma (ma),
  CONSTRAINT ck_vai_tro_ma CHECK (ma IN ('AD','TN','NK'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE tai_khoan (
  id                  INT UNSIGNED     NOT NULL AUTO_INCREMENT,
  ten_dang_nhap       VARCHAR(50)      NOT NULL,
  mat_khau_bam        VARCHAR(255)     NOT NULL,
  ho_ten              VARCHAR(100)     NOT NULL,
  so_dien_thoai       VARCHAR(10)      NULL,
  email               VARCHAR(100)     NULL,
  vai_tro_id          TINYINT UNSIGNED NOT NULL,
  trang_thai          ENUM('HOAT_DONG','DA_KHOA') NOT NULL DEFAULT 'HOAT_DONG',
  ly_do_khoa          VARCHAR(255)     NULL,
  bat_buoc_doi_mk     BOOLEAN          NOT NULL DEFAULT TRUE,
  so_lan_sai          TINYINT UNSIGNED NOT NULL DEFAULT 0,
  khoa_tam_den        DATETIME         NULL,
  lan_dang_nhap_cuoi  DATETIME         NULL,
  phien_ban           INT UNSIGNED     NOT NULL DEFAULT 1,
  ngay_tao            DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ngay_cap_nhat       DATETIME         NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_tai_khoan_ten (ten_dang_nhap),
  UNIQUE KEY uq_tai_khoan_sdt (so_dien_thoai),
  UNIQUE KEY uq_tai_khoan_email (email),
  KEY idx_tai_khoan_vai_tro (vai_tro_id, trang_thai),
  CONSTRAINT ck_tai_khoan_ten CHECK (REGEXP_LIKE(ten_dang_nhap, '^[A-Za-z0-9._]{3,50}$')),
  CONSTRAINT ck_tai_khoan_sdt CHECK (so_dien_thoai IS NULL OR REGEXP_LIKE(so_dien_thoai, '^0[0-9]{9}$')),
  CONSTRAINT ck_tai_khoan_khoa CHECK (trang_thai <> 'DA_KHOA' OR ly_do_khoa IS NOT NULL),
  CONSTRAINT fk_tai_khoan_vai_tro FOREIGN KEY (vai_tro_id) REFERENCES vai_tro (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE phien_dang_nhap (
  id              BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  tai_khoan_id    INT UNSIGNED    NOT NULL,
  token_bam       CHAR(64)        NOT NULL,
  ngay_tao        DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  hoat_dong_cuoi  DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  huy_luc         DATETIME        NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_phien_token (token_bam),
  KEY idx_phien_tai_khoan (tai_khoan_id, huy_luc),
  CONSTRAINT fk_phien_tai_khoan FOREIGN KEY (tai_khoan_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------
-- 2. SẢN PHẨM VÀ DANH MỤC (#2)
-- ---------------------------------------------------------------------

CREATE TABLE danh_muc (
  id             INT UNSIGNED NOT NULL AUTO_INCREMENT,
  ten            VARCHAR(100) NOT NULL,
  mo_ta          VARCHAR(255) NULL,
  khong_doi_tra  BOOLEAN      NOT NULL DEFAULT FALSE,
  trang_thai     ENUM('HOAT_DONG','DA_AN') NOT NULL DEFAULT 'HOAT_DONG',
  phien_ban      INT UNSIGNED NOT NULL DEFAULT 1,
  ngay_tao       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ngay_cap_nhat  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_danh_muc_ten (ten)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE san_pham (
  id             INT UNSIGNED NOT NULL AUTO_INCREMENT,
  ma             VARCHAR(20)  NOT NULL,
  ten_san_pham   VARCHAR(200) NOT NULL,
  danh_muc_id    INT UNSIGNED NOT NULL,
  don_vi_tinh    VARCHAR(20)  NOT NULL,
  anh_url        VARCHAR(255) NULL,
  mo_ta          VARCHAR(1000) NULL,
  trang_thai     ENUM('DANG_KINH_DOANH','NGUNG_KINH_DOANH') NOT NULL DEFAULT 'DANG_KINH_DOANH',
  nguoi_tao_id   INT UNSIGNED NOT NULL,
  phien_ban      INT UNSIGNED NOT NULL DEFAULT 1,
  ngay_tao       DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ngay_cap_nhat  DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_san_pham_ma (ma),
  KEY idx_san_pham_danh_muc (danh_muc_id, trang_thai),
  KEY idx_san_pham_ten (ten_san_pham),
  CONSTRAINT fk_san_pham_danh_muc FOREIGN KEY (danh_muc_id) REFERENCES danh_muc (id),
  CONSTRAINT fk_san_pham_nguoi_tao FOREIGN KEY (nguoi_tao_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Lịch sử giá bán (PA-SP-04.B). Giá đang hiệu lực = bản ghi chưa hủy có
-- hieu_luc_tu gần nhất và không sau thời điểm hiện tại (BR-SP-11).
CREATE TABLE lich_su_gia (
  id            INT UNSIGNED NOT NULL AUTO_INCREMENT,
  san_pham_id   INT UNSIGNED NOT NULL,
  gia_ban       INT UNSIGNED NOT NULL,
  hieu_luc_tu   DATETIME     NOT NULL,
  nguoi_tao_id  INT UNSIGNED NOT NULL,
  ngay_tao      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  huy_luc       DATETIME     NULL,
  nguoi_huy_id  INT UNSIGNED NULL,
  PRIMARY KEY (id),
  KEY idx_lsg_san_pham_hieu_luc (san_pham_id, hieu_luc_tu),
  CONSTRAINT ck_lsg_gia CHECK (gia_ban > 0 AND gia_ban % 100 = 0),
  CONSTRAINT ck_lsg_huy CHECK ((huy_luc IS NULL) = (nguoi_huy_id IS NULL)),
  CONSTRAINT fk_lsg_san_pham FOREIGN KEY (san_pham_id) REFERENCES san_pham (id),
  CONSTRAINT fk_lsg_nguoi_tao FOREIGN KEY (nguoi_tao_id) REFERENCES tai_khoan (id),
  CONSTRAINT fk_lsg_nguoi_huy FOREIGN KEY (nguoi_huy_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------
-- 3. NHẬP HÀNG VÀ NHÀ CUNG CẤP (#2)
-- ---------------------------------------------------------------------

CREATE TABLE nha_cung_cap (
  id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
  ma              VARCHAR(20)  NOT NULL,
  ten             VARCHAR(200) NOT NULL,
  nguoi_lien_he   VARCHAR(100) NULL,
  so_dien_thoai   VARCHAR(11)  NOT NULL,
  email           VARCHAR(100) NULL,
  dia_chi         VARCHAR(255) NULL,
  ma_so_thue      VARCHAR(14)  NULL,
  ghi_chu         VARCHAR(500) NULL,
  trang_thai      ENUM('HOAT_DONG','NGUNG_HOP_TAC') NOT NULL DEFAULT 'HOAT_DONG',
  phien_ban       INT UNSIGNED NOT NULL DEFAULT 1,
  ngay_tao        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ngay_cap_nhat   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_ncc_ma (ma),
  UNIQUE KEY uq_ncc_sdt (so_dien_thoai),
  KEY idx_ncc_ten (ten),
  CONSTRAINT ck_ncc_sdt CHECK (REGEXP_LIKE(so_dien_thoai, '^0[0-9]{9,10}$'))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE phieu_nhap (
  id                 INT UNSIGNED NOT NULL AUTO_INCREMENT,
  ma_phieu           VARCHAR(20)  NOT NULL,
  nha_cung_cap_id    INT UNSIGNED NOT NULL,
  so_chung_tu_ncc    VARCHAR(50)  NULL,
  ngay_nhap          DATE         NOT NULL,
  trang_thai         ENUM('NHAP','DA_NHAP_KHO','DA_HUY') NOT NULL DEFAULT 'NHAP',
  tong_tien          BIGINT UNSIGNED NOT NULL DEFAULT 0,
  ghi_chu            VARCHAR(255) NULL,
  nguoi_lap_id       INT UNSIGNED NOT NULL,
  ngay_lap           DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  nguoi_xac_nhan_id  INT UNSIGNED NULL,
  xac_nhan_luc       DATETIME     NULL,
  nguoi_huy_id       INT UNSIGNED NULL,
  huy_luc            DATETIME     NULL,
  ly_do_huy          VARCHAR(255) NULL,
  phien_ban          INT UNSIGNED NOT NULL DEFAULT 1,
  ngay_cap_nhat      DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_phieu_nhap_ma (ma_phieu),
  KEY idx_phieu_nhap_tt (trang_thai, ngay_lap),
  KEY idx_phieu_nhap_ncc (nha_cung_cap_id, xac_nhan_luc),
  KEY idx_phieu_nhap_xac_nhan (xac_nhan_luc),
  CONSTRAINT ck_pn_xac_nhan CHECK (trang_thai <> 'DA_NHAP_KHO' OR (nguoi_xac_nhan_id IS NOT NULL AND xac_nhan_luc IS NOT NULL)),
  CONSTRAINT ck_pn_huy CHECK (trang_thai <> 'DA_HUY' OR (nguoi_huy_id IS NOT NULL AND huy_luc IS NOT NULL AND ly_do_huy IS NOT NULL)),
  CONSTRAINT fk_pn_ncc FOREIGN KEY (nha_cung_cap_id) REFERENCES nha_cung_cap (id),
  CONSTRAINT fk_pn_nguoi_lap FOREIGN KEY (nguoi_lap_id) REFERENCES tai_khoan (id),
  CONSTRAINT fk_pn_nguoi_xac_nhan FOREIGN KEY (nguoi_xac_nhan_id) REFERENCES tai_khoan (id),
  CONSTRAINT fk_pn_nguoi_huy FOREIGN KEY (nguoi_huy_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ct_phieu_nhap (
  phieu_nhap_id  INT UNSIGNED NOT NULL,
  san_pham_id    INT UNSIGNED NOT NULL,
  ten_san_pham   VARCHAR(200) NOT NULL,
  don_vi_tinh    VARCHAR(20)  NOT NULL,
  so_luong       INT UNSIGNED NOT NULL,
  gia_nhap       INT UNSIGNED NOT NULL,
  thanh_tien     BIGINT UNSIGNED AS (so_luong * gia_nhap) STORED,
  PRIMARY KEY (phieu_nhap_id, san_pham_id),
  KEY idx_ctpn_san_pham (san_pham_id),
  CONSTRAINT ck_ctpn_so_luong CHECK (so_luong > 0),
  CONSTRAINT fk_ctpn_phieu FOREIGN KEY (phieu_nhap_id) REFERENCES phieu_nhap (id),
  CONSTRAINT fk_ctpn_san_pham FOREIGN KEY (san_pham_id) REFERENCES san_pham (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------
-- 4. KHO VÀ TỒN KHO (#1 thiết kế; #2 lập trình KHO-xx; #1 dịch vụ kho)
-- ---------------------------------------------------------------------

CREATE TABLE ton_kho (
  san_pham_id        INT UNSIGNED  NOT NULL,
  so_luong_ton       INT           NOT NULL DEFAULT 0,
  gia_von_binh_quan  DECIMAL(15,2) NOT NULL DEFAULT 0,
  nguong_toi_thieu   INT           NOT NULL DEFAULT 5,
  ngay_cap_nhat      DATETIME      NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (san_pham_id),
  CONSTRAINT ck_ton_khong_am CHECK (so_luong_ton >= 0),
  CONSTRAINT ck_gia_von_khong_am CHECK (gia_von_binh_quan >= 0),
  CONSTRAINT ck_nguong_khong_am CHECK (nguong_toi_thieu >= 0),
  CONSTRAINT fk_ton_kho_san_pham FOREIGN KEY (san_pham_id) REFERENCES san_pham (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE the_kho (
  id                  BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  san_pham_id         INT UNSIGNED    NOT NULL,
  loai_bien_dong      ENUM('NHAP_HANG','BAN_HANG','HUY_HOA_DON','TRA_HANG','KIEM_KE','XUAT_HUY') NOT NULL,
  ma_chung_tu         VARCHAR(20)     NOT NULL,
  chung_tu_id         INT UNSIGNED    NOT NULL,
  so_luong_thay_doi   INT             NOT NULL,
  ton_sau             INT             NOT NULL,
  gia_von_ap_dung     DECIMAL(15,2)   NOT NULL,
  gia_von_sau         DECIMAL(15,2)   NOT NULL,
  nguoi_thuc_hien_id  INT UNSIGNED    NOT NULL,
  thoi_diem           DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_the_kho_sp_tg (san_pham_id, thoi_diem),
  KEY idx_the_kho_tg (thoi_diem),
  KEY idx_the_kho_chung_tu (loai_bien_dong, chung_tu_id),
  CONSTRAINT ck_the_kho_khac_0 CHECK (so_luong_thay_doi <> 0),
  CONSTRAINT ck_the_kho_ton_sau CHECK (ton_sau >= 0),
  CONSTRAINT ck_the_kho_huong CHECK (
       (loai_bien_dong IN ('NHAP_HANG','HUY_HOA_DON','TRA_HANG') AND so_luong_thay_doi > 0)
    OR (loai_bien_dong IN ('BAN_HANG','XUAT_HUY') AND so_luong_thay_doi < 0)
    OR  loai_bien_dong = 'KIEM_KE'),
  CONSTRAINT fk_the_kho_san_pham FOREIGN KEY (san_pham_id) REFERENCES san_pham (id),
  CONSTRAINT fk_the_kho_nguoi FOREIGN KEY (nguoi_thuc_hien_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE phieu_kiem_ke (
  id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
  ma_phieu        VARCHAR(20)  NOT NULL,
  pham_vi         ENUM('TOAN_BO','DANH_MUC','CHON_SAN_PHAM') NOT NULL,
  danh_muc_id     INT UNSIGNED NULL,
  trang_thai      ENUM('NHAP','CHO_DUYET','BI_TU_CHOI','DA_DIEU_CHINH') NOT NULL DEFAULT 'NHAP',
  ghi_chu         VARCHAR(255) NULL,
  nguoi_lap_id    INT UNSIGNED NOT NULL,
  ngay_lap        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  gui_duyet_luc   DATETIME     NULL,
  nguoi_duyet_id  INT UNSIGNED NULL,
  duyet_luc       DATETIME     NULL,
  ly_do_tu_choi   VARCHAR(255) NULL,
  phien_ban       INT UNSIGNED NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  UNIQUE KEY uq_phieu_kiem_ke_ma (ma_phieu),
  KEY idx_phieu_kiem_ke_tt (trang_thai),
  CONSTRAINT ck_pkk_danh_muc CHECK ((pham_vi = 'DANH_MUC') = (danh_muc_id IS NOT NULL)),
  CONSTRAINT ck_pkk_tu_choi CHECK (trang_thai <> 'BI_TU_CHOI' OR ly_do_tu_choi IS NOT NULL),
  CONSTRAINT ck_pkk_duyet CHECK (trang_thai NOT IN ('BI_TU_CHOI','DA_DIEU_CHINH') OR (nguoi_duyet_id IS NOT NULL AND duyet_luc IS NOT NULL)),
  CONSTRAINT fk_pkk_danh_muc FOREIGN KEY (danh_muc_id) REFERENCES danh_muc (id),
  CONSTRAINT fk_pkk_nguoi_lap FOREIGN KEY (nguoi_lap_id) REFERENCES tai_khoan (id),
  CONSTRAINT fk_pkk_nguoi_duyet FOREIGN KEY (nguoi_duyet_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ct_kiem_ke (
  phieu_kiem_ke_id      INT UNSIGNED  NOT NULL,
  san_pham_id           INT UNSIGNED  NOT NULL,
  ton_luc_lap           INT           NOT NULL,
  so_luong_thuc_te      INT           NULL,
  ly_do_chenh_lech      VARCHAR(255)  NULL,
  chenh_lech_luc_duyet  INT           NULL,
  gia_von_luc_duyet     DECIMAL(15,2) NULL,
  PRIMARY KEY (phieu_kiem_ke_id, san_pham_id),
  KEY idx_ctkk_san_pham (san_pham_id),
  CONSTRAINT ck_ctkk_thuc_te CHECK (so_luong_thuc_te IS NULL OR so_luong_thuc_te >= 0),
  CONSTRAINT fk_ctkk_phieu FOREIGN KEY (phieu_kiem_ke_id) REFERENCES phieu_kiem_ke (id),
  CONSTRAINT fk_ctkk_san_pham FOREIGN KEY (san_pham_id) REFERENCES san_pham (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE phieu_xuat_huy (
  id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
  ma_phieu        VARCHAR(20)  NOT NULL,
  trang_thai      ENUM('NHAP','CHO_DUYET','BI_TU_CHOI','DA_XUAT_HUY') NOT NULL DEFAULT 'NHAP',
  ghi_chu         VARCHAR(255) NULL,
  nguoi_lap_id    INT UNSIGNED NOT NULL,
  ngay_lap        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  gui_duyet_luc   DATETIME     NULL,
  nguoi_duyet_id  INT UNSIGNED NULL,
  duyet_luc       DATETIME     NULL,
  ly_do_tu_choi   VARCHAR(255) NULL,
  phien_ban       INT UNSIGNED NOT NULL DEFAULT 1,
  PRIMARY KEY (id),
  UNIQUE KEY uq_phieu_xuat_huy_ma (ma_phieu),
  KEY idx_phieu_xuat_huy_tt (trang_thai),
  KEY idx_phieu_xuat_huy_duyet (duyet_luc),
  CONSTRAINT ck_pxh_tu_choi CHECK (trang_thai <> 'BI_TU_CHOI' OR ly_do_tu_choi IS NOT NULL),
  CONSTRAINT ck_pxh_duyet CHECK (trang_thai NOT IN ('BI_TU_CHOI','DA_XUAT_HUY') OR (nguoi_duyet_id IS NOT NULL AND duyet_luc IS NOT NULL)),
  CONSTRAINT fk_pxh_nguoi_lap FOREIGN KEY (nguoi_lap_id) REFERENCES tai_khoan (id),
  CONSTRAINT fk_pxh_nguoi_duyet FOREIGN KEY (nguoi_duyet_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ct_xuat_huy (
  phieu_xuat_huy_id  INT UNSIGNED  NOT NULL,
  san_pham_id        INT UNSIGNED  NOT NULL,
  so_luong           INT UNSIGNED  NOT NULL,
  ly_do              ENUM('HONG','HET_HAN','MAT','KHAC') NOT NULL,
  ghi_chu_ly_do      VARCHAR(255)  NULL,
  gia_von_luc_duyet  DECIMAL(15,2) NULL,
  PRIMARY KEY (phieu_xuat_huy_id, san_pham_id),
  KEY idx_ctxh_san_pham (san_pham_id),
  CONSTRAINT ck_ctxh_so_luong CHECK (so_luong > 0),
  CONSTRAINT ck_ctxh_khac CHECK (ly_do <> 'KHAC' OR ghi_chu_ly_do IS NOT NULL),
  CONSTRAINT fk_ctxh_phieu FOREIGN KEY (phieu_xuat_huy_id) REFERENCES phieu_xuat_huy (id),
  CONSTRAINT fk_ctxh_san_pham FOREIGN KEY (san_pham_id) REFERENCES san_pham (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------
-- 5. KHÁCH HÀNG, TÍCH ĐIỂM, KHUYẾN MÃI (#5)
-- ---------------------------------------------------------------------

CREATE TABLE khach_hang (
  id                INT UNSIGNED NOT NULL AUTO_INCREMENT,
  ho_ten            VARCHAR(100) NULL,
  so_dien_thoai     VARCHAR(10)  NULL,
  email             VARCHAR(100) NULL,
  ngay_sinh         DATE         NULL,
  gioi_tinh         ENUM('NAM','NU','KHAC') NULL,
  diem_hien_co      INT UNSIGNED NOT NULL DEFAULT 0,
  trang_thai        ENUM('HOAT_DONG','DA_AN') NOT NULL DEFAULT 'HOAT_DONG',
  xoa_du_lieu_luc   DATETIME     NULL,
  nguoi_tao_id      INT UNSIGNED NOT NULL,
  phien_ban         INT UNSIGNED NOT NULL DEFAULT 1,
  ngay_tao          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ngay_cap_nhat     DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_khach_hang_sdt (so_dien_thoai),
  KEY idx_khach_hang_ten (ho_ten),
  CONSTRAINT ck_kh_sdt CHECK (so_dien_thoai IS NULL OR REGEXP_LIKE(so_dien_thoai, '^0[0-9]{9}$')),
  CONSTRAINT ck_kh_hoat_dong CHECK (trang_thai <> 'HOAT_DONG' OR (ho_ten IS NOT NULL AND so_dien_thoai IS NOT NULL)),
  CONSTRAINT fk_kh_nguoi_tao FOREIGN KEY (nguoi_tao_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- Quy tắc tích điểm (KH-07). Quy tắc đang áp dụng = bản ghi có hieu_luc_tu
-- gần nhất không sau hiện tại. Không sửa, đổi quy tắc là thêm dòng mới.
CREATE TABLE quy_tac_tich_diem (
  id                INT UNSIGNED NOT NULL AUTO_INCREMENT,
  so_tien_mot_diem  INT UNSIGNED NOT NULL DEFAULT 10000,
  dang_bat          BOOLEAN      NOT NULL DEFAULT TRUE,
  hieu_luc_tu       DATETIME     NOT NULL,
  nguoi_tao_id      INT UNSIGNED NULL,
  ngay_tao          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_qttd_hieu_luc (hieu_luc_tu),
  CONSTRAINT ck_qttd_so_tien CHECK (so_tien_mot_diem > 0),
  CONSTRAINT fk_qttd_nguoi_tao FOREIGN KEY (nguoi_tao_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE chuong_trinh_khuyen_mai (
  id              INT UNSIGNED NOT NULL AUTO_INCREMENT,
  ma              VARCHAR(20)  NOT NULL,
  ten             VARCHAR(200) NOT NULL,
  mo_ta           VARCHAR(500) NULL,
  loai_giam       ENUM('PHAN_TRAM','SO_TIEN') NOT NULL,
  gia_tri         INT UNSIGNED NOT NULL,
  giam_toi_da     INT UNSIGNED NULL,
  bat_dau_luc     DATETIME     NOT NULL,
  ket_thuc_luc    DATETIME     NOT NULL,
  chi_thanh_vien  BOOLEAN      NOT NULL DEFAULT FALSE,
  trang_thai      ENUM('NHAP','DA_KICH_HOAT','TAM_DUNG','DA_KET_THUC') NOT NULL DEFAULT 'NHAP',
  nguoi_tao_id    INT UNSIGNED NOT NULL,
  phien_ban       INT UNSIGNED NOT NULL DEFAULT 1,
  ngay_tao        DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ngay_cap_nhat   DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  UNIQUE KEY uq_ctkm_ma (ma),
  KEY idx_ctkm_tt (trang_thai, bat_dau_luc, ket_thuc_luc),
  CONSTRAINT ck_ctkm_thoi_gian CHECK (ket_thuc_luc > bat_dau_luc),
  CONSTRAINT ck_ctkm_gia_tri CHECK (
       (loai_giam = 'PHAN_TRAM' AND gia_tri BETWEEN 1 AND 100
          AND (giam_toi_da IS NULL OR (giam_toi_da > 0 AND giam_toi_da % 100 = 0)))
    OR (loai_giam = 'SO_TIEN' AND gia_tri > 0 AND gia_tri % 100 = 0 AND giam_toi_da IS NULL)),
  CONSTRAINT fk_ctkm_nguoi_tao FOREIGN KEY (nguoi_tao_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE khuyen_mai_san_pham (
  chuong_trinh_id  INT UNSIGNED NOT NULL,
  san_pham_id      INT UNSIGNED NOT NULL,
  PRIMARY KEY (chuong_trinh_id, san_pham_id),
  KEY idx_kmsp_san_pham (san_pham_id),
  CONSTRAINT fk_kmsp_ct FOREIGN KEY (chuong_trinh_id) REFERENCES chuong_trinh_khuyen_mai (id),
  CONSTRAINT fk_kmsp_sp FOREIGN KEY (san_pham_id) REFERENCES san_pham (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------
-- 6. BÁN HÀNG VÀ HÓA ĐƠN (#3)
-- ---------------------------------------------------------------------

-- Giỏ hàng = đơn chưa thanh toán (lưu ở máy chủ, /carts).
CREATE TABLE gio_hang (
  id                     INT UNSIGNED NOT NULL AUTO_INCREMENT,
  thu_ngan_id            INT UNSIGNED NOT NULL,
  khach_hang_id          INT UNSIGNED NULL,
  trang_thai             ENUM('DANG_BAN','TAM_GIU','DA_HOAN_TAT','DA_HUY') NOT NULL DEFAULT 'DANG_BAN',
  ghi_chu_tam_giu        VARCHAR(100) NULL,
  tam_giu_luc            DATETIME     NULL,
  hoan_tat_luc           DATETIME     NULL,
  huy_luc                DATETIME     NULL,
  ly_do_huy              ENUM('THU_NGAN_HUY','HET_HAN_TAM_GIU') NULL,
  gia_tri_khi_huy        BIGINT UNSIGNED NULL,
  phien_ban              INT UNSIGNED NOT NULL DEFAULT 1,
  ngay_tao               DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  ngay_cap_nhat          DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
  -- BR-POS-03: mỗi thu ngân chỉ có một đơn DANG_BAN
  dang_ban_cua_thu_ngan  INT UNSIGNED AS (IF(trang_thai = 'DANG_BAN', thu_ngan_id, NULL)) STORED,
  PRIMARY KEY (id),
  UNIQUE KEY uq_gio_dang_ban (dang_ban_cua_thu_ngan),
  KEY idx_gio_thu_ngan (thu_ngan_id, trang_thai),
  KEY idx_gio_huy (trang_thai, huy_luc),
  KEY idx_gio_khach (khach_hang_id),
  CONSTRAINT ck_gio_huy CHECK (trang_thai <> 'DA_HUY' OR (huy_luc IS NOT NULL AND ly_do_huy IS NOT NULL AND gia_tri_khi_huy IS NOT NULL)),
  CONSTRAINT ck_gio_tam_giu CHECK (trang_thai <> 'TAM_GIU' OR tam_giu_luc IS NOT NULL),
  CONSTRAINT fk_gio_thu_ngan FOREIGN KEY (thu_ngan_id) REFERENCES tai_khoan (id),
  CONSTRAINT fk_gio_khach FOREIGN KEY (khach_hang_id) REFERENCES khach_hang (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ct_gio_hang (
  gio_hang_id      INT UNSIGNED NOT NULL,
  san_pham_id      INT UNSIGNED NOT NULL,
  so_luong         INT UNSIGNED NOT NULL,
  don_gia          INT UNSIGNED NOT NULL,
  khuyen_mai_id    INT UNSIGNED NULL,
  giam_don_vi      INT UNSIGNED NOT NULL DEFAULT 0,
  them_luc         DATETIME     NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (gio_hang_id, san_pham_id),
  KEY idx_ctgh_san_pham (san_pham_id),
  CONSTRAINT ck_ctgh_so_luong CHECK (so_luong > 0),
  CONSTRAINT ck_ctgh_giam CHECK (giam_don_vi <= don_gia AND giam_don_vi % 100 = 0),
  CONSTRAINT ck_ctgh_km CHECK (khuyen_mai_id IS NOT NULL OR giam_don_vi = 0),
  CONSTRAINT fk_ctgh_gio FOREIGN KEY (gio_hang_id) REFERENCES gio_hang (id),
  CONSTRAINT fk_ctgh_san_pham FOREIGN KEY (san_pham_id) REFERENCES san_pham (id),
  CONSTRAINT fk_ctgh_km FOREIGN KEY (khuyen_mai_id) REFERENCES chuong_trinh_khuyen_mai (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE hoa_don (
  id                  INT UNSIGNED    NOT NULL AUTO_INCREMENT,
  ma_hoa_don          VARCHAR(20)     NOT NULL,
  gio_hang_id         INT UNSIGNED    NOT NULL,
  thu_ngan_id         INT UNSIGNED    NOT NULL,
  khach_hang_id       INT UNSIGNED    NULL,
  thanh_toan_luc      DATETIME        NOT NULL,
  tong_tien_hang      BIGINT UNSIGNED NOT NULL,
  tong_giam           BIGINT UNSIGNED NOT NULL DEFAULT 0,
  tong_thanh_toan     BIGINT UNSIGNED NOT NULL,
  tien_tru_hang_doi   BIGINT UNSIGNED NOT NULL DEFAULT 0,
  tien_khach_dua      BIGINT UNSIGNED NOT NULL,
  tien_thua           BIGINT UNSIGNED NOT NULL,
  so_tien_mot_diem    INT UNSIGNED    NULL,
  diem_cong           INT UNSIGNED    NOT NULL DEFAULT 0,
  trang_thai          ENUM('DA_THANH_TOAN','DA_HUY') NOT NULL DEFAULT 'DA_THANH_TOAN',
  nguoi_huy_id        INT UNSIGNED    NULL,
  huy_luc             DATETIME        NULL,
  ly_do_huy           VARCHAR(255)    NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_hoa_don_ma (ma_hoa_don),
  UNIQUE KEY uq_hoa_don_gio (gio_hang_id),
  KEY idx_hoa_don_tt_tg (trang_thai, thanh_toan_luc),
  KEY idx_hoa_don_thu_ngan (thu_ngan_id, thanh_toan_luc),
  KEY idx_hoa_don_khach (khach_hang_id, thanh_toan_luc),
  KEY idx_hoa_don_huy (huy_luc),
  CONSTRAINT ck_hd_tong CHECK (tong_thanh_toan + tong_giam = tong_tien_hang),
  CONSTRAINT ck_hd_tien_thua CHECK (tien_khach_dua + tien_tru_hang_doi = tong_thanh_toan + tien_thua),
  CONSTRAINT ck_hd_tru_doi CHECK (tien_tru_hang_doi <= tong_thanh_toan),
  CONSTRAINT ck_hd_diem CHECK (khach_hang_id IS NOT NULL OR (diem_cong = 0 AND so_tien_mot_diem IS NULL)),
  CONSTRAINT ck_hd_huy CHECK (trang_thai <> 'DA_HUY' OR (nguoi_huy_id IS NOT NULL AND huy_luc IS NOT NULL AND ly_do_huy IS NOT NULL)),
  CONSTRAINT fk_hd_gio FOREIGN KEY (gio_hang_id) REFERENCES gio_hang (id),
  CONSTRAINT fk_hd_thu_ngan FOREIGN KEY (thu_ngan_id) REFERENCES tai_khoan (id),
  CONSTRAINT fk_hd_khach FOREIGN KEY (khach_hang_id) REFERENCES khach_hang (id),
  CONSTRAINT fk_hd_nguoi_huy FOREIGN KEY (nguoi_huy_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ct_hoa_don (
  hoa_don_id     INT UNSIGNED    NOT NULL,
  san_pham_id    INT UNSIGNED    NOT NULL,
  ten_san_pham   VARCHAR(200)    NOT NULL,
  don_vi_tinh    VARCHAR(20)     NOT NULL,
  so_luong       INT UNSIGNED    NOT NULL,
  don_gia        INT UNSIGNED    NOT NULL,
  khuyen_mai_id  INT UNSIGNED    NULL,
  giam_don_vi    INT UNSIGNED    NOT NULL DEFAULT 0,
  gia_von        DECIMAL(15,2)   NOT NULL,
  thanh_tien     BIGINT UNSIGNED AS (so_luong * (don_gia - giam_don_vi)) STORED,
  PRIMARY KEY (hoa_don_id, san_pham_id),
  KEY idx_cthd_san_pham (san_pham_id),
  KEY idx_cthd_km (khuyen_mai_id),
  CONSTRAINT ck_cthd_so_luong CHECK (so_luong > 0),
  CONSTRAINT ck_cthd_giam CHECK (giam_don_vi <= don_gia AND giam_don_vi % 100 = 0),
  CONSTRAINT ck_cthd_km CHECK (khuyen_mai_id IS NOT NULL OR giam_don_vi = 0),
  CONSTRAINT fk_cthd_hd FOREIGN KEY (hoa_don_id) REFERENCES hoa_don (id),
  CONSTRAINT fk_cthd_san_pham FOREIGN KEY (san_pham_id) REFERENCES san_pham (id),
  CONSTRAINT fk_cthd_km FOREIGN KEY (khuyen_mai_id) REFERENCES chuong_trinh_khuyen_mai (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------
-- 7. ĐỔI TRẢ (#3; DT-08 do #6 lập trình)
-- ---------------------------------------------------------------------

CREATE TABLE chinh_sach_doi_tra (
  id                     INT UNSIGNED    NOT NULL AUTO_INCREMENT,
  so_ngay_doi_tra        SMALLINT UNSIGNED NOT NULL DEFAULT 7,
  nguong_tien_can_duyet  BIGINT UNSIGNED NOT NULL DEFAULT 500000,
  hieu_luc_tu            DATETIME        NOT NULL,
  nguoi_tao_id           INT UNSIGNED    NULL,
  ngay_tao               DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_csdt_hieu_luc (hieu_luc_tu),
  CONSTRAINT fk_csdt_nguoi_tao FOREIGN KEY (nguoi_tao_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE phieu_doi_tra (
  id                     INT UNSIGNED    NOT NULL AUTO_INCREMENT,
  ma_phieu               VARCHAR(20)     NOT NULL,
  loai                   ENUM('TRA_HANG','DOI_HANG') NOT NULL,
  hoa_don_goc_id         INT UNSIGNED    NOT NULL,
  hoa_don_moi_id         INT UNSIGNED    NULL,
  khach_hang_id          INT UNSIGNED    NULL,
  chinh_sach_id          INT UNSIGNED    NOT NULL,
  trang_thai             ENUM('CHO_DUYET','DA_DUYET','HOAN_TAT','BI_TU_CHOI') NOT NULL,
  can_duyet_qua_han      BOOLEAN         NOT NULL DEFAULT FALSE,
  can_duyet_danh_muc     BOOLEAN         NOT NULL DEFAULT FALSE,
  can_duyet_vuot_nguong  BOOLEAN         NOT NULL DEFAULT FALSE,
  gia_tri_hang_tra       BIGINT UNSIGNED NOT NULL,
  tien_hoan              BIGINT UNSIGNED NOT NULL DEFAULT 0,
  tien_thu_them          BIGINT UNSIGNED NOT NULL DEFAULT 0,
  diem_thu_hoi           INT UNSIGNED    NOT NULL DEFAULT 0,
  thu_ngan_id            INT UNSIGNED    NOT NULL,
  ngay_lap               DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  nguoi_duyet_id         INT UNSIGNED    NULL,
  duyet_luc              DATETIME        NULL,
  ghi_chu_duyet          VARCHAR(255)    NULL,
  hoan_tat_luc           DATETIME        NULL,
  PRIMARY KEY (id),
  UNIQUE KEY uq_pdt_ma (ma_phieu),
  UNIQUE KEY uq_pdt_hd_moi (hoa_don_moi_id),
  UNIQUE KEY uq_pdt_id_hd_goc (id, hoa_don_goc_id),
  KEY idx_pdt_hd_goc (hoa_don_goc_id),
  KEY idx_pdt_tt (trang_thai, hoan_tat_luc),
  KEY idx_pdt_thu_ngan (thu_ngan_id, ngay_lap),
  CONSTRAINT ck_pdt_tra CHECK (loai <> 'TRA_HANG' OR (hoa_don_moi_id IS NULL AND tien_thu_them = 0 AND tien_hoan = gia_tri_hang_tra)),
  CONSTRAINT ck_pdt_doi CHECK (loai <> 'DOI_HANG' OR (tien_hoan = 0 AND (trang_thai <> 'HOAN_TAT' OR hoa_don_moi_id IS NOT NULL))),
  CONSTRAINT ck_pdt_hoan_tat CHECK (trang_thai <> 'HOAN_TAT' OR hoan_tat_luc IS NOT NULL),
  -- Phiếu không có lý do cần duyệt thì hoàn tất ngay, không có người duyệt
  CONSTRAINT ck_pdt_khong_can_duyet CHECK (
       can_duyet_qua_han OR can_duyet_danh_muc OR can_duyet_vuot_nguong
    OR (trang_thai = 'HOAN_TAT' AND nguoi_duyet_id IS NULL)),
  -- Phiếu cần duyệt: rời CHO_DUYET thì phải có người duyệt và thời điểm
  CONSTRAINT ck_pdt_can_duyet CHECK (
       NOT (can_duyet_qua_han OR can_duyet_danh_muc OR can_duyet_vuot_nguong)
    OR trang_thai = 'CHO_DUYET'
    OR (nguoi_duyet_id IS NOT NULL AND duyet_luc IS NOT NULL)),
  -- DA_DUYET chỉ dùng cho đổi hàng (chờ thu ngân thêm hàng mới và hoàn tất)
  CONSTRAINT ck_pdt_da_duyet CHECK (trang_thai <> 'DA_DUYET' OR loai = 'DOI_HANG'),
  CONSTRAINT fk_pdt_hd_goc FOREIGN KEY (hoa_don_goc_id) REFERENCES hoa_don (id),
  CONSTRAINT fk_pdt_hd_moi FOREIGN KEY (hoa_don_moi_id) REFERENCES hoa_don (id),
  CONSTRAINT fk_pdt_khach FOREIGN KEY (khach_hang_id) REFERENCES khach_hang (id),
  CONSTRAINT fk_pdt_cs FOREIGN KEY (chinh_sach_id) REFERENCES chinh_sach_doi_tra (id),
  CONSTRAINT fk_pdt_thu_ngan FOREIGN KEY (thu_ngan_id) REFERENCES tai_khoan (id),
  CONSTRAINT fk_pdt_nguoi_duyet FOREIGN KEY (nguoi_duyet_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

CREATE TABLE ct_phieu_doi_tra (
  phieu_doi_tra_id   INT UNSIGNED    NOT NULL,
  san_pham_id        INT UNSIGNED    NOT NULL,
  hoa_don_id         INT UNSIGNED    NOT NULL,
  so_luong           INT UNSIGNED    NOT NULL,
  don_gia_thuc_tra   INT UNSIGNED    NOT NULL,
  ly_do              ENUM('LOI_SAN_PHAM','HET_HAN','KHONG_DUNG_NHU_CAU','KHAC') NOT NULL,
  ghi_chu_ly_do      VARCHAR(255)    NULL,
  tinh_trang         VARCHAR(255)    NULL,
  cach_xu_ly         ENUM('NHAP_LAI','HUY') NOT NULL,
  ngoai_le           BOOLEAN         NOT NULL DEFAULT FALSE,
  gia_von            DECIMAL(15,2)   NOT NULL,
  thanh_tien         BIGINT UNSIGNED AS (so_luong * don_gia_thuc_tra) STORED,
  gia_tri_huy        DECIMAL(17,2)   AS (IF(cach_xu_ly = 'HUY', so_luong * gia_von, 0)) STORED,
  PRIMARY KEY (phieu_doi_tra_id, san_pham_id),
  KEY idx_ctdt_dong_hd (hoa_don_id, san_pham_id),
  KEY idx_ctdt_san_pham (san_pham_id),
  CONSTRAINT ck_ctdt_so_luong CHECK (so_luong > 0),
  CONSTRAINT ck_ctdt_khac CHECK (ly_do <> 'KHAC' OR ghi_chu_ly_do IS NOT NULL),
  CONSTRAINT fk_ctdt_phieu FOREIGN KEY (phieu_doi_tra_id) REFERENCES phieu_doi_tra (id),
  -- dòng trả phải thuộc đúng hóa đơn gốc của phiếu
  CONSTRAINT fk_ctdt_phieu_hd_goc FOREIGN KEY (phieu_doi_tra_id, hoa_don_id) REFERENCES phieu_doi_tra (id, hoa_don_goc_id),
  CONSTRAINT fk_ctdt_dong_hd FOREIGN KEY (hoa_don_id, san_pham_id) REFERENCES ct_hoa_don (hoa_don_id, san_pham_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------
-- 8. LỊCH SỬ ĐIỂM (#5) - đặt sau hóa đơn, phiếu đổi trả vì có khóa ngoại tới hai bảng này
-- ---------------------------------------------------------------------

CREATE TABLE lich_su_diem (
  id                  BIGINT UNSIGNED NOT NULL AUTO_INCREMENT,
  khach_hang_id       INT UNSIGNED    NOT NULL,
  loai                ENUM('TICH_DIEM','THU_HOI_TRA_HANG','THU_HOI_HUY_HOA_DON','DIEU_CHINH') NOT NULL,
  so_diem             INT             NOT NULL,
  diem_sau            INT UNSIGNED    NOT NULL,
  hoa_don_id          INT UNSIGNED    NULL,
  phieu_doi_tra_id    INT UNSIGNED    NULL,
  ly_do               VARCHAR(255)    NULL,
  nguoi_thuc_hien_id  INT UNSIGNED    NOT NULL,
  thoi_diem           DATETIME        NOT NULL DEFAULT CURRENT_TIMESTAMP,
  PRIMARY KEY (id),
  KEY idx_lsd_khach_tg (khach_hang_id, thoi_diem),
  KEY idx_lsd_hoa_don (hoa_don_id),
  CONSTRAINT ck_lsd_khac_0 CHECK (so_diem <> 0),
  CONSTRAINT ck_lsd_huong CHECK (
       (loai = 'TICH_DIEM' AND so_diem > 0 AND hoa_don_id IS NOT NULL)
    OR (loai = 'THU_HOI_TRA_HANG' AND so_diem < 0 AND phieu_doi_tra_id IS NOT NULL)
    OR (loai = 'THU_HOI_HUY_HOA_DON' AND so_diem < 0 AND hoa_don_id IS NOT NULL)
    OR (loai = 'DIEU_CHINH' AND ly_do IS NOT NULL)),
  CONSTRAINT fk_lsd_khach FOREIGN KEY (khach_hang_id) REFERENCES khach_hang (id),
  CONSTRAINT fk_lsd_hd FOREIGN KEY (hoa_don_id) REFERENCES hoa_don (id),
  CONSTRAINT fk_lsd_pdt FOREIGN KEY (phieu_doi_tra_id) REFERENCES phieu_doi_tra (id),
  CONSTRAINT fk_lsd_nguoi FOREIGN KEY (nguoi_thuc_hien_id) REFERENCES tai_khoan (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;

-- ---------------------------------------------------------------------
-- 9a. VIEW (chỉ đọc). NOW() dùng múi giờ của phiên: kết nối phải đặt
--     time_zone = '+07:00' (xem tài liệu ERD toàn hệ thống, mục quy ước).
-- ---------------------------------------------------------------------

-- Giá bán đang hiệu lực của từng sản phẩm (BR-SP-11)
CREATE VIEW v_gia_ban_hien_hanh AS
SELECT san_pham_id, id AS lich_su_gia_id, gia_ban, hieu_luc_tu
FROM (
  SELECT l.*, ROW_NUMBER() OVER (PARTITION BY l.san_pham_id ORDER BY l.hieu_luc_tu DESC, l.id DESC) AS thu_tu
  FROM lich_su_gia l
  WHERE l.huy_luc IS NULL AND l.hieu_luc_tu <= NOW()
) x
WHERE x.thu_tu = 1;

-- Trạng thái hiển thị của chương trình khuyến mãi (BR-KM-07, PA-KM-05.A)
CREATE VIEW v_chuong_trinh_khuyen_mai AS
SELECT c.*,
  CASE
    WHEN c.trang_thai = 'NHAP' THEN 'NHAP'
    WHEN c.trang_thai = 'DA_KET_THUC' OR NOW() >= c.ket_thuc_luc THEN 'DA_KET_THUC'
    WHEN c.trang_thai = 'TAM_DUNG' THEN 'TAM_DUNG'
    WHEN NOW() < c.bat_dau_luc THEN 'DA_LEN_LICH'
    ELSE 'DANG_CHAY'
  END AS trang_thai_hien_thi
FROM chuong_trinh_khuyen_mai c;

-- Lượt dùng khuyến mãi (KM-06, BC-11): mỗi cặp (hóa đơn, chương trình) là
-- một lượt; hóa đơn đã hủy không tính (BR-KM-06)
CREATE VIEW v_luot_dung_khuyen_mai AS
SELECT d.khuyen_mai_id AS chuong_trinh_id,
       h.id AS hoa_don_id, h.ma_hoa_don, h.thanh_toan_luc, h.thu_ngan_id, h.khach_hang_id,
       SUM(d.so_luong) AS so_luong_san_pham,
       SUM(d.so_luong * d.giam_don_vi) AS tien_giam
FROM ct_hoa_don d
JOIN hoa_don h ON h.id = d.hoa_don_id
WHERE d.khuyen_mai_id IS NOT NULL AND h.trang_thai = 'DA_THANH_TOAN'
GROUP BY d.khuyen_mai_id, h.id, h.ma_hoa_don, h.thanh_toan_luc, h.thu_ngan_id, h.khach_hang_id;

-- ---------------------------------------------------------------------
-- 9b. TRIGGER bảo vệ dữ liệu không được sửa, xóa
-- ---------------------------------------------------------------------

-- BR-KHO-03: thẻ kho chỉ được thêm
CREATE TRIGGER trg_the_kho_khong_sua BEFORE UPDATE ON the_kho FOR EACH ROW
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-KHO-03: the_kho khong duoc sua';
CREATE TRIGGER trg_the_kho_khong_xoa BEFORE DELETE ON the_kho FOR EACH ROW
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-KHO-03: the_kho khong duoc xoa';

-- BR-KH-02: lịch sử điểm chỉ được thêm
CREATE TRIGGER trg_lsd_khong_sua BEFORE UPDATE ON lich_su_diem FOR EACH ROW
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-KH-02: lich_su_diem khong duoc sua';
CREATE TRIGGER trg_lsd_khong_xoa BEFORE DELETE ON lich_su_diem FOR EACH ROW
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-KH-02: lich_su_diem khong duoc xoa';

-- BR-POS-12: hóa đơn không bị xóa; BR-POS-07: chỉ được chuyển DA_THANH_TOAN -> DA_HUY
CREATE TRIGGER trg_hoa_don_khong_xoa BEFORE DELETE ON hoa_don FOR EACH ROW
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-POS-12: hoa_don khong duoc xoa';
DELIMITER //
CREATE TRIGGER trg_hoa_don_chi_huy BEFORE UPDATE ON hoa_don FOR EACH ROW
BEGIN
  IF OLD.trang_thai <> 'DA_THANH_TOAN' OR NEW.trang_thai <> 'DA_HUY'
     OR NOT (NEW.ma_hoa_don <=> OLD.ma_hoa_don) OR NOT (NEW.thu_ngan_id <=> OLD.thu_ngan_id)
     OR NOT (NEW.khach_hang_id <=> OLD.khach_hang_id) OR NOT (NEW.thanh_toan_luc <=> OLD.thanh_toan_luc)
     OR NEW.tong_tien_hang <> OLD.tong_tien_hang OR NEW.tong_giam <> OLD.tong_giam
     OR NEW.tong_thanh_toan <> OLD.tong_thanh_toan OR NEW.tien_khach_dua <> OLD.tien_khach_dua
     OR NEW.tien_tru_hang_doi <> OLD.tien_tru_hang_doi OR NEW.tien_thua <> OLD.tien_thua
     OR NEW.diem_cong <> OLD.diem_cong THEN
    SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-POS-07: hoa_don chi duoc chuyen sang DA_HUY';
  END IF;
END//
DELIMITER ;

-- BR-POS-07, BR-POS-08: dòng hóa đơn không sửa, không xóa
CREATE TRIGGER trg_ct_hoa_don_khong_sua BEFORE UPDATE ON ct_hoa_don FOR EACH ROW
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-POS-07: ct_hoa_don khong duoc sua';
CREATE TRIGGER trg_ct_hoa_don_khong_xoa BEFORE DELETE ON ct_hoa_don FOR EACH ROW
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-POS-07: ct_hoa_don khong duoc xoa';

-- BR-DT-03: dòng phiếu đổi trả không sửa, không xóa
CREATE TRIGGER trg_ct_doi_tra_khong_sua BEFORE UPDATE ON ct_phieu_doi_tra FOR EACH ROW
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-DT-03: ct_phieu_doi_tra khong duoc sua';
CREATE TRIGGER trg_ct_doi_tra_khong_xoa BEFORE DELETE ON ct_phieu_doi_tra FOR EACH ROW
  SIGNAL SQLSTATE '45000' SET MESSAGE_TEXT = 'BR-DT-03: ct_phieu_doi_tra khong duoc xoa';

-- ---------------------------------------------------------------------
-- 9c. DỮ LIỆU KHỞI TẠO
--     Tài khoản Admin đầu tiên do chương trình khởi tạo (seeder) tạo, vì
--     mật khẩu phải băm bằng PasswordHasher của ASP.NET Core.
-- ---------------------------------------------------------------------

INSERT INTO vai_tro (id, ma, ten) VALUES
  (1, 'AD', 'Admin/Quản lý'), (2, 'TN', 'Thu ngân'), (3, 'NK', 'Nhân viên kho');

INSERT INTO quy_tac_tich_diem (so_tien_mot_diem, dang_bat, hieu_luc_tu, nguoi_tao_id)
VALUES (10000, TRUE, '2026-01-01 00:00:00', NULL);

INSERT INTO chinh_sach_doi_tra (so_ngay_doi_tra, nguong_tien_can_duyet, hieu_luc_tu, nguoi_tao_id)
VALUES (7, 500000, '2026-01-01 00:00:00', NULL);
