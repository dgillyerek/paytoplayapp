-- Phase 2b: the server owns troop counts and stored resources. Additive only: new columns with defaults, nothing dropped.
ALTER TABLE players ADD COLUMN IF NOT EXISTS troops_seeded boolean NOT NULL DEFAULT false;   -- client may seed troops once, then the server owns them
ALTER TABLE players ADD COLUMN IF NOT EXISTS troops_at timestamptz NOT NULL DEFAULT now();    -- last time wounded troops healed
ALTER TABLE players ADD COLUMN IF NOT EXISTS stock jsonb NOT NULL DEFAULT '{}'::jsonb;        -- stored food/wood/stone/gold that PvP can loot
ALTER TABLE marches ADD COLUMN IF NOT EXISTS left_home boolean NOT NULL DEFAULT false;        -- troops were taken out of the castle at send
