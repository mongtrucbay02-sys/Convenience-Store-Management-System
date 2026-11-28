# Ma trận kiểm thử 27 Must theo SRS chính thức — Issue #264

**Nguồn:** [Ma trận 27 Must/NFR của Thăng, PR #301, commit ff68536](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/ff68536fd6e8fcae4fed20975c3fcd2487b66101/docs/03-requirements/ma-tran-truy-vet-27-must_SRS-chinh-thuc.md); SRS v1.0.3 mục 3.2 / use case mục 3.4–3.12. **PR #301 đang OPEN, chưa merge/được nghiệm thu cuối**. Đây là 27 ID Must do Thăng cung cấp, khác 27 ca TC ứng viên ban đầu. Tên tệp giữ `candidate-regression.md` để các liên kết PR không gãy.

Cột “test liên quan” lấy từ ma trận của Thăng và test HTTP mới của #304; **không phải chứng cứ từng use case đã nghiệm thu**. Source ghi 25/27 có test liên quan tại baseline 877/877; 2 chưa có test riêng. Không đồng nhất số ca PASS với coverage luồng chính/ngoại lệ. Để xác nhận từng dòng phải bổ sung method/actor, expected, PASS/FAIL/SKIP, SHA, TRX hoặc screenshot, và hai reviewer.

**Cảnh báo sửa mã sai:** NH-03 *chính thức* = ngừng hợp tác/xóa NCC; xác nhận nhập kho = **NH-06**. POS-08 *chính thức* = tạm giữ/gọi lại đơn; bán đồng thời món cuối thuộc **POS-07**. Không dùng nhãn TC-09(NH-03), TC-21(POS-08) cũ làm official trace. Những ca cũ vẫn là ý tưởng hồi quy, không được coi là use case Must chính thức.

| ID test | UC Must chính thức | Issue | Test tự động có liên quan (baseline/historical) | Lớp và việc còn thiếu | Trạng thái nghiệm thu |
|---|---|---|---|---|---|
| MUST-01 | AUTH-01 Đăng nhập | #23 | DichVuXacThucTests; PostgreSqlRuntimeTests.Auth_CreateLoginChangePasswordLogout_AndLockAfterFiveFailures | PG integration + HTTP — Đúng/mất mật khẩu, khóa tạm, session; role | PENDING per-UC evidence |
| MUST-02 | AUTH-02 Đăng xuất | #24 | DichVuXacThucTests; PostgreSqlRuntimeTests.Auth_*Logout* | PG integration — Hủy session/giỏ hợp lệ, deny protected request after logout | PENDING per-UC evidence |
| MUST-03 | AUTH-03 Đổi mật khẩu | #25 | DichVuTaiKhoanTests; PostgreSqlRuntimeTests.SM01_* | PG integration — Mật khẩu cũ/mới, forced-change, revoke old session | PENDING per-UC evidence |
| MUST-04 | AUTH-05 Xem tài khoản | #26 | DichVuTaiKhoanQuanTriTests | PG integration; HTTP TODO — Danh sách Admin, TN/NK bị từ chối | PENDING per-UC evidence |
| MUST-05 | AUTH-06 Tạo tài khoản | #27 | DichVuTaiKhoanQuanTriTests; KhoiTaoAdminDauTienTests | PG integration; exact role TODO — Tạo role hợp lệ, SĐT tùy chọn, bootstrap đồng thời WR-07 | PENDING per-UC evidence |
| MUST-06 | AUTH-10 Gán vai trò và phân quyền | #28 | QuyenVaTokenTests; DichVuTaiKhoanTests.GanVaiTro_* | Unit + PG integration — 27 mã quyền, actor AD, không UPDATE vai_tro trái ACL | PENDING per-UC evidence |
| MUST-07 | SP-01 Xem/tìm/lọc sản phẩm | #56 | Sp01ProductHttpTests; Sp01ProductPostgreSqlTests | HTTP + PG — Tìm dấu tiếng Việt, lọc danh mục và trạng thái | PENDING per-UC evidence |
| MUST-08 | SP-03 Thêm sản phẩm | #58 | Sp03ProductHttpTests; Sp03ProductPostgreSqlTests | HTTP + PG — SP+giá+tồn 0 atomic; role; WR-03 guard | PENDING per-UC evidence |
| MUST-09 | SP-04 Sửa sản phẩm | #59 | Sp04ProductHttpTests; Sp04ProductPostgreSqlTests | HTTP + PG — Validation, version và quyền | PENDING per-UC evidence |
| MUST-10 | SP-05 Cập nhật giá bán | #60 | Sp05PricingHttpTests; Sp05PricingPostgreSqlTests | HTTP + PG — Không sửa giá quá khứ, hiệu lực giá | PENDING per-UC evidence |
| MUST-11 | SP-07 Danh sách danh mục | #62 | CategoryHttpFeatureTests; CategoryPostgreSqlTests | HTTP + PG — Danh mục; phân trang/role | PENDING per-UC evidence |
| MUST-12 | SP-08 Thêm/sửa danh mục | #63 | Sp08CategoryHttpTests; Sp08CategoryPostgreSqlTests; DanhMucConcurrencyTests | HTTP + PG — Version/race, quyền CRUD | PENDING per-UC evidence |
| MUST-13 | KHO-01 Xem tồn kho hiện tại | #89 | DichVuKhoTests.LayTonKho_KhongKhoa_TraDungThuTuVaCoDuoiNguong | Service only; HTTP/manual TODO — Trang tồn, ngưỡng cảnh báo, role, không mutation | PENDING per-UC evidence |
| MUST-14 | NH-01 Xem danh sách NCC | #65 | PurchasingParityTests | HTTP + PG — Danh sách, lọc; role | PENDING per-UC evidence |
| MUST-15 | NH-02 Thêm/sửa NCC | #66 | No dedicated automated case confirmed | HTTP/manual TODO — Create/edit NCC, duplicate code/phone, permission and version | PENDING per-UC evidence |
| MUST-16 | NH-04 Tạo phiếu nhập nháp | #92 | PurchaseReceiptFlowTests (indirect) | Service only; HTTP/manual TODO — NHAP persisted, lines/totals, stock unchanged until confirm | PENDING per-UC evidence |
| MUST-17 | NH-06 Xác nhận nhập kho | #94 | TichHopDichVuKhoTests.Nh06_*; PurchaseReceiptFlowTests; PostgreSqlRuntimeTests.Purchasing_ConfirmTwice* | PG integration — Stock+ledger exactly once, rollback & concurrency | PENDING per-UC evidence |
| MUST-18 | NH-07 Tra cứu phiếu nhập | #95 | PurchasingParityTests | HTTP + PG — Lọc NCC/ngày/trạng thái, role | PENDING per-UC evidence |
| MUST-19 | POS-01 Tạo đơn/tìm/thêm giỏ | #33 | PostgreSqlRuntimeTests.Cart_ConcurrentFirstRequests*; PosPromotionDatabaseTests | PG integration + HTTP — Session cashier, thêm hàng, chính sách giá | PENDING per-UC evidence |
| MUST-20 | POS-02 Sửa giỏ hàng | #34 | PosHeldOrderTests; PosPromotionDatabaseTests (indirect); PosHttp_CashierCheckoutToReceipt (partial) | HTTP/browser/manual TODO — Sửa số lượng, xóa dòng, vượt tồn, concurrent update | PENDING per-UC evidence |
| MUST-21 | POS-05 Thanh toán tiền mặt | #35 | PostgreSqlRuntimeTests.Checkout_CommitsInvoiceStockPointsAndCash*; PosHttp_CashierCheckoutToReceipt | PG integration + HTTP — Tiền trả thiếu, tiền thừa, atomic cash/stock/points theo #257 | PENDING per-UC evidence |
| MUST-22 | POS-07 Hoàn tất đơn/in hóa đơn | #97 | TichHopDichVuKhoTests.Pos07_*; PostgreSqlRuntimeTests.ConcurrentCheckout*; PosInvoiceReceiptRenderTests | PG integration + render — Không bán quá tồn, hóa đơn và retry, in lại không ghi | PENDING per-UC evidence |
| MUST-23 | POS-09 Hủy đơn đang bán | #36 | DichVuXacThucTests.Pos09_*; PosHeldOrderTests | PG integration — Dọn giỏ, không hủy hóa đơn đã thanh toán | PENDING per-UC evidence |
| MUST-24 | POS-10 Tra cứu hóa đơn | #98 | PosInvoiceDetailsTests; PosInvoiceStoreTests | Unit + PG — Tìm hóa đơn, không phát sinh bán mới | PENDING per-UC evidence |
| MUST-25 | BC-01 Dashboard tổng quan | #45 | DashboardServiceTests | Unit only; HTTP TODO — Tổng hợp chính xác theo BR-BC-02 sau quyết định REC-D01 | PENDING per-UC evidence |
| MUST-26 | BC-02 Doanh thu thời gian | #105 | OperationalReportsServiceTests; PostgreSqlRuntimeTests.Reports_UseVietnamMidnightForUtcPaidAt | Unit + PG — Ngày UTC+7, cancel/refund; REC-D01 | PENDING per-UC evidence |
| MUST-27 | BC-03 Doanh thu sản phẩm/bán chạy | #106 | OperationalReportsServiceTests; ReportSnapshotPostgreSqlTests | Unit + PG — Dữ liệu bán thực, lịch sử danh mục | PENDING per-UC evidence |

### Bốn khoảng trống review QA-02 của Thăng
1. **NH-02 (MUST-15)**: cần HTTP/PG riêng tạo/sửa NCC, duplicate, authorization/rollback.
2. **POS-02 (MUST-20)**: cần HTTP/browser sửa/xóa dòng giỏ, số lượng âm/vượt tồn, đồng thời.
3. **KHO-01 (MUST-13)**: thêm HTTP/Razor/manual tồn kho và quyền, vì chỉ có service test.
4. **NH-04 (MUST-16)**: thêm test nháp trước xác nhận, chứng minh chưa nhập tồn.

### Các scenario khác ngoài 27 Must
WR-06…10, SP/KH/DT/POS âm quyền, NFR, đổi trả và LF-01…08 vẫn phải test theo scope (#257/#264/#293), nhưng **không ép thành 27 Must**. POS-08 tạm giữ/gọi lại và NH-03 ngừng NCC là use case khác; theo dõi ở security test plan #300. Nếu chưa có dữ liệu source cho một ngoại lệ SRS thì BLOCKED, không suy diễn theo tài liệu SIMULATED.

## Additional loyalty freeze scenarios (outside the 27 candidate rows)
Scope source: [#257 decision](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257#issuecomment-7241048563), [lead agreement](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257#issuecomment-7241091898). Scope agreed; implementation and acceptance pending.
All below NOT RUN under approved final scope; use isolated PG17 real runtime login, no shared Supabase writes.
| ID | Scenario | Expected |
|---|---|---|
| LF-01 | New checkout, customer attached, disabled effective rule | Zero points, NULL conversion snapshot, no TICH_DIEM or balance increase |
| LF-02 | Missing effective rule or future active rule | Fail closed against accrual; rollout preflight detects future reactivation; no fallback award |
| LF-03 | KH-08 direct endpoint and UI | Server denies manual adjustment, no ledger/balance mutation; UI hidden |
| LF-04 | KH-07 configuration access | No active configuration feature in final scope; no unauthorized writer |
| LF-05 | Cancel/return new zero-point invoice in retained scope | No loyalty movement; cash/stock/refund correct and atomic |
| LF-06 | Cancel/return historical awarded invoice, including retry and insufficient balance | Valid reversal only, no duplicate or negative balance; historical invoice snapshot preserved |
| LF-07 | KH-05 and BC-10 historical/customer reports, KM discounts | Historical balances and reporting preserved; promotions unchanged |
| LF-08 | Backup/restore and disable-rule rollout rehearsal | All 31 tables/history retained; rule ordering and future rules verified; no reset/drop; owner approval before live |

## Gate distinction — PR #304 vs system release
- **Merge gate (QA contribution):** reviewed QA/test/workflow diff; verified final PR-head CI (expected 885 tests, zero fail/skip), traceable TRX, isolated-only execution, consistent scope/evidence and independent reviewer approval. Unresolved in-scope defects must be fixed or explicitly accepted by reviewers; do not silently waive fixture isolation evidence. Status: PENDING final CI and review.
- **Release gate (system):** approved SRS/Must mapping, WR-06–10 and TO-BE ACL/RLS, live read-only catalog/runtime verification, #257 freeze implementation and LF tests, browser E2E, NFR/UAT, full-schema restore, schema adoption, HTTPS/staging, release SHA and required sign-offs. Status: HOLD. These outstanding system gates are tracked separately; they do not automatically block merging a correctly scoped QA contribution, nor does QA merge authorize release, migration or issue closure.
- Evidence: [rolling QA report](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/docs/qa-devops-deliverables-20261127/docs/06-testing/test-reports/2026-11-27-qa-regression.md); last verified case-level evidence at `baa30035`: 885/885, 16 named security cases PASS in isolated PG17. Re-run on final head; do not relabel prior evidence as current-head PASS.
