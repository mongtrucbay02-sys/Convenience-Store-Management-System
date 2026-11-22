# User Scenarios — Smart Mart

> **SIMULATED / DESIRED BEHAVIOR HYPOTHESES.** Các kịch bản sau dùng cùng nguồn Giai đoạn 1 nhưng mô tả hướng sử dụng sản phẩm **đề xuất**, không phải tính năng đã triển khai hoặc user stories đã duyệt.

**ID:** SM-PD-SCENARIOS-001 · **Status:** DRAFT.

## SC-SIM-01 — Thu ngân tìm hàng khi không quét được mã
**Actor:** PP-02. **Goal:** Chọn đúng sản phẩm, kiểm giá/tổng và hoàn thành bán hàng.  
**Bối cảnh As-is:** [AS-SIM-01](../01-survey/analysis/as-is-sales.md); quét mã không nhận rồi tìm tên (F-SIM-002).  
**Narrative đề xuất:** Khách mua hai chai nước và một gói snack. Một nhãn không quét được; thu ngân tìm theo tên, chọn đúng mặt hàng, kiểm số lượng và giá, sau đó tiếp tục thanh toán. Nếu sản phẩm không có trong danh mục hoặc bị ngừng bán, hệ thống cần báo rõ và không tự thêm hàng tùy ý.  
**Dữ liệu minh họa từ diễn tập:** 2×12.000 + 9.000 = 33.000đ; khách đưa 50.000đ, trả 17.000đ. Không có bằng chứng giao dịch thực tế.  
**Trace:** PS-02 → N-SIM-01 → FC-SIM-01, FC-SIM-04.  
**Quyết định liên quan:** D-01 ([scope.md](scope.md)) — giao dịch hoàn tất trừ tồn chính thức (đề xuất nội bộ).  
**Cần xác minh:** Cách xử lý hàng thiếu tồn và sản phẩm không xác định.

## SC-SIM-02 — Nhập hàng với chênh lệch giữa phiếu và thực nhận
**Actor:** PP-03 + PP-01. **Goal:** Ghi rõ chênh lệch và chỉ cập nhật hàng đã được chấp thuận.  
**As-is:** [AS-SIM-02](../01-survey/analysis/as-is-purchasing.md).  
**Narrative đề xuất:** Phiếu giao ghi 12 chai, nhân viên đếm được 10. Nhân viên nhập hai số tách biệt, ghi thiếu 2 và gửi để chủ xem xét. Chỉ sau thao tác xác nhận thích hợp mới tạo biến động tồn dựa trên số được chấp nhận. Nếu chờ duyệt, số ghi tăng tồn không bị tính là 12.  
**Trace:** PS-01 → N-SIM-02,03,06 → FC-SIM-02,03,06.  
**Cần xác minh:** Ai được duyệt phiếu thường, hàng lỗi và đơn vị thùng/chai.

## SC-SIM-03 — Kiểm kê phát hiện lệch, chưa được phép sửa ngay
**Actor:** PP-03 + PP-01. **Goal:** Lưu kết quả kiểm đếm, xem xét nguyên nhân và quyết định điều chỉnh.  
**As-is:** [AS-SIM-03](../01-survey/analysis/as-is-inventory.md).  
**Narrative đề xuất:** Bảng ghi 18, người kiểm đếm được 17 và kiểm lại vẫn 17. Người kiểm kê ghi chênh lệch −1 và chứng cứ/lý do nếu có. Chủ xem chứng từ liên quan; trước khi duyệt, số tồn chính thức vẫn giữ trạng thái chưa điều chỉnh.  
**Trace:** PS-01,04 → N-SIM-04,06 → FC-SIM-05,06.  
**Cần xác minh:** Quy tắc duyệt, số tồn snapshot và kiểm kê đồng thời.

## SC-SIM-04 — Chủ cửa hàng đối soát cuối ngày
**Actor:** PP-01. **Goal:** Kiểm tra tổng bán và các loại thanh toán; tìm giao dịch cần đối chiếu.  
**As-is:** [AS-SIM-04](../01-survey/analysis/as-is-daily-close.md), chỉ từ phỏng vấn.  
**Narrative đề xuất:** Chủ chọn ngày hoặc ca, xem doanh số tổng hợp và danh sách giao dịch, đối chiếu tiền mặt/chuyển khoản theo các trạng thái đã được xác minh. Khi phát sinh khoản khác biệt, chủ tra mã giao dịch và ghi chú để tìm hiểu; không tự xác nhận đã nhận tiền chỉ từ ảnh khách cung cấp.  
**Trace:** PS-03,05 → N-SIM-07,05; FC-SIM-07,04; PS-05 còn OPEN.  
**Cần xác minh:** Chính sách chốt ca, xử lý hoàn tiền và cách xác nhận tiền chuyển khoản; chưa đề xuất API ngân hàng.

## SC-SIM-05 — Tìm và in lại hóa đơn có kiểm soát
**Actor:** PP-02, có thể cần PP-01 tùy quyền. **Goal:** Phục hồi chứng từ khi máy in gặp lỗi.  
**Nguồn:** [INT-002](../01-survey/interviews/records/INT-002-cashier.md) CAS-05, CAS-14 (interview-only).  
**Narrative đề xuất:** Máy in hết giấy sau giao dịch. Thu ngân tra cứu bằng mã hoặc thời gian và in lại bản sao có thông tin giao dịch; hệ thống không tạo giao dịch bán hàng thứ hai.  
**Trace:** PS-02,04 → N-SIM-05,06 → FC-SIM-04,06.  
**Cần xác minh:** Quyền in lại, đánh dấu bản sao, chính sách hóa đơn và lưu trữ.

## Chưa kết luận
- Scenarios chưa thay User Stories / Acceptance Criteria.
- Các hành vi 'đề xuất' chưa phải xác nhận của người dùng thật.
- Phân quyền và đồng bộ tồn phải được xác minh trước khi viết SRS và test acceptance chính thức.
