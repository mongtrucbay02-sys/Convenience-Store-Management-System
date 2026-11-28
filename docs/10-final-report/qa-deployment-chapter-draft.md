# Chương kiểm thử và triển khai — bản thảo #245
## 1. Chiến lược kiểm thử
Smart Mart sử dụng kiểm thử đơn vị, tích hợp PostgreSQL, API và nghiệm thu giao diện. Kế hoạch và tiêu chí tại [Test Plan](../06-testing/test-plan.md). Phạm vi gồm AUTH, SP, NH, KHO, POS, KH, KM, DT, BC; ma trận 27 ca đề xuất chưa thay thế danh sách Must gốc.
Phạm vi tích điểm theo #257: KH-06 DISABLED/FREEZE, KH-07 CUT, KH-08 LOCKED/FREEZE; giữ KH-01–05, BC-10 và dữ liệu lịch sử, không thay đổi khuyến mãi. Trung đã đồng ý phạm vi; chưa nghiệm thu triển khai. Hóa đơn mới sau mốc tắt phải không cộng điểm; chỉ thu hồi hợp lệ điểm lịch sử khi hủy/trả còn trong phạm vi. Giữ 31 bảng, không reset dữ liệu. Các ca LF-01–08 chưa chạy theo phạm vi cuối. [Quyết định #257](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257#issuecomment-7241048563).
## 2. Môi trường và kết quả
GitHub Actions Ubuntu 24.04, .NET 10, PostgreSQL 17 cô lập, xUnit và TRX. Lượt CI ngày 27/11/2026 checkout merge affe1c4 chứa head cc203365: 877/877 đạt, không lỗi hoặc bỏ qua, thời gian test 73 giây. Build và publish đạt. [Báo cáo](../06-testing/test-reports/2026-11-27-qa-regression.md). Đây là kết quả tự động, không phải nghiệm thu toàn bộ nghiệp vụ.
## 3. Kiểm thử phi chức năng
Đo p50/p95/max với dữ liệu và thiết bị cố định; kiểm tra phân quyền, CSRF, phiên, bảo mật cấu hình và tính toàn vẹn giao dịch. [Quy trình NFR](../06-testing/nfr-verification.md). Chưa có số đo và ngưỡng gốc được duyệt.
## 4. Triển khai
Browser → reverse proxy HTTPS → ASP.NET Core MVC/Razor → PostgreSQL 17/Supabase qua private backend role và TLS. CI restore/build/publish/test/verify TRX; triển khai chỉ sau review, E2E, backup và schema adoption #283. [Hướng dẫn](../07-deployment/deployment-guide.md). Không tự chạy baseline trên schema đang tồn tại.
## 5. Sao lưu và phục hồi
pg_dump custom, checksum, lưu mã hóa, restore vào PG17 cô lập và đối soát. [Runbook](../07-deployment/backup-restore.md). Chưa có bằng chứng thực hiện.
## 6. Hạn chế và nghiệm thu
F289-03 cần xác nhận quyết định lịch sử danh mục; browser E2E, NFR, UAT, HTTPS, backup/restore và hai reviewer chưa được xác nhận. Bản thảo cần gộp vào DOCX cuối, không tự ghi PASS.
