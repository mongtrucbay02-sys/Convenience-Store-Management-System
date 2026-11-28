# Dàn ý slide kiểm thử và triển khai — #265
Bản nội dung chờ mẫu #6; không phải file PPTX đã nghiệm thu.
1. Mục tiêu: kiểm thử 9 module, tính toàn vẹn và an toàn dữ liệu.
2. Chiến lược: unit, integration PG17, API, UI/E2E, NFR; phân biệt tự động và thủ công.
3. CI: PR → restore → build → publish → PG17 test → verify TRX → artifact.
4. Baseline lịch sử 27/11: 877 PASS. Kết quả xác minh 28/11: 885 PASS, 0 FAIL, 0 SKIP, 116 giây tại baa30035; 16 ca security có TRX. Không gọi đây là 27 Must đã nghiệm thu.
5. Luồng nghiệp vụ: POS → hóa đơn → kho/quỹ → đổi trả còn trong phạm vi → báo cáo; KH-06 tắt cộng điểm mới, KH-07 cắt, KH-08 khóa server; giữ lịch sử điểm và thu hồi hợp lệ, giữ BC-10/KM; lịch sử test giao dịch và concurrency, chưa nghiệm thu freeze theo #257; LF-01–08 NOT RUN.
6. Triển khai: HTTPS reverse proxy → .NET 10 → PG17; backup/restore, schema adoption #283, staging chưa nghiệm thu.
7. Merge QA: chờ CI SHA cuối và review; release: HOLD. Hạn chế và bước tiếp: E2E browser, NFR, UAT, backup, review, F289-03.
## Hỏi đáp
- Vì sao PG17 cô lập? Không ảnh hưởng dữ liệu chung, tái lập giao dịch.
- 100% có nghĩa không lỗi? Không; chỉ 885 test đã thực thi trên checkout xác định.
- CI xanh có tự deploy? Không; workflow hiện build/test, cần gate deploy riêng.
- Rollback? Gói ứng dụng trước đó; dữ liệu theo kế hoạch owner duyệt.

## Gate distinction — PR #304 vs system release
- **Merge gate (QA contribution):** reviewed QA/test/workflow diff; verified final PR-head CI (expected 885 tests, zero fail/skip), traceable TRX, isolated-only execution, consistent scope/evidence and independent reviewer approval. Unresolved in-scope defects must be fixed or explicitly accepted by reviewers; do not silently waive fixture isolation evidence. Status: PENDING final CI and review.
- **Release gate (system):** approved SRS/Must mapping, WR-06–10 and TO-BE ACL/RLS, live read-only catalog/runtime verification, #257 freeze implementation and LF tests, browser E2E, NFR/UAT, full-schema restore, schema adoption, HTTPS/staging, release SHA and required sign-offs. Status: HOLD. These outstanding system gates are tracked separately; they do not automatically block merging a correctly scoped QA contribution, nor does QA merge authorize release, migration or issue closure.
- Evidence: [rolling QA report](../06-testing/test-reports/2026-11-27-qa-regression.md); last verified case-level evidence at `baa30035`: 885/885, 16 named security cases PASS in isolated PG17. Re-run on final head; do not relabel prior evidence as current-head PASS.
