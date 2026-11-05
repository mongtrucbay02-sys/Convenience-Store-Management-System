# Hợp đồng MVC POS-08

Các action kế thừa policy BAN_HANG của PosController; service chỉ thao tác cho tài khoản TN đang hoạt động và không bị bắt buộc đổi mật khẩu. Không nhận cashierId từ client.

| Phương thức | Đường dẫn | Đầu vào | Kết quả |
|---|---|---|---|
| GET | /Pos | search, categoryId (đã có) | Trang bán hàng và danh sách đơn tạm giữ của mình; tự hết hạn đơn ngày trước |
| POST | /Pos/Hold | note (tùy chọn, <=100 ký tự), antiforgery token | 302 về /Pos, thông báo thành công/lỗi |
| POST | /Pos/Recall | orderId (số nguyên dương), antiforgery token | 302 về /Pos, thông báo kiểm tra giá/tồn/khuyến mãi hoặc lỗi |

Bị từ chối: đơn rỗng, >=5 đơn giữ, ghi chú dài, ID không hợp lệ/không thuộc mình/hết hạn, giỏ đang bán có hàng, sản phẩm không bán/giá không hợp lệ/thiếu tồn. Lỗi nghiệp vụ không tạo hóa đơn hoặc ghi biến động tồn, giữ nguyên đơn/giỏ liên quan. Lỗi CSDL rollback giao tác.

Chưa có API JSON mới. Áp dụng kiểm tra quyền và antiforgery chuẩn của ứng dụng, không thay đổi hợp đồng API hiện có.
