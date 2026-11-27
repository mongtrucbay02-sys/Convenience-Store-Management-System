# Hợp đồng DT 03 và DT 06 theo PR 291

Ngày 27/11/2026. Đối chiếu review Yến trên HEAD `ab05ee2fe5dcfa40b7095604aea5f91039376a2f`. Tài liệu mô tả giao diện thực tế của bản sửa; người phụ trách API/SRS cần review trước khi merge. Các route POS/DT đề xuất khác trong OpenAPI không tự được coi là đã hiện thực.

## DT 03 dùng JSON API và MVC

Prefix JSON là `/api/v1`. Thu ngân có quyền `DOI_TRA`; cookie phiên và global XSRF filter vẫn áp dụng, không thêm bộ lọc trùng.

| Method | Route sau prefix | Đầu vào và kết quả |
| --- | --- | --- |
| GET | `/returns/exchange/invoices/{invoiceId}` | Hóa đơn gốc và giỏ hiện tại |
| POST | `/returns/exchange/preview` | `ExchangeRequest` → `ExchangePreview`, có confirmationToken |
| POST | `/returns/exchange` | `ExchangeRequest` kèm token → 201 và Location tới receipt |
| GET | `/returns/exchange/{id}` | Receipt của chính thu ngân |

`ExchangeRequest`: `invoiceId` (int64), `lines[]` gồm `productId` (int64), `quantity`, `reason`, `condition`, `note` tùy chọn, `handling` = NHAP_LAI/HUY; `confirmationToken`, `cashReceived` (int64), `cashConfirmed` (boolean). Hàng mới lấy từ giỏ POS của phiên đã xác thực, không lấy từ danh sách do client tự gửi. Preview không thay cho xác nhận; Create kiểm tra lại trong giao tác.

`POST /returns/{id}/exchange` trước đây là đề xuất và không phải endpoint hiện thực. OpenAPI đã bỏ route đề xuất này và mô tả đúng bốn route trên. MVC DT-03 hiện có `/Returns/Exchange`, `/Returns/Exchange/Preview`, `/Returns/Exchange/Create`, `/Returns/Exchange/Receipts/{id}`.

## DT 06 dùng form MVC

Các route dưới đây nằm ngoài `/api/v1`, trả HTML hoặc redirect. Không có JSON `POST /api/v1/returns/{id}/approve`. OpenAPI ghi chúng ở extension `x-mvc-dt06-routes`, không giả lập thành JSON endpoint.

| Actor | Route | Method và form |
| --- | --- | --- |
| AD | `/Returns/Approvals` | GET danh sách chờ |
| AD | `/Returns/Approvals/{id}` | GET chi tiết và snapshot; POST `decision`, `note`, `refundConfirmed` |
| TN | `/Returns/Approvals/ApprovedExchanges` | GET phiếu đổi của mình đã duyệt |
| TN | `/Returns/Approvals/ApprovedExchanges/{id}` | GET snapshot/lịch sử; POST `cashReceived`, `cashConfirmed` để hoàn tất |
| TN | `/Returns/Approvals/ApprovedExchanges/{id}/Repreview` | POST xem phương án giỏ hiện tại, trả HTML có token |
| TN | `/Returns/Approvals/ApprovedExchanges/{id}/Resubmit` | POST `token`, chuyển lại CHO_DUYET |

Mọi POST MVC cần `__RequestVerificationToken` và cookie antiforgery. AD cần `DOI_TRA_DUYET_CAU_HINH`, TN cần `DOI_TRA`. Quyền còn được kiểm tra lại trong giao tác; khóa tài khoản, khóa tạm hoặc bắt buộc đổi mật khẩu chặn hoàn tất.

## Ai xác nhận chi tiền

Luồng hiện tại: Admin nhấn duyệt phiếu trả và trực tiếp đánh dấu xác nhận hoàn tiền. Vì vậy `giao_dich_tien_mat.nguoi_thuc_hien_id`, tác động kho và lịch sử thu hồi điểm ghi Admin đang thực hiện. `phieu_doi_tra.thu_ngan_id` vẫn là người lập; `nguoi_duyet_id` là người duyệt. Không giả danh thu ngân lập phiếu. Dịch vụ hoàn thường của TN vẫn giới hạn đúng thu ngân sở hữu phiếu; đường chi của Admin chỉ gọi nội bộ từ giao tác duyệt và kiểm tra Admin đó trùng người duyệt.

Nếu vận hành muốn TN trực tiếp chi sau khi AD duyệt, cần thiết kế bước xác nhận của TN riêng; bản này không tự mở luồng đó. Biên nhận của TN sở hữu phiếu hiển thị đúng người đã xác nhận chi, kể cả là Admin.

## Phạm vi duyệt cả phương án đổi

Theo lựa chọn người yêu cầu ngày 27/11: AD duyệt cả hàng trả lẫn danh sách hàng mới. Snapshot gồm ID/phiên bản giỏ, ID và tên sản phẩm, số lượng, giá, giảm giá, ID khuyến mãi, tổng, thu thêm và người/thời điểm đề xuất. Mỗi phiên bản giữ quyết định, người duyệt, thời điểm và ghi chú.

Hoàn tất phải khớp snapshot đã APPROVED. Thay hàng cùng giá, số lượng, giá, ưu đãi, khách hoặc giỏ đều không được lách kiểm tra chỉ bằng tổng tiền. Bản thay đổi được xem lại bằng Repreview, xác nhận bằng token rồi Resubmit; phiên bản mới chờ duyệt và quyết định cũ vẫn nằm trong lịch sử. Sửa hàng trả gốc cần lập phiếu khác theo quy tắc số lượng, không thay dòng trả bằng Resubmit.

Phiếu cũ thiếu snapshot không được suy diễn là đã duyệt phương án hiện tại: phải gửi lại và duyệt lại. Phiếu hoàn tất cũ vẫn tra cứu được và không được backfill lịch sử giả.

## Delta CSDL và gate nâng cấp

Migration `20261127030000_ExchangePlanSnapshots` thêm **một cột nullable jsonb** `smartmart.phieu_doi_tra.phuong_an_doi`, kèm entity, EF mapping và model snapshot. Không thêm bảng: vẫn 31 bảng/3 view; model mục tiêu có 308 cột so với baseline 307. Contract Supabase lịch sử giữ nguyên, test kiểm riêng delta này.

Không chỉnh migration baseline, không tự chạy DDL trên Supabase chung. Cần Yến/Thăng review tại #283/#293, backup và kế hoạch triển khai trước khi dùng runtime mới với Supabase. Smoke EF của bản có cột mới chỉ chạy sau khi môi trường được nâng cấp đúng migration. `Down` từ chối xóa lịch sử; rollback phải dùng phương án backup đã được duyệt.

Các quyết định này cần đồng bộ #257/SRS và được hai reviewer xác nhận. Có code hoặc test đạt không tự tương đương phê duyệt CSDL, staging hoặc nghiệm thu cuối.

## Giới hạn kiểm chứng quyền CSDL

Các regression mới mở kết nối bằng login riêng, không superuser, không BYPASSRLS, không phải database owner; login chỉ kế thừa `smartmart_backend` của migration baseline. Đây là kiểm thử quyền baseline trong PostgreSQL riêng, chưa xác nhận ACL đang chạy trên Supabase hoặc contract bảo mật đề xuất ở PR #300. Không tự áp thay đổi quyền dùng chung trong bản sửa này. Các gate phân quyền toàn hệ thống vẫn theo #283/#293 và review độc lập.
