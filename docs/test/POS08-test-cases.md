# POS-08 — Tạm giữ / gọi lại đơn

Nhánh `feature/pos-08`, issue #150. CSDL dùng các cột/trạng thái sẵn có của `gio_hang`, không đổi cấu trúc, không có migration.

## Luồng và hành vi

- GET `/Pos`: chỉ thu ngân có quyền BAN_HANG; trước khi đọc giỏ, tự hủy đơn TAM_GIU của chính thu ngân có `tam_giu_luc` trước đầu ngày Việt Nam (+07:00). Lưu HET_HAN_TAM_GIU, thời điểm và giá trị hủy. Không tác động đơn của người khác.
- POST `/Pos/Hold`, trường `note` tùy chọn, tối đa 100 ký tự. Chỉ nhận giỏ DANG_BAN không rỗng, tối đa 5 đơn TAM_GIU/thu ngân. Lưu trạng thái, thời gian, ghi chú và tạo giỏ trống trong cùng giao tác.
- POST `/Pos/Recall`, trường `orderId`. Chỉ gọi đơn của chính mình trong ngày. Không ghi đè giỏ đang có sản phẩm; thu ngân phải thanh toán/tạm giữ/hủy giỏ đó trước.
- Gọi lại kiểm tra trạng thái sản phẩm/danh mục, giá hiện hành, tồn và khuyến mãi. Thiếu tồn hoặc sản phẩm không còn bán/chưa có giá: báo rõ, giữ nguyên đơn TAM_GIU, không tự bỏ dòng hoặc giảm số lượng.
- Khi hợp lệ: cập nhật giá và chọn lại chương trình tốt nhất mỗi dòng, mở lại DANG_BAN. Thông báo thay đổi giá/khuyến mãi và tồn hiện tại; yêu cầu kiểm tra tổng trước thanh toán.
- Không giữ chỗ tồn, không ghi thẻ kho, không ghi hóa đơn/hoàn tiền hay điểm. Gọi lại không thay thế kiểm tra tại bước chốt thanh toán POS-07.
- Tất cả POST có antiforgery. ID thu ngân lấy từ claim và đối chiếu tài khoản TN hoạt động, đã đổi mật khẩu. Mã đơn từ request luôn lọc theo chủ sở hữu.
- Phần tính khuyến mãi dùng dữ liệu KM hiện hành; không gộp toàn bộ nhánh POS-04. Ưu đãi chỉ thành viên bị bỏ qua nếu đơn chưa gắn khách hoạt động.

Các POST MVC trả redirect về `/Pos`, thông báo bằng TempData; đây không phải endpoint JSON mới.

## Thử thủ công trên local

1. Chạy nhánh feature/pos-08, đăng nhập thu ngân, mở /Pos.
2. Thêm 2 sản phẩm; ghi nhận số tồn trong SQL hoặc màn hình. Nhập ghi chú và bấm Tạm giữ.
3. Giỏ mới trống; danh sách có đơn vừa giữ, ghi chú, giờ giữ và tổng lúc giữ. Tồn không đổi.
4. Bấm Gọi lại khi giỏ trống: lấy lại đúng đơn và số lượng, danh sách bỏ đơn vừa gọi, tồn không đổi.
5. Giữ 5 đơn có hàng, thử POST giữ đơn thứ 6: bị từ chối; không mất giỏ đang bán.
6. Giỏ hiện tại còn hàng: gọi đơn khác bị từ chối, hai đơn giữ nguyên.
7. Thay đổi giá/khuyến mãi qua chức năng quản lý rồi gọi lại: cập nhật và báo thay đổi, kiểm tra tổng mới.
8. Cho tồn giảm bằng giao dịch bán hợp lệ của thu ngân khác rồi gọi lại: nếu thiếu, báo số lượng cần/còn, không mở đơn.
9. Trên CSDL kiểm thử riêng, tạo đơn giữ ngày trước: mở /Pos, đơn chuyển DA_HUY với đầy đủ thông tin hủy. Không sửa ngày trên dữ liệu chung để thử.
10. Đăng nhập TN khác, thử ID đơn đã giữ của người trước: bị từ chối. AD/NK/khách chưa đăng nhập không được thao tác POS-08.

## Kiểm thử tự động

Chạy `dotnet test tests/ConvenienceStore.Web.Tests --filter FullyQualifiedName~PosHeldOrder` với biến môi trường ConnectionStrings__CuaHangTest trỏ MySQL local có quyền tạo CSDL tạm. Không dùng Aiven chung cho fixture.

Bao phủ: giữ/gọi lại; giới hạn 5; đơn rỗng/ghi chú dài; hết ngày; quyền sở hữu; bảo vệ giỏ đang có hàng; thiếu tồn; đổi giá/khuyến mãi; gỡ ưu đãi tạm dừng; đồng thời giữ/gọi lại; actor/claim bắt buộc đổi mật khẩu; form Razor mã hóa ghi chú và có antiforgery.

Chưa xác nhận CI GitHub, staging hoặc review nhóm. Không đánh dấu hoàn thành tiêu chí nghiệm thu trước khi có các xác nhận này.
## Kết quả ngày 05/11/2026

- Build đạt; 17/17 kiểm thử POS-08 đạt (MySQL tạm, đồng thời, quyền, Razor/antiforgery).
- Toàn bộ suite: 255 đạt, 2 lỗi có sẵn trên develop e01c0ad:
  - OperationalReportsServiceTests.ProductAndCategoryReports_AllocateInvoiceDeductionsProportionally (dòng 78).
  - DanhMucConcurrencyTests.TwoContexts_StaleUpdateThrowsConcurrencyException (dòng 85).
- Không sửa hai lỗi baseline trong POS-08. Chưa chạy staging/CI hoặc được review nhóm.
