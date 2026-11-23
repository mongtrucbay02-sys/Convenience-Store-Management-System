# 08 — Nghiệm thu

> **Trục chính:** 01 Khảo sát → 02 Định nghĩa sản phẩm → 03 Yêu cầu → 04 Thiết kế → 05 Hiện thực → 06 Kiểm thử → 07 Triển khai → **08 Nghiệm thu**

## Mục đích
Xác nhận sản phẩm **đúng nhu cầu người dùng** — khép vòng với khảo sát (`01`) và định nghĩa sản phẩm (`02`). Khác với `06` (kiểm thử đúng đặc tả), nghiệm thu hỏi: *người dùng có thực sự làm được việc của họ không?*

## Đầu vào → Đầu ra
- **Đầu vào:** hệ thống đã triển khai (`07`), tiêu chí thành công (`02-product-definition/success-metrics.md`), yêu cầu (`03`).
- **Đầu ra cho `10`:** biên bản nghiệm thu, mức độ đáp ứng yêu cầu, phản hồi và đề xuất cải tiến.

## Nội dung cần có

| Tài liệu | Mô tả | Trạng thái |
|---|---|---|
| `uat-plan.md` | Kế hoạch nghiệm thu: người tham gia (vai trò quản lý, thu ngân…), thời gian, tiêu chí chấp nhận | Chưa có |
| `uat-scenarios/` | Kịch bản nghiệm thu theo nghiệp vụ, mã `UAT-xx` (ví dụ: bán một đơn có khuyến mãi rồi đổi trả) | Chưa có |
| `uat-results.md` | Kết quả từng kịch bản: Đạt / Không đạt / Ghi chú | Chưa có |
| `feedback.md` | Phản hồi người dùng và hướng xử lý | Chưa có |
| `acceptance-record.md` | Biên bản nghiệm thu có xác nhận | Chưa có |

## Tiêu chí hoàn thành
- [ ] Mỗi yêu cầu MVP có kết quả nghiệm thu, cập nhật vào cột Nghiệm thu của ma trận truy vết.
- [ ] Đối chiếu lại với Problem Statement `PS-01…05` và câu hỏi xác minh `RV-01…08` ở `01-survey`: vấn đề nào đã giải quyết, vấn đề nào chưa.
