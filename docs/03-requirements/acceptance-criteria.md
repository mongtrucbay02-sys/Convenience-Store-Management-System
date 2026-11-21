# Acceptance Criteria – Candidate Gherkin Cases

> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

Mỗi AC là **giả thuyết kiểm thử có thể chuyển thành test case**; chưa được Product Owner phê duyệt hay chạy. Những tình huống ví dụ giữ nguyên dữ liệu giả lập Giai đoạn 1.

| AC | US | Given–When–Then (tóm tắt) | FR |
|---|---|---|---|
| AC-01a | US-01 | Given sản phẩm có mã/tên hợp lệ; When thu ngân tìm bằng tên sau quét không nhận; Then hệ thống hiển thị kết quả đúng tập dữ liệu thử | FR-01 |
| AC-01b | US-01 | Given sản phẩm không có hoặc ngừng bán; When thu ngân chọn; Then hệ thống không tự thêm sản phẩm không hợp lệ vào giỏ | FR-01 |
| AC-02a | US-02 | Given giỏ 2 nước × 12.000 và 1 snack × 9.000; When tính tiền; Then tổng 33.000đ và tiền thừa của 50.000đ là 17.000đ | FR-02,03 |
| AC-02b | US-02 | Given thanh toán chưa hoàn tất; When thao tác bị hủy; Then không xuất hiện hóa đơn ở trạng thái hoàn tất | FR-03 |
| AC-03a | US-03 | Given tồn ban đầu đã biết và đủ; When đơn 2 nước + 1 snack được hoàn tất; Then sổ tồn ghi giảm 2 và 1 đúng một lần theo D-01 | FR-04,10 |
| AC-03b | US-03 | Given yêu cầu hoàn tất bán được retry; When gửi lại cùng giao dịch; Then không ghi giảm tồn hai lần | FR-04,10 |
| AC-03c | US-03 | Given giao dịch chưa hoàn tất hoặc update thất bại; When kiểm tồn chính thức; Then không phát sinh trạng thái bán hoàn tất mà không có biến động tồn hợp lệ | FR-04 |
| AC-04a | US-04 | Given hóa đơn đã lưu; When tra theo mã; Then xem lại được đúng hóa đơn và không tạo đơn mới | FR-05 |
| AC-05a | US-05 | Given hóa đơn đã hoàn tất và đủ quyền; When in lại; Then tạo bản sao chứng từ, không phát sinh giao dịch bán thứ hai | FR-06 |
| AC-06a | US-06 | Given phiếu giao 12 chai nhưng đếm 10; When lưu; Then giữ số giao 12, thực nhận 10, chênh −2 | FR-07 |
| AC-06b | US-06 | Given số thực nhận âm hoặc đơn vị không xác định; When nộp phiếu; Then không thể xác nhận hợp lệ, lỗi được báo rõ (đơn vị tính cần duyệt) | FR-07 |
| AC-07a | US-07 | Given phiếu có thực nhận 10 và được người có quyền xác nhận; When hoàn tất xác nhận; Then tăng tồn +10 đúng một lần, không phải +12 | FR-08,09,10 |
| AC-07b | US-07 | Given phiếu đang chờ duyệt; When đọc sổ tồn; Then không có biến động nhập đã xác nhận cho phiếu đó | FR-08,09 |
| AC-07c | US-07 | Given phiếu đã hoàn tất; When xác nhận lặp; Then không cộng thêm tồn lần nữa | FR-09,10 |
| AC-08a | US-08 | Given tồn ghi 18 và đếm hai lần 17; When lưu kiểm kê; Then chênh −1 ở pending, tồn chính thức chưa đổi | FR-11 |
| AC-09a | US-09 | Given đề xuất kiểm kê pending; When người đủ quyền duyệt/từ chối; Then lưu quyết định, người, thời điểm và chỉ thay tồn khi duyệt hợp lệ | FR-12,13 |
| AC-10a | US-10 | Given vai thu ngân không có quyền đổi giá; When yêu cầu đổi giá; Then hệ thống từ chối và lưu dấu vết theo chính sách đã được chấp thuận | FR-13 |
| AC-10b | US-10 | Given người có quyền xử lý ngoại lệ; When thực hiện; Then lưu tối thiểu người, hành động, đối tượng, thời điểm và kết quả | FR-13 |
| AC-11a | US-11 | Given các giao dịch đủ điều kiện; When xem báo cáo theo ngày; Then tổng hợp đúng tập giao dịch đã chốt, loại trừ trạng thái chưa thanh toán | FR-14 |
| AC-12a | US-12 | Given thiết bị mục tiêu được chọn; When thực hiện task nhập/kiểm kho; Then các trường trọng yếu xem và nhập được; tiêu chí kích thước/tốc độ TBD | FR-15 |

## Quy tắc nghiệm thu
- Tất cả AC trên đang **NOT RUN / NOT APPROVED**; không điền PASS khi chưa có run/evidence.
- Với M-10a phải gắn AC→test và trạng thái; với M-10b phân biệt PASS/FAIL. M-11 yêu cầu **tích hợp POS→KHO** thực chứng.
- Các chính sách permission, trả/hủy, thanh toán chuyển khoản và concurrent update cần được duyệt trước khi cố định kỳ vọng test.
