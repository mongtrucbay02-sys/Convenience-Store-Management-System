# Xác minh tích hợp NH-06 → KHO phục vụ báo cáo giữa kỳ

## 1. Thông tin kiểm thử

| Mục | Thông tin |
|---|---|
| Chức năng | NH-06 — xác nhận phiếu nhập và ghi nhận biến động kho |
| Mức kiểm thử | Tích hợp dịch vụ KHO với MySQL; **test mô phỏng bước xác nhận NH-06 bằng SQL** |
| Test | `TichHopDichVuKhoTests.Nh06_XacNhanNhapKho_GhiTheKhoTheoMaPhieu_ChuyenDaNhapKho` |
| Source đối chiếu | `origin/develop` tại `bd4b7b57f151b5774cae41035ed653e0fedbd50d` |
| Môi trường thực thi được cung cấp | MySQL 8.4.13 trong Docker riêng, endpoint `127.0.0.1:33307` |
| Bằng chứng | TRX cục bộ `tmp/nh06-evidence/nh06-kho.trx` trong worktree kiểm thử; chưa upload lên GitHub |
| Build | PASS theo thông tin thực thi đã cung cấp; tài liệu này không chạy lại build |

Mã test ở [TichHopDichVuKhoTests.cs](../../tests/ConvenienceStore.Web.Tests/Inventory/TichHopDichVuKhoTests.cs). Số liệu ở mục 7 được đối chiếu từ TRX; phiên bản MySQL, endpoint và kết quả build là thông tin được cung cấp, không phải thuộc tính do TRX chứng minh.

## 2. Mục tiêu và phạm vi nghiệp vụ NH-06

Kịch bản kiểm tra việc ghi hai dòng nhập hàng vào `ton_kho` và `the_kho`, gắn biến động với mã phiếu nhập, rồi chuyển `phieu_nhap.trang_thai` từ `NHAP` sang `DA_NHAP_KHO`. Phạm vi đã chạy là **mã test → `DichVuKho` → MySQL**. Test không gọi controller, API hoặc giao diện NH-06.

## 3. Kiến trúc tích hợp NH-06 → KHO được test mô phỏng

[`IDichVuKho.GhiNhanBienDongAsync`](../../src/ConvenienceStore.Web/Modules/Inventory/Services/IDichVuKho.cs) nhận `YeuCauBienDong` gồm `LoaiBienDong.NHAP_HANG`, ID/mã chứng từ, người thực hiện và các dòng hàng. [`DichVuKho`](../../src/ConvenienceStore.Web/Modules/Inventory/Services/DichVuKho.cs) yêu cầu bên gọi mở transaction, khóa `ton_kho` bằng `FOR UPDATE`, tính tồn và giá vốn, cập nhật `ton_kho`, ghi `the_kho`; dịch vụ không tự commit. Trong test, **mã test** khóa phiếu, gọi dịch vụ, đổi trạng thái phiếu và commit.

```mermaid
sequenceDiagram
    autonumber
    participant Test as Test mô phỏng NH-06
    participant DB as MySQL test database
    participant Kho as DichVuKho
    Test->>DB: Tạo nhà cung cấp, phiếu NHAP, 2 chi tiết bằng SQL
    Test->>DB: BeginTransactionAsync()
    Test->>DB: SELECT phieu_nhap ... FOR UPDATE
    DB-->>Test: trang_thai = NHAP
    Test->>DB: Đọc ct_phieu_nhap
    Test->>Kho: GhiNhanBienDongAsync(NHAP_HANG, ID/mã phiếu, 2 dòng)
    Kho->>DB: Khóa ton_kho; cập nhật tồn và giá vốn
    Kho->>DB: INSERT 2 dòng the_kho
    Kho-->>Test: KetQuaBienDong
    Test->>DB: UPDATE phieu_nhap = DA_NHAP_KHO
    Test->>DB: CommitAsync()
    Test->>DB: Đọc lại tồn, thẻ kho và trạng thái
```

