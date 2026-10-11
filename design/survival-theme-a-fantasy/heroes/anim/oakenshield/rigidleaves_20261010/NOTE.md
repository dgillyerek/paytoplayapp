# OAKENSHIELD — rigid leaves + clean weights + new walk/attack (2026-10-10)

Fixes Derek's Game-view note on PR #48 (4b7f231): "oakenshield's walk is extremely distorted". In the cloth-split pack the leaf
skirt and leg leaves tore into long shards, the lower legs smeared and pieces were dragged away from the body.

**Single source blend (rig + all three actions):** `OAKENSHIELD_rigidleaves.blend`
(objects `OAKENSHIELD_body`, `OAKENSHIELD_rig`; actions `OAKENSHIELD_rest`, `OAKENSHIELD_walk`, `OAKENSHIELD_attack`, frames 0-30 @ 30 fps).
Built from `../clothsplit_20261007/OAKENSHIELD_clothsplit.blend` in headless Blender 4.3.2 with the scripts in `work/`.

## What was removed
- Derek's cloth rule fallback (same call as Rowan #50 / Vespera #46): the hanging leaf skirt/tabard object `OAKENSHIELD_cloth`
  (9,246 verts / 18,456 faces) and its 14 cloth bones (`vine_FL_*`, `vine_L_*`, `leaf_BL_*`, `leaf_B_*`, `leaf_BR_*`).
- The runtime ClothSpringRig springs and thigh/shin/hip leg colliders are no longer used by Oakenshield
  (`OakenshieldClothSplit` spec + tests removed; the generic ClothSpring code stays for other characters).

## Rig and weights
- 22 mixamorig bones, joints refit to the mesh (the old auto joints sat outside the legs/arms; legs moved 20-28 cm, arms 11-23 cm).
- Body split into torso/head, L/R arm, L/R leg by geometry; 348 weld edges between separate pieces cut (306 arm-to-torso/belt, 42 left hand-to-skirt).
- **Rigid pieces:** 330 separate hard pieces (leaf plates, bark, gold vines, shin and foot spikes, claw tips; 12,524 verts) skinned 100%
  to the one bone that dominates where they attach, with a 3 cm blend ring on the body under each root so seams do not open.
  64 tiny loose bits cut off by the split follow the surface they touch with one shared weight set. 9 small pieces that sit across
  the shoulder joint keep smooth weights.
- Max 4 bones per vertex, 0 unweighted, legs carry only leg bones (0 leg verts with torso/arm weight), 0 torso verts with
  forearm/hand weight, 8 verts with >5% weight on a bone more than 30 cm away (influence histogram 1/2/3/4 bones: 55,201 / 30,347 / 6,311 / 1,003).

## Holes / one-sided surfaces (Design paint pass)
- 56 fill faces closed the small holes left by the cut; 2,143 flipped twin faces line the open cut rims, and 2,678 flipped twin
  faces back the 1,929 one-sided faces whose back was visible from outside (leaf undersides, skirt insides). All use the
  neighbouring UVs/paint. They are in the vertex group **`DESIGN_paint_fill`** (3,359 verts) for a look-paint pass.

## Animations
- **Rest:** bind pose.
- **Walk** (in place, f30 = f0): body squared to the walk direction (the bind pose has the pelvis turned 19° and chest 28°), feet and
  knees forward, ~12 cm stance, 39 cm stride, heel strike (-14°) → flat → toe-off (26°) pivoting on the measured heel/ball of the
  claw feet, 7.5 cm foot lift, contacts R f8 / L f23, 1 cm bob, close arm swing, slight head nod/turn. Soles never go below the floor,
  planted feet do not slide (leg IK).
- **Attack** (Thorn Spear throw, release f14, starts/ends at bind): feet planted; spear conjured in RightHand f3-8 while the arm lifts;
  cocked beside the right shoulder at f10 with the left arm aiming forward; chest drives through and the arm whips up and over to
  release at f14 with the spear kept pointing forward in the hand (wrist turn ≤ 54°); follow-through low across by f20; bind by f30.
  The spear is a rigid held prop bound to RightHand at f10 (grip 7 cm along the hand); spear-to-head clearance ≥ 30 cm (work/spear_clearance.txt).
  Projectile spawns at the f14 grip and flies character-forward at 11 m/s.

## Stretch check (edges growing vs bind; body mesh)
| | edges > 2 cm | edges > 5 cm | max growth |
|---|---|---|---|
| before (4b7f231) walk | 10,667 | 6,707 | 66.8 cm |
| before (4b7f231) attack | 4,168 | 3,463 | 14.3 cm |
| after rest | 0 | 0 | 0 |
| after walk | 80 | 0 | 4.6 cm |
| after attack | 302 | 21 | 8.6 cm (right armpit at the overhead cock/whip) |

Loose rigid pieces vs the surface they sit on: max gap opening 0.7 cm (walk) / 0.6 cm (attack) (work/gap.json).
(Before: the cloth object did not move in Blender — it only moved through runtime springs — so its stretch is not in these numbers.)

## Files
- `OAKENSHIELD_rigidleaves.fbx` (rest), `OAKENSHIELD_rigidleaves_walk.fbx`, `OAKENSHIELD_rigidleaves_attack.fbx` — same export
  profiles as the earlier packs (binary, -Z forward / Y up, no leaf bones, embedded textures, baked all bones step 1, take `Scene`).
  Copied over the ThemePack `OAKENSHIELD_blenderig{,_walk,_attack}.fbx` (names, metas, GUIDs kept).
- Reimport QC (work/qc_fbx.json): 22 bones in all three, 187,406 faces, max 4 influences, 0 unweighted, clips frames 1-31.
- `OAKENSHIELD_rigidleaves_contact.jpg`: before/after, rest + walk f0/8/15/23 + attack f6/10/14/20, front / three-quarter / rear battle cam / legs close-up.
- `work/`: prep.py (cloth removal), rig.py (refit + parts + welds + rigid pieces + weights), anim.py (rest/walk/attack), finalize.py
  (backface twins, calibration, spear frames, export), stretch.py, gap.py, spear_clear.py, qc_fbx.py, render/sheet scripts and their JSON output.

## Caveats
- Right armpit still stretches up to 8.6 cm at the overhead throw (21 edges); the walk is clean.
- Paint on the filled/twin faces is copied from neighbours; Design may want to repaint `DESIGN_paint_fill`.
- The leg spikes that look like arrows in Game view are body ornaments (now rigid); Oakenshield holds no bow — the only held prop is the thorn spear.

HOLD: Derek Game-view before any merge.
