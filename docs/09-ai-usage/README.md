# 09 — Sử dụng AI

> Khu vực **xuyên suốt**: AI có thể được dùng ở bất kỳ giai đoạn nào từ `00` đến `08`; mọi lần dùng đều được ghi nhận tại đây.

## Mục đích
Minh bạch việc dùng công cụ AI (ChatGPT, Gemini, Copilot…) trong dự án: dùng ở đâu, để làm gì, ai kiểm chứng, và phần nào là sản phẩm của nhóm.

## Nội dung

| Tài liệu | Mô tả | Trạng thái |
|---|---|---|
| `ai-policy.md` | Quy định của nhóm: được dùng AI cho việc gì, không được dùng cho việc gì, yêu cầu kiểm chứng | Chưa có |
| [`ai-usage-log.md`](ai-usage-log.md) | Nhật ký từng lần dùng AI, gắn với giai đoạn và tài liệu/PR bị ảnh hưởng | Khung sẵn |
| `prompts/` | Lưu prompt quan trọng có thể tái sử dụng | Chưa có |
| `evaluation.md` | Đánh giá cuối kỳ: AI giúp ở đâu, sai ở đâu, bài học | Chưa có |

## Quy tắc ghi nhận
1. Mỗi PR có nội dung do AI tạo hoặc sửa đáng kể → thêm một dòng vào `ai-usage-log.md` và tích ô tương ứng trong mẫu PR.
2. Ghi rõ **người kiểm chứng** — người chịu trách nhiệm nội dung cuối cùng.
3. Không dán mật khẩu, chuỗi kết nối, dữ liệu khách hàng thật vào công cụ AI.

## Liên kết
- `10-final-report` dùng `evaluation.md` và số liệu tổng hợp từ nhật ký.
