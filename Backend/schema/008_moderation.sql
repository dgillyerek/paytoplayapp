-- 13+ audience: guild chat moderation queue (filter hits + player reports) and chat mutes. Builds on 005_child_safety.
-- chat_reports is the queue: source 'player' (POST /v1/chat/report) or 'filter' (masked / refused by moderation.mjs,
-- reporter NULL). A moderator resolves rows with GET /v1/mod/queue + POST /v1/mod/resolve (MODERATION_TOKEN).
-- Retention of report rows: as counsel directs (Product checklist A3).
ALTER TABLE chat_reports ALTER COLUMN reporter_id DROP NOT NULL;
ALTER TABLE chat_reports ADD COLUMN IF NOT EXISTS source text NOT NULL DEFAULT 'player' CHECK (source IN ('player', 'filter'));
ALTER TABLE chat_reports ADD COLUMN IF NOT EXISTS status text NOT NULL DEFAULT 'open' CHECK (status IN ('open', 'closed'));
ALTER TABLE chat_reports ADD COLUMN IF NOT EXISTS action text CHECK (action IN ('dismiss', 'delete_message', 'mute_24h', 'mute_7d'));
ALTER TABLE chat_reports ADD COLUMN IF NOT EXISTS reviewer_note text;
ALTER TABLE chat_reports ADD COLUMN IF NOT EXISTS reviewed_at timestamptz;
CREATE INDEX IF NOT EXISTS chat_reports_status_idx ON chat_reports(status, created_at);
ALTER TABLE players ADD COLUMN IF NOT EXISTS chat_muted_until timestamptz;
