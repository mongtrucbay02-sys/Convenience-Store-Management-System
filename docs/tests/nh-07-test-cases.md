# NH-07 — Test Case Specification

Nguồn: Issue #95 (Use Case NH-07), `PhieuNhapConfiguration` từ PR #168. Phạm vi nhánh này chỉ gồm service/repository đọc, chưa có HTTP hoặc thay đổi schema.

| ID | Yêu cầu | Level | Tiền điều kiện / Input | Bước và kết quả mong đợi | Actual / Status | Evidence |
|---|---|---|---|---|---|---|
| NH07-TC-01 | Tìm mã, NCC, khoảng ngày, trạng thái | Unit | Code có khoảng trắng; SupplierId=7; 01–31/10/2026; DA_NHAP_KHO | Gọi service; repository nhận code/status đã trim và đúng ID; danh sách rỗng được giữ | Đúng; PASS | `ReceiptListServiceTests.Filters_AreNormalizedAndEmptyResultIsPreserved` |
| NH07-TC-02 | Trạng thái từ schema | Unit | INVALID | Gọi service; ném lỗi và không truy vấn | Đúng; PASS | `UnsupportedStatus_DoesNotQuery(INVALID)` |
| NH07-TC-03 | Bộ lọc trống | Unit | Status rỗng | Gọi service; status chuyển null | Đúng; PASS | `UnsupportedStatus_DoesNotQuery("")` |
| NH07-TC-04 | Khoảng ngày và NCC sai | Unit | From > To; SupplierId=0 | Từ chối cả hai, không truy vấn | Đúng; PASS | `ReversedDatesAndZeroSupplier_DoNotQuery` |
| NH07-TC-05 | Truy vấn dữ liệu thực, FK và thứ tự | Repository integration | Database MySQL NH-07 riêng, account được phê duyệt | Tạo NCC, tài khoản và phiếu trong transaction; lọc từng điều kiện, đối chiếu tên/giá trị, rollback | Chưa chạy; BLOCKED | Chưa có target NH-07 riêng được duyệt |
| NH07-TC-06 | AD/NK truy cập, TN bị cấm, chưa đăng nhập | HTTP | AUTH tích hợp vào base | Gửi request thật; kiểm tra 200/401/403 và body | Chưa chạy; BLOCKED | Chưa có endpoint được bảo vệ |

Không đặt giới hạn phân trang tùy ý khi Issue #95 chưa quy định. Cần xác nhận hợp đồng API và kiểm thử repository MySQL trước acceptance.
