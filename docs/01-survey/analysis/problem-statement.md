# Problem Statement — Smart Mart (Bước 5)

> **SIMULATED / DỮ LIỆU GIẢ LẬP — KHÔNG PHẢI KHẢO SÁT THỰC TẾ.** Các cuộc phỏng vấn, quan sát, findings và vấn đề đều là kịch bản AI dựng. Tài liệu này chỉ phục vụ luyện tập. Không trình bày như kết luận đã được xác minh tại cửa hàng thật.


**Document ID:** SM-SUR-PS-001 · **Version:** 0.1 (Draft)  
**Project:** Smart Mart — Hệ thống quản lý cửa hàng tiện lợi nhỏ lẻ  
**Status:** SIMULATED SOURCE / NOT FIELD VALIDATED / NOT APPROVED  
**Scope:** Một cửa hàng tiện lợi độc lập; Store Owner, Cashier, Warehouse Staff.

## 1. Bối cảnh vấn đề (Problem Context)

Theo *tình huống phỏng vấn và quan sát mô phỏng* của Bước 3–4, cửa hàng thực hiện các nghiệp vụ bán hàng tại quầy, nhập hàng, kiểm kê tồn kho và đối soát cuối ngày với sự tham gia của ba vai trò. Một số hoạt động cần tra cứu, đối chiếu và xác nhận qua nhiều bước. Những công cụ được nhắc đến trong kịch bản gồm ứng dụng bán hàng tại quầy, chứng từ nhận hàng và bảng tính; hiện chưa kiểm tra được công cụ thực tế, cấu hình hay mức độ phổ biến ngoài đời. [Nguồn:](source-traceability-reference.md) [survey-findings](survey-findings.md), [as-is-sales](as-is-sales.md), [as-is-purchasing](as-is-purchasing.md), [as-is-inventory](as-is-inventory.md), [as-is-daily-close](as-is-daily-close.md).

## 2. Các vấn đề trọng tâm được gợi ra bởi mô phỏng

**PS-01 — Tính nhất quán thông tin hàng hóa.** Khi số lượng trên phiếu giao khác số thực nhận (kịch bản 12 và 10 chai), quá trình ghi nhận cần phản ánh đúng số hàng được chấp nhận. Khi kiểm kê có chênh lệch (tồn ghi 18, đếm 17), nhân viên phải đối chiếu trước khi điều chỉnh. Chênh lệch không chứng minh tồn đã cập nhật sai hoặc lý do gây lệch. Nguồn: F-SIM-004/005; P-SIM-03/05.

**PS-02 — Tra cứu, xử lý giao dịch bán hàng.** Kịch bản thu ngân quét mã không thành công và tìm sản phẩm theo tên; lời vai diễn cũng đề cập việc tìm và in lại hóa đơn khi máy in có trục trặc. Không có dữ liệu đo tốc độ thao tác, tỷ lệ quét thất bại hoặc quan sát việc in lại. Nguồn: F-SIM-002/007; P-SIM-02/07.

**PS-03 — Đối soát và truy vết doanh thu.** Lời vai chủ cửa hàng mô tả đối chiếu báo cáo quầy, tiền mặt, chuyển khoản và ghi tổng kết vào bảng tính. Việc đối soát có thể liên quan nhiều bước, nhưng chưa có số liệu thời gian, sai lệch hoặc thất thoát được xác minh. Nguồn: F-SIM-001; P-SIM-01.

**PS-04 — Ranh giới trách nhiệm và quyền xử lý ngoại lệ.** Trong mô phỏng, chủ cửa hàng muốn kiểm soát thay đổi giá, hủy hóa đơn đã thanh toán và duyệt điều chỉnh tồn; thu ngân không tự sửa giá. Chưa kiểm chứng cơ chế phân quyền hiện tại hoặc lịch sử thao tác trên hệ thống ngoài đời. Nguồn: F-SIM-006; P-SIM-06.

**PS-05 — Kiểm tra đã nhận tiền chuyển khoản.** Lời thu ngân cho thấy cần xác minh tiền thực sự đến tài khoản thay vì tin ảnh chuyển khoản. Không quan sát trực tiếp tình huống này; đây là một chủ đề xác minh riêng, **không mặc định giải pháp kết nối ngân hàng/API**. Nguồn: F-SIM-003; P-SIM-08.

