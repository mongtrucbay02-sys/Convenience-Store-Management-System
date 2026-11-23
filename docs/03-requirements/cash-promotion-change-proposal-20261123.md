# Đề xuất cập nhật SRS: hủy hóa đơn, quỹ và ảnh khuyến mãi

Trạng thái: **Đang review / chưa được nhóm duyệt**, 23/11/2026. Bổ sung cho bản đối chiếu `migration-srs-review-2026-11-23.md`; không thay SRS gốc khi chưa có quyết định của nhóm.

| Mã đề xuất | Yêu cầu và tiêu chí chấp nhận |
|---|---|
| CP-01 | Khi Admin hủy hóa đơn đã thanh toán tiền mặt, ghi khoản chi bằng tiền khách đưa trừ tiền thừa. Việc hoàn kho, thu hồi điểm, ghi lịch sử hoàn và chi quỹ phải cùng transaction. Quỹ thiếu hoặc bất kỳ bước ghi nào lỗi thì không thay đổi các phần còn lại. Hóa đơn 0 tiền không sinh giao dịch quỹ 0 tiền. |
| CP-02 | Một hóa đơn chỉ được hủy/hoàn một lần. Hai yêu cầu đồng thời chỉ có một thành công; yêu cầu còn lại báo xung đột. Lịch sử chi không được sửa/xóa. |
| CP-03 | Admin được chốt số dư mở quỹ một lần bằng số tiền thực đếm tại thời điểm chốt, có lý do. TN/NK không được phép. Lưu trước/sau/chênh lệch/người làm/thời điểm; không cộng lại doanh thu lịch sử. Chặn dữ liệu âm, lý do rỗng, quỹ đã chốt hoặc số dư đã đổi từ lúc tải trang. |
| CP-04 | Admin thêm/sửa/xóa liên kết ảnh khuyến mãi. URL HTTP/HTTPS tối đa 2048 ký tự; lưu xuống MySQL và giữ sau khi tải lại hoặc khởi động lại. Chương trình cũ chưa có URL vẫn hiển thị được với ảnh thay thế. |

Phạm vi vẫn là một quỹ cửa hàng; chưa bổ sung quản lý ca, thu chi tùy ý, thanh toán online hay suy diễn số dư từ hóa đơn trước khi triển khai sổ quỹ.

Thiết kế, migration và hướng dẫn nâng cấp: [cash-promotion-20261123](../04-system-design/database/cash-promotion-20261123.md). Cần #1 duyệt CSDL theo #234, review nghiệp vụ POS/DT với #3, và test độc lập/staging theo #243/#244/#242. Đợt sửa không tự hoàn thành các issue tuần 8 KM-05/KM-06/BC-09–11/#251/#252.
