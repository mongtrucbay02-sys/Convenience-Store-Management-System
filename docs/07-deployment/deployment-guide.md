# Deployment guide — #242
Status: procedure ready; actual HTTPS staging/production NOT VERIFIED.
## Prerequisites
Approved host/domain, .NET 10 runtime, private PostgreSQL 17 backend role, secret manager, verified TLS, release SHA and CI success. See ../db/POSTGRESQL-MIGRATIONS.md and postgresql-publish.md.
## Release
1. Require reviewed release candidate and green SmartMart CI for exact release SHA; verify TRX and publish dependency guard. Run browser E2E gate separately until automated gate exists.
2. Back up and verify restore per backup-restore.md. Existing Supabase baseline adoption #283 must be approved; never apply initial baseline to existing schema. Application does not auto-migrate; owner applies only reviewed migration plan.
3. Build/publish Release: dotnet publish src/ConvenienceStore.Web -c Release -o artifacts/publish. Run scripts/ci/verify-postgresql-runtime.ps1 on output.
4. Set ConnectionStrings__CuaHang using private role and verified TLS, ASPNETCORE_ENVIRONMENT=Production, ASPNETCORE_URLS=http://127.0.0.1:5292. Keep secrets out of Git and wwwroot. Run dotnet ConvenienceStore.Web.dll under nonprivileged service account/process manager.
5. Configure reverse proxy with valid domain certificate, redirect HTTP to HTTPS, restrict app port to loopback, configure trusted forwarded headers as appropriate and verify secure session cookies. Do not expose database to browsers.
6. Smoke test login, role denial, POS, return, stock and reports on isolated staging; verify certificate, redirects, logs and health. Share URL and demo accounts through approved channels, not public passwords.
7. Rollback application to previous immutable publish/config if failure; database recovery only via owner-approved plan. Record SHA, URL, timestamps, test evidence and reviewer.
## System release gate
CI pass is not deployment. Host, DNS, credentials, schema approval, backup and restore evidence, E2E and UAT are pending. No deployment performed by this document.

## Gate distinction — PR #304 vs system release
- **Merge gate (QA contribution):** reviewed QA/test/workflow diff; verified final PR-head CI (expected 885 tests, zero fail/skip), traceable TRX, isolated-only execution, consistent scope/evidence and independent reviewer approval. Unresolved in-scope defects must be fixed or explicitly accepted by reviewers; do not silently waive fixture isolation evidence. Status: PENDING final CI and review.
- **Release gate (system):** approved SRS/Must mapping, WR-06–10 and TO-BE ACL/RLS, live read-only catalog/runtime verification, #257 freeze implementation and LF tests, browser E2E, NFR/UAT, full-schema restore, schema adoption, HTTPS/staging, release SHA and required sign-offs. Status: HOLD. These outstanding system gates are tracked separately; they do not automatically block merging a correctly scoped QA contribution, nor does QA merge authorize release, migration or issue closure.
- Evidence: [rolling QA report](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/docs/qa-devops-deliverables-20261127/docs/06-testing/test-reports/2026-11-27-qa-regression.md); last verified case-level evidence at `baa30035`: 885/885, 16 named security cases PASS in isolated PG17. Re-run on final head; do not relabel prior evidence as current-head PASS.
