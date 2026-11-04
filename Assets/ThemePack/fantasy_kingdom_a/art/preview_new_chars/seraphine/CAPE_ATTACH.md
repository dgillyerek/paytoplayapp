# SERAPHINE cape — attach notes

Asset: `SERAPHINE_cape.fbx` (cape mesh + its own armature `SERAPHINE_cape_rig`), texture `SERAPHINE_cape_1024.png`.
The body FBXs in this folder (rig_rest, idle, walk, attack*) do NOT contain the cape.

## Parent
- Parent bone: `mixamorig:Spine2` (upper spine/shoulders; there are no leg weights).
- `cape_root` was built from Spine2's head, tail and roll, so its bind matrix equals Spine2's (error ~1e-7).
- Local offset (0,0,0), local rotation (0,0,0), scale 1. Spine2 head in rest: [0.0, -0.0083, 1.2778] m.
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
| mixamorig:Hips | 0.11 | 0.0 |
| mixamorig:Spine | 0.063 | 0.0 |
| mixamorig:Spine1 | 0.074 | 0.0 |
| mixamorig:Spine2 | 0.069 | 0.0 |
| mixamorig:LeftUpLeg | 0.102 | 0.1 |
| mixamorig:RightUpLeg | 0.104 | 0.1 |
| mixamorig:LeftLeg | 0.06 | 0.1 |
| mixamorig:RightLeg | 0.06 | 0.1 |
| mixamorig:LeftFoot | 0.05 | 0.1 |
| mixamorig:RightFoot | 0.05 | 0.1 |

Arm and wing colliders were added in v5. See below.

## Shape
- Faces trimmed from the front: 10306. The cape is back-only/open at the front.
- Pushed out from the body with a smooth offset field: target 4.5 cm, min rest clearance 0.030 m, median 0.082 m, 100% of verts ≥3 cm.


## v5 colliders (2026-11-03)
### Arm capsules (bone head→tail)
| bone | radius m |
|---|---|
| mixamorig:LeftArm / RightArm (upper arm) | 0.08 |
| mixamorig:LeftForeArm / RightForeArm | 0.06 |

**Rest clamp (required).** For each joint, the effective arm-capsule radius is min(radius, bind-pose clearance − 0.01 m). This matters only for the `_01` joints, which overlap the arms at rest. For each arm, the side `_01` joint gets about 0.02 m vs the upper arm and the full radius vs the forearm. Without the clamp, the arms throw the cape into the wings.
Push direction: radial when the joint is behind the capsule axis, otherwise straight back (−Z of Spine2).

### Torso colliders
These are unchanged. See the table above (Hips 0.11, Spine 0.063, Spine1 0.074, Spine2 0.069; joint hit 0.0).

### Wing capsules (rigid child of mixamorig:Spine2, local space, m)
| capsule | a | b | radius |
|---|---|---|---|
| L root | (-0.102, 0.113, -0.212) | (-0.092, -0.103, -0.234) | 0.055 |
| L root | (-0.177, -0.004, -0.201) | (-0.034, 0.006, -0.241) | 0.055 |
| L slice | (-0.665, -0.603, -0.257) | (-0.574, -0.657, -0.255) | 0.072 |
| L slice | (-0.665, -0.386, -0.247) | (-0.480, -0.496, -0.242) | 0.085 |
| L slice | (-0.627, -0.189, -0.233) | (-0.392, -0.328, -0.227) | 0.100 |
| L slice | (-0.561, -0.005, -0.223) | (-0.221, -0.207, -0.214) | 0.108 |
| L slice | (-0.476, 0.175, -0.231) | (-0.070, -0.066, -0.221) | 0.105 |
| L slice | (-0.391, 0.314, -0.231) | (-0.037, 0.103, -0.222) | 0.098 |
| R root | (0.102, 0.113, -0.212) | (0.092, -0.103, -0.234) | 0.055 |
| R root | (0.177, -0.004, -0.201) | (0.034, 0.006, -0.241) | 0.055 |
| R slice | (0.665, -0.603, -0.257) | (0.574, -0.657, -0.255) | 0.072 |
| R slice | (0.665, -0.386, -0.247) | (0.480, -0.496, -0.242) | 0.085 |
| R slice | (0.627, -0.189, -0.233) | (0.392, -0.328, -0.227) | 0.100 |
| R slice | (0.561, -0.005, -0.223) | (0.221, -0.207, -0.214) | 0.108 |
| R slice | (0.476, 0.175, -0.231) | (0.070, -0.066, -0.221) | 0.105 |
| R slice | (0.391, 0.314, -0.231) | (0.037, 0.103, -0.222) | 0.098 |

### Weights
The rim rigid falloff was widened from 4/8 cm to 8/12 cm (fully rigid within 8 cm of a wing, blending to spring by 12 cm). No new cuts. The body FBXs are unchanged.

### Weapon clearance (v5b)
In attack_throw, the spear no longer passes through the cape (full-res minimum is +0.09 cm). The fix uses small additive rotation keys on RightHand, RightForeArm and RightArm in `SERAPHINE_attack_throw.fbx` between f39 and f50, smoothed over ±2 frames.
No spear collider is needed in Unity. A thin RightHand shaft capsule (r 0.03–0.05 m, f40–46) was tested and did not help, because the crossing verts are not on spring joints.
