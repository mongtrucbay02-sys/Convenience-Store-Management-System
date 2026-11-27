# Test Plan — Smart Mart
Version 1.0 candidate | 27/11/2026 | #15 | Owner Tester/DevOps | Review pending
## Scope and basis
One store; AUTH, SP, NH, KHO, POS, KH, KM, DT, BC. Use current PostgreSQL runtime, candidate FR/AC in ../03-requirements/acceptance-criteria.md and actual approved SRS when supplied. Original 27 Must list is outside repository; do not infer equivalence. Bank integration, real customer data and production destructive tests excluded.
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
Entry: reviewed scope, reproducible build, synthetic seed, isolated DB, accounts and expected results. Exit: all approved Must cases mapped and executed, no unresolved critical/high defects unless formally accepted, CI no fail/skip, NFR thresholds approved and met, browser E2E and backup/restore verified, two reviewers approve. CI alone is not release acceptance.
## Defects
Issue title [BUG][module] symptom; include SHA, environment, case ID, steps, expected/actual, redacted evidence, severity and owner. High target fix within two days per #41; retest before closure. Daily triage near release.
## Roles and schedule
Tester executes and reports; module owners fix; Yến/Thăng review final report per #264; deployment owner approves database operations. 27–29/11: regression, manual/NFR, restore rehearsal, review/freeze. Blocked items remain open.
