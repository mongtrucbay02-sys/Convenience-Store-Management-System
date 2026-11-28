# QA acceptance status — 2026-11-28

Target: develop via PR #304. No shared Supabase writes or credentials.

| Issue | Evidence | Remaining gate |
|---|---|---|
| #76 | ADR-003 accepted PostgreSQL 17/Supabase; actual runtime ASP.NET Core MVC/Razor .NET 10, EF Core/Npgsql, custom AUTH, GitHub Actions. ADR-001 historical React/MySQL text is not current runtime. | Team acknowledgement |
| #44 | DB schema and secret guard passed on ae6bd91; existing HTTP role and CSRF tests. | Historical secret audit and deployed TLS/config evidence; do not expose credentials |
| #103 | PostgreSQL runtime fixture creates synthetic product, stock, price, cashier and cash opening balance. | Full demo seed and BC-02/03/06 reconciliation with owner |
| #244 | Added HTTP checkout-to-receipt regression on isolated PG17. | CI result and actual browser automation (HTTP is not browser E2E) |
| #264 | 877 passed on prior PR run 46029008105; all three workflows passed on ae6bd91. Synthetic backup rehearsal run 46154321905. | Frozen SHA, official Must mapping, NFR measurements, real-schema backup rehearsal, independent review |
| #242 | Main ruleset 25212210 active, one approval, deletion/non-fast-forward blocked; no required status checks in that ruleset. | Owner configures required checks; clean deployment, migration adoption and HTTPS evidence |

## #257 loyalty scope acceptance
Scope source: [#257 decision](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257#issuecomment-7241048563), [lead agreement](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/issues/257#issuecomment-7241091898). Scope agreed; implementation and acceptance pending.
| Scope | Status | Remaining gate |
|---|---|---|
| KH-06 | DISABLED / FREEZE — implementation NOT VERIFIED | LF-01/02/05/06 exact-role tests |
| KH-07 | CUT | LF-04, final SRS/UI trace |
| KH-08 | LOCKED / FREEZE — implementation NOT VERIFIED | LF-03 direct endpoint denial |
| KH-01–05, BC-10, KM | KEEP | LF-07 historical data and promotion regression |
| NH-09, BC-09–11, KM-05/06; DT-03/06 | KEEP first six; CUT DT-03/06 | Final SRS mapping and retained flows |
| Data / rollout | NOT VERIFIED | LF-08, D5 review, backup/restore and approval |
Historical security probes on lich_su_diem do not prove the freeze is implemented. No PASS for active CUT/DISABLED functionality.

Shared Supabase is read-only for any future authorized verification; no migration, seed, load test or checkout against it. Connection details must be configured by owner outside Git. Online deployment is not verified.
