# Project Grove — Style Bible (LOCKED)

| Field | Value |
|-------|-------|
| **Status** | **LOCKED** — industry visual bar (Derek 2026-09-14) |
| **Owner** | Design |
| **Law** | Phone Game-view must match reference mock; merge/deliver juice clip required for PASS |
| **Comps** | Merge Mansion · Travel Town · Gardenscapes class (vibe/bar only — never copy art) |

This document **supersedes** incremental bug lists for visual PASS/FAIL. Design FAILs the **whole Play look** against this bar.

---

## 0. Exit rule (non-negotiable)

**PASS only when all are true:**

1. Phone-aspect Game-view shot sits next to `GROVE_PLAY_REFERENCE_MOCK_LOCKED_1080x1920.png` and matches its polish (materials, typography, chrome, piece readability, no dead empty board).
2. A short clip (≤8s) shows hero loop: **crate tap → merge pop → deliver to Maya** with premium juice.
3. No programmer / debug / placeholder chrome anywhere Derek can see.

**Asset-only PNG drops = NOT done.**

Visual PRs that change board art, HUD, pieces, VFX, or Play layout do **not** merge without Design attaching phone Play shot + juice clip vs this mock.


## 0b. Scope boundary (Product 2026-09-14)

The locked mock is a **visual quality / layout / juice** target only.

| In scope (mock) | Out of scope (do not expand without Product) |
|-----------------|-----------------------------------------------|
| Materials, chrome polish, board density feel | New item IP (gnomes, lanterns-as-chain, etc.) beyond Area 1 lists |
| HUD layout, type, true-alpha pieces | Economy numbers shown in mock (illustrative; Product caps/regen SoT) |
| Crate→merge→deliver motion feel | Content beyond Front Garden Orders 1–6 + WF/Herb/Tools chains |

MVP content SoT remains Product Area 1 sheet + visual-identity / area1-asset-pack-spec.

---

## 1. Reference pack (SoT comps)

Internal comps only — study layout density, material finish, juice, HUD hierarchy. Do not ship competitor art.

| # | File | Game | What we steal (structure, not pixels) |
|---|------|------|----------------------------------------|
| 1 | `references/ref01_merge_mansion.jpg` | Merge Mansion | Dense readable board, painted piece icons with **true alpha**, orders as clear cards, premium wood/metal materials, energy/wallet chrome that looks finished |
| 2 | `references/ref02_travel_town.jpg` | Travel Town | Order-driven merge board, spit producer clarity, thumb-friendly bottom/side chrome, soft merge pops |
| 3 | `references/ref03_gardenscapes_phone.jpg` | Gardenscapes | Warm garden coziness, soft lighting, character portrait in HUD (not on board), premium casual F2P finish |

### Grove locked Play mock

| File | Use |
|------|-----|
| `GROVE_PLAY_REFERENCE_MOCK_LOCKED.png` | Master mock |
| `GROVE_PLAY_REFERENCE_MOCK_LOCKED_1080x1920.png` | Phone SoT — side-by-side with Derek Game-view |

---

## 2. Positioning (from visual-identity-v0, raised to ship bar)

**One-liner:** A sun-washed cottage garden you restore one tidy merge at a time — **store-quality on mid-range phones**, cozy-modern, never student/prototype.

| Keyword | Ship meaning |
|---------|--------------|
| **cozy-modern** | Soft warmth + clean geometry; lived-in, not fairy-tale clutter |
| **sun-washed** | Warm key, gentle AO; outdoor glare-safe contrast |
| **tidy-not-sterile** | Calm order; no clinical white plates under pieces |
| **readable-first** | Silhouette + hue + tier pip beat decoration |
| **juice-first** | Hero crate→merge→deliver feels expensive before chrome sprawl |

---

## 3. Material & finish law

| Surface | Required feel |
|---------|---------------|
| Board | Warm wood / soft soil tray — never bare grey grid or debug cells |
| Cells | Recessed cream with **true alpha** around pieces; no opaque cream plates under icons |
| Pieces | Painted icons, soft sheen, **true alpha PNGs**, distinct T1–3 silhouettes |
| Crate | Hero object — glow when charged, clear tap + charges |
| HUD chrome | Wood + soft gold + Grove Green; matches Maya/splash polish |
| Buttons | Embossed wood / soft clay; never flat grey Unity default |
| Type | Mobile floors (DES-005): energy/wallets ≥28px @1080; no mid-word wrap on STARTER / order titles |

---

## 4. Layout law (phone portrait)

```
┌─────────────────────────────┐
│ Energy · Coins      Maya    │  top band (HUD only)
│ Goal pill                   │
├─────────────────────────────┤
│                             │
│     BOARD (sanctity)        │  no order overlap
│     Crate on-board          │  ≥16dp gap to dock
│                             │
├─────────────────────────────┤
│ Order dock · DELIVER        │  reserved bottom band
└─────────────────────────────┘
```

- Board never covered by orders.
- Maya = 2D HUD portrait only (Product lock).
- Dead empty board = FAIL (fill density / props / vignette to industry density).

---

## 5. Motion / VFX juice (first-class)

Hero moment must beat chrome polish:

| Beat | Feel | Timing target |
|------|------|---------------|
| Crate tap | Soft squash + spit arc + charge pip tick | 80–120ms |
| Merge | Juicy pop, soft gold sparkle, scale overshoot | 120–180ms |
| Deliver | Card settle + fly-to Maya + soft success flash | 200–350ms |
| Tier-up (bonus) | Soft bloom pulse | ≤200ms |

No heavy mesh trails. GPU particles / simple quads. Haptic soft on merge if platform allows.

**Juice clip PASS criteria:** see `JUICE_CLIP_PASS.md`.

---

## 6. FAIL whole product when…

Any of these vs reference mock / comps:

- Looks like a student prototype next to Merge Mansion / Travel Town / Gardenscapes
- Cream / white plates under pieces or order icons
- Debug toast, checkerboards, yellow debug rings, letter labels
- Unreadable / wrapping type on phone
- Orders overlapping board or floating unfinished chrome
- Crate→merge→deliver feels flat (no juice)
- Asset drop without phone proof

Do **not** PASS on “five bugs fixed.” PASS only when the **whole screen** clears the bar.

---

## 7. Related docs

- `../visual-identity-v0.md` — palette / type / shape detail
- `../docs/des-005-mobile-hud-pass.md` — type floors + alpha
- `../docs/des-006-a9e4-fail-fix.md` — prior phone FAIL checklist (subsumed by whole-product FAIL)
- `../docs/play-hud-layout-v1.md` — dock / teach anchors
- `JUICE_CLIP_PASS.md` — clip SoT

---

## 8. Ownership

| Who | Does |
|-----|------|
| **Design** | Owns Play look; locks mock + bible; PASS/FAIL with phone proof + juice clip |
| **Dev** | Integrates; never merges visual PRs without Design Play proof attached |
| **QA** | Holds NOT PASS / no Derek invite until Design PASS under this bar |
| **Product** | Scope/theme only |
| **CEO** | Escalation / Derek rollups |

Working title: **Project Grove** only.
