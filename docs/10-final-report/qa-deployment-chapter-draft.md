# Chương kiểm thử và triển khai — bản thảo #245
## 1. Chiến lược kiểm thử
Smart Mart sử dụng kiểm thử đơn vị, tích hợp PostgreSQL, API và nghiệm thu giao diện. Kế hoạch và tiêu chí tại [Test Plan](../06-testing/test-plan.md). Phạm vi gồm AUTH, SP, NH, KHO, POS, KH, KM, DT, BC; ma trận 27 ca đề xuất chưa thay thế danh sách Must gốc.
Phạm vi tích điểm theo #257: KH-06 DISABLED/FREEZE, KH-07 CUT, KH-08 LOCKED/FREEZE; giữ KH-01–05, BC-10 và dữ liệu lịch sử, không thay đổi khuyến mãi. Trung đã đồng ý phạm vi; chưa nghiệm thu triển khai. Hóa đơn mới sau mốc tắt phải không cộng điểm; chỉ thu hồi hợp lệ điểm lịch sử khi hủy/trả còn trong phạm vi. Giữ 31 bảng, không reset dữ liệu. Các ca LF-01–08 chưa chạy theo phạm vi cuối. [Quyết định #257](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257#issuecomment-7241048563).
## 2. Môi trường và kết quả
GitHub Actions Ubuntu 24.04, .NET 10, PostgreSQL 17 cô lập, xUnit và TRX. Baseline lịch sử ngày 27/11: 877/877. Lượt CI đã xác minh ngày 28/11 tại baa30035: 885/885 đạt, 0 lỗi, 0 bỏ qua, 116 giây; 16 ca security có bằng chứng TRX riêng. Build và publish đạt. [Báo cáo](../06-testing/test-reports/2026-11-27-qa-regression.md). Đây là kết quả tự động, không phải nghiệm thu toàn bộ nghiệp vụ.
## 3. Kiểm thử phi chức năng
Đo p50/p95/max với dữ liệu và thiết bị cố định; kiểm tra phân quyền, CSRF, phiên, bảo mật cấu hình và tính toàn vẹn giao dịch. [Quy trình NFR](../06-testing/nfr-verification.md). Chưa có số đo và ngưỡng gốc được duyệt.
## 4. Triển khai
Browser → reverse proxy HTTPS → ASP.NET Core MVC/Razor → PostgreSQL 17/Supabase qua private backend role và TLS. CI restore/build/publish/test/verify TRX; triển khai chỉ sau review, E2E, backup và schema adoption #283. [Hướng dẫn](../07-deployment/deployment-guide.md). Không tự chạy baseline trên schema đang tồn tại.
## 5. Sao lưu và phục hồi
pg_dump custom, checksum, lưu mã hóa, restore vào PG17 cô lập và đối soát. [Runbook](../07-deployment/backup-restore.md). Rehearsal bảng tổng hợp cô lập PASS; phục hồi toàn bộ schema và dữ liệu demo chưa được xác minh.
## 6. Hạn chế và nghiệm thu
F289-03 cần xác nhận quyết định lịch sử danh mục; browser E2E, NFR, UAT, HTTPS, backup/restore và hai reviewer chưa được xác nhận. Bản thảo cần gộp vào DOCX cuối, không tự ghi PASS.

## Gate distinction — PR #304 vs system release
- **Merge gate (QA contribution):** reviewed QA/test/workflow diff; verified final PR-head CI (expected 885 tests, zero fail/skip), traceable TRX, isolated-only execution, consistent scope/evidence and independent reviewer approval. Unresolved in-scope defects must be fixed or explicitly accepted by reviewers; do not silently waive fixture isolation evidence. Status: PENDING final CI and review.
- **Release gate (system):** approved SRS/Must mapping, WR-06–10 and TO-BE ACL/RLS, live read-only catalog/runtime verification, #257 freeze implementation and LF tests, browser E2E, NFR/UAT, full-schema restore, schema adoption, HTTPS/staging, release SHA and required sign-offs. Status: HOLD. These outstanding system gates are tracked separately; they do not automatically block merging a correctly scoped QA contribution, nor does QA merge authorize release, migration or issue closure.
- Evidence: [rolling QA report](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/docs/qa-devops-deliverables-20261127/docs/06-testing/test-reports/2026-11-27-qa-regression.md); last verified case-level evidence at `baa30035`: 885/885, 16 named security cases PASS in isolated PG17. Re-run on final head; do not relabel prior evidence as current-head PASS.
