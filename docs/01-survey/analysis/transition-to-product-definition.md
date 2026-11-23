# Handoff — From Survey Problem to Product Definition

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Các cuộc phỏng vấn, quan sát, findings và vấn đề đều là kịch bản AI dựng. Tài liệu này chỉ phục vụ luyện tập. Không trình bày như kết luận đã được xác minh tại cửa hàng thật.


**Document ID:** SM-SUR-PS-HANDOFF-001 · **Status:** CONDITIONAL / SIMULATED.

## 1. Các nhu cầu ứng viên từ Bước 4

| Candidate | Mô tả ngắn | Liên quan Problem Statement | Trạng thái |
|---|---|---|---|
| N-SIM-01 | Tìm sản phẩm theo mã và tên | PS-02 | Candidate only |
| N-SIM-02 | Ghi số giao/số thực nhận/chênh lệch | PS-01 | Candidate only |
| N-SIM-03 | Ghi nhận biến động tồn theo trạng thái xác nhận | PS-01 | Candidate only |
| N-SIM-04 | Kiểm kê có tồn ghi, tồn đếm và duyệt điều chỉnh | PS-01/PS-04 | Candidate only |
| N-SIM-05 | Tra cứu/in lại hóa đơn theo quyền | PS-02/PS-04 | Candidate only |
| N-SIM-06 | Phân quyền và lưu dấu vết thao tác nhạy cảm | PS-04 | Candidate only |
| N-SIM-07 | Tổng hợp và đối soát doanh thu | PS-03 | Candidate only |
| N-SIM-08 | Thông tin tồn và thiết bị phù hợp quy mô nhỏ | CON-01 | Candidate only |

**Đặc biệt:** PS-05 (kiểm tra nhận tiền chuyển khoản) **chưa được chốt thành giải pháp hay API ngân hàng**. Cần điều tra quy trình xác minh trước.

## 2. Đầu vào giai đoạn 2
- Xây dựng **Product Vision** bằng ngôn ngữ nhu cầu/giá trị người dùng, không quảng bá giải pháp chưa được chứng minh.
- Dựng **Proto-personas** (từ mô phỏng) cho P01/P02/P03; không gọi là Personas được nghiên cứu thực địa.
- Viết Scenarios dựa trên As-is và tìm hướng cải tiến, ghi giả định.
- Xây dựng Feature Candidates rồi ưu tiên bằng MoSCoW sau khi có phản hồi cửa hàng và đánh giá nguồn lực.
- Chọn MVP có luồng nghiệp vụ trọn vẹn; thêm Acceptance Criteria và Test Strategy sau khi xác minh.

## 3. Tiêu chí chuyển giai đoạn
**Cho mục đích luyện tập:** được xây Draft Product Vision và Proto-personas, mọi kết luận vẫn gắn nhãn SIMULATED.  
**Cho báo cáo nghiên cứu thực tế:** cần khảo sát thật, xác nhận findings, duyệt Problem Statement trước khi chốt Product Vision/SRS/MVP.
