# Bonequill attacks 2 and 3 (Mixamo Pro Longbow, 2026-10-18). HOLD: Derek Game-view check before any merge.

Sources (raw downloads kept here, md5s in CHECKSUMS.md5; Mixamo auto-rig of BONEQUILL.fbx, same 25 mixamorig names, no In Place offered):
- `BONEQUILL_mixamo_standing_draw_arrow.fbx` (32 f): the right hand reaches back over the right shoulder to the quiver, nocks, and draws.
- `BONEQUILL_mixamo_standing_aim_overdraw.fbx` (114 f): a held, slowly overdrawing aim with no release.
- **Release:** "Standing Aim Recoil" was not downloaded for Bonequill, so both clips loose with Rowan's copy (`heroes/anim/rowan/mixamo_attacks/ROWAN_mixamo_standing_aim_recoil.fbx`, md5 ecc0ca95c858ee1083294ba57614d994). Its motion is retargeted the same way, with a 4-frame crossfade in. A native download onto BONEQUILL.fbx would drop in with the same params.

## Bakes (`work/scripts/archer_mixamo_retarget_20261018.py` + `params_*.json`, from `gritty_20261013/export/BONEQUILL_gritty.blend`, not saved over)
- **`BONEQUILL_blenderig_attack2_rapid_quiver_shot.fbx`** (68 f, 0–67):
  - Draw Arrow played 1.3x faster, then Aim Recoil.
  - The arrow is in hand at the quiver from f19, nocked at f24, loosed at f36; in battle it lands at f45 with BoneShard.
- **`BONEQUILL_blenderig_attack3_bone_volley.fbx`** (116 f, 0–115):
  - Draw Arrow, then Aim Overdraw f1–40, then Aim Recoil. Spine and Spine1 are lifted 35° in total (never the bow arm).
  - Loosed at f84; three BoneShard hits land at f93, f95 and f97, 0.35 m apart, with a 0.6 m arc.

## Method
Same pipeline as Rowan's (see `heroes/anim/rowan/mixamo_attacks/NOTE.md`):
- **Bind fix:** the T-pose bind is matched to the rig's bind.
- **Bones:** body bones only.
- **Rest pose:** the reference rest is attack f0, which the idle bow is bound at. The left finger grip comes from it.
- **Root:** the hips' horizontal drift is removed.
- **Stance:** left-foot step into the side-on stance.
- **Draw length:** capped at 0.72 m, so the 0.79 m arrow reaches past the riser.
- **Bow:** stays in the locked `BonequillAttack.BowGrip` and only rolls in it (10.5° to 30° per shot).
- **Arrow:**
  - The attack's `BONEQUILL_bonearrow_purple_fletch` track rides RightHand at its attack f10 binding.
  - From the nock it is aimed through the locked arrow rest (0.033, 0, 0.0574), on his left side of the riser: +X is his left on every full-draw frame (checked).
  - The drawn string meets the nock while nocked.

## QC (work/qc_*.json)
- Bow against the body, left hand and forearm excluded:
  - 0 overlapping faces through the quiver draw, nock, draw and release;
  - after the loose the lower limb grazes the legs as the bow drops, at attack 2 f48–50 (6 and 173 faces) and attack 3 f98 (110 faces).
- Rest-roll string against the left forearm: centreline 4.7 cm.
- The cape is not in the bakes: it runs on spring chains in Unity (BonequillCape).
