# QA regression report — baseline 27/11, rolling acceptance 28/11/2026
#264 #293 #300 #243 #244 | **Single final-report candidate**, review pending; actual release acceptance NOT VERIFIED
## Final executed QA contribution recheck — 28/11/2026 (latest verified source)

**Tested source SHA:** `9433da3a0992660cd687e3b926727d24759fc712` (PR #304, base `develop`, NOT deployed/merged). This recheck supersedes older pending/failed KHO-01 assertions below. A subsequent *report-only* commit must separately pass CI before being called final-head tested.

| QA workflow | Result | Evidence |
|---|---|---|
| Full SmartMart CI | **889 / 889 PASS, 0 FAIL, 0 SKIP, 0 ERROR, 0 TIMEOUT; 2m24s test** | [run 46165960573](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165960573), [TRX artifact 15271887149](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165960573/artifacts/15271887149) |
| Real Chromium browser POS E2E #244 | **PASS 1/1** | [run 46165960505](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165960505) |
| Isolated NFR HTTP-proxy workload | **PASS 1/1 execution**; SRS release thresholds only PARTIAL/NOT VERIFIED | [run 46165960545](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165960545) |
| Full-schema baseline + reference-data dump/restore | **PASS**, not real Supabase data or 10k-invoice demo restore | [run 46165960673](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165960673) |
| DB schema/secret guard | **PASS** | [run 46165960549](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165960549) |

**Independent TRX inspection:** downloaded GitHub artifact `15271887149`, parsed TRX `Counters` and all four named `UnitTestResult` records:
- `Nh02_HttpCreateEditDuplicatePhoneStaleVersionAndCsrf` — **Passed**
- `Pos02_HttpEditRemoveAndInvalidQuantity_DoNotChangeStockOrCreateInvoice` — **Passed**
- `Kho01_HttpStockListFiltersAndNoMutation` — **Passed**
- `Nh04_HttpCreateDraftPersistsLinesAndDoesNotMoveStock` — **Passed**

All four are HTTP tests on isolated PG17 with ordinary app role/permissions; **not** approved TO-BE `smartmart_backend` business writer acceptance or browser E2E of each Must. Browser #244 currently covers one happy path only. No changes to the shared Supabase.

**Disposition:** QA contribution technical regression/CI evidence **PASS at tested source only**. PR remains **DRAFT, review requested/pending**, with Yến's REQUEST CHANGES not autonomously dismissed. System **RELEASE HOLD** for #293 security backend/migration WR-06–10, final-scope loyalty freeze #257, broader negative/browser coverage, production-like NFR acceptance, full real demo recovery, HTTPS/deploy, official SRS PR #301 review and independent approval. Do not close #264/#244/#293 based solely on the green workflows.

## New executed QA evidence — 28/11/2026 (supersedes older QA-02/03/04/05 status below)

Source of the executed **application test code**: `5325248f028af730008db64c2600cd0fd0c2cc62`; all write operations ran on disposable GitHub Actions PostgreSQL 17, **never shared Supabase**.

| Gate / run | Evidence | Outcome and limit |
|---|---|---|
| QA-03 browser #244 [46165517017](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165517017) | Chromium E2E **1/1 PASS**, TRX + `pos-receipt.png` + `pos-trace.zip` [artifact 15271232265](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165517017/artifacts/15271232265); independently inspected ZIP | Actual UI login→add→cash→receipt, invoice/stock/cash reconciliation. **Happy path only**; deploy required-check configuration and negative/browser matrix pending |
| QA-04 NFR proxy [46165517092](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165517092) | **1/1 PASS** workload; 30 HTTP samples per HN01–04, 30 checkout responses, 30 rounds × 5 distinct user sessions; raw JSON + TRX [artifact 15271712153](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165517092/artifacts/15271712153), independently parsed | **MEASURED HTTP PROXY ONLY**, 1,000 synthetic products, 10,000 synthetic invoices spread over 180 days; not keyboard-to-render, live latency or SRS release PASS |
| QA-05 full baseline restore [46165517023](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165517023) | pg_dump/sha256/pg_restore on isolated PG17, **31 tables and 3 views**, roles/rule/policy/cash reference rows, source/restored count parity; artifact [15271711914](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165517023/artifacts/15271711914) | **PASS baseline full-schema + reference-data rehearsal**. Not full 10k-invoice demo snapshot, live Supabase backup or platform migration approval |
| QA-02 dedicated HTTP cases [46165517162](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46165517162) | 889 tests executed: **888 PASS, 1 FAIL, 0 SKIP** | NH-02, POS-02, NH-04 new HTTP cases passed. KHO-01 failed test assertion comparing Razor HTML-encoded Vietnamese text, not identified as a production inventory defect. Test assertion decoded HTML in subsequent commit `38aadc541d3dce1468f154ab5c9d12c26758093f`; **retest pending**, no final-head CI PASS claim |
| Security #300/#293 | Prior AS-IS real-login probes | AS-IS subset PASS only; approved TO-BE ACL/RLS, WR-06–10 business writes and D1–D7 sign-offs **BLOCKED** |

### Raw NFR HTTP proxy results (CI runner, milliseconds)

| Official SRS group | 30-sample p50 | p95 | Max | Scope |
|---|---:|---:|---:|---|
| NFR-HN-01 | 105.4 | 131.3 | 140.7 | POS search HTTP response, not keypress-to-render |
| NFR-HN-02 | 62.9 | 78.2 | 109.3 | checkout POST HTTP response and 30 committed synthetic sales |
| NFR-HN-03 | 33.5 | 48.1 | 48.6 | Dashboard GET HTTP response, not all monthly reports |
| NFR-HN-04 | 24.3 | 33.2 | 34.7 | inventory list page GET HTTP response, not every specified list |
| NFR-HN-05 | 171.4 | 217.0 | 233.0 | 150 requests over 30 simultaneous five-session rounds; 3 TN, 1 NK, 1 AD |

**Interpretation:** Measured p95 proxies are below nominal numerical thresholds but **the measured end points differ from the full SRS user-observed latency definitions**, and the percentile convention/production-like environment is not approved. Thus final NFR acceptance is **PARTIAL / NOT VERIFIED**, not PASS. NFR BM/TLS, UAT, online HTTPS and SRS release still HOLD. Data are synthetic and sample-size-specific; no extrapolation to Supabase live.

### What still blocks final acceptance
- Re-run all 889 ordinary regressions at final PR head after KHO-01 HTML decoding fix; retain artifact and precise run SHA.
- Verify official Must coverage beyond these four targeted HTTP positives, especially negative authorization, NH-04 terminal-state race and WR-06 V2 under the true backend login.
- Browser negative/permissions/deploy required check, true HTTPS app deployment, full demo/realistic restore and signed recovery runbook.
- WR-06–10 and TO-BE ACL/RLS migration not supplied/approved; Security Contract PR #300 was **docs-only**.
- Final SRS PR #301 and #257 loyalty freeze implementation review; independent Yến/Thăng approvals.

## Historical review reconciliation — QA-01…07 (before new execution)

Review sources: [Yến QA-01…07](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/304#pullrequestreview-6092820134), [REQUEST CHANGES correction](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/304#pullrequestreview-6092830408), [Thăng 27 Must and NFR](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/pull/304#pullrequestreview-6094976252).

- **QA-01 PARTIAL:** isolated real LOGIN smartmart_backend AS-IS table/view/sequence/function ACL + representative denied and rollback probes executed; complete **TO-BE business workflows** WR-06…10 and migration still BLOCKED. postgres must never be counted as business runtime.
- **QA-02 SOURCE CORRECTED, COVERAGE PENDING:** 27 **official** Must IDs sourced from [Thăng's PR #301 traceability at ff68536](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/ff68536fd6e8fcae4fed20975c3fcd2487b66101/docs/03-requirements/ma-tran-truy-vet-27-must_SRS-chinh-thuc.md) and mapped in `../test-cases/candidate-regression.md`. SRS PR #301 remains unmerged. Historical 25/27 associated classes ≠ 25/27 acceptance PASS; at that time direct NH-02 and POS-02 tests were missing and KHO-01 and NH-04 only service-level; all four now have dedicated HTTP tests in PR #304 (new evidence above). NH-03/06 and POS-07/08 formerly confused, now corrected.
- **QA-03 HISTORICAL STATUS:** browser E2E #244 previously required real Playwright automation, isolated host, trace and browser assertion. HTTP integration is not a substitute.
- **QA-04 HISTORICAL STATUS (before HTTP proxy run):** official HN-01 ≤1s product suggestions, HN-02 ≤2s finish checkout, HN-03 ≤3s monthly reports, HN-04 ≤2s list page, HN-05 ≥5 concurrent (3 TN/1 NK/1 AD). Scale 1,000 products + 10,000 invoices / 6 months. See `../nfr-verification.md`. Avoid candidate SIMULATED NFR-02…07; release perf acceptance BLOCKED.
- **QA-05 HISTORICAL STATUS (before full-schema baseline rehearsal):** pg_dump/checksum/restore PG17 workflow PASS; **full Smart Mart schema/demo-data restore and reviewer signoff NOT VERIFIED**.
- **QA-06 HISTORICAL EVIDENCE ADDED:** [CI 46029008105](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46029008105), source `6df3a6a32439b6d8e89dba6ab9a72a9dcecb3294`: log 877 PASS, 0 FAIL, 0 SKIP, **70 seconds**, TRX artifact `test-results-46029008105-1`, artifact ID **15213202308**. Historical only; newer runs and their own SHA recorded separately below.
- **QA-07 TRACKING:** Issue #264 is consolidated test acceptance; #244 remains independent browser automation dependency; #293 + merged docs-only #300 hold DB integration/security changes. Closed historical sprint issues are not retroactive PASS.

#257 scope now agreed by lead: KEEP NH-09, BC-09–11, KM-05/06; CUT DT-03/06, KH-07; KH-06 DISABLED, KH-08 LOCKED; existing historical loyalty preserved. This is **scope decision**, not completed code, migration, or passing final-scope LF tests. [Yến proposal](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257#issuecomment-7241048563), [Trung agreement](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257#issuecomment-7241091898).

**Review outcome:** Original Yến REQUEST CHANGES remains active pending independent re-review. The corrections above fix documentation/source problems only; functional/browser/release gates remain explicitly open.

## Latest verified CI and ACL/RLS-A/B technical acceptance — 28/11/2026

**Decision: PASS for implemented local AS-IS security probes; release HOLD.** Independent reviewer approval, live catalog verification and TO-BE acceptance remain outstanding.

Tested PR source SHA: `baa30035fca25b643ea83b45c5e8552a72d80cfd`. This is not a deployed release or the subsequent report-only commit.

| Metric | Result |
|---|---:|
| Total / executed / passed | 885 / 885 / 885 |
| Failed / skipped / error / timeout | 0 / 0 / 0 / 0 |
| Test duration | 116 seconds |
| SmartMart CI | [PASS 46160034408](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46160034408), [job 141025102485](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46160034408/job/141025102485) |
| DB schema and secret guard | [PASS 46160034434](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46160034434) |
| QA isolated backup rehearsal | [PASS 46160034432](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46160034432) — synthetic only |

### Evidence chain

| Stage | Tested source | Tests passed | CI run | TRX artifact ID |
|---|---|---:|---|---:|
| Fixture isolation | `6d648ba47b804742718548020c818127eedc3ffd` | 879 | [46159089567](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46159089567) | 15269316017 |
| ACL/RLS-A | `b58d484e4800d91ab51531b49959a054f0d042d7` | 880 | [46159646549](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46159646549) | 15268963622 |
| ACL/RLS-B | `baa30035fca25b643ea83b45c5e8552a72d80cfd` | 885 | [46160034408](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46160034408) | 15268614279 |

Latest artifact [test-results-46160034408-1](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46160034408/artifacts/15268614279), ZIP 224048 bytes. Downloaded and independently hashed; SHA-256 matches GitHub digest: `4e6d882f6207bc11eee42d36b84d46e30fd186dd8c60d20f418a3bee577aa83f`. Extracted tests.trx and parsed ResultSummary counters (885 total/executed/passed; zero failed, error, timeout, aborted, inconclusive, notRunnable and notExecuted). Named UnitTestResult records inspected at latest SHA, not merely inferred from aggregate count.

### Named security case evidence (latest TRX)

| Class / case | Outcome | Duration (ms, rounded) |
|---|---|---:|
| ObservedAclPostgreSqlTests.RealBackendLogin_ObservedAclAndRls_DenyForbiddenWritesAndRollbackAllowedWrite | Passed | 484 |
| ObservedAclPostgreSqlTests.RealBackendLogin_ObservedCatalog_MatchesExactLocalSecurityContract | Passed | 688 |
| ObservedSecurityBehaviorPostgreSqlTests.RealBackend_ObservedRls_VisibilityAndWithCheck_AreDistinctFromAcl | Passed | 487 |
| ObservedSecurityBehaviorPostgreSqlTests.RealBackend_SequenceAndFunctionPermissions_AndUpdatedAtTrigger | Passed | 487 |
| ObservedSecurityBehaviorPostgreSqlTests.RealBackend_ExactTriggerInventory_MatchesBaselineAttachments | Passed | 719 |
| ObservedSecurityBehaviorPostgreSqlTests.RealBackend_HistoryAclAndAppendOnlyTriggers_RejectMutationIndependently | Passed | 593 |
| ObservedSecurityBehaviorPostgreSqlTests.RealBackend_InvoiceGuard_ProtectsFinancialSnapshotAndRollback | Passed | 471 |

The same TRX contains nine passing SupabaseRuntimeRoleIntegrationTests: actual backend login, admin rejection, five unsafe role variants, SET ROLE/session-user mismatch and read-write guard rejection. Total named security cases: 16 passed, zero failed.

### Acceptance and limitations

- ACL/RLS-A: PASS for implemented local inventory and privilege assertions (31 tables, 3 views, 20 sequences, 3 functions; 31 RLS policies; direct ACL, effective privileges, grant options, ownership, RLS flags and active status). Local default ACL and outgoing membership checks are not evidence of live platform defaults or incoming memberships.
- ACL/RLS-B: PASS for implemented representative RLS, sequence/function, 17 baseline trigger attachments, six history tables, invoice cancellation and rollback probes. The restrictive policy and temporary extra grants are fault-injection controls only in the disposable database, not approved TO-BE ACL or production changes. Historical loyalty records are synthetic preservation probes, not reactivation of loyalty flows (#257).
- F304-01: local AS-IS remediation verified; broader live contract coverage remains open (Supabase-managed roles, incoming platform membership, live defaults, exact live trigger attachments/function bodies).
- F304-02: fixture remediation and baseline ACL/RLS fingerprint assertions PASS in full suite; independent class execution, explicit reversed order and cleanup failure injection are still NOT RUN. Do not mark final isolation acceptance complete.
- F304-03 / WR-06–10 / ACL/RLS-C: OPEN/BLOCKED; approved TO-BE migration and real-login business flow tests are not supplied by these probes. No release or live migration approval.
- Merge gate: local QA evidence PASS for this tested source, pending remaining PR findings, independent review and CI at final head. Release gate remains HOLD (official Must mapping, browser E2E, NFR, UAT, full-schema restore, HTTPS, live runtime and WR remediation outstanding).

## Historical verified CI — initial PR #304 (879 tests)

Source head: `34c2dae83831b254ae433a6dcb03bd9b3f151e95` (PR source SHA associated with the verified runs; not a deployed release or the subsequent report-only commit).

| Metric | Result |
|---|---:|
| Total | 879 |
| Passed | 879 |
| Failed | 0 |
| Skipped | 0 |
| Pass rate | 100% |
| Test duration | 71 seconds |

| Workflow | Result | Evidence |
|---|---|---|
| SmartMart CI | PASS | [Run 46156886228](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156886228), [job 141015395460](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156886228/job/141015395460) |
| DB schema and secret guard | PASS | [Run 46156886217](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156886217) |
| QA isolated backup rehearsal | PASS (synthetic only) | [Run 46156886211](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156886211) |

CI log: `Verified: 879/879 tests passed; no failures or skipped tests.` Build, publish, PostgreSQL-only dependency verification, isolated PG17 tests and artifact upload succeeded. TRX artifact: `test-results-46156886228-1`, ID `15267743766`, 222001 bytes ([artifact](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156886228/artifacts/15267743766)). Aggregate execution verified from job logs; individual TRX cases have not been independently parsed.

The suite PASS applies only to the tested PR checkout. Any subsequent report-only or code commit needs its own CI evidence; release remains HOLD. Synthetic restore is not full-schema recovery, and AS-IS ACL probes are not TO-BE or WR-06–10 acceptance.

## Historical verified CI — baseline 27/11
Head cc20336566ff3ac12933e3c1c65c36a6553b2000; checkout merge affe1c4 into ef3a2557fb7ad825c46086292eb2c42a5fadb97a (per log). [Rerun job](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/45874035651/job/140604355863).
| Metric | Result |
|---|---:|
| Total | 877 |
| Passed | 877 |
| Failed | 0 |
| Skipped | 0 |
| Pass rate | 100% |
| Test duration | 73 seconds |
Build Release, publish PostgreSQL-only, dependency verification, isolated PG17 test, TRX verification and artifact upload all succeeded. This is a PR merge checkout, not evidence of a deployed environment. No per-case TRX inspection in this review.
## Scope and limits
Historical report 2026-11-25-pr289-review.md documents POS/return/stock/points/cash/report transaction cases and concurrency; not independent proof of individual rerun results. Browser E2E, 27 Must mapping, manual UAT, NFR, staging and backup restore remain NOT RUN/NOT VERIFIED.
## Open risks
F289-03 historical category snapshot: last documented design gap; current resolution unverified. #283 existing Supabase adoption: release blocker until owner review. #244 browser E2E/deploy gate unverified. #183 approved performance targets and samples absent. #264 backup/restore and reviewer sign-off absent.
## Decision
Automated suite PASS for tested checkout only. Final acceptance pending Yến/Thăng review, exact release SHA, case-level evidence and staging/restore. No issue closure implied.


## Update — PR #300 security contract and PR #304 (28/11)
**Reporting rule:** this is the sole rolling final QA report for #264; historical reports and candidate matrices are cited as evidence, not parallel final reports. The canonical test plan is `../test-plan.md`. Only verified CI runs and actual artifacts may be counted as PASS.

- PR #300 **merged docs-only** into develop at `f102268690e5ae22a571c71e9c4a887ab2258ca9`. This delivered proposed B02 observed contract / WR-01–10, **not** any backend patch, privilege migration or live write acceptance. Security Contract D1–D7 still require approval.
- On PR #304, run [46155721462](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46155721462) succeeded at source `f2ccf6dff22d49417b7019e9ef7cbe9e7c693fb8`: log states **878 passed / 0 failed / 0 skipped**, TRX uploaded. One added authenticated **HTTP** cashier POS→receipt regression is included. This is not a Playwright/browser run or a runtime-`smartmart_backend` business-write proof.
- Next #304 commits introduce `ObservedAclPostgreSqlTests`, B02 AS-IS snapshot replay in a disposable PG17 role-test service, exact real-login `current_user/session_user`, table privilege assertions, authorized insert+transaction rollback, and expected 42501 denials. **Execution verified:** [run 46156604109](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156604109) on source `b4b28ece76b317625177e4485d898bb619878aac`: **879 PASS, 0 FAIL, 0 SKIP**, 1m29s test, TRX artifact `test-results-46156604109-1` ID `15267778341`. This verifies B02 AS-IS fixture regression, not approved TO-BE ACL/migration or all WR-06–10 application flows. No per-test TRX parsing in this review.
- Existing read-only smoke `SupabaseRuntimeRoleIntegrationTests` verifies SELECT on 31 tables/3 views using a real backend login in dedicated PG17, but intentionally uses `SET TRANSACTION READ ONLY` and therefore cannot attest normal business writes.
- QA synthetic backup rehearsal had passed on [46155721467](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46155721467). A subsequent intermediate run [46156413537](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156413537) failed at transient container startup (`pg_isready` no response); PR #304 workflow updated to wait on TCP readiness. The subsequent fixed TCP-readiness rehearsal [46156604162](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156604162) **PASS** at the same source `b4b28ec`; the start-up flake is resolved for this run. Neither synthetic drill constitutes restore of a full Smart Mart schema or demo dataset.

### Acceptance matrix — no inference from a green suite
| Gate | Status | Decision / evidence |
|---|---|---|
| Unit/integration baseline (HTTP/service) | PASS at `baa30035` | 885/885; latest verified CI above; rerun for any subsequent head |
| B02 AS-IS backend login / observed table ACL / negative privileges | PASS for implemented local probes at `baa30035` | 16 named security cases PASS; exact local catalog and behavioral probes, not full live or TO-BE acceptance |
| WR-06–10 corrected backend + approved TO-BE ACL/RLS migration | BLOCKED | PR #300 design merged; backend/migration PR and D1–D7 approval outstanding |
| 27 official MoSCoW Must UC→TC map | SOURCE CORRECTED; FINAL COVERAGE BLOCKED | Thăng's official 27 UC IDs from open PR #301 now mapped; gaps NH-02/POS-02/KHO-01/NH-04 and per-case evidence |
| Browser POS E2E #244 | NOT RUN | Existing new check is HTTP, not browser |
| NFR measured thresholds | NOT VERIFIED | Official SRS HN-01…05 thresholds now sourced from Thăng; no measured samples on 1k/10k data |
| Full-schema demo backup/restore and reviewer approval | NOT VERIFIED | Synthetic-only drill does not satisfy |
| Online HTTPS deployment, exact role/profile, production secrets | NOT VERIFIED | #242/#44; shared Supabase stays read-only |
| Final regression and independent Yến/Thăng sign-off | PENDING | Freeze release SHA after changes, rerun full suite and inspect TRX |

**Release decision: HOLD despite 885/885.** Do not close #264/#293 or approve a live migration based solely on historical or latest green checks. Login role `postgres` is allowed **only** for disposable fixture administration, never for the business SQL counted as security acceptance. In shared Supabase: no seed, mutation, migration, reset or destructive test.

#257 QA scope synchronization: lead agreement recorded; LF-01–08 are proposed final-scope scenarios, NOT RUN. See candidate regression matrix. Preserve historical CI and loyalty data protection results; no final freeze implementation claim.

## Gate distinction — PR #304 vs system release
- **Merge gate (QA contribution):** reviewed QA/test/workflow diff; verified final PR-head CI (expected 885 tests, zero fail/skip), traceable TRX, isolated-only execution, consistent scope/evidence and independent reviewer approval. Unresolved in-scope defects must be fixed or explicitly accepted by reviewers; do not silently waive fixture isolation evidence. Status: PENDING final CI and review.
- **Release gate (system):** approved SRS/Must mapping, WR-06–10 and TO-BE ACL/RLS, live read-only catalog/runtime verification, #257 freeze implementation and LF tests, browser E2E, NFR/UAT, full-schema restore, schema adoption, HTTPS/staging, release SHA and required sign-offs. Status: HOLD. These outstanding system gates are tracked separately; they do not automatically block merging a correctly scoped QA contribution, nor does QA merge authorize release, migration or issue closure.
- Evidence: [rolling QA report](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/docs/qa-devops-deliverables-20261127/docs/06-testing/test-reports/2026-11-27-qa-regression.md); last verified case-level evidence at `baa30035`: 885/885, 16 named security cases PASS in isolated PG17. Re-run on final head; do not relabel prior evidence as current-head PASS.
