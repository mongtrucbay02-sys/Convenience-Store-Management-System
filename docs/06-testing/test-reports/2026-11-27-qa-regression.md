# QA regression report — baseline 27/11, rolling acceptance 28/11/2026
#264 #293 #300 #243 #244 | **Single final-report candidate**, review pending; actual release acceptance NOT VERIFIED
## Verified CI
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
- Next #304 commits introduce \`ObservedAclPostgreSqlTests\`, B02 AS-IS snapshot replay in a disposable PG17 role-test service, exact real-login \`current_user/session_user\`, table privilege assertions, authorized insert+transaction rollback, and expected 42501 denials. **Execution status: PENDING at time of this report**; check the latest PR Checks before claiming PASS.
- Existing read-only smoke \`SupabaseRuntimeRoleIntegrationTests\` verifies SELECT on 31 tables/3 views using a real backend login in dedicated PG17, but intentionally uses \`SET TRANSACTION READ ONLY\` and therefore cannot attest normal business writes.
- QA synthetic backup rehearsal had passed on [46155721467](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46155721467). A subsequent intermediate run [46156413537](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/actions/runs/46156413537) failed at transient container startup (\`pg_isready\` no response); PR #304 workflow updated to wait on TCP readiness. Later successful rerun evidence required before treating this regression as resolved. Neither synthetic drill constitutes restore of a full Smart Mart schema or demo dataset.

### Acceptance matrix — no inference from a green suite
| Gate | Status | Decision / evidence |
|---|---|---|
| Unit/integration baseline (HTTP/service) | PASS at \`f2ccf6d\` only | 878/878, link above; new head must rerun |
| B02 AS-IS exact backend login / table ACL / negative privileges | PENDING | New test in PR #304, no confirmed CI result yet |
| WR-06–10 corrected backend + approved TO-BE ACL/RLS migration | BLOCKED | PR #300 design merged; backend/migration PR and D1–D7 approval outstanding |
| 27 official MoSCoW Must UC→TC map | BLOCKED | Candidate 27 != official list. Need approved SRS and #257 scope |
| Browser POS E2E #244 | NOT RUN | Existing new check is HTTP, not browser |
| NFR measured thresholds | NOT VERIFIED | No approved numerical samples |
| Full-schema demo backup/restore and reviewer approval | NOT VERIFIED | Synthetic-only drill does not satisfy |
| Online HTTPS deployment, exact role/profile, production secrets | NOT VERIFIED | #242/#44; shared Supabase stays read-only |
| Final regression and independent Yến/Thăng sign-off | PENDING | Freeze release SHA after changes, rerun full suite and inspect TRX |

**Release decision: HOLD.** Do not close #264/#293 or approve a live migration based on 877/878 historical green checks. Login role \`postgres\` is allowed **only** for disposable fixture administration, never for the business SQL counted as security acceptance. In shared Supabase: no seed, mutation, migration, reset or destructive test.
