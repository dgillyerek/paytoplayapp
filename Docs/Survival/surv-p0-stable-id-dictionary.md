# SURV-P0 — Stable ID Dictionary v0

| Field | Value |
|-------|--------|
| **Status** | SHIPPED (Product 2026-09-16) |
| **Product** | PayToPlay white-label survival · Theme A fantasy first flavor |
| **Companion** | `skinnable-survival-theme-pack-spec.md` v1.1 |
| **Design SoT** | `/workspace/design/survival-theme-a-fantasy/` (splash + Play mock + `play-mock/world-nodes/`) |
| **Rule** | Logic uses **stable IDs** only. Theme packs bind display strings/art. **NOT** Grove. **NOT** player skins. |

Machine-readable twin: `surv-p0-stable-id-dictionary.json`

---

## 0. Naming conventions

| Pattern | Use |
|---------|-----|
| `sys.*` | Systems / modules |
| `building.*` | Base buildings |
| `research.*` | Tech nodes |
| `hero.*` / `hero.archetype.*` | Heroes |
| `alliance.*` | Alliance / guild systems |
| `world.*` / `world.node_type.*` / `world.node.*` | Crusade map |
| `battle.*` | Combat |
| `res.*` | Resources / currencies |
| `energy.*` | Energy actions / meters |
| `event.*` | Live events |
| `iap.*` / `sku.*` | Monetization |
| `ftue.*` | Tutorial steps |
| `theme.*` / `flavor.*` / `pack.*` | White-label build keys |
| `theme_a.*` | Theme A **content keys** (strings/art bindings only) |

IDs are `snake_case`, lowercase, ASCII. Display names live in theme pack strings — never hardcode Theme A copy in systems.

---

## 1. Systems modules

| Stable ID | Purpose |
|-----------|---------|
| `sys.base` | Base / build queues |
| `sys.research` | Research tree |
| `sys.heroes` | Hero roster / progression |
| `sys.alliance` | Alliance (Theme A: Guild) |
| `sys.world` | World / crusade map |
| `sys.battles` | Battles |
| `sys.energy` | Energy |
| `sys.events` | Events |
| `sys.iap` | IAP / wallets |
| `sys.ftue` | FTUE state machine |
| `sys.meta` | Account / settings / analytics shell |

---

## 2. Resources / currencies

| Stable ID | Kind | Notes |
|-----------|------|--------|
| `res.energy` | Session gate | Cap + regen in shared economy (SURV-P2) |
| `res.soft` | Soft currency | Theme A label via pack (e.g. coin/gold) |
| `res.hard` | Premium currency | Gems-class; IAP sink |
| `res.speedup_m` | Consumable | General speed-up (minutes) |
| `res.hero_xp` | Progression | Hero leveling |
| `res.alliance_gift` | Social | Guild gift token |
| `res.event_score` | Event | Per-event score currency (instance scoped) |

---

## 3. Base / buildings

| Stable ID | Role |
|-----------|------|
| `building.hq` | HQ / keep |
| `building.barracks` | Troop / muster |
| `building.economy` | Soft income |
| `building.research` | Unlocks research UI |
| `building.hero_hall` | Hero management |
| `building.wall` | Defense / wall |
| `building.alliance` | Alliance hub building |
| `building.warehouse` | Storage cap (optional v1) |
| `building.workshop` | Craft / speed helpers (optional v1) |

**Queue:** `base.queue.build` · `base.queue.upgrade`  
**Actions:** `base.action.place` · `base.action.upgrade` · `base.action.collect`

---

## 4. Research

| Stable ID | Branch |
|-----------|--------|
| `research.offense_1` … `research.offense_n` | Offense |
| `research.defense_1` … | Defense |
| `research.economy_1` … | Economy |
| `research.march_1` … | World march |
| `research.hero_1` … | Hero power |

Tree edges are data (`requires: []`) — not hardcoded per theme.

---

## 5. Heroes

| Stable ID | Role |
|-----------|------|
| `hero.slot_01` … `hero.slot_n` | Roster slots |
| `hero.archetype.tank` | Kit family |
| `hero.archetype.dps` | |
| `hero.archetype.support` | |
| `hero.archetype.ranged` | |
| `hero.rarity.common` / `rare` / `epic` / `legendary` | Rarity tiers |
| `hero.stat.atk` · `hero.stat.hp` · `hero.stat.def` · `hero.stat.power` | Stats |

Theme A binds knight portrait etc. to a concrete hero content key (see §10) → archetype + rarity.

---

## 6. Alliance (Theme A display: Guild)

| Stable ID | Purpose |
|-----------|---------|
| `alliance.org` | Org entity |
| `alliance.rank.leader` | Ranks |
| `alliance.rank.officer` | |
| `alliance.rank.member` | |
| `alliance.action.create` · `join` · `leave` · `kick` | |
| `alliance.chat` | |
| `alliance.gift` | |
| `alliance.war` | War/raid hook (stub OK in slice) |

---

## 7. World map — node **types** (align Design SoT)

Design placeable roles (`play-mock/world-nodes/MANIFEST.md`) → stable types:

| Stable ID | Design role | Theme A art stub (binding only) |
|-----------|-------------|----------------------------------|
| `world.node_type.home` | HOME | `01_home_cottage` |
| `world.node_type.gather` | GATHER | `02_gather_quarry` |
| `world.node_type.build` | BUILD | `03_build_outpost` |
| `world.node_type.guild` | GUILD | `04_guild_fort` |
| `world.node_type.fight` | FIGHT | `05_fight_dark_keep` |
| `world.node_type.explore` | EXPLORE | `06_explore_ruins` |

