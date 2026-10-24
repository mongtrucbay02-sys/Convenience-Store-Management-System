# Identity Code First - Database Verification

## Environment
- Backend: .NET 10 / EF Core 10
- Provider: MySql.EntityFrameworkCore
- MySQL: Aiven MySQL 8.4.11
- Test database: cuahang_codefirst_dev
- Migration: 20261024191653_IdentitySchemaProbe

## Tables
- vai_tro
- tai_khoan
- phien_dang_nhap

## Verification
- Migration applied successfully.
- Storage engine: InnoDB.
- Collation: utf8mb4_0900_ai_ci.
- Constraint tests: 10 passed, 0 failed.
- Rollback completed.
- Post-rollback records: 0 in all three business tables.

## Known Limitations
- IdentitySchemaProbe is not the final 28-table baseline.
- UPDATE timestamp behavior not yet tested.
- Optimistic concurrency behavior not yet tested.
- Authentication and session lifecycle not yet tested.
- EF Core package version conflict observed in temporary inspection project.
- No production/shared database deployment performed.

## Status
Database structure and tested constraints: PASS.
Full Identity module acceptance: PENDING.

## Additional Verification

### Timestamp
- TS-01: PASS.
- MySQL automatically updates `ngay_cap_nhat`.

### SQL Optimistic Concurrency
- CC-01: PASS.
- Version guard rejects an update using an outdated `phien_ban`.
- Verified version transition: 1 -> 2.

### EF Core Optimistic Concurrency
- EF-CC-01a: PASS.
- Context A updates the account and increments version from 1 to 2.
- EF-CC-01b: PASS.
- Context B attempts to save an outdated version.
- EF Core throws `DbUpdateConcurrencyException`.
- Transaction rollback completed.

### Limitations
- EF Core concurrency was tested with two DbContexts sharing one transaction.
- Independent concurrent transactions have not been tested.
- Direct SQL and ExecuteUpdate must manage `phien_ban` separately.
- Post-test record counts should be verified.
- The migration is still an Identity-only probe, not the final baseline.
