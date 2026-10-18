# Oakenshield attacks 2 and 3 (Mixamo, 2026-10-18). HOLD: Derek Game-view check before any merge.

Sources (raw downloads kept here, md5s in CHECKSUMS.md5; Mixamo auto-rig of OAKENSHIELD.fbx, same 20 mixamorig names):
- `OAKENSHIELD_mixamo_standing_2h_magic_area_attack_02.fbx` (99 f): both hands rise, then drive down to the ground in a lunge, then recover.
- `OAKENSHIELD_mixamo_stabbing_REJECTED.fbx` (80 f): kept for the record only; not wired (see below).

## Attack 3: root snare (wired)
- **`OAKENSHIELD_blenderig_attack3_root_snare.fbx`** (121 f, 0-120, take "Scene"): the whole clip with 10 frames of blend-in and 12 frames of blend-out.
  - Left foot steps forward first in the blend-in. The hips start side-on (-58 deg at f10, the same as her spear throw's -62 deg wind-up) and square up into the cast (-7 deg at f40, about 0 at f52), as her approved throw squares at release.
  - At f52 both palms drive down at about 0.85-0.9 m in a deep lunge (hips 0.63 m). In battle the hero-blue roots (`BattleImpactKind.HeroRootSnare`) burst up out of the floor under the target on f52.
  - Empty-handed: the thorn spear exists only in attack 1 (no idle props).
- Method: `work/scripts/mixamo_retarget_20261018.py` + `params_attack3_root_snare.json` (the archer pipeline generalised: bow/arrow only when a grip is given, optional held prop). Mixamo T-pose bind matched to the A-pose rig bind, body bones only, hips' horizontal drift removed (net 0; in-clip shift up to 14 cm kept), feet stepping in and out on the blends.

## Attack 2: "Stabbing" rejected
Probe: Stabbing retargeted the same way with the thorn spear bound to RightHand at her attack-1 grip (the f10 binding of `OAKENSHIELD_blenderig_attack.fbx`: 7 cm up the hand bone; `work/scripts/oak_spear_rhand_local.json`). Sheet: `OAKENSHIELD_stabbing_probe_contact_sheet.jpg`; spear tip path: `work/qc_stabbing_probe.json`.
- The rear hand winds out to the side, then hooks across and forward. At its furthest reach (f40-56) the hand bone points forward, but her grip carries the spear along the hand's thumb axis, so the spear stands **straight up** (tip direction z 0.93-0.99), like a staff held out. It reads as a punch with a staff, not a forward spear thrust. Rotating the spear in the hand would break her locked grip, so it is not wired.
- Wanted instead: a two-handed bayonet/spear thrust, where the rear fist drives forward with the thumb leading. Mixamo description to search: **"Advancing And Stabbing With Rifle Bayonet Or Spear"** (enable In Place if offered; the bake removes travel either way). The search phrase comes from a Mixamo catalogue listing; the clip's display title is unverified.