Sơ đồ mô tả **kịch bản test**, không phải luồng production NH-06 đã hoàn thiện.

## 4. Điều kiện tiền đề và dữ liệu test

[`CsdlKhoFixture`](../../tests/ConvenienceStore.Web.Tests/Inventory/CsdlKhoFixture.cs) kế thừa [`CsdlMySqlTam`](../../tests/ConvenienceStore.Web.Tests/HaTang/CsdlMySqlTam.cs): tạo database tạm `cuahang_test_...`, chạy migration `InitialCreate`, nạp tài khoản test và danh mục, rồi xóa database khi kết thúc nếu không bật chế độ giữ lại. Fixture nhận biến `ConnectionStrings__CuaHangTest`, chưa tự xác minh host là server test độc lập; chỉ chạy sau khi xác nhận môi trường và quyền CREATE/DROP DATABASE.

Test dùng `BoTroGoiDichVuKho.TaoSanPhamCoTonAsync` để tạo sản phẩm riêng, sau đó tạo nhà cung cấp, một phiếu nhập mặc định `NHAP` và hai chi tiết bằng SQL:

| Sản phẩm | Tồn trước | Giá vốn trước | Dòng phiếu nhập | Tồn mong đợi |
|---|---:|---:|---:|---:|
| SP1 (ID sinh trong test) | 10 | 8.000đ | 20 × 9.500đ | 30 |
| SP2 (ID sinh trong test) | 0 | 0đ | 5 × 12.000đ | 5 |

Mã phiếu và ID được sinh riêng cho lần chạy; tài liệu không chứa thông tin xác thực database.

## 5. Các bước thực hiện và logic transaction

1. Tạo dữ liệu nền và phiếu nhập bằng helper/SQL trong [TichHopDichVuKhoTests.cs](../../tests/ConvenienceStore.Web.Tests/Inventory/TichHopDichVuKhoTests.cs).
2. Mở transaction trên `CuaHangDbContext`; đọc `phieu_nhap` với `FOR UPDATE` và xác nhận trạng thái `NHAP`.
3. Đọc hai dòng `ct_phieu_nhap`, chuyển thành `DongBienDong`; gọi `DichVuKho.GhiNhanBienDongAsync` với `LoaiBienDong.NHAP_HANG`, ID và mã phiếu thực tế.
4. Trong cùng transaction, mã test cập nhật phiếu sang `DA_NHAP_KHO`, điền người/thời điểm xác nhận, tăng `phien_ban`; assertion yêu cầu đúng một dòng được cập nhật.
5. Commit; sau đó đọc tồn kho, đếm `the_kho` theo **cả** `ma_chung_tu` và `chung_tu_id`, rồi đọc lại trạng thái phiếu.

Test chứng minh các thao tác ghi thành công được đặt trong cùng transaction. Kịch bản NH-06 này **chưa** gây lỗi giữa chừng để chứng minh rollback khi thất bại.

## 6. Bảng test case và assertion

| ID | Mục tiêu | Expected result | Actual result từ lần chạy | Status |
|---|---|---|---|---|
| NH06-KHO-01 | Chỉ xác nhận phiếu đang `NHAP` | Đọc được `NHAP`; lệnh đổi trạng thái cập nhật 1 dòng | Assertion tương ứng thuộc test Passed | PASS |
| NH06-KHO-02 | Ghi nhập SP1 và tính giá vốn | Tồn 10 → 30; `(10 × 8.000 + 20 × 9.500) / 30 = 9.000đ` | Assertion tồn sau và `GiaVonSau = 9.000đ` thuộc test Passed | PASS |
| NH06-KHO-03 | Ghi nhập SP2 từ tồn 0 | Tồn 0 → 5; giá vốn sau 12.000đ | Assertion `TonKhoDoc(5, 12000m, 5)` thuộc test Passed | PASS |
| NH06-KHO-04 | Liên kết thẻ kho với phiếu | Đúng 2 dòng `NHAP_HANG`, cùng mã và ID phiếu | Assertion `COUNT(*) = 2` thuộc test Passed | PASS |
| NH06-KHO-05 | Hoàn tất phiếu | Sau commit, trạng thái `DA_NHAP_KHO` | Assertion đọc lại trạng thái thuộc test Passed | PASS |
| NH06-KHO-06 | Rollback khi bước sau ghi kho thất bại | Không để tồn, thẻ kho và trạng thái lệch nhau | Kịch bản NH-06 này không gây lỗi/rollback để kiểm tra | NOT RUN |

