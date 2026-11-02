# Kịch bản báo cáo giữa kỳ (T6-01)

| | |
| --- | --- |
| Issue | #141 – T6-01; dùng cho #164 – T6-05 Báo cáo giữa kỳ |
| Người soạn | #1 Phạm Gia Thăng · 02/11/2026 |
| Thời lượng | 10 phút trình bày + 5 phút hỏi đáp (theo "Lưu ý lấy điểm giữa kỳ") |
| Slide | Bản 16 slide `Slide_giua_ky_Nhom7_SmartMart` (#6 hoàn thiện trên Google Slides, T6-10) |
| Ngày báo cáo | `__/11/2026` – #1 điền khi lớp chốt lịch. Mốc đóng băng mã bên dưới tính lùi từ ngày này |

## 1. Thứ tự trình bày

Mỗi người trình bày phần mình sở hữu, để khi giảng viên hỏi thì người trả lời là người làm. Thời gian cộng lại 9 phút 30 giây, chừa 30 giây chuyển người.

| Slide | Nội dung (yêu cầu trong "Lưu ý") | Người nói | Thời gian |
| --- | --- | --- | --- |
| 1–2 | Mở đầu, lý do chọn đề tài | #1 Thăng | 0:40 |
| 3 | Phân tích thị trường | #6 Trang (khảo sát hiện trạng) | 0:40 |
| 4–5 | Product vision, phạm vi MVP, persona | #1 Thăng | 0:50 |
| 6–7 | Product backlog (81 use case, MoSCoW), user story có tiêu chí chấp nhận | #5 Trung | 1:00 |
| 8–9 | Tổ chức nhóm, lưu trữ tài liệu và mã nguồn trên GitHub | #4 Huy (admin GitHub, CI) | 0:50 |
| 10–11 | Kế hoạch 9 tuần, phân công task theo tuần | #1 Thăng | 0:50 |
| 12 | Kiến trúc phần mềm, CSDL (ERD v1, 28 bảng) | #2 Yến (khung máy chủ) | 1:00 |
| 13 | UI/UX: từ khung 20 màn hình đến màn hình thật | #6 Trang | 0:50 |
| 14 | Kết quả đến tuần 6 + **demo trực tiếp** (mục 2) | #3 Hoài | 2:00 |
| 15–16 | Việc tiếp theo, xin góp ý, cảm ơn | #1 Thăng | 0:30 |

## 2. Demo trực tiếp (2 phút, #3 thao tác, #1 cầm máy dự phòng)

Chỉ demo những luồng đã chạy ổn trên bản đóng băng. Lần chạy thử 02/11 trên MySQL 9.7.2 cho thấy các luồng dưới đây chạy đúng.

1. Đăng nhập bằng tài khoản thu ngân `thungan.demo` (đã đổi mật khẩu sẵn).
2. Màn hình bán hàng: tìm "Aquafina", thêm 3 chai; thêm 1 "Coca-Cola".
3. Thanh toán tiền mặt, khách đưa 50.000 đ; chỉ ra tiền thừa.
4. In hóa đơn K80 (`/Pos/Receipt`).
5. Mở danh sách hóa đơn (`/Invoices`), chỉ ra hóa đơn vừa tạo.
6. Nếu còn thời gian và màn hình thẻ kho (KHO-02, #2) đã gộp trước giờ đóng băng: mở thẻ kho của Aquafina để thấy dòng `BAN_HANG -3`, nói về dịch vụ kho và khóa `FOR UPDATE`. Chưa có màn hình thì chỉ nói, không mở.

**Không mở** trong buổi báo cáo, trừ khi đã sửa và chạy thử lại trước giờ đóng băng: trang Sản phẩm, Báo cáo, Dashboard, Khuyến mãi (đang lỗi 500 hoặc mở công khai; xem bản đánh giá repo, lỗi 1, 2, 5).

Dữ liệu: chạy `seed_du_lieu_mau.sql` trên bản demo sau khi khởi động ứng dụng lần đầu (15 sản phẩm, có tồn). #4 sao lưu CSDL demo ngay sau khi nạp dữ liệu (T6-03, NFR-BT-06).

Dự phòng: #6 quay sẵn video 2 phút của đúng các bước trên (từ bản đóng băng). Mạng hoặc máy lỗi thì chiếu video, không sửa tại chỗ.

## 3. Đóng băng mã

| Mốc (tính lùi từ ngày báo cáo D) | Việc | Ai |
| --- | --- | --- |
| D−3, 18:00 | Hạn cuối gộp PR tính năng vào `develop` cho buổi giữa kỳ | Cả nhóm |
| D−2, 18:00 | **Đóng băng**: từ đây chỉ gộp PR sửa lỗi có nhãn `bug`, cần 1 review và test đạt | #1 điều phối |
| D−2 | Dựng bản demo từ commit đóng băng, nạp dữ liệu mẫu, sao lưu | #4 |
| D−1 | Tập toàn bộ 10 phút có bấm giờ; quay video dự phòng | Cả nhóm; #6 quay |
| D | Mang theo laptop dự phòng đã cài sẵn bản demo | #1 |

Ghi commit đóng băng vào issue #164 và gắn tag `sprint-3` lên commit đó.

## 4. Phân công trả lời câu hỏi (5 phút)

| Câu hỏi có thể gặp | Người trả lời chính |
| --- | --- |
| Vì sao chọn monolith chia module, MySQL, Razor MVC thay cho React như SRS ban đầu | #1 (ADR-001), #2 |
| Hai thu ngân cùng bán món cuối cùng thì sao | #1 (dịch vụ kho), #3 |
| Test thế nào, bao nhiêu test, có CI chưa | #4 |
| Phân quyền, mật khẩu, phiên đăng nhập | #1 |
| Màn hình, khả năng dùng trên điện thoại | #6 |
| Tính năng AI là gì, dùng công cụ AI ra sao trong quá trình làm | #1, mỗi người nói phần mình theo nhật ký AI |
| Đóng góp của từng người | Người được hỏi tự trả lời; minh chứng là issue, PR, commit đứng tên |

## 5. Việc còn lại

- [ ] #1 điền ngày báo cáo và mốc D ở mục 3, gửi kịch bản cho #6 (T6-10)
- [ ] #4 chốt bản demo và tài khoản `thungan.demo` (T6-03)
- [ ] #6 cập nhật ghi chú người nói trên từng slide theo bảng mục 1
- [ ] #5 ghi biên bản góp ý sau buổi báo cáo (T6-09)
