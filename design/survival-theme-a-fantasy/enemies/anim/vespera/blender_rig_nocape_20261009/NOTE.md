# VESPERA: no cape (2026-10-09), rebuilt on Derek's rig edit (2026-10-10)

## 2026-10-11 rave conjure (attack only) - current state of the attack
Derek on a470cea: "that's pretty good. make her hands go in different directions as if she's outlining the orb. almost like a
rave dance." Only the attack changed: rest `d46729fb`, walk `fe139c5f` and orb `45b71d1a` are byte-identical to a470cea (the
blend's walk action re-keys to identical pose matrices on all 31 frames; Derek's 16 bones identical; `actdump.py`, `bonecmp.py`).
Scripts and reports: `work/rave_20261011/` (`anim_rave.py` + `attack_params.json`).

**Attack (31 frames, release f21, was f20).** f0-5 hands lift off her sides to opposite sides of the orb (visible f4). f5-17
contrary "gloving": the hands stay on opposite sides of the orb outline (always facing each other through it) and sweep in
contrary directions - left over the top while the right goes under (hit f8), swap: right over, left under (hit f11), again (hit
f14), back to the sides (settle f17). Each sweep snaps (fast middle, crisp stop), palms face the orb at orb radius (within 36 deg),
the orb grows to full size by f17 and spins with the sweeps. Loop plane tipped 25 deg so the under-pass swings forward, clear of her
stomach. A small groove on each hit: 1.2 cm dip, alternating hip roll 2 deg and chest roll 3 deg; feet planted. Chest turns toward
the target (85% of her bind twist). f17-21 two-hand push, f21 release from the orb centre (Unity 0.08, 1.44, 0.675), f21-24
follow-through, rest by f30 (push/return shapes as a470cea, one frame later).
Wrists: bend <=44 deg, twist <=39 deg; the forearm carries up to 46 deg of the palm turn (no twist bones). The right arm is at full
reach on the under-passes (IK miss <=1.2 cm). Unity: `VESPERA_orb` f4-20, `VESPERA_shadowbolt_vfx` from f21; ReleaseFrame 21, OrbFullFrame 17.

**Clearance** (`clear_attack.json`): 0 body overlaps f1-29 both sides, 0 hand-vs-hand, 0 hair; hands + lower forearms >=4.3 cm (right)
/ >=8.0 cm (left) f2-28, 3.0 cm at f1 and 3.3 cm at f29; during the conjure >=5.9 cm (right) / 8.0 cm (left).
**Stretch** (`stretch.json`): attack 6 edges >2 cm, max 2.41 cm (a470cea 5 / 2.9 cm); walk/rest unchanged (0 / 1.63 cm, 0).
**Files.** attack `9b9f123acbc8504b8eec703cd7dce430`, blend `ebb48cf63b66b984664c71ac125a70bd`; contact sheet replaced (10 conjure/attack
frames + follow-through, front + side).


