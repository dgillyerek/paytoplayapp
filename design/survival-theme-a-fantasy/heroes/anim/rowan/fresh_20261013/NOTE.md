## v2 fixes (2026-10-14, after Derek's Unity review of 8d638c7)

- **Arm through the chest on the raise:** I rebuilt the attack so both hands follow keyed waypoints in the turning torso frame, which means the 70° chest turn carries the arms with it instead of sweeping the chest into them.
  - Draw arm: at f3 the elbow leads out to the side, at f6 it goes up outside the shoulder line, at f8 the hand comes in at head height, and at f10 it reaches full draw at the right jaw. Near the jaw the wrist is flexed (swing only, no roll) so the fingers point along the arrow instead of at the neck. At full draw the elbow is out to the side, about 12 cm above the shoulder joint.
  - Bow arm: it swings out to the side first, then straight out from the shoulder. Its roll is solved so the bow stands within 13.19° of vertical.
  - The return to rest mirrors the raise.
  - Penetration test (`work/apose_20261014/pen.py`), run on every frame and half-frame from 0 to 30 in all three clips. An arm vertex counts as inside if it is inside the posed torso cross-section hull and behind the nearest torso face by more than 4 mm beyond where it sat in the bind, or behind a coat-tail face.
    - Excluded: arm vertices within 12 cm of the shoulder joint, and the arm/torso skin-seam ring, which is the 2 face-rings that share faces with the torso ({'L': 110, 'R': 134}).
    - Result: **0 in every sample** (`pen.json`, `pen_rest_walk.json`). A new test enforces it.
  - To get there:
    - Underarm sleeve: shares up to 90% with the arm-chain Shoulder bone (`rig.py`).
    - Rest hands: moved to (±0.39, −0.04, 0.95).
    - Walk back-swing: goes out around the coat tails.
- **Bow through the hand:** the 20-bone rig has no finger bones, so `fist.py` sculpts the left hand into a fist at bind.
  - Every vertex past the knuckle line (0.08 m from the wrist, 1170 vertices) wraps around a grip axis across the palm.
  - The grip is cleared to a radius of 0.024 m: 69 vertices were pushed off it.
  - The bow grip sits on that axis, at the palm centre, with the string toward the wrist.
  - The hand is 100% LeftHand and the bow is rigid on LeftHand, so the fist and the grip hold in every frame of rest, walk and attack.
  - Checks on every frame (densely sampled bow surface vs hand, and hand vertices vs bow): {"rest": {"bow_in_hand": 0, "hand_in_bow": 0, "bow_in_body": 0, "bow_in_coat": 0}, "walk": {"bow_in_hand": 0, "hand_in_bow": 0, "bow_in_body": 0, "bow_in_coat": 0}, "attack": {"bow_in_hand": 0, "hand_in_bow": 0, "bow_in_body": 0, "bow_in_coat": 0}}.
- **Roll check:** {"max_hinge_mismatch_deg": 0.0, "max_abs_forearm_twist_deg": 0.0, "max_abs_hand_twist_deg": 0.0, "walk_Left_max_hand_step_deg": 3.03, "walk_Right_max_hand_step_deg": 3.58, "attack_Left_max_hand_step_deg": 20.99, "attack_Right_max_hand_step_deg": 31.11}.
- **New sheets:**
  - `sheets/ROWAN_fresh_attack_raise_strip.jpg`: f0 to f10, every frame, front and side.
  - `sheets/ROWAN_fresh_bow_hand_closeup.jpg`: rest, walk and attack, three angles.
- **Pipeline order:** insp → dec → uvcol → seg → split → rig → fist → coat → anim → pen → sheet → mont/strip/bowhand → export.

# Rowan fresh rebuild (2026-10-13), HOLD
## A-pose rebuild (2026-10-14, #64) — supersedes the sections below

