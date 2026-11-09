-- In-app / web account deletion (Play Data safety + COPPA / Families). The players row is scrubbed, not dropped, so
-- store receipt records (iap_purchases / iap_grants, no PII) keep a valid owner for refunds and tax. Counsel to confirm
-- the retention period for those receipt rows.
ALTER TABLE players ADD COLUMN IF NOT EXISTS deleted_at timestamptz;
