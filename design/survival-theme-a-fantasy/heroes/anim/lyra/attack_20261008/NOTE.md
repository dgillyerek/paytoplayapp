# LYRA fitted: attack add-on (Design, 2026-10-08): staff spell cast

HOLD: staging only. Nothing committed, pushed or PR'd. Derek must check it in Game-view before any merge.

Built on `heroes/anim/lyra/blender_rig_tighten_20261008/LYRA_tighten_blenderig.blend`. The rig blend was opened and never saved.

## Clip
- `LYRA_tighten_blenderig_attack.fbx`: f0–f30 @30fps. Authored from identity rest; f0 = f30 = rest. Exported with the `hum` profile.
- Timeline: same timing family as lyra attack_20261007 (efd19534).
  - f8 anticipation: staff pulled back and up, torso turned right, left hand forward.
  - **f12 strike / release:** staff thrust forward, tilted 27°.
  - f19 follow-through.
  - f30 rest.
- **Staff stays parented to `mixamorig:RightHand` for the whole clip**, with a constant offset (meta).
- `LYRA_arcane_bolt_vfx` spawns at the staff crystal at f12. It grows from 40% to 100% over 3 frames and travels along exactly −Y at 9 m/s, **toward the top of the screen** in the rear battle cam.

## Props / VFX
- `LYRA_staff.fbx` (14,048 faces): the Meshy staff split from the body. Origin is at the grip, +Z runs toward the crystal.
- `LYRA_arcane_bolt_vfx.fbx`: emissive blue bolt with a gold ring.
- `LYRA_tighten_attack_props.blend`: both props.

## Reimport QC (`work/verify_attack.json`)
- 22 bones, same names and order as rest. Faces 200,000. Frames 1–31.
- **Hips at the first frame vs rest: 0.0°. Max over all frames: 0°.** End-pose deviation 0.0°.

## QC media
- `LYRA_tighten_attack_sheet.jpg`
- `LYRA_tighten_walk_attack_preview.mp4`

## Caveats
- The hand keeps the Meshy fist. The strike reads as a staff thrust and bolt, not an open-palm cast.
- Fist caps are the same as noted in the rig pack.

## Re-export after Derek's rig edit (2026-10-08)
`LYRA_tighten_blenderig_attack.fbx` was re-exported from Derek's edited combined blend: 90385eda → 64d8b8d4. The attack keys are unchanged. Because the right elbow and wrist joints moved, the same keys now put the staff crystal at f12 17 cm closer to the body midline and 5 cm higher. The bolt spawn in `work/attack_meta.json` was updated. See `../blender_rig_tighten_20261008/NOTE.md`.

## Attack staff in Unity (2026-10-08)
Unity no longer loads `LYRA_staff.fbx` for the attack. The rest/walk staff (`LYRA_staff_rest.fbx`) stands in for the attack's `LYRA_staff` track: it has the same mesh and the same grip on RightHand. One textured staff now serves all three clips. The old attack copy rendered white because its imported material never got the rig's texture atlas. The file is kept, unchanged.

## Rebuild on Derek's second rig edit (2026-10-08 22:45, e8b050d)
`LYRA_tighten_blenderig_attack.fbx` 64d8b8d4 → 53fd21bb. Derek moved the hips, knees, ankles, left elbow/wrist and right hand, and removed both toe bones. The attack was retargeted from a995489 by world-space rotation from rest, so the cast reads the same. The staff wrist path is within 1.6 cm and the staff crystal path within 1.5 cm. The bolt spawn was re-measured at f12: (-0.154, -0.660, 1.912). See `../blender_rig_tighten_20261008/NOTE.md`.

## Staff clear of the body, natural wrist (2026-10-09)
Derek, Game view on desktop: "her staff pushes through the body on attack and her wrist bends in an unnatural way."

- **Penetration before (e1c7d36):** f9–f23 the lower shaft went through the right thigh (up to 8.5 cm deep at f18) and f20–f23 through the right knee/shin (up to 6.8 cm at f21); f11–f18 it also cut 1–2 cm into the right forearm. The crystal and head never touched the head or hair (closest about 11 cm).
- **Wrist before:** the staff wrist bent up to 119° away from Derek's rest grip (f15). That was about 99° of bend around the shaft, about 40° sideways (84° at f10) and 64° of twist. In the wind-up at f6–f8 it was 52–76°, mostly sideways.
- **Fix:** only RightArm, RightForeArm and RightHand keys changed, solved per frame against the real posed mesh. The staff angle now comes from the shoulder and elbow. During the cast the butt leans out past her right hip, and the crystal sits about 9 cm further to her right. Timing, f12 release, crystal height and forward reach, hips, legs, spine, head and the left arm are unchanged. f0 and f30 are identical to rest.
- **After:** no staff penetration on any frame; minimum clearance 3.7 cm. Staff wrist at most 25° total (at most 15° around the shaft, 16° sideways, 12° twist). The elbow stays a hinge (at most 6° off-plane), forearm twist at most 56°, upper-arm twist at most 50°. Left wrist unchanged (0° from rest).
- **Bolt:** re-keyed at the f12 crystal: (-0.246, -0.659, 1.902), was (-0.154, -0.660, 1.912). Unity `LyraAttack` staff and bolt frames regenerated; calibration unchanged because the bones are unchanged.
- **FBX:** attack 53fd21bb → 835b0502. Rest, walk and both staff FBXs are byte-identical.
- **Scripts:** `work/scripts/staff_clear_*_20261009.py`. QC: `work/staff_clear_before_e1c7d36.json`, `work/staff_clear_after.json`.

