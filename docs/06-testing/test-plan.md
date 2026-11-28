# Test Plan — Smart Mart
Version 1.2 QA merge/release gate review | 28/11/2026 | Source of truth #264 / PR #304 | Owner Tester/DevOps | Review pending
## Scope and basis
One store; AUTH, SP, NH, KHO, POS, KH, KM, DT, BC. Use PostgreSQL 17 and the official SRS v1.0.3 use-case list identified by Thăng in PR #301 (`docs/03-requirements/ma-tran-truy-vet-27-must_SRS-chinh-thuc.md`, SHA `ff68536`). The traceability document is available on **open/unmerged PR #301**, so it is a sourced draft crosswalk pending final independent SRS review. Do not use `../03-requirements/acceptance-criteria.md`, `moscow-priority.md`, or candidate FR/NFR as authoritative acceptance evidence. Bank integration, real customer data and production destructive tests excluded.
## Loyalty DISABLE & FREEZE — #257
Scope source: [#257 decision](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257#issuecomment-7241048563), [lead agreement](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257#issuecomment-7241091898). Scope agreed; implementation and acceptance pending.
- KEEP KH-01–05, NH-09, BC-09–11, KM-05/06; CUT DT-03/06 and KH-07; DISABLED KH-06; LOCKED KH-08 (server-side, including direct requests). Other scope unchanged.
- New checkout after approved cutoff: diem_cong=0, so_tien_mot_diem=NULL, no TICH_DIEM ledger entry or customer points increase. No missing-rule fallback or future active rule may silently reactivate accrual. Retain existing rules, all 31 tables and historic balances/records.
- New no-points invoices: cancellation/return in retained scope must not create loyalty movements. Historical invoices: legitimate reversals only, no duplicate reversal or negative balance; stock/cash/refund atomic. BC-10 remains historical/customer reporting; promotions unaffected. Do not claim CUT/DISABLED use cases passed as active features.
- LF-01–08 in candidate matrix are additional targeted scenarios, NOT RUN under final scope. Historical tests remain evidence of prior behavior, not final-scope acceptance. D5 privilege narrowing and live rollout require separate approved migration and backup/restore.
## Strategy
| Level | Method | Evidence |
|---|---|---|
| Unit | xUnit service boundaries, validation, rounding | TRX |
| Integration | PG17 isolated, real transactions, concurrency, rollback, RLS | TRX and DB assertions |
| API | positive/negative, auth, CSRF, stale version, retries | request/response redacted |
| UI/E2E | browser on disposable staging, cashier and admin, receipt and stock reconciliation | screenshots, trace |
| NFR | timed repeatable workload, role/session/security tests, recovery | raw samples and signed thresholds |
## Environment
.NET 10, EF Core/Npgsql, PostgreSQL 17; GitHub Actions Ubuntu 24.04. Use synthetic data; fixtures create/drop only owned random databases. Never use shared Supabase for write tests. Record SHA, OS, SDK, DB version, seed, browser, UTC+07 business date and runner.
## Execution
1. Freeze scope and approved acceptance criteria; map test IDs to UC and owners.
2. Restore/build/publish; run SmartMart CI on PR; verify TRX and retain artifact.
3. Execute candidate manual cases, then browser POS/DT, security and performance on isolated staging.
4. Log actual vs expected, evidence, status PASS/FAIL/BLOCKED/NOT RUN; retest fixes and full regression.
5. Review defect list, backup/restore, staging HTTPS and schema adoption #283 before release.
## Entry/exit
Entry: reviewed scope, reproducible build, synthetic seed, isolated DB, accounts and expected results. System release exit: all approved Must cases mapped and executed, no unresolved critical/high defects unless formally accepted, CI no fail/skip, NFR thresholds approved and met, browser E2E and backup/restore verified, two reviewers approve. CI alone is not release acceptance.
## Defects
Issue title [BUG][module] symptom; include SHA, environment, case ID, steps, expected/actual, redacted evidence, severity and owner. High target fix within two days per #41; retest before closure. Daily triage near release.
## Roles and schedule
Tester executes and reports; module owners fix; Yến/Thăng review final report per #264; deployment owner approves database operations. Review against the current #257 scope and frozen release SHA; previous calendar milestones are historical, not evidence. Blocked items remain open.


## Security Contract #300 / #293 — acceptance extension (28/11)
This is the **one** canonical test plan for #264. The final factual test report is `test-reports/2026-11-27-qa-regression.md`; other testing documents are source evidence, historical reports or case inventories, not competing final reports. PR #300 has merged **design documents only**; D1–D7, ACL migration and backend WR fixes are proposed, not deployed. The B02 observed contract at `docs/04-system-design/database/security-contract/evidence/observed-contract.json` is AS-IS **not TO-BE**.

### Environment and role separation
- Service A: existing CI PostgreSQL 17 loopback fixture (ordinary regression; may use `postgres` exclusively for database setup and administration). It does **not** prove runtime effective grants.
- Service B: separate disposable PostgreSQL 17 instance (CI `postgres-role`), separate disposable baseline and observed databases. Admin creates schema, disposable LOGIN `smartmart_backend`, seed and grants. Business probes connect **with real username/password** as `smartmart_backend`, verify `current_user = session_user = smartmart_backend`, `row_security=on`, LOGIN without SUPERUSER/BYPASSRLS/CREATEDB/CREATEROLE. `SET ROLE` alone is not valid proof.
- The fixture replays the checked-in B02 **observed** table/view/sequence/function ACL from the JSON contract; explicitly removes baseline broad table/sequence grants and replaces permissive baseline policy. It is not a substitute for applying the future approved migration with restrictive policies and trigger guards. Fail on drift/unknown objects, never broaden grants to make a test green; explicit temporary fault-injection grants in the isolated observed profile are separately identified and revoked.
- No Supabase shared write, reset, migration, or large-scale seed. No passwords, connection strings, business PII, TRX secret fields or backup dumps in commits/artifacts. Production TLS VerifyFull is separate from ephemeral loopback testing.
- On backend/migration branch changes, produce both fresh-baseline and live-like AS-IS → TO-BE fixtures, apply reviewed Up/Down/re-Up, assert effective privileges and policy/trigger behavior under the same login, then execute business operations. Build/CI source SHA must match the release candidate.

### Prioritized security test IDs
| ID | Issue | Setup and action under exact runtime login | Expected / evidence gate | State |
|---|---|---|---|---|
| SC-ACL-01 | #300 B02-11 | Recreate observed grants for 31 tables, 3 views, 20 sequences; inspect current/session user, RLS, has_table_privilege | Exact AS-IS grants; no TRUNCATE; role guard | PASS at baa30035; named TRX evidence in rolling report |
| SC-ACL-02 | #300 B02-11 | Real backend insert category, transaction rollback; DELETE vai_tro and UPDATE ct_hoa_don | Allowed insert visible in tx then rolled back; 42501 denied; invariant unchanged | PASS at baa30035; named TRX evidence in rolling report |
| SC-ACL-03 | #293 B08 | Apply *approved* migration on both isolated fresh and live-like databases; exact catalog diff/Up/Down/re-Up | Same TO-BE and reversion; policy/trigger/ACL, fail closed | BLOCKED: migration/approvals |
| WR-06 / NH02–04 | #300 D3 | NHAP draft add/update/remove line, concurrent confirm, terminal insert/update/delete, reopen header | Accurate totals/version, stock unaffected until confirm, no terminal edits, safe rollback/race | NOT RUN; WR-06 V2 pending |
| WR-07 / AU01–04 | #300 D4 | Bootstrap Admin under backend role; two concurrent initializations; role table SELECT only; negative ADMIN/TN/NK+CSRF | Exactly one admin; no role UPDATE; denied cases no mutations | NOT RUN under AS-IS ACL |
| WR-08 / PO01–02 | #300 D4 | AD cancels paid invoice, unauthorized role, concurrent cancel/return | Exact lock target, stock/cash atomic; historical points reversal only when applicable; no duplicate refund | NOT RUN under AS-IS ACL |
| WR-09 / QU01–02 | #300 D4 | AD settles cash once, concurrent settle, stale/insufficient balance | Only valid settlement; negative rolls back; no role UPDATE | NOT RUN under AS-IS ACL |
| WR-10 / PO03–04 | #300 D4 | Cashier holds and recalls own order, other actor/expired order, concurrency | Ownership/max five, locks only authorized table, no role UPDATE | NOT RUN under AS-IS ACL |
| RG01–03 | #300 B02-10 | Invalid login/superuser/bypassRLS/TLS/row_security; EF/raw/bootstrap paths | Fail closed, no data leak; loopback and true VerifyFull separately | Role-read guard existing; runtime write guard pending |
| CN01–02 | #264 | Last-unit concurrent checkout, injected failures into receipt, return, cash and stock | No oversell, no partial commit except permissible sequence gaps | Existing service regression; new exact-role proof pending |
| E2E-244 | #244 | Playwright browser login→cart→cash→invoice on disposable server | Trace, screenshot, browser result with CI gate | NOT RUN; HTTP flow test ≠ browser |
| BACKUP-264 | #264 | pg_dump/checksum/pg_restore on isolated full schema and demo sample | Catalog + business rows reconciled; no sensitive artifact | Synthetic table-only rehearsal PASS; real schema NOT VERIFIED |

**Defect rubric:** record test ID, SHA, environment, HTTP/SQLSTATE, repro, expected/actual, redacted log, severity P0/P1/P2, owner and retest. A denied DML `42501` is expected in negative cases; an unexpected `42501` during a legitimate workflow is FAIL, never SKIP/PASS. Distinguish deadlock `40P01` (retry policy review) from stale version `409/40001`, constraint `23514`, and credential/TLS failures. Prohibit fake PASS on tests not executed.

### Official 27 Must traceability gate
Thăng has supplied the **actual 27 Must UC IDs** from SRS v1.0.3 in [PR #301 at `ff68536`](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/ff68536fd6e8fcae4fed20975c3fcd2487b66101/docs/03-requirements/ma-tran-truy-vet-27-must_SRS-chinh-thuc.md). The canonical 27-row inventory is now `test-cases/candidate-regression.md` (historical filename retained for link stability). The formerly proposed `TC-01…27` are **not** official and their parenthesized labels were misleading; use `MUST-01…27` / official UC IDs only. Source PR #301 is still unmerged and under review, so the 27-item **identity** is sourced but final accepted SRS revision and per-UC coverage remain gated. The source notes 25/27 have associated historical automated classes—not 25/27 release PASS—and highlights NH-02, POS-02 (no dedicated automatic tests) and KHO-01, NH-04 (only service-level coverage). Add standalone authorized/manual or HTTP cases and record exact method, assertion, SHA, TRX/browser evidence, actor and reviewer. Never infer equivalence from identical row counts.

### Test gates and parallel ownership
1. Huy: fixture and baseline RED/GREEN coverage under B02 observed login; update #304 with TRX and acceptance report. Yến: WR-06–10 implementation and ACL/guard migration in separate code PR; do not change the test expected result to accommodate a regression.
2. Trung/Thăng/Hoài: approve D1–D7, WR-06 V2 and 27 Must source; contract MUST precede TO-BE migration acceptance.
3. Repeat exact-role test under reviewed migration and module code; require security negative, transaction, rollback and concurrency. After green CI, execute browser #244, realistic demo #103 validation, NFR and full backup/restore on isolated PG17.
4. Freeze final HEAD, publish TRX/test totals/open defects, obtain two independent reviews of #264, then update deploy #242/#44 and manual demo evidence. No merge or release solely on historical CI; apply the separate gates below.

## Gate distinction — PR #304 vs system release
- **Merge gate (QA contribution):** reviewed QA/test/workflow diff; verified final PR-head CI (expected 885 tests, zero fail/skip), traceable TRX, isolated-only execution, consistent scope/evidence and independent reviewer approval. Unresolved in-scope defects must be fixed or explicitly accepted by reviewers; do not silently waive fixture isolation evidence. Status: PENDING final CI and review.
- **Release gate (system):** approved SRS/Must mapping, WR-06–10 and TO-BE ACL/RLS, live read-only catalog/runtime verification, #257 freeze implementation and LF tests, browser E2E, NFR/UAT, full-schema restore, schema adoption, HTTPS/staging, release SHA and required sign-offs. Status: HOLD. These outstanding system gates are tracked separately; they do not automatically block merging a correctly scoped QA contribution, nor does QA merge authorize release, migration or issue closure.
- Evidence: [rolling QA report](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/docs/qa-devops-deliverables-20261127/docs/06-testing/test-reports/2026-11-27-qa-regression.md); last verified case-level evidence at `baa30035`: 885/885, 16 named security cases PASS in isolated PG17. Re-run on final head; do not relabel prior evidence as current-head PASS.
