# QA regression report — 27/11/2026
#264 #243 #244 | Review pending
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
