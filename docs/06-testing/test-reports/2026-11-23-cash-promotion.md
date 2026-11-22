# Kiểm thử sửa quỹ, hủy hóa đơn và ảnh khuyến mãi

Ngày 23/11/2026. Trạng thái: **Đã kiểm thử cục bộ, đang review**. Mã được kiểm thử và commit: `ce06dff`, nhánh `fix/integrate-main-develop-20261122`. Chưa push/triển khai trong đợt này; chưa xác nhận CI GitHub, staging hay review độc lập. Bố cục điện thoại **để sau theo yêu cầu người dùng**.

## Phạm vi đã sửa

- POS-12 ghi chi quỹ cùng transaction với hoàn kho, thu hồi điểm, ghi kiểm toán và chuyển trạng thái hóa đơn.
- Admin chốt số tiền thực đếm một lần, có lý do, kiểm tra số dư đã thay đổi; TN không được phép. Không suy diễn hay cộng lại tiền từ hóa đơn cũ.
- Thêm unique key chống trùng giao dịch tiền mặt và trigger chặn sửa/xóa lịch sử.
- Lưu URL ảnh khuyến mãi qua MySQL và ánh xạ EF; ảnh vẫn còn sau khi tạo lại DbContext và tải lại trang.

Thiết kế/migration chờ duyệt: [sổ quỹ và ảnh](../../04-system-design/database/cash-promotion-20261123.md). Đề xuất SRS: [CP-01–04](../../03-requirements/cash-promotion-change-proposal-20261123.md).

## Kết quả thực chạy

| Kiểm tra | Kết quả |
|---|---|
| Build solution, Release, `--no-restore` | **0 lỗi, 0 cảnh báo** |
| Toàn bộ project test trên MySQL 8.4.13 riêng | **707/707 đạt, 0 lỗi, 0 bỏ qua**, 3 phút 10 giây theo VSTest |
| Gate `scripts/ci/verify-trx.ps1` | Đạt: kiểm tra TRX có test, không failed/skipped |
| HTTP với session đăng nhập thật | **41/41 đạt**: anonymous, AD, TN; các màn hình cũ, `/CashLedger`, POST chốt quỹ thiếu antiforgery |
| Trình duyệt máy tính | AD đăng nhập, chốt thử 1.000.000 đ trên DB tạm; lịch sử có một dòng, biểu mẫu chốt lại biến mất |
| Ảnh KM trên trình duyệt | Tạo nháp có URL ảnh, tải lại trang: còn URL, ảnh `complete=true` và `naturalWidth>0` |
| Đối chiếu DB sau thao tác UI | `so_du=1000000`, `da_chot_so_du_dau=1`, một dòng `CHOT_SO_DU_DAU`; URL ảnh nằm trong `anh_url` |
| Migration và mapping | Bộ test schema/rollback/reapply đạt; 31 bảng nghiệp vụ, 305 cột bảng nghiệp vụ, 3 view, 14 trigger, 5 migration; không phát sinh DbContext thứ hai |

Bảy test mới: hủy hoàn kho/điểm/quỹ và không hoàn hai lần; quỹ thiếu rollback; lỗi ghi kiểm toán sau khi ghi quỹ rollback toàn bộ; hủy đồng thời chỉ một thành công; chốt cần Admin/không chấp nhận số dư cũ/không chốt lại/lịch sử bất biến; DB chặn giao dịch trùng; URL ảnh qua tạo–đọc mới–sửa–xóa–đọc lại.

Lần chạy đầu gặp kiểm tra baseline còn đếm 10 trigger và kết nối giữ named lock của EF/MySQL khi các fixture chạy song song. Đã tách baseline khỏi trigger mở rộng, bổ sung kiểm tra hai trigger mới và đóng kết nối vật lý của fixture bằng `Pooling=false`. Lượt đầy đủ sau sửa đạt 707/707. Đây là thay đổi hạ tầng test, không tắt pooling của ứng dụng.

## Môi trường và bằng chứng

Native MySQL 8.4.13 trên loopback `33592`; mỗi fixture dùng `cuahang_test_<8hex>` được tạo bằng migration và xóa khi kết thúc. Web tạm tại `127.0.0.1:33593`, dùng tài khoản kiểm thử được tạo riêng. Đã dừng web và MySQL test sau khi ghi bằng chứng. Không ghi lên Aiven Dev chung, không thay tài khoản thật.

- [Verification và hash mã nguồn/TRX](../evidence/2026-11-23/cash-promotion/verification.json).
- [Build](../evidence/2026-11-23/cash-promotion/build.txt), [test](../evidence/2026-11-23/cash-promotion/test.txt), [41 kết quả HTTP](../evidence/2026-11-23/cash-promotion/http-smoke.json).
- [Metadata schema thực tế](../evidence/2026-11-23/cash-promotion/runtime-schema.json).
- [Màn hình quỹ](../evidence/2026-11-23/cash-promotion/cash-opening-desktop.png), [ảnh khuyến mãi sau tải lại](../evidence/2026-11-23/cash-promotion/promotion-image-desktop.png).

TRX đầy đủ lưu tại `outputs/cash-promotion-final-20261123/test-results/tests.trx` của workspace, SHA256 trong verification. Không đưa tệp TRX hơn 1 MB vào Git; GitHub CI sẽ lưu TRX bằng artifact khi được chạy thật. Bộ hỗ trợ HTTP/UI trong `tmp/cash-ui-smoke` ở workspace là harness kiểm chứng cục bộ, chưa phải E2E tự động gắn vào deploy theo #244. Restore của harness có cảnh báo NU1900 vì không lấy được dữ liệu đánh giá lỗ hổng NuGet; build solution và lượt test chính không có cảnh báo/lỗi.

Chụp toàn trang từng bị công cụ trình duyệt timeout; đã khôi phục phiên, xác minh UI và lưu ảnh viewport thông thường. Chưa nghiệm thu toàn bộ độ rộng màn hình hoặc bố cục điện thoại.

## Còn cần thực hiện trước nghiệm thu

- #1 duyệt hai migration/ERD/SRS theo #1/#234; sao lưu và kiểm tra dữ liệu trùng trước khi áp dụng. Chưa tự sửa baseline đã duyệt hoặc đánh dấu được duyệt.
- Chạy workflow trên GitHub sau khi chủ repo push, lấy run URL; chưa đồng nhất kết quả local với CI.
- Deploy staging có cấu hình đúng, kiểm thử upgrade trên bản sao có dữ liệu lịch sử, kiểm thử người dùng và review độc lập.
- Điện thoại làm sau. Đợt này không tự hoàn thành các issue tuần 8: KM-05 #247, KM-06 #246, BC-09 #248, BC-10 #249, BC-11 #250, báo cáo #251, sơ đồ #252. Đã đọc nội dung issue để đối chiếu liên quan; chưa triển khai thêm các chức năng đó trong đợt sửa.
