# Chuẩn bị nộp bài tập quá trình 2 (T7-06)

| | |
| --- | --- |
| Issue | #171 – T7-06 |
| Người phụ trách | #1 Phạm Gia Thăng |
| Hạn | 13/11/2026 (cuối tuần 7) |
| Deliverable theo Kế hoạch v3 | 6 module chủ chốt + Khách hàng + Khuyến mãi đầy đủ, Đổi trả phần chính; kết quả kiểm thử phi chức năng |

## 1. Điều kiện để nộp

Chỉ nộp khi các mục dưới đây đạt trên đúng commit sẽ nộp (ghi mã commit vào issue #171).

| # | Điều kiện | Cách kiểm | Ai |
| --- | --- | --- | --- |
| 1 | `main` và `develop` đã gộp về một bản; mọi tính năng nằm trên `main` | So sánh hai nhánh trên GitHub không còn commit lệch | #1, #5 |
| 2 | Build không lỗi, toàn bộ test đạt | `dotnet test` trên MySQL 9.7.2; CI xanh nếu đã có `ci.yml` | #4 |
| 3 | Không còn 7 lỗi chặn demo trong bản đánh giá repo ngày 01/11 (trang 500, Dashboard công khai, luồng đăng nhập, mật khẩu tạm, README…) | Mở từng trang bằng tài khoản TN, NK, AD | #4 |
| 4 | Chạy được từ đầu theo README trên một máy sạch | Một người không code phần đó làm theo README | #6 |
| 5 | Có dữ liệu mẫu để demo | `seed_du_lieu_mau.sql` hoặc script của #4 | #4 |
| 6 | Kết quả kiểm thử chức năng (T7-04) và phi chức năng (T7-08) | Tệp trong `docs/tests/` | #4 |
| 7 | Issue đã xong được đóng, có link PR; issue chưa xong còn mở với ghi chú lý do | Lọc milestone Tuần 7 | Cả nhóm |
| 8 | Nhật ký AI của tuần 6–7 đã ghi | `docs/ai-logs/` hoặc `06_NHAT_KY_AI.md` | Cả nhóm |

## 2. Đối chiếu deliverable với use case

Điền cột "Trạng thái" bằng: Xong (đã gộp, #4 test đạt) / Một phần / Chưa.

| Nhóm | Use case cần có | Trạng thái | Ghi chú |
| --- | --- | --- | --- |
| AUTH | AUTH-01 → 10 (trừ AUTH-04 của #6) | | |
| SP | SP Must + Should | | |
| NH | NH Must + Should | | |
| KHO | KHO-01 → 04, 06, 07; KHO-05, 08 (Could) | | |
| POS | POS-01 → 12 | | |
| BC | BC Must + Should | | |
| KH | KH-01 → 08 | | |
| KM | KM-01 → 06, áp vào đơn (POS-04) | | |
| DT | DT-01, 02, 04, 05 | | |
| Phi chức năng | Bảng kết quả theo mã NFR | | |

## 3. Các bước ngày nộp

1. Gộp PR cuối, chờ CI xanh.
2. Gắn tag `sprint-4` trên commit nộp; tạo GitHub Release `v0.2.0` với ghi chú: tính năng đã có, cách chạy, kết quả test.
3. Tải bản nén mã nguồn từ Release (không nén thư mục trên máy, để không lẫn `bin/`, `obj/`, `.env`).
4. Nộp theo hình thức lớp yêu cầu; chụp màn hình xác nhận nộp và đính kèm vào issue #171.
5. Ghi vào bảng task: T7-06 "Hoàn thành", ngày nộp.
