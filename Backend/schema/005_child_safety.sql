-- Child safety (COPPA / Google Play Families, mixed audience): chat mode, report, block. Uses players.age_band (004).
-- chat_mode NULL = default for the band: 13plus -> 'free'; under13 / unknown -> 'off'. Under13 / unknown can never be 'free'.
ALTER TABLE players ADD COLUMN IF NOT EXISTS chat_mode text CHECK (chat_mode IN ('off', 'presets', 'free'));
CREATE TABLE IF NOT EXISTS chat_reports (
  id bigserial PRIMARY KEY,
  reporter_id bigint NOT NULL REFERENCES players(id) ON DELETE CASCADE,
  message_id bigint,                         -- alliance_chat.id (kept even if the message is later deleted)
  reported_player_id bigint REFERENCES players(id) ON DELETE SET NULL,
  body_snapshot text NOT NULL DEFAULT '',    -- what the reporter saw, for moderation
  reason text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now(),
  reviewed boolean NOT NULL DEFAULT false
);
CREATE INDEX IF NOT EXISTS chat_reports_open_idx ON chat_reports(created_at) WHERE NOT reviewed;
CREATE TABLE IF NOT EXISTS chat_blocks (
  blocker_id bigint NOT NULL REFERENCES players(id) ON DELETE CASCADE,
  blocked_id bigint NOT NULL REFERENCES players(id) ON DELETE CASCADE,
  created_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (blocker_id, blocked_id)
);