- **Source:** Derek-approved A-pose Meshy model `ROWAN_apose_meshy_texture.glb` (1,783,138 faces, md5 in `source/MESHY_APOSE_SOURCE.md5`, not committed), decimated to 200,000 faces with the Meshy texture kept. Pipeline in `work/apose_20261014/` (insp, dec, uvcol, seg, split, rig, coat, anim, sheet, mont, export).
- **Rig:** same 20-bone Generic rig. Hands and fingers 100% on Hand (hard wrist plane), lower forearm 100% on ForeArm, elbow blend ±3 cm, no arm vertex weighted to Hips or the spine (checks in `rig.json`: arm_verts_with_torso_or_hips_weight 0, hand_verts_not_100pct_hand 0), face 100% on Head, belt 100% on Hips. The right belt pouch had been caught in the right-arm part and was moved back to the torso.
- **Coat tails (Derek's cloth rule):** split below the belt into `ROWAN_coat.fbx` (17,428 faces) on its own armature: anchor `coat_Hips` plus 7 chains × 3 bones (B, BL, L, FL, FR, R, BR), all parented to Hips, never the legs. Runtime `RowanCoat` + `ClothSpringRig` (damping 0.45, 35°, solver velocity clamp; leg/hips/forearm/hand colliders). The body clips move body bones only. The split looked clean, so the shorter-coat fallback was not needed. `ClothSpring.cs` / `ClothSpringRig.cs` are byte-identical copies from the unmerged Bonequill branch (same GUIDs).
- **Clips:** rest = arms down by the sides from the A-pose; walk 1 s in place; archer attack: torso side-on (70° spread over hips and spine), bow arm straight out from the shoulder, draw hand at the cheek, loose f14.
- **Arm roll:** after the aim the full Arm/ForeArm basis maps the rest elbow hinge onto the IK hinge (explicit pole roll). Both Hands are keyed every frame. Every attack frame was checked: hinge mismatch 0, forearm twist 0, hand twist 0 (`anim.json`, `upperarm_twist.json`).
- **Props:** one bow (`ROWAN_bow.fbx`), rigid on LeftHand in every clip. Nothing on his back; the quiver was removed.
- **Sheet metrics:** {"rest": {"face": 0.0, "band": 0.0, "arm_stretch": 20.076, "coat_in_legs": 0}, "walk": {"face": 0.0, "band": 0.5382, "arm_stretch": 43.4339, "coat_in_legs": 47}, "attack": {"face": 0.0, "band": 8.5044, "arm_stretch": 154.6727, "coat_in_legs": 0}}; hand gap minimum (cm) {"rest": {"L": 7.48, "R": 3.42}, "walk": {"L": 5.39, "R": 1.76}, "attack": {"L": 6.56, "R": 0.02}}. The attack R value is the draw hand at the cheek, as intended.
- **Caveats:** the bowstring stays at brace height (static prop). The walk spring preview shows 47 coat verts more than 2 cm inside a leg capsule; Unity colliders push these out. The attack neck band moves up to 8.5 cm with the 70° twist.


Derek approved the hero-blue front/back look (`source/ROWAN_FRONT_BACK_BLUE_LOCKED.jpg`). Design made the Meshy model from it (`source/MESHY_SOURCE.md5`; GLB 1.6M tris and zip not committed). This replaces the failed #54/#50/#45/#35 packs. The game setup and the bow attack idea come from `cursor/rowan-nocape-20261008`.

- **Model (`work/dec.py`):** duplicate verts merged, decimated (collapse) to 199,999 faces / 99,753 verts, scaled to 1.80 m, feet on 0. Meshy UVs and maps kept (base colour, normal, metallic-roughness 2048²).
- **Bow on his back:** Meshy fused the slung bow into the coat surface. Deleting it left see-through holes in the back of the coat (`work/nb5_views.png`), so it stays on his back, 100% `Spine2` (3,985 verts). In the attack, a separate hand bow (`props/ROWAN_bow.fbx`) shows from f4 to f24, so **two bows are visible during the draw**. The fix is a Meshy pass from a bow-less image, or a hand repaint/patch.
- **Rig (`work/rig.py`, Blender 5.2.2):** 20 mixamorig bones (no ToeBase), max 3 influences.
  - Face, skull and hair are 100% `Head` above a tilted plane (1.50 m at the front to 1.53 m at the back), with a ±0.8 cm band.
  - Tunic, coat flaps, belt and pouches: everything in the torso part below 1.02 m is 100% `Hips`. Zero leg weight (`loincloth_with_leg_weight: 0`).
  - The short capelet rides the spine chain and the Shoulder bones (the part over the upper arm follows the arm).
  - Arms are ripped from the coat/torso below the armpit (1,539 faces), and the legs from the coat below the hip ramp (1,168 faces), so nothing webs when the arms rise (`work/av_b.png`).
- **Props (`work/props.py`):** `ROWAN_quiver.fbx` is a leather quiver with 7 fletched arrows, rigid on `Spine2` over the right shoulder in every clip (idle prop that stands in for the attack track). Also `ROWAN_bow.fbx` (wooden recurve, grip at the origin) and `ROWAN_arrow.fbx`.
- **Actions (`work/anim.py`):**
  - Rest is the bind pose with arms hanging by the sides.
  - Walk is in place, 1 s, 30 fps, ankles 22–23 cm apart, head forward with a tiny nod.
  - Attack: bow raised and nocked at f6. Full draw at f10, with the chest turned 24° and the left shoulder leading, and RightHand at the jaw by IK. Loose at f14 (14 m/s, hero-blue trail), recoil at f17, rest at f30. Feet planted, head forward.
- **QC (`work/sheet.json`):**
  - Face/skull deviation from the rigid Head = 0.000 cm in every frame.
  - Neck–head band max: 0.55 cm in the walk, 3.55 cm in the attack (neck counter-turn).
  - Arrow direction in Unity: (−0.05, −0.01, 1.00).
- **Known limits:**
  - The coat is rigid on Hips, so the thighs pass through the front flaps at full stride.
  - A few coat tatters that sat behind the fists show at the hips when the arms rise.
  - The bow stave crosses the lower face in the front view at full draw.
- **Sheets:** `sheets/ROWAN_fresh_{rest,walk,attack}_sheet.jpg`, each with front, face close-up, side and back rows.
- **Unity:** ThemePack `heroes/3d/rowan/ROWAN_blenderig*.fbx`, Generic (`animationType: 2`). Open it with Survival → Rowan Demo.

## Review fixes (2026-10-13, after Derek's Unity review of #64)
- **Hands stuck to hips:** the Hand/HandEnd joints were placed about 5 cm off the real hands, so the surface-distance arm split left the hands and fingers on Hips. Fixed the joints and used the same split as Bonequill's fingertip fix (per-side surface distance to the arm and forearm segments, with an x-gap of 0.222 m from the coat). Hands and fingers below the wrist are now 100% on the Hand bone, and the wrist and lower forearm are 100% on ForeArm. Faces that joined the arm to the torso were cut (807). Checked in rest, walk, attack, and a 75° arm-raise test (work/pose2.png, work/hv_c.png).
- **Attack shoulder:** re-baked as an archer stance. The body turns side-on (70° spread over hips and spine), and the left bow arm extends straight from the shoulder toward the target, clear of the torso. The draw hand anchors on the right cheek by 2-bone IK with the elbow out to the side. Front, side and back views at f10 full draw are in work/v3_f10zoom.png. Face drift is 0. The neck-to-head band moves about 11.2 cm in attack (counter-twist of the neck) versus 0.55 cm in walk.
- Known issues: the back bow (Meshy mesh) and the hand bow are both visible during f4–24. The coat flaps still graze the thighs at the walk extremes.

## v3 bow hand on real finger bones (2026-10-14, Derek: the sculpted fist stretched the fingers into long hooks)
- **Hand:** the sculpted fist (`fist.py`) is gone. The left hand is the original A-pose Meshy hand (`rig_nofist.blend`, compare the top row of `sheets/ROWAN_fresh_bow_hand_closeup.jpg`).
- **Finger bones (`work/apose_20261014/fingers4.py`, same approach as Bonequill #63 `1347972`):** `mixamorig:LeftHandFingers1/2/3` (one 3-segment chain for the four fingers, knuckle line 13.5 cm down the hand) and `mixamorig:LeftHandThumb1/2`, all parented to `LeftHand`. Finger and thumb weights blend smoothly along each chain (200 smoothing passes). The palm stays on `LeftHand`. The rig now has 25 bones.
- **Grip:** keyed in rest, walk and attack. Fingers curl 15/35/20°, thumb 60/20°, all about the knuckle axis. The bow's long axis lies along the knuckle axis, and the grip sits in the curled fingers against the palm, with the thumb closing over it. The grip was picked from a full grid search: the best wrap with zero penetration, closest to Bonequill's angles.
- **Checks on every half-frame (`work/apose_20261014/grip_halfframe.json`, 61 samples per clip):**
  - Wrap of the hand around the grip is at least 270° (rest 280°, walk 270°, attack 270°).
  - Hand-in-bow, bow-in-hand, bow-in-body and bow-in-coat are all 0. The closest hand vertex is 2.21 cm from the grip axis (grip radius 2 cm).
  - The finger centreline length changes by 0.5% (the same measure as Bonequill), and the bone chains do not change length.
  - Edge stretch on the finger and thumb skin: 99th percentile 1.28, worst single edge 2.02. Surface paths on the palm side shorten by 5–7% where the skin compresses inside the curl. Nothing gets longer.
- **Arms:** arm-around-chest raise penetration is still 0 on every frame and half-frame (`pen.json`, `pen_rest_walk.json`). Forearm and hand twist stay at 0 (`anim.json`). The bow stands 18° from vertical at full draw.
- **Unity:** the FBXs, checksums, `RowanAttack` bow, arrow and idle-bow frames (from `sheet.json`), and `RowanMotion.BoneCount` = 25 are all updated. HOLD for Derek's Game-view check.

## v4 bow on the hand bone, narrower walk, attack step (2026-10-15, Derek: bow floats beside the fist, legs too far apart in the walk, left leg should step forward as he fires)
- **Bow in hand (same fix as Bonequill #63 `1375657`/`5d660bf`/`90f128d`):** the bow is now a child of `mixamorig:LeftHand` at the Blender grip (`BlenderBoneGrip`, bone space, mirror-X import conversion, scale checked against the `LeftHandFingers1`/`LeftHandThumb1` heads). Rest shows the keyed carry (attack f0) so the fingers curl (`RowanActor` `restAtAttackStart: true`). Grip numbers: `work/apose_20261014/grip_unity.json` (`gripunity.py`); the grip is 0.82 cm from the centre of the circle the closed hand wraps on every half-frame.
- **Bow roll (`bowroll.py`, searched in `rollsearch.py`):** string away from his body when carried (230°), eased to face him f2–f8, held through the f14 release to f17, back by f24 (40°). String clearance on every frame and half-frame (`string_clearance.json`): body incl. bow hand and forearm 6.4 cm, coat 17.7 cm, bow-arm bones 2.95 cm minimum; only the draw hand is exempt, f8–f14.
- **Walk:** both legs adducted 2.9° with the sole kept flat. The ankle track goes from 31 cm to about 23.5 cm (≈24% narrower, close to the 20 cm hip joints). The stance foot drifts sideways by at most 2 mm. In the spring preview, coat vertices inside the leg capsules dropped from 47 to 40 (this grazing at the walk extremes was already there; Unity's leg colliders push the chains out).
- **Attack step:** the left foot lifts at f2, plants 29 cm toward the target at f8 (6 frames before the release), holds through f19 and is back at rest by f26. The hips move 12 cm forward and 3 cm down onto it (f4–f11). Both feet use 2-bone IK on fixed world targets, so the right foot never moves (0 mm) and the planted left foot stays put. f0 and f30 are the rest pose. Arm vs chest/coat is still 0 (`pen.json`), and coat in legs is 0 in the attack.
- Sheets: `sheets/ROWAN_fresh_bow_grip_closeup.jpg`, `ROWAN_fresh_walk_narrow_compare.jpg` and `ROWAN_fresh_attack_step_strip.jpg`. HOLD for Derek's Game-view check.
