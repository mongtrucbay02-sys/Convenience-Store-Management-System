# POS-04: kiểm thử khuyến mãi tự động

Nhánh: feature/pos-04. Không thay cấu trúc CSDL.

## Chạy giao diện

1. Dừng ứng dụng cũ, build/chạy lại nhánh feature/pos-04, đăng nhập thu ngân có BAN_HANG.
2. Chuẩn bị bằng module Khuyến mãi một chương trình đã kích hoạt, đang trong thời gian hiệu lực, gắn đúng sản phẩm có giá và tồn. Không tự nạp dữ liệu vào database chung.
3. Thêm sản phẩm: tên chương trình và tiền giảm xuất hiện ngay trên dòng; tổng giảm và tổng phải trả cập nhật.
4. Ví dụ giá 25.000đ, giảm 15%, số lượng 2: giảm mỗi đơn vị 3.700đ, tổng giảm 7.400đ, phải trả 42.600đ. Nếu trần 3.000đ/đơn vị: tổng giảm 6.000đ.
5. Nhiều chương trình: chọn giảm thực tế lớn nhất, hòa thì hết hạn sớm hơn, tiếp tục hòa thì ID nhỏ hơn.
6. Tạm dừng/hết hạn/gỡ phạm vi sản phẩm hoặc gỡ khách: tải lại giỏ, ưu đãi được tính lại, có thông báo; không có ưu đãi thì ẩn dòng giảm giá.
7. Chương trình chỉ thành viên: đơn chưa gắn khách không được giảm; gắn khách HOAT_DONG qua POS-03 rồi tải lại để tính lại. POS-04 không bổ sung chức năng gắn khách thay POS-03.
8. Nếu chương trình đổi giữa lúc xem và chốt thanh toán, máy chủ từ chối chốt và yêu cầu xác nhận lại. Không nhận giảm giá thủ công từ request.

## Khi không thấy khuyến mãi

Chạy truy vấn chỉ đọc trên đúng database ứng dụng đang kết nối:

```sql
SELECT p.ma, p.ten, p.trang_thai_hien_thi, p.chi_thanh_vien,
       p.bat_dau_luc, p.ket_thuc_luc, s.san_pham_id
FROM v_chuong_trinh_khuyen_mai p
LEFT JOIN khuyen_mai_san_pham s ON s.chuong_trinh_id = p.id
ORDER BY p.id;
```

Cần DANG_CHAY, đúng sản phẩm và đúng điều kiện thành viên. Dữ liệu local không tự xuất hiện trên Aiven.

## Kết quả 05/11/2026

Build đạt. 9/9 test PosPromotion đạt (tính toán và áp/gỡ khuyến mãi trên MySQL tạm dựng bằng migration). Chưa xác nhận thủ công trên database người dùng, CI hoặc staging.