## Two-handed side-on cast (2026-10-09, replaces the one-handed cast)
Derek: re-author the attack as a natural two-handed cast. The body turns sideways, both hands are on the staff, the crystal points forward, and the shoulders must not pull out or stretch the armpit skin.

- **New attack (31 frames, release f12):** f0–f8 hips turn 52° (spine adds about 21° more at the chest; neck and head counter-turn so she keeps looking at the target). She drops 4.5 cm into a soft knee bend and pivots on the balls of both feet (toes fixed, heels swing 8–9 cm, no sliding). The right hand brings the staff up level-forward; the left hand comes in palm-first and closes on the shaft about 25 cm above the right hand at f8. f9–f11 is a small body pull-back (hips 3.5 cm back, 6° lean back), f12 is the thrust (hips 3.5 cm forward, 7° lean in) and the bolt leaves the crystal. f13–f20 hold and settle; f21–f23 the left hand opens and comes off the shaft; she recovers to rest by f30. f0 and f30 match rest (max bone-matrix difference 4e-6).
- **Shoulder skin stretch** (armpit blend zone, edge length ÷ rest; worst frame): right p99 2.66 → 1.50, mean |stretch| 0.188 → 0.093. Left p99 1.75 → 1.53. The left mean goes up from 0.075 to 0.115 because the left arm now lifts to the shaft; the before value had the left arm hanging. Upper-arm swing: right 55° → 10.5°, left 28° → 34.5°. Upper-arm twist: right 50° → 8.4°, left 2° → 8°. Clavicles unkeyed (0°).
- **Elbows:** hinge only (right at most 92° bend, left 38°). Forearm twist is at most 56° on the right and 17° on the left.
- **Wrists:** right at most 23°, left at most 19.5° from rest (limit 25–30°).
- **Clearance:** no staff/body triangle intersections on any frame. Closest non-hand body part to the staff: 16 cm (left forearm/torso during the hold), right leg at least 21 cm, head/hair never among the closest regions. Before, the shaft came within 3.5 cm of the right thigh.
- **Grip:** the right hand keeps Derek's staff offset. The left grip axis sits on the shaft centre line (0.3 mm) f8–f20. The fingers press at most 6 mm into the shaft surface on hold frames and 9 mm on the close/open frames (f7, f21).
- **Bolt:** re-keyed at the f12 crystal (-0.214, -0.916, 1.450), was (-0.246, -0.659, 1.902). It still travels -Y (character forward / Unity +Z, toward the enemy) at 9 m/s, whatever the body turn. Unity `LyraAttack` staff and bolt frames regenerated. Calibration is unchanged because the bones and rest are unchanged.
- **FBX:** attack 835b0502 → 2d6f799e (Design + ThemePack). Rest e4898881, walk 7c5b1577 and both staff FBXs are byte-identical. The bones, rest and walk actions are unchanged in the blend.
- **Not refreshed:** `LYRA_tighten_attack_sheet.jpg` and `LYRA_tighten_walk_attack_preview.mp4` still show the older cast.
- **Scripts:** `work/scripts/twohand_*_20261009.*`. QC: `work/twohand_verify_before_265f536.json`, `work/twohand_verify_after.json`.

## Left turn, low left-hand grip (2026-10-10, replaces the side-on two-handed cast)
Derek: turn her left, the left hand grips lower on the staff, the staff points forward, and the legs stay straight and natural.

- **Body:** at the hold, the hips turn 33° left and the chest 45° (was 52° and about 73°). Feet stay planted: toes move under 0.5 cm and she pivots on the balls of the feet, as before. The left thigh-to-shin twist stays within 3° of rest, and the right knee stays straight.
- **Grip:** the right hand keeps Derek's staff offset. Between f8 and f21 the left hand sits about 61 cm LOWER on the shaft than the right hand, toward the butt (before, it was 25 cm above). On hold frames it is 1 cm from the shaft centre line and the fingers press up to 7 mm into the surface.
- **Staff:** from f9 to f15 the crystal leads forward toward the target. The staff points 22° left of straight ahead and is tipped up 17–19°. The bolt is re-keyed at the f12 crystal (-0.166, -1.055, 1.268), was (-0.214, -0.916, 1.450). It still travels straight forward (-Y, Unity +Z) at 9 m/s.
- **Shoulders and wrists:** right armpit p99 stretch is 1.86 (was 1.50), and left is 1.25 (was 1.53). The right wrist is at most 25°, the left 12°, and the upper-arm twist at most 4°. Clavicles move up to 12° on the right and 5° on the left.
- **Clearance:** no staff/body intersections from f6 to f24 (the whole cast and hold). In two swing frames the butt of the staff grazes her left leg: about 1 mm at f5 and up to 2 cm into the left boot at f25.
- **Changed:** only the `LYRA_tighten_attack` and `LYRA_arcane_bolt_attack` actions. Bones, the rest and walk actions, the staff offset, the rest and walk FBXs and both staff FBXs are byte-identical. f0 and f30 match rest. Saved in Object Mode with the walk active.
- **FBX:** attack 2d6f799e → 64f42e00 (Design + ThemePack). Unity `LyraAttack` staff and bolt frames regenerated. Calibration is unchanged.
- **Scripts:** `work/scripts/lowgrip_*_20261010.*`. QC: `work/lowgrip_verify_after.json`, `work/lowgrip_legs_after.json`.
