-- Phase 3: real-money purchases (Google Play now, App Store later). Additive only: new tables, nothing changed or dropped.
-- Account model: the existing players row (device sign-in -> bearer token, 001_alliance.sql) is the one account that
-- purchases, entitlements and (next) cloud saves hang off. No new auth.
-- COPPA / Play Families: the account is an app-generated id only (device_hash = sha256 of a random id the app made,
-- not a hardware or advertising id). No email, birth date, ad id or device identifiers are stored. The age band is the
-- only age data: under13 | 13plus | unknown (default).
ALTER TABLE players ADD COLUMN IF NOT EXISTS age_band text NOT NULL DEFAULT 'unknown' CHECK (age_band IN ('under13', '13plus', 'unknown'));
CREATE TABLE IF NOT EXISTS iap_purchases (
  id bigserial PRIMARY KEY,
  player_id bigint NOT NULL REFERENCES players(id),           -- account that first redeemed the purchase
  store text NOT NULL CHECK (store IN ('google_play', 'app_store')),
  product_id text NOT NULL,                                    -- store product id (com.paytoplay.fantasykingdoma.*)
  sku text NOT NULL,                                           -- theme-pack sku (sku.*)
  purchase_token text NOT NULL,                                -- Play purchase token / App Store originalTransactionId
  order_id text,                                               -- Play orderId (GPA.*) / App Store transactionId
  consumable boolean NOT NULL,
  state text NOT NULL CHECK (state IN ('pending', 'granted', 'refunded', 'voided', 'cancelled')),
  purchased_at timestamptz,
  store_payload jsonb NOT NULL DEFAULT '{}'::jsonb,            -- state fields of the store's answer only (no PII), for audits
  created_at timestamptz NOT NULL DEFAULT now(),
  updated_at timestamptz NOT NULL DEFAULT now(),
  UNIQUE (store, purchase_token)                               -- idempotency: one token is redeemed exactly once
);
CREATE INDEX IF NOT EXISTS iap_purchases_player_id_idx ON iap_purchases(player_id);
CREATE INDEX IF NOT EXISTS iap_purchases_order_id_idx ON iap_purchases(order_id);

-- What the server granted for a purchase, per account. kind 'restore' = a non-consumable re-granted to another account
-- of the same store user (reinstall / new device). revoked_at is set by a refund or void (RTDN).
CREATE TABLE IF NOT EXISTS iap_grants (
  id bigserial PRIMARY KEY,
  purchase_id bigint NOT NULL REFERENCES iap_purchases(id),
  player_id bigint NOT NULL REFERENCES players(id),
  sku text NOT NULL,
  grants jsonb NOT NULL DEFAULT '{}'::jsonb,                   -- wallet resources, e.g. {"res.hard": 500}
  entitlements text[] NOT NULL DEFAULT '{}',
  kind text NOT NULL DEFAULT 'purchase' CHECK (kind IN ('purchase', 'restore')),
  created_at timestamptz NOT NULL DEFAULT now(),
  revoked_at timestamptz,
  UNIQUE (purchase_id, player_id)
);
CREATE INDEX IF NOT EXISTS iap_grants_player_id_idx ON iap_grants(player_id);

-- Unlock flags per account (reward track columns, crusade banner, temp builder). Cloud saves read these.
CREATE TABLE IF NOT EXISTS player_entitlements (
  player_id bigint NOT NULL REFERENCES players(id),
  entitlement text NOT NULL,
  purchase_id bigint REFERENCES iap_purchases(id),
  granted_at timestamptz NOT NULL DEFAULT now(),
  revoked_at timestamptz,
  PRIMARY KEY (player_id, entitlement)
);

-- Google Play Real-time Developer Notifications (Pub/Sub push), deduplicated by Pub/Sub message id.
CREATE TABLE IF NOT EXISTS iap_rtdn_events (
  message_id text PRIMARY KEY,
  kind text NOT NULL,
  purchase_token text,
  payload jsonb NOT NULL,
  received_at timestamptz NOT NULL DEFAULT now()
);
