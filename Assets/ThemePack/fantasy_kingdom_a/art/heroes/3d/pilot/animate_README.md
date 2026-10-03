# AccuRIG Walk + Attack clips — 2026-09-28

Clips authored on Meshy **AccuRIG** avatar `SirAldric_PILOT_mid28` / mid280k biped
(same Character_output as `../accurig/SirAldric_PILOT_accurig_humanoid.fbx`).

## Export settings (Meshy)

| Clip | Preset / motion name | Skin | FPS | Source download |
|---|---|---|---|---|
| Walk | Preset **`Walking`** (not Running) | **With Skin** | **30** | `Meshy_AI_SirAldric_PILOT_mid28_biped (3).zip` → `…_Animation_Walking_withSkin.fbx` |
| Attack | Custom AI motion `SirAldric_PILOT_Attack_Forward` / Meshy id `01a0ddb4-17af-70a5-861d-d7364be71530` (left-hip draw → forward downstrike / TOP) | **With Skin** | **30** | `Meshy_AI_SirAldric_PILOT_mid28_biped (6).zip` → `…_Animation_01a0ddb4-…_withSkin.fbx` |

## Files

- `SirAldric_PILOT_walk_accurig.fbx`
- ~~`SirAldric_PILOT_attack_accurig.fbx`~~ **DISCARD** — Play attack is library `SirAldric_PILOT_attack_library.fbx` (Right-hand Sword Slash).
- `stills/` — Meshy viewport captures (walk front/¾/rear; attack draw / overhead-mid / strike / angles)
- `BODY_MESH_SOT.txt` — visible body must be AccuRIG Character_output only

## Bone map (same as AccuRIG humanoid)

`Hips`, `Spine`, `Spine01`, `Spine02`, `Head`, `LeftUpLeg`/`LeftLeg`/`LeftFoot`,
`RightUpLeg`/`RightLeg`/`RightFoot`, `LeftArm`/`LeftForeArm`/`LeftHand`,
`RightArm`/`RightForeArm`/`RightHand` (+ Hand_End).

## Dev wire

**Clips-only.** Import `../accurig/SirAldric_PILOT_accurig_humanoid.fbx` as mesh SoT;
pull animation clips from these FBX; discard embedded With-Skin mesh if Unity duplicates.

## Soft leftovers

Sword remains soft-fused into look mesh (HOLD Path A / ClipSword). No remesh.

## Note on 2026-09-28 executor pass

Fresh Meshy UI re-author was **blocked** on this executor (no `computerUse` /
Screenshot tools available). Staged FBX are the proven AccuRIG-mid28 With-Skin
exports from the 2026-09-27 Animate-on-AccuRIG session (md5-matched to Downloads
biped zips). Same skeleton as Character_output — re-author would not change bone
names. Parent may re-dispatch with computerUse if new motion takes / stills are required.
