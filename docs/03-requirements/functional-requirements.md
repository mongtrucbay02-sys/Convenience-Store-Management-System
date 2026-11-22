# Functional Requirements – Candidate Catalogue

> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

**FR là mã ứng viên mới**, không thay mã chức năng cũ của hệ thống. `M/S` là Must/Should *proposed*, không có approval.

| FR | Hệ thống cần hỗ trợ | Actor | FC | Need | Slice | MoSCoW | Điều kiện / chưa rõ |
|---|---|---|---|---|---|---|---|
| FR-01 | Tra cứu sản phẩm bằng mã/tên | P02 | FC-SIM-01 | N-SIM-01 | I0 | M | Sản phẩm không có/ngừng bán: không chọn tùy ý |
| FR-02 | Tạo giỏ hàng và tính tổng | P02 | FC-SIM-04 | N-SIM-01 | I1 | M | Không chốt giá khác quyền hoặc tính sai tiền |
| FR-03 | Ghi nhận bán tiền mặt và lưu giao dịch hoàn tất | P02 | FC-SIM-04 | N-SIM-01 | I1 | M | Chưa hoàn tất không tạo giao dịch bán hoàn tất |
| FR-04 | Hoàn tất bán trừ đúng tồn chính thức | P02 | FC-SIM-03 | N-SIM-03 | I1 | M | D-01 pending; không trừ trùng khi retry |
| FR-05 | Tra cứu hóa đơn đã lưu | P02/P01 | FC-SIM-04 | N-SIM-05 | I1 | M | Không phát sinh giao dịch bán mới khi truy vấn |
| FR-06 | In lại bản sao hóa đơn | P02/P01 | FC-SIM-04 | N-SIM-05 | I3/Review | S | Chính sách hóa đơn/in lại chưa xác minh |
| FR-07 | Lập phiếu nhận ghi số trên phiếu, thực nhận, chênh | P03 | FC-SIM-02 | N-SIM-02 | I2 | M | Đơn vị tính, hàng hỏng và phê duyệt ngoại lệ TBD |
| FR-08 | Xác nhận phiếu nhận theo quyền | P01/P03 | FC-SIM-03 | N-SIM-03 | I2 | M | Người duyệt và trạng thái phiếu TBD |
| FR-09 | Phiếu nhận đã xác nhận chỉ cộng tồn số thực nhận được chấp nhận | P03 | FC-SIM-03 | N-SIM-03 | I2 | M | D-01 pending; một lần ghi biến động |
| FR-10 | Tách sổ biến động tồn và cho phép tra cứu | P01/P03 | FC-SIM-03 | N-SIM-03 | I2 | M | Định nghĩa một sổ tồn chính thức, quy tắc trùng ghi |
| FR-11 | Ghi nhận kiểm kê và chênh lệch ở trạng thái chờ duyệt | P03 | FC-SIM-05 | N-SIM-04 | I3 | S | Không sửa tồn khi pending |
| FR-12 | Duyệt hoặc từ chối điều chỉnh tồn có dấu vết | P01 | FC-SIM-05 | N-SIM-04 | I3 | S | Chưa chốt snapshot/đồng thời |
| FR-13 | Phân quyền cơ bản và ghi audit thao tác nhạy cảm | P01/P02/P03 | FC-SIM-06 | N-SIM-06 | I0–I2 | M | Role matrix chi tiết còn pending |
| FR-14 | Báo cáo và lọc giao dịch để đối soát ngày | P01 | FC-SIM-07 | N-SIM-07 | I3 | S | Chưa chốt logic chuyển khoản/hoàn |
| FR-15 | Giao diện phục vụ thiết bị quầy/kho mục tiêu | P02/P03 | FC-SIM-08 | N-SIM-08 | I3/Review | S | Chưa xác định thiết bị thực tế |

## Ranh giới không tự thêm
- Không thiết lập API ngân hàng hay xác thực chuyển khoản tự động từ `PS-05`.
- KH/KM/DT và các mã chức năng legacy phải truy về nguồn độc lập, không mặc nhiên nhận `N-SIM`.
- Mọi FR với D-01/approval chưa duyệt là **đề xuất kiểm chứng**, chưa phải cam kết.
