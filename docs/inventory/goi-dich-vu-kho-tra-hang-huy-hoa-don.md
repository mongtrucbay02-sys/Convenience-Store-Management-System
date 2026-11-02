# Gọi dịch vụ kho cho trả hàng (DT-05) và hủy hóa đơn (POS-12)

Task T-KHO-SV2 (#1, issue #140). Bàn giao cho #3 trước khi làm DT-02, DT-05, POS-12 ở tuần 7.
Căn cứ: Tài liệu giao diện dịch vụ kho bản 1.2 (mục 3.4, 5, 7), Đặc tả Use Case 18/10 (BR-DT-06, POS-12).

## Thay đổi

- `TinhToanKho.LoaiDangHoTro` nhận thêm `TRA_HANG` và `HUY_HOA_DON`. Trước đây hai loại này bị từ chối với lỗi `DU_LIEU_KHONG_HOP_LE` (trường `loai`).
- Cách tính đã có sẵn từ T-KHO-SV, nay được bật và có test:
  - số lượng thay đổi là `+soLuong` (tồn tăng);
  - `giaVon` bắt buộc, không âm; thiếu thì báo `DU_LIEU_KHONG_HOP_LE` ở trường `dong[i].giaVon`;
  - giá vốn sau = bình quân gia quyền, làm tròn nửa lên 2 chữ số; nếu tồn trước bằng 0 thì giá vốn sau = giá vốn truyền vào;
  - `giaVonApDung` trả về = giá vốn truyền vào.
- Không đổi cấu trúc CSDL, không có migration. Bảng `the_kho` đã cho phép hai loại này với số lượng dương (CHECK `ck_the_kho_huong`).

## Cách gọi

Dịch vụ không tự mở giao tác. Bên gọi mở giao tác, gọi dịch vụ cùng các thao tác khác của use case, rồi tự `CommitAsync` hoặc `RollbackAsync`.

### DT-05: dòng hàng trả được chọn "nhập lại"

```csharp
await using var gd = await db.Database.BeginTransactionAsync(ct);
// ... lưu phiếu trả (phieu_doi_tra, ct_phieu_doi_tra) để có id và mã phiếu
var dongNhapLai = chiTiet.Where(d => d.CachXuLy == "NHAP_LAI")
    .Select(d => new DongBienDong(d.SanPhamId, SoLuong: d.SoLuong, GiaVon: d.GiaVonDongHoaDonGoc))
    .ToList();
if (dongNhapLai.Count > 0)
    await dichVuKho.GhiNhanBienDongAsync(new YeuCauBienDong(
        LoaiBienDong.TRA_HANG, phieu.Id, phieu.MaPhieu, thuNganId, dongNhapLai), ct);
// ... hoàn tiền (DT-04), thu hồi điểm
await gd.CommitAsync(ct);
```

- `GiaVon` là `ct_hoa_don.gia_von` của dòng hóa đơn gốc (BR-DT-06), không lấy giá vốn hiện tại.
- Dòng chọn "hủy" không gọi dịch vụ kho; chỉ ghi giá trị hủy trên dòng phiếu trả.
- Mỗi sản phẩm chỉ xuất hiện một lần trong một lần gọi; nếu phiếu có hai dòng cùng sản phẩm thì cộng số lượng trước.

### POS-12: hủy hóa đơn đã thanh toán

```csharp
var dong = chiTietHoaDon
    .GroupBy(d => d.SanPhamId)
    .Select(g => new DongBienDong(g.Key, SoLuong: g.Sum(x => x.SoLuong), GiaVon: g.First().GiaVon))
    .ToList();
await dichVuKho.GhiNhanBienDongAsync(new YeuCauBienDong(
    LoaiBienDong.HUY_HOA_DON, hoaDon.Id, hoaDon.MaHoaDon, adminId, dong), ct);
```

- Gọi trong cùng giao tác với thu hồi điểm, loại khỏi lượt khuyến mãi và chuyển hóa đơn sang `DA_HUY`.
- `GiaVon` là giá vốn đã lưu trên dòng hóa đơn (`ct_hoa_don.gia_von`).

## Lỗi bên gọi cần xử lý

| Mã lỗi | Khi nào | Bên gọi làm gì |
| --- | --- | --- |
| `DU_LIEU_KHONG_HOP_LE` (422) | Thiếu hoặc âm `giaVon`, số lượng ≤ 0, sản phẩm trùng | Rollback; đây là lỗi lập trình, test phải bắt trước khi gộp |
| `SAN_PHAM_KHONG_TON_TAI` (422) | Sản phẩm chưa có dòng `ton_kho` | Rollback |
| `LOI_GHI_DU_LIEU` (500) | Lỗi CSDL khi ghi | Rollback, chứng từ giữ trạng thái cũ |

Hai loại này làm tồn tăng nên không phát sinh `TON_KHONG_DU`.

## Test

`tests/ConvenienceStore.Web.Tests/Inventory/DichVuKhoTests.cs`, chạy bằng `dotnet test --filter Category=DichVuKho`:

| Test | Tình huống | Kết quả mong đợi |
| --- | --- | --- |
| `Ut10_TraHangNhapLai1GiaVon8000KhiTon2GiaVon9000` | UT-10 trong tài liệu dịch vụ kho 1.2 | Tồn 3, giá vốn 8.666,67 đ, 1 dòng thẻ kho `TRA_HANG` |
| `TraHang_TonTruocBang0_GiaVonMoiBangGiaVonDongHoaDon` | Tồn 0, trả 2 với giá vốn 7.250,5 đ | Giá vốn mới 7.250,5 đ |
| `HuyHoaDon_HoanTon2GiaVon8500KhiTon4GiaVon10000_GiaVon9500` | Hủy hóa đơn hoàn 2 | Tồn 6, giá vốn 9.500 đ |
| `HuyHoaDon_LamTronNuaLen2ChuSo_Ton3GiaVon10000Them4Gia10500` | Làm tròn | 10.285,71 đ |
| `HuyHoaDon_NhieuDong_GhiMoiDongMotTheKho_BenGoiRollbackThiKhongConGi` | Hai dòng, bên gọi rollback | Tồn và thẻ kho giữ nguyên |
| `Ut11_YeuCauSai_DuLieuKhongHopLe` (bổ sung) | Thiếu/âm `giaVon`, số lượng 0, loại không tồn tại | `DU_LIEU_KHONG_HOP_LE` đúng trường |

Kết quả chạy ngày 02/11/2026 trên MySQL 9.7.2: 41/41 test dịch vụ kho đạt; toàn bộ 164/164 test đạt.

Cùng PR này sửa 2 test đang trượt (`Ut04_…` và `Pos07_TonKhongDu_…`): test cũ so cả câu "chỉ còn N sản phẩm", trong khi thông báo đã đổi ở commit `e8bc40f` (POS-07). Test nay chỉ kiểm tra phần "chỉ còn N", không sửa code của POS-07.
