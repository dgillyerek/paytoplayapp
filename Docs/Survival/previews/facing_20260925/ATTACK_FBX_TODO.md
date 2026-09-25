# Design attack FBX — LANDED (2026-09-25)

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
