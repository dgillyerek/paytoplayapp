# Seraphine FIX5 — palm saddle on the fitted shaft, still a draft

Not merged. Not Derek-ready. No claim that the clips pass.

## What changed in this pass

The left target is no longer the whole-hand centroid and no longer the bone-X line. Eight vertices at the middle of the palm face (finger-base band, sternum side of the centroid) are averaged in the left-hand bone frame. That saddle is placed 2.4 cm off the fitted handle axis, 36 cm toward the butt, on every thrust frame where the grip is on. The right hand is not moved off its bind. The staff stays skinned to it.

The station was out of the left arm (about 49 cm against a 44 cm reach), so the hand stopped short and the palm floated. The thrust line is now forward and up, and the whole shaft is shifted toward the left shoulder until that 36 cm station is inside the arm. The spacing is not shortened to fake a reach.

Throw recovery no longer lifts both feet together. Frames 43 and 44 have both soles on the floor (lowest skinned foot vertex about 0.10 cm and 0.12 cm). Frame 45 keeps the left sole down and lifts the right (about 7 cm).

## Measured on the exported thrust FBX, rest mesh reimported

Left-hand skin versus the staff surface. Nearest vertex, median, count within 2 cm, of 532 LeftHand vertices:

| frame | nearest | median | within 2 cm |
|---:|---:|---:|---:|
| 14 | 0.03 cm | 2.46 cm | 219 |
| 17 | 0.02 cm | 2.43 cm | 227 |
| 18 | 0.04 cm | 2.55 cm | 185 |
| 19 | 0.01 cm | 2.42 cm | 227 |
| 20 | 0.04 cm | 2.55 cm | 185 |
| 21 | 0.01 cm | 2.42 cm | 225 |
| 24 | 0.06 cm | 2.59 cm | 181 |

The saddle itself sits at 2.40 cm from the axis and 36.0 cm along it at frames 14, 18 and 24, and the hand bone reaches that target (0 cm miss). Frames 17–21 are no longer a complete miss. Frame 24 is no longer a shaft driven through the palm: the nearest skin is on the surface, not 0.13 cm from the axis.

This is an open hand. There are no finger bones, so the fingers stay in the bind shape. A BVH leaf overlap of the hand triangles against the staff is 70 pairs at frame 14, 66 at 17, 45 at 18, and 38 at 24. That count is a bounding overlap, not a signed penetration depth. It is not zero.

## Still open

Upper-arm steps are smaller than the old 148° and 179° flips, and they are not within 25°. Thrust right arm is about 70° into frame 2 and about 70° into frame 27. The left arm moves about 45° per frame through the drive. Throw right arm is about 70° into frame 2 and still about 64° into frame 27. The elbow is held to 9 cm from the previous frame on the right arm; the remaining step is orientation, and the left arm is not capped because capping it pulled the palm off the shaft.

Throw staff-versus-body and pauldron-versus-hair were not re-counted on this export. Do not treat the foot plant or the palm table as a pass on those.

Rest skeleton, idle, walk, cape mesh and weights were not edited.
