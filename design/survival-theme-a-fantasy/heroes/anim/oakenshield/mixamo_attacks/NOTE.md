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

## Attack 2: "Bayonet Stab" also rejected (2026-10-18, 02:30 EDT)
Download: `OAKENSHIELD_mixamo_bayonet_stab_REJECTED.fbx` (Mixamo "Bayonet Stab", 99 f, uploaded on her mesh; md5 in CHECKSUMS).
The motion itself is right: braced low stance with the left foot leading, left foot steps 0.62 m forward f21-31, both hands
drive forward to full reach at source f39 (bake f46-48), back to the start stance by source f61. Probe bake: hips travel cancelled
(new `hold_hips_xy`, source shift up to 0.43 m removed), blend in 10 / out 12, spear on RightHand at her locked attack-1 grip.
Hips go side-on from -16 to -45 deg at the reach (her throw reaches -62).
- **Locked grip:** at the reach (bake f40-52) the spear points **124 deg away from the target**: the sharp thorn tip goes back
  through her body (500-600 body polygons intersected per frame, f36-52) and the leaf butt leads. In the braced stance it lies
  across her body, 80 deg off the target. Sheet: `OAKENSHIELD_bayonet_stab_probe_contact_sheet.jpg`.
- **Same grip, spear reversed end-for-end** (comparison only, not her locked grip): the tip leads, but 55 deg to her right
  of the target. The shaft misses the front (left) hand by 0.42 m and goes through her body (1250-1530 polygons, f36-52).
  Sheet: `OAKENSHIELD_bayonet_stab_reversed_grip_probe_contact_sheet.jpg`.
- Why: Mixamo poses the hands for a rifle. The line from the rear fist to the front hand points 30 deg off the target, like the
  source rifle. But her fist's thumb axis, which carries the spear, ends up about 96 deg off that line at the reach. Putting the
  shaft through both hands would mean re-aiming the right wrist by about 96 deg, which forces it. So nothing is wired: no runtime
  change, and attack 2 stays open. Spear numbers per frame: `work/scripts/bayonet_stab_spear_eval.py` on the probe blend.