**Map systems:**

| Stable ID | Purpose |
|-----------|---------|
| `world.map.campaign` | Primary crusade map |
| `world.action.march` | Move squad to node |
| `world.action.scout` | Reveal / explore |
| `world.action.gather` | Gather at gather nodes |
| `world.fog` | Fog / unlock |

**Instance nodes** (examples for slice — IDs fixed, positions from Design pins):

| Stable ID | Type |
|-----------|------|
| `world.node.home_01` | `world.node_type.home` |
| `world.node.gather_01` | `world.node_type.gather` |
| `world.node.build_01` | `world.node_type.build` |
| `world.node.guild_01` | `world.node_type.guild` |
| `world.node.fight_01` | `world.node_type.fight` |
| `world.node.explore_01` | `world.node_type.explore` |

Do not redesign look — pin/layout follow Design Play mock.

---

## 8. Battles

| Stable ID | Purpose |
|-----------|---------|
| `battle.mode.pve` | PvE |
| `battle.mode.pvp` | PvP stub |
| `battle.formation.front` · `mid` · `back` | Rows |
| `battle.result.win` · `lose` · `flee` | Outcomes |
| `battle.action.start` · `auto` · `skip` | |

---

## 9. Energy / events / IAP / FTUE

### Energy
| ID | Purpose |
|----|---------|
| `energy.meter.main` | Primary energy |
| `energy.action.march` · `battle` · `gather` · `build_rush` | Spend sinks |

### Events
| ID | Purpose |
|----|---------|
| `event.type.siege` | Kingdom siege frame |
| `event.type.guild_crusade` | Alliance event |
| `event.type.banner` | Limited banner |
| `event.instance.*` | Runtime instances |

### IAP SKU classes (theme-agnostic)
| ID | Purpose |
|----|---------|
| `sku.energy_s` · `energy_m` · `energy_l` | Energy packs |
| `sku.hard_s` · `hard_m` · `hard_l` · `hard_xl` | Hard currency |
| `sku.speedup_bundle` | Speed-ups |
| `sku.starter_pack` | One-time starter |
| `sku.pass_season` | Battle pass–class |

Billing: StoreKit + Play only. Acceleration only — no paywall on HQ story.

### FTUE steps (systems)
| ID | Purpose |
|----|---------|
| `ftue.step.boot` | |
| `ftue.step.place_hq` | |
| `ftue.step.first_build` | |
| `ftue.step.first_march` | |
| `ftue.step.first_battle` | |
| `ftue.step.alliance_intro` | |
| `ftue.flag.complete` | |

---

## 10. Theme A content keys (bindings — not systems)

| Content key | Binds to | Design SoT |
|-------------|---------|------------|
| `theme_a.building.hq.keep` | `building.hq` | direction-5 keep |
| `theme_a.hero.knight_01` | `hero.slot_01` + `hero.archetype.tank` | hero knight portrait |
| `theme_a.threat.darkness_01` | battle PvE enemy kit | darkness monster |
| `theme_a.world.chip.dark_keep` | `world.node_type.fight` chip | map chip |
| `theme_a.store.icon` | AppFlavor icon | store icon v3 |
| `theme_a.alliance.label` | `alliance.*` strings | “Guild” |
| `theme_a.map.label` | `world.map.campaign` | “Crusade Map” |
| `theme_a.node.home_cottage` | `world.node_type.home` | `01_home_cottage` |
| `theme_a.node.gather_quarry` | `world.node_type.gather` | `02_gather_quarry` |
| `theme_a.node.build_outpost` | `world.node_type.build` | `03_build_outpost` |
| `theme_a.node.guild_fort` | `world.node_type.guild` | `04_guild_fort` |
| `theme_a.node.fight_dark_keep` | `world.node_type.fight` | `05_fight_dark_keep` |
| `theme_a.node.explore_ruins` | `world.node_type.explore` | `06_explore_ruins` |

Palette (Design lock): stone + royal blue/gold player · black/purple threat — presentation only.

---

## 11. Build flavor / theme-pack keys

| Stable ID | Purpose |
|-----------|---------|
| `theme.id.fantasy_kingdom_a` | Theme A pack id |
| `flavor.id.fantasy_kingdom_a` | AppFlavor / store app |
| `pack.path` | Build-time path to baked ThemePack |
| `pack.systems_api` | Must equal `survival_core_v1` |
| `survival_core.api` | `survival_core_v1` |

**AppFlavor build pins:** `flavor.bundle_id` · `flavor.app_name` · `flavor.icon` · `flavor.splash` · `flavor.theme_id`

Later themes get new `theme.id.*` / `flavor.id.*` — same `survival_core.api`.

---

## 12. What Dev can scaffold now

1. Enums / ScriptableObjects / JSON tables keyed by these IDs  
2. ThemePack binder: `theme_a.*` → art/string refs under `ThemePack/fantasy_kingdom_a/`  
3. World map spawner: six `world.node_type.*` + instance nodes; art from Design `world-nodes/`  
4. AppFlavor define symbols / Addressables group `fantasy_kingdom_a`  
5. **Do not** implement Grove merge IDs or runtime theme switcher  

**Out of SURV-P0:** balance numbers (SURV-P2), full FTUE copy, vertical slice scope detail (SURV-P3).

---

**— Product**