## 2026-10-11 orb conjure + walk fix (walk + orb still current; attack superseded by the rave section above)
Derek on 369c25d: "it all looks good. her walk is a little weird. maybe she needs a small weapon to launch the attack." He picked
option C (orb) from the weapon sheet: "make her put both hands in front and move in a circle to 'conjure' the orb before firing."
Derek's 16 bones are byte-identical to 369c25d/265ec84 (heads, tails, matrices, parents, connect flags; `bonecmp.py`). Mesh and
weights untouched, so the rest FBX is kept byte-identical (`d46729fb`). Blend saved in Blender 5.2.2, walk active, Object Mode.
Scripts and reports: `work/orb_20261011/` (`anim_orb.py` with `attack_params.json` / `walk_params.json`; paths are the build box's).

**Orb prop.** `VESPERA_orb.fbx` (+ `VESPERA_orb_prop.blend`): the sheet's option C at a 10 cm orb (radius 5 cm), dark-violet orb,
silver claw cradle, floating rune ring across the hand-to-hand axis (so it never cuts the palms); origin at the orb centre, 1400
faces. Materials `VESPERA_orb_crystal` / `VESPERA_orb_rune_glow` / `VESPERA_orb_silver` - the Unity attack driver turns emission on
for the crystal and rune materials, and the Orb effect adds the glow core, embers and trail around the mesh. Hidden at rest/walk.

**Attack (31 frames, release f20).** Feet planted. f0-6 both hands lift off her sides (outward first, clear of the skirt) and come
up in front of her chest, palms facing each other and cupping the orb from the sides; f6-16 the two hands trace one full circle
together (14 cm wide/high, a little depth, small roll) while the orb grows between the palms (visible f4 at 18%, full size f16,
spinning with the circle); f16-20 two-hand push forward and up to chest height, body squares to the target and leans in; f20 the
orb leaves as the Shadow Bolt from the orb centre (Unity 0.065, 1.40, 0.635), 9 m/s character-forward; f20-23 hands open a little
and follow through; back to the bind pose by f30. Wrists: bend vs her bind wrist <=30 deg and twist <=30 deg during the conjure;
up to 41 deg on the right wrist in the follow-through (f22-25, right arm fully extended at f24). Hands stay open (no finger bones).
Unity: `VESPERA_orb` (Fixed track, per-frame orb centre = midpoint of the two palms, scale, spin) f4-19, then
`VESPERA_shadowbolt_vfx` (Projectile, same mesh) from f20. Bolt spawn re-keyed to the orb at release.

**Clearance** (`clear2.py`, both hand + forearm pieces vs every non-arm, non-hair body triangle; `clear_attack.json`, `clear_walk.json`):
attack 0 body overlaps f1-29 on both sides, 0 hand-vs-hand overlaps, 0 hair overlaps (was f4-10 on the right), hands + lower forearms
>=3.5 cm (right) / >=4.5 cm (left) from the body f2-28 (2.7 cm at f29, just before the bind pose; 13-14 cm at the release).
Walk: 0 hand/lower-forearm overlaps on either side; min clearance right 1.8 cm, left 0.6 cm; the left upper forearm next to the
elbow brushes the waist on 14 frames (max 63 small triangles) because the arms now hang closer.

**Walk.** Knees straighten as each foot lands: min knee bend 11.5 deg (was 20-25), mid-stance 15-24 deg (was 30-38); hips carried
higher (no constant -2.8 cm crouch) with the natural dip at double support; stride 0.31/0.32 m (was 0.25/0.26); stance width 12 cm,
foot yaw unchanged. Arms: forearm flare 1 deg (was 4), elbows bend 24-46 deg (was 10-32) so the hands pass in front of the skirt,
upper arm 19 deg out on the forward swing and 17 deg on the back swing (was 20/14). The flared skirt stops the arms going much
closer: at 16-17 deg the hands enter the skirt (measured).

**Stretch** (`stretch.json`, 204,125 edges, growth vs bind over every frame): rest 0; walk 0 edges >2 cm, max 1.63 cm; attack 5 edges
>2 cm (right shoulder top in the follow-through f23), max 2.9 cm (369c25d: walk 0 / 1.7, attack 7 / 3.2).

**Files.** walk `fe139c5fbaa4146b4bc3eadc03265d72`, attack `5b907c6f5ec944e9fb4233bd9d451e5c`, rest `d46729fb1f36700957dd1ee0f08cb55f`
(unchanged), blend `60e8b1ffdd37dcdb480a572631a19bb6`, orb `45b71d1aa45d4083d922560f59b77cc4`. Contact sheet `VESPERA_nocape_contact.jpg`
(front + side, 10 attack + 4 walk frames, EEVEE with the orb prop). Reimport QC `work/orb_20261011/qc_fbx.json`: 16 bones, 134,863
faces, max 4 influences, 0 unweighted, clips 1-31.

---

## 2026-10-10 right hand freed + Shadow Bolt goes around her (superseded by the section above)
Derek on f41ef77: "there is still something attached to her right hand, maybe a piece of clothing. for the attack her hand
comes through her body, it should go around." Derek's 16 bones are byte-identical to 265ec84 (heads, tails, rolls, parents,
connect flags compared at full float precision). Blend saved in Blender 5.2.2, walk active, Object Mode. Scripts and reports:
`work/hand_20261010/` (run.sh lists the chain; paths are the build box's).

**What was attached to the hand.** In the source mesh the right forearm/hand surface is fused to the clothing next to it:
the fingers and palm to skirt flaps and the hip (35 joining edges, z 0.90-0.97), the wrist/forearm to the belt/waist
(18 edges, z 1.03-1.06), and the inner upper arm to the side of the chest by the armpit (230 edges, z 1.34-1.46). In
f41ef77, 767 skirt/belt/hip vertices followed the right forearm/hand (>25% weight; 100 skirt/hip vertices >50%), so
pieces of skirt travelled with the hand, and 40 hand/forearm vertices were partly on the torso.

**How it was separated (cut_label.py, cut_fill.py).** A minimum cut over the surface between the arm core (hand, fingers,
thumb, forearm, elbow) and the clothing around it found the shortest boundary along the contact; coarse skirt shards
sitting against the fingers were forced onto the clothing side. The 350 faces crossing that boundary (281 surface + 69
lining) were deleted, so the arm piece and the clothing no longer share any face or vertex (0 duplicate surface verts).
Weights: the arm piece carries right-arm bones only (forearm/hand only below the elbow band; max non-arm weight 0.0); the
skirt, belt and hip pieces near the hand carry torso/thigh bones only (max right-arm weight 0.0). Max 4 per vertex, 0 unweighted.

**Filled holes (flag for Design to paint).** All filled with real triangles that take the UVs (paint) of the surrounding
surface; flat shaded. Hand side: a 12-edge hole on the forearm (z 1.05), a 14-edge gap at the fingertips/palm (z 0.94),
and four holes on the inner upper arm by the armpit (z 1.41-1.43). Body side: an 18-edge hole on the hip/skirt where the
fingers were fused (z 0.935), and four on the side of the chest by the armpit (z 1.41-1.44). The hand and lower forearm are
closed: 0 open edges (the only open edges there are the lining-patch borders that existed before). Seven short open edges
remain on the inner upper arm right under the armpit (z 1.37-1.44, hidden by the arm in every pose we render) - flag for Design.

**Attack path (anim_rw.py).** The right hand now winds back OUT to her right side (f3-f9, hand about 40 cm out from the
shoulder at waist height), sweeps forward in an arc outside her hip, waist and chest (f9-f13), releases in front of her right
shoulder at f13 (bolt spawn Unity 0.218, 1.409, 0.980 - her right side, chest height; was 0.040 across the chest), follows
through to f20 and comes back along the outside to rest by f30. Wrist bend peaks at 21.7 deg; the upper arm
lifts ~64 deg out to the side at the wind-up and ~79 deg forward at the release (a normal forward throw). Per-frame triangle test (clear.py, hand + forearm vs every non-arm, non-hair body triangle): 0 overlaps f1-f29;
hand + lower forearm clearance 1.1 cm at f1 (just lifting off the hip), >=3.4 cm f2-f29, 15-23 cm around the release; the
elbow passes 0.3-2 cm from the side of the chest during the wind-up. f0/f30 are the bind pose, where the hand still rests
against the skirt. The arm does pass through a few strands of her long hair hanging at her right side during the wind-up
(f4-f10; hair is skinned, it cannot move aside). Bolt frames in VesperaAttack re-keyed from the new spawn.

**Walk.** Arm swing back to 16 deg (from 12) with the f41ef77 elbow pump restored, and the upper arm held at her bind-pose
abduction (20 deg, easing to 14 deg on the back swing) so the freed hand passes outside the flared skirt instead of through it:
hand + lower forearm stay >=2.0 cm from the body on every frame. Legs unchanged (knee bend 20-60 deg, stride ~0.25 m, 12 cm stance).

**Stretch check** (204,125 edges incl. lining twins, growth vs bind over every frame, `stretch.json`, `where.json`):
| clip | grew >1 cm | >2 cm | >5 cm | max growth | stretched >25% and >5 mm |
|---|---|---|---|---|---|
| rest | 0 | 0 | 0 | 0 cm | 0 |
| walk (16 deg swing) | 63 | **0** | 0 | 1.7 cm | 671 |
| attack | 85 | **7** | 0 | 3.2 cm | 659 |
(f41ef77: walk 0 >2 cm / max 2.0 cm at 12 deg; attack 206 >2 cm / max 7.4 cm.) The 7 attack edges are on the right shoulder top at the release.

---

## 2026-10-10 (f41ef77) re-weight on Derek's rig (superseded by the section above)
Derek: "just reanimate using the new rig." His 16-bone rig (265ec84) is kept exactly: no bones added back, none moved,
same names/parents/rolls. What changed is the skin: weights re-solved on his joints, the 507fb9a arm seams re-welded,
and rest / walk / attack rebuilt. Blend saved in Blender 5.2.2, walk active, Object Mode. Scripts and reports:
`work/reweight_20261010/` (run.sh runs the whole chain; paths are the build box's).

**Weights (b_solve.py).** Solved as a smooth (Laplace / "heat") blend over the welded surface, seeded only where a
vertex clearly belongs to one of Derek's bones; the joint bands (his knees, ankles, elbows, wrists, the waist/pelvis
band, the shoulder tops) and the re-welded contact areas are left free so the solve blends them smoothly. Then: limb
weights fade out with distance from their own chain, a vertex never mixes left- and right-arm weights, head/neck stay
on the head, neck and the scalp end of the hair, max 4 per vertex (truncate + relax, so no popping), normalised. The six
deleted bones' vertex groups are gone; only his 16 groups remain.
Where the orphaned weights went (average new weights of the verts that were mostly on each deleted bone; `orphan_moved.json`):
- Hips (2,082 verts, waist/pelvis z 0.78-1.06): Spine1 56%, LeftUpLeg 37%, RightUpLeg 4% (the pelvis band blends from the torso root at the top into the thighs at the bottom).
- Spine (3,937 verts, waist z 1.02-1.18): Spine1 82%, upper thighs 9%, Spine2 5%, right hand 4% (where the hand rests on the waist).
- LeftShoulder (618 armour/skin verts, z 1.37-1.55): LeftArm 51%, Spine2 41%, Head 6%. Its 258 hair verts: Spine2 53%, LeftArm 25%, Head 19%.
- RightShoulder (1,413 armour/skin verts, z 1.38-1.56): RightArm 41%, chest (Spine2 + Spine1) 35%, Head 19% (long hair lying over this shoulder that the hair detector did not flag). Its 1,358 hair verts: RightArm 33%, Head 27%, chest 39%.
- LeftToeBase / RightToeBase (105 / 90 verts): 100% LeftFoot / RightFoot.
- The 2,651 verts that were left on deleted bones only (did not move at all in 3d12122) now follow Spine1 1,887, LeftUpLeg 505, Spine2 108, RightArm 71, others 80.
- Result: 0 unweighted, max 4 influences (1: 23,016; 2: 19,743; 3: 14,884; 4: 7,069 original-surface verts). Bends happen at Derek's knees, hips (waist-height UpLeg heads) and shoulders.

**Seams.** The 525 coincident vertex pairs left by the 507fb9a contact-edge split (forearm / upper arm / hand against the
chest, waist and hip; 933 verts) were re-welded (Blender merge-by-distance limited to those, 483 verts merged; 69,202 -> 68,719
verts, 135,003 -> 135,002 faces). 0 coincident duplicate surface verts remain, so no gap can open on the arm. The 4,007 flipped
lining-twin verts (back-face shell from 507fb9a) are not welded; they copy the weights of the surface they line. Sleeves
that are separate geometry in the source stay separate (only the split pairs were welded).

**Animation.** Same intent as 507fb9a / 3d12122, keyed on his 16 bones only (deleted bones are virtual pivots). Walk: arm swing
16 -> 12 deg with less elbow pump (the right hand is welded to the hip, so a bigger swing stretches the hip skin); legs unchanged.
Attack: unchanged (Shadow Bolt: wind-up f9, release f13, follow-through f20, rest f30), calibration and bolt spawn unchanged.
Gait on his raised joints (`gait_new.json`): knee bend 20-60 deg (stance ~30-35, swing peak ~58-60; his bind knee is ~14 deg),
each foot travels ~0.25 m per step on the treadmill, ankles 12 cm apart, feet within 12 deg of forward, pelvis root bob 1.074-1.102 m.
Same leg motion as 3d12122; his waist-height hip pivots do not change stride or knee timing.

**Stretch check** (204,257 edges incl. lining twins; growth vs bind over every frame, `stretch.json`, `where.json`):
| clip | grew >1 cm | >2 cm | >5 cm | max growth | stretched >25% and >5 mm |
|---|---|---|---|---|---|
| rest | 0 | 0 | 0 | 0 cm | 0 |
| walk | 92 | **0** | 0 | 2.0 cm | 835 |
| attack | 1,023 | 206 | 7 | 7.4 cm | 3,128 |
(3d12122: walk >2 cm 2,002 / max 27.7 cm; attack 3,081 / 44.6 cm.) The attack's remaining stretch is where the right hand is
welded to the hip/skirt (fused in the source mesh): when the hand drives forward ~0.5 m that web has to stretch; also some
right shoulder-top edges at the release.

---

## 2026-10-10 (3d12122): animations on Derek's rig, weights untouched (superseded by the section above)
Derek reviewed 507fb9a in Unity ("distorted with her legs, big gap on her arm from pieces not moving"), edited the
rig himself in Blender 5.2 and committed `VESPERA_nocape.blend` (265ec84). Rest / walk / attack were rebuilt on his
rig; **his bones, weights and mesh are byte-for-byte unchanged** (verified by dump). Blend saved in Blender 5.2.2,
walk active, Object Mode. FBXs exported from 5.2.2 with the same profiles.

**What Derek changed (vs 507fb9a)**
- Deleted 6 bones: Hips, Spine, LeftShoulder, RightShoulder, LeftToeBase, RightToeBase (22 -> 16 bones).
- Hierarchy: Spine1, LeftUpLeg and RightUpLeg are now three separate roots (no Hips); both arms now hang directly off Spine2 (not connected).
- Moved joints: hips/thigh tops up ~19-21 cm to waist height (z 1.07-1.09), knees up ~20 cm (z 0.68-0.70), ankles up ~9-12 cm (z 0.22-0.25); shoulders up ~11-14 cm (z 1.55-1.56) and inward, elbows up ~9 cm, wrists up 5-8 cm; Neck/Head moved ~5-6 cm forward; Spine1 down 5 cm; Spine2 head unchanged (roll changed).
- Unchanged: mesh (69,202 verts / 135,003 faces), all 22 vertex groups and every weight value, modifiers, parenting, materials. No hand keys (all three actions identical to 507fb9a). Saved with the armature in Edit Mode.

**Consequences (not fixed: weights are Derek's to change)**
- The 6 deleted bones still have vertex groups. 3,044 verts are weighted only to deleted bones and do not move at all: waist/pelvis band (Hips/Spine, 2,691 verts, z 0.87-1.13) and shoulder tops (Right 189, Left 164 verts, z 1.41-1.53). 22,220 more are partly on deleted bones and get renormalised onto the remaining bones (Blender and Unity both do this).
- The weights were made for the 507fb9a joint positions, so with the joints moved, legs and shoulders bend at Derek's pivots while the skin blends at the old ones.
- The arm gap Derek saw: 539 split seam pairs where arm-weighted and torso-weighted copies of the same surface separate (right forearm/upper arm vs chest and waist at x -0.36..-0.17, z 1.03..1.27; left forearm/upper arm vs chest at x 0.11..0.21, z 1.07..1.26; right hand vs hip at z 0.89..1.04). These come from the contact edges split in 507fb9a and are still in this mesh. Report: `work/derek_rig_20261010/seam_gap.json`.
- Unity: no Hips bone, so the rig imports as **Generic** (metas animationType 2) to avoid a Humanoid failure and reimport on every Play.

**Animation rebuild:** same intent as 507fb9a. Deleted bones are used only as virtual pivots (their 507fb9a rest matrices) to place Derek's real bones; only his 16 bones are keyed (the three roots get the pelvis motion). Walk: in place, feet/knees forward, ~12 cm stance, heel-to-toe with floor clamp, arms close, slight head motion. Attack: Shadow Bolt, left hand to hip, right hand winds back by the hip (f9), drives forward (release f13), follow-through f20, rest f30. Bolt calibration now uses Spine1/Head/LeftHand/RightHand.

**Stretch check on Derek's rig** (204,717 edges): rest 0. Walk: grew >5 cm 1,585, >2 cm 2,002, max 27.7 cm, stretched >25% and >5 mm 5,756. Attack: >5 cm 1,684, >2 cm 3,081, max 44.6 cm, >25% and >5 mm 7,138. Of the >5 cm edges, walk 1,520 touch a vertex left only on a deleted bone and 65 a partly-orphaned vertex (0 on clean weights); attack 1,379 / 288 / 17. Report: `work/derek_rig_20261010/` (stretch.json, tear_where.json).

---
## 2026-10-09 (507fb9a, superseded by the section above)

Derek's cloth-rule fallback after the split cape still read badly in motion (same call as Rowan no-cape, #50):
the cape/cloak is removed, the body is re-rigged on a clean 22-bone skeleton, and rest / walk / attack are re-authored.
The cloth-split pack (`clothsplit_20261007/`) is unchanged and stays in git history.

**Single source blend (rig + all three actions):** `design/survival-theme-a-fantasy/enemies/anim/vespera/blender_rig_nocape_20261009/VESPERA_nocape.blend`
(now Derek's 5.2 rig, see above; objects `VESPERA_rig`, `VESPERA_body`; actions `VESPERA_nocape_rest`, `VESPERA_nocape_walk`, `VESPERA_nocape_attack`, 0-30 @ 30 fps).

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
- `VESPERA_nocape_contact.jpg` (now: 3d12122 vs re-weight; rest, walk f0/8/15/23, attack f9/13/20; front, three-quarter, rear battle cam, waist/shoulder/arm close-ups; before that: 507fb9a vs Derek rig rebuild; was: old cloth split vs new no cape; rest, walk f0/8/15/23, attack f9/13/20; front, three-quarter, rear battle camera)
- `work/`: build scripts (prep_mesh, seg1, cut1, rig_build, anim_build, finalize, stretch, qc, measure, r_sheet) and reports (prep, rig, anim, finalize, qc_fbx, stretch, walk_measure, joints)

HOLD: Derek Game-view before any merge.
