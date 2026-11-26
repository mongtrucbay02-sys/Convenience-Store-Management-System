# Bàn giao kiểm thử và tài liệu (NOT RUN)

Mỗi ID là scenario cần triển khai, không phải số test hiện có. Dữ liệu test synthetic trên PostgreSQL 17 disposable, không Supabase shared. Tách admin connection migrate/seed/assert (host loopback, disposable cluster) với real-login runtime role; test bằng actual `current_user=session_user=smartmart_backend`, không SET ROLE thay cho real login. Tạo test credentials ngẫu nhiên, không log; role cluster-wide isolation như PR299; teardown chỉ tài nguyên test. Kiểm tra TLS VerifyFull trong môi trường phù hợp chứng chỉ; không nới production vì local CI. Baseline 877 test hiện hữu vẫn phải pass, không cố định tổng sau khi thêm test.

| ID | Scenario | Expected evidence |
|---|---|---|
| CT01 | Catalog 31/3/20 + function/trigger count expected | Exact names, ownership, grants, policies, defs, flags; fail missing/extra |
| CT02 | Mỗi table × SELECT/I/U/D, forbidden TRUNCATE/REFERENCES/TRIGGER/MAINTAIN/grant option | Privilege assertions; D conditional tests riêng, không nhầm has_table_privilege với row visibility |
| CT03 | PUBLIC/anon/authenticated, column grants, memberships, schema CREATE, DB CREATE | Denied, no bypass; role flags/owner exact; TEMP recorded |
| CT04 | 3 views, sequences nextval/RETURNING; setval denied | SELECT works; writes denied; sequence USAGE/SELECT only |
| CT05 | Function EXECUTE and search_path, trigger firing | Direct execute denied, trigger works; preserve history 23514/42501 as appropriate |
| CT06 | Fresh baseline vs live-like AS-IS → Up | Same normalized target; 33 policies expected if proposed design unchanged |
| CT07 | Missing role, extra object, ACL drift, mid-migration failure | Fail closed, rollback unchanged |
| CT08 | Isolated Up/Down/re-Up | Exact pre-state restore, data/history unchanged |
| SP01 | WR-01/02/03 ngừng SP, mở lại, stale version, stock warning, permission AD | No physical delete, price/stock preserved, UI/API consistent after D1 |
| SP02 | SP create price, edit threshold, cancel future price, image metadata | Successful under backend role; past price not rewritten |
| NH01 | WR-04 NCC stop no refs/with history, draft exists | Preserve history; draft blocks; UI/API approved |
| NH02 | WR-06 draft edit add/update/remove lines | DELETE only removed rows, totals correct, stock unchanged |
| NH03 | Direct I/U/D detail confirmed/cancelled; parent reassignment | Denied by guard/policy, no data change |
| NH04 | Confirm vs edit parallel, retry/duplicate confirm | Serialize or safe abort; one stock posting, no post-confirm detail changes |
| NH05 | Quick product creation, receipt confirm/cancel, failure stock posting | Same transaction, rollback counter/receipt/stock/audit as applicable; sequence gaps permitted |
| AU01 | WR-07 bootstrap valid config with SELECT-only vai_tro | Exactly one Admin, force password change; no UPDATE role |
| AU02 | 2 concurrent bootstrap + concurrent other create AD path | Same lock protocol or explicit invariant; no duplicate bootstrap; no secret in logs |
| AU03 | AUTH login wrong password, session insert/revoke, change password, lock/reset/role assignment | Proper account/session writes, reject last Admin loss; no role-table mutation |
| AU04 | AD/TN/NK endpoint negative + CSRF, stale version | 403/409 as applicable; no DB changes, not just RLS pass |
| PO01 | WR-08 cancel paid invoice as AD | Only t locked in auth JOIN; invoice canceled once, restore stock/points, refund/ledger atomic |
| PO02 | TN/NK cancel denied; insufficient cash; cancel twice; return conflicts | No partial mutation, no double refund, proper business error |
| PO03 | Cart create/update/delete/hold/resume/cancel; promotion recalculation; checkout | Correct I/U/D, lock promotions/stock, no oversell |
| DT01 | Return preview/create/approve/refund per scope; policy FOR SHARE | Correct quantity/threshold, points/stock/quỹ, no double return |
| QU01 | WR-09 chốt quỹ AD, concurrent chốt, stale balance | One successful chốt, before/after correct, no role UPDATE |
| QU02 | Sell/refund/cancel vs concurrent quỹ transaction | Lock balance; no negative; no duplicate cash event; rollback on failure |
| KH01 | WR-05 unreferenced customer delete | Exactly 1 row removed; no PII left |
| KH02 | Customer referenced by invoice/points/cart/return, open cart | Hide/anonymize for retained reference; open cart rejected; direct DELETE denied/0 rows; FK race safe |
| KH03 | Concurrent attach customer vs delete/anonymize | Safe serialization/constraint, no dangling FK or partial PII mutation |
| KH04 | Customer create/edit/points adjust, KH-06 default/disabled rule | No invalid negative points; rule table reads only after D5 |
| KM01 | Promotion create/update/delete only unreferenced, link replace | I/U/D permissions, FOR SHARE retained; checkout race consistent |
| KO01 | Inventory count/disposal, threshold, history update/delete denied | Consistent stock, append-only ledger, negative stock blocked |
| BC01 | Reports and 3 views using real backend | Read-only, no hidden DML; no write grants to views |
| RG01 | Guard production TLS wrong/missing CA, role postgres/wrong role, bypassRLS, flags, row_security off | Reject before business SQL, no secrets; test each EF/raw connection path including pool checkout and bootstrap |
| RG02 | Valid runtime role, connection reuse, write transaction not read-only; deployment admin separate | Normal business operation; admin profile cannot leak into runtime |
| RG03 | HTTP/UI route changes after SP/NCC approval | No deceptive delete response, correct permissions/confirmation, regression Razor/API |
| CN01 | Two checkout requests same stock; receipt confirm vs stock change | No oversell, exactly once posting, rollback invariants |
| CN02 | Fault injection after each stage of NH/POS/DT/cancel/cash/points | Transaction atomic; DB data restored except allowed sequence gaps |

