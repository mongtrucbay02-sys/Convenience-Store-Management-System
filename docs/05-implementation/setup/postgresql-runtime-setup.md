# PostgreSQL-only runtime setup

Current integration contract (2026-11-24): one .NET 10 MVC/Razor application, one EF Core/Npgsql context, PostgreSQL 17, schema `smartmart`. See [configuration, baseline adoption and safety](../../db/POSTGRESQL-MIGRATIONS.md) and [current verification report](../../06-testing/test-reports/2026-11-24-postgresql-integration.md).

Use `src/ConvenienceStore.Web/.env.example` or User Secrets. The runtime role is `smartmart_backend`, NOSUPERUSER/NOBYPASSRLS. Store its password outside Git and use verified TLS. POS shares `ConnectionStrings__CuaHang` with every other module. No provider fallback exists.

The initial PostgreSQL baseline is for a new empty database. Do not apply it to the existing Supabase schema; follow the adoption gate in the migration document. The application never runs migrations automatically. Existing roles, policies, permissions, data and backup must be reviewed before deployment.

```powershell
dotnet run --project src/ConvenienceStore.Web --no-launch-profile
```

Routes remain `/login.html`, `/Dashboard`, `/Products`, `/Categories`, `/Customers`, `/NhaCungCap`, `/NhapHang`, `/Kho`, `/Kho/ChungTu`, `/CashLedger`, `/Promotions/Promotions` and `/Reports/Reports/Dashboard`. The cashier uses `/Pos`. Existing role restrictions and CSRF remain enforced. A forced-password session must change its password before business routes are accessible.

All write tests require a disposable loopback PostgreSQL 17 connection through `ConnectionStrings__CuaHangPostgreSqlTest`. No MySQL test connection is needed. CI uses a PostgreSQL 17 service, runs the entire suite, validates the TRX and verifies the release dependency graph. Shared Supabase is never a fixture target.

The dated transitional setup is preserved in [archive](../../archive/postgresql-transition-20261123/postgresql-runtime-setup.md).
