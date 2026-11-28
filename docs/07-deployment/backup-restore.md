# PostgreSQL 17 backup and restore runbook — #101 #181 #264
Status: procedure only; no actual dump or restore executed. Owner approval required. Use PostgreSQL tools, not mysqldump.
1. Confirm approved source, backup window, access role, schema scope, retention, encryption and storage. Record UTC timestamp, source version and release SHA. Use secret manager or protected pgpass; never put credentials in command history or Git.
2. With PostgreSQL 17 client and approved source service configured, run:
```sh
umask 077
stamp=$(date -u +%Y%m%dT%H%M%SZ)
file="smartmart-${stamp}.dump"
pg_dump --dbname="service=smartmart_backup_source" --format=custom --file="$file"
pg_restore --list "$file" > "$file.list"
sha256sum "$file" > "$file.sha256"
```
3. Encrypt and store in approved restricted storage; share only evidence/hash, never customer dump in public repo. Record size and checksum. Roles/global objects need separate owner-managed inventory; managed Supabase objects may require provider-specific backup plan.
4. Restore only to a new isolated PostgreSQL 17 database after verifying host/database identity and provisioning approved roles. Example with local disposable service:
```sh
sha256sum -c "$file.sha256"
pg_restore --exit-on-error --no-owner --dbname="service=smartmart_restore_isolated" "$file"
```
Do not use --clean or restore into shared Supabase. Check grants/RLS separately because ownership differs. Verify tables, constraints, triggers, views, row counts, invoice/stock totals and smoke with backend role. Record duration and compare approved RPO/RTO. Destroy only the disposable DB after approval.
5. Evidence: backup timestamp, checksum, restore target (redacted), log, reconciliation, reviewer. A dump alone is not a verified backup.

Evidence status: isolated synthetic-table backup/restore PASS at baa30035 (run 46160034432); full SmartMart schema and demo restore NOT VERIFIED. Do not promote the synthetic rehearsal to a production recovery claim.

## Gate distinction — PR #304 vs system release
- **Merge gate (QA contribution):** reviewed QA/test/workflow diff; verified final PR-head CI (expected 885 tests, zero fail/skip), traceable TRX, isolated-only execution, consistent scope/evidence and independent reviewer approval. Unresolved in-scope defects must be fixed or explicitly accepted by reviewers; do not silently waive fixture isolation evidence. Status: PENDING final CI and review.
- **Release gate (system):** approved SRS/Must mapping, WR-06–10 and TO-BE ACL/RLS, live read-only catalog/runtime verification, #257 freeze implementation and LF tests, browser E2E, NFR/UAT, full-schema restore, schema adoption, HTTPS/staging, release SHA and required sign-offs. Status: HOLD. These outstanding system gates are tracked separately; they do not automatically block merging a correctly scoped QA contribution, nor does QA merge authorize release, migration or issue closure.
- Evidence: [rolling QA report](https://github.com/mongtrucbay02-sys/Convenience-Store-Management-System/blob/docs/qa-devops-deliverables-20261127/docs/06-testing/test-reports/2026-11-27-qa-regression.md); last verified case-level evidence at `baa30035`: 885/885, 16 named security cases PASS in isolated PG17. Re-run on final head; do not relabel prior evidence as current-head PASS.
