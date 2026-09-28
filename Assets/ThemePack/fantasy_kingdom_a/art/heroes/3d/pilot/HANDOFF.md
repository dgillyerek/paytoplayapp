# Sir Aldric PILOT — Design HANDOFF for HI → Dev wire

**Date:** 2026-09-27  
**Pilot:** Sir Aldric only  
**Package:** `/workspace/design/survival-theme-a-fantasy/heroes/anim/sir_aldric/PILOT_LOOK_20260926/`  
**HOLD:** Path A / ClipSword as SoT. No shred/melt/Meshy remesh.

## Path completed (Design)

| Step | Result |
|---|---|
| Locked fullbody SoT → Image→3D look | Derek **PASS** look |
| UV Decimate COLLAPSE mid ≤300k | **280,000** faces (NOT Meshy remesh) |
| Clean Humanoid AccuRIG | **PASS** |
| Sword/scabbard separate | **Soft leftover — still fused** (split attempt; see note) |
| Animate Walk (rear/TOP) | **Exported** |
| Animate Attack (forward draw/downstrike) | **Exported** |

## Key files for Dev wire

| Role | Path |
|---|---|
| Look SoT (Derek PASS paint stills) | `stills/look_{front,34,rear}_paint.png` |
| Painted look GLB (hi-poly) | `exports/SirAldric_PILOT_look_fullbody_PAINTED.glb` |
| Mid280k painted (AccuRIG input) | `exports/SirAldric_PILOT_look_fullbody_PAINTED_mid280k.{glb,fbx}` |
| AccuRIG Humanoid FBX | `accurig/SirAldric_PILOT_accurig_humanoid.fbx` |
| AccuRIG stills | `stills_rig/accurig_{front,34,rear}.png` |
| Walk FBX (MeshyRig, skin, 30 FPS) | `animate/SirAldric_PILOT_walk.fbx` |
| Attack FBX | `animate/SirAldric_PILOT_attack.fbx` |
| Walk stills | `animate/stills/walk_{front,34,rear}.png` |
| Attack stills | `animate/stills/attack_*.png` |
| Midpoly meta | `prep/mid280k_meta.json` |

## Soft leftovers (honest)

1. **Sword fused** into body mesh (look pose had RH sword). No clean separate body/sword/scabbard mesh package yet. Dev may need prop parent later or Design follow-up prop gen — not Path A.
2. Attack motion name on Meshy: `SirAldric_PILOT_Attack_Forward` (left-hip draw/downstrike intent). Fused sword may not leave an empty hip scabbard on draw.
3. Design lean on Animate stills only — **Derek Play / phone Game-view not yet hard-gated** after this wire.

## Ask of HI

Staff Dev **one** wire PR importing AccuRIG + Walk + Attack from this package. Design will lean-gate Play-cam proofs when Dev drops them.
