# KH-01–06, KH-08 và POS-03

Base: `develop`, commit `1fc1105`. Nhánh triển khai: `feature/customers-pos03`.

## Phạm vi

- `/Customers`: tìm theo tên, số điện thoại, mã; chỉ khách hoạt động, tối đa 100 kết quả. Thu ngân thấy số điện thoại che một phần ở danh sách và lịch sử.
- `/Customers/Create`, `/Customers/Edit`: TN/AD thêm/sửa thông tin, số điện thoại duy nhất 10 chữ số bắt đầu 0. Sửa dùng `phien_ban`, không nhận điểm từ form. Khi trùng số, quay lại danh sách khách có số đó để chọn.
- POS-03: từ `/Pos` mở tìm/thêm nhanh khách. Chọn hoặc gỡ khách chỉ cho TN, lấy thu ngân từ phiên đăng nhập. Khóa giỏ đang bán và khách, cập nhật liên kết và tính lại khuyến mãi trong cùng transaction.
- Thêm nhanh: lưu khách trước rồi gắn vào giỏ. Nếu gắn thất bại, khách vẫn được lưu và hiển thị lại để chọn; không tự tạo bản ghi khách trùng.
- KH-06: tiếp tục dùng transaction POS-07 hiện có, bổ sung mặc định 10.000 đồng/điểm khi chưa có quy tắc. Quy tắc tắt thì không tích điểm. Lưu tỷ lệ và điểm trên hóa đơn. Điểm ít hơn ngưỡng là 0, không tạo dòng lịch sử 0 theo CHECK hiện có.
- `CustomerPoints.ChangeAsync`: hàm cộng/thu hồi trong transaction của caller; khóa khách, không cho số dư âm, chỉ thêm dòng lịch sử. Sự kiện tích/thu hồi cùng hóa đơn/phiếu trả được xử lý lặp có kiểm tra số điểm; điều chỉnh thủ công luôn là sự kiện mới. DT-02/POS-12 có thể gọi hàm này, chưa tự tích hợp các use case đó.
- KH-05: lịch sử hóa đơn, điểm và lý do; bấm hóa đơn mở `/Invoices/{id}` (POS-11), giữ phân quyền xem hóa đơn của POS-11.
- KH-04: AD xác nhận xóa/ẩn. Chặn khách trong đơn đang bán/tạm giữ. Không có bất kỳ tham chiếu thì xóa; nếu còn tham chiếu thì ẩn và xóa thông tin cá nhân.
- KH-08: AD điều chỉnh khác 0, bắt buộc lý do; mã nội bộ hóa đơn tùy chọn phải thuộc khách. Ghi người thực hiện từ phiên và lý do vào lịch sử.
- Các POST MVC có CSRF token; quyền được kiểm tra trên controller. Không có đăng ký/đăng nhập dành cho khách hàng tự phục vụ trong phạm vi này.

## Điểm cần nhóm xác nhận

KH-04 yêu cầu xóa hẳn khách chưa có hóa đơn, nhưng schema còn FK từ giỏ, lịch sử điểm và phiếu đổi trả. Để giữ lịch sử bất biến, bản này ẩn khách khi còn bất kỳ tham chiếu nào, kể cả chưa có hóa đơn. Cần #1 xác nhận và ghi vào SRS nếu giữ cách xử lý này; không xóa lịch sử/cascade để né FK.

Không đổi schema, không tạo migration. Các route trên là MVC; chưa bổ sung các endpoint JSON `/api/v1/customers` trong hợp đồng OpenAPI cũ. Việc nghiệm thu hợp đồng API cần theo dõi riêng, không suy ra từ build MVC.

## Kiểm chứng

- Restore solution thành công; build ban đầu 0 lỗi/0 cảnh báo.
- `CustomerRulesTests`: 15/15 đạt (che số điện thoại, tỷ lệ tích điểm, làm tròn, số dư và bảo vệ action).
- Lần chạy cuối: 47/47 test KH mới + hồi quy hóa đơn/khuyến mãi đạt, 0 thất bại, 0 bỏ qua. Build kèm test thành công; cả bốn ca MySQL mới biên dịch được nhưng không nằm trong bộ lọc chạy.
- `CustomerDatabaseTests`: bốn ca MySQL đã thêm, chưa chạy khi thiếu `ConnectionStrings__CuaHangTest`. Fixture chỉ tạo database tạm `cuahang_test_*`, không dùng Aiven của nhóm.
- Docker Desktop trên máy chưa có daemon Linux hoạt động. Không xác nhận MySQL, HTTP E2E, staging, CI hay review độc lập đạt.

Lệnh chạy trên môi trường test MySQL riêng:

```powershell
dotnet test .\tests\ConvenienceStore.Web.Tests\ConvenienceStore.Web.Tests.csproj --filter FullyQualifiedName~CustomerRulesTests
# Đặt ConnectionStrings__CuaHangTest tới máy chủ MySQL TEST có quyền tạo/xóa database tạm.
dotnet test .\tests\ConvenienceStore.Web.Tests\ConvenienceStore.Web.Tests.csproj --filter FullyQualifiedName~CustomerDatabaseTests
```

Issue: #157, #158, #159, #184, #185, #186, #187, #188.
Biên bản #160 cần góp ý thực tế; #164 còn demo/staging/tag do nhóm điều phối.
