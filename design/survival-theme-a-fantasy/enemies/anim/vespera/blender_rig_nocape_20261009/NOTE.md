# VESPERA: no cape (2026-10-09)

Derek's cloth-rule fallback after the split cape still read badly in motion (same call as Rowan no-cape, #50):
the cape/cloak is removed, the body is re-rigged on a clean 22-bone skeleton, and rest / walk / attack are re-authored.
The cloth-split pack (`clothsplit_20261007/`) is unchanged and stays in git history.

**Single source blend (rig + all three actions):** `design/survival-theme-a-fantasy/enemies/anim/vespera/blender_rig_nocape_20261009/VESPERA_nocape.blend`
(Blender 4.3.2; objects `VESPERA_rig`, `VESPERA_body`; actions `VESPERA_nocape_rest`, `VESPERA_nocape_walk`, `VESPERA_nocape_attack`, 0-30 @ 30 fps).

## What was removed
- `VESPERA_cloth` object (56,542 faces): the simulated lower cape and skirt strips, plus all 28 cloth bones (8 chains `skirt_*`, `cape_*`).
- From the body mesh: the upper cape sheet still welded to the back and shoulders (11,647 faces), stray cape ornaments/spikes (1,121 faces) and 53 loose floating pieces (2,161 faces).
- Unity: `VesperaClothSplit` spec, its tests, and the ClothSpringRig springs + leg colliders on Vespera's actor. (The generic ClothSpring code stays for other characters but Vespera no longer uses it.)

## Holes, fills and paint (FLAG for Design look-paint pass)
- 2 closed holes were filled with real triangles using the surrounding UVs/paint: right shoulder back (20 faces, centre x -0.31 z 1.30) and front right hip (2 faces, x -0.22 z 0.90).
- Where the cape was cut free, the skirt flaps, hip sides and shoulder edges are single-sided (open inner surfaces). 3,568 faces whose back side is visible from outside (plus a one-ring border, 5,276 faces) got a flipped twin face with the **same UVs, paint and weights**, so the inside of the garment shows the adjacent paint instead of a see-through hole under normal back-face culling. By region: skirt/hip band 2,098, torso/shoulders 3,140, head/hair 38.
- **Design flag:** those inner faces (skirt-flap insides, hip sides, shoulder edges) reuse the outer paint; they read as dark fabric but were never painted as lining. No body skin under the cape was missing; the back reads as armour/hair from the original paint.

## Rig and weights
- 22 mixamorig bones (Hips, Spine, Spine1, Spine2, Neck, Head, L/R Shoulder-Arm-ForeArm-Hand, L/R UpLeg-Leg-Foot-ToeBase), joints measured from mesh cross-sections. No cape or cloth bones.
- Weights are geometric, not auto-weights: each vertex belongs to one part (torso/head, hair, each arm, each leg), parts are blended only across the shoulder (geodesic blend on both sides of the seam) and hip joints, then smoothed. 461 welded contact edges (hand/sleeve resting on the skirt, hair on sleeves) were split so separate surfaces no longer drag each other.
- Final mesh 135,003 faces / 69,202 verts. Max influences 4, unweighted 0. Influence count per vertex: 1: 23,820; 2: 34,331; 3: 10,164; 4: 887.
- No far-away bones: 97.5% of vertices only use bones within 2 links of their main bone; 1,738 armpit/hip verts reach 3 links (e.g. RightArm + Spine1), 14 reach 4.

## Edge-stretch check (edges 204,717; growth vs bind over every frame)
| clip | >1.25x | >1.5x | >2x | grew >1 cm | >2 cm | >5 cm | max growth | stretched >25% and >5 mm |
|---|---|---|---|---|---|---|---|---|
| rest | 0 | 0 | 0 | 0 | 0 | 0 | 0 cm | 0 |
| walk | 5,874 | 1,767 | 344 | 77 | 3 | 0 | 2.6 cm | 516 (0.25%) |
| attack | 11,010 | 5,337 | 2,033 | 857 | 158 | 28 | 8.4 cm | 3,123 (1.5%) |
The remaining attack stretch is in the right armpit when the arm drives forward (tiny edges deep in the armpit; linear skinning limit). The ratio counts are dominated by sub-millimetre edges.

## Animation
- **Rest:** bind pose (as before).
- **Walk** (1 s loop, treadmill in place): feet point forward (the bind right foot is turned ~52 deg out; it is yawed in), ankles 12 cm apart, heel strike -> flat -> toe-off rolling over the measured platform heel and ball points, soles clamped to the floor (lowest sole point 0.1-0.2 cm above ground every frame, planted feet move at one constant treadmill speed), arms swing close to the body (hands 21-31 cm from the hip centre), slight head turn/nod (about 3 deg), small hip sway and chest counter-twist.
- **Attack** (Shadow Bolt, 1 s, same intent as attack_20261007): left hand goes to the hip; the right hand cocks back by the hip (wind-up f9) while the chest coils, then drives forward at chest height with a 12 deg chest twist (release f13), follows through (f20) and recovers to rest (f30). Wrist bend max ~20 deg. No held prop. The bolt still spawns 12 cm ahead of the RightHand tail at f13 and flies character-forward at 9 m/s (VesperaAttack calibration/frames updated to the new rig).

## Exports
Same profiles as the earlier packs (rest: hum profile, no animation; walk/attack: rebake, all bones, step 1, simplify 0, force start/end, -Z forward / Y up, embedded basecolor + normal). Take name `Scene`, frames 1-31 on reimport, 22 bones, max 4 influences, 0 unweighted (`work/qc_fbx.json`).
ThemePack copies keep their names, metas and GUIDs: `VESPERA_blenderig.fbx` = `VESPERA_nocape.fbx`, `_walk` = `VESPERA_nocape_walk.fbx`, `_attack` = `VESPERA_nocape_attack.fbx`.

## Files
- `VESPERA_nocape.blend`, `VESPERA_nocape.fbx`, `VESPERA_nocape_walk.fbx`, `VESPERA_nocape_attack.fbx`
- `VESPERA_nocape_contact.jpg` (old cloth split vs new no cape; rest, walk f0/8/15/23, attack f9/13/20; front, three-quarter, rear battle camera)
- `work/`: build scripts (prep_mesh, seg1, cut1, rig_build, anim_build, finalize, stretch, qc, measure, r_sheet) and reports (prep, rig, anim, finalize, qc_fbx, stretch, walk_measure, joints)

HOLD: Derek Game-view before any merge.
