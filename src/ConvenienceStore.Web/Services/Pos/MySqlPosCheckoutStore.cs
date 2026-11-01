
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Modules.Inventory.Services;
using ConvenienceStore.Web.Shared.Data;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ConvenienceStore.Web.Services.Pos;

/// <summary>
/// POS-07: Lưu hóa đơn, trừ kho, tích điểm và hoàn tất giỏ.
/// Mọi thao tác ghi đều chạy trong transaction của PosCheckoutService.
/// </summary>
public sealed class MySqlPosCheckoutStore(
    CuaHangDbContext db,
    IDichVuKho kho,
    ILogger<MySqlPosCheckoutStore> log) : IPosCheckoutStore
{
    private sealed record GioDangBan(
        int Id,
        int? KhachHangId);

    private sealed record DongGio(
        int SanPhamId,
        string Ten,
        string DonVi,
        int SoLuong,
        long DonGia,
        int? KhuyenMaiId,
        long GiamDonVi,
        string TrangThaiSanPham);

    private sealed record KhachHang(
        long DiemHienCo,
        string TrangThai);

    private sealed record QuyTacDiem(
        int SoTienMotDiem,
        bool DangBat);

    // =====================================================
    // POS-07: HOÀN TẤT ĐƠN HÀNG
    // =====================================================

    public async Task<PosOrderSaved> SaveOrderAsync(
        int cashierId,
        long cashReceived,
        CancellationToken cancellationToken)
    {
        // 1. Bắt buộc có transaction từ PosCheckoutService.
        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "SaveOrderAsync phải chạy trong transaction.");
        }

        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(
            cashierId);

        if (cashReceived < 0)
        {
            throw new PosCheckoutException(
                "Tiền khách đưa không hợp lệ.");
        }

        // 2. Khóa giỏ hàng đang bán.
        var gio = await db.DocMotDongAsync(
            """
            SELECT id, khach_hang_id
            FROM gio_hang
            WHERE thu_ngan_id = @t
              AND trang_thai = 'DANG_BAN'
            FOR UPDATE
            """,
            d => new GioDangBan(
                d.SoNguyen(0),
                d.IsDBNull(1) ? null : d.SoNguyen(1)),
            cancellationToken,
            ("@t", cashierId));

        if (gio is null)
        {
            throw new PosCheckoutException(
                "Không có đơn đang bán để thanh toán.");
        }

        // 3. Đọc lại giỏ hàng trực tiếp từ database.
        var dong = await db.DocDanhSachAsync(
            """
            SELECT
                ct.san_pham_id,
                sp.ten_san_pham,
                sp.don_vi_tinh,
                ct.so_luong,
                ct.don_gia,
                ct.khuyen_mai_id,
                ct.giam_don_vi,
                sp.trang_thai
            FROM ct_gio_hang ct
            JOIN san_pham sp
                ON sp.id = ct.san_pham_id
            WHERE ct.gio_hang_id = @g
            ORDER BY ct.san_pham_id
            """,
            DocDong,
            cancellationToken,
            ("@g", gio.Id));

        if (dong.Count == 0)
        {
            throw new PosCheckoutException(
                "Giỏ hàng đang trống.");
        }

        KiemTraDong(dong);

        // 4. E2: Kiểm tra khuyến mãi trước mọi thao tác ghi.
        // Nếu có khuyến mãi không hợp lệ, phương thức sẽ
        // ném PosCheckoutException để Service rollback.
        await KiemTraKhuyenMaiAsync(
            dong,
            gio.KhachHangId is not null,
            cancellationToken);

        // 5. Tính lại tiền từ dữ liệu trong database.
        long tongTienHang = 0;
        long tongGiam = 0;

        checked
        {
            foreach (var d in dong)
            {
                tongTienHang +=
                    (long)d.SoLuong * d.DonGia;

                tongGiam +=
                    (long)d.SoLuong * d.GiamDonVi;
            }
        }

        long tongThanhToan = checked(
            tongTienHang - tongGiam);

        if (tongThanhToan <= 0)
        {
            throw new PosCheckoutException(
                "Tổng tiền thanh toán phải lớn hơn 0.");
        }

        if (cashReceived < tongThanhToan)
        {
            throw new PosCheckoutException(
                $"Khách đưa chưa đủ. Còn thiếu " +
                $"{tongThanhToan - cashReceived:N0} đ.");
        }

        long tienThua = cashReceived - tongThanhToan;

        // 6. Lấy thời gian database và cấp mã hóa đơn.
        var bayGio = Convert.ToDateTime(
            await db.DocMotGiaTriAsync(
                "SELECT NOW()",
                cancellationToken));

        var ky = bayGio.ToString(
            "yyyyMMdd",
            CultureInfo.InvariantCulture);

        var maHoaDon = await CapMaHoaDonAsync(
            ky,
            cancellationToken);

        // 7. Khóa khách hàng và tính điểm.
        KhachHang? khach = null;
        int? soTienMotDiem = null;
        int diemCong = 0;

        if (gio.KhachHangId is int khachId)
        {
            khach = await db.DocMotDongAsync(
                """
                SELECT diem_hien_co, trang_thai
                FROM khach_hang
                WHERE id = @k
                FOR UPDATE
                """,
                d => new KhachHang(
                    d.SoNguyenDai(0),
                    d.Chuoi(1)),
                cancellationToken,
                ("@k", khachId));

            if (khach is null ||
                khach.TrangThai != "HOAT_DONG")
            {
                throw new PosCheckoutException(
                    "Khách hàng không còn hoạt động.");
            }

            var quyTac = await db.DocMotDongAsync(
                """
                SELECT so_tien_mot_diem, dang_bat
                FROM quy_tac_tich_diem
                WHERE hieu_luc_tu <= NOW()
                ORDER BY hieu_luc_tu DESC, id DESC
                LIMIT 1
                """,
                d => new QuyTacDiem(
                    d.SoNguyen(0),
                    d.DungSai(1)),
                cancellationToken);

            if (quyTac is { DangBat: true })
            {
                if (quyTac.SoTienMotDiem <= 0)
                {
                    throw new InvalidOperationException(
                        "Quy tắc tích điểm không hợp lệ.");
                }

                soTienMotDiem = quyTac.SoTienMotDiem;

                diemCong = checked((int)(
                    tongThanhToan /
                    quyTac.SoTienMotDiem));
            }
        }

        // 8. Lưu hóa đơn.
        var hoaDonId = Convert.ToInt64(
            await db.DocMotGiaTriAsync(
                """
                INSERT INTO hoa_don
                (
                    ma_hoa_don,
                    gio_hang_id,
                    thu_ngan_id,
                    khach_hang_id,
                    thanh_toan_luc,
                    tong_tien_hang,
                    tong_giam,
                    tong_thanh_toan,
                    tien_tru_hang_doi,
                    tien_khach_dua,
                    tien_thua,
                    so_tien_mot_diem,
                    diem_cong
                )
                VALUES
                (
                    @ma, @g, @t, @kh, @tg,
                    @tth, @giam, @ttt, 0,
                    @khach, @thua, @stmd, @diem
                );
                SELECT LAST_INSERT_ID();
                """,
                cancellationToken,
                ("@ma", maHoaDon),
                ("@g", gio.Id),
                ("@t", cashierId),
                ("@kh", gio.KhachHangId),
                ("@tg", bayGio),
                ("@tth", tongTienHang),
                ("@giam", tongGiam),
                ("@ttt", tongThanhToan),
                ("@khach", cashReceived),
                ("@thua", tienThua),
                ("@stmd", soTienMotDiem),
                ("@diem", diemCong)));

        if (hoaDonId <= 0)
        {
            throw new InvalidOperationException(
                "Không lấy được ID hóa đơn vừa tạo.");
        }

        // 9. E1: Kiểm tra và trừ tồn kho nguyên tử.
        // Nếu thiếu tồn, IDichVuKho sẽ ném lỗi.
        // PosCheckoutService sẽ rollback cả hóa đơn.
        var ketQuaKho = await kho.GhiNhanBienDongAsync(
            new YeuCauBienDong(
                LoaiBienDong.BAN_HANG,
                checked((int)hoaDonId),
                maHoaDon,
                cashierId,
                dong.Select(x =>
                    new DongBienDong(
                        x.SanPhamId,
                        x.SoLuong)).ToList()),
            cancellationToken);

        var giaVonTheoSanPham = ketQuaKho.Dong
            .ToDictionary(
                x => x.SanPhamId,
                x => x.GiaVonApDung);

        foreach (var d in dong)
        {
            if (!giaVonTheoSanPham.ContainsKey(
                d.SanPhamId))
            {
                throw new InvalidOperationException(
                    $"Thiếu thông tin giá vốn của {d.Ten}.");
            }
        }

        // 10. Lưu chi tiết hóa đơn.
        foreach (var d in dong)
        {
            await db.ChayAsync(
                """
                INSERT INTO ct_hoa_don
                (
                    hoa_don_id,
                    san_pham_id,
                    ten_san_pham,
                    don_vi_tinh,
                    so_luong,
                    don_gia,
                    khuyen_mai_id,
                    giam_don_vi,
                    gia_von
                )
                VALUES
                (
                    @hd, @sp, @ten, @dvt, @sl,
                    @gia, @km, @giam, @von
                )
                """,
                cancellationToken,
                ("@hd", hoaDonId),
                ("@sp", d.SanPhamId),
                ("@ten", d.Ten),
                ("@dvt", d.DonVi),
                ("@sl", d.SoLuong),
                ("@gia", d.DonGia),
                ("@km", d.KhuyenMaiId),
                ("@giam", d.GiamDonVi),
                ("@von",
                    giaVonTheoSanPham[d.SanPhamId]));
        }

        // 11. Tích điểm khách hàng.
        if (khach is not null && diemCong > 0)
        {
            long diemSau = checked(
                khach.DiemHienCo + diemCong);

            var soDongCapNhat = await db.ChayAsync(
                """
                UPDATE khach_hang
                SET diem_hien_co = @sau,
                    phien_ban = phien_ban + 1
                WHERE id = @k
                """,
                cancellationToken,
                ("@sau", diemSau),
                ("@k", gio.KhachHangId));

            if (soDongCapNhat != 1)
            {
                throw new InvalidOperationException(
                    "Không cập nhật được điểm khách hàng.");
            }

            await db.ChayAsync(
                """
                INSERT INTO lich_su_diem
                (
                    khach_hang_id,
                    loai,
                    so_diem,
                    diem_sau,
                    hoa_don_id,
                    nguoi_thuc_hien_id
                )
                VALUES
                (
                    @k, 'TICH_DIEM',
                    @diem, @sau, @hd, @t
                )
                """,
                cancellationToken,
                ("@k", gio.KhachHangId),
                ("@diem", diemCong),
                ("@sau", diemSau),
                ("@hd", hoaDonId),
                ("@t", cashierId));
        }

        // 12. Hoàn tất giỏ hàng.
        var daChot = await db.ChayAsync(
            """
            UPDATE gio_hang
            SET trang_thai = 'DA_HOAN_TAT',
                hoan_tat_luc = @tg,
                phien_ban = phien_ban + 1
            WHERE id = @g
              AND trang_thai = 'DANG_BAN'
            """,
            cancellationToken,
            ("@tg", bayGio),
            ("@g", gio.Id));

        if (daChot != 1)
        {
            throw new InvalidOperationException(
                "Không thể hoàn tất đơn hàng.");
        }

        log.LogInformation(
            "POS-07: Đã ghi hóa đơn {Ma}, ID {Id}, " +
            "tổng tiền {Tong}. Đang chờ commit.",
            maHoaDon,
            hoaDonId,
            tongThanhToan);

        // Không commit ở đây.
        return new PosOrderSaved(
            hoaDonId,
            maHoaDon,
            tongThanhToan,
            cashReceived,
            tienThua,
            diemCong);
    }

    // =====================================================
    // CẤP MÃ HÓA ĐƠN
    // =====================================================

    private async Task<string> CapMaHoaDonAsync(
        string ky,
        CancellationToken ct)
    {
        await db.ChayAsync(
            """
            INSERT INTO bo_dem_chung_tu
                (tien_to, ky, so_cuoi)
            VALUES ('HD', @ky, 1)
            ON DUPLICATE KEY UPDATE
                so_cuoi = so_cuoi + 1
            """,
            ct,
            ("@ky", ky));

        var soCuoi = Convert.ToInt32(
            await db.DocMotGiaTriAsync(
                """
                SELECT so_cuoi
                FROM bo_dem_chung_tu
                WHERE tien_to = 'HD'
                  AND ky = @ky
                """,
                ct,
                ("@ky", ky)));

        return $"HD-{ky}-{soCuoi:D4}";
    }

    // =====================================================
    // ĐỌC DÒNG GIỎ HÀNG
    // =====================================================

    private static DongGio DocDong(DbDataReader d)
    {
        return new DongGio(
            d.SoNguyen(0),
            d.Chuoi(1),
            d.Chuoi(2),
            d.SoNguyen(3),
            d.SoNguyenDai(4),
            d.IsDBNull(5) ? null : d.SoNguyen(5),
            d.SoNguyenDai(6),
            d.Chuoi(7));
    }

    // =====================================================
    // KIỂM TRA SẢN PHẨM
    // =====================================================

    private static void KiemTraDong(
        IReadOnlyList<DongGio> dong)
    {
        foreach (var d in dong)
        {
            if (d.TrangThaiSanPham !=
                "DANG_KINH_DOANH")
            {
                throw new PosCheckoutException(
                    $"Sản phẩm {d.Ten} không còn kinh doanh.");
            }

            if (d.SoLuong <= 0)
            {
                throw new PosCheckoutException(
                    $"Số lượng của {d.Ten} không hợp lệ.");
            }

            if (d.DonGia <= 0 ||
                d.DonGia % 100 != 0)
            {
                throw new PosCheckoutException(
                    $"Giá của {d.Ten} không hợp lệ.");
            }

            if (d.GiamDonVi < 0 ||
                d.GiamDonVi > d.DonGia ||
                d.GiamDonVi % 100 != 0)
            {
                throw new PosCheckoutException(
                    $"Mức giảm của {d.Ten} không hợp lệ.");
            }
        }
    }

    // =====================================================
    // E2: KIỂM TRA KHUYẾN MÃI
    // =====================================================

    private async Task KiemTraKhuyenMaiAsync(
        IReadOnlyList<DongGio> dong,
        bool coKhach,
        CancellationToken ct)
    {
        var ids = dong
            .Where(d => d.KhuyenMaiId.HasValue)
            .Select(d => d.KhuyenMaiId!.Value)
            .Distinct()
            .OrderBy(id => id)
            .ToList();

        if (ids.Count == 0)
        {
            return;
        }

        var (danhSach, thamSoId) =
            TruyVanCsdl.TaoThamSoIn("km", ids);

        var thamSo = thamSoId
            .Append((
                "@coKhach",
                (object?)(coKhach ? 1 : 0)))
            .ToArray();

        // Lấy danh sách khuyến mãi đang hợp lệ.
        var idsHopLe = await db.DocDanhSachAsync(
            $"""
            SELECT id
            FROM v_chuong_trinh_khuyen_mai
            WHERE id IN ({danhSach})
              AND trang_thai_hien_thi = 'DANG_CHAY'
              AND (
                  chi_thanh_vien = FALSE
                  OR @coKhach = 1
              )
            """,
            d => d.SoNguyen(0),
            ct,
            thamSo);

        var tapHopHopLe = idsHopLe.ToHashSet();

        var dongKhongHopLe = dong
            .Where(d =>
                d.KhuyenMaiId.HasValue &&
                !tapHopHopLe.Contains(
                    d.KhuyenMaiId.Value))
            .ToList();

        if (dongKhongHopLe.Count == 0)
        {
            return;
        }

        long tongCu = 0;
        long tongMoi = 0;

        checked
        {
            foreach (var d in dong)
            {
                long tienHang =
                    (long)d.SoLuong * d.DonGia;

                long giamCu =
                    (long)d.SoLuong * d.GiamDonVi;

                tongCu += tienHang - giamCu;

                bool khuyenMaiHopLe =
                    !d.KhuyenMaiId.HasValue ||
                    tapHopHopLe.Contains(
                        d.KhuyenMaiId.Value);

                long giamMoi = khuyenMaiHopLe
                    ? giamCu
                    : 0;

                tongMoi += tienHang - giamMoi;
            }
        }

        var tenSanPham = string.Join(
            ", ",
            dongKhongHopLe
                .Select(d => d.Ten)
                .Distinct());

        log.LogWarning(
            "POS-07 E2: Khuyến mãi không còn hợp lệ. " +
            "Sản phẩm: {Products}. " +
            "Tổng cũ: {OldTotal}. " +
            "Tổng mới dự kiến: {NewTotal}",
            tenSanPham,
            tongCu,
            tongMoi);

        // Chưa cập nhật giỏ trong transaction này.
        // Nếu cập nhật rồi throw, toàn bộ cập nhật
        // sẽ bị rollback bởi PosCheckoutService.
        throw new PosCheckoutException(
            "Khuyến mãi đã hết hạn hoặc bị tạm dừng " +
            $"đối với sản phẩm: {tenSanPham}. " +
            $"Tổng tiền ban đầu: {tongCu:N0} đ. " +
            $"Tổng tiền mới dự kiến: {tongMoi:N0} đ. " +
            "Đơn hàng chưa được thanh toán. " +
            "Vui lòng kiểm tra và xác nhận lại giá mới.");
    }
}
