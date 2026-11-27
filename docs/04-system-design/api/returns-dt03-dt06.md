# Hợp đồng DT 03 và DT 06 theo schema hiện có

Đối chiếu review Yến PR #291 (ab05ee2), issue #238/#239/#293 và CONTRIBUTING 4.1. Bản sửa không thay schema/entity/mapping/migration so với develop cc20336566ff. Bản thử nghiệm 17488f9 có cột JSONB chưa được duyệt, không đại diện cho bản hiện tại.

## JSON API DT 03

Prefix /api/v1; TN có quyền DOI_TRA, cookie và global XSRF filter.

| Method | Route sau prefix | Kết quả |
| --- | --- | --- |
| GET | /returns/exchange/invoices/{invoiceId} | Hóa đơn gốc và giỏ hiện tại |
| POST | /returns/exchange/preview | Giá trị trả, giỏ, chênh lệch và confirmationToken |
| POST | /returns/exchange | 201 đổi thường; 409 nếu cần duyệt ngoại lệ |
| GET | /returns/exchange/{id} | Receipt của chính TN, gồm chứng từ lịch sử |

ExchangeRequest gồm invoiceId int64, lines (productId int64, quantity, reason, condition/note tùy chọn, handling NHAP_LAI/HUY), confirmationToken, cashReceived int64 và cashConfirmed boolean. Hàng mới lấy từ giỏ phiên xác thực, kiểm lại trong transaction. Đổi thường ghi hóa đơn/kho/quỹ/điểm nguyên tử, chênh lệch không âm. NeedsApproval trong preview không cấp quyền Create. Hai route JSON /returns/{id}/exchange và /returns/{id}/approve cũ không phải implementation; không thêm CSRF filter trùng.

## Form MVC DT 06

Các route ngoài /api/v1, trả HTML/redirect; AD cần DOI_TRA_DUYET_CAU_HINH, TN cần DOI_TRA. Mọi POST có antiforgery token.

| Actor | Method và route | Hành vi |
| --- | --- | --- |
| AD | GET /Returns/Approvals | Danh sách chờ |
| AD | GET /Returns/Approvals/{id} | Chi tiết, thông báo hold nếu DOI_HANG |
| AD | POST /Returns/Approvals/{id} | decision, note, refundConfirmed; duyệt TRA_HANG hoặc reject |
| TN | GET /Returns/Approvals/ApprovedExchanges | Phiếu cũ đã duyệt của mình |
| TN | GET /Returns/Approvals/ApprovedExchanges/{id} | Trạng thái và thông báo hold |
| TN | POST /Returns/Approvals/ApprovedExchanges/{id} | Kiểm quyền/trạng thái rồi chặn hoàn tất, không ghi dữ liệu |

Kiểm lại tài khoản trong transaction: vai trò, hoạt động, khóa tạm và bắt buộc đổi mật khẩu. Không có Repreview/Resubmit trong bản này.

## Actor hoàn tiền

Admin trực tiếp duyệt và xác nhận chi TRA_HANG: quỹ/kho/điểm ghi đúng Admin thực hiện. thu_ngan_id vẫn là người lập, nguoi_duyet_id là người duyệt. TN sở hữu phiếu đọc receipt có đúng tên người xác nhận chi. Refund thường vẫn giới hạn chủ phiếu; RefundApproved chỉ gọi nội bộ và kiểm actor trùng người duyệt. Không nhận actor tùy ý từ client.

## Hold đổi ngoại lệ

Trung đã chọn Admin duyệt cả hàng mới. Schema hiện có chưa lưu snapshot được duyệt. Không nhét JSON/hash vào ghi_chu_duyet hoặc bảng lịch sử khác, không dùng bộ nhớ/file để lách quy trình CSDL.

Theo phương án dự phòng #239: Create DOI_HANG cần duyệt trả 409 DT06_CHO_THIET_KE_PHUONG_AN trước ghi/thu tiền; AD không approve phiếu đổi cũ, TN không complete kể cả DA_DUYET. Không tiêu thụ giỏ POS chung. AD vẫn reject phiếu cũ để giải phóng số lượng chờ. Phiếu hoàn tất lịch sử vẫn đọc/in. TRA_HANG vẫn duyệt/hoàn tiền bình thường.

Đây là HOLD, không phải đã hoàn thành duyệt toàn bộ phương án; không đóng #238/#239 hoặc tự chốt cắt use case. Yến/Thăng cần review thiết kế lưu trữ, API/SRS và regression reapproval trước khi bật lại. Nhánh proposal/dt06-snapshot-schema-20261127 chỉ là đề xuất, chưa approval; runtime hiện tại không phụ thuộc nhánh đó.

## Giới hạn evidence

Test login hạn chế trên PostgreSQL riêng dùng grant baseline smartmart_backend, không owner. Chưa chứng minh ACL thật Supabase hoặc exact current_user/session_user smartmart_backend theo đề xuất PR #300. CI HEAD mới, reviewer độc lập, UAT và K80/MH-20 cần bằng chứng riêng. Không tự DDL/test ghi Supabase; không sửa guard.
