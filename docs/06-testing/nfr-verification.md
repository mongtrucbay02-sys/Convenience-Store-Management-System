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
