# TODO — Design drop: Sir Aldric attack FBX

**Status (2026-09-25): WAITING ON DESIGN.** No attack FBX in ThemePack, Survival, design/, or GitHub releases (only `meshy-animate-walk-fbx-20260922`). Walk FBX takes are `Walking` + `Walking.001` only.

Dev does **not** weight-paint, hang-skin, retopo-bind, or bake a fake attack by hand. Path A cancelled.

## Drop path (convention)

Put the Mixamo / AccuRIG Animate package here (sibling of the walk FBX):

```
Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_attack.fbx
```

Also accepted (actor scans in order):

1. `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_animate_attack.fbx` ← **preferred**
2. `Assets/Survival/Art/sir_aldric_meshy_animate_attack.fbx`
3. A named take on the existing walk FBX (`Attack` / `Slash` / `Strike` / `Punch`)

A multi-clip FBX is fine. Clip name must contain **Attack**, **Slash**, **Strike**, or **Punch**. `#25` rule still applies: `clipAnimations.takeName` must match the FBX stack (not a short invented name that drops the clip on Humanoid import).

## What Dev already wired

`SirAldricMeshyAnimateActor.LoadAttackClip()` + `AnimationMixerPlayable` crossfade:

- Walking (input 0, loop) for `WalkCyclesBeforeAttack` cycles
- Attack (input 1, once) then fade back to Walking
- Same `RearYawDegrees` 180 — attack plays **back-to-camera / toward TOP**

On drop: Unity Humanoid-import the FBX, Play `SirAldric`. Actor sets `HasAttackClip`. Re-render `sir_aldric_attack_toward_top_rear.mp4` into this folder.

## Do not

- Bind Path 2 `sir_aldric_meshy_atlas.png` as the Animate albedo (wrong unwrap; `#26` uses packed FBX `texture_0`).
- Treat `Docs/Survival/previews/sir_aldric_walk_attack_toward_top.*` as the Design attack (old capsule `Evaluate()` loop).
- Claim Design PASS from compile or material punch.
