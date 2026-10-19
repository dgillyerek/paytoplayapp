# Survival core (white-label F2P) — Theme A first flavor

**Not Grove.** Shared systems live under `Assets/Survival/` (`survival_core_v1`). Theme A is a baked store flavor, not an in-app skin.

| Piece | Path |
| --- | --- |
| Systems | `Assets/Survival/Runtime/` + `Assets/Survival/Resources/Survival/*.json` |
| Theme pack | `Assets/ThemePack/fantasy_kingdom_a/` |
| App flavor | `Assets/AppFlavor/fantasy_kingdom_a/flavor.json` |
| Splash scene | `Assets/Survival/Scenes/Splash.unity` |
| Play scene | `Assets/Survival/Scenes/Play.unity` |
| Design stills | `design/survival-theme-a-fantasy/play-mock/` |
| Gather juice SoT | `design/survival-theme-a-fantasy/juice/quarry-loop-stills/` |

## Open in Unity 6.3 LTS (`6000.3.6f1`)

1. Open this repo folder.
2. Double-click **`Assets/Survival/Scenes/Splash.unity`**.
3. Game view **1080×1920 portrait** (also check a wider/taller size — splash must fill edges, no letterbox bars). Click **Play**.
4. Splash keep fills the screen for **~5 seconds** with animated `Loading.` / `Loading..` / `Loading...`, then Play crusade world (no tap to skip).
5. Tap **GATHER · Quarry** → Stone Quarry inspect → **Mine** → march for the listed time (e.g. 2m 12s, live countdown) → **+840 Stone** → short return HOME.
6. Tap **FIGHT · Dark Keep** → Dark Keep inspect (Threat · Darkness · March + battle · **Attack**) → march → purple pulse / **+1,200 Gold** → return HOME.
7. Tap **EXPLORE · Ruins** → Ruins inspect (Relics · March + scout · **Explore**) → march → **+600 Gold** → return HOME.

Direct Play shell (skips splash): `Assets/Survival/Scenes/Play.unity`.

Sir Aldric **3D Animator** (high-angle rear, march+attack toward TOP): open **`Assets/Survival/Scenes/SirAldric.unity`**, Game view **1080×1920**, Play. Capsule-sculpted skinned mesh (`SirAldric3DActor`) with look-target albedo. Remaining DCC gap: `Docs/Survival/previews/ART_UPGRADE.md`. Play hub still uses locked rear PNG `SIR_ALDRIC_REAR_MASTER_LOCKED.png` until Design PASS. HUD portrait stays `theme_a_hero_knight_01`.

Locked battle (heroes vs villains): menu **Survival → Locked Battle (heroes vs villains, Game view 1080x1920)** or open **`Assets/Survival/Scenes/BattleLocked.unity`**, Game view **1080×1920**, Play. Four lanes, one matchup each: the original inner lanes Emberfang vs Ironhowl and Stormcrest vs Nightfang, plus outer lanes Lyra vs Vespera (left) and Oakenshield vs Ashwyrm (right). Heroes start at the bottom, villains at the top. Each walks in on its locked walk (Nightfang trots), stops at its attack range, and loops its locked attack with a short rest. Hits land with an impact per attacker (Emberfang fire burst, Stormcrest lightning crackle, Nightfang shadow claw rake, Ironhowl claw sparks and dust, Lyra staff light, Oakenshield leaves and earth, Vespera purple conjure, Ashwyrm void-fire breath with a smoke gust): every flying attack (fireball, storm bolt, arcane bolt, thorn spear, shadow orb, void fireball) stops on the target body, the claws land on their contact frame (Nightfang f14, Ironhowl f11). The defender flinches back a few cm (skipped while it is mid-attack) and flashes. Ground: tiled world-map grass with a worn dirt track down each lane, soft contact shadows under every fighter, and a hazy green horizon with distance fog (`LockedBattleGround`, `LockedBattleGroundSpec`). Hits: `LockedBattleHits`. The dropdown switches rear / three-quarter / front / side cameras (keys 1–4); **RESTART** runs it again. Roster: `Survival.Domain.Roster.LockedBattleRoster`. HOLD merge until Derek Game-view PASS.

