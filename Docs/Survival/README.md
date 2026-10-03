# SURV-P0 scaffold

White-label survival core + Theme A (`fantasy_kingdom_a`) first store flavor.

Product dictionary and theme-pack spec are copied here for repo SoT.

**Playable now:** crusade World hub → GATHER (quarry Mine), FIGHT (dark keep Attack), and EXPLORE (ruins Scout).

**Sir Aldric (3D Animator):** `Assets/Survival/Scenes/SirAldric.unity` — Game view 1080×1920 — high-angle rear, **march toward TOP**. Capsule-sculpted skinned mesh + `01_rear` albedo + Animator PlayableGraph. Locked rear PNG stays the **Play placeholder** until Design PASS. Pipeline: `Docs/Survival/HERO_3D_PIPELINE.md`. Previews: `Docs/Survival/previews/` (`PIXEL_PROOF.txt`, `ART_UPGRADE.md`). 2D PNG warp is quarantined.

**Blightroot (creature pack):** `Assets/Survival/Scenes/Blightroot.unity` — Game view 1080×1920. Dual Weapon Combo skinned body keeps the FBX proportions (axis conversion baked, root scale stays uniform) and plays the 19 Mixamo creature-pack takes (`mixamo.com`, not Scene) from one dropdown of the exact Mixamo names. No retarget, no mirror, no time reverse. HOLD merge until Derek Game-view PASS.

**Splash:** locked keep, aspect-fill (no letterbox), ~5s dwell, animated Loading dots. Path: `Assets/Survival/Scenes/Splash.unity`.

Visual Play proof (phone Game view 1080×1920) is required before merging visual PRs. Do not self-merge.
