# PASS ASHWYRM

bones=46 faces=199999 unw=0 clip_max=0.40079324682490325
- Kind: winged dragon custom skeleton
- Weighting: ARMATURE_AUTO; L/R suppress; 4-infl; **wing membrane rebind 2026-10-05**
- Caveat: auto wing placement; soft fold leftovers OK on extreme poses

## FIX 2026-10-05 — wingflap looked like membrane stretch / idle breath

**Root cause: B (weights wrong).** Not a missing-bone rig, not a missing-key clip.
- Rig has proper wing chains (14 bones): `wing_root → wing_arm → wing_forearm → wing_f1..f4` × L/R.
- Rebaked clip already keyed real flap: `wing_root`/`wing_arm` euler X ±~45° (quat amp ~0.76/0.65 in FBX). Tip bone travel ~0.86m.
- Heat weights had put outer wing mesh mostly on front **arm** bones (`forearm`/`hand`/`upperarm`) because auto arm bones over-span into wing space. Outer mean wing influence was ~0.17 (Emberfang ~1.0). Bones flapped; mesh stayed locked → preview looked like chest/tail breath only.

**Fix:** Geometric re-bind of outer wing verts onto `wing_*` bones; suppress arm/leg stealers. Soft leftovers OK.

**QC after:**
- Outer `|x|>0.35` mean wing ~0.94, arm ~0.06, frac wing-dom ~0.98 (was wing~0.17 / arm~0.60)
- Tip-vert mean travel ~0.76m (was ~0.11m); wing_z_mean range across cycle ~0.40m
- Preview: `handoffs/anim_previews_20261005/rebake/ASHWYRM_wingflap_preview.mp4`

**Dev tip-swap:** replace **both** `ASHWYRM_blenderig.fbx` (rest+weights) and `ASHWYRM_blenderig_wingflap.fbx` (clip+weights). Clip-only tip-swap is insufficient if Unity uses rest pack mesh. Backups: `broken_clips_backup/*_pre_wingweight.fbx`.