Menu **Survival → Use Theme A Flavor (fantasy_kingdom_a)** puts Splash then Play first in Editor Build Settings (Grove `Board` stays listed).

Visual / playable: Design phone Game-view re-gate before merge. Do not self-merge.

## World + quest hook slice (HOLD)

Menu **Survival → Play: World + Quest Hook (Game view 1080x1920)** opens `Assets/Survival/Scenes/Play.unity` and enters Play (or open Play.unity and press Play; Splash → Play also gets it).

- **Base strip** (above the hero card): Castle, Farm, Lumber Mill. Farm/Lumber start full: tap the card (or its bobbing `+N` bubble) to collect: coins fly to the Food/Wood chip and it counts up. Refill 1 food/s, 0.83 wood/s, min 20 to collect.
- **Castle → Upgrade**: 2,000 Gold · 1,500 Stone · 1,200 Wood, 10:00 timer, one builder. Tap again for **Speed Up**: gem rush (1 gem/min, rounded up), use speed-up minutes, or **Finish FREE** once ≤ 5:00 is left. Finish → level-up burst. Farm/Lumber upgrades unlock at Castle Lv 2.
- **Quest tracker** (top-left, replaces the static queues mock): 1/3 collect ×3 → 2/3 mine the Stone Quarry (GATHER · Quarry → Mine) → 3/3 Castle Lv 2. Complete → bouncing chest → tap → chest-burst celebration → rewards fly to the HUD.
- **Gem Shop** (right, under Events) + **Gems chip**: Free Gift slot, Pouch of Gems (80, $0.99), Chest of Gems (500, $4.99, BEST VALUE), 15 min Speed-up (12 gems). **SANDBOX** only: purchases succeed instantly, nothing is charged. Pulsing **FREE** badge while a gift waits.
- **Free gift**: one every 20–45 min (random), max 6 per UTC day, never more than one unclaimed.
- **Debug** (Editor / dev builds): **G** or the red **DEBUG: Gift now** button = gift instantly; **T** or **DEBUG: +5 min** = skip game clock 5 minutes.

Data: `Resources/Survival/base_economy.json`, `quests.json`, `gifts.json`, `stores.json`; Theme A: `ThemePack/fantasy_kingdom_a/store/products.json`, `strings/en.json` (`hook.*`, `quest.*`, `store.*`, `offer.*`, `gift.*`, `res.*.name`), `ui/hud_layout.json` (`quest_tracker`, `res_hard`, `gem_shop`, `base_strip`, `debug_*`). Runtime: `Runtime/Clock`, `Economy`, `Base/BaseEconomy.cs`, `Quest`, `Gift`, `Store`, `Session/HookSession.cs`; view: `Unity/SurvivalHookView.cs`. Progress is in-memory (resets on Play). HOLD merge until Derek Game-view PASS.

## IDs

Logic uses SURV-P0 stable IDs only (`sys.*`, `building.*`, `world.node_type.*`, `world.node.gather_01` / `fight_01` / `explore_01`, `world.action.gather` / `scout` / `march`, `battle.action.start`, `energy.action.gather` / `battle` / `march`). Gold / Stone / Wood / Food / Gems are real `res.*` ids (`res.soft`, `res.stone`, `res.wood`, `res.food`, `res.hard`) held in one `Economy.Wallet`; the HUD chips (`res_soft` / `res_stone` / `res_wood` / `res_food` / `res_hard` layout slots) are a view over it (`ChipWallet`). Hook-slice families: `base.plot.*`, `quest.*`, `store.*`, `offer.*`, `gift.*` (see `Docs/Survival/surv-p0-stable-id-dictionary.md`). Theme A copy/art is `theme_a.*` inside the pack. Do not add `grove.*` / merge-board IDs.

Headless: `dotnet test tests/Survival.Domain.Tests/Survival.Domain.Tests.csproj`
