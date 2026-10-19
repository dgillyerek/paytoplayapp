# Vespera attacks 2 and 3 (Mixamo, 2026-10-18). HOLD: Derek Game-view check before any merge.

Sources (raw downloads kept here, md5s in CHECKSUMS.md5). Mixamo animated her own uploaded no-cape skeleton: 16 bones, three
roots (Spine1, LeftUpLeg, RightUpLeg), no Hips. Mixamo's hips motion sits on the armature object, so the source is read in world space.
- `VESPERA_mixamo_standing_1h_magic_attack_01.fbx` (70 f): the right hand winds back, then lashes forward (full reach at source f30-37).
- `VESPERA_mixamo_standing_2h_magic_area_attack_02.fbx` (99 f): both hands rise and gather, drive down and forward in a crouch (f40-46), then burst up and out (f52-58).

## Bakes (`work/scripts/vespera_mixamo_retarget_20261018.py` + `params_*.json`, from `blender_rig_nocape_20261009/VESPERA_nocape.blend`, not saved over)
- **`VESPERA_blenderig_attack2_shadow_lash.fbx`** (102 f, 0-101): wind-up (source f1-22) played 1.5x slower, then the lash at normal speed. The right hand is near full reach at **f50** (contact), and furthest out at f56.
- **`VESPERA_blenderig_attack3_hex_rain.fbx`** (140 f, 0-139): the gather (source f1-40) played 1.5x slower, push down f75, then the arms burst up at **f84**. Three hexes land at f84, f88 and f92, 0.3 m apart across the target.
- Both wind-ups are 1.5x slower, as her approved conjure (`WindupScalePercent = 150`).

## Method
- Bind: the spine and legs of the Mixamo skeleton bind exactly as hers (armature space). The arms are T-pose, so each bone's reference is her bind turned by the minimal rotation onto the source bind bone.
- World rotations come from the source each frame. The roots take the source root heads.
- Stance: the right (trailing) foot keeps its rest spot. The clip stance is side-on with the left foot forward, so the left foot steps 0.36 m forward on the blend-in. Two-bone IK steps run on the 10-frame blend-in (left first) and the 12-frame blend-out (right first).
- Feet stay at 0.245-0.28 m ankle height (grounded) throughout the clip.
- Spine1 horizontal drift across each clip is 0 (start and end coincide). The in-clip lean is kept.
- No orb or props. The conjured orb belongs to attack 1 only. Hits are purple (`VillainShadowLash`, `VillainHexRain`).

## QC
- Contact sheets in this folder (side, 3/4 front, close-up). Hand/foot/Spine1 paths are in work/qc_*.json.
- No mesh-overlap pass was run on these two (her approved attack has one); eyeball the hair and the forearms against the skirt on the crouch (hex rain f60-75).
