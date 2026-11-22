# Proto-personas & Business Roles — Smart Mart

> **SIMULATED — PROTO-PERSONAS ONLY.** Dựng từ lời nhân vật trong INT-001…003; không có nghiên cứu nhân khẩu học, tuổi, giới tính, trình độ thật. Tránh tự bịa lý lịch chi tiết.

**ID:** SM-PD-PERSONAS-001 · **Status:** DRAFT.

## PP-01 — Người chủ kiêm quản lý (P01)
- **Công việc:** Quản lý sản phẩm/giá, quyết định đặt hàng, đối chiếu doanh thu, duyệt ngoại lệ và đôi khi đứng quầy.
- **Mục tiêu:** Tồn được ghi đúng sau nhập/bán; tìm hóa đơn dễ; tổng kết cuối ngày đơn giản.
- **Khó khăn được kể trong mô phỏng:** Đối soát báo cáo quầy–tiền mặt–chuyển khoản–bảng tính; ghi chú chưa đủ cần tra lại giao dịch.
- **Ràng buộc được kể:** Một máy tại quầy, xem được qua trình duyệt điện thoại, tránh chi phí duy trì phức tạp.
- **Cần biết thêm:** Quy trình duyệt, ngân sách thực, loại thiết bị, việc kiêm nhiệm, tần suất xử lý ngoại lệ.
- **Nguồn:** [INT-001](../01-survey/interviews/records/INT-001-owner.md) OWN-01, 04–07, 09, 12–17.

## PP-02 — Thu ngân tại quầy (P02)
- **Công việc:** Nhận hàng khách mua, quét hoặc tìm sản phẩm, kiểm giá/tổng tiền, nhận thanh toán, hoàn thành và tìm lại giao dịch khi cần.
- **Mục tiêu:** Giữ luồng bán hàng không bị gián đoạn, xử lý mã không quét được, tìm/in lại hóa đơn.
- **Khó khăn trong mô phỏng:** Quét mã lỗi, cần hỏi chủ khi giá lệch; xác minh tiền chuyển khoản chưa rõ.
- **Ranh giới:** Không tự đổi giá; tình huống đã thanh toán cần chuyển người có quyền.
- **Cần biết thêm:** Dòng giao dịch khi mất kết nối, thiết bị quét/in, nguyên tắc xác nhận đã nhận tiền, số bước sử dụng thực tế.
- **Nguồn:** [INT-002](../01-survey/interviews/records/INT-002-cashier.md) CAS-01…15.

## PP-03 — Người phụ trách hàng và kiểm kê (P03)
- **Công việc:** Nhận chứng từ, kiểm mã/đơn vị/tình trạng, đếm thực nhận, ghi chênh, đối chiếu tồn, kiểm kê và báo chủ.
- **Mục tiêu:** Chỉ ghi số thực nhận đã xác nhận; nhìn rõ tồn ghi–tồn đếm–chênh; biết lịch sử điều chỉnh.
- **Khó khăn trong mô phỏng:** Ghi lặp ở nhiều nơi, dễ nhầm thùng/chai, chênh lệch chờ xử lý.
- **Ràng buộc:** Khu nhập nhỏ; cần biểu mẫu sử dụng thuận tiện trên thiết bị ở kho.
- **Cần biết thêm:** Quy đổi đơn vị, tần suất kiểm kê, chính sách phê duyệt, cập nhật tồn đồng thời.
- **Nguồn:** [INT-003](../01-survey/interviews/records/INT-003-warehouse.md) WH-01…17.

## Role/permission hypotheses — cần duyệt
| Hoạt động | P01 Owner | P02 Cashier | P03 Warehouse | Độ tin cậy |
|---|---|---|---|---|
| Tra cứu sản phẩm | Đề xuất Có | Có | Có | S0 |
| Bán hàng và giỏ | Có thể kiêm | Chính | Không mặc định | S0 |
| Đổi giá niêm yết | Duyệt | Không tự đổi | Không mặc định | S0 |
| Nhận hàng / ghi số thực | Giám sát | Không mặc định | Chính | S0 |
| Kiểm kê / đề xuất lệch | Duyệt | Không mặc định | Chính | S0 |
| Hủy hóa đơn đã thanh toán | Duyệt theo quy tắc | Không tự làm | Không mặc định | S0 |
| Xem đối soát cuối ngày | Chính | Phần theo ca (chưa rõ) | Không mặc định | S0 |

**Không diễn giải bảng trên thành authorization matrix đã phê duyệt.** Nguồn mô phỏng chỉ hỗ trợ nguyên tắc phân nhiệm ban đầu; permission chi tiết sang `03-requirements`.
