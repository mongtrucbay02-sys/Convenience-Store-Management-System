# T9-01 – Câu hỏi dự kiến khi bảo vệ (issue #256)

Soạn 23/11/2026 · #1 Phạm Gia Thăng.

> **Lịch tổng duyệt và phân thời gian thuyết trình** (cũng thuộc T9-01) **sẽ được đưa lên repo sau**, khi trưởng nhóm (#5) chốt giờ và thứ tự người nói.

## Câu hỏi dự kiến và người trả lời

Nguồn: "Câu hỏi bảo vệ bắt buộc" trong Project Guidelines mục 5 (tuần 10), Rubric (vấn đáp 20%: giải thích quyết định kỹ thuật và **đóng góp cá nhân trên GitHub**), và các điểm yếu đã biết của repo.

### 1. Sản phẩm và phạm vi

| # | Câu hỏi | Người trả lời | Ý chính / minh chứng |
| --- | --- | --- | --- |
| 1 | Sản phẩm làm lại từ phần mềm nào, giải quyết vấn đề gì? | #1 | Hướng 2 của Guidelines; KiotViet, Sapo, MISA dư thừa cho cửa hàng đơn lẻ; vision slide 3 |
| 2 | Vì sao chọn MVP này? Use case nào bị cắt và vì sao? | #1, #5 | 27 Must; thứ tự cắt theo Kế hoạch v3 mục 1.4; danh sách cắt nhóm chốt ở buổi demo sprint 5 (#235) |
| 3 | Khảo sát người dùng thật ở đâu? | #6 | Phải nói thật: tài liệu giai đoạn 1 là giả lập (AI-001); nếu có khảo sát thật thì đưa số liệu |
| 4 | Vì sao chỉ một cửa hàng, không làm đa chi nhánh hay chấm công? | #1 | Vision theo SRS: cửa hàng đơn lẻ; đa chi nhánh, chấm công ngoài phạm vi (SRS 1.2) để giữ MVP vừa sức 6 người |

### 2. Kỹ thuật

| # | Câu hỏi | Người trả lời | Ý chính / minh chứng |
| --- | --- | --- | --- |
| 5 | Vì sao monolith, không microservices? | #1 | Một giao tác qua kho, điểm, khuyến mãi; ADR-001 |
| 6 | Hai thu ngân cùng bán món cuối thì sao? | #1, #3 | Dịch vụ kho khóa dòng `FOR UPDATE`; test bán đồng thời |
| 7 | Vì sao đổi React sang Razor MVC? | #1, #3 | ADR-002 (đề xuất); khung Razor có trước code React |
| 8 | Hủy hóa đơn ảnh hưởng tồn, điểm, quỹ thế nào? | #3, #5 | Một giao tác: trả tồn, thu hồi điểm, ghi hoàn tiền và chi quỹ; chỉ hủy được một lần |
| 9 | Báo cáo lợi nhuận gộp tính giá vốn thế nào? | #5 | Giá vốn bình quân gia quyền lưu trên dòng hóa đơn lúc bán |
| 10 | Bảo mật: phiên, mật khẩu, phân quyền? | #1 | Cookie HttpOnly, token băm, XSRF, 27 mã quyền kiểm ở máy chủ |
| 11 | Test nào chứng minh chức năng chạy đúng? Vì sao không dùng InMemory? | #4, #5 | Test chạy trên CSDL thật trong CI (709/709 trên MySQL 8.4 ngày 23/11; số liệu PostgreSQL cập nhật khi chuyển xong); InMemory không kiểm được trigger, CHECK, khóa dòng |
| 12 | Vì sao đổi từ MySQL sang PostgreSQL/Supabase ở cuối kỳ? Đã chuyển xong chưa? | #5, #2 | ADR-003: lý do, phương án đã cân nhắc, điều kiện hoàn tất, tag bản lùi MySQL; trả lời đúng hiện trạng tại tag bản cuối |

### 3. Quy trình, AI và đóng góp

| # | Câu hỏi | Người trả lời | Ý chính / minh chứng |
| --- | --- | --- | --- |
| 13 | Tính năng AI là gì? Dữ liệu nào gửi vào AI? | #1, #5 | AI-01 thiết kế trong ADR-001 mục 3.11; nếu chưa làm, nói rõ đã cắt và lý do |
| 14 | AI đã giúp những việc gì ngoài viết code? Prompt tốt nhất? AI tạo lỗi gì, phát hiện bằng cách nào? | Mỗi người | Nhật ký AI `docs/09-ai-usage/`; ví dụ: review phát hiện migration quỹ không chạy lại được (PR #270) |
| 15 | Nhóm có đọc diff không? Review PR thế nào? | #5 | Thừa nhận nhiều PR gộp không review; từ 22/11 có bảo vệ `main`, từ 23/11 có CI |
| 16 | Phần đóng góp của bạn trên GitHub là gì? | Mỗi người | Mỗi người chuẩn bị 2–3 PR hoặc commit tiêu biểu; bảng đóng góp ở Phụ lục A báo cáo |
| 17 | Vì sao nhiều issue đóng mà không có code, hoặc có code mà issue vẫn mở? | #5, #1 | `Closes #` chỉ tự đóng khi PR vào nhánh mặc định; đã rà và sửa trạng thái ngày 22/11 |
| 18 | Nếu bỏ tính năng AI, sản phẩm còn giá trị không? | #1 | Có: giá trị chính là tồn kho khớp chứng từ và bán hàng một màn hình |

Câu 16 là câu chắc chắn được hỏi (Rubric). Thành viên không có commit hoặc PR trên repo cần chuẩn bị minh chứng khác ngay.
