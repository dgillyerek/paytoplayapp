# KAELEN cape — attach notes

Asset: `KAELEN_cape.fbx` (cape mesh + its own armature `KAELEN_cape_rig`), texture `KAELEN_cape_1024.png`.
The body FBXs in this folder (rig_rest, idle, walk, attack*) do NOT contain the cape.

## Parent
- Parent bone: `mixamorig:Spine2` (upper spine/shoulders; there are no leg weights).
- `cape_root` was built from Spine2's head, tail and roll, so its bind matrix equals Spine2's (error ~1e-7).
- Local offset (0,0,0), local rotation (0,0,0), scale 1. Spine2 head in rest: [0.0, 0.0034, 1.371] m.
- In Unity, parent `cape_root` under the body's Spine2 with an identity local transform.
  Or use a ParentConstraint with zero offset.

## Chains (children of cape_root)
{
 "L": [
  "cape_L_01",
  "cape_L_02",
  "cape_L_03"
 ],
 "C": [
  "cape_C_01",
  "cape_C_02",
  "cape_C_03"
 ],
 "R": [
  "cape_R_01",
  "cape_R_02",
  "cape_R_03"
 ]
}
Spring roots: `cape_L_01`, `cape_C_01`, `cape_R_01`.

## Spring-bone settings (VRM SpringBone style; the PREVIEW used these)
- stiffness 1.0, drag 0.4, gravity 0.4 (down), dt 1/30
- joint hitRadius 0.1 m vs leg/foot colliders, 0.0 vs torso colliders
- The preview also collides the L–C and C–R row segments with the capsules, so legs cannot slip between columns.
  If your spring solver has no segment collision, use hitRadius 0.10 on all joints for the legs.

## Capsule colliders (bone head→tail; radius = 75th-percentile body radius)
| bone | radius m | joint hit m |
|---|---|---|
| mixamorig:Hips | 0.107 | 0.0 |
| mixamorig:Spine | 0.143 | 0.0 |
| mixamorig:Spine1 | 0.156 | 0.0 |
| mixamorig:LeftUpLeg | 0.115 | 0.1 |
| mixamorig:RightUpLeg | 0.116 | 0.1 |
| mixamorig:LeftLeg | 0.073 | 0.1 |
| mixamorig:RightLeg | 0.071 | 0.1 |
| mixamorig:LeftFoot | 0.05 | 0.1 |
| mixamorig:RightFoot | 0.05 | 0.1 |

## v4 (Nov 3): arm capsule colliders added (preview sim)
Add these in Unity too; they stop the arms from showing through the cape during walk (walk cape-into-body: 78 frames, max 4.5 cm before -> 0 frames after).
| bone | radius m | joint hit m |
|---|---|---|
| mixamorig:LeftArm | 0.08 | 0.0 |
| mixamorig:RightArm | 0.08 | 0.0 |
| mixamorig:LeftArm.001 | 0.06 | 0.0 |
| mixamorig:RightArm.001 | 0.06 | 0.0 |
Cape mesh, weights and attach are unchanged in v4 (KAELEN_cape.fbx not re-exported).

## 2026-11-04: Derek's 20-bone skeleton
The body rig is now 20 bones. ForeArm, Hand, Hand-index, ToeBase and Toe_End are gone.
- Renamed capsules: `mixamorig:LeftForeArm` became `mixamorig:LeftArm.001`, and `mixamorig:RightForeArm` became `mixamorig:RightArm.001`. Each capsule runs from Arm.001's head to Arm.002's head, and the radius stays 0.06.
- Re-measured leg capsules on the new bones: LeftUpLeg 0.115, RightUpLeg 0.116, LeftLeg 0.073 (was 0.071), RightLeg 0.071.
- Foot capsules (0.05) now run from the Foot head to the Foot tail. The Foot bone goes from the ankle to the toe tip. Before, they ran from Foot to ToeBase.
- Cape attach is unchanged. It parents to `mixamorig:Spine2`, whose rest head is still [0.0, 0.0034, 1.371]. KAELEN_cape.fbx was not re-exported (MD5 a58a559ab972a1588aaf58f87b5bcbf1).
- Unity: `Assets/Survival/Unity/NewCharsPreview.cs` still maps capsules Foot→ToeBase and Arm→ForeArm→Hand by bone name. The ForeArm, Hand and ToeBase bones no longer exist, so that code has to change before those capsules will resolve. That change is not made here.

- Cape sim settings for the re-keyed attack_dualslash check: `RESTCLAMP=1`, `ARMR=0.10`, `FARMR=0.08` (arm capsule radii used for the rest-distance clamp). Rim-rigid weights near the arms were tried and dropped: only 2 cape verts lie within 8 cm of the rest arms, so they had no effect.
- Fresh sim of the re-keyed dual slash: no sword-into-cape penetration on any frame (min sword-to-cape 2.3 cm, f57). Cape into body still happens on 4 frames (f33 -5.2 cm, f56 -11.8 cm/137 verts, f57 -1.8 cm, f97 -0.3 cm), mostly the hands moving into the cape.

## Shape
- Faces trimmed from the front: 9939. The cape is back-only/open at the front.
- Pushed out from the body with a smooth offset field: target 4.5 cm, min rest clearance 0.035 m, median 0.098 m, 100% of verts ≥3 cm.
