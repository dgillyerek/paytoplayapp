# Derek OVERRIDE — Humanoid draw→strike (2026-09-25)

**HOLD merge on PR #27 until Derek eyes.** Do not claim Design PASS. Path A weight-paint CANCELLED.

Derek OVERRIDE: do **not** wait on Design attack FBX re-export. Use the **existing** Meshy Animate walk Humanoid (same mesh / atlas / Walking clip). Author **draw sword → strike forward** toward World TOP (RearYaw 180). No new Design mesh.

## How the clip is authored

`SirAldricHumanoidAttack` + `SirAldricMeshyAnimateActor.ApplyDrawStrike`:

1. Play the Design walk clip AS-IS on the Mixamo Humanoid (`mixamorig:*`, same Avatar).
2. After `WalkCyclesBeforeAttack` cycles, **plant** the last walk frame (wide stance stays Design walk iterate).
3. Aim the **RightUpperArm → RightLowerArm → RightHand** chain at hips-local targets: scabbard → guard → **+Z thrust** → recover. `Quaternion.FromToRotation` only — **never writes `localScale`** (stretch spikes = FAIL).
4. Clip-only `ClipSword` cube parented to `RightHand` while drawn. Walk mesh scabbard is painted; no Path A rebind.

Leftover Design attack FBX (`sir_aldric_meshy_animate_attack.fbx`, Rigify `Hips/Spine02`) stays on disk and is **not played**. Playing it on this Avatar was the squash.

## Walk stance

Still clip-authored wide (rest 0.396 m, Walking take max 0.719 m @ f8). Import `heightFromFeet` / `addHumanoidExtraRoot` unchanged. **Wait Design walk iterate.** Secondary to attack.

## Proof

Unity Game-view is `Camera.Render` 1080×1920 from the SirAldricDemo Play cam `(0, 2.80, −5.40)` LookAt `(0, 0.90, 0.50)` FOV 30.

| File | What |
| --- | --- |
| `sir_aldric_humanoid_draw_strike_gameview.mp4` | Game-view draw → forward strike, rear / TOP |
| `sir_aldric_humanoid_mid_strike_gameview.png` | Game-view mid-strike still |
| Menu | Survival → Capture Sir Aldric Humanoid Attack (Game view) |
| Batch | Play with `-aldric-capture` or `ALDRIC_CAPTURE=1` |

This cloud VM has no Unity Editor binary. Capturer ships with the actor; Derek Play produces the MP4/still. Do not treat leftover Blender standing-slash frames as this OVERRIDE.

No Design PASS.
