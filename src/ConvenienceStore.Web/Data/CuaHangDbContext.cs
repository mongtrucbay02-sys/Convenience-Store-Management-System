using System;
using System.Collections.Generic;
using ConvenienceStore.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace ConvenienceStore.Web.Data;

public partial class CuaHangDbContext : DbContext
{
    public CuaHangDbContext(DbContextOptions<CuaHangDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<BoDemChungTu> BoDemChungTus { get; set; }

    public virtual DbSet<ChinhSachDoiTra> ChinhSachDoiTras { get; set; }

    public virtual DbSet<ChuongTrinhKhuyenMai> ChuongTrinhKhuyenMais { get; set; }

    public virtual DbSet<CtGioHang> CtGioHangs { get; set; }

    public virtual DbSet<CtHoaDon> CtHoaDons { get; set; }

    public virtual DbSet<CtKiemKe> CtKiemKes { get; set; }

    public virtual DbSet<CtPhieuDoiTra> CtPhieuDoiTras { get; set; }

    public virtual DbSet<CtPhieuNhap> CtPhieuNhaps { get; set; }

    public virtual DbSet<CtXuatHuy> CtXuatHuys { get; set; }

    public virtual DbSet<DanhMuc> DanhMucs { get; set; }

    public virtual DbSet<GioHang> GioHangs { get; set; }

    public virtual DbSet<HoaDon> HoaDons { get; set; }

    public virtual DbSet<KhachHang> KhachHangs { get; set; }

    public virtual DbSet<LichSuDiem> LichSuDiems { get; set; }

    public virtual DbSet<LichSuGium> LichSuGia { get; set; }

    public virtual DbSet<NhaCungCap> NhaCungCaps { get; set; }

    public virtual DbSet<PhienDangNhap> PhienDangNhaps { get; set; }

    public virtual DbSet<PhieuDoiTra> PhieuDoiTras { get; set; }

    public virtual DbSet<PhieuKiemKe> PhieuKiemKes { get; set; }

    public virtual DbSet<PhieuNhap> PhieuNhaps { get; set; }

    public virtual DbSet<PhieuXuatHuy> PhieuXuatHuys { get; set; }

    public virtual DbSet<QuyTacTichDiem> QuyTacTichDiems { get; set; }

    public virtual DbSet<SanPham> SanPhams { get; set; }

    public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }

    public virtual DbSet<TheKho> TheKhos { get; set; }

    public virtual DbSet<TonKho> TonKhos { get; set; }

    public virtual DbSet<VChuongTrinhKhuyenMai> VChuongTrinhKhuyenMais { get; set; }

    public virtual DbSet<VGiaBanHienHanh> VGiaBanHienHanhs { get; set; }

    public virtual DbSet<VLuotDungKhuyenMai> VLuotDungKhuyenMais { get; set; }

