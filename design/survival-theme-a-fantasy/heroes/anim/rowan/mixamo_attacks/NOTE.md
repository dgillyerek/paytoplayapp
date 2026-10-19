# Rowan attacks 2 and 3 (Mixamo Pro Longbow, 2026-10-18). HOLD: Derek Game-view check before any merge.

Sources (raw downloads kept here, md5s in CHECKSUMS.md5; Mixamo auto-rig of ROWAN.fbx, same 25 mixamorig names, no In Place offered):
- `ROWAN_mixamo_standing_draw_arrow.fbx` (32 f): reaches up over the shoulder, nocks, draws to the jaw.
- `ROWAN_mixamo_standing_aim_recoil.fbx` (22 f): looses on f3 and recoils back to the low bow.
- `ROWAN_mixamo_standing_aim_overdraw.fbx` (114 f): a held aim that slowly overdraws, with no release.

## Bakes (`work/scripts/archer_mixamo_retarget_20261018.py` + `params_*.json`, from `fresh_20261013/export/ROWAN_fresh.blend`, not saved over)
- **`ROWAN_blenderig_attack2_double_shot.fbx`** (112 f, 0–111):
  - Sequence: Draw Arrow, then Aim Recoil f1–12 (looses f44), then Draw Arrow f9–32 entered from the recoil with a 6-frame crossfade, then Aim Recoil (looses f80).
  - In battle the arrows land at f53 and f89.
- **`ROWAN_blenderig_attack3_volley.fbx`** (116 f, 0–115):
  - Sequence: Draw Arrow, then Aim Overdraw f1–40, then Aim Recoil (looses f84).
  - Spine and Spine1 are lifted 35° in total (eased in over f26–51, held to f89, eased out by f101), so the whole aim tilts up while the bow arm and hand keep the Mixamo pose. The arrow leaves about 37° up.
  - Three arrows land at f93, f95 and f97, 0.35 m apart, each with a 0.6 m arc.

## Method
- **Bind fix:** Mixamo re-binds the auto-rig in its T-pose, but ROWAN_rig binds in the A-pose (arms differ by 57°). Each target bone's reference is therefore its bind turned, by the minimal rotation with roll kept, to point where the source bind bone points. The world rotation delta from that reference is then applied.
- **Bones:** body bones only. The left finger grip comes from the rest action; there are no right-hand fingers.
- **Root:** the hips' horizontal drift is removed (net travel 0; in-clip shift up to 4 cm is kept).
- **Blend in/out:** 10 frames in from rest, with the left foot stepping forward first into the side-on stance (left foot forward, hips side-on, as in the clip). 12 frames back out.
- **Draw length:** capped at 0.74 m (the right arm is IK'd forward while nocked) so the 0.795 m arrow always reaches past the riser. The raw Mixamo draw is 0.85–0.90 m.
- **Bow:** never takes the Mixamo hand. It stays in the locked `RowanAttack.BowGrip` on LeftHand and only rolls in the grip, 8° rest to 50° draw per shot (`RowanMotion.Attack*BowShots`), mirrored in Unity by `BlenderBoneGrip.RollDeg(BlenderBowShots, frame)`.
- **Arrow:** the attack's `ROWAN_arrow` track rides RightHand at its attack f10 binding (nock 5 cm up the hand bone). From the nock it is aimed through the arrow rest (prop-local 0.03, 0, 0.06), on his left side of the riser: +X is his left on every full-draw frame (checked).

## QC (work/qc_*.json; contact sheets in this folder)
- Bow against the body, left hand and forearm excluded:
  - attack 2: 0 overlapping faces on every checked frame except f94, where the lower limb grazes the leg on the way down (44 faces);
  - attack 3: 0 on every checked frame.
- Rest-roll string against the left forearm: centreline 4.3 cm, the same as the approved carry (about 0.8 cm off the skin).
- At full draw the bow stands within 8° of vertical, with the string toward him.

## Not shown on the sheets
- The coat springs run in Unity only (ClothSpring).
- The drawn string: Rowan's bow has no drawn-string line, the same as in his attack 1.
