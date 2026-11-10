# Products Code First Verification

## Scope
- Database: cuahang_codefirst_dev (Aiven MySQL 8.4.11)
- Provider: MySql.EntityFrameworkCore 10.0.9
- EF Core: 10.0.13
- Migration: 20261024202156_ProductsSchemaProbe
- Tables: danh_muc, san_pham, lich_su_gia

## Migration Verification
- Three Products tables created successfully.
- Five foreign keys created.
- Two CHECK constraints created.
- Required indexes created.
- Charset: utf8mb4.
- Database collation: utf8mb4_0900_ai_ci.
- EF Core reports no pending model changes.

## SQL Constraint Tests
| Test | Description | Result |
| --- | --- | --- |
| PR-01 | Insert valid category | PASS |
| PR-02 | Duplicate category name (1062) | PASS |
| PR-03 | Invalid product foreign keys (1452) | PASS |
| PR-03B | Invalid category with valid creator (1452) | PASS |
| PR-04 | Invalid creator foreign key (1452) | PASS |
| PR-05 | Zero selling price (3819) | PASS |
| PR-06 | Price not divisible by 100 (3819) | PASS |
| PR-07 | Cancellation timestamp without user (3819) | PASS |
| PR-08 | Valid selling price | PASS |

## EF Core Concurrency Tests
| Test | Description | Result |
| --- | --- | --- |
| EF-CC-02 | Category stale update rejected | PASS |
| EF-CC-03 | Product stale update rejected | PASS |
| EF-CC-04 | Unchanged category retains version | PASS |
| EF-CC-05 | Category update increments version once | PASS |

## Data Cleanup
- Tests executed within transactions.
- Transaction rollback completed.
- All six business tables contained zero records after testing.

## Limitations
- EF concurrency used two DbContexts sharing one transaction.
- Independent concurrent transactions were not tested.
- Price cancellation business rules were not fully tested.
- Tests currently reside in untracked tmp/ directories.
- Only six of the planned 28 tables have been implemented.
- The three views and ten triggers are not yet implemented.
- These migrations are development probes, not the final full-system baseline.
