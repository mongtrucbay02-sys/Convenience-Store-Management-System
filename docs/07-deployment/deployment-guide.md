# Deployment guide — #242
Status: procedure ready; actual HTTPS staging/production NOT VERIFIED.
## Prerequisites
Approved host/domain, .NET 10 runtime, private PostgreSQL 17 backend role, secret manager, verified TLS, release SHA and CI success. See ../db/POSTGRESQL-MIGRATIONS.md and postgresql-publish.md.
## Release
1. Require reviewed PR and green SmartMart CI for exact release SHA; verify TRX and publish dependency guard. Run browser E2E gate separately until automated gate exists.
2. Back up and verify restore per backup-restore.md. Existing Supabase baseline adoption #283 must be approved; never apply initial baseline to existing schema. Application does not auto-migrate; owner applies only reviewed migration plan.
3. Build/publish Release: dotnet publish src/ConvenienceStore.Web -c Release -o artifacts/publish. Run scripts/ci/verify-postgresql-runtime.ps1 on output.
4. Set ConnectionStrings__CuaHang using private role and verified TLS, ASPNETCORE_ENVIRONMENT=Production, ASPNETCORE_URLS=http://127.0.0.1:5292. Keep secrets out of Git and wwwroot. Run dotnet ConvenienceStore.Web.dll under nonprivileged service account/process manager.
5. Configure reverse proxy with valid domain certificate, redirect HTTP to HTTPS, restrict app port to loopback, configure trusted forwarded headers as appropriate and verify secure session cookies. Do not expose database to browsers.
6. Smoke test login, role denial, POS, return, stock and reports on isolated staging; verify certificate, redirects, logs and health. Share URL and demo accounts through approved channels, not public passwords.
7. Rollback application to previous immutable publish/config if failure; database recovery only via owner-approved plan. Record SHA, URL, timestamps, test evidence and reviewer.
## Release gate
CI pass is not deployment. Host, DNS, credentials, schema approval, backup and restore evidence, E2E and UAT are pending. No deployment performed by this document.
