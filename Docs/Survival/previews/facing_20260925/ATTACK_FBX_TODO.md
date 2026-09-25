# Design attack FBX — HOLD for same-rig re-export (2026-09-25)

**Derek FAIL:** current drop is Rigify-named (`Hips` / `Spine02`), not Mixamo. Playing it on the walk Humanoid squashed. Actor now uses a **native attack instance**. Design is re-exporting on the **walk Mixamo rig**, draw → strike forward, rear / TOP.

# Design attack FBX — first drop (superseded)

**Status: WIRED.** Design drop extracted from `sir_aldric_meshy_animate_attack.fbx.tar.gz`.

| | |
| --- | --- |
| Path | `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_attack.fbx` |
| Package label | Meshy Lionguard Knight · Standing Sword Slash Attack |
| FBX take (Blender / #25) | `target_character|rigify_clip|BaseLayer` (frames 3–92 @ 30 fps) |
| Stub take (ignore) | `target_character\|Armature\|clip0\|baselayer\|BaseLayer` (2–3) |
| Unity clip name | `Attack` (short name; takeName stays the stack) |

`SirAldricMeshyAnimateActor` mixer: Walking (loop) × `WalkCyclesBeforeAttack` → 0.12s fade → Attack (once) → fade back. Same Humanoid as walk. Same `RearYawDegrees` 180 — **back to camera, slash toward TOP**.

Do not use old capsule `sir_aldric_walk_attack_toward_top.*`. Path A cancelled. No Design PASS claimed.
