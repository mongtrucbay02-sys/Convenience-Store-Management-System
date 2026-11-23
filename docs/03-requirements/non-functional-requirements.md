# Non-functional Requirements – Candidate Register

> **SIMULATED / INTERNAL DRAFT / NOT CUSTOMER-APPROVED.** Nguồn khảo sát Giai đoạn 1 là mô phỏng (S0); ưu tiên/MVP Giai đoạn 2 chưa được phê duyệt. Các yêu cầu dưới đây là **candidate specifications**, không phải xác nhận hệ thống đã triển khai hay đã PASS.

Không có baseline thực tế, mẫu thiết bị, số giao dịch/ngày, số người dùng đồng thời hoặc giá trị SLA được xác nhận. Trường `TBD` là điểm cần đo/chốt, không phải thiếu sót bị giấu.

| NFR | Thuộc tính | Yêu cầu/đề xuất có thể kiểm tra | Phương pháp | Target | Nguồn |
|---|---|---|---|---|---|
| NFR-01 | Usability | Các vai trò thực hiện luồng bán/nhận hàng trên thiết bị mục tiêu | Task walkthrough | TBD sau chọn thiết bị | CON-01 / M-09 |
| NFR-02 | Performance | Đo thời gian tìm sản phẩm, lưu bán, xác nhận phiếu nhận | Timed test cùng dữ liệu & thiết bị | TBD có baseline | M-01/02 |
| NFR-03 | Security | Tài khoản thiếu quyền bị từ chối đổi giá/duyệt tồn | Positive & negative authorization tests | Pass policy đã chốt | N-SIM-06 / M-06 |
| NFR-04 | Integrity | Retry/lỗi giữa chừng không tạo ghi tồn trùng hoặc trạng thái bán không nhất quán | Integration + fault-injection tests | Pass cases đã duyệt | D-01 / M-04/11 |
| NFR-05 | Auditability | Truy hồi ai thực hiện thao tác nhạy cảm, đối tượng, thời điểm, kết quả | Audit inspection test | TBD retention | N-SIM-06 |
| NFR-06 | Availability | Xác định nhu cầu online/offline và mất mạng | Stakeholder interview + recovery scenario | TBD | U-02-03 |
| NFR-07 | Privacy | Tránh đưa thông tin khách thật, mật khẩu, token vào repo công khai | Repo/static review | Không lộ dữ liệu thực | Survey validation |
| NFR-08 | Compatibility | Xác minh trình duyệt và máy quét/in của cửa hàng | Device matrix | TBD hardware | CON-01 |
| NFR-09 | Maintainability | Module và kiểm thử dễ bảo trì với quy mô đồ án | Code review, tests, build | TBD / project policy | Team decision |
| NFR-10 | Localization | Quy ước VND, thời gian cửa hàng và đơn vị tính chính xác | Format test & stakeholder confirmation | Locale/TZ TBD | Product scope |

**Không kế thừa tự động** các giá trị SRS Chương 6 được nhắc trong README (bao gồm UTC+7) vì chưa có nội dung SRS gốc để đối chiếu; cần nhập & kiểm chứng trước phê duyệt.
