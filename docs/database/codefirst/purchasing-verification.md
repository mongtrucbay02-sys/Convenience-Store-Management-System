# Purchasing Code First Verification

## 1. Environment

- Database: `cuahang_codefirst_dev`
- Platform: Aiven MySQL
- EF Core: 10.0.13
- Provider: `MySql.EntityFrameworkCore`
- Migration: `20261026042627_PurchasingSchemaProbe`
- Migration status: Applied
- Reference: `docs/database/reference/schema_v1.sql`

## 2. Implemented Tables

1. `nha_cung_cap`
2. `phieu_nhap`
3. `ct_phieu_nhap`

### Schema Verification

- Three Purchasing tables created successfully.
- Six foreign keys verified.
- Four CHECK constraints verified.
- Three UNIQUE constraints verified.
- Composite primary key verified on `ct_phieu_nhap`.
- Required indexes verified.
- `thanh_tien` verified as `STORED GENERATED`.
- EF Core reports no pending model changes.

## 3. SQL Constraint Test Results

| ID | Test | Expected | Result |
| --- | --- | --- | --- |
| NH-01 | Insert valid supplier | Success | PASS |
| NH-02 | Duplicate supplier code | MySQL 1062 | PASS |
| NH-03 | Duplicate supplier phone | MySQL 1062 | PASS |
| NH-04 | Invalid supplier phone | MySQL 3819 | PASS |
| NH-05 | Invalid supplier foreign key | MySQL 1452 | PASS |
| NH-06 | Received receipt without confirmation | MySQL 3819 | PASS |
| NH-07 | Cancelled receipt without required details | MySQL 3819 | PASS |
| NH-08 | Receipt item with zero quantity | MySQL 3819 | PASS |
| NH-09 | Duplicate product in same receipt | MySQL 1062 | PASS |
| NH-10 | Stored total: 3 x 12500 | 37500 | PASS |

**Result: 10/10 PASS**

## 4. Data Cleanup

All test fixtures were executed inside transactions and rolled back.

Post-test read-only verification:

- `vai_tro`: 0
- `tai_khoan`: 0
- `phien_dang_nhap`: 0
- `danh_muc`: 0
- `san_pham`: 0
- `lich_su_gia`: 0
- `nha_cung_cap`: 0
- `phieu_nhap`: 0
- `ct_phieu_nhap`: 0

No remaining test records were observed.

## 5. Migration Incident and Resolution

The MySQL provider generated the `thanh_tien` computed
column without the `STORED` keyword.

The initial attempt to convert the column with
`MODIFY COLUMN` failed because MySQL does not support
changing generated-column storage status in that way.

Three empty Purchasing tables remained after the
failed migration.

The tables were verified empty, confirmed to have
no incoming foreign keys from outside Purchasing,
and cleaned up in the development database.

The pending migration was corrected to drop and
re-add the computed column as `STORED`.

The corrected migration was then applied successfully
and verified through `INFORMATION_SCHEMA`.

## 6. Limitations

- SQL constraints were tested; not every business rule was tested.
- Receipt state-transition rules are not fully verified.
- Receipt-total synchronization is not verified.
- Inventory increase and stock valuation are not verified.
- Independent transaction concurrency is not verified.
- EF Core concurrency behavior for Purchasing is not yet tested.
- Integration test implementations currently reside in untracked `tmp/`.
- Only 9 of the planned 28 tables have been implemented.
- Views and triggers are not yet implemented.
- Development migration probes are not the final full-system baseline.

## 7. Conclusion

Purchasing database schema and the ten executed SQL
constraint tests meet the verified acceptance criteria.

Further application-service, EF concurrency, inventory
and end-to-end workflow verification remains required.
