# SURV-P0 scaffold

White-label survival core + Theme A (`fantasy_kingdom_a`) first store flavor.

Product dictionary and theme-pack spec are copied here for repo SoT.

**Playable now:** crusade World hub → GATHER (quarry Mine), FIGHT (dark keep Attack), and EXPLORE (ruins Scout).

**Characters:** The one-character demo scenes (Sir Aldric, Lyra, Rowan, Oakenshield, Vespera, Bonequill, Blightroot, Ironhowl, Emberfang, Stormcrest, Nightfang, Ashwyrm) and their Survival menu entries were removed on 2026-10-19; every character plays in the Locked Battle. Their actors, FBXs, materials and clips are unchanged. Sir Aldric 3D pipeline notes: `Docs/Survival/HERO_3D_PIPELINE.md`. Previews: `Docs/Survival/previews/`.

**Blightroot (creature pack):** plays in the Locked Battle (`Assets/Survival/Scenes/BattleLocked.unity`). Dual Weapon Combo is Y-up in the FBX. Axis conversion is not baked, and the instance root rotation is cleared, so Game view keeps that upright body instead of a flat axis-compensation. Scale stays uniform (1 inside 0.5–5 m, otherwise 1.80 m). One dropdown plays the 19 exact Mixamo names (`mixamo.com`, not Scene). No retarget, no mirror, no time reverse. HOLD merge until Derek Game-view PASS.

**Splash:** locked keep, aspect-fill (no letterbox), ~5s dwell, animated Loading dots. Path: `Assets/Survival/Scenes/Splash.unity`.

Visual Play proof (phone Game view 1080×1920) is required before merging visual PRs. Do not self-merge.
