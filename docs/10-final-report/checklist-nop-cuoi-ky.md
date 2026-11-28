# T9-03 – Checklist nghiệm thu hồ sơ, gắn tag bản cuối và nộp sản phẩm

Soạn 23/11/2026, kiểm tra lại 28/11/2026 · #1 Phạm Gia Thăng.
- Gắn tag và tạo Release do **#5 Trung** (admin GitHub, trưởng nhóm) thực hiện.
- #1 nghiệm thu hồ sơ hai lần: **trước** khi gắn tag (28/11, bảng dưới) và **sau** khi gắn tag (29/11, đối chiếu đúng commit của tag).
- Issue #258 (gắn tag, nộp) đã đóng "not planned"; checklist này thay cho issue đó.
- Bảo vệ ngày 04/12/2026.

Trạng thái: ✅ xong · ⏳ đang làm/chờ review · ❌ chưa có.

## 1. Điều kiện trước khi gắn tag (kiểm 28/11)

| Điều kiện | Trạng thái | Ghi chú |
| --- | --- | --- |
| #232, #270, #271, #230, #231, #272, #275 gộp | ✅ | |
| CSDL PostgreSQL 17 (ADR-003, #286); CI `develop` 877/877 | ✅ | #1 chạy lại cục bộ 27/11: 877/877 |
| Duyệt CSDL `PostgreSqlBaseline` | ✅ | Biên bản 28/11 (PR #302) |
| Phạm vi use case chốt trong SRS bản cuối | ⏳ | PR #301; DT-03, DT-06 phụ thuộc #291 |
| PR #291 (DT-03/06/07) | ⏳ | Đang bị yêu cầu sửa |
| Đóng băng tính năng, sửa lỗi theo test report (#260, #262, #266) | ⏳ | |
| Ghi dữ liệu trên Supabase bằng `smartmart_backend`, luồng demo (#293, PR #300 WR-06 → 10) | ❌ | 4 lỗi P0 khóa `vai_tro`, chưa sửa code |
| `develop` → `main` (PR #277), CI xanh trên `main` | ❌ | `main` thiếu khoảng 175 commit. Gộp bằng **"Create a merge commit"**, không squash |
| README ghi đúng cách chạy bản nộp | ✅ | `README.md`: .NET 10, PostgreSQL 17, `.env` theo `.env.example` |
| Tag bản lùi MySQL (ADR-003 mục 4) | ❌ | Tùy nhóm quyết định |

## 2. Gắn tag (do #5)

- Tag bản cuối trên `main`, ví dụ `v1.0-nop-cuoi-ky`, kèm GitHub Release: phạm vi đã làm, use case cắt, cách chạy, link báo cáo, slide, video.
- Rubric (Sử dụng GitHub, 20%) yêu cầu "có release gắn tag". Ngày 28/11 repo **chưa có tag hay Release nào**.

## 3. Hồ sơ nộp (kiểm 28/11)

| Hạng mục | Người | Vị trí | Trạng thái |
| --- | --- | --- | --- |
| Báo cáo cuối kỳ | #1 tổng hợp | PR #303 → Google Docs | ⏳ Phần #1 xong; chương thành viên: Huy có bản nháp (PR #304), Hoài có bản Word (PR #292), Trung có `week8-bc-kh-km-report.md`; Yến (#236), Trang (#254) chưa có |
| SRS bản cuối (Word, PDF) | #1 | PR #301 | ⏳ Word bật theo dõi thay đổi; xuất PDF sau khi chốt DT-03/06 |
| Ma trận 27 Must, NFR chính thức | #1 | PR #301 | ✅ |
| Tài liệu CSDL (ADR-003, biên bản duyệt, đối chiếu Supabase) | #1 | PR #302 | ✅ chờ review |
| Security contract | #2 | PR #300 | ⏳ đề xuất, D1 → D7 chờ duyệt |
| Test report cuối | #4 | PR #304 (#264) | ⏳ bị yêu cầu sửa QA-01 → 07 |
| Mã nguồn theo tag | #5 | Release | ❌ |
| HDSD | #6 | #255 | ❌ issue đóng "not planned" – báo cáo mục 5.1 thay thế |
| Slide | #6 ghép | #268 | ⏳ issue đóng "not planned" nhưng lịch giao #6 ngày 29/11; 10 slide của #1 ở `docs/10-final-report/slides/` |
| Video demo dự phòng | #6 | #269 (đóng "completed") | ❌ chưa thấy tệp trong repo hoặc thư mục chung |
| Nhật ký AI mọi thành viên | Mỗi người | `docs/09-ai-usage/ai-usage-log.md` | ⏳ chỉ có #1 (AI-001 → 007) và #5 (AI-TRUNG-001 → 005); thiếu #2, #3, #4, #6 |

## 4. Kiểm tra cuối (29/11, sau khi có tag)

- [ ] Mọi số liệu trong báo cáo và SRS khớp repo tại commit của tag (số test, số bảng, phạm vi use case)
- [ ] Báo cáo bỏ trang "Hướng dẫn ghép" và mọi khung vàng
- [ ] Tài liệu giả lập giữ nhãn SIMULATED
- [ ] Release có link tới báo cáo, slide, video
- [ ] Ngay trước bảo vệ: mở project Supabase (gói Free tự tạm dừng khi không có truy cập); sao lưu dữ liệu; Admin chốt số dư mở quỹ
