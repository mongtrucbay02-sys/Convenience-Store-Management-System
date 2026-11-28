# DT-01 — Tra cứu hóa đơn gốc (issue #148)

## Chuẩn bị

Dùng nhánh feature/dt-01 đã cập nhật develop e01c0ad. Đăng nhập website bằng tài khoản TN có DOI_TRA, đã đổi mật khẩu lần đầu. Tài khoản MySQL dev_hoai chỉ dùng cho chuỗi kết nối, không nhập ở form đăng nhập. Cấu hình ConnectionStrings:CuaHang bằng User Secrets; nếu POS còn khóa PosMySql thì dùng cùng database để tạo/tra hóa đơn nhất quán.

Không cần migration mới cho DT-01. Dữ liệu để thử: chính sách đổi trả đang hiệu lực, hóa đơn đã thanh toán có chi tiết, khách thành viên có số điện thoại và danh mục có cờ khong_doi_tra. Nếu database chung trống, vẫn mở được form; nhưng chưa thử đầy đủ kết quả hóa đơn. Chạy test tự động ở dưới dùng CSDL tạm, không cần nạp dữ liệu vào Aiven.

Kiểm tra dữ liệu chỉ đọc trong Workbench:

```sql
USE cuahang_codefirst_dev;
SELECT COUNT(*) AS so_chinh_sach FROM chinh_sach_doi_tra;
SELECT id, so_ngay_doi_tra, hieu_luc_tu FROM chinh_sach_doi_tra
WHERE hieu_luc_tu <= NOW() ORDER BY hieu_luc_tu DESC, id DESC;
SELECT ma_hoa_don, thanh_toan_luc, thu_ngan_id, trang_thai
FROM hoa_don ORDER BY id DESC LIMIT 20;
```

Nạp dữ liệu demo qua quy trình nhóm đã thống nhất; không tự cập nhật hóa đơn bất biến hoặc bảng kho bằng SQL chỉ để tạo ngoại lệ.

## Thử trên giao diện

Stop Debug, Ctrl+F5, đăng nhập TN, mở https://localhost:7173/Returns hoặc bấm Đổi trả.

| Ca | Thao tác | Kết quả cần thấy |
|---|---|---|
| Luồng chính | Nhập đúng mã một hóa đơn còn hạn, của TN khác | Ngày mua, khách, dòng hàng, thực trả/đơn vị, đã trả/đang xử lý, còn được trả; dòng hợp lệ |
| Theo khách | Nhập duy nhất số điện thoại thành viên 10 số | Danh sách hóa đơn gần đây còn trong hạn; chọn hóa đơn xem từng dòng; điện thoại kết quả được che |
| E1 | Nhập mã không tồn tại | Không tìm thấy hóa đơn; không có hóa đơn gốc để tiếp tục |
| E2 | Nhập chính xác mã hóa đơn DA_HUY | Hiển thị ĐÃ HỦY, từng dòng không hợp lệ; không cho tiếp tục đổi trả |
| E3 quá hạn | Tra mã hóa đơn quá hạn theo chính sách | Ngoại lệ — cần AD duyệt, chỉ tiếp tục ở DT-06 khi có duyệt; không tự chấp nhận |
| E3 danh mục | Tra hóa đơn có sản phẩm thuộc danh mục không đổi trả | Đánh dấu riêng dòng bị hạn chế |
| BR-DT-02 | Mua 10, phiếu CHO_DUYET 1, DA_DUYET 2, HOAN_TAT 3, BI_TU_CHOI 4 | Đã trả/đang xử lý 6, còn trả 4; không cộng phiếu bị từ chối |
| Quyền | Mở MVC và API bằng AD hoặc NK | 403; không nới quyền POS-10 để xem hóa đơn người khác |
| Chưa đăng nhập | Mở MVC/API khi không có phiên hợp lệ | 401 theo AUTH |
| Thiếu chính sách | Tra cứu khi không có chính sách hiệu lực | Thông báo liên hệ quản lý (API 409), không tự giả định chính sách |

DT-01 chỉ tra cứu và đánh giá. Chưa tạo phiếu DT-02/03 hoặc phê duyệt DT-06. Kết quả phải được kiểm tra lại trong transaction khi các bước sau lập phiếu. Giới hạn ngày theo lịch Việt Nam: mua ngày 10/11 lúc 21:00, chính sách 7 ngày thì hết hạn cuối ngày 17/11.

## Chạy tự động

```powershell
dotnet test tests/ConvenienceStore.Web.Tests --filter "FullyQualifiedName~Tests.Returns"
```

Fixture dùng ConnectionStrings__CuaHangTest để tạo database cuahang_test_* riêng và dọn sau test, không dùng database nghiệp vụ trong User Secrets. 50 ca DT-01 đã đạt ngày 05/11/2026. Bộ test toàn dự án còn 2 lỗi baseline, xem docs/backend/DT01.md.

## Bằng chứng và tiêu chí bàn giao

Chụp ca hợp lệ (có hóa đơn TN khác), không tìm thấy, đã hủy và ngoại lệ. Che thông tin khách hàng. Đưa kết quả test vào PR, nhờ Trung review. PR ghi Closes #148, đích develop. Chỉ đánh dấu staging đã đạt sau khi thực sự chạy kiểm thử tại đó; kết quả fixture local không thay thế staging.
