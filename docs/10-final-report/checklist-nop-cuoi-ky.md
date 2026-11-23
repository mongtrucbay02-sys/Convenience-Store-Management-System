# T9-03 – Checklist gắn tag bản cuối và nộp sản phẩm (issue #258)

Soạn 23/11/2026 · #1 Phạm Gia Thăng. Gắn tag và tạo Release do **#5 Trung (admin GitHub, trưởng nhóm)** thực hiện; #1 kiểm tra đủ hồ sơ.

## 1. Điều kiện trước khi gắn tag

- [ ] PR #270 gộp vào nhánh của #232; PR #232 gộp vào `develop`; CI xanh
- [ ] PR #230 (vào `main`) và #231 (vào `develop`) đã gộp; xử lý xung đột `docs/09-ai-usage/ai-usage-log.md` khi đưa `develop` sang `main`
- [ ] `develop` gộp vào `main` qua PR, CI xanh trên `main`
- [ ] Đóng băng tính năng (#260, #262, #266) và sửa lỗi theo test report
- [ ] Danh sách use case cắt đã chốt và ghi vào SRS bản cuối

## 2. Gắn tag (do #5 thực hiện)

- Tag `sprint-5` trên `main` sau buổi demo sprint 5 (#235).
- Tag bản cuối trên `main`, ví dụ `v1.0-nop-cuoi-ky`, và tạo GitHub Release từ tag đó, mô tả: phạm vi đã làm, use case cắt, cách chạy, link báo cáo.
- Rubric (mục Sử dụng GitHub, 20%) yêu cầu "có release gắn tag" – hiện repo chưa có tag/release nào.

## 3. Hồ sơ nộp

| Hạng mục | Người | Issue | Tệp / vị trí | Xong |
| --- | --- | --- | --- | --- |
| Báo cáo cuối kỳ | #1 tổng hợp | #233, #257 | `02_T8-01_T9-02_Bao-cao-cuoi-ky/` | [ ] |
| SRS bản cuối | #1 | #257 | `04_T9-02_SRS-ban-cuoi/` → Word/PDF | [ ] |
| Mã nguồn theo tag bản cuối | #5 | #258 | Release trên GitHub | [ ] |
| Test report cuối | #4 | #264 | `docs/06-testing/` | [ ] |
| README cài đặt, chạy, deploy | #4 | #242 | `README.md` | [ ] |
| HDSD | #6 | #255 | | [ ] |
| Slide | #6 ghép | #268 | | [ ] |
| Video demo dự phòng | #6 | #269 (đã đóng 22/11, chưa thấy tệp) | | [ ] |
| Nhật ký AI đầy đủ mọi thành viên | Mỗi người | – | `docs/09-ai-usage/ai-usage-log.md` | [ ] |

## 4. Kiểm tra cuối trước khi nộp

- [ ] Mọi số liệu trong báo cáo khớp repo tại tag bản cuối
- [ ] Báo cáo bỏ trang "Hướng dẫn hoàn thiện bản nháp" và mọi đoạn tô vàng
- [ ] Tài liệu giả lập giữ nhãn SIMULATED
- [ ] Release có link tới báo cáo, slide, video
