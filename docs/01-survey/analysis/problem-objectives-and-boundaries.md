# Preliminary Objectives, Boundaries & Non-goals

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Các cuộc phỏng vấn, quan sát, findings và vấn đề đều là kịch bản AI dựng. Tài liệu này chỉ phục vụ luyện tập. Không trình bày như kết luận đã được xác minh tại cửa hàng thật.


**Document ID:** SM-SUR-PS-OBJ-001 · **Status:** SIMULATED / HYPOTHESES; NOT APPROVED PRODUCT REQUIREMENTS.

## Mục tiêu nghiên cứu vấn đề (không phải KPI cam kết)

| Objective ID | Mục tiêu định hướng | Vấn đề liên quan | Cách xác minh trong khảo sát thật |
|---|---|---|---|
| O-01 | Hiểu cách cửa hàng ghi số thực nhận và kiểm soát biến động tồn | PS-01 | Quan sát nhận hàng/kiểm kê, so chứng từ được phép xem |
| O-02 | Hiểu các thao tác tìm sản phẩm và tra cứu hóa đơn | PS-02 | Quan sát thao tác quầy; hỏi trường hợp mã quét thất bại/in lại |
| O-03 | Hiểu cách đối soát tiền và giao dịch cuối ca/ngày | PS-03, PS-05 | Quan sát đối soát nếu được cho phép; hỏi tiêu chí xác nhận chuyển khoản |
| O-04 | Xác định vai trò và quyền phê duyệt điều chỉnh | PS-04 | Hỏi ba vai; đối chiếu biểu mẫu/quy định cửa hàng |
| O-05 | Xác định giới hạn thiết bị, ngân sách, năng lực sử dụng | CON-01 | Ghi nhận thiết bị và sự thuận tiện của người dùng; hỏi chi phí |

## Chỉ số có thể đo sau này (cần thiết kế thang đo, chưa có baseline)
- Thời gian tìm sản phẩm khi mã không đọc được.
- Thời gian thực hiện đối soát cuối ngày.
- Số bước ghi nhận nhập hàng hoặc kiểm kê phải nhập lại thông tin.
- Khả năng truy vết lịch sử điều chỉnh tồn, đổi giá, hủy hóa đơn.
- Tỷ lệ kịch bản nghiệm thu đạt yêu cầu sau khi SRS chốt.

**Tất cả là gợi ý cách đo, không phải dữ liệu hay cam kết hiệu quả.** Không tự ghi ví dụ 'giảm 50%', 'hoàn tất trong 2 giây' vào Problem Statement.

## Trong phạm vi nghiên cứu
- Một cửa hàng độc lập.
- Quản lý sản phẩm, bán hàng, nhập hàng, kiểm kê/tồn kho, đối soát/báo cáo và trách nhiệm thao tác.
- Giao diện phù hợp điều kiện sử dụng thực tế sau khi khảo sát.

## Ngoài phạm vi
- Quản lý chuỗi đa chi nhánh, điều phối kho phân tán.
- AI dự báo tự động, phân tích dữ liệu toàn doanh nghiệp, logistics quy mô lớn: chưa có nguồn nhu cầu.
- Tự động xác thực ngân hàng: chưa có yêu cầu/phê duyệt và đánh giá tích hợp.

## Không đồng nghĩa với MVP
Từ problem → Need Candidate (Bước 4) → Product Vision và Product Backlog (Giai đoạn 2). Phần mềm có thể không triển khai mọi nhu cầu; phải phân ưu tiên, xác nhận tính khả thi và Acceptance Criteria.
