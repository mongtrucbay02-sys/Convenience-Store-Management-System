# Candidate regression matrix — #16
27 proposed cases, NOT RUN as individual cases. Based on candidate AC and issue criteria, not the authoritative 27 Must list dated 12/10 (not available in repository). Review and map before acceptance. Each row: seed prerequisite; action; expected. Record actual, status, SHA and evidence per execution.
| ID | Trace | Given / When | Expected |
|---|---|---|---|
| TC-01 (AUTH-01) | NFR-03 | valid cashier; login | session with cashier rights |
| TC-02 (AUTH-02) | NFR-03 | cashier; request admin operation | server denies; no mutation |
| TC-03 (AUTH-03) | NFR-03 | expired session; request protected page | login required |
| TC-04 (SP-01) | AC-01a | active product; search by name | correct product |
| TC-05 (SP-02) | AC-01b | inactive product; add to cart | rejected |
| TC-06 (SP-03) | AC-10a | cashier; change price | denied |
| TC-07 (NH-01) | AC-06a | delivery 12, received 10; save | difference -2 |
| TC-08 (NH-02) | AC-06b | negative received quantity; submit | validation error |
| TC-09 (NH-03) | AC-07a | authorized confirmation of 10 | stock +10 once |
| TC-10 (NH-04) | AC-07b | pending receipt; inspect stock | no confirmed movement |
| TC-11 (NH-05) | AC-07c | confirmed receipt; retry | no duplicate stock |
| TC-12 (KHO-01) | AC-08a | stock 18 count 17; save | pending -1, stock unchanged |
| TC-13 (KHO-02) | AC-09a | authorized approval of count | decision audit and correct stock |
| TC-14 (POS-01) | AC-02a | 2 x 12000 + 9000; tender 50000 | total 33000 change 17000 |
| TC-15 (POS-02) | AC-02b | unpaid cart; cancel | no completed invoice |
| TC-16 (POS-03) | AC-03a | stock sufficient; checkout | stock decreases exactly once |
| TC-17 (POS-04) | AC-03b | retry same checkout | no duplicate invoice/movement |
| TC-18 (POS-05) | AC-03c | inject failure mid checkout | atomic rollback |
| TC-19 (POS-06) | AC-04a | saved invoice; lookup | same invoice, no new sale |
| TC-20 (POS-07) | AC-05a | authorized reprint | no new transaction |
| TC-21 (POS-08) | #102 | last item; two simultaneous checkouts | one sale, no negative stock |
| TC-22 (KH-01) | #257 / KH-06 | eligible customer; checkout after approved disable cutoff | zero awarded points, NULL conversion snapshot, no accrual ledger or balance increase; NOT RUN |
| TC-23 (KM-01) | #182 | eligible promotion; checkout | correct discount per approved rule |
| TC-24 (DT-01) | #243 / #257 | retained return flow for new or historical invoice | refund and stock consistent; no new accrual; legitimate historical reversal only, no negative balance or duplicate movement; NOT RUN |
| TC-25 (DT-02) | #243 | repeat same return | no duplicate refund |
| TC-26 (BC-01) | AC-11a | paid and unpaid transactions; report | only eligible transactions |
| TC-27 (BC-02) | F289-03 | move product category; historical report | compare with approved category policy; BLOCKED until decision |
Execution priority proposed: AUTH/SP/NH/POS first, then KHO and cross-module regression. IDs in parentheses are local labels, not official UC codes.

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
