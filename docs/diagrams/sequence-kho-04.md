# Sequence Diagram KHO-04 — Duyệt và điều chỉnh tồn sau kiểm kê

**Nhiệm vụ:** [T6-07, Issue #147](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/147). **Căn cứ nghiệp vụ:** [KHO-03, Issue #144](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/144) và [KHO-04, Issue #145](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/145). Ký hiệu actor → giao diện → bộ điều phối → CSDL → dịch vụ KHO và nhánh `alt` theo [sơ đồ POS-07](pos/POS-07-sequence.md). “Điều phối KHO-04” trong hình là **thành phần thiết kế dự kiến**, chưa phải tên class production đã xác nhận.

## Mục đích, actor và điều kiện

- **Mục đích:** AD duyệt phiếu kiểm kê từ KHO-03, điều chỉnh tồn về số lượng đếm thực tế tại thời điểm duyệt và ghi thẻ kho nếu có chênh lệch. BR-KHO-04 yêu cầu tính lại `số lượng thực tế − tồn tại lúc duyệt`, **không dùng `ton_luc_lap` để điều chỉnh**. BR-KHO-05 giữ nguyên giá vốn bình quân khi kiểm kê.
- **Actor:** Admin/Quản lý (AD), cần quyền `KIEM_KE_XUAT_HUY_DUYET` theo [MaQuyen.cs](../../src/ConvenienceStore.Web/Modules/Auth/Security/MaQuyen.cs) và [ma trận quyền](../../src/ConvenienceStore.Web/Modules/Auth/Security/BangQuyen.cs).
- **Tiền điều kiện:** NK đã lập/gửi phiếu theo KHO-03; `phieu_kiem_ke.trang_thai = CHO_DUYET`, mỗi dòng được duyệt có `so_luong_thuc_te` hợp lệ. Giao diện có thể hiển thị chênh lệch tạm tính; số đó phải được tính lại khi AD duyệt.
- **Hậu điều kiện duyệt:** Tồn phản ánh số đếm, `phieu_kiem_ke.trang_thai = DA_DIEU_CHINH`, có người/thời điểm duyệt; `ct_kiem_ke.chenh_lech_luc_duyet` và `gia_von_luc_duyet` phản ánh thời điểm duyệt. Dòng chênh lệch 0 không sinh `the_kho` theo hành vi dịch vụ hiện tại.
- **Hậu điều kiện từ chối:** Phiếu `BI_TU_CHOI` với lý do, trả về NK để kiểm lại; tồn không đổi.

## Sơ đồ tuần tự — thiết kế dự kiến

```mermaid
sequenceDiagram
    autonumber
    actor AD as Admin hoặc Quản lý
    participant UI as Giao diện kiểm kê
    participant KK as Điều phối KHO-04 dự kiến
    participant DB as MySQL
    participant KHO as DichVuKho

    AD->>UI: Mở phiếu CHO_DUYET
    UI->>KK: Yêu cầu xem phiếu kiểm kê
    KK->>KK: Kiểm tra quyền KIEM_KE_XUAT_HUY_DUYET
    KK->>DB: Đọc phiếu, dòng, tồn và giá trị chênh lệch tạm tính
    KK-->>UI: Hiển thị từng dòng và tổng giá trị

    alt AD từ chối kèm lý do (A1)
        AD->>UI: Chọn từ chối, nhập lý do
        UI->>KK: Gửi quyết định từ chối
        KK->>DB: BeginTransactionAsync(), khóa phiếu
        KK->>DB: UPDATE CHO_DUYET -> BI_TU_CHOI, ghi lý do và người/thời điểm
        alt UPDATE ném exception hoặc cập nhật 0 dòng (E1)
            DB-->>KK: Exception hoặc 0 dòng
            KK->>DB: RollbackAsync()
            KK-->>UI: Báo xung đột và tải lại
        else Cập nhật đúng 1 dòng
            DB-->>KK: 1 dòng
            KK->>DB: CommitAsync()
            KK-->>UI: Từ chối; tồn không đổi, NK kiểm lại
        end
    else AD duyệt
        AD->>UI: Chọn duyệt
        UI->>KK: Gửi quyết định duyệt
        KK->>DB: BeginTransactionAsync(), khóa phiếu và đọc lại dòng
        alt Không còn CHO_DUYET hoặc số đếm không hợp lệ (E1)
            KK->>DB: RollbackAsync()
            KK-->>UI: Báo xung đột hoặc dữ liệu không hợp lệ, tải lại
        else Phiếu hợp lệ
            KK->>KHO: GhiNhanBienDongAsync(KIEM_KE, số thực tế mỗi sản phẩm)
            KHO->>DB: Khóa ton_kho FOR UPDATE theo SanPhamId tăng dần
            KHO->>KHO: Chênh lệch = thực tế - tồn vừa khóa
            KHO->>DB: Điều chỉnh số lượng, giữ giá vốn; ghi the_kho nếu lệch khác 0
            alt GhiNhanBienDongAsync ném exception
                KHO-->>KK: Ném exception
                KK->>DB: RollbackAsync()
                KK-->>UI: Báo lỗi, tồn và phiếu giữ trạng thái cũ
            else KHO trả KetQuaBienDong
                KHO-->>KK: KetQuaBienDong gồm TonTruoc và SoLuongThayDoi
                KK->>DB: Ghi chenh_lech_luc_duyet, gia_von_luc_duyet
                KK->>DB: UPDATE CHO_DUYET -> DA_DIEU_CHINH, ghi người/thời điểm
                alt Ghi chi tiết hoặc UPDATE phiếu ném exception / cập nhật 0 dòng
                    DB-->>KK: Exception hoặc 0 dòng
                    KK->>DB: RollbackAsync()
                    KK-->>UI: Báo lỗi hoặc xung đột, tải lại phiếu
                else Hoàn tất
                    DB-->>KK: Ghi thành công, UPDATE đúng 1 dòng
                    KK->>DB: CommitAsync()
                    KK-->>UI: Phiếu DA_DIEU_CHINH và tồn sau duyệt
                end
            end
        end
    end
```

## Luồng chính, thay thế và ngoại lệ

1. AD xem phiếu `CHO_DUYET` cùng chênh lệch và tổng giá trị **tạm tính**. Khi bấm duyệt, bên điều phối kiểm tra quyền và mở transaction, khóa phiếu, đọc lại các dòng.
2. Gọi [`IDichVuKho.GhiNhanBienDongAsync`](../../src/ConvenienceStore.Web/Modules/Inventory/Services/IDichVuKho.cs) với `LoaiBienDong.KIEM_KE` và `DongBienDong(SanPhamId, SoLuongThucTe: số đếm)`. Dịch vụ khóa `ton_kho` hiện tại, tính `SoLuongThayDoi = SoLuongThucTe − SoLuongTon` **ngay tại lúc duyệt**. Ví dụ KHO-03 lưu `ton_luc_lap = 10`, NK đếm 8, sau đó bán 1 nên tồn lúc duyệt là 9: chênh lệch cần ghi **−1**, không phải −2.
3. [`TinhToanKho.GiaVon`](../../src/ConvenienceStore.Web/Modules/Inventory/Services/TinhToanKho.cs) trả lại giá vốn trước cho `KIEM_KE`; giá vốn bình quân của `ton_kho` không đổi. Dịch vụ ghi `the_kho` khi chênh lệch khác 0; bên điều phối lưu kết quả lúc duyệt vào `ct_kiem_ke`, chuyển phiếu có điều kiện sang `DA_DIEU_CHINH` rồi commit.
4. **A1 — từ chối:** AD nhập lý do; chuyển `CHO_DUYET` → `BI_TU_CHOI`, ghi người/thời điểm. Không gọi dịch vụ KHO và không thay đổi tồn. NK xem lại, đếm lại và gửi duyệt theo KHO-03.
5. **E1 — phiếu đã xử lý:** Sau khi khóa, nếu trạng thái không còn `CHO_DUYET` hoặc cập nhật có điều kiện ảnh hưởng 0 dòng, rollback, báo xung đột và tải lại. Số đếm âm hoặc còn thiếu phải bị từ chối trước khi ghi tồn; không tự suy ra mã HTTP cho handler chưa triển khai.

## Transaction boundary và rollback

Bên điều phối mở **một transaction** cho việc khóa/kiểm tra lại `phieu_kiem_ke`, gọi KHO, ghi `ct_kiem_ke` và chuyển trạng thái phiếu. [`DichVuKho`](../../src/ConvenienceStore.Web/Modules/Inventory/Services/DichVuKho.cs) yêu cầu transaction sẵn có và không tự commit. Khi bất kỳ bước nào thất bại, rollback phải khôi phục cả tồn, thẻ kho, chi tiết lúc duyệt và trạng thái phiếu. Nhánh từ chối chỉ sửa phiếu, không gọi KHO. Điều kiện `WHERE trang_thai = 'CHO_DUYET'` trên lệnh cập nhật cuối cùng là thiết kế chống duyệt/từ chối hai lần; 0 dòng là xung đột nghiệp vụ.

## Mapping tới source, entity và bảng dữ liệu

| Thành phần | Mapping và mức hiện có |
|---|---|
| Phiếu kiểm kê | [`PhieuKiemKe`](../../src/ConvenienceStore.Web/Data/Entities/PhieuKiemKe.cs) ↔ `phieu_kiem_ke`; trạng thái `NHAP`, `CHO_DUYET`, `BI_TU_CHOI`, `DA_DIEU_CHINH` theo [SchemaV1.cs](../../src/ConvenienceStore.Web/Data/Migrations/SchemaV1.cs). Entity đã có `NguoiDuyetId` → `nguoi_duyet_id`, `DuyetLuc` → `duyet_luc`, `LyDoTuChoi` → `ly_do_tu_choi`. |
| Chi tiết lúc lập/lúc duyệt | [`CtKiemKe`](../../src/ConvenienceStore.Web/Data/Entities/CtKiemKe.cs) ↔ `ct_kiem_ke`; entity đã có `TonLucLap` → `ton_luc_lap`, `SoLuongThucTe` → `so_luong_thuc_te`, `ChenhLechLucDuyet` → `chenh_lech_luc_duyet`, `GiaVonLucDuyet` → `gia_von_luc_duyet`. Các tên cột được xác minh trong [`CuaHangDbContext`](../../src/ConvenienceStore.Web/Data/CuaHangDbContext.cs); logic production ghi giá trị lúc duyệt vẫn là thiết kế dự kiến. |
| Hợp đồng KHO | [`LoaiBienDong.KIEM_KE`](../../src/ConvenienceStore.Web/Modules/Inventory/Models/LoaiBienDong.cs), [`YeuCauBienDong`](../../src/ConvenienceStore.Web/Modules/Inventory/Models/YeuCauBienDong.cs), [`DongBienDong`](../../src/ConvenienceStore.Web/Modules/Inventory/Models/DongBienDong.cs), `IDichVuKho.GhiNhanBienDongAsync` đã có. |
| Dữ liệu tồn/thẻ | `ton_kho`, `the_kho`; dịch vụ dùng `FOR UPDATE`, giữ giá vốn khi kiểm kê, bỏ qua ghi thẻ cho chênh lệch 0. [DichVuKhoTests.cs](../../tests/ConvenienceStore.Web.Tests/Inventory/DichVuKhoTests.cs) có `Ut07` (chênh lệch 0) và `Ut08` (tồn thay đổi sau lúc lập). |

**Ranh giới triển khai:** Source baseline được đối chiếu chưa có controller/service production điều phối KHO-04, xử lý từ chối, ghi giá trị vào các cột “lúc duyệt” hoặc kiểm tra quyền tại endpoint. Các property và cột vật lý nêu trên **đã tồn tại**; phần chưa có bằng chứng là luồng nghiệp vụ production sử dụng chúng. Test KHO xác minh quy tắc tính tồn/giá vốn ở mức dịch vụ, **không** chứng minh toàn bộ use case KHO-04, rollback hoặc giao diện. Sơ đồ phục vụ chương Thiết kế hệ thống.
