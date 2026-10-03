# SUPERSEDED — AccuRIG clips-only is not Play SoT

**2026-09-28 Mixamo separate-portrait tip supersedes this AccuRIG HANDOFF.**  
Play body / walk / slash: see `MIXAMO_SEP_MANIFEST.md` + `Docs/Survival/previews/aldric_pilot_20260928/PILOT_HOLD.md`.  
Do **not** instantiate AccuRIG as the playable body. HOLD merge on PR #27.

---

# Sir Aldric PILOT — Design HANDOFF for HI → Dev wire (archive)

**Date:** 2026-09-28 (library Right-hand Sword Slash replaces AI Attack_Forward)  
**Pilot:** Sir Aldric only  
**HOLD:** Path A / ClipSword as SoT. No shred/melt/Meshy remesh of the look mesh.

## Status 2026-09-28 — library attack (Play SoT)

**Attack clip SoT:** `SirAldric_PILOT_attack_library.fbx` — Meshy Preset **Right-hand Sword Slash** (library/mocap, not generative). Discard Meshy AI Attack_Forward / `SirAldric_PILOT_attack_accurig.fbx`.

Keep AccuRIG Character_output as the **ONLY** visible body. Walk stays `SirAldric_PILOT_walk_accurig.fbx`.


Derek FAIL tip **1d1b030**: Play used **Animate FBX mesh** (stretch cape/sword, jagged) — not AccuRIG look.  
Dev Path 1 retarget FAIL tip **d85c62c**: AccuRIG extremely distorted. Dev will **NOT** push another retarget tip.

**HI GO → Design:** Walk + Attack on AccuRIG skeleton for a **CLIPS-ONLY** Unity wire.

### Explicit Dev ask (clips-only wire)

1. Import **`accurig/SirAldric_PILOT_accurig_humanoid.fbx`** as the **ONLY** visible body mesh (Meshy AccuRIG Character_output / mid280k biped).
2. Extract / use **animation clips only** from:
   - `animate_accurig_20260928/SirAldric_PILOT_walk_accurig.fbx`
   - `SirAldric_PILOT_attack_library.fbx` (library Right-hand Sword Slash — **not** AI Attack_Forward)
3. If With-Skin FBX embeds a mesh: **discard embedded mesh** if Unity duplicates — do **not** ship Animate mesh as body.
4. Soft fused sword leftover OK (no Path A force-split).
5. Design will **lean-gate Play-cam** after Dev drops proofs. Do not treat asset drop as PASS.

**NEVER** use a separate Meshy "Animate mesh" as the playable body.

## Key files for Dev wire

| Role | Path |
|---|---|
| Look SoT (Derek PASS paint stills) | `stills/look_{front,34,rear}_paint.png` |
| AccuRIG Humanoid FBX (**ONLY visible body**) | `accurig/SirAldric_PILOT_accurig_humanoid.fbx` |
| AccuRIG stills (silhouette SoT) | `stills_rig/accurig_{front,34,rear}.png` |
| Walk clip FBX (AccuRIG skeleton, With Skin, 30 FPS, preset Walking) | `animate_accurig_20260928/SirAldric_PILOT_walk_accurig.fbx` |
| Attack clip FBX (library/mocap Right-hand Sword Slash, AccuRIG mid280k, 30 FPS) | `SirAldric_PILOT_attack_library.fbx` |
| DISCARD | Meshy AI Attack_Forward / `SirAldric_PILOT_attack_accurig.fbx` |
| New stills | `animate_accurig_20260928/stills/` |
| Export settings | `animate_accurig_20260928/README.md` |
| Body SoT note | `animate_accurig_20260928/BODY_MESH_SOT.txt` |
| LEGACY prior animate (FAIL-ref; do not use as body) | `animate_LEGACY_meshy_20260927/` |

## Soft leftovers (honest)

1. **Sword fused** into body mesh (look pose had RH sword). No clean separate body/sword/scabbard package. HOLD Path A.
2. Attack motion: left-hip draw then forward/TOP downstrike (`SirAldric_PILOT_Attack_Forward` / Meshy id `01a0ddb4-…`). Fused sword may not leave empty hip scabbard on draw.
3. Fresh Meshy UI re-capture on 2026-09-28 was **blocked** (executor lacked computerUse). Staged clips are AccuRIG-mid28 With-Skin exports from the Animate-on-AccuRIG session (same bone names as Character_output; md5-matched to Downloads biped zips). Stills under `animate_accurig_20260928/stills/` are from that AccuRIG session (280k faces / `SirAldric_PILOT_mid280k` viewport).

## Path completed (Design)

| Step | Result |
|---|---|
| Locked fullbody SoT → Image→3D look | Derek **PASS** look |
| UV Decimate COLLAPSE mid ≤300k | **280,000** faces (NOT Meshy remesh) |
| Clean Humanoid AccuRIG | **PASS** |
| Sword/scabbard separate | **Soft leftover — still fused** |
| Walk + Attack on AccuRIG skeleton (clips package) | **Staged** `animate_accurig_20260928/` |
| Clips-only HANDOFF for Dev | **This doc** |

## Ask of HI

Staff Dev **one** clips-only wire PR: AccuRIG Character_output as mesh + Walk/Attack clips from `animate_accurig_20260928/`. Design lean-gates Play-cam when Dev drops proofs.
