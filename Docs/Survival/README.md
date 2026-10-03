# SURV-P0 scaffold

White-label survival core + Theme A (`fantasy_kingdom_a`) first store flavor.

Product dictionary and theme-pack spec are copied here for repo SoT.

**Playable now:** crusade World hub → GATHER (quarry Mine), FIGHT (dark keep Attack), and EXPLORE (ruins Scout).

**Sir Aldric (3D Animator):** `Assets/Survival/Scenes/SirAldric.unity` — Game view 1080×1920 — high-angle rear, **march toward TOP**. Capsule-sculpted skinned mesh + `01_rear` albedo + Animator PlayableGraph. Locked rear PNG stays the **Play placeholder** until Design PASS. Pipeline: `Docs/Survival/HERO_3D_PIPELINE.md`. Previews: `Docs/Survival/previews/` (`PIXEL_PROOF.txt`, `ART_UPGRADE.md`). 2D PNG warp is quarantined.

**Blightroot (creature pack):** `Assets/Survival/Scenes/Blightroot.unity` — Game view 1080×1920. Dual Weapon Combo is Y-up in the FBX. Axis conversion is not baked, and the instance root rotation is cleared, so Game view keeps that upright body instead of a flat axis-compensation. Scale stays uniform (1 inside 0.5–5 m, otherwise 1.80 m). One dropdown plays the 19 exact Mixamo names (`mixamo.com`, not Scene). No retarget, no mirror, no time reverse. HOLD merge until Derek Game-view PASS.

**Splash:** locked keep, aspect-fill (no letterbox), ~5s dwell, animated Loading dots. Path: `Assets/Survival/Scenes/Splash.unity`.

Visual Play proof (phone Game view 1080×1920) is required before merging visual PRs. Do not self-merge.
