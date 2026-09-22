# Skinnable Survival — Theme Pack Spec v1.1

| Field | Value |
|-------|--------|
| **Status** | LOCKED (Derek / CEO 2026-09-15) — **white-label multi-app** |
| **Product line** | Skinnable survival core — **separate** from Project Grove (merge) |
| **Systems inspiration** | Z Survival–class: base queues, research, heroes, alliance, world map, battles, energy, events, IAP acceleration |
| **Theme A** | Fantasy kingdom defense — **first store app** |
| **Owner** | Product (this spec) · Design (per-app theme packs) · Dev (one shared codebase + build flavors) |

---

## 1. Player / business outcome

PayToPlay ships **multiple store apps** that share one Unity survival systems codebase. Each app is a **baked theme** (fantasy, later animal/farm/space, etc.): own bundle ID, icons, listing, and theme pack. Players do **not** pick skins in-app. Studio ships a feature once in shared code, then rolls builds to each theme app.

Monetization: generous free path + IAP acceleration only (energy, speed-ups, hero/progress boosts, pass-class). Payments: StoreKit + Google Play Billing.

---

## 2. Correct model: studio white-label (NOT in-app skins)

### 2.1 What this is

| | |
|--|--|
| **One** shared Unity systems project | Base queues, research, heroes, alliance, world map, battles, energy, events, IAP |
| **Many** store apps / build flavors | Each theme = separate app identity |
| **Baked at build** | Theme pack + config compiled/bundled into that app — not chosen by the player at runtime |
| **Feature pipeline** | Implement once in shared code → cut builds for Theme A, B, C… |

### 2.2 What this is NOT

- Player-selectable theme switcher in settings  
- Runtime download of a different “world skin” that changes the whole game identity  
- Remote pack swap as the primary shipping model for a new theme  
- `#ifdef` gameplay forks per theme  

Remote config may still tune **balance numbers / events** inside an already-shipped theme app. It must **not** redefine which white-label product the user installed.

### 2.3 Build flavor / app factory

```
survival_core/          # shared systems (theme-agnostic IDs + logic)
ThemePack/
  fantasy_kingdom_a/    # baked into App A
  animal_farm_b/        # baked into App B (later)
  …
AppFlavor/
  fantasy_kingdom_a/    # bundle ID, app icon, splash, store metadata hooks, pack pointer
  …
```

Each **AppFlavor** at build time:

1. Sets application ID / bundle ID  
2. Embeds that theme’s `ThemePack` (art, strings, UI, audio, FTUE copy)  
3. Points config at that pack as **compile-time / build-time constant**  
4. Produces a store binary that only ever presents that theme  

**New theme = new flavor + new pack + new store listing**, same `survival_core`.

---

## 3. Architecture: fixed systems vs skin layer

### 3.1 Fixed systems (shared code + schemas)

| System | Fixed | Per-app skin (baked pack) |
|--------|-------|---------------------------|
| **Base / build queues** | Slots, queue, timers, caps, dependency graph | Names, icons, building art |
| **Research** | Tech graph, costs, timers, gates | Titles, icons, flavor |
| **Heroes** | Slots, rarity, level/star, numeric kits | Portraits, art, skill names/VFX |
| **Alliance** | Create/join, chat, gifts, war hooks, ranks | Label (“Guild”), banners, copy |
| **World map** | Node graph, march rules, unlock, PvE/PvP flags | Map art, node names |
| **Battles** | Formation, sim hooks, power, rewards | Unit skins, VFX, battlefield BG |
| **Energy** | Cap, regen, spend actions | Icon + label |
| **Events** | Calendar, scores, reward tables | Titles, art, narrative |
| **IAP** | SKU classes, billing adapters | Pack names, store creatives |
| **FTUE** | Step machine, fail-safes, analytics names | Mentor lines, character |

Logic uses **stable IDs** (`building.barracks`, `hero.slot_03`, `map.node_12`). Each app’s baked pack binds presentation + strings to those IDs.

### 3.2 Theme pack (per app, baked)

```
ThemePack/<theme_id>/
  pack.json       # theme_id, display_name, systems_api: survival_core_v1
  strings/
  map/            # id → display strings
  ui/
  art/
  audio/
  store/          # listing frames for that app
  ftue/
```

---

## 4. Theme A — Fantasy kingdom defense (first app)

| Systems concept | Theme A |
|-----------------|---------|
| Base | **Castle town** |
| Threat | Monsters / darkness |
| Heroes | Fantasy champions |
| Alliance | **Guild** |
| World | **Crusade map** |
| Research | War College / royal edicts (same graph) |
| Energy | Thematic label TBD; math shared |

### Working building names → stable IDs

| Stable ID | Theme A |
|-----------|---------|
| `building.hq` | Keep |
| `building.barracks` | Barracks |
| `building.economy` | Market |
| `building.research` | War College |
| `building.hero_hall` | Hall of Heroes |
| `building.wall` | Curtain Wall |
| `building.alliance` | Guild Hall |

| Stable ID | Theme A |
|-----------|---------|
| `alliance` | Guild |
| `map.campaign` | Crusade Map |

**Out of Theme A v1:** open-world MMO, UGC land, paywalled Keep, Grove merge systems.

---

## 5. Theme B+ — new store apps, same code

| Step | Action |
|------|--------|
| 1 | Keep shipping `survival_core_v1` |
| 2 | Author `ThemePack/<new_theme>/` + `AppFlavor/<new_theme>/` (bundle ID, icons) |
| 3 | Bind same stable IDs to new art/copy |
| 4 | Cut store build for that flavor; smoke: queue → research → battle → map → IAP sandbox |
| 5 | Store listing is **that app’s** identity — players never switch to Theme A inside Theme B |

**Later themes (TBD packs):** animal / farm / space — new apps, same core.

**Feature deploy:** merge systems work once → CI flavors produce updated binaries for every live theme app (staggered rollout OK).

---

## 6. Monetization (all theme apps)

- Free path through core loops  
- IAP accelerates only  
- StoreKit + Play Billing  
- Soft-launch gates when a theme app soft-launches: CFS ≥ 99.5% / ANR ≤ 0.30% unless Product revises  

---

## 7. vs Project Grove

| | Grove | Skinnable survival |
|--|-------|--------------------|
| Loop | Merge / garden | Base / research / heroes / map / battles |
| Shipping | Grove app | **White-label family** of survival apps |
| Code | Grove repo | Shared survival core + flavors (repo TBD) |

Do not mix Grove merge tickets into survival flavors.

---

## 8. Next Product tickets (thin)

| ID | Deliverable |
|----|-------------|
| SURV-P0 | Stable ID dictionary v0 — **SHIPPED** `surv-p0-stable-id-dictionary.md` / `.json` |
| SURV-P1 | Theme A pack + AppFlavor brief (fantasy first store app) |
| SURV-P2 | Shared economy sketch v0 |
| SURV-P3 | Vertical slice: HQ + 2 buildings + 1 research + 1 hero battle + 3 map nodes **in Theme A flavor** |

---

**— Product**
