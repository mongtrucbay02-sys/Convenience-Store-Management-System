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

Shared Supabase is read-only for any future authorized verification; no migration, seed, load test or checkout against it. Connection details must be configured by owner outside Git. Online deployment is not verified.
