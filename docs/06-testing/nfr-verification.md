# NFR verification protocol — #183
Status: protocol ready, measurements NOT RUN. Source ../03-requirements/non-functional-requirements.md is candidate; original NFR-HN/BM/TC thresholds unavailable.
| Candidate NFR | Test procedure | Target | Result |
|---|---|---|---|
| NFR-02 | On isolated staging with fixed synthetic dataset, warm up 5 times then record 30 samples each for product search, checkout, stock and report; record p50/p95/max and errors | TBD approved SRS | NOT RUN |
| NFR-03 | Cashier calls admin endpoint, expired session, missing CSRF token, inspect password hashing implementation, benign quote input in search; verify deny/no mutation/no disclosure | approved policy | NOT RUN |
| NFR-04 | retry checkout/return, concurrent last item, injected failure; reconcile invoice, stock, points, cash | no duplicate or partial commit | historical regression evidence; per-case rerun unverified |
| NFR-05 | inspect actor/action/object/time/result after authorized change | retention TBD | NOT RUN |
| NFR-07 | review ignored config and scan Git history with redacted output | no real secrets | .env guard passed; full scan NOT RUN |
| NFR-06 | restore approved backup into isolated PG17 and run smoke | RPO/RTO TBD | NOT RUN |
Record timestamp, SHA, environment, dataset size, concurrent users, raw milliseconds, HTTP status, expected/actual, evidence and defect issue. Do not treat suite duration as latency. Do not test attacks on shared production. Obtain approved NFR mapping and thresholds before PASS.

## Gate distinction — PR #304 vs system release
- **Merge gate (QA contribution):** reviewed QA/test/workflow diff; verified final PR-head CI (expected 885 tests, zero fail/skip), traceable TRX, isolated-only execution, consistent scope/evidence and independent reviewer approval. Unresolved in-scope defects must be fixed or explicitly accepted by reviewers; do not silently waive fixture isolation evidence. Status: PENDING final CI and review.
- **Release gate (system):** approved SRS/Must mapping, WR-06–10 and TO-BE ACL/RLS, live read-only catalog/runtime verification, #257 freeze implementation and LF tests, browser E2E, NFR/UAT, full-schema restore, schema adoption, HTTPS/staging, release SHA and required sign-offs. Status: HOLD. These outstanding system gates are tracked separately; they do not automatically block merging a correctly scoped QA contribution, nor does QA merge authorize release, migration or issue closure.
- Evidence: [rolling QA report](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/docs/qa-devops-deliverables-20261127/docs/06-testing/test-reports/2026-11-27-qa-regression.md); last verified case-level evidence at `baa30035`: 885/885, 16 named security cases PASS in isolated PG17. Re-run on final head; do not relabel prior evidence as current-head PASS.
