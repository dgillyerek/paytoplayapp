# ROWAN Meshy no-cape, NO-BOW body + rigid bow prop (Design, 2026-10-08)

HOLD: staging only. Nothing has been committed, pushed, PR'd or sent. Derek checks this in Game view before any merge.

- Source: `../meshy_nocape_20261008/`. It is untouched; the MD5s are unchanged.
- The bow prop comes from `../blender_rig_nocape_20261008/props/ROWAN_bow.fbx` (80758fce…). It is untouched.
- The Meshy mesh, skin, skeleton and clips are kept as delivered. The only changes are listed below.

## Files
| file | what |
|---|---|
| `ROWAN_meshy_nobow_rest.fbx` | Idle_02, frames 0–45 |
| `ROWAN_meshy_nobow_walk.fbx` | Walking, frames 0–25 |
| `ROWAN_meshy_nobow_attack.fbx` | Archery Shot, frames 0–120, **aim-fixed** |
| `ROWAN_meshy_bow_attack.fbx` | Rigid bow (`ROWAN_bow_rig`: `bow_grip` + `bow_nock`). The per-frame bake for the attack is in character space. |
| `ROWAN_meshy_arrow_attack.fbx` | Blue-fletched arrow with its per-frame object animation for the attack: hand → nock → flight |
| `props/ROWAN_bow_meshy.fbx`, `props/ROWAN_arrow_blue_fletch_meshy.fbx` | Static props at Meshy scale |
| `ROWAN_meshy_bow_meta.json` | Hand offsets (RightHand for rest/walk, LeftHand for the attack), string draw per frame, arrow windows, spawn and speed, aim-fix numbers |
| `textures/` | The 4 Meshy PNGs, byte-identical copies of `attack_zip/Meshy_AI_Azure_Ranger_biped/`. They are also embedded in the body FBX files. |
| `ROWAN_meshy_nocape_nobow.blend` | Everything in one file, with all 7 actions. Textures are relative to `textures/`. |
| `ROWAN_meshy_nobow_attack_sheet.jpg` | Rows: rear battle cam, side, 3/4. Columns are key frames. |
| `ROWAN_meshy_nobow_walk_sheet.jpg` | Rear, side and 3/4 views of the walk |
| `ROWAN_meshy_nobow_walk_attack_preview.mp4` | Rear battle cam, 24 fps: walk ×2 then attack (workbench shading) |
| `work/` | Scripts, the reimport check (`verify_reimport.json`) and the QC close-ups |

No zips.

## Units, fps, frames
- Units are **metres** in all files: FBX `UnitScaleFactor` 100, `axis_forward −Z`, `up Y`, no leaf bones.
  - The source FBX files were in cm (`UnitScaleFactor` 1).
  - On reimport, the body is 1.70 m tall with its feet at z = 0. The armature object is identity.
- fps is **24**, the same as the source (FBX TimeMode custom, 24).
- Frame numbers in this note and in the meta are **0-based**, matching FBX time (`frame = t × 24`).
  - Blender's importer shows them as +1, so the source attack imports as 1–121.
  - Clip lengths match the source exactly: 46, 26 and 121 frames.

## What changed vs the Meshy source
1. **The bow and bowstring are deleted from the body mesh.**
   - The bow and string vertices were found by exact geometric correspondence with the earlier full-res Meshy segmentation. The similarity fit has scale 0.8947 and a median error of 0.09 mm.
   - 1,628 faces were deleted (every face touching a bow or string vertex).
   - The 4 holes left at the right fist are closed with flat fan caps: 70 triangles plus 4 centre verts.
     - Each cap uses one solid rim texel. There is no blur or smear.
     - Each centre vert's weights are the average of its rim verts (top 4 influences).
   - Result: 29,551 tris and 14,635 verts. The source had 31,109 tris and 15,408 verts.
   - Every kept vertex is bit-identical in position to the source, and its weights are untouched.
   - The quiver and its blue fletching on the back are kept.
