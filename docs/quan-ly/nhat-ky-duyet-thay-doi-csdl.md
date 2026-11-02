# Nhật ký duyệt thay đổi CSDL (T6-R1, T7-R1)

Theo Kế hoạch v3 mục 1.4: mọi thay đổi cấu trúc CSDL phải đi qua tệp migration trong pull request và được #1 duyệt; không ai sửa trực tiếp CSDL dùng chung. Migration nằm ở `src/ConvenienceStore.Web/Data/Migrations/`. CODEOWNERS trước đây ghi nhầm `/src/ConvenienceStore.Web/Migrations/` nên PR có migration không tự gán #1; PR này sửa lại đường dẫn.

Nguồn chuẩn của cấu trúc CSDL: `docs/db/schema_v1.sql` (ERD v1, 18/10/2026) và migration `InitialCreate` trên `develop`, sinh bằng `CuaHangDbContext` (scaffold từ CSDL, ADR-001 mục 3.8). Thay đổi mới viết thành migration tiếp theo của `CuaHangDbContext`, không tạo DbContext thứ hai.

## Lượt rà 02/11/2026 (tuần 6)

Rà toàn bộ pull request đang mở có tệp migration, `.sql`, DbContext hoặc entity.

| PR | Tác giả | Nhánh đích | Thay đổi CSDL | Kết luận | Lý do và việc cần làm |
| --- | --- | --- | --- | --- | --- |
| #129 [T4-27] POS-09 hủy đơn đang bán | #3 Hoài | `develop` | Thêm bảng `pos_cancelled_orders` bằng tệp SQL rời | **Không duyệt; đề nghị đóng** | ERD v1 đã lưu đơn hủy trong `gio_hang` (`trang_thai = 'DA_HUY'`, lý do, giá trị, thời điểm hủy). POS-09 trên `develop` đã dùng cách này và chạy đúng khi thử 01/11. Bảng mới sẽ trùng dữ liệu, không có khóa ngoại, và không đi qua migration. PR đang xung đột với `develop` |
| #130 Feature/db core codefirst baseline | #2 Yến | `main` | `ApplicationDbContext` code-first riêng, 3 migration "SchemaProbe" | **Không duyệt** | Tạo DbContext thứ hai song song với `CuaHangDbContext`; hai bộ migration sẽ dựng hai CSDL khác nhau. Đã được thay bằng #168 |
| #168 [DB] Complete Code First Schema | #2 Yến | `main` | Như #130, mở rộng đủ các bảng (116 tệp, khoảng 44.000 dòng) | **Không duyệt**; giữ lại phần test | Cùng lý do với #130. Đề nghị: chuyển code SP-07 (#169) và test tích hợp sang `CuaHangDbContext` trên `develop`; nếu thấy ERD v1 thiếu cột hay ràng buộc nào thì mở issue nhãn "thay đổi yêu cầu", #1 thêm migration mới |
| #169 [SP-07] Category List API | #2 Yến | nhánh của #168 | Phụ thuộc DbContext của #168 | Chờ chuyển nhánh | Code nghiệp vụ dùng được; chỉ cần đổi sang `CuaHangDbContext` và nhắm `develop` |
| #194, #195 Báo cáo BC-02 → BC-12 | #5 Trung | `main` | Không có tệp CSDL | Không thuộc phạm vi duyệt CSDL | Lưu ý: nhắm `main` trong khi AUTH, kho, POS ở `develop`; nên nhắm `develop` để không làm hai nhánh lệch thêm (lỗi 5 trong bản đánh giá) |
| #196, #197 SP-08, NH-01 (Draft) | #2 Yến | nhánh code-first | Không có tệp migration mới | Chờ | Sẽ cần chuyển nhánh như #169 |

Kết quả: không có thay đổi CSDL nào được gộp mà chưa duyệt. Không có migration mới nào cần thêm vào `CuaHangDbContext` trong tuần 6. T-KHO-SV2 (#140) không đổi cấu trúc CSDL.

## Mẫu ghi cho các lượt sau (T7-R1 và về sau)

| Ngày | PR | Tác giả | Thay đổi CSDL | Kết luận | Ghi chú |
| --- | --- | --- | --- | --- | --- |
| | | | | | |

Tiêu chí duyệt một migration:

1. Sinh từ `CuaHangDbContext`, đặt trong `src/ConvenienceStore.Web/Data/Migrations/`, có cả `Up()` và `Down()`.
2. Không sửa hay xóa migration đã gộp; mọi thay đổi là migration mới.
3. Khớp ERD v1 hoặc có issue "thay đổi yêu cầu" đã được #1 đồng ý; cập nhật `docs/db/schema_v1.sql` trong cùng PR.
4. Không phá trigger và CHECK hiện có (chạy `docs/db/kiem_thu.sh`: 29/29 thao tác sai vẫn bị chặn).
5. `dotnet test` đạt trên MySQL 9.7.2.
