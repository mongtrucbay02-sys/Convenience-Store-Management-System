using ConvenienceStore.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Data;

public partial class CuaHangDbContext
{
    private static void ConfigurePostgreSqlModel(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BoDemChungTu>(entity =>
        {
            entity.HasKey(e => new { e.TienTo, e.Ky }).HasName("bo_dem_chung_tu_pkey");

            entity.ToTable("bo_dem_chung_tu", "smartmart");

            entity.Property(e => e.TienTo)
                .HasMaxLength(3)
                .HasColumnName("tien_to");
            entity.Property(e => e.Ky)
                .HasMaxLength(8)
                .HasDefaultValueSql("''::bpchar")
                .IsFixedLength()
                .HasColumnName("ky");
            entity.Property(e => e.SoCuoi).HasColumnName("so_cuoi");
        });

        modelBuilder.Entity<ChinhSachDoiTra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("chinh_sach_doi_tra_pkey");

            entity.ToTable("chinh_sach_doi_tra", "smartmart");

            entity.HasIndex(e => e.HieuLucTu, "idx_csdt_hieu_luc");

            entity.HasIndex(e => e.NguoiTaoId, "ix_chinh_sach_doi_tra_nguoi_tao_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HieuLucTu).HasColumnName("hieu_luc_tu");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.NguongTienCanDuyet)
                .HasDefaultValue(500000L)
                .HasColumnName("nguong_tien_can_duyet");
            entity.Property(e => e.SoNgayDoiTra)
                .HasDefaultValue((short)7)
                .HasColumnName("so_ngay_doi_tra");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.ChinhSachDoiTras)
                .HasForeignKey(d => d.NguoiTaoId)
                .HasConstraintName("fk_csdt_nguoi_tao");
        });

        modelBuilder.Entity<ChuongTrinhKhuyenMai>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("chuong_trinh_khuyen_mai_pkey");

            entity.ToTable("chuong_trinh_khuyen_mai", "smartmart");

            entity.HasIndex(e => new { e.TrangThai, e.BatDauLuc, e.KetThucLuc }, "idx_ctkm_tt");

            entity.HasIndex(e => e.NguoiTaoId, "ix_chuong_trinh_khuyen_mai_nguoi_tao_id");

            entity.HasIndex(e => e.Ma, "uq_ctkm_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AnhUrl)
                .HasMaxLength(2048)
                .HasColumnName("anh_url");
            entity.Property(e => e.BatDauLuc).HasColumnName("bat_dau_luc");
            entity.Property(e => e.ChiThanhVien).HasColumnName("chi_thanh_vien");
            entity.Property(e => e.GiaTri).HasColumnName("gia_tri");
            entity.Property(e => e.GiamToiDa).HasColumnName("giam_toi_da");
            entity.Property(e => e.KetThucLuc).HasColumnName("ket_thuc_luc");
            entity.Property(e => e.LoaiGiam)
                .HasMaxLength(40)
                .HasColumnName("loai_giam");
            entity.Property(e => e.Ma)
                .HasMaxLength(20)
                .HasColumnName("ma");
            entity.Property(e => e.MoTa)
                .HasMaxLength(500)
                .HasColumnName("mo_ta");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.PhienBan).IsConcurrencyToken()
                .HasDefaultValue(1L)
                .HasColumnName("phien_ban");
            entity.Property(e => e.Ten)
                .HasMaxLength(200)
                .HasColumnName("ten");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'NHAP'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.ChuongTrinhKhuyenMais)
                .HasForeignKey(d => d.NguoiTaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctkm_nguoi_tao");

            entity.HasMany(d => d.SanPhams).WithMany(p => p.ChuongTrinhs)
                .UsingEntity<Dictionary<string, object>>(
                    "KhuyenMaiSanPham",
                    r => r.HasOne<SanPham>().WithMany()
                        .HasForeignKey("SanPhamId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_kmsp_sp"),
                    l => l.HasOne<ChuongTrinhKhuyenMai>().WithMany()
                        .HasForeignKey("ChuongTrinhId")
                        .OnDelete(DeleteBehavior.ClientSetNull)
                        .HasConstraintName("fk_kmsp_ct"),
                    j =>
                    {
                        j.HasKey("ChuongTrinhId", "SanPhamId").HasName("khuyen_mai_san_pham_pkey");
                        j.ToTable("khuyen_mai_san_pham", "smartmart");
                        j.HasIndex(new[] { "SanPhamId" }, "idx_kmsp_san_pham");
                        j.IndexerProperty<long>("ChuongTrinhId").HasColumnName("chuong_trinh_id");
                        j.IndexerProperty<long>("SanPhamId").HasColumnName("san_pham_id");
                    });
        });

        modelBuilder.Entity<CtGioHang>(entity =>
        {
            entity.HasKey(e => new { e.GioHangId, e.SanPhamId }).HasName("ct_gio_hang_pkey");

            entity.ToTable("ct_gio_hang", "smartmart");

            entity.HasIndex(e => e.SanPhamId, "idx_ctgh_san_pham");

            entity.HasIndex(e => e.KhuyenMaiId, "ix_ct_gio_hang_khuyen_mai_id");

            entity.Property(e => e.GioHangId).HasColumnName("gio_hang_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.DonGia).HasColumnName("don_gia");
            entity.Property(e => e.GiamDonVi).HasColumnName("giam_don_vi");
            entity.Property(e => e.KhuyenMaiId).HasColumnName("khuyen_mai_id");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.ThemLuc)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("them_luc");

            entity.HasOne(d => d.GioHang).WithMany(p => p.CtGioHangs)
                .HasForeignKey(d => d.GioHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctgh_gio");

            entity.HasOne(d => d.KhuyenMai).WithMany(p => p.CtGioHangs)
                .HasForeignKey(d => d.KhuyenMaiId)
                .HasConstraintName("fk_ctgh_km");

            entity.HasOne(d => d.SanPham).WithMany(p => p.CtGioHangs)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctgh_san_pham");
        });

        modelBuilder.Entity<CtHoaDon>(entity =>
        {
            entity.HasKey(e => new { e.HoaDonId, e.SanPhamId }).HasName("ct_hoa_don_pkey");

            entity.ToTable("ct_hoa_don", "smartmart");

            entity.HasIndex(e => e.KhuyenMaiId, "idx_cthd_km");

            entity.HasIndex(e => e.SanPhamId, "idx_cthd_san_pham");

            entity.Property(e => e.HoaDonId).HasColumnName("hoa_don_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.DonGia).HasColumnName("don_gia");
            entity.Property(e => e.DonViTinh)
                .HasMaxLength(20)
                .HasColumnName("don_vi_tinh");
            entity.Property(e => e.GiaVon)
                .HasPrecision(15, 2)
                .HasColumnName("gia_von");
            entity.Property(e => e.GiamDonVi).HasColumnName("giam_don_vi");
            entity.Property(e => e.KhuyenMaiId).HasColumnName("khuyen_mai_id");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(200)
                .HasColumnName("ten_san_pham");
            entity.Property(e => e.ThanhTien)
                .HasComputedColumnSql("(so_luong * (don_gia - giam_don_vi))", true)
                .HasColumnName("thanh_tien");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.CtHoaDons)
                .HasForeignKey(d => d.HoaDonId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cthd_hd");

            entity.HasOne(d => d.KhuyenMai).WithMany(p => p.CtHoaDons)
                .HasForeignKey(d => d.KhuyenMaiId)
                .HasConstraintName("fk_cthd_km");

            entity.HasOne(d => d.SanPham).WithMany(p => p.CtHoaDons)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_cthd_san_pham");
        });

        modelBuilder.Entity<CtKiemKe>(entity =>
        {
            entity.HasKey(e => new { e.PhieuKiemKeId, e.SanPhamId }).HasName("ct_kiem_ke_pkey");

            entity.ToTable("ct_kiem_ke", "smartmart");

            entity.HasIndex(e => e.SanPhamId, "idx_ctkk_san_pham");

            entity.Property(e => e.PhieuKiemKeId).HasColumnName("phieu_kiem_ke_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.ChenhLechLucDuyet).HasColumnName("chenh_lech_luc_duyet");
            entity.Property(e => e.GiaVonLucDuyet)
                .HasPrecision(15, 2)
                .HasColumnName("gia_von_luc_duyet");
            entity.Property(e => e.LyDoChenhLech)
                .HasMaxLength(255)
                .HasColumnName("ly_do_chenh_lech");
            entity.Property(e => e.SoLuongThucTe).HasColumnName("so_luong_thuc_te");
            entity.Property(e => e.TonLucLap).HasColumnName("ton_luc_lap");

            entity.HasOne(d => d.PhieuKiemKe).WithMany(p => p.CtKiemKes)
                .HasForeignKey(d => d.PhieuKiemKeId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctkk_phieu");

            entity.HasOne(d => d.SanPham).WithMany(p => p.CtKiemKes)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctkk_san_pham");
        });

        modelBuilder.Entity<CtPhieuDoiTra>(entity =>
        {
            entity.HasKey(e => new { e.PhieuDoiTraId, e.SanPhamId }).HasName("ct_phieu_doi_tra_pkey");

            entity.ToTable("ct_phieu_doi_tra", "smartmart");

            entity.HasIndex(e => new { e.HoaDonId, e.SanPhamId }, "idx_ctdt_dong_hd");

            entity.HasIndex(e => e.SanPhamId, "idx_ctdt_san_pham");

            entity.HasIndex(e => new { e.PhieuDoiTraId, e.HoaDonId }, "ix_ct_phieu_doi_tra_phieu_doi_tra_id_hoa_don_id");

            entity.Property(e => e.PhieuDoiTraId).HasColumnName("phieu_doi_tra_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.CachXuLy)
                .HasMaxLength(40)
                .HasColumnName("cach_xu_ly");
            entity.Property(e => e.DonGiaThucTra).HasColumnName("don_gia_thuc_tra");
            entity.Property(e => e.GhiChuLyDo)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu_ly_do");
            entity.Property(e => e.GiaTriHuy)
                .HasPrecision(17, 2)
                .HasComputedColumnSql("\nCASE\n    WHEN ((cach_xu_ly)::text = 'HUY'::text) THEN ((so_luong)::numeric * gia_von)\n    ELSE (0)::numeric\nEND", true)
                .HasColumnName("gia_tri_huy");
            entity.Property(e => e.GiaVon)
                .HasPrecision(15, 2)
                .HasColumnName("gia_von");
            entity.Property(e => e.HoaDonId).HasColumnName("hoa_don_id");
            entity.Property(e => e.LyDo)
                .HasMaxLength(40)
                .HasColumnName("ly_do");
            entity.Property(e => e.NgoaiLe).HasColumnName("ngoai_le");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.ThanhTien)
                .HasComputedColumnSql("(so_luong * don_gia_thuc_tra)", true)
                .HasColumnName("thanh_tien");
            entity.Property(e => e.TinhTrang)
                .HasMaxLength(255)
                .HasColumnName("tinh_trang");

            entity.HasOne(d => d.PhieuDoiTra).WithMany(p => p.CtPhieuDoiTraPhieuDoiTras)
                .HasForeignKey(d => d.PhieuDoiTraId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctdt_phieu");

            entity.HasOne(d => d.CtHoaDon).WithMany(p => p.CtPhieuDoiTras)
                .HasForeignKey(d => new { d.HoaDonId, d.SanPhamId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctdt_dong_hd");

            entity.HasOne(d => d.PhieuDoiTraNavigation).WithMany(p => p.CtPhieuDoiTraPhieuDoiTraNavigations)
                .HasPrincipalKey(p => new { p.Id, p.HoaDonGocId })
                .HasForeignKey(d => new { d.PhieuDoiTraId, d.HoaDonId })
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctdt_phieu_hd_goc");
        });

        modelBuilder.Entity<CtPhieuNhap>(entity =>
        {
            entity.HasKey(e => new { e.PhieuNhapId, e.SanPhamId }).HasName("ct_phieu_nhap_pkey");

            entity.ToTable("ct_phieu_nhap", "smartmart");

            entity.HasIndex(e => e.SanPhamId, "idx_ctpn_san_pham");

            entity.Property(e => e.PhieuNhapId).HasColumnName("phieu_nhap_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.DonViTinh)
                .HasMaxLength(20)
                .HasColumnName("don_vi_tinh");
            entity.Property(e => e.GiaNhap).HasColumnName("gia_nhap");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(200)
                .HasColumnName("ten_san_pham");
            entity.Property(e => e.ThanhTien)
                .HasComputedColumnSql("(so_luong * gia_nhap)", true)
                .HasColumnName("thanh_tien");

            entity.HasOne(d => d.PhieuNhap).WithMany(p => p.CtPhieuNhaps)
                .HasForeignKey(d => d.PhieuNhapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctpn_phieu");

            entity.HasOne(d => d.SanPham).WithMany(p => p.CtPhieuNhaps)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctpn_san_pham");
        });

        modelBuilder.Entity<CtXuatHuy>(entity =>
        {
            entity.HasKey(e => new { e.PhieuXuatHuyId, e.SanPhamId }).HasName("ct_xuat_huy_pkey");

            entity.ToTable("ct_xuat_huy", "smartmart");

            entity.HasIndex(e => e.SanPhamId, "idx_ctxh_san_pham");

            entity.Property(e => e.PhieuXuatHuyId).HasColumnName("phieu_xuat_huy_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.GhiChuLyDo)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu_ly_do");
            entity.Property(e => e.GiaVonLucDuyet)
                .HasPrecision(15, 2)
                .HasColumnName("gia_von_luc_duyet");
            entity.Property(e => e.LyDo)
                .HasMaxLength(40)
                .HasColumnName("ly_do");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");

            entity.HasOne(d => d.PhieuXuatHuy).WithMany(p => p.CtXuatHuys)
                .HasForeignKey(d => d.PhieuXuatHuyId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctxh_phieu");

            entity.HasOne(d => d.SanPham).WithMany(p => p.CtXuatHuys)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ctxh_san_pham");
        });

        modelBuilder.Entity<DanhMuc>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("danh_muc_pkey");

            entity.ToTable("danh_muc", "smartmart");

            entity.HasIndex(e => e.Ten, "uq_danh_muc_ten").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KhongDoiTra).HasColumnName("khong_doi_tra");
            entity.Property(e => e.MoTa)
                .HasMaxLength(255)
                .HasColumnName("mo_ta");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.PhienBan)
                .IsConcurrencyToken()
                .HasDefaultValue(1L)
                .HasColumnName("phien_ban");
            entity.Property(e => e.Ten)
                .HasMaxLength(100)
                .HasColumnName("ten");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'HOAT_DONG'::character varying")
                .HasColumnName("trang_thai");
        });

        modelBuilder.Entity<GiaoDichTienMat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("giao_dich_tien_mat_pkey");

            entity.ToTable("giao_dich_tien_mat", "smartmart");

            entity.HasIndex(e => e.HoaDonId, "ix_giao_dich_tien_mat_hoa_don_id");

            entity.HasIndex(e => e.NguoiThucHienId, "ix_giao_dich_tien_mat_nguoi_thuc_hien_id");

            entity.HasIndex(e => e.PhieuDoiTraId, "ix_giao_dich_tien_mat_phieu_doi_tra_id");

            entity.HasIndex(e => e.QuyTienMatId, "ix_giao_dich_tien_mat_quy_tien_mat_id");

            entity.HasIndex(e => new { e.Loai, e.HoaDonId }, "uq_gdtm_hd")
                .IsUnique()
                .HasFilter("(hoa_don_id IS NOT NULL)");

            entity.HasIndex(e => new { e.Loai, e.PhieuDoiTraId }, "uq_gdtm_phieu")
                .IsUnique()
                .HasFilter("(phieu_doi_tra_id IS NOT NULL)");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(500)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.HoaDonId).HasColumnName("hoa_don_id");
            entity.Property(e => e.Loai)
                .HasMaxLength(24)
                .HasColumnName("loai");
            entity.Property(e => e.NguoiThucHienId).HasColumnName("nguoi_thuc_hien_id");
            entity.Property(e => e.PhieuDoiTraId).HasColumnName("phieu_doi_tra_id");
            entity.Property(e => e.QuyTienMatId)
                .HasDefaultValue(1L)
                .HasColumnName("quy_tien_mat_id");
            entity.Property(e => e.SoDuSau).HasColumnName("so_du_sau");
            entity.Property(e => e.SoDuTruoc).HasColumnName("so_du_truoc");
            entity.Property(e => e.SoTien).HasColumnName("so_tien");
            entity.Property(e => e.ThoiDiem)
                .HasDefaultValueSql("now()")
                .HasColumnName("thoi_diem");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.GiaoDichTienMats)
                .HasForeignKey(d => d.HoaDonId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("giao_dich_tien_mat_hoa_don_id_fkey");

            entity.HasOne(d => d.NguoiThucHien).WithMany(p => p.GiaoDichTienMats)
                .HasForeignKey(d => d.NguoiThucHienId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("giao_dich_tien_mat_nguoi_thuc_hien_id_fkey");

            entity.HasOne(d => d.PhieuDoiTra).WithMany(p => p.GiaoDichTienMats)
                .HasForeignKey(d => d.PhieuDoiTraId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("giao_dich_tien_mat_phieu_doi_tra_id_fkey");

            entity.HasOne(d => d.QuyTienMat).WithMany(p => p.GiaoDichTienMats)
                .HasForeignKey(d => d.QuyTienMatId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("giao_dich_tien_mat_quy_tien_mat_id_fkey");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("gio_hang_pkey");

            entity.ToTable("gio_hang", "smartmart");

            entity.HasIndex(e => new { e.TrangThai, e.HuyLuc }, "idx_gio_huy");

            entity.HasIndex(e => e.KhachHangId, "idx_gio_khach");

            entity.HasIndex(e => new { e.ThuNganId, e.TrangThai }, "idx_gio_thu_ngan");

            entity.HasIndex(e => e.DangBanCuaThuNgan, "uq_gio_dang_ban").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DangBanCuaThuNgan)
                .HasComputedColumnSql("\nCASE\n    WHEN ((trang_thai)::text = 'DANG_BAN'::text) THEN thu_ngan_id\n    ELSE NULL::bigint\nEND", true)
                .HasColumnName("dang_ban_cua_thu_ngan");
            entity.Property(e => e.GhiChuTamGiu)
                .HasMaxLength(100)
                .HasColumnName("ghi_chu_tam_giu");
            entity.Property(e => e.GiaTriKhiHuy).HasColumnName("gia_tri_khi_huy");
            entity.Property(e => e.HoanTatLuc).HasColumnName("hoan_tat_luc");
            entity.Property(e => e.HuyLuc).HasColumnName("huy_luc");
            entity.Property(e => e.KhachHangId).HasColumnName("khach_hang_id");
            entity.Property(e => e.LyDoHuy)
                .HasMaxLength(40)
                .HasColumnName("ly_do_huy");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.PhienBan)
                .HasDefaultValue(1L)
                .HasColumnName("phien_ban");
            entity.Property(e => e.TamGiuLuc).HasColumnName("tam_giu_luc");
            entity.Property(e => e.ThuNganId).HasColumnName("thu_ngan_id");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'DANG_BAN'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.KhachHang).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.KhachHangId)
                .HasConstraintName("fk_gio_khach");

            entity.HasOne(d => d.ThuNgan).WithMany(p => p.GioHangs)
                .HasForeignKey(d => d.ThuNganId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_gio_thu_ngan");
        });

        modelBuilder.Entity<HoaDon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("hoa_don_pkey");

            entity.ToTable("hoa_don", "smartmart");

            entity.HasIndex(e => e.HuyLuc, "idx_hoa_don_huy");

            entity.HasIndex(e => new { e.KhachHangId, e.ThanhToanLuc }, "idx_hoa_don_khach");

            entity.HasIndex(e => new { e.ThuNganId, e.ThanhToanLuc }, "idx_hoa_don_thu_ngan");

            entity.HasIndex(e => new { e.TrangThai, e.ThanhToanLuc }, "idx_hoa_don_tt_tg");

            entity.HasIndex(e => e.NguoiHuyId, "ix_hoa_don_nguoi_huy_id");

            entity.HasIndex(e => e.GioHangId, "uq_hoa_don_gio").IsUnique();

            entity.HasIndex(e => e.MaHoaDon, "uq_hoa_don_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiemCong).HasColumnName("diem_cong");
            entity.Property(e => e.GioHangId).HasColumnName("gio_hang_id");
            entity.Property(e => e.HuyLuc).HasColumnName("huy_luc");
            entity.Property(e => e.KhachHangId).HasColumnName("khach_hang_id");
            entity.Property(e => e.LyDoHuy)
                .HasMaxLength(255)
                .HasColumnName("ly_do_huy");
            entity.Property(e => e.MaHoaDon)
                .HasMaxLength(20)
                .HasColumnName("ma_hoa_don");
            entity.Property(e => e.NguoiHuyId).HasColumnName("nguoi_huy_id");
            entity.Property(e => e.SoTienMotDiem).HasColumnName("so_tien_mot_diem");
            entity.Property(e => e.ThanhToanLuc).HasColumnName("thanh_toan_luc");
            entity.Property(e => e.ThuNganId).HasColumnName("thu_ngan_id");
            entity.Property(e => e.TienKhachDua).HasColumnName("tien_khach_dua");
            entity.Property(e => e.TienThua).HasColumnName("tien_thua");
            entity.Property(e => e.TienTruHangDoi).HasColumnName("tien_tru_hang_doi");
            entity.Property(e => e.TongGiam).HasColumnName("tong_giam");
            entity.Property(e => e.TongThanhToan).HasColumnName("tong_thanh_toan");
            entity.Property(e => e.TongTienHang).HasColumnName("tong_tien_hang");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'DA_THANH_TOAN'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.GioHang).WithOne(p => p.HoaDon)
                .HasForeignKey<HoaDon>(d => d.GioHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_hd_gio");

            entity.HasOne(d => d.KhachHang).WithMany(p => p.HoaDons)
                .HasForeignKey(d => d.KhachHangId)
                .HasConstraintName("fk_hd_khach");

            entity.HasOne(d => d.NguoiHuy).WithMany(p => p.HoaDonNguoiHuys)
                .HasForeignKey(d => d.NguoiHuyId)
                .HasConstraintName("fk_hd_nguoi_huy");

            entity.HasOne(d => d.ThuNgan).WithMany(p => p.HoaDonThuNgans)
                .HasForeignKey(d => d.ThuNganId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_hd_thu_ngan");
        });

        modelBuilder.Entity<HoanTienHoaDon>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("hoan_tien_hoa_don_pkey");

            entity.ToTable("hoan_tien_hoa_don", "smartmart");

            entity.HasIndex(e => e.HoaDonId, "hoan_tien_hoa_don_hoa_don_id_key").IsUnique();

            entity.HasIndex(e => e.NguoiThucHienId, "ix_hoan_tien_hoa_don_nguoi_thuc_hien_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiemChuaThuHoi).HasColumnName("diem_chua_thu_hoi");
            entity.Property(e => e.DiemDaThuHoi).HasColumnName("diem_da_thu_hoi");
            entity.Property(e => e.HoaDonId).HasColumnName("hoa_don_id");
            entity.Property(e => e.LyDo)
                .HasMaxLength(255)
                .HasColumnName("ly_do");
            entity.Property(e => e.NguoiThucHienId).HasColumnName("nguoi_thuc_hien_id");
            entity.Property(e => e.PhuongThuc)
                .HasMaxLength(20)
                .HasDefaultValueSql("'TIEN_MAT'::character varying")
                .HasColumnName("phuong_thuc");
            entity.Property(e => e.SoTienHoan).HasColumnName("so_tien_hoan");
            entity.Property(e => e.ThoiDiem)
                .HasDefaultValueSql("now()")
                .HasColumnName("thoi_diem");

            entity.HasOne(d => d.HoaDon).WithOne(p => p.HoanTienHoaDon)
                .HasForeignKey<HoanTienHoaDon>(d => d.HoaDonId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("hoan_tien_hoa_don_hoa_don_id_fkey");

            entity.HasOne(d => d.NguoiThucHien).WithMany(p => p.HoanTienHoaDons)
                .HasForeignKey(d => d.NguoiThucHienId)
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("hoan_tien_hoa_don_nguoi_thuc_hien_id_fkey");
        });

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("khach_hang_pkey");

            entity.ToTable("khach_hang", "smartmart");

            entity.HasIndex(e => e.HoTen, "idx_khach_hang_ten");

            entity.HasIndex(e => e.NguoiTaoId, "ix_khach_hang_nguoi_tao_id");

            entity.HasIndex(e => e.SoDienThoai, "uq_khach_hang_sdt").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiemHienCo).HasColumnName("diem_hien_co");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.GioiTinh)
                .HasMaxLength(40)
                .HasColumnName("gioi_tinh");
            entity.Property(e => e.HoTen)
                .HasMaxLength(100)
                .HasColumnName("ho_ten");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgaySinh).HasColumnName("ngay_sinh");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.PhienBan)
                .HasDefaultValue(1L)
                .HasColumnName("phien_ban");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(10)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'HOAT_DONG'::character varying")
                .HasColumnName("trang_thai");
            entity.Property(e => e.XoaDuLieuLuc).HasColumnName("xoa_du_lieu_luc");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.KhachHangs)
                .HasForeignKey(d => d.NguoiTaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_kh_nguoi_tao");
        });

        modelBuilder.Entity<LichSuDiem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lich_su_diem_pkey");

            entity.ToTable("lich_su_diem", "smartmart");

            entity.HasIndex(e => e.HoaDonId, "idx_lsd_hoa_don");

            entity.HasIndex(e => new { e.KhachHangId, e.ThoiDiem }, "idx_lsd_khach_tg");

            entity.HasIndex(e => e.NguoiThucHienId, "ix_lich_su_diem_nguoi_thuc_hien_id");

            entity.HasIndex(e => e.PhieuDoiTraId, "ix_lich_su_diem_phieu_doi_tra_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiemSau).HasColumnName("diem_sau");
            entity.Property(e => e.HoaDonId).HasColumnName("hoa_don_id");
            entity.Property(e => e.KhachHangId).HasColumnName("khach_hang_id");
            entity.Property(e => e.Loai)
                .HasMaxLength(40)
                .HasColumnName("loai");
            entity.Property(e => e.LyDo)
                .HasMaxLength(255)
                .HasColumnName("ly_do");
            entity.Property(e => e.NguoiThucHienId).HasColumnName("nguoi_thuc_hien_id");
            entity.Property(e => e.PhieuDoiTraId).HasColumnName("phieu_doi_tra_id");
            entity.Property(e => e.SoDiem).HasColumnName("so_diem");
            entity.Property(e => e.ThoiDiem)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("thoi_diem");

            entity.HasOne(d => d.HoaDon).WithMany(p => p.LichSuDiems)
                .HasForeignKey(d => d.HoaDonId)
                .HasConstraintName("fk_lsd_hd");

            entity.HasOne(d => d.KhachHang).WithMany(p => p.LichSuDiems)
                .HasForeignKey(d => d.KhachHangId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lsd_khach");

            entity.HasOne(d => d.NguoiThucHien).WithMany(p => p.LichSuDiems)
                .HasForeignKey(d => d.NguoiThucHienId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lsd_nguoi");

            entity.HasOne(d => d.PhieuDoiTra).WithMany(p => p.LichSuDiems)
                .HasForeignKey(d => d.PhieuDoiTraId)
                .HasConstraintName("fk_lsd_pdt");
        });

        modelBuilder.Entity<LichSuGium>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("lich_su_gia_pkey");

            entity.ToTable("lich_su_gia", "smartmart");

            entity.HasIndex(e => new { e.SanPhamId, e.HieuLucTu }, "idx_lsg_san_pham_hieu_luc");

            entity.HasIndex(e => e.NguoiHuyId, "ix_lich_su_gia_nguoi_huy_id");

            entity.HasIndex(e => e.NguoiTaoId, "ix_lich_su_gia_nguoi_tao_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GiaBan).HasColumnName("gia_ban");
            entity.Property(e => e.HieuLucTu).HasColumnName("hieu_luc_tu");
            entity.Property(e => e.HuyLuc).HasColumnName("huy_luc");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiHuyId).HasColumnName("nguoi_huy_id");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");

            entity.HasOne(d => d.NguoiHuy).WithMany(p => p.LichSuGiumNguoiHuys)
                .HasForeignKey(d => d.NguoiHuyId)
                .HasConstraintName("fk_lsg_nguoi_huy");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.LichSuGiumNguoiTaos)
                .HasForeignKey(d => d.NguoiTaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lsg_nguoi_tao");

            entity.HasOne(d => d.SanPham).WithMany(p => p.LichSuGia)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_lsg_san_pham");
        });

        modelBuilder.Entity<NhaCungCap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("nha_cung_cap_pkey");

            entity.ToTable("nha_cung_cap", "smartmart");

            entity.HasIndex(e => e.Ten, "idx_ncc_ten");

            entity.HasIndex(e => e.Ma, "uq_ncc_ma").IsUnique();

            entity.HasIndex(e => e.SoDienThoai, "uq_ncc_sdt").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiaChi)
                .HasMaxLength(255)
                .HasColumnName("dia_chi");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(500)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.Ma)
                .HasMaxLength(20)
                .HasColumnName("ma");
            entity.Property(e => e.MaSoThue)
                .HasMaxLength(14)
                .HasColumnName("ma_so_thue");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiLienHe)
                .HasMaxLength(100)
                .HasColumnName("nguoi_lien_he");
            entity.Property(e => e.PhienBan)
                .HasDefaultValue(1L)
                .HasColumnName("phien_ban");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(11)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.Ten)
                .HasMaxLength(200)
                .HasColumnName("ten");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'HOAT_DONG'::character varying")
                .HasColumnName("trang_thai");
        });

        modelBuilder.Entity<PhienDangNhap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("phien_dang_nhap_pkey");

            entity.ToTable("phien_dang_nhap", "smartmart");

            entity.HasIndex(e => new { e.TaiKhoanId, e.HuyLuc }, "idx_phien_tai_khoan");

            entity.HasIndex(e => e.TokenBam, "uq_phien_token").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HetHanLuc).HasColumnName("het_han_luc");
            entity.Property(e => e.HoatDongCuoi)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("hoat_dong_cuoi");
            entity.Property(e => e.HuyLuc).HasColumnName("huy_luc");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.TaiKhoanId).HasColumnName("tai_khoan_id");
            entity.Property(e => e.TokenBam)
                .HasMaxLength(64)
                .IsFixedLength()
                .HasColumnName("token_bam");

            entity.HasOne(d => d.TaiKhoan).WithMany(p => p.PhienDangNhaps)
                .HasForeignKey(d => d.TaiKhoanId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_phien_tai_khoan");
        });

        modelBuilder.Entity<PhieuDoiTra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("phieu_doi_tra_pkey");

            entity.ToTable("phieu_doi_tra", "smartmart");

            entity.HasIndex(e => e.HoaDonGocId, "idx_pdt_hd_goc");

            entity.HasIndex(e => new { e.ThuNganId, e.NgayLap }, "idx_pdt_thu_ngan");

            entity.HasIndex(e => new { e.TrangThai, e.HoanTatLuc }, "idx_pdt_tt");

            entity.HasIndex(e => e.ChinhSachId, "ix_phieu_doi_tra_chinh_sach_id");

            entity.HasIndex(e => e.KhachHangId, "ix_phieu_doi_tra_khach_hang_id");

            entity.HasIndex(e => e.NguoiDuyetId, "ix_phieu_doi_tra_nguoi_duyet_id");

            entity.HasIndex(e => e.HoaDonMoiId, "uq_pdt_hd_moi").IsUnique();

            entity.HasIndex(e => new { e.Id, e.HoaDonGocId }, "uq_pdt_id_hd_goc").IsUnique();

            entity.HasIndex(e => e.MaPhieu, "uq_pdt_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CanDuyetDanhMuc).HasColumnName("can_duyet_danh_muc");
            entity.Property(e => e.CanDuyetQuaHan).HasColumnName("can_duyet_qua_han");
            entity.Property(e => e.CanDuyetVuotNguong).HasColumnName("can_duyet_vuot_nguong");
            entity.Property(e => e.ChinhSachId).HasColumnName("chinh_sach_id");
            entity.Property(e => e.DiemThuHoi).HasColumnName("diem_thu_hoi");
            entity.Property(e => e.DuyetLuc).HasColumnName("duyet_luc");
            entity.Property(e => e.GhiChuDuyet)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu_duyet");
            entity.Property(e => e.GiaTriHangTra).HasColumnName("gia_tri_hang_tra");
            entity.Property(e => e.HoaDonGocId).HasColumnName("hoa_don_goc_id");
            entity.Property(e => e.HoaDonMoiId).HasColumnName("hoa_don_moi_id");
            entity.Property(e => e.HoanTatLuc).HasColumnName("hoan_tat_luc");
            entity.Property(e => e.KhachHangId).HasColumnName("khach_hang_id");
            entity.Property(e => e.Loai)
                .HasMaxLength(40)
                .HasColumnName("loai");
            entity.Property(e => e.MaPhieu)
                .HasMaxLength(20)
                .HasColumnName("ma_phieu");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_lap");
            entity.Property(e => e.NguoiDuyetId).HasColumnName("nguoi_duyet_id");
            entity.Property(e => e.ThuNganId).HasColumnName("thu_ngan_id");
            entity.Property(e => e.TienHoan).HasColumnName("tien_hoan");
            entity.Property(e => e.TienThuThem).HasColumnName("tien_thu_them");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.ChinhSach).WithMany(p => p.PhieuDoiTras)
                .HasForeignKey(d => d.ChinhSachId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pdt_cs");

            entity.HasOne(d => d.HoaDonGoc).WithMany(p => p.PhieuDoiTraHoaDonGocs)
                .HasForeignKey(d => d.HoaDonGocId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pdt_hd_goc");

            entity.HasOne(d => d.HoaDonMoi).WithOne(p => p.PhieuDoiTraHoaDonMoi)
                .HasForeignKey<PhieuDoiTra>(d => d.HoaDonMoiId)
                .HasConstraintName("fk_pdt_hd_moi");

            entity.HasOne(d => d.KhachHang).WithMany(p => p.PhieuDoiTras)
                .HasForeignKey(d => d.KhachHangId)
                .HasConstraintName("fk_pdt_khach");

            entity.HasOne(d => d.NguoiDuyet).WithMany(p => p.PhieuDoiTraNguoiDuyets)
                .HasForeignKey(d => d.NguoiDuyetId)
                .HasConstraintName("fk_pdt_nguoi_duyet");

            entity.HasOne(d => d.ThuNgan).WithMany(p => p.PhieuDoiTraThuNgans)
                .HasForeignKey(d => d.ThuNganId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pdt_thu_ngan");
        });

        modelBuilder.Entity<PhieuKiemKe>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("phieu_kiem_ke_pkey");

            entity.ToTable("phieu_kiem_ke", "smartmart");

            entity.HasIndex(e => e.TrangThai, "idx_phieu_kiem_ke_tt");

            entity.HasIndex(e => e.DanhMucId, "ix_phieu_kiem_ke_danh_muc_id");

            entity.HasIndex(e => e.NguoiDuyetId, "ix_phieu_kiem_ke_nguoi_duyet_id");

            entity.HasIndex(e => e.NguoiLapId, "ix_phieu_kiem_ke_nguoi_lap_id");

            entity.HasIndex(e => e.MaPhieu, "uq_phieu_kiem_ke_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DanhMucId).HasColumnName("danh_muc_id");
            entity.Property(e => e.DuyetLuc).HasColumnName("duyet_luc");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.GuiDuyetLuc).HasColumnName("gui_duyet_luc");
            entity.Property(e => e.LyDoTuChoi)
                .HasMaxLength(255)
                .HasColumnName("ly_do_tu_choi");
            entity.Property(e => e.MaPhieu)
                .HasMaxLength(20)
                .HasColumnName("ma_phieu");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_lap");
            entity.Property(e => e.NguoiDuyetId).HasColumnName("nguoi_duyet_id");
            entity.Property(e => e.NguoiLapId).HasColumnName("nguoi_lap_id");
            entity.Property(e => e.PhamVi)
                .HasMaxLength(40)
                .HasColumnName("pham_vi");
            entity.Property(e => e.PhienBan)
                .HasDefaultValue(1L)
                .HasColumnName("phien_ban");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'NHAP'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.DanhMuc).WithMany(p => p.PhieuKiemKes)
                .HasForeignKey(d => d.DanhMucId)
                .HasConstraintName("fk_pkk_danh_muc");

            entity.HasOne(d => d.NguoiDuyet).WithMany(p => p.PhieuKiemKeNguoiDuyets)
                .HasForeignKey(d => d.NguoiDuyetId)
                .HasConstraintName("fk_pkk_nguoi_duyet");

            entity.HasOne(d => d.NguoiLap).WithMany(p => p.PhieuKiemKeNguoiLaps)
                .HasForeignKey(d => d.NguoiLapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pkk_nguoi_lap");
        });

        modelBuilder.Entity<PhieuNhap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("phieu_nhap_pkey");

            entity.ToTable("phieu_nhap", "smartmart");

            entity.HasIndex(e => new { e.NhaCungCapId, e.XacNhanLuc }, "idx_phieu_nhap_ncc");

            entity.HasIndex(e => new { e.TrangThai, e.NgayLap }, "idx_phieu_nhap_tt");

            entity.HasIndex(e => e.XacNhanLuc, "idx_phieu_nhap_xac_nhan");

            entity.HasIndex(e => e.NguoiHuyId, "ix_phieu_nhap_nguoi_huy_id");

            entity.HasIndex(e => e.NguoiLapId, "ix_phieu_nhap_nguoi_lap_id");

            entity.HasIndex(e => e.NguoiXacNhanId, "ix_phieu_nhap_nguoi_xac_nhan_id");

            entity.HasIndex(e => e.MaPhieu, "uq_phieu_nhap_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.HuyLuc).HasColumnName("huy_luc");
            entity.Property(e => e.LyDoHuy)
                .HasMaxLength(255)
                .HasColumnName("ly_do_huy");
            entity.Property(e => e.MaPhieu)
                .HasMaxLength(20)
                .HasColumnName("ma_phieu");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_lap");
            entity.Property(e => e.NgayNhap).HasColumnName("ngay_nhap");
            entity.Property(e => e.NguoiHuyId).HasColumnName("nguoi_huy_id");
            entity.Property(e => e.NguoiLapId).HasColumnName("nguoi_lap_id");
            entity.Property(e => e.NguoiXacNhanId).HasColumnName("nguoi_xac_nhan_id");
            entity.Property(e => e.NhaCungCapId).HasColumnName("nha_cung_cap_id");
            entity.Property(e => e.PhienBan)
                .HasDefaultValue(1L)
                .HasColumnName("phien_ban");
            entity.Property(e => e.SoChungTuNcc)
                .HasMaxLength(50)
                .HasColumnName("so_chung_tu_ncc");
            entity.Property(e => e.TongTien).HasColumnName("tong_tien");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'NHAP'::character varying")
                .HasColumnName("trang_thai");
            entity.Property(e => e.XacNhanLuc).HasColumnName("xac_nhan_luc");

            entity.HasOne(d => d.NguoiHuy).WithMany(p => p.PhieuNhapNguoiHuys)
                .HasForeignKey(d => d.NguoiHuyId)
                .HasConstraintName("fk_pn_nguoi_huy");

            entity.HasOne(d => d.NguoiLap).WithMany(p => p.PhieuNhapNguoiLaps)
                .HasForeignKey(d => d.NguoiLapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pn_nguoi_lap");

            entity.HasOne(d => d.NguoiXacNhan).WithMany(p => p.PhieuNhapNguoiXacNhans)
                .HasForeignKey(d => d.NguoiXacNhanId)
                .HasConstraintName("fk_pn_nguoi_xac_nhan");

            entity.HasOne(d => d.NhaCungCap).WithMany(p => p.PhieuNhaps)
                .HasForeignKey(d => d.NhaCungCapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pn_ncc");
        });

        modelBuilder.Entity<PhieuXuatHuy>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("phieu_xuat_huy_pkey");

            entity.ToTable("phieu_xuat_huy", "smartmart");

            entity.HasIndex(e => e.DuyetLuc, "idx_phieu_xuat_huy_duyet");

            entity.HasIndex(e => e.TrangThai, "idx_phieu_xuat_huy_tt");

            entity.HasIndex(e => e.NguoiDuyetId, "ix_phieu_xuat_huy_nguoi_duyet_id");

            entity.HasIndex(e => e.NguoiLapId, "ix_phieu_xuat_huy_nguoi_lap_id");

            entity.HasIndex(e => e.MaPhieu, "uq_phieu_xuat_huy_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DuyetLuc).HasColumnName("duyet_luc");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.GuiDuyetLuc).HasColumnName("gui_duyet_luc");
            entity.Property(e => e.LyDoTuChoi)
                .HasMaxLength(255)
                .HasColumnName("ly_do_tu_choi");
            entity.Property(e => e.MaPhieu)
                .HasMaxLength(20)
                .HasColumnName("ma_phieu");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_lap");
            entity.Property(e => e.NguoiDuyetId).HasColumnName("nguoi_duyet_id");
            entity.Property(e => e.NguoiLapId).HasColumnName("nguoi_lap_id");
            entity.Property(e => e.PhienBan)
                .HasDefaultValue(1L)
                .HasColumnName("phien_ban");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'NHAP'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.NguoiDuyet).WithMany(p => p.PhieuXuatHuyNguoiDuyets)
                .HasForeignKey(d => d.NguoiDuyetId)
                .HasConstraintName("fk_pxh_nguoi_duyet");

            entity.HasOne(d => d.NguoiLap).WithMany(p => p.PhieuXuatHuyNguoiLaps)
                .HasForeignKey(d => d.NguoiLapId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_pxh_nguoi_lap");
        });

        modelBuilder.Entity<QuyTacTichDiem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("quy_tac_tich_diem_pkey");

            entity.ToTable("quy_tac_tich_diem", "smartmart");

            entity.HasIndex(e => e.HieuLucTu, "idx_qttd_hieu_luc");

            entity.HasIndex(e => e.NguoiTaoId, "ix_quy_tac_tich_diem_nguoi_tao_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DangBat)
                .HasDefaultValue(true)
                .HasColumnName("dang_bat");
            entity.Property(e => e.HieuLucTu).HasColumnName("hieu_luc_tu");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.SoTienMotDiem)
                .HasDefaultValue(10000L)
                .HasColumnName("so_tien_mot_diem");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.QuyTacTichDiems)
                .HasForeignKey(d => d.NguoiTaoId)
                .HasConstraintName("fk_qttd_nguoi_tao");
        });

        modelBuilder.Entity<QuyTienMat>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("quy_tien_mat_pkey");

            entity.ToTable("quy_tien_mat", "smartmart");

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.DaChotSoDuDau).HasColumnName("da_chot_so_du_dau");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("now()")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.SoDu).HasColumnName("so_du");
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("san_pham_pkey");

            entity.ToTable("san_pham", "smartmart");

            entity.HasIndex(e => new { e.DanhMucId, e.TrangThai }, "idx_san_pham_danh_muc");

            entity.HasIndex(e => e.TenSanPham, "idx_san_pham_ten");

            entity.HasIndex(e => e.NguoiTaoId, "ix_san_pham_nguoi_tao_id");

            entity.HasIndex(e => e.Ma, "uq_san_pham_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.AnhUrl)
                .HasMaxLength(255)
                .HasColumnName("anh_url");
            entity.Property(e => e.DanhMucId).HasColumnName("danh_muc_id");
            entity.Property(e => e.DonViTinh)
                .HasMaxLength(20)
                .HasColumnName("don_vi_tinh");
            entity.Property(e => e.Ma)
                .HasMaxLength(20)
                .HasColumnName("ma");
            entity.Property(e => e.MoTa)
                .HasMaxLength(1000)
                .HasColumnName("mo_ta");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.PhienBan)
                .HasDefaultValue(1L)
                .HasColumnName("phien_ban");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(200)
                .HasColumnName("ten_san_pham");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'DANG_KINH_DOANH'::character varying")
                .HasColumnName("trang_thai");

            entity.HasOne(d => d.DanhMuc).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.DanhMucId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_san_pham_danh_muc");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.SanPhams)
                .HasForeignKey(d => d.NguoiTaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_san_pham_nguoi_tao");
        });

        modelBuilder.Entity<TaiKhoan>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("tai_khoan_pkey");

            entity.ToTable("tai_khoan", "smartmart");

            entity.HasIndex(e => new { e.VaiTroId, e.TrangThai }, "idx_tai_khoan_vai_tro");

            entity.HasIndex(e => e.Email, "uq_tai_khoan_email").IsUnique();

            entity.HasIndex(e => e.SoDienThoai, "uq_tai_khoan_sdt").IsUnique();

            entity.HasIndex(e => e.TenDangNhap, "uq_tai_khoan_ten").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatBuocDoiMk)
                .HasDefaultValue(true)
                .HasColumnName("bat_buoc_doi_mk");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.HoTen)
                .HasMaxLength(100)
                .HasColumnName("ho_ten");
            entity.Property(e => e.KhoaTamDen).HasColumnName("khoa_tam_den");
            entity.Property(e => e.LanDangNhapCuoi).HasColumnName("lan_dang_nhap_cuoi");
            entity.Property(e => e.LyDoKhoa)
                .HasMaxLength(255)
                .HasColumnName("ly_do_khoa");
            entity.Property(e => e.MatKhauBam)
                .HasMaxLength(255)
                .HasColumnName("mat_khau_bam");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.PhienBan)
                .HasDefaultValue(1L)
                .HasColumnName("phien_ban");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(10)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.SoLanSai).HasColumnName("so_lan_sai");
            entity.Property(e => e.TenDangNhap)
                .HasMaxLength(50)
                .HasColumnName("ten_dang_nhap");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasDefaultValueSql("'HOAT_DONG'::character varying")
                .HasColumnName("trang_thai");
            entity.Property(e => e.VaiTroId).HasColumnName("vai_tro_id");

            entity.HasOne(d => d.VaiTro).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.VaiTroId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tai_khoan_vai_tro");
        });

        modelBuilder.Entity<TheKho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("the_kho_pkey");

            entity.ToTable("the_kho", "smartmart");

            entity.HasIndex(e => new { e.LoaiBienDong, e.ChungTuId }, "idx_the_kho_chung_tu");

            entity.HasIndex(e => new { e.SanPhamId, e.ThoiDiem }, "idx_the_kho_sp_tg");

            entity.HasIndex(e => e.ThoiDiem, "idx_the_kho_tg");

            entity.HasIndex(e => e.NguoiThucHienId, "ix_the_kho_nguoi_thuc_hien_id");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChungTuId).HasColumnName("chung_tu_id");
            entity.Property(e => e.GiaVonApDung)
                .HasPrecision(15, 2)
                .HasColumnName("gia_von_ap_dung");
            entity.Property(e => e.GiaVonSau)
                .HasPrecision(15, 2)
                .HasColumnName("gia_von_sau");
            entity.Property(e => e.LoaiBienDong)
                .HasMaxLength(40)
                .HasColumnName("loai_bien_dong");
            entity.Property(e => e.MaChungTu)
                .HasMaxLength(20)
                .HasColumnName("ma_chung_tu");
            entity.Property(e => e.NguoiThucHienId).HasColumnName("nguoi_thuc_hien_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.SoLuongThayDoi).HasColumnName("so_luong_thay_doi");
            entity.Property(e => e.ThoiDiem)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("thoi_diem");
            entity.Property(e => e.TonSau).HasColumnName("ton_sau");

            entity.HasOne(d => d.NguoiThucHien).WithMany(p => p.TheKhos)
                .HasForeignKey(d => d.NguoiThucHienId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_the_kho_nguoi");

            entity.HasOne(d => d.SanPham).WithMany(p => p.TheKhos)
                .HasForeignKey(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_the_kho_san_pham");
        });

        modelBuilder.Entity<TonKho>(entity =>
        {
            entity.HasKey(e => e.SanPhamId).HasName("ton_kho_pkey");

            entity.ToTable("ton_kho", "smartmart");

            entity.Property(e => e.SanPhamId)
                .ValueGeneratedNever()
                .HasColumnName("san_pham_id");
            entity.Property(e => e.GiaVonBinhQuan)
                .HasPrecision(15, 2)
                .HasColumnName("gia_von_binh_quan");
            entity.Property(e => e.NgayCapNhat)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NguongToiThieu)
                .HasDefaultValue(5)
                .HasColumnName("nguong_toi_thieu");
            entity.Property(e => e.SoLuongTon).HasColumnName("so_luong_ton");

            entity.HasOne(d => d.SanPham).WithOne(p => p.TonKho)
                .HasForeignKey<TonKho>(d => d.SanPhamId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_ton_kho_san_pham");
        });

        modelBuilder.Entity<VChuongTrinhKhuyenMai>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_chuong_trinh_khuyen_mai", "smartmart")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.AnhUrl)
                .HasMaxLength(2048)
                .HasColumnName("anh_url");
            entity.Property(e => e.BatDauLuc).HasColumnName("bat_dau_luc");
            entity.Property(e => e.ChiThanhVien).HasColumnName("chi_thanh_vien");
            entity.Property(e => e.GiaTri).HasColumnName("gia_tri");
            entity.Property(e => e.GiamToiDa).HasColumnName("giam_toi_da");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KetThucLuc).HasColumnName("ket_thuc_luc");
            entity.Property(e => e.LoaiGiam)
                .HasMaxLength(40)
                .HasColumnName("loai_giam");
            entity.Property(e => e.Ma)
                .HasMaxLength(20)
                .HasColumnName("ma");
            entity.Property(e => e.MoTa)
                .HasMaxLength(500)
                .HasColumnName("mo_ta");
            entity.Property(e => e.NgayCapNhat).HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao).HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.PhienBan).HasColumnName("phien_ban");
            entity.Property(e => e.Ten)
                .HasMaxLength(200)
                .HasColumnName("ten");
            entity.Property(e => e.TrangThai)
                .HasMaxLength(40)
                .HasColumnName("trang_thai");
            entity.Property(e => e.TrangThaiHienThi).HasColumnName("trang_thai_hien_thi");
        });

        modelBuilder.Entity<VGiaBanHienHanh>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_gia_ban_hien_hanh", "smartmart")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.GiaBan).HasColumnName("gia_ban");
            entity.Property(e => e.HieuLucTu).HasColumnName("hieu_luc_tu");
            entity.Property(e => e.LichSuGiaId).HasColumnName("lich_su_gia_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
        });

        modelBuilder.Entity<VLuotDungKhuyenMai>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_luot_dung_khuyen_mai", "smartmart")
                .HasAnnotation("Npgsql:StorageParameter:security_invoker", "true");

            entity.Property(e => e.ChuongTrinhId).HasColumnName("chuong_trinh_id");
            entity.Property(e => e.HoaDonId).HasColumnName("hoa_don_id");
            entity.Property(e => e.KhachHangId).HasColumnName("khach_hang_id");
            entity.Property(e => e.MaHoaDon)
                .HasMaxLength(20)
                .HasColumnName("ma_hoa_don");
            entity.Property(e => e.SoLuongSanPham).HasColumnName("so_luong_san_pham");
            entity.Property(e => e.ThanhToanLuc).HasColumnName("thanh_toan_luc");
            entity.Property(e => e.ThuNganId).HasColumnName("thu_ngan_id");
            entity.Property(e => e.TienGiam).HasColumnName("tien_giam");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("vai_tro_pkey");

            entity.ToTable("vai_tro", "smartmart");

            entity.HasIndex(e => e.Ma, "uq_vai_tro_ma").IsUnique();

            entity.Property(e => e.Id)
                .ValueGeneratedNever()
                .HasColumnName("id");
            entity.Property(e => e.Ma)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("ma");
            entity.Property(e => e.Ten)
                .HasMaxLength(50)
                .HasColumnName("ten");
        });

        ConfigurePostgreSqlConstraints(modelBuilder);
        ConfigurePostgreSqlDefaults(modelBuilder);
        ConvenienceStore.Web.Shared.Data.VietnameseSearch.Configure(modelBuilder);
    }
}
