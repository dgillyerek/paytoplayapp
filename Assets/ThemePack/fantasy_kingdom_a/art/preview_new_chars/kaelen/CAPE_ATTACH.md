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
| mixamorig:LeftLeg | 0.071 | 0.1 |
| mixamorig:RightLeg | 0.071 | 0.1 |
| mixamorig:LeftFoot | 0.05 | 0.1 |
| mixamorig:RightFoot | 0.05 | 0.1 |

There are no arm colliders: arms may swing behind the back-only cape.

## Shape
- Faces trimmed from the front: 9939. The cape is back-only/open at the front.
- Pushed out from the body with a smooth offset field: target 4.5 cm, min rest clearance 0.035 m, median 0.098 m, 100% of verts ≥3 cm.