“Actual result” phản ánh assertion trong source kết hợp outcome `Passed` của đúng một test trong TRX; tài liệu không thay thế log truy vấn từng bước.

## 7. Kết quả thực tế và bằng chứng TRX

TRX cục bộ ghi `ResultSummary.outcome = Completed`; `Counters`: **Total 1, Executed 1, Passed 1, Failed 0, Skipped 0**. `UnitTestResult` của đúng phương thức `Nh06_XacNhanNhapKho_GhiTheKhoTheoMaPhieu_ChuyenDaNhapKho` có outcome `Passed` (bắt đầu `2026-11-03 01:10:25 +07:00`, kết thúc `01:10:26 +07:00`). TRX chỉ nằm trên máy kiểm thử, chưa được đưa vào repository hoặc GitHub.

## 8. Kiểm tra tính nhất quán dữ liệu

Sau commit, test đọc lại `ton_kho`: SP1 có 30 đơn vị, SP2 có 5 đơn vị. Kết quả dịch vụ xác nhận giá vốn bình quân SP1 là 9.000đ. Test đếm hai dòng `the_kho` của **phiếu đang kiểm tra** theo loại biến động, mã và ID chứng từ, rồi đọc lại trạng thái `DA_NHAP_KHO`. Helper tạo tồn ban đầu có thể sinh thẻ kho với chứng từ giả riêng; phép đếm hai dòng không tính chúng.

Những assertion này chứng minh trạng thái sau một giao dịch thành công. Chúng chưa chứng minh chống xác nhận lặp, xử lý xác nhận đồng thời hoặc rollback khi cập nhật phiếu thất bại.

## 9. Giới hạn của kết quả kiểm thử

- Source ở baseline có entity/mapping `PhieuNhap`, `CtPhieuNhap` và quyền `PhieuNhapXacNhan`, nhưng chưa tìm thấy controller hoặc service production thực hiện xác nhận phiếu nhập. Test tự chạy SQL thay cho luồng NH-06 và gọi trực tiếp `DichVuKho`.
- Không có bằng chứng HTTP API, phân quyền endpoint, giao diện, kiểm thử end-to-end, CI hoặc trạng thái merge NH-06 từ lần chạy này.
- Một test Passed trên MySQL 8.4.13 chỉ xác minh dữ liệu và đường tích hợp đã chọn; không bao phủ mọi quy tắc nghiệp vụ, lỗi và giao dịch đồng thời.
- `CsdlMySqlTam` có thao tác CREATE/DROP DATABASE. Không chạy lại test trên Aiven, staging, production hoặc database dùng chung khi chưa xác minh môi trường.

## 10. Kết luận và công việc tiếp theo

**Đạt ở mức tích hợp service/database cho kịch bản thành công được mô phỏng:** KHO ghi biến động `NHAP_HANG`, tồn và giá vốn SP1/SP2 đúng, có hai thẻ kho liên kết phiếu, và trạng thái phiếu chuyển sang `DA_NHAP_KHO` trong transaction do test mở. **Chưa thể kết luận NH-06 đã hoàn thiện ở mức API/giao diện hoặc end-to-end.**

Tiếp theo: triển khai và kiểm thử controller/service NH-06 theo hợp đồng nghiệp vụ; thêm test xác nhận lặp, lỗi giữa giao dịch/rollback, concurrency và phân quyền; chạy lại trên MySQL test server độc lập trong CI rồi lưu bằng chứng tương ứng. Những việc này nằm ngoài phạm vi tài liệu hiện tại.
