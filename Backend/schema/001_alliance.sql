-- Phase 1: players, alliances, chat, Guild Help, gifts, rate limits. Applied to Neon project paytoplay-survival.
CREATE TABLE IF NOT EXISTS players (
  id bigserial PRIMARY KEY,
  device_hash text NOT NULL UNIQUE,          -- sha256("dev:" + device id); the raw id never reaches the DB
  token_hash text NOT NULL UNIQUE,           -- sha256(bearer token)
  display_name text NOT NULL,
  power integer NOT NULL DEFAULT 0,
  castle_x integer NOT NULL DEFAULT 0,
  castle_y integer NOT NULL DEFAULT 0,
  created_at timestamptz NOT NULL DEFAULT now(),
  last_seen timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS alliances (
  id bigserial PRIMARY KEY,
  name text NOT NULL UNIQUE,
  tag text NOT NULL UNIQUE,
  banner text NOT NULL DEFAULT 'banner_01',
  leader_id bigint NOT NULL REFERENCES players(id),
  is_open boolean NOT NULL DEFAULT true,
  max_members integer NOT NULL DEFAULT 50,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE TABLE IF NOT EXISTS alliance_members (
  player_id bigint PRIMARY KEY REFERENCES players(id),
  alliance_id bigint NOT NULL REFERENCES alliances(id) ON DELETE CASCADE,
  role smallint NOT NULL DEFAULT 1 CHECK (role BETWEEN 1 AND 5),   -- R5 leader, R4 officer
  joined_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS alliance_members_alliance_id_idx ON alliance_members(alliance_id);
CREATE TABLE IF NOT EXISTS alliance_chat (
  id bigserial PRIMARY KEY,
  alliance_id bigint NOT NULL REFERENCES alliances(id) ON DELETE CASCADE,
  player_id bigint NOT NULL REFERENCES players(id),
  body text NOT NULL,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS alliance_chat_alliance_id_id_idx ON alliance_chat(alliance_id, id);
CREATE TABLE IF NOT EXISTS help_requests (
  id bigserial PRIMARY KEY,
  alliance_id bigint NOT NULL REFERENCES alliances(id) ON DELETE CASCADE,
  player_id bigint NOT NULL REFERENCES players(id),
  task_key text NOT NULL,
  max_helps integer NOT NULL DEFAULT 10,
  seconds_per_help integer NOT NULL DEFAULT 60,
  helps integer NOT NULL DEFAULT 0,
  seconds_saved integer NOT NULL DEFAULT 0,
  open boolean NOT NULL DEFAULT true,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS help_requests_alliance_id_idx ON help_requests(alliance_id) WHERE open;
CREATE TABLE IF NOT EXISTS help_given (
  request_id bigint NOT NULL REFERENCES help_requests(id) ON DELETE CASCADE,
  helper_id bigint NOT NULL REFERENCES players(id),
  created_at timestamptz NOT NULL DEFAULT now(),
  PRIMARY KEY (request_id, helper_id)
);
CREATE TABLE IF NOT EXISTS alliance_gifts (
  id bigserial PRIMARY KEY,
  alliance_id bigint NOT NULL REFERENCES alliances(id) ON DELETE CASCADE,
  recipient_id bigint NOT NULL REFERENCES players(id),
  source text NOT NULL,
  gems integer NOT NULL DEFAULT 0,
  claimed boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS alliance_gifts_recipient_id_idx ON alliance_gifts(recipient_id) WHERE NOT claimed;
CREATE TABLE IF NOT EXISTS rate_hits (
  player_id bigint NOT NULL,
  bucket text NOT NULL,
  at timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX IF NOT EXISTS rate_hits_player_id_bucket_at_idx ON rate_hits(player_id, bucket, at);