2. **The 2 m origin sphere (`Icosphere`) is removed.** Each body FBX holds only `output_unwrapped` and `target_character`.
3. **Aim fix, attack only.**
   - The root `mixamorig:Hips` location and rotation are re-keyed on every frame, rotated **−99.0° about world up**. The pivot is the hips' ground point at frame 0: (0.079, −0.112).
   - This is a rigid whole-body turn, so nothing is distorted.
     - All 87 other bones' local channels are identical to the source (max diff 1.6e-6).
     - Hand-to-hand distances change by less than 1e-6 m.
   - Shot yaw is measured as the right-fist→left-fist line, averaged over the full-draw hold (frames 68–75). It was **+99.0° before the fix** (toward Rowan's left; Dev's 97° was close) and is **0.0° after** (straight −Y = up-screen in the rear battle cam).
   - The shot pitch is still −11° (slightly downward). That is how the clip was authored, and it was not changed.
   - Rest and walk are not touched.
4. The actions are renamed `ROWAN_meshy_rest`, `ROWAN_meshy_walk` and `ROWAN_meshy_attack`. All 88 bones are kept, with the same names and order (including Bone_0xx and headfront). Object names are kept.

## Bow (rigid prop, never skinned to the body)
- Scale: the bow is scaled by s = 0.8947 to the Meshy body. Its overall length is 1.32 m with brace 0.091 m.
- Frame: origin at the grip, +Z is the long axis, −Y is the shot direction. `bow_nock` local +Y is the string draw in metres. Only the string verts follow `bow_nock`; the limbs are 100% `bow_grip`.
- Rigidity check (reimported FBX): the limb verts' pairwise distances vary by ≤ 6.5e-7 m across frames. The bow does not warp.
- **Rest/walk: RightHand.** `offset_in_RightHand_rest_walk` is the Meshy bind placement of the look SoT bow, so it sits where the Meshy bow was.
- **Attack: LeftHand for the whole clip (frames 0–120).** The Archery Shot holds the bow in the LEFT hand: the left arm extends and the right hand draws to the face.
  - `offset_in_LeftHand_attack` is defined at the full-draw hold: grip at the left-fist centre, −Y along the shot line, +Z up.
  - The baked `bow_grip` matches the LeftHand fist to within 1e-6 m.
- String draw per frame is in the meta:
  - 0 until frame 45.
  - From frame 45 to 47 it eases in to the right-hand position.
  - From frame 47 it follows the right fist, up to **0.85 m at full draw** (frames 64–75).
  - Release snaps it to −0.02, then +0.008, then 0.

## Arrow (blue fletching)
- Shape and length:
  - The tip points −Y and the origin is at the nock.
  - Its thickness is scaled by s. It was lengthened to **1.056 m** so it spans the clip's very long draw (nock to grip about 0.94 m).
  - Same materials as before, with **blue fletching**.
- Frames 25–27: scales in inside the right hand while the hand is above the head (the clip's reach for an arrow).
- Frames 25–44: held in the RightHand (`offset_in_RightHand` in the meta).
- Frames 45–75: nocked on the string, along the bow axis.
- **Release: frame 76 (t = 3.167 s; Blender-import frame 77).** At this frame the right hand jumps away from the string line and the fist distance spikes.
- Flight: from frame 76 it flies horizontally along −Y at 12.5 m/s (14 × s). That is **toward the top of the screen in the rear battle cam**. The spawn point is in the meta.

## Leftovers / caveats
- **Meshy skin bleed (pre-existing).** When the arms are raised, torso and belt pieces stretch into dark shards under the LEFT forearm. About 60 verts at the left torso/hip are weighted to LeftArm. A similar flap follows the right arm at frames 25–27.
  - This is in the source clip too: see `work/source_meshy_attack_f68_leftarm_skin_shards.png`.
  - It was not fixed, because the brief says not to re-skin. A weight clean-up on those verts would remove it if Derek wants it.
- **Clip transitions.**
  - The bow switches from RightHand to LeftHand instantly at the idle/walk→attack transition and back at the end. The clip has no hand-over.
  - The attack's first and last frames are also turned −99° relative to idle/walk, the same as the Unity body turn did.
- **Draw length.** The draw is very long (0.85 m on a 1.32 m bow), because the Mixamo hands sit about 1 m apart at full draw. The string makes a deep V, and the arrow had to be lengthened to 1.06 m.
- **Fists.** The right fist keeps the Meshy closed-fist shape with flat caps where the bow passed through. The left fist was modelled empty and closed, so the bow grip passes through it.
- **Arrow pitch.** The arrow's pitch snaps from the clip's −10° nock angle to horizontal flight at release.
- **Walk.** The bow is rigid in the right hand, so it follows the Meshy wrist roll: it is about horizontal at frames 0 and 12, and may brush the legs.
