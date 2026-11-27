-- Review-only equivalent of EF migration 20261127030000_ExchangePlanSnapshots.
-- DO NOT run on shared Supabase before DB review, backup and deployment approval (#283/#293).
-- Use the EF migration as the deployment authority; do not run both paths independently.
BEGIN;
ALTER TABLE smartmart.phieu_doi_tra ADD COLUMN phuong_an_doi jsonb NULL;
-- Existing rows remain NULL: do not infer historical replacement goods.
COMMIT;
