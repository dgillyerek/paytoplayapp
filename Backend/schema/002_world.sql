-- Phase 2: shared world map. Marches are settled lazily by the API when anyone reads the world (no tick server).
ALTER TABLE players ADD COLUMN IF NOT EXISTS placed boolean NOT NULL DEFAULT false;
ALTER TABLE players ADD COLUMN IF NOT EXISTS troops integer NOT NULL DEFAULT 1000;      -- garrison, synced from the client
ALTER TABLE players ADD COLUMN IF NOT EXISTS hero_power integer NOT NULL DEFAULT 0;
ALTER TABLE players ADD COLUMN IF NOT EXISTS shield_until timestamptz NOT NULL DEFAULT now() + interval '24 hours';
CREATE INDEX IF NOT EXISTS players_castle_idx ON players(castle_x, castle_y) WHERE placed;

CREATE TABLE IF NOT EXISTS world_nodes (
  id bigserial PRIMARY KEY,
  x integer NOT NULL, y integer NOT NULL,
  kind text NOT NULL CHECK (kind IN ('food','wood','stone','camp')),
  level smallint NOT NULL DEFAULT 1 CHECK (level BETWEEN 1 AND 5),
  amount integer NOT NULL DEFAULT 0,               -- resources left (camps: 0)
  occupier_march_id bigint,                        -- one gatherer at a time
  respawn_at timestamptz,                          -- hidden until then (beaten camp / emptied node)
  UNIQUE (x, y)
);
CREATE INDEX IF NOT EXISTS world_nodes_xy_idx ON world_nodes(x, y);

CREATE TABLE IF NOT EXISTS marches (
  id bigserial PRIMARY KEY,
  player_id bigint NOT NULL REFERENCES players(id),
  kind text NOT NULL CHECK (kind IN ('gather','camp','attack')),
  from_x integer NOT NULL, from_y integer NOT NULL,
  to_x integer NOT NULL, to_y integer NOT NULL,
  node_id bigint REFERENCES world_nodes(id),
  target_player_id bigint REFERENCES players(id),
  troops integer NOT NULL, hero_power integer NOT NULL DEFAULT 0,
  state text NOT NULL DEFAULT 'marching' CHECK (state IN ('marching','gathering','returning','done')),
  start_at timestamptz NOT NULL DEFAULT now(),
  arrive_at timestamptz NOT NULL,
  gather_until timestamptz,
  return_start timestamptz,
  return_at timestamptz,
  carried jsonb NOT NULL DEFAULT '{}'::jsonb
);
CREATE INDEX IF NOT EXISTS marches_live_idx ON marches(state) WHERE state <> 'done';
CREATE INDEX IF NOT EXISTS marches_player_idx ON marches(player_id) WHERE state <> 'done';

CREATE TABLE IF NOT EXISTS battle_reports (
  id bigserial PRIMARY KEY,
  player_id bigint NOT NULL REFERENCES players(id),   -- who sees this report
  march_id bigint REFERENCES marches(id),
  kind text NOT NULL,          -- attack_won, attack_lost, defend_won, defend_lost, camp_won, camp_lost, gather_done, gather_contested, shielded, bounced
  opponent_id bigint REFERENCES players(id),
  x integer NOT NULL, y integer NOT NULL,
  my_troops integer NOT NULL DEFAULT 0, my_loss integer NOT NULL DEFAULT 0,
  their_troops integer NOT NULL DEFAULT 0, their_loss integer NOT NULL DEFAULT 0,
  loot jsonb NOT NULL DEFAULT '{}'::jsonb,
  claimed boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS battle_reports_player_idx ON battle_reports(player_id, id);
