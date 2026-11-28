# PostgreSQL migrations and operations

Smart Mart has one .NET 10 MVC/Razor application and one `CuaHangDbContext`. Runtime uses Npgsql exclusively, PostgreSQL 17, schema `smartmart`. Runtime SQL uses signed bigint IDs, native booleans and timestamptz. A timestamp without a kind coming from an existing Vietnamese date/time form is interpreted as UTC+07; stored instants and price comparisons use UTC.

## Configuration

Use `ConnectionStrings__CuaHang` in the process environment, an ignored `.env` beside the web project, or User Secrets. `.env.example` contains placeholders only. `Database__Provider` may be omitted or set to `PostgreSql`; any other provider is rejected. POS uses the same connection as the other modules. Use the private `smartmart_backend` database role, with `NOSUPERUSER NOBYPASSRLS`, and verified TLS. Do not use Supabase anon/service API keys as database credentials.

The deployment owner must provision the backend login/password through a secret manager. The repository never creates a password or alters an existing shared Supabase role. ASP.NET session/permission checks enforce individual users; the private backend role has database access through policies explicitly addressed to that role. Supabase `anon`, `authenticated` and `PUBLIC` have no Smart Mart data privileges. This is a private server application, not direct browser access to Supabase tables.

## Baseline for a new, empty database

`Data/Migrations/*_PostgreSqlBaseline.cs` embeds an immutable SQL baseline at `Data/Migrations/Sql/PostgreSqlBaseline.sql`. It creates 31 tables, 89 CHECK constraints, 62 foreign keys, 17 triggers and three invoker-security views. It enables RLS on every business table. If `smartmart_backend` exists, it grants the backend schema/table/sequence privileges and creates role-specific policies. A missing backend role leaves RLS in default-deny mode for non-owners. Provision/verify the role before applying the baseline. EF history lives in `public.__EFMigrationsHistory`.

`CuaHangDesignTimeFactory` uses an inert localhost connection for offline model/snapshot/script generation. It never loads a deployment secret. The application does not run migrations on startup. Baseline rollback is disabled to avoid silently dropping business data. MySQL migration history is retained under `docs/archive/mysql-runtime`; it is outside the application compilation and is never a PostgreSQL migration path.

## Existing Supabase schema: adoption remains a deployment blocker

Do **not** run this initial baseline against the existing shared Supabase schema. Do **not** mark it applied merely because table names match. The deployment owner must first take/verify a backup, compare the full catalog, constraints, indexes, functions, trigger bodies, view security, RLS policies and grants, and agree on baseline adoption/history recording and any incremental differences. That review must use the actual runtime role. No shared migrations, role changes, seed or reset were performed during this integration.

The checked-in catalog contract is a captured reference, not proof that today's shared database or credentials match. Issue #283 M0 remains authoritative for adoption and ownership. Existing data may include case/accent variants that the old MySQL collation treated as equivalent. The application serializes folded category/product-code creation and performs folded duplicate checks; a catalog/data review is required before adding expression unique indexes to the existing database.

## Isolated verification

Set `ConnectionStrings__CuaHangPostgreSqlTest` to a disposable PostgreSQL 17 server on `127.0.0.1`, `localhost` or `::1`. The migration fixture checks version 17 and refuses non-loopback hosts. Each fixture creates its own random `smartmart_test_*` or `smartmart_pgtest_*` database and drops only that database. The privileged fixture connection must never be the release connection. CI provides its own PostgreSQL 17 service and runs the entire suite without MySQL.

```powershell
dotnet restore ConvenienceStoreManagement.slnx
dotnet build ConvenienceStoreManagement.slnx -c Release --no-restore
dotnet test tests/ConvenienceStore.Web.Tests -c Release --no-build --no-restore --logger "trx;LogFileName=tests.trx" --results-directory artifacts/test-results
./scripts/ci/verify-trx.ps1 -Path artifacts/test-results/tests.trx
dotnet publish src/ConvenienceStore.Web -c Release --no-build --no-restore -o artifacts/publish
./scripts/ci/verify-postgresql-runtime.ps1 -PublishPath artifacts/publish
```

Promotion updates/status changes and deletes require the `version` returned by the read response. Razor forms carry it in a hidden field. A missing/stale version is rejected; API clients should reload on HTTP 409. Price requests require an ISO 8601 offset or `Z`, or a blank effective time for immediate application. Document counters derive their date from the stored instant in Vietnamese time.
