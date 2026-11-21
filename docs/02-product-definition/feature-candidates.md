# Feature Candidates & Prioritization — Smart Mart

> **SIMULATED / PRIORITIES ARE INTERNAL PROPOSALS, NOT CUSTOMER-APPROVED.**

**ID:** SM-PD-FEATURE-001 · **Status:** DRAFT.

## 1. Danh sách có nguồn khảo sát mô phỏng
| Feature ID | Feature candidate | Need | Problem | Scenario | MoSCoW đề xuất | Ghi chú |
|---|---|---|---|---|---|---|
| FC-SIM-01 | Tìm sản phẩm theo mã và tên | N-SIM-01 | PS-02 | SC-SIM-01 | Must (proposed) | Chưa xác minh tiêu chí tìm gần đúng |
| FC-SIM-02 | Lập phiếu nhận: số giao, số thực, chênh | N-SIM-02 | PS-01 | SC-SIM-02 | Must (proposed) | 12/10 là ví dụ diễn tập |
| FC-SIM-03 | Chỉ ghi biến động tồn sau xác nhận phù hợp | N-SIM-03 | PS-01 | SC-SIM-02 | Must (proposed) | Chốt transaction/boundary ở 03–04 |
| FC-SIM-04 | Giao dịch bán & tra cứu/in lại hóa đơn | N-SIM-01,05 | PS-02,04 | SC-SIM-01,05 | Must/Should (proposed) | Bán: Must; in lại: Should |
| FC-SIM-05 | Kiểm kê: tồn ghi/đếm, chênh, trạng thái duyệt | N-SIM-04 | PS-01,04 | SC-SIM-03 | Should (proposed) | Có thể chia increment |
| FC-SIM-06 | Phân quyền cơ bản, dấu vết thao tác nhạy cảm | N-SIM-06 | PS-04 | SC-SIM-02,03,05 | Must (proposed) | Chưa chốt chi tiết role matrix |
| FC-SIM-07 | Báo cáo bán và hỗ trợ đối soát ngày | N-SIM-07 | PS-03 | SC-SIM-04 | Should (proposed) | Trạng thái thanh toán cần làm rõ |
| FC-SIM-08 | Bố cục phù hợp máy quầy và trình duyệt thiết bị kho | N-SIM-08 | CON-01 | SC-SIM-02,03 | Should (proposed) | Chưa có loại thiết bị thực tế |

**Tất cả tám `N-SIM` đã được liên kết.** Mức ưu tiên không đồng nghĩa 'đã chốt MVP'.

## 2. Những chức năng hiện có trong dự án, không được tự gắn vào khảo sát
| Module | Nguồn hiện tại | Quyết định ở Giai đoạn 2 |
|---|---|---|
| SP (Products) | Dự án có module; N-SIM-01 hỗ trợ chức năng tra cứu | In scope candidate |
| NH (Purchasing) | N-SIM-02,03 | In scope candidate |
| KHO (Inventory) | N-SIM-03,04,08 | In scope candidate |
| POS (Sales) | N-SIM-01,05 | In scope candidate |
| AUTH | N-SIM-06 | In scope candidate |
| BC (Reporting) | N-SIM-07 | In scope candidate, priority pending |
| KH (Customers) | EXISTING PROJECT; không có N-SIM trực tiếp | Separate validation; không mặc định MVP |
| KM (Promotions) | EXISTING PROJECT; không có N-SIM trực tiếp | Separate validation; không mặc định MVP |
| DT (Returns) | EXISTING PROJECT; kịch bản P02 nhắc hủy/hoàn; chưa đặc tả đầy đủ | Separate validation; không mặc định MVP |

## 3. Backlog candidate cho Giai đoạn 3
- Viết `US-*` cho những FC được review/chọn vào Increment 1.
- Gắn mỗi US vào `PS/N-SIM` hoặc nguồn requirement độc lập (Issue/decision) đã xác minh.
- Đặc tả acceptance criteria: thành công, thất bại, dữ liệu hợp lệ, quyền, transaction/rollback, audit và test evidence.
- Không tạo yêu cầu kết nối ngân hàng từ PS-05 cho đến khi khảo sát làm rõ.

## 4. Câu hỏi cần quyết định
1. Chủ có chấp thuận thứ tự Must/Should này không?
2. Nhập hàng và bán hàng có nhất thiết phải cập nhật cùng sổ tồn chính thức trong MVP đầu tiên không?
3. Quy tắc duyệt chênh lệch áp dụng cho mọi phiếu hay chỉ phiếu ngoại lệ?
4. Phạm vi tra cứu/in lại hóa đơn và khôi phục máy in là gì?
5. Khách hàng, khuyến mãi, đổi trả có thuộc MVP theo yêu cầu độc lập nào?
