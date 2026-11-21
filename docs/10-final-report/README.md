# 10 — Báo cáo cuối kỳ

> Khu vực **tổng hợp**: không tạo nội dung mới, mà rút gọn và trích dẫn từ `00`–`09`.

## Mục đích
Nơi chứa báo cáo cuối kỳ, slide bảo vệ và phụ lục nộp kèm.

## Nội dung

| Tài liệu | Mô tả | Trạng thái |
|---|---|---|
| `final-report.md` / `.docx` / `.pdf` | Báo cáo cuối kỳ | Chưa có |
| `slides/` | Slide thuyết trình bảo vệ | Chưa có |
| `appendices/` | Phụ lục: ERD, OpenAPI, test report, biên bản nghiệm thu (liên kết hoặc bản xuất) | Chưa có |
| `demo.md` | Kịch bản demo và tài khoản demo | Chưa có |

## Dàn ý báo cáo — mỗi chương lấy từ một khu vực

| Chương | Nội dung | Nguồn |
|---|---|---|
| 1. Giới thiệu và quản lý dự án | Bối cảnh, thành viên, kế hoạch, cách làm việc | `00-project-management` |
| 2. Khảo sát hiện trạng | Quy trình hiện tại, vấn đề | `01-survey` |
| 3. Định nghĩa sản phẩm | Tầm nhìn, người dùng, phạm vi MVP | `02-product-definition` |
| 4. Đặc tả yêu cầu | Chức năng, phi chức năng, Use Case, MoSCoW | `03-requirements` |
| 5. Thiết kế hệ thống | Kiến trúc, CSDL, API, giao diện | `04-system-design` |
| 6. Hiện thực | Công nghệ, cấu trúc mã nguồn, module đã làm | `05-implementation` |
| 7. Kiểm thử | Chiến lược, kết quả, lỗi tồn đọng | `06-testing` |
| 8. Triển khai | Môi trường, cách triển khai | `07-deployment` |
| 9. Nghiệm thu và đánh giá | Kết quả UAT, mức đáp ứng so với khảo sát | `08-validation` |
| 10. Sử dụng AI | Phạm vi dùng AI, kiểm chứng, bài học | `09-ai-usage` |
| 11. Kết luận và hướng phát triển | Tổng kết, việc sau MVP | Tổng hợp |
| Phụ lục | Ma trận truy vết, ERD, OpenAPI | `00`, `04` |

## Tiêu chí hoàn thành
- [ ] Mọi số liệu/kết luận trong báo cáo dẫn được về tài liệu gốc trong `00`–`09`.
- [ ] Ma trận truy vết đầy đủ được đưa vào phụ lục.
