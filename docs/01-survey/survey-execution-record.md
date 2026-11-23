# Bước 3 — Survey Execution Record (Nhật ký diễn tập)

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Tất cả tên, lời nói, quy trình, số liệu, thời gian và tình huống ở đây là do AI dựng để luyện tập. Không được trình bày như bằng chứng khảo sát thật trong báo cáo.

**ID:** SM-SUR-EXEC-001 · **Scenario date:** 2026-11-21 · **Status:** SIMULATION COMPLETED; REAL FIELDWORK NOT CONDUCTED.

## Người tham gia và bối cảnh

| Code | Role | Nature |
|---|---|---|
| ENG-01 | Software Engineer / Interviewer | AI-simulated interviewer |
| P01 | Store Owner | AI-simulated participant |
| P02 | Cashier | AI-simulated participant |
| P03 | Warehouse Staff | AI-simulated participant |

Cửa hàng `S-01` là bối cảnh giả lập; không có địa điểm, người tham gia, tài liệu giao dịch hay giấy đồng ý từ người thật.

## Nhật ký phiên diễn tập

| Slot | Record | Event | Status |
|---|---|---|---|
| 13:30–13:40 | Opening | Giới thiệu mục tiêu và sự đồng ý *trong kịch bản* | Simulated |
| 13:40–14:10 | [INT-001](interviews/records/INT-001-owner.md) | Phỏng vấn Chủ cửa hàng | Simulated |
| 14:10–14:35 | [INT-002](interviews/records/INT-002-cashier.md) | Phỏng vấn Thu ngân | Simulated |
| 14:35–15:05 | [INT-003](interviews/records/INT-003-warehouse.md) | Phỏng vấn Nhân viên kho | Simulated |
| 15:05–15:30 | [OBS-001](observations/records/OBS-001-sales.md) | Quan sát bán hàng giả lập | Simulated |
| 15:30–15:55 | [OBS-002](observations/records/OBS-002-purchasing.md) | Quan sát nhập hàng giả lập | Simulated |
| 15:55–16:15 | [OBS-003](observations/records/OBS-003-inventory.md) | Quan sát tồn kho giả lập | Simulated |
| 16:15–16:30 | Closing | Đối chiếu các điểm chưa rõ | Simulated |

## Tóm lược lời diễn tập (không phải kết luận khảo sát)

- P01 nhấn mạnh quản lý tồn kho, tra cứu giao dịch và báo cáo doanh thu cuối ngày.
- P02 nhấn mạnh tìm sản phẩm không quét được, xác nhận thanh toán và in lại hóa đơn.
- P03 nhấn mạnh ghi nhận **số thực nhận**, xử lý chênh lệch nhập hàng và đối chiếu tồn kho.
- Kịch bản quan sát mô phỏng một lỗi quét mã, một phiếu giao thiếu 2 chai và một chênh lệch tồn 18/17; tất cả đều là **tình huống diễn tập được tạo ra**.

## Buổi đối chiếu mô phỏng (ENG-01 với P01–P03)

**ENG-01:** Tôi hiểu phải phân biệt số giao trên phiếu và số thực nhận; tồn kho không nên cộng theo số phiếu khi chưa xác nhận. Mọi người có đồng ý với cách mô tả kịch bản này không?  
**P01:** Đúng trong tình huống mô phỏng; tôi muốn những chênh lệch được xác minh.  
**P03:** Tôi cần chỗ ghi thiếu/thừa rõ ràng.  
**P02:** Ở quầy, tôi cũng cần thấy cảnh báo nếu không bán được mặt hàng.

**ENG-01:** Tôi chưa có số liệu đo lỗi, tần suất hoặc công cụ thật, nên sẽ giữ các nội dung đó ở trạng thái chưa xác minh.

## Chất lượng nguồn và giới hạn

- Các câu trả lời đều do AI dựng; **không có lời chứng người thật**.
- Các quan sát đều là tình huống được mô phỏng; **không có quan sát thực địa**.
- Không có audio, ảnh hiện trường, phiếu giao/hóa đơn thật hoặc xác nhận của cửa hàng.
- Không thể kết luận có bất kỳ vấn đề nào thực sự tồn tại ở một cửa hàng cụ thể.

## Kết quả học tập

- [x] Có hồ sơ đối thoại giả lập cho ba vai trò.
- [x] Có sơ bộ As-is từ ba kịch bản diễn tập để luyện phân tích.
- [x] Có mã nguồn trích dẫn nội bộ và danh mục giới hạn.
- [ ] Chưa đạt điều kiện xác minh khảo sát thực địa.
- [ ] Chưa được dùng làm bằng chứng nghiệm thu yêu cầu trong báo cáo cuối kỳ.
