# Quy trình đóng góp

Tài liệu này ghi hai quy ước của nhóm: **quy trình thay đổi yêu cầu** và **danh sách người dự phòng** cho từng module (issue #30, mã task T4-08 trong Kế hoạch 9 tuần v3).

> Từ 22/11/2026, **#5 Nguyễn Tiến Trung** làm trưởng nhóm thay #1 Phạm Gia Thăng. Mọi chỗ ghi "trưởng nhóm" dưới đây là người đang giữ vai trò này.

## 1. Quy trình thay đổi yêu cầu

Áp dụng khi một yêu cầu đã ghi trong SRS, đặc tả use case, ERD hoặc OpenAPI cần đổi, thêm hoặc bỏ, kể cả khi lệch được phát hiện lúc lập trình.

1. **Tạo issue** có nhãn `thay đổi yêu cầu`. Nội dung issue ghi đủ:
   - mã yêu cầu hoặc use case bị ảnh hưởng (ví dụ `AUTH-07`, `BR-KM-03`) và mục SRS tương ứng;
   - nội dung hiện tại và nội dung đề nghị đổi;
   - lý do và nguồn phát sinh (PR, review, góp ý giảng viên, kết quả kiểm thử);
   - module và thành viên bị ảnh hưởng.
2. **Trưởng nhóm đánh giá ảnh hưởng** bằng một comment trên issue, gồm: phạm vi chức năng, thay đổi CSDL (có cần migration không), thay đổi API, test cần sửa, ảnh hưởng tới lịch sprint.
3. **Quyết định**: trưởng nhóm ghi một trong ba kết luận *Chấp nhận*, *Từ chối* hoặc *Hoãn* kèm lý do. Issue bị từ chối thì đóng với lý do; issue hoãn thì gắn vào milestone sau.
4. **Cập nhật tài liệu** trong cùng PR với code, hoặc PR riêng có liên kết tới issue:
   - thêm một dòng vào danh sách thay đổi SRS đang mở trong `docs/srs/` (ví dụ `SRS_v1.2_danh-sach-thay-doi.md` trên `develop`);
   - sửa đặc tả use case, OpenAPI, ERD nếu bị ảnh hưởng;
   - thay đổi cấu trúc CSDL chỉ được làm qua migration mới, theo tiêu chí trong `docs/quan-ly/nhat-ky-duyet-thay-doi-csdl.md` (trên `develop`).
5. **Đưa vào sprint**: gắn issue vào milestone của sprint sẽ làm và gán người phụ trách.
6. **Đóng issue** khi PR liên quan đã được review và gộp. Ghi số PR trong comment đóng issue.

Một thay đổi chưa đi qua các bước trên thì không được ghi là "Đã chốt" trong SRS.

## 2. Danh sách người dự phòng

Người dự phòng nắm được thiết kế và code của module, review PR của module đó và nhận tiếp việc khi người phụ trách không làm tiếp được.

| Module | Người phụ trách | Người dự phòng |
|---|---|---|
| Sản phẩm, Nhập hàng, Kho (SP, NH, KHO) | #2 Phan Hải Yến | #5 Nguyễn Tiến Trung, #3 Nguyễn Thị Thu Hoài |
| Bán hàng, Đổi trả (POS, DT) | #3 Nguyễn Thị Thu Hoài | #2 Phan Hải Yến, #5 Nguyễn Tiến Trung |
| Báo cáo, Khách hàng, Khuyến mãi (BC, KH, KM), POS-03 | #5 Nguyễn Tiến Trung | #2 Phan Hải Yến, #3 Nguyễn Thị Thu Hoài |
| Dịch vụ Ghi nhận biến động kho | #1 Phạm Gia Thăng | #2 Phan Hải Yến |
| Xác thực và phân quyền (AUTH) | #1 Phạm Gia Thăng | Chưa phân công |
| Kiểm thử, CI, vận hành CSDL | #4 Tạ Quang Huy | Chưa phân công |
| Giao diện dùng chung, mẫu in | #6 Cao Huyền Trang | Chưa phân công |

Các cặp dự phòng của SP/NH/KHO, POS/DT, BC/KH/KM và dịch vụ kho lấy theo issue #30 (`#2 ↔ #5`, `#3 ↔ #2`, `#5 ↔ #3`; #1 và #2 cùng nắm dịch vụ kho). Các dòng "Chưa phân công" do trưởng nhóm bổ sung.

## 3. Nhãn liên quan

| Nhãn | Dùng khi |
|---|---|
| `thay đổi yêu cầu` | Issue đề nghị đổi yêu cầu theo mục 1 |
| `Must`, `Should`, `Could` | Mức ưu tiên MoSCoW |
| `sprint-N`, `tuan-N` | Sprint và tuần thực hiện |
| `tv-1` … `tv-6` | Thành viên phụ trách |
