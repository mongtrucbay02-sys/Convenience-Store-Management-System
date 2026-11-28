# QA regression report — baseline 27/11, rolling acceptance 28/11/2026
#264 #293 #300 #243 #244 | **Single final-report candidate**, review pending; actual release acceptance NOT VERIFIED
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
**Reporting rule:** this is the sole rolling final QA report for #264; historical reports and candidate matrices are cited as evidence, not parallel final reports. The canonical test plan is \`../test-plan.md\`. Only verified CI runs and actual artifacts may be counted as PASS.

- PR #300 **merged docs-only** into develop at \`f102268690e5ae22a571c71e9c4a887ab2258ca9\`. This delivered proposed B02 observed contract / WR-01–10, **not** any backend patch, privilege migration or live write acceptance. Security Contract D1–D7 still require approval.
- On PR #304, run [46155721462](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46155721462) succeeded at source \`f2ccf6dff22d49417b7019e9ef7cbe9e7c693fb8\`: log states **878 passed / 0 failed / 0 skipped**, TRX uploaded. One added authenticated **HTTP** cashier POS→receipt regression is included. This is not a Playwright/browser run or a runtime-\`smartmart_backend\` business-write proof.
- Next #304 commits introduce \`ObservedAclPostgreSqlTests\`, B02 AS-IS snapshot replay in a disposable PG17 role-test service, exact real-login \`current_user/session_user\`, table privilege assertions, authorized insert+transaction rollback, and expected 42501 denials. **Execution verified:** [run 46156604109](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156604109) on source `b4b28ece76b317625177e4485d898bb619878aac`: **879 PASS, 0 FAIL, 0 SKIP**, 1m29s test, TRX artifact `test-results-46156604109-1` ID `15267778341`. This verifies B02 AS-IS fixture regression, not approved TO-BE ACL/migration or all WR-06–10 application flows. No per-test TRX parsing in this review.
- Existing read-only smoke \`SupabaseRuntimeRoleIntegrationTests\` verifies SELECT on 31 tables/3 views using a real backend login in dedicated PG17, but intentionally uses \`SET TRANSACTION READ ONLY\` and therefore cannot attest normal business writes.
- QA synthetic backup rehearsal had passed on [46155721467](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46155721467). A subsequent intermediate run [46156413537](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156413537) failed at transient container startup (\`pg_isready\` no response); PR #304 workflow updated to wait on TCP readiness. The subsequent fixed TCP-readiness rehearsal [46156604162](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156604162) **PASS** at the same source `b4b28ec`; the start-up flake is resolved for this run. Neither synthetic drill constitutes restore of a full Smart Mart schema or demo dataset.

### Acceptance matrix — no inference from a green suite
| Gate | Status | Decision / evidence |
|---|---|---|
| Unit/integration baseline (HTTP/service) | PASS at `baa30035` | 885/885; latest verified CI above; rerun for any subsequent head |
| B02 AS-IS backend login / observed table ACL / negative privileges | PASS for implemented local probes at `baa30035` | 16 named security cases PASS; exact local catalog and behavioral probes, not full live or TO-BE acceptance |
| WR-06–10 corrected backend + approved TO-BE ACL/RLS migration | BLOCKED | PR #300 design merged; backend/migration PR and D1–D7 approval outstanding |
| 27 official MoSCoW Must UC→TC map | BLOCKED | Candidate 27 != official list. Need approved SRS and #257 scope |
| Browser POS E2E #244 | NOT RUN | Existing new check is HTTP, not browser |
| NFR measured thresholds | NOT VERIFIED | No approved numerical samples |
| Full-schema demo backup/restore and reviewer approval | NOT VERIFIED | Synthetic-only drill does not satisfy |
| Online HTTPS deployment, exact role/profile, production secrets | NOT VERIFIED | #242/#44; shared Supabase stays read-only |
| Final regression and independent Yến/Thăng sign-off | PENDING | Freeze release SHA after changes, rerun full suite and inspect TRX |

**Release decision: HOLD despite 885/885.** Do not close #264/#293 or approve a live migration based solely on historical or latest green checks. Login role \`postgres\` is allowed **only** for disposable fixture administration, never for the business SQL counted as security acceptance. In shared Supabase: no seed, mutation, migration, reset or destructive test.

#257 QA scope synchronization: lead agreement recorded; LF-01–08 are proposed final-scope scenarios, NOT RUN. See candidate regression matrix. Preserve historical CI and loyalty data protection results; no final freeze implementation claim.

## Gate distinction — PR #304 vs system release
- **Merge gate (QA contribution):** reviewed QA/test/workflow diff; verified final PR-head CI (expected 885 tests, zero fail/skip), traceable TRX, isolated-only execution, consistent scope/evidence and independent reviewer approval. Unresolved in-scope defects must be fixed or explicitly accepted by reviewers; do not silently waive fixture isolation evidence. Status: PENDING final CI and review.
- **Release gate (system):** approved SRS/Must mapping, WR-06–10 and TO-BE ACL/RLS, live read-only catalog/runtime verification, #257 freeze implementation and LF tests, browser E2E, NFR/UAT, full-schema restore, schema adoption, HTTPS/staging, release SHA and required sign-offs. Status: HOLD. These outstanding system gates are tracked separately; they do not automatically block merging a correctly scoped QA contribution, nor does QA merge authorize release, migration or issue closure.
- Evidence: [rolling QA report](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/docs/qa-devops-deliverables-20261127/docs/06-testing/test-reports/2026-11-27-qa-regression.md); last verified case-level evidence at `baa30035`: 885/885, 16 named security cases PASS in isolated PG17. Re-run on final head; do not relabel prior evidence as current-head PASS.
