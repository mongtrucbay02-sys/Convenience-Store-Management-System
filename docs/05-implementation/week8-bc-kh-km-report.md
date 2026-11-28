# Báo cáo phần BC, KH, KM — tuần 8 / #251

**Thành viên:** #5 Nguyễn Tiến Trung. **Ngày:** 23/11/2026, cập nhật 24/11/2026. **Trạng thái:** Đang review; bản để Thăng tổng hợp báo cáo, không phải biên bản nghiệm thu. Bản gốc thuộc nhánh `feature/week8-bc-km-20261123`; runtime hiện tại đối chiếu `develop` nền `770550c` sau tích hợp PostgreSQL. Kết quả/ảnh MySQL ngày 23/11 bên dưới là bằng chứng lịch sử, không đại diện cho lần chạy PostgreSQL mới. Xem [kiểm thử bản sửa #266](../06-testing/test-reports/2026-11-24-bc-kh-km-acceptance.md).

## 1. Phạm vi và kiến trúc

Phần của Trung gồm quản lý khách hàng, tích điểm, khuyến mãi và báo cáo. Runtime dùng C# / ASP.NET Core MVC + Razor, EF Core `CuaHangDbContext`, Npgsql và PostgreSQL 17 (schema `smartmart`); truy vấn tổng hợp trực tiếp theo phương án PA-BC-03.A. Không tạo DbContext thứ hai, không tạo CSDL riêng bằng SQL thay migration. Trong kiểm thử, dùng CSDL tạm độc lập để không ghi vào Supabase chung.

Controller kiểm tra quyền máy chủ bằng AUTH của repo. Trang quản lý khách phục vụ TN/AD; báo cáo và KM-05/06 chỉ AD. Thu ngân không được cấp quyền báo cáo bằng cách gửi `isAdmin` hoặc header tự đặt.

| Thành phần | Trách nhiệm |
|---|---|
| `Modules/Customers` | Tìm/thêm/sửa, lịch sử, ẩn/xóa và điều chỉnh điểm; `CustomerPoints` là cổng ghi điểm dùng chung cho POS/DT |
| `Modules/Promotions` | Quản lý KM-01..04 hiện có; tuần 8 bổ sung `PromotionUsageStore`, lịch sử và xóa có bảo vệ tham chiếu |
| `Modules/Reports` | Dashboard và khung kỳ/bộ lọc/detail/Excel; `WeekEightReportsService`, nguồn PostgreSQL và BC-09..11 |
| Bản sao giao dịch | Dòng hóa đơn lưu tên, giá, giảm và giá vốn; lịch sử điểm bất biến; báo cáo không lấy giá hiện tại để tính lại giao dịch cũ |

## 2. Thiết kế dữ liệu và luồng chính