## 3. Phát biểu bài toán tổng hợp (Consolidated Problem Statement)

> Trong kịch bản vận hành một cửa hàng tiện lợi độc lập, các nghiệp vụ bán hàng, nhập hàng, kiểm kê và đối soát doanh thu cần phối hợp thông tin và trách nhiệm giữa chủ cửa hàng, thu ngân và người phụ trách kho. Các tình huống giả lập về tìm sản phẩm khi quét mã không thành công, chênh lệch giữa số giao và số thực nhận, khác biệt giữa tồn ghi và tồn đếm, và việc đối chiếu giao dịch cho thấy những điểm cần được nghiên cứu về khả năng tra cứu, tính nhất quán dữ liệu và kiểm soát thao tác. Nhóm cần xác minh những vấn đề này tại cửa hàng thật trước khi chốt mức độ ưu tiên, yêu cầu và phạm vi MVP. Trên cơ sở đó, Smart Mart được định hướng là một giải pháp phần mềm đơn giản, phù hợp quy mô và nguồn lực vận hành của **một cửa hàng tiện lợi nhỏ lẻ**, không nhằm giải quyết các nhu cầu đặc thù của chuỗi bán lẻ lớn.

## 4. Người chịu ảnh hưởng (Affected Stakeholders)
- **Store Owner:** quan tâm đối soát, kiểm soát quyền thay đổi, khả năng truy vết và tính phù hợp chi phí (INT-001; F-SIM-001/006/009).
- **Cashier:** quan tâm tra cứu sản phẩm, hoàn tất giao dịch, xác nhận chuyển khoản và hóa đơn (INT-002; F-SIM-002/003/007).
- **Warehouse Staff:** quan tâm thực nhận, kiểm kê, đơn vị tính, chênh lệch và phê duyệt (INT-003; F-SIM-004/005/008).
- Một người có thể kiêm nhiệm nhiều vai trò. **Chưa có dữ liệu thực tế về số người sử dụng.**

## 5. Phạm vi nghiên cứu (Boundary)

**Trong phạm vi khảo sát:** quản lý sản phẩm, bán hàng tại quầy, nhập hàng, tồn kho, quyền thao tác cơ bản, báo cáo/đối soát.  
**Ngoài phạm vi định hướng:** quản lý nhiều chi nhánh, logistics phân tán, chuỗi siêu thị quy mô lớn, thương mại điện tử toàn diện, kết nối ngân hàng tự động nếu chưa có nhu cầu và điều kiện hợp lệ.

Danh sách trên là **ranh giới nghiên cứu ban đầu**, không phải cam kết rằng mọi tính năng đều được đưa vào MVP.

## 6. Không thể kết luận từ hồ sơ hiện có
- Không có bằng chứng thực tế về thất thoát tiền/hàng, lỗi hệ thống đang xảy ra hay tỷ lệ sai lệch.
- Chưa biết nguyên nhân chênh lệch kiểm kê -1; không thể khẳng định do thu ngân hay kho.
- Không chứng minh phiếu giao 12 đã bị cộng tồn 12: kịch bản ghi nhận **10 chai thực nhận** theo quyết định chủ cửa hàng.
- Phép tính ví dụ bán hàng **33.000đ**, đưa **50.000đ**, thối **17.000đ** là đúng trong kịch bản, không phải lỗi.
- Không có số liệu khảo sát về thời gian, doanh thu, số giao dịch/ngày, số nhân viên, ngân sách.
- Chưa có phép xác minh thực địa, chữ ký đồng ý, hay duyệt bản Problem Statement của cửa hàng thật.

## 7. Kết luận và trạng thái phê duyệt
**Draft / simulation exercise complete; real-world approval blocked.** Chỉ được dùng làm nền cho bài tập Product Vision và User Stories *giả định*. Để dùng trong báo cáo khảo sát thực, phải thu thập và thẩm định chứng cứ người dùng thật theo [Problem Statement Review](../validation/problem-statement-review.md).