## Huy — kiểm thử độc lập
Huy chịu trách nhiệm viết test code, fixture, CI, bảng test case và evidence kỹ thuật cho toàn bộ scenario trên; thêm test trực tiếp sửa giá đã hiệu lực và header phiếu nhập quay lại NHAP để phân biệt invariant service/DB. Yến chỉ sửa backend/migration nếu phát hiện lỗi, không tự nghiệm thu. Không dùng role postgres chạy nghiệp vụ, không chạy DML hoặc destructive tests trên Supabase dùng chung.

## Thăng — tài liệu
Thăng phụ trách cung cấp SRS gốc đã duyệt, cập nhật SRS/use case/API nếu hành vi thay đổi; ADR, security contract mô tả từ matrix/migration của Yến; ERD/từ điển nếu có tác động, deployment runbook; tổng hợp test report từ kết quả Huy. Ghi rõ SHA, môi trường, test ID, số pass/fail/skip, workflow/artifact; không tự ghi PASS khi chưa có bằng chứng.

## Điều kiện nghiệm thu
- Trung duyệt D1–D7, Thăng/Hoài/Huy review phạm vi liên quan. Không tự merge; mọi thay đổi nghiệp vụ phải có approval và cập nhật tài liệu theo CONTRIBUTING.
- B04–B08: code và contract tương thích, migration mới không sửa lịch sử; fresh và live-like hội tụ; backup/restore rehearsal.
- B09–B11: toàn bộ 877 test nền và test mới PASS, không skip bắt buộc; exact catalog, real-login write, negative AD/TN/NK tại app, transaction, rollback, race; không còn P0/P1; P2 sửa hoặc có chấp nhận rủi ro.
- Chỉ rollout Supabase sau owner/lead phê duyệt và isolated PASS. Sau deploy smoke read-only; không test ghi phá hủy live. B12 Trung nghiệm thu, đối chiếu checklist #293 và evidence; không đóng trước khi đủ.
