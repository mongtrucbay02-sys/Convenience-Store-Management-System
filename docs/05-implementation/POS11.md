# POS-11 — Xem chi tiết và in lại hóa đơn

Issue: #99. Nhánh: `feature/pos-11`. Phụ thuộc POS-10.

## Cách dùng

1. Đăng nhập TN hoặc AD rồi mở **Hóa đơn** (`/Invoices`).
2. Chọn **Xem chi tiết**, kể cả dòng hóa đơn đã hủy.
3. Xem dòng hàng, mã khuyến mãi và giảm giá đã lưu, khách, thanh toán, điểm đã cộng và các phiếu đổi trả liên quan.
4. Bấm **In lại hóa đơn K80**. Vùng in có dòng **In lại**. Hóa đơn hủy có thêm **ĐÃ HỦY** và ghi không có giá trị thanh toán.

Hộp thoại in do trình duyệt quản lý; người dùng chọn máy in K80, tắt header/footer của trình duyệt nếu cần. Hủy hộp thoại in không thay đổi giao dịch.

## Đường dẫn và quyền

| Đường dẫn | Kết quả |
|---|---|
| `GET /Invoices/{id}` | Màn hình Razor chi tiết và nút in lại |
| `GET /api/v1/invoices/{id}` | JSON theo schema HoaDon |
| `GET /api/v1/invoices/{id}/print` | JSON hoaDon, khoGiay=K80, inLai=true, daHuy |

- Cả hai controller yêu cầu policy `HOA_DON_XEM`.
- TN chỉ xem hóa đơn có `thu_ngan_id` trùng tài khoản từ claim của phiên; AD có quyền xem tất cả. Không yêu cầu thêm quyền BAN_HANG cho POS-11.
- Kiểm tra quyền sở hữu thực hiện trong service dùng chung cho màn hình, API chi tiết và API in. Truy cập hóa đơn của người khác trả 403.
- ID không hợp lệ: màn hình 400, API 422 theo envelope lỗi chung. Không tìm thấy: 404. Chưa đăng nhập: xử lý bởi module AUTH.
- Không cache dữ liệu chi tiết trên trình duyệt/proxy.

## Dữ liệu và mẫu in

- Tên sản phẩm, đơn vị, số lượng, đơn giá, giảm giá và thành tiền lấy từ `ct_hoa_don`; không tính lại từ giá/khuyến mãi hiện tại.
- Mã chương trình khuyến mãi được đọc qua ID đã lưu; số tiền giảm luôn lấy từ dòng hóa đơn.
- Điểm hiển thị là `hoa_don.diem_cong` tại lúc bán; phiếu đổi trả hiển thị riêng điểm thu hồi.
- Thanh toán bao gồm `tien_tru_hang_doi`, tiền khách đưa và tiền thừa. Kiểm tra POS-07 cũng chấp nhận số tiền đã trừ do đổi hàng theo CHECK của schema hiện có.
- Phiếu đổi trả được liên kết khi hóa đơn là hóa đơn gốc **hoặc** hóa đơn mới của phiếu đổi hàng; hiển thị mã, loại, trạng thái, thời gian, tiền hoàn, thu thêm và điểm thu hồi. Xử lý đổi trả vẫn thuộc DT-01.
- Mẫu `Views/Shared/_ReceiptPaper.cshtml` dùng chung cho POS-07 và POS-11. POS-07 không tự có dấu In lại; POS-11 luôn có dấu này, kể cả khi người dùng in bằng Ctrl+P.
- Không INSERT/UPDATE khi xem hoặc in lại. Không thay đổi schema, không cần migration. Không có thay đổi nghiệp vụ so với đặc tả POS-11 đã cung cấp.

## Kiểm thử

```powershell
dotnet build src/ConvenienceStore.Web --no-restore -p:NuGetAudit=false
dotnet test tests/ConvenienceStore.Web.Tests --no-restore --filter "FullyQualifiedName~PosInvoice"
```

Nhóm test POS-11 gồm 26 ca:

- 21 ca service/controller/API: TN của mình, TN khác, AD, thiếu quyền, thiếu hóa đơn, ID sai, cancellation, dữ liệu K80, hóa đơn đã hủy, tiền và điểm không đổi khi in nhiều lần, hợp đồng JSON và giờ Việt Nam.
- 3 ca render Razor mẫu K80: bản POS-07, bản in lại, bản hủy; kiểm tra marker và mã hóa HTML của dữ liệu khách.
- 2 ca tích hợp MySQL: schema/migration thật, dòng snapshot khác tên sản phẩm hiện tại, khuyến mãi, khách, khoản trừ đổi hàng, điểm, hóa đơn hủy và cả hai chiều liên kết phiếu đổi trả. Dùng fixture tạo CSDL `cuahang_test_*`, không dùng database nghiệp vụ. Cần biến môi trường `ConnectionStrings__CuaHangTest` theo hạ tầng test hiện có.

Kết quả kiểm tra trong phiên triển khai: 26/26 test POS-11 đạt. Bộ test toàn dự án trước khi thêm 5 test render/tích hợp mới có 152 đạt, 2 không đạt: `Inventory.TichHopDichVuKhoTests.Pos07_TonKhongDu_Rollback_KhongConHoaDonVaTheKho` và `Inventory.DichVuKhoTests.Ut04_Ban6KhiTon5_TonKhongDu_KhongGhiTheKho`. Hai ca này so khớp chuỗi thông báo tồn kho; không sửa module kho trong nhánh POS-11.

Chưa xác nhận máy in vật lý hoặc staging. PR cần reviewer và CI xác nhận trước khi đánh dấu Done.

## Checklist nghiệm thu thủ công

- TN A mở hóa đơn A từ danh sách; thử sửa URL sang hóa đơn B phải bị từ chối.
- AD xem được hóa đơn của mọi TN.
- Hóa đơn thường: đầy đủ dữ liệu; mở Print Preview, chỉ có giấy K80 và dấu In lại, không lẫn sidebar/bảng phiếu đổi trả.
- Hóa đơn hủy: vẫn mở được, cả màn hình và vùng in có ĐÃ HỦY.
- Không có phiếu đổi trả: thông báo rỗng. Có phiếu: đủ dữ liệu và vai trò hóa đơn gốc/mới.
- In lại nhiều lần hoặc hủy hộp thoại in: doanh thu, dòng hàng, điểm và tồn kho không thay đổi.