- [ERD KH/KM, từ điển và ràng buộc](../04-system-design/database/customers-promotions/README.md).
- [Sequence KH-06 và BC-08 — #252](../04-system-design/sequences/kh06-bc08.md).
- [Hợp đồng API, bộ lọc và công thức tuần 8](../04-system-design/api/week8-reports-promotions.md).

KH-06 tích điểm trong transaction thanh toán POS-07. Khi lỗi kho, lưu hóa đơn, điểm hoặc quỹ, transaction phải rollback. Không cung cấp API cộng điểm thanh toán độc lập. BC-08 lấy bản sao giá vốn và doanh thu, trừ vốn phần trả nhập lại, hiển thị xuất hủy/hàng trả hủy thành chỉ tiêu riêng.

## 3. Kết quả hiện thực theo use case

“Có code” bên dưới chỉ mô tả phạm vi hiện thực, không đồng nghĩa mọi checklist issue đã được review/CI/staging thông qua.

| Use case | Sản phẩm trong code / tình trạng |
|---|---|
| BC-01 | Dashboard doanh thu, xu hướng, sản phẩm bán chạy và số phiếu chờ duyệt đã có ở nhánh nền |
| BC-02–05 | Doanh thu theo thời gian, sản phẩm, danh mục và thu ngân đã có ở nhánh nền |
| BC-06–08 | Tồn kho, nhập hàng, lợi nhuận gộp đã có ở nhánh nền |
| BC-09 / #248 | Tuần 8: thống kê trả/đổi hoàn tất, số lượng, tiền hoàn, giá trị trả, lý do và sản phẩm trả nhiều; lọc lý do/thu ngân, biểu đồ cột, A2 chi tiết và BC-12 Excel |
| BC-10 / #249 | Tuần 8: khách mới, khách có mua, tỷ lệ đơn gắn khách, top chi tiêu, tích/thu hồi/điều chỉnh điểm, chi tiết khách và Excel |
| BC-11 / #250 | Tuần 8: lượt và giảm giá theo CT, doanh thu đơn có KM khử trùng, trung bình đơn có/không KM, lọc CT, cột/chi tiết/Excel |
| BC-12 | Dùng lại trình xuất .xlsx có metadata, chỉ số, bảng và tổng; thêm BC-09..11 vào dispatcher |
| KH-01–03 | Tìm/thêm/sửa khách; kiểm tra số điện thoại, trùng và quyền; không sửa trực tiếp điểm trên form thông tin |
| KH-04–06 | Ẩn/xóa có giữ tham chiếu, lịch sử mua/điểm, tích điểm trong thanh toán đã có ở nhánh nền |
| KH-08 | Điều chỉnh có lý do, chỉ AD, không âm và ghi ledger đã có ở nhánh nền |
| KH-07 | Bảng/quy tắc hiệu lực được checkout đọc; chưa dùng báo cáo này để xác nhận hoàn thành toàn bộ UI cấu hình KH-07 |
| KM-01–04 | Danh sách/tạo/sửa/trạng thái đã có; URL ảnh nằm trong `chuong_trinh_khuyen_mai.anh_url`, entity và baseline PostgreSQL hiện tại |
| KM-05 / #247 | Tuần 8: trang xác nhận và xóa chỉ khi không có tham chiếu; chặn cả hóa đơn hủy/giỏ hàng, gợi ý Kết thúc |
| KM-06 / #246 | Tuần 8: lịch sử có hóa đơn, thời gian, TN, khách, tiền giảm và tổng; loại hóa đơn hủy; nguồn view dùng chung BC-11 |

Trong phạm vi #246–252 **không đề xuất cắt chức năng** khi người dùng đã yêu cầu triển khai đầy đủ. Việc cắt các Could khác của nhóm do người điều phối chốt và ghi bảng task; không tự tuyên bố toàn bộ 81 use case đã xong.

## 4. Luồng demo trên dữ liệu kiểm thử

1. Đăng nhập Admin → Khuyến mãi → Lịch sử sử dụng: đối chiếu mã hóa đơn, giảm giá và tổng; hóa đơn hủy không xuất hiện.
2. Tạo chương trình chưa dùng → Xóa → đọc thông báo, xác nhận; chương trình đã dùng bị chặn, dùng Kết thúc thay thế.
3. Báo cáo đổi trả → kỳ có phiếu hoàn tất → lọc lý do/thu ngân → xem dòng chi tiết → xuất Excel.
4. Báo cáo khách hàng → đối chiếu khách mới, chi tiêu và ledger điểm; phân biệt tích điểm, thu hồi và điều chỉnh.
5. Hiệu quả KM → lọc một CT / tất cả → kiểm tra một đơn dùng nhiều CT không bị cộng trùng tổng doanh thu → xuất Excel.
6. Thử đăng nhập TN/NK: gọi trực tiếp URL mới phải bị từ chối ở máy chủ. Thử khoảng ngày đảo hoặc quá 366 ngày; kỳ rỗng hiển thị E3.

## 5. Kiểm thử và ảnh minh chứng

Đã chạy 733/733 test đầy đủ; sau thay đổi cuối chạy lại 27/27 ca tuần 8/Excel; 53/53 kiểm tra HTTP và thao tác trình duyệt trên MySQL tạm. Xem [báo cáo kiểm thử và giới hạn phiên bản](../06-testing/test-reports/2026-11-23-week8.md), [dashboard](../06-testing/evidence/2026-11-23/week8/dashboard-complete.jpg), [BC-09](../06-testing/evidence/2026-11-23/week8/bc09-desktop.png), [BC-10](../06-testing/evidence/2026-11-23/week8/bc10-desktop.png), [BC-11](../06-testing/evidence/2026-11-23/week8/bc11-desktop.png), [khách hàng](../06-testing/evidence/2026-11-23/week8/customers-complete.jpg) và [lịch sử khuyến mãi](../06-testing/evidence/2026-11-23/week8/promotion-history-complete.jpg). Đây là ảnh dữ liệu kiểm thử, không phải kết quả kinh doanh thực tế.

## 6. Giới hạn và phần chờ xác nhận

- Chưa tự xác nhận người review #2 Yến, kiểm thử của #4 Huy, CI GitHub hoặc staging. Code/test cục bộ và đóng issue là hai bước khác nhau.
- Bố cục điện thoại hoãn theo yêu cầu người dùng; không ghi đã nghiệm thu responsive.
- Định nghĩa mẫu số tỷ lệ trả và doanh thu CT có đơn chồng nhau được ghi minh bạch trong hợp đồng; cần nhóm xác nhận cách hiểu.
- DT-03/DT-06/DT-08 thuộc người khác; báo cáo đọc đúng loại/trạng thái khi có dữ liệu nhưng không đồng nghĩa đợt này đã triển khai các chức năng DT đó.
- Nhánh nền có hai migration quỹ/ảnh đang chờ duyệt; tuần 8 không thêm migration. Không tự sửa baseline ERD/SRS đã chốt.
- Không ghi nhận “khảo sát thực tế” từ bộ tài liệu SIMULATED; số liệu trong ảnh/test không phải kết quả vận hành kinh doanh thực tế.
