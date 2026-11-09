-- Phase: cloud saves by account (workstream 3). Additive only. Stacks on 004_purchases + kids 005/006. Numbered 007 after child_safety (005) and account_deletion (006).
-- COPPA / Play Families: the blob holds game progress only (wallet, village, quest, FTUE, campaign). No name, email,
-- birth date, ad id or device id. The row is keyed by players.id (app-made random account). Under-13 accounts need
-- parental consent before a cloud write; cloud_save_consent_at records that the consent hook passed (no PII).

ALTER TABLE players ADD COLUMN IF NOT EXISTS cloud_save_consent_at timestamptz;

CREATE TABLE IF NOT EXISTS player_saves (
  player_id bigint PRIMARY KEY REFERENCES players(id) ON DELETE CASCADE,
  version bigint NOT NULL DEFAULT 0 CHECK (version >= 0),
  blob jsonb NOT NULL DEFAULT '{}'::jsonb,
  updated_at timestamptz NOT NULL DEFAULT now()
);

-- Refund / void clawback against the saved wallet (RTDN → subtract grants, floor 0). Idempotent per purchase_id.
CREATE TABLE IF NOT EXISTS save_clawbacks (
  id bigserial PRIMARY KEY,
  player_id bigint NOT NULL REFERENCES players(id) ON DELETE CASCADE,
  purchase_id bigint NOT NULL REFERENCES iap_purchases(id),
  grants jsonb NOT NULL DEFAULT '{}'::jsonb,
  applied_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (purchase_id)
);
CREATE INDEX IF NOT EXISTS save_clawbacks_player_id_idx ON save_clawbacks(player_id);