    public virtual DbSet<VaiTro> VaiTros { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BoDemChungTu>(entity =>
        {
            entity.HasKey(e => new { e.TienTo, e.Ky }).HasName("PRIMARY");

            entity.ToTable("bo_dem_chung_tu");

            entity.Property(e => e.TienTo)
                .HasMaxLength(3)
                .HasColumnName("tien_to");
            entity.Property(e => e.Ky)
                .HasMaxLength(8)
                .HasDefaultValueSql("''")
                .IsFixedLength()
                .HasColumnName("ky");
            entity.Property(e => e.SoCuoi).HasColumnName("so_cuoi");
        });

        modelBuilder.Entity<ChinhSachDoiTra>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("chinh_sach_doi_tra");

            entity.HasIndex(e => e.NguoiTaoId, "fk_csdt_nguoi_tao");

            entity.HasIndex(e => e.HieuLucTu, "idx_csdt_hieu_luc");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HieuLucTu)
                .HasColumnType("datetime")
                .HasColumnName("hieu_luc_tu");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.NguongTienCanDuyet)
                .HasDefaultValueSql("'500000'")
                .HasColumnName("nguong_tien_can_duyet");
            entity.Property(e => e.SoNgayDoiTra)
                .HasDefaultValueSql("'7'")
                .HasColumnName("so_ngay_doi_tra");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.ChinhSachDoiTras)
                .HasForeignKey(d => d.NguoiTaoId)
                .HasConstraintName("fk_csdt_nguoi_tao");
        });

        modelBuilder.Entity<ChuongTrinhKhuyenMai>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("chuong_trinh_khuyen_mai");

            entity.HasIndex(e => e.NguoiTaoId, "fk_ctkm_nguoi_tao");

            entity.HasIndex(e => new { e.TrangThai, e.BatDauLuc, e.KetThucLuc }, "idx_ctkm_tt");

            entity.HasIndex(e => e.Ma, "uq_ctkm_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatDauLuc)
                .HasColumnType("datetime")
                .HasColumnName("bat_dau_luc");
            entity.Property(e => e.ChiThanhVien).HasColumnName("chi_thanh_vien");
            entity.Property(e => e.GiaTri).HasColumnName("gia_tri");
            entity.Property(e => e.GiamToiDa).HasColumnName("giam_toi_da");
            entity.Property(e => e.KetThucLuc)
                .HasColumnType("datetime")
                .HasColumnName("ket_thuc_luc");
            entity.Property(e => e.LoaiGiam)
                .HasColumnType("enum('PHAN_TRAM','SO_TIEN')")
                .HasColumnName("loai_giam");
            entity.Property(e => e.Ma)
                .HasMaxLength(20)
                .HasColumnName("ma");
            entity.Property(e => e.MoTa)
                .HasMaxLength(500)
                .HasColumnName("mo_ta");
            entity.Property(e => e.NgayCapNhat)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.Ten)
                .HasMaxLength(200)
                .HasColumnName("ten");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'NHAP'")
                .HasColumnType("enum('NHAP','DA_KICH_HOAT','TAM_DUNG','DA_KET_THUC')")
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
                        j.HasKey("ChuongTrinhId", "SanPhamId").HasName("PRIMARY");
                        j.ToTable("khuyen_mai_san_pham");
                        j.HasIndex(new[] { "SanPhamId" }, "idx_kmsp_san_pham");
                        j.IndexerProperty<uint>("ChuongTrinhId").HasColumnName("chuong_trinh_id");
                        j.IndexerProperty<uint>("SanPhamId").HasColumnName("san_pham_id");
                    });
        });

        modelBuilder.Entity<CtGioHang>(entity =>
        {
            entity.HasKey(e => new { e.GioHangId, e.SanPhamId }).HasName("PRIMARY");

            entity.ToTable("ct_gio_hang");

            entity.HasIndex(e => e.KhuyenMaiId, "fk_ctgh_km");

            entity.HasIndex(e => e.SanPhamId, "idx_ctgh_san_pham");

            entity.Property(e => e.GioHangId).HasColumnName("gio_hang_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.DonGia).HasColumnName("don_gia");
            entity.Property(e => e.GiamDonVi).HasColumnName("giam_don_vi");
            entity.Property(e => e.KhuyenMaiId).HasColumnName("khuyen_mai_id");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.ThemLuc)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
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
            entity.HasKey(e => new { e.HoaDonId, e.SanPhamId }).HasName("PRIMARY");

            entity.ToTable("ct_hoa_don");

            entity.HasIndex(e => e.KhuyenMaiId, "idx_cthd_km");

            entity.HasIndex(e => e.SanPhamId, "idx_cthd_san_pham");

            entity.Property(e => e.HoaDonId).HasColumnName("hoa_don_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.DonGia).HasColumnName("don_gia");
            entity.Property(e => e.DonViTinh)
                .HasMaxLength(20)
                .HasColumnName("don_vi_tinh");
            entity.Property(e => e.GiaVon)
                .HasPrecision(15)
                .HasColumnName("gia_von");
            entity.Property(e => e.GiamDonVi).HasColumnName("giam_don_vi");
            entity.Property(e => e.KhuyenMaiId).HasColumnName("khuyen_mai_id");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(200)
                .HasColumnName("ten_san_pham");
            entity.Property(e => e.ThanhTien).HasColumnName("thanh_tien");

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
            entity.HasKey(e => new { e.PhieuKiemKeId, e.SanPhamId }).HasName("PRIMARY");

            entity.ToTable("ct_kiem_ke");

            entity.HasIndex(e => e.SanPhamId, "idx_ctkk_san_pham");

            entity.Property(e => e.PhieuKiemKeId).HasColumnName("phieu_kiem_ke_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.ChenhLechLucDuyet).HasColumnName("chenh_lech_luc_duyet");
            entity.Property(e => e.GiaVonLucDuyet)
                .HasPrecision(15)
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
            entity.HasKey(e => new { e.PhieuDoiTraId, e.SanPhamId }).HasName("PRIMARY");

            entity.ToTable("ct_phieu_doi_tra");

            entity.HasIndex(e => new { e.PhieuDoiTraId, e.HoaDonId }, "fk_ctdt_phieu_hd_goc");

            entity.HasIndex(e => new { e.HoaDonId, e.SanPhamId }, "idx_ctdt_dong_hd");

            entity.HasIndex(e => e.SanPhamId, "idx_ctdt_san_pham");

            entity.Property(e => e.PhieuDoiTraId).HasColumnName("phieu_doi_tra_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.CachXuLy)
                .HasColumnType("enum('NHAP_LAI','HUY')")
                .HasColumnName("cach_xu_ly");
            entity.Property(e => e.DonGiaThucTra).HasColumnName("don_gia_thuc_tra");
            entity.Property(e => e.GhiChuLyDo)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu_ly_do");
            entity.Property(e => e.GiaTriHuy)
                .HasPrecision(17)
                .HasColumnName("gia_tri_huy");
            entity.Property(e => e.GiaVon)
                .HasPrecision(15)
                .HasColumnName("gia_von");
            entity.Property(e => e.HoaDonId).HasColumnName("hoa_don_id");
            entity.Property(e => e.LyDo)
                .HasColumnType("enum('LOI_SAN_PHAM','HET_HAN','KHONG_DUNG_NHU_CAU','KHAC')")
                .HasColumnName("ly_do");
            entity.Property(e => e.NgoaiLe).HasColumnName("ngoai_le");
            entity.Property(e => e.SoLuong).HasColumnName("so_luong");
            entity.Property(e => e.ThanhTien).HasColumnName("thanh_tien");
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
            entity.HasKey(e => new { e.PhieuNhapId, e.SanPhamId }).HasName("PRIMARY");

            entity.ToTable("ct_phieu_nhap");

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
            entity.Property(e => e.ThanhTien).HasColumnName("thanh_tien");

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
            entity.HasKey(e => new { e.PhieuXuatHuyId, e.SanPhamId }).HasName("PRIMARY");

            entity.ToTable("ct_xuat_huy");

            entity.HasIndex(e => e.SanPhamId, "idx_ctxh_san_pham");

            entity.Property(e => e.PhieuXuatHuyId).HasColumnName("phieu_xuat_huy_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.GhiChuLyDo)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu_ly_do");
            entity.Property(e => e.GiaVonLucDuyet)
                .HasPrecision(15)
                .HasColumnName("gia_von_luc_duyet");
            entity.Property(e => e.LyDo)
                .HasColumnType("enum('HONG','HET_HAN','MAT','KHAC')")
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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("danh_muc");

            entity.HasIndex(e => e.Ten, "uq_danh_muc_ten").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KhongDoiTra).HasColumnName("khong_doi_tra");
            entity.Property(e => e.MoTa)
                .HasMaxLength(255)
                .HasColumnName("mo_ta");
            entity.Property(e => e.NgayCapNhat)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.Ten)
                .HasMaxLength(100)
                .HasColumnName("ten");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'HOAT_DONG'")
                .HasColumnType("enum('HOAT_DONG','DA_AN')")
                .HasColumnName("trang_thai");
        });

        modelBuilder.Entity<GioHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("gio_hang");

            entity.HasIndex(e => new { e.TrangThai, e.HuyLuc }, "idx_gio_huy");

            entity.HasIndex(e => e.KhachHangId, "idx_gio_khach");

            entity.HasIndex(e => new { e.ThuNganId, e.TrangThai }, "idx_gio_thu_ngan");

            entity.HasIndex(e => e.DangBanCuaThuNgan, "uq_gio_dang_ban").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DangBanCuaThuNgan).HasColumnName("dang_ban_cua_thu_ngan");
            entity.Property(e => e.GhiChuTamGiu)
                .HasMaxLength(100)
                .HasColumnName("ghi_chu_tam_giu");
            entity.Property(e => e.GiaTriKhiHuy).HasColumnName("gia_tri_khi_huy");
            entity.Property(e => e.HoanTatLuc)
                .HasColumnType("datetime")
                .HasColumnName("hoan_tat_luc");
            entity.Property(e => e.HuyLuc)
                .HasColumnType("datetime")
                .HasColumnName("huy_luc");
            entity.Property(e => e.KhachHangId).HasColumnName("khach_hang_id");
            entity.Property(e => e.LyDoHuy)
                .HasColumnType("enum('THU_NGAN_HUY','HET_HAN_TAM_GIU')")
                .HasColumnName("ly_do_huy");
            entity.Property(e => e.NgayCapNhat)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.TamGiuLuc)
                .HasColumnType("datetime")
                .HasColumnName("tam_giu_luc");
            entity.Property(e => e.ThuNganId).HasColumnName("thu_ngan_id");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'DANG_BAN'")
                .HasColumnType("enum('DANG_BAN','TAM_GIU','DA_HOAN_TAT','DA_HUY')")
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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("hoa_don");

            entity.HasIndex(e => e.NguoiHuyId, "fk_hd_nguoi_huy");

            entity.HasIndex(e => e.HuyLuc, "idx_hoa_don_huy");

            entity.HasIndex(e => new { e.KhachHangId, e.ThanhToanLuc }, "idx_hoa_don_khach");

            entity.HasIndex(e => new { e.ThuNganId, e.ThanhToanLuc }, "idx_hoa_don_thu_ngan");

            entity.HasIndex(e => new { e.TrangThai, e.ThanhToanLuc }, "idx_hoa_don_tt_tg");

            entity.HasIndex(e => e.GioHangId, "uq_hoa_don_gio").IsUnique();

            entity.HasIndex(e => e.MaHoaDon, "uq_hoa_don_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiemCong).HasColumnName("diem_cong");
            entity.Property(e => e.GioHangId).HasColumnName("gio_hang_id");
            entity.Property(e => e.HuyLuc)
                .HasColumnType("datetime")
                .HasColumnName("huy_luc");
            entity.Property(e => e.KhachHangId).HasColumnName("khach_hang_id");
            entity.Property(e => e.LyDoHuy)
                .HasMaxLength(255)
                .HasColumnName("ly_do_huy");
            entity.Property(e => e.MaHoaDon)
                .HasMaxLength(20)
                .HasColumnName("ma_hoa_don");
            entity.Property(e => e.NguoiHuyId).HasColumnName("nguoi_huy_id");
            entity.Property(e => e.SoTienMotDiem).HasColumnName("so_tien_mot_diem");
            entity.Property(e => e.ThanhToanLuc)
                .HasColumnType("datetime")
                .HasColumnName("thanh_toan_luc");
            entity.Property(e => e.ThuNganId).HasColumnName("thu_ngan_id");
            entity.Property(e => e.TienKhachDua).HasColumnName("tien_khach_dua");
            entity.Property(e => e.TienThua).HasColumnName("tien_thua");
            entity.Property(e => e.TienTruHangDoi).HasColumnName("tien_tru_hang_doi");
            entity.Property(e => e.TongGiam).HasColumnName("tong_giam");
            entity.Property(e => e.TongThanhToan).HasColumnName("tong_thanh_toan");
            entity.Property(e => e.TongTienHang).HasColumnName("tong_tien_hang");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'DA_THANH_TOAN'")
                .HasColumnType("enum('DA_THANH_TOAN','DA_HUY')")
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

        modelBuilder.Entity<KhachHang>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("khach_hang");

            entity.HasIndex(e => e.NguoiTaoId, "fk_kh_nguoi_tao");

            entity.HasIndex(e => e.HoTen, "idx_khach_hang_ten");

            entity.HasIndex(e => e.SoDienThoai, "uq_khach_hang_sdt").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiemHienCo).HasColumnName("diem_hien_co");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.GioiTinh)
                .HasColumnType("enum('NAM','NU','KHAC')")
                .HasColumnName("gioi_tinh");
            entity.Property(e => e.HoTen)
                .HasMaxLength(100)
                .HasColumnName("ho_ten");
            entity.Property(e => e.NgayCapNhat)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgaySinh)
                .HasColumnType("date")
                .HasColumnName("ngay_sinh");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(10)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'HOAT_DONG'")
                .HasColumnType("enum('HOAT_DONG','DA_AN')")
                .HasColumnName("trang_thai");
            entity.Property(e => e.XoaDuLieuLuc)
                .HasColumnType("datetime")
                .HasColumnName("xoa_du_lieu_luc");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.KhachHangs)
                .HasForeignKey(d => d.NguoiTaoId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_kh_nguoi_tao");
        });

        modelBuilder.Entity<LichSuDiem>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("lich_su_diem");

            entity.HasIndex(e => e.NguoiThucHienId, "fk_lsd_nguoi");

            entity.HasIndex(e => e.PhieuDoiTraId, "fk_lsd_pdt");

            entity.HasIndex(e => e.HoaDonId, "idx_lsd_hoa_don");

            entity.HasIndex(e => new { e.KhachHangId, e.ThoiDiem }, "idx_lsd_khach_tg");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DiemSau).HasColumnName("diem_sau");
            entity.Property(e => e.HoaDonId).HasColumnName("hoa_don_id");
            entity.Property(e => e.KhachHangId).HasColumnName("khach_hang_id");
            entity.Property(e => e.Loai)
                .HasColumnType("enum('TICH_DIEM','THU_HOI_TRA_HANG','THU_HOI_HUY_HOA_DON','DIEU_CHINH')")
                .HasColumnName("loai");
            entity.Property(e => e.LyDo)
                .HasMaxLength(255)
                .HasColumnName("ly_do");
            entity.Property(e => e.NguoiThucHienId).HasColumnName("nguoi_thuc_hien_id");
            entity.Property(e => e.PhieuDoiTraId).HasColumnName("phieu_doi_tra_id");
            entity.Property(e => e.SoDiem).HasColumnName("so_diem");
            entity.Property(e => e.ThoiDiem)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("lich_su_gia");

            entity.HasIndex(e => e.NguoiHuyId, "fk_lsg_nguoi_huy");

            entity.HasIndex(e => e.NguoiTaoId, "fk_lsg_nguoi_tao");

            entity.HasIndex(e => new { e.SanPhamId, e.HieuLucTu }, "idx_lsg_san_pham_hieu_luc");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GiaBan).HasColumnName("gia_ban");
            entity.Property(e => e.HieuLucTu)
                .HasColumnType("datetime")
                .HasColumnName("hieu_luc_tu");
            entity.Property(e => e.HuyLuc)
                .HasColumnType("datetime")
                .HasColumnName("huy_luc");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("nha_cung_cap");

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
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiLienHe)
                .HasMaxLength(100)
                .HasColumnName("nguoi_lien_he");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(11)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.Ten)
                .HasMaxLength(200)
                .HasColumnName("ten");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'HOAT_DONG'")
                .HasColumnType("enum('HOAT_DONG','NGUNG_HOP_TAC')")
                .HasColumnName("trang_thai");
        });

        modelBuilder.Entity<PhienDangNhap>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("phien_dang_nhap");

            entity.HasIndex(e => new { e.TaiKhoanId, e.HuyLuc }, "idx_phien_tai_khoan");

            entity.HasIndex(e => e.TokenBam, "uq_phien_token").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.HoatDongCuoi)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("hoat_dong_cuoi");
            entity.Property(e => e.HuyLuc)
                .HasColumnType("datetime")
                .HasColumnName("huy_luc");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("phieu_doi_tra");

            entity.HasIndex(e => e.ChinhSachId, "fk_pdt_cs");

            entity.HasIndex(e => e.KhachHangId, "fk_pdt_khach");

            entity.HasIndex(e => e.NguoiDuyetId, "fk_pdt_nguoi_duyet");

            entity.HasIndex(e => e.HoaDonGocId, "idx_pdt_hd_goc");

            entity.HasIndex(e => new { e.ThuNganId, e.NgayLap }, "idx_pdt_thu_ngan");

            entity.HasIndex(e => new { e.TrangThai, e.HoanTatLuc }, "idx_pdt_tt");

            entity.HasIndex(e => e.HoaDonMoiId, "uq_pdt_hd_moi").IsUnique();

            entity.HasIndex(e => new { e.Id, e.HoaDonGocId }, "uq_pdt_id_hd_goc").IsUnique();

            entity.HasIndex(e => e.MaPhieu, "uq_pdt_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.CanDuyetDanhMuc).HasColumnName("can_duyet_danh_muc");
            entity.Property(e => e.CanDuyetQuaHan).HasColumnName("can_duyet_qua_han");
            entity.Property(e => e.CanDuyetVuotNguong).HasColumnName("can_duyet_vuot_nguong");
            entity.Property(e => e.ChinhSachId).HasColumnName("chinh_sach_id");
            entity.Property(e => e.DiemThuHoi).HasColumnName("diem_thu_hoi");
            entity.Property(e => e.DuyetLuc)
                .HasColumnType("datetime")
                .HasColumnName("duyet_luc");
            entity.Property(e => e.GhiChuDuyet)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu_duyet");
            entity.Property(e => e.GiaTriHangTra).HasColumnName("gia_tri_hang_tra");
            entity.Property(e => e.HoaDonGocId).HasColumnName("hoa_don_goc_id");
            entity.Property(e => e.HoaDonMoiId).HasColumnName("hoa_don_moi_id");
            entity.Property(e => e.HoanTatLuc)
                .HasColumnType("datetime")
                .HasColumnName("hoan_tat_luc");
            entity.Property(e => e.KhachHangId).HasColumnName("khach_hang_id");
            entity.Property(e => e.Loai)
                .HasColumnType("enum('TRA_HANG','DOI_HANG')")
                .HasColumnName("loai");
            entity.Property(e => e.MaPhieu)
                .HasMaxLength(20)
                .HasColumnName("ma_phieu");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_lap");
            entity.Property(e => e.NguoiDuyetId).HasColumnName("nguoi_duyet_id");
            entity.Property(e => e.ThuNganId).HasColumnName("thu_ngan_id");
            entity.Property(e => e.TienHoan).HasColumnName("tien_hoan");
            entity.Property(e => e.TienThuThem).HasColumnName("tien_thu_them");
            entity.Property(e => e.TrangThai)
                .HasColumnType("enum('CHO_DUYET','DA_DUYET','HOAN_TAT','BI_TU_CHOI')")
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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("phieu_kiem_ke");

            entity.HasIndex(e => e.DanhMucId, "fk_pkk_danh_muc");

            entity.HasIndex(e => e.NguoiDuyetId, "fk_pkk_nguoi_duyet");

            entity.HasIndex(e => e.NguoiLapId, "fk_pkk_nguoi_lap");

            entity.HasIndex(e => e.TrangThai, "idx_phieu_kiem_ke_tt");

            entity.HasIndex(e => e.MaPhieu, "uq_phieu_kiem_ke_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DanhMucId).HasColumnName("danh_muc_id");
            entity.Property(e => e.DuyetLuc)
                .HasColumnType("datetime")
                .HasColumnName("duyet_luc");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.GuiDuyetLuc)
                .HasColumnType("datetime")
                .HasColumnName("gui_duyet_luc");
            entity.Property(e => e.LyDoTuChoi)
                .HasMaxLength(255)
                .HasColumnName("ly_do_tu_choi");
            entity.Property(e => e.MaPhieu)
                .HasMaxLength(20)
                .HasColumnName("ma_phieu");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_lap");
            entity.Property(e => e.NguoiDuyetId).HasColumnName("nguoi_duyet_id");
            entity.Property(e => e.NguoiLapId).HasColumnName("nguoi_lap_id");
            entity.Property(e => e.PhamVi)
                .HasColumnType("enum('TOAN_BO','DANH_MUC','CHON_SAN_PHAM')")
                .HasColumnName("pham_vi");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'NHAP'")
                .HasColumnType("enum('NHAP','CHO_DUYET','BI_TU_CHOI','DA_DIEU_CHINH')")
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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("phieu_nhap");

            entity.HasIndex(e => e.NguoiHuyId, "fk_pn_nguoi_huy");

            entity.HasIndex(e => e.NguoiLapId, "fk_pn_nguoi_lap");

            entity.HasIndex(e => e.NguoiXacNhanId, "fk_pn_nguoi_xac_nhan");

            entity.HasIndex(e => new { e.NhaCungCapId, e.XacNhanLuc }, "idx_phieu_nhap_ncc");

            entity.HasIndex(e => new { e.TrangThai, e.NgayLap }, "idx_phieu_nhap_tt");

            entity.HasIndex(e => e.XacNhanLuc, "idx_phieu_nhap_xac_nhan");

            entity.HasIndex(e => e.MaPhieu, "uq_phieu_nhap_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.HuyLuc)
                .HasColumnType("datetime")
                .HasColumnName("huy_luc");
            entity.Property(e => e.LyDoHuy)
                .HasMaxLength(255)
                .HasColumnName("ly_do_huy");
            entity.Property(e => e.MaPhieu)
                .HasMaxLength(20)
                .HasColumnName("ma_phieu");
            entity.Property(e => e.NgayCapNhat)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_lap");
            entity.Property(e => e.NgayNhap)
                .HasColumnType("date")
                .HasColumnName("ngay_nhap");
            entity.Property(e => e.NguoiHuyId).HasColumnName("nguoi_huy_id");
            entity.Property(e => e.NguoiLapId).HasColumnName("nguoi_lap_id");
            entity.Property(e => e.NguoiXacNhanId).HasColumnName("nguoi_xac_nhan_id");
            entity.Property(e => e.NhaCungCapId).HasColumnName("nha_cung_cap_id");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.SoChungTuNcc)
                .HasMaxLength(50)
                .HasColumnName("so_chung_tu_ncc");
            entity.Property(e => e.TongTien).HasColumnName("tong_tien");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'NHAP'")
                .HasColumnType("enum('NHAP','DA_NHAP_KHO','DA_HUY')")
                .HasColumnName("trang_thai");
            entity.Property(e => e.XacNhanLuc)
                .HasColumnType("datetime")
                .HasColumnName("xac_nhan_luc");

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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("phieu_xuat_huy");

            entity.HasIndex(e => e.NguoiDuyetId, "fk_pxh_nguoi_duyet");

            entity.HasIndex(e => e.NguoiLapId, "fk_pxh_nguoi_lap");

            entity.HasIndex(e => e.DuyetLuc, "idx_phieu_xuat_huy_duyet");

            entity.HasIndex(e => e.TrangThai, "idx_phieu_xuat_huy_tt");

            entity.HasIndex(e => e.MaPhieu, "uq_phieu_xuat_huy_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DuyetLuc)
                .HasColumnType("datetime")
                .HasColumnName("duyet_luc");
            entity.Property(e => e.GhiChu)
                .HasMaxLength(255)
                .HasColumnName("ghi_chu");
            entity.Property(e => e.GuiDuyetLuc)
                .HasColumnType("datetime")
                .HasColumnName("gui_duyet_luc");
            entity.Property(e => e.LyDoTuChoi)
                .HasMaxLength(255)
                .HasColumnName("ly_do_tu_choi");
            entity.Property(e => e.MaPhieu)
                .HasMaxLength(20)
                .HasColumnName("ma_phieu");
            entity.Property(e => e.NgayLap)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_lap");
            entity.Property(e => e.NguoiDuyetId).HasColumnName("nguoi_duyet_id");
            entity.Property(e => e.NguoiLapId).HasColumnName("nguoi_lap_id");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'NHAP'")
                .HasColumnType("enum('NHAP','CHO_DUYET','BI_TU_CHOI','DA_XUAT_HUY')")
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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("quy_tac_tich_diem");

            entity.HasIndex(e => e.NguoiTaoId, "fk_qttd_nguoi_tao");

            entity.HasIndex(e => e.HieuLucTu, "idx_qttd_hieu_luc");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.DangBat)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("dang_bat");
            entity.Property(e => e.HieuLucTu)
                .HasColumnType("datetime")
                .HasColumnName("hieu_luc_tu");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.SoTienMotDiem)
                .HasDefaultValueSql("'10000'")
                .HasColumnName("so_tien_mot_diem");

            entity.HasOne(d => d.NguoiTao).WithMany(p => p.QuyTacTichDiems)
                .HasForeignKey(d => d.NguoiTaoId)
                .HasConstraintName("fk_qttd_nguoi_tao");
        });

        modelBuilder.Entity<SanPham>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("san_pham");

            entity.HasIndex(e => e.NguoiTaoId, "fk_san_pham_nguoi_tao");

            entity.HasIndex(e => new { e.DanhMucId, e.TrangThai }, "idx_san_pham_danh_muc");

            entity.HasIndex(e => e.TenSanPham, "idx_san_pham_ten");

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
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.TenSanPham)
                .HasMaxLength(200)
                .HasColumnName("ten_san_pham");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'DANG_KINH_DOANH'")
                .HasColumnType("enum('DANG_KINH_DOANH','NGUNG_KINH_DOANH')")
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
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("tai_khoan");

            entity.HasIndex(e => new { e.VaiTroId, e.TrangThai }, "idx_tai_khoan_vai_tro");

            entity.HasIndex(e => e.Email, "uq_tai_khoan_email").IsUnique();

            entity.HasIndex(e => e.SoDienThoai, "uq_tai_khoan_sdt").IsUnique();

            entity.HasIndex(e => e.TenDangNhap, "uq_tai_khoan_ten").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.BatBuocDoiMk)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("bat_buoc_doi_mk");
            entity.Property(e => e.Email)
                .HasMaxLength(100)
                .HasColumnName("email");
            entity.Property(e => e.HoTen)
                .HasMaxLength(100)
                .HasColumnName("ho_ten");
            entity.Property(e => e.KhoaTamDen)
                .HasColumnType("datetime")
                .HasColumnName("khoa_tam_den");
            entity.Property(e => e.LanDangNhapCuoi)
                .HasColumnType("datetime")
                .HasColumnName("lan_dang_nhap_cuoi");
            entity.Property(e => e.LyDoKhoa)
                .HasMaxLength(255)
                .HasColumnName("ly_do_khoa");
            entity.Property(e => e.MatKhauBam)
                .HasMaxLength(255)
                .HasColumnName("mat_khau_bam");
            entity.Property(e => e.NgayCapNhat)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.SoDienThoai)
                .HasMaxLength(10)
                .HasColumnName("so_dien_thoai");
            entity.Property(e => e.SoLanSai).HasColumnName("so_lan_sai");
            entity.Property(e => e.TenDangNhap)
                .HasMaxLength(50)
                .HasColumnName("ten_dang_nhap");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'HOAT_DONG'")
                .HasColumnType("enum('HOAT_DONG','DA_KHOA')")
                .HasColumnName("trang_thai");
            entity.Property(e => e.VaiTroId).HasColumnName("vai_tro_id");

            entity.HasOne(d => d.VaiTro).WithMany(p => p.TaiKhoans)
                .HasForeignKey(d => d.VaiTroId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("fk_tai_khoan_vai_tro");
        });

        modelBuilder.Entity<TheKho>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("the_kho");

            entity.HasIndex(e => e.NguoiThucHienId, "fk_the_kho_nguoi");

            entity.HasIndex(e => new { e.LoaiBienDong, e.ChungTuId }, "idx_the_kho_chung_tu");

            entity.HasIndex(e => new { e.SanPhamId, e.ThoiDiem }, "idx_the_kho_sp_tg");

            entity.HasIndex(e => e.ThoiDiem, "idx_the_kho_tg");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChungTuId).HasColumnName("chung_tu_id");
            entity.Property(e => e.GiaVonApDung)
                .HasPrecision(15)
                .HasColumnName("gia_von_ap_dung");
            entity.Property(e => e.GiaVonSau)
                .HasPrecision(15)
                .HasColumnName("gia_von_sau");
            entity.Property(e => e.LoaiBienDong)
                .HasColumnType("enum('NHAP_HANG','BAN_HANG','HUY_HOA_DON','TRA_HANG','KIEM_KE','XUAT_HUY')")
                .HasColumnName("loai_bien_dong");
            entity.Property(e => e.MaChungTu)
                .HasMaxLength(20)
                .HasColumnName("ma_chung_tu");
            entity.Property(e => e.NguoiThucHienId).HasColumnName("nguoi_thuc_hien_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.SoLuongThayDoi).HasColumnName("so_luong_thay_doi");
            entity.Property(e => e.ThoiDiem)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
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
            entity.HasKey(e => e.SanPhamId).HasName("PRIMARY");

            entity.ToTable("ton_kho");

            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
            entity.Property(e => e.GiaVonBinhQuan)
                .HasPrecision(15)
                .HasColumnName("gia_von_binh_quan");
            entity.Property(e => e.NgayCapNhat)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NguongToiThieu)
                .HasDefaultValueSql("'5'")
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
                .ToView("v_chuong_trinh_khuyen_mai");

            entity.Property(e => e.BatDauLuc)
                .HasColumnType("datetime")
                .HasColumnName("bat_dau_luc");
            entity.Property(e => e.ChiThanhVien).HasColumnName("chi_thanh_vien");
            entity.Property(e => e.GiaTri).HasColumnName("gia_tri");
            entity.Property(e => e.GiamToiDa).HasColumnName("giam_toi_da");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.KetThucLuc)
                .HasColumnType("datetime")
                .HasColumnName("ket_thuc_luc");
            entity.Property(e => e.LoaiGiam)
                .HasColumnType("enum('PHAN_TRAM','SO_TIEN')")
                .HasColumnName("loai_giam");
            entity.Property(e => e.Ma)
                .HasMaxLength(20)
                .HasColumnName("ma");
            entity.Property(e => e.MoTa)
                .HasMaxLength(500)
                .HasColumnName("mo_ta");
            entity.Property(e => e.NgayCapNhat)
                .ValueGeneratedOnAddOrUpdate()
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_cap_nhat");
            entity.Property(e => e.NgayTao)
                .HasDefaultValueSql("CURRENT_TIMESTAMP")
                .HasColumnType("datetime")
                .HasColumnName("ngay_tao");
            entity.Property(e => e.NguoiTaoId).HasColumnName("nguoi_tao_id");
            entity.Property(e => e.PhienBan)
                .HasDefaultValueSql("'1'")
                .HasColumnName("phien_ban");
            entity.Property(e => e.Ten)
                .HasMaxLength(200)
                .HasColumnName("ten");
            entity.Property(e => e.TrangThai)
                .HasDefaultValueSql("'NHAP'")
                .HasColumnType("enum('NHAP','DA_KICH_HOAT','TAM_DUNG','DA_KET_THUC')")
                .HasColumnName("trang_thai");
            entity.Property(e => e.TrangThaiHienThi)
                .HasMaxLength(11)
                .HasDefaultValueSql("''")
                .HasColumnName("trang_thai_hien_thi");
        });

        modelBuilder.Entity<VGiaBanHienHanh>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_gia_ban_hien_hanh");

            entity.Property(e => e.GiaBan).HasColumnName("gia_ban");
            entity.Property(e => e.HieuLucTu)
                .HasColumnType("datetime")
                .HasColumnName("hieu_luc_tu");
            entity.Property(e => e.LichSuGiaId).HasColumnName("lich_su_gia_id");
            entity.Property(e => e.SanPhamId).HasColumnName("san_pham_id");
        });

        modelBuilder.Entity<VLuotDungKhuyenMai>(entity =>
        {
            entity
                .HasNoKey()
                .ToView("v_luot_dung_khuyen_mai");

            entity.Property(e => e.ChuongTrinhId).HasColumnName("chuong_trinh_id");
            entity.Property(e => e.HoaDonId).HasColumnName("hoa_don_id");
            entity.Property(e => e.KhachHangId).HasColumnName("khach_hang_id");
            entity.Property(e => e.MaHoaDon)
                .HasMaxLength(20)
                .HasColumnName("ma_hoa_don");
            entity.Property(e => e.SoLuongSanPham)
                .HasPrecision(32)
                .HasColumnName("so_luong_san_pham");
            entity.Property(e => e.ThanhToanLuc)
                .HasColumnType("datetime")
                .HasColumnName("thanh_toan_luc");
            entity.Property(e => e.ThuNganId).HasColumnName("thu_ngan_id");
            entity.Property(e => e.TienGiam)
                .HasPrecision(42)
                .HasColumnName("tien_giam");
        });

        modelBuilder.Entity<VaiTro>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("vai_tro");

            entity.HasIndex(e => e.Ma, "uq_vai_tro_ma").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Ma)
                .HasMaxLength(2)
                .IsFixedLength()
                .HasColumnName("ma");
            entity.Property(e => e.Ten)
                .HasMaxLength(50)
                .HasColumnName("ten");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
