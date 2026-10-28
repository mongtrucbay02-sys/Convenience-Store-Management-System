
using System.Data.Common;
using ConvenienceStore.Web.Data;
using ConvenienceStore.Web.Modules.Inventory.Models;
using ConvenienceStore.Web.Shared.Data;
using ConvenienceStore.Web.Shared.Errors;

namespace ConvenienceStore.Web.Modules.Inventory.Services;

public sealed class DichVuKho(
    CuaHangDbContext db,
    ILogger<DichVuKho> log) : IDichVuKho
{
    private const string SqlDemDuoiNguong =
        "SELECT COUNT(*) FROM ton_kho " +
        "WHERE nguong_toi_thieu > 0 " +
        "AND so_luong_ton <= nguong_toi_thieu";

    // =====================================================
    // GHI NHẬN BIẾN ĐỘNG KHO - E1
    // =====================================================

    public async Task<KetQuaBienDong> GhiNhanBienDongAsync(
        YeuCauBienDong yeuCau,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(yeuCau);

        if (db.Database.CurrentTransaction is null)
        {
            throw new InvalidOperationException(
                "Dịch vụ kho phải chạy trong giao tác " +
                "do bên gọi mở bằng Database.BeginTransactionAsync().");
        }

        KiemTraYeuCau(yeuCau);

        try
        {
            // Khóa các dòng tồn kho trong transaction
            var tonTruoc = await KhoaTonKhoAsync(
                yeuCau.Dong.Select(d => d.SanPhamId),
                ct);

            var chuaCoTonKho = yeuCau.Dong
                .Select(d => d.SanPhamId)
                .Where(id => !tonTruoc.ContainsKey(id))
                .ToList();

            if (chuaCoTonKho.Count > 0)
            {
                throw LoiDichVuKho.SanPhamKhongTonTai(
                    chuaCoTonKho);
            }

            // Tính biến động dựa trên tồn đã khóa
            var thayDoi = yeuCau.Dong
                .Select(d => (
                    Dong: d,
                    Truoc: tonTruoc[d.SanPhamId],
                    SoLuong: TinhToanKho.SoLuongThayDoi(
                        yeuCau.Loai,
                        d,
                        tonTruoc[d.SanPhamId].SoLuongTon)
                ))
                .ToList();

            // E1: Kiểm tra thiếu tồn trước khi cập nhật
            var thieuTon = thayDoi
                .Where(t =>
                    (long)t.Truoc.SoLuongTon + t.SoLuong < 0)
                .Select(t => new ThieuTon(
                    t.Dong.SanPhamId,
                    t.Truoc.SoLuongTon,
                    t.Dong.SoLuong ?? 0))
                .ToList();

            if (thieuTon.Count > 0)
            {
                throw await TaoLoiTonKhongDuAsync(
                    thieuTon,
                    ct);
            }

            var ketQua = new List<KetQuaDong>(
                thayDoi.Count);

            foreach (var (dong, truoc, soLuong) in thayDoi)
            {
                if (soLuong == 0)
                {
                    ketQua.Add(new KetQuaDong(
                        dong.SanPhamId,
                        0,
                        truoc.SoLuongTon,
                        truoc.SoLuongTon,
                        truoc.GiaVonBinhQuan,
                        truoc.GiaVonBinhQuan,
                        truoc.GiaVonBinhQuan,
                        null));

                    continue;
                }

                var tonSau = checked(
                    truoc.SoLuongTon + soLuong);

                var (giaVonSau, giaVonApDung) =
                    TinhToanKho.GiaVon(
                        yeuCau.Loai,
                        truoc.SoLuongTon,
                        truoc.GiaVonBinhQuan,
                        soLuong,
                        dong.GiaVon);

                // Cập nhật có kiểm tra tồn trước đó.
                // FOR UPDATE phía trên vẫn là khóa chính
                // để tránh hai giao dịch cùng bán vượt tồn.
                var soDongCapNhat = await db.ChayAsync(
                    """
                    UPDATE ton_kho
                    SET so_luong_ton = @ton,
                        gia_von_binh_quan = @giaVon
                    WHERE san_pham_id = @sp
                      AND so_luong_ton = @tonTruoc
                    """,
                    ct,
                    ("@ton", tonSau),
                    ("@giaVon", giaVonSau),
                    ("@sp", dong.SanPhamId),
                    ("@tonTruoc", truoc.SoLuongTon));

                if (soDongCapNhat != 1)
                {
                    throw new InvalidOperationException(
                        "Không cập nhật được tồn kho sản phẩm " +
                        $"{dong.SanPhamId}. " +
                        "Giao dịch cần được hoàn tác.");
                }

                // Ghi thẻ kho trong cùng transaction
                var theKhoId = Convert.ToInt64(
                    await db.DocMotGiaTriAsync(
                        """
                        INSERT INTO the_kho
                        (
                            san_pham_id,
                            loai_bien_dong,
                            ma_chung_tu,
                            chung_tu_id,
                            so_luong_thay_doi,
                            ton_sau,
                            gia_von_ap_dung,
                            gia_von_sau,
                            nguoi_thuc_hien_id
                        )
                        VALUES
                        (
                            @sp,
                            @loai,
                            @maChungTu,
                            @chungTuId,
                            @soLuong,
                            @tonSau,
                            @giaVonApDung,
                            @giaVonSau,
                            @nguoi
                        );
                        SELECT LAST_INSERT_ID();
                        """,
                        ct,
                        ("@sp", dong.SanPhamId),
                        ("@loai", yeuCau.Loai.ToString()),
                        ("@maChungTu",
                            yeuCau.MaChungTu.Trim()),
                        ("@chungTuId", yeuCau.ChungTuId),
                        ("@soLuong", soLuong),
                        ("@tonSau", tonSau),
                        ("@giaVonApDung", giaVonApDung),
                        ("@giaVonSau", giaVonSau),
                        ("@nguoi", yeuCau.NguoiThucHienId)));

                if (theKhoId <= 0)
                {
                    throw new InvalidOperationException(
                        "Không lấy được mã thẻ kho vừa ghi.");
                }

                ketQua.Add(new KetQuaDong(
                    dong.SanPhamId,
                    soLuong,
                    truoc.SoLuongTon,
                    tonSau,
                    truoc.GiaVonBinhQuan,
                    giaVonSau,
                    giaVonApDung,
                    theKhoId));
            }

            var soDuoiNguong = Convert.ToInt32(
                await db.DocMotGiaTriAsync(
                    SqlDemDuoiNguong,
                    ct));

            // Không commit tại đây.
            // PosCheckoutService hoặc dịch vụ gọi sẽ commit.
            return new KetQuaBienDong(
                ketQua,
                soDuoiNguong);
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (DbException loi)
        {
            log.LogError(
                loi,
                "Ghi biến động kho {Loai} " +
                "cho chứng từ {MaChungTu} thất bại",
                yeuCau.Loai,
                yeuCau.MaChungTu);

            throw LoiDichVuKho.LoiGhiDuLieu(loi);
        }
    }

    // =====================================================
    // KHỞI TẠO TỒN KHO
    // =====================================================

    public async Task KhoiTaoTonKhoAsync(
        int sanPhamId,
        int nguongToiThieu = 5,
        CancellationToken ct = default)
    {
        var loi = new List<LoiTruong>();

        if (sanPhamId <= 0)
        {
            loi.Add(new LoiTruong(
                "sanPhamId",
                "Mã sản phẩm không hợp lệ."));
        }

        if (nguongToiThieu < 0)
        {
            loi.Add(new LoiTruong(
                "nguongToiThieu",
                "Ngưỡng tối thiểu không được âm."));
        }

        if (loi.Count > 0)
        {
            throw LoiDichVuKho.KhongHopLe(loi);
        }

        try
        {
            var coSanPham = Convert.ToInt32(
                await db.DocMotGiaTriAsync(
                    """
                    SELECT COUNT(*)
                    FROM san_pham
                    WHERE id = @sp
                    """,
                    ct,
                    ("@sp", sanPhamId))) > 0;

            if (!coSanPham)
            {
                throw LoiDichVuKho.SanPhamKhongTonTai(
                    [sanPhamId]);
            }

            var daCoTonKho = Convert.ToInt32(
                await db.DocMotGiaTriAsync(
                    """
                    SELECT COUNT(*)
                    FROM ton_kho
                    WHERE san_pham_id = @sp
                    """,
                    ct,
                    ("@sp", sanPhamId))) > 0;

            if (daCoTonKho)
            {
                throw LoiDichVuKho.KhongHopLe(
                    [
                        new LoiTruong(
                            "sanPhamId",
                            "Sản phẩm đã có bản ghi tồn kho.")
                    ]);
            }

            await db.ChayAsync(
                """
                INSERT INTO ton_kho
                (
                    san_pham_id,
                    so_luong_ton,
                    gia_von_binh_quan,
                    nguong_toi_thieu
                )
                VALUES (@sp, 0, 0, @nguong)
                """,
                ct,
                ("@sp", sanPhamId),
                ("@nguong", nguongToiThieu));
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (DbException loiCsdl)
        {
            log.LogError(
                loiCsdl,
                "Khởi tạo tồn kho cho sản phẩm " +
                "{SanPhamId} thất bại",
                sanPhamId);

            throw LoiDichVuKho.LoiGhiDuLieu(
                loiCsdl);
        }
    }

    // =====================================================
    // LẤY TỒN KHO HIỆN TẠI
    // =====================================================

    public async Task<IReadOnlyList<TonKhoHienTai>>
        LayTonKhoAsync(
            IReadOnlyCollection<int> sanPhamIds,
            CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(
            sanPhamIds);

        var ids = sanPhamIds
            .Where(id => id > 0)
            .Distinct()
            .Order()
            .ToList();

        if (ids.Count == 0)
        {
            return [];
        }

        var (danhSach, thamSo) =
            TruyVanCsdl.TaoThamSoIn("sp", ids);

        return await db.DocDanhSachAsync(
            $"""
            SELECT
                san_pham_id,
                so_luong_ton,
                gia_von_binh_quan,
                nguong_toi_thieu
            FROM ton_kho
            WHERE san_pham_id IN ({danhSach})
            ORDER BY san_pham_id
            """,
            DocTonKho,
            ct,
            thamSo);
    }

    public async Task<int> DemSanPhamDuoiNguongAsync(
        CancellationToken ct = default)
    {
        return Convert.ToInt32(
            await db.DocMotGiaTriAsync(
                SqlDemDuoiNguong,
                ct));
    }

    // =====================================================
    // KHÓA TỒN KHO - CHỐNG BÁN VƯỢT TỒN
    // =====================================================

    private async Task<Dictionary<int, TonKhoHienTai>>
        KhoaTonKhoAsync(
            IEnumerable<int> sanPhamIds,
            CancellationToken ct)
    {
        var ids = sanPhamIds
            .Distinct()
            .Order()
            .ToList();

        var (danhSach, thamSo) =
            TruyVanCsdl.TaoThamSoIn("sp", ids);

        var ton = await db.DocDanhSachAsync(
            $"""
            SELECT
                san_pham_id,
                so_luong_ton,
                gia_von_binh_quan,
                nguong_toi_thieu
            FROM ton_kho
            WHERE san_pham_id IN ({danhSach})
            ORDER BY san_pham_id
            FOR UPDATE
            """,
            DocTonKho,
            ct,
            thamSo);

        return ton.ToDictionary(
            t => t.SanPhamId);
    }

    // =====================================================
    // E1: THÔNG BÁO THIẾU TỒN KHO
    // =====================================================

    private async Task<LoiDichVuKho>
        TaoLoiTonKhongDuAsync(
            IReadOnlyList<ThieuTon> thieuTon,
            CancellationToken ct)
    {
        var thongBao = new List<string>();

        foreach (var dong in thieuTon)
        {
            var ten = Convert.ToString(
                await db.DocMotGiaTriAsync(
                    """
                    SELECT ten_san_pham
                    FROM san_pham
                    WHERE id = @sp
                    """,
                    ct,
                    ("@sp", dong.SanPhamId)))
                ?? $"Sản phẩm #{dong.SanPhamId}";

            // Chỉ sử dụng thuộc tính đã xác minh
            // trong code gốc: TonHienTai.
            thongBao.Add(
                $"{ten}: chỉ còn {dong.TonHienTai}");
        }

        var noiDung =
            "Không đủ tồn kho để thanh toán. " +
            string.Join("; ", thongBao) +
            ". Vui lòng quay lại giỏ hàng " +
            "và điều chỉnh số lượng.";

        return LoiDichVuKho.TonKhongDu(
            noiDung,
            thieuTon);
    }

    // =====================================================
    // KIỂM TRA YÊU CẦU BIẾN ĐỘNG KHO
    // =====================================================

    private static void KiemTraYeuCau(
        YeuCauBienDong yeuCau)
    {
        var loi = new List<LoiTruong>();

        if (!Enum.IsDefined(yeuCau.Loai))
        {
            loi.Add(new LoiTruong(
                "loai",
                "Loại biến động không hợp lệ."));
        }
        else if (!TinhToanKho.LoaiDangHoTro.Contains(
                     yeuCau.Loai))
        {
            loi.Add(new LoiTruong(
                "loai",
                $"Loại biến động {yeuCau.Loai} " +
                "chưa được hỗ trợ."));
        }

        if (yeuCau.ChungTuId <= 0)
        {
            loi.Add(new LoiTruong(
                "chungTuId",
                "Mã định danh chứng từ phải lớn hơn 0."));
        }

        if (string.IsNullOrWhiteSpace(
                yeuCau.MaChungTu) ||
            yeuCau.MaChungTu.Trim().Length > 20)
        {
            loi.Add(new LoiTruong(
                "maChungTu",
                "Mã chứng từ bắt buộc, tối đa 20 ký tự."));
        }

        if (yeuCau.NguoiThucHienId <= 0)
        {
            loi.Add(new LoiTruong(
                "nguoiThucHienId",
                "Người thực hiện không hợp lệ."));
        }

        if (yeuCau.Dong is null ||
            yeuCau.Dong.Count == 0)
        {
            loi.Add(new LoiTruong(
                "dong",
                "Danh sách dòng không được rỗng."));

            throw LoiDichVuKho.KhongHopLe(loi);
        }

        var daGap = new HashSet<int>();

        for (var i = 0; i < yeuCau.Dong.Count; i++)
        {
            var dong = yeuCau.Dong[i];

            if (dong is null)
            {
                loi.Add(new LoiTruong(
                    $"dong[{i}]",
                    "Dòng không được rỗng."));

                continue;
            }

            if (dong.SanPhamId <= 0)
            {
                loi.Add(new LoiTruong(
                    $"dong[{i}].sanPhamId",
                    "Mã sản phẩm không hợp lệ."));
            }
            else if (!daGap.Add(dong.SanPhamId))
            {
                loi.Add(new LoiTruong(
                    $"dong[{i}].sanPhamId",
                    "Mỗi sản phẩm chỉ xuất hiện một lần."));
            }

            if (yeuCau.Loai == LoaiBienDong.KIEM_KE)
            {
                if (dong.SoLuongThucTe is null or < 0)
                {
                    loi.Add(new LoiTruong(
                        $"dong[{i}].soLuongThucTe",
                        "Số lượng thực tế phải từ 0 trở lên."));
                }
            }
            else if (dong.SoLuong is null or <= 0)
            {
                loi.Add(new LoiTruong(
                    $"dong[{i}].soLuong",
                    "Số lượng phải lớn hơn 0."));
            }

            if (TinhToanKho.CanGiaVon(
                    yeuCau.Loai) &&
                dong.GiaVon is null or < 0)
            {
                loi.Add(new LoiTruong(
                    $"dong[{i}].giaVon",
                    "Giá vốn bắt buộc và không được âm."));
            }
        }

        if (loi.Count > 0)
        {
            throw LoiDichVuKho.KhongHopLe(loi);
        }
    }

    // =====================================================
    // ÁNH XẠ DỮ LIỆU TỒN KHO
    // =====================================================

    private static TonKhoHienTai DocTonKho(
        DbDataReader doc)
    {
        return new TonKhoHienTai(
            doc.SoNguyen(0),
            doc.SoNguyen(1),
            doc.SoThapPhan(2),
            doc.SoNguyen(3));
    }
}
