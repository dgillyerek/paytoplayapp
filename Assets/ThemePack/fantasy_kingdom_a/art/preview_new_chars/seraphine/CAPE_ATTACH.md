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
| mixamorig:LeftUpLeg | 0.108 | 0.1 |
| mixamorig:RightUpLeg | 0.110 | 0.1 |
| mixamorig:LeftUpLeg.001 | 0.064 | 0.1 |
| mixamorig:RightUpLeg.001 | 0.064 | 0.1 |
| mixamorig:LeftUpLeg.002 | 0.061 | 0.1 |
| mixamorig:RightUpLeg.002 | 0.054 | 0.1 |

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

## Derek 24-bone skeleton (2026-11-04)
The body rig is now Derek's 24-bone skeleton. Leg bones were renamed. The capsule table above uses the new names:
- LeftLeg → LeftUpLeg.001, LeftFoot → LeftUpLeg.002, LeftToeBase → LeftUpLeg.003 (same on the right).
- Finger bones were removed. Fingers are weighted to the Hand bones.

The cape FBX, the cape rig and the attach are unchanged. Spine2 still exists, and the Hips, Spine, Spine1, Spine2, UpLeg, arm and wing references keep their names.
Leg radii were re-measured on the new rig (75th percentile distance of verts weighted > 0.6 to each bone segment):

| Bone | Old r (m) | New r (m) |
|---|---|---|
| LeftUpLeg | 0.102 | 0.108 |
| RightUpLeg | 0.104 | 0.110 |
| LeftUpLeg.001 (was LeftLeg) | 0.060 | 0.064 |
| RightUpLeg.001 (was RightLeg) | 0.060 | 0.064 |
| LeftUpLeg.002 (was LeftFoot) | 0.050 | 0.061 |
| RightUpLeg.002 (was RightFoot) | 0.050 | 0.054 |

Torso, arm (0.08 / 0.06) and wing capsules are unchanged.

### Fresh spring sim on the 24-bone rig (2026-11-04)
The cape was re-simulated for all four clips (v5 config: RESTCLAMP, WSEG 6, WPCT 100, WPAD 0.06).
- Cape-vs-wing crossings: 0 frames on idle, walk, attack_thrust and attack_throw.
- Weapon-vs-cape (full-res, every frame), minimum clearance: idle 2.2 cm, walk 8.5 cm, attack_thrust 13.8 cm, attack_throw 0.95 cm.
- The first fresh sim had the spear crossing the cape at walk f17 and attack_throw f41. Both were fixed with small RightArm/RightForeArm/RightHand key corrections (walk f16–18, throw f40–46), and the cape was re-simulated. `SERAPHINE_walk.fbx` and `SERAPHINE_attack_throw.fbx` were re-exported.
- Cape-into-body penetration is on the same level as the v5 baseline (thrust about 6.7 cm at the hair/arm, throw about 6 cm, idle under 0.5 cm, walk none).

Unity: `NewCharsPreview.cs` maps the UpLeg → .001 → .002 → .003 chain when these rows are present (Seraphine only); the capsule table above carries the re-measured radii.


## FIX4 review candidate r2 (2026-11-06)
The cape is the original main asset, unchanged: `SERAPHINE_cape.fbx` is byte-identical to main 259608d6. It has 10,977 vertices and 11,773 faces, and no faces are removed or masked. The rig, root/bind attachment, weights, UVs, texture and the collider table above are unchanged. No Unity code changed.

Root cause of the old collar contacts: they were all against her rigid braid (hair weighted to HeadTop_End), never against the torso. The neck/head turned up to 66° relative to the chest in the attacks, which swung the braid through the Spine2-rigid upper cape and also through both wings and her own body. FIX4 r2 limits the head turn relative to the chest per frame. The head never exceeds ~10–12° in the attacks or ~9° in idle, smoothed and with no snapping. A small two-link elbow extension in attack_throw f14–40 keeps the staff >=3.7 cm off her crown, with hand world orientation unchanged.

Fresh v5 spring simulation (same settings) with the full cape, final motion, and every-frame full-triangle checks: 0 cape/body (including hair), 0 cape/staff and 0 cape/wing intersections in idle, walk, thrust and throw. The back-ray check also reports 0 penetrations. Minimum cape-to-body clearance is idle 0.72 cm, walk 0.88 cm, thrust 0.86 cm and throw 1.00 cm. These are Blender spring results; Unity's runtime solver still needs an in-engine look.
