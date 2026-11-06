# Seraphine FIX5 — recovery bend and throw wrist, still a draft

Not merged. Not Derek-ready. No claim that the clips pass.

Two motion faults from the check of `cc66f3b9`, and nothing else.

The thrust left wrist switched from the grip orientation to a straight line with the forearm when the grip blend crossed 0.4, so frame 47 was bent about 129° and frame 48 was at 0°. The arm then hung stiff and the last frame dropped the elbow. The hand rotation now blends back to the idle wrist across the whole release, and the elbow pole returns toward the idle elbow with it. Measured bend on the export: frame 46 is 160°, 47 is 142°, 48 is 114°, 49 is 96°, 50 is 69°, 51 is 54°, 52 is 35°. Twist around the hand's long axis is back to 0 at frame 52. There is no single-frame upper-arm step over 25° on that recovery.

The throw right wrist was being re-rolled to chase the forearm, so the twist around the hand's long axis climbed to about 53° in the windup and the bend spiked in the first frames. The forearm now rolls so the staff, which stays fixed in the hand, points along the planned line, and the wrist joint stays at the idle pose. Measured bend is 26.4° and the twist is 0.0 on frames 3 through 17. Head, hip and leg distances at frames 14, 16, 27 and 49 stay in the same clear range as before. The solver still reports one staff sample about 2 cm from the body at frame 49; the region distances there are head 44 cm, hip 25 cm, leg 12 cm.

Cuff pairs on the thrust samples, including 8–11, 17–21 and 40–43, are 0. Frame 17 still has 110 hand vertices within 2 cm of the staff, frame 18 has 286, frame 21 has 245. Rest, idle, walk, cape and the right-hand socket were not edited.

# Previous note — left cuff cleared off the shaft, still a draft

Not merged. Not Derek-ready. No claim that the clips pass.

The independent check of `63b91499` left one real contact: the character's left forearm cuff through the thrust staff on frames 8–11 and 40–43. The right forearm had none. This pass only moves that wrist a few centimetres straight off the handle when the forearm is inside it, then re-solves the left arm. The right-hand socket, the throw, and the shaft path are unchanged.

Left-forearm triangle pairs against the staff on the exported thrust, rest mesh:

| frame | cuff pairs | hand verts within 2 cm | nearest hand |
|---:|---:|---:|---:|
| 8 | 0 | 179/532 | 0.00 cm |
| 9 | 0 | 181/532 | 0.01 cm |
| 10 | 0 | 182/532 | 0.02 cm |
| 11 | 0 | 179/532 | 0.04 cm |
| 17 | 0 | 110/532 | 0.03 cm |
| 18 | 0 | 280/532 | 0.02 cm |
| 21 | 0 | 238/532 | 0.01 cm |
| 40 | 0 | 149/532 | 0.01 cm |
| 41 | 0 | 160/532 | 0.02 cm |
| 42 | 0 | 162/532 | 0.02 cm |
| 43 | 0 | 136/532 | 0.03 cm |

Neighbors 7, 12, 39, 44 and 46 are also 0 cuff pairs. Frame 17 still touches; the lift caught it because the forearm was within 3.2 cm of the axis even though it was not intersecting, so fewer hand verts sit inside 2 cm than before (110, was about 280). Frames 18 and 21 were not lifted.

Head, hip and leg clearances on these frames match the previous export. Throw frames 1, 27, 43, 44 and 49 match it too, including both soles down at 43 and 44. Rest, idle, walk and cape were not edited. The left-arm step at frame 52 was left as it was; the motion read did not flag it.

# Previous note — shaft moved back out of the body

Not merged. Not Derek-ready. No claim that the clips pass.

`37c62d0` put the palm on the staff by pulling the whole weapon toward the left shoulder. That cleared the hand gap and the double-float, and it drove the shaft through the torso, the hair, and the right leg. This pass takes that pull out.

The thrust shaft now sits in front of the shoulder line and moves with the chest. It is not walked into the sternum. The left palm stays on that same handle. Where 36 cm toward the butt is outside the arm, the palm slides toward the right hand along the shaft, down to about 12 cm. The shaft itself is not moved to buy the spacing.

The throw windup is out to her right and forward, not vertical beside the ear. The shaft stays leaned forward until the hand is home, so it does not hang through the right thigh on the way back.

Upper-arm orientation is capped at 28° per frame from the idle pose, so the 67–72° single-frame swings are cut to that cap. Two left-arm steps at the end of the thrust are still over it: frame 50 is 35° and frame 52 is 40°.

## What the exported FBX measures

Staff distance to head/neck, to hips/spine, and to the upper legs. These are the regions that were cut on `37c62d0`. Hand contact is separate.

Thrust, left-hand skin vs the staff (532 verts), then head / hip / leg:

| frame | nearest | within 2 cm | head | hip | leg |
|---:|---:|---:|---:|---:|---:|
| 14 | 0.00 cm | 346 | 17.8 cm | 16.4 cm | 21.1 cm |
| 17 | 0.01 cm | 312 | 18.6 cm | 16.7 cm | 10.6 cm |
| 18 | 0.02 cm | 280 | 19.0 cm | 16.7 cm | 10.6 cm |
| 21 | 0.01 cm | 239 | 17.6 cm | 18.0 cm | 5.4 cm |
| 24 | 0.01 cm | 227 | 16.6 cm | 20.0 cm | 5.9 cm |
| 27 | 0.01 cm | 165 | 15.5 cm | 21.9 cm | 9.8 cm |

Frames 17–21 still touch the staff. The head and the hips are clear of it. The lead leg comes within about 5 cm of the butt at frames 21 and 36–40. That is not an intersection. It is the closest remaining body approach.

Throw, same regions. The left hand is not on the staff in this action:

| frame | head | hip | leg |
|---:|---:|---:|---:|
| 24 | 35.2 cm | 25.4 cm | 22.5 cm |
| 27 | 34.7 cm | 26.0 cm | 25.9 cm |
| 28 | 34.4 cm | 25.9 cm | 27.1 cm |
| 33 | 39.2 cm | 30.1 cm | 5.5 cm |
| 49 | 38.7 cm | 34.0 cm | 22.7 cm |

Frames 43 and 44 still have both soles down. Frame 45 lifts only the right foot.

The open hand has no finger bones. Where it meets the staff, hand triangles overlap the shaft (about 40–124 pairs on the thrust samples). That is the hand, not the torso. The bone still stops about 5 cm short of the planned saddle on some of those frames, and the skin reaches anyway.

Both actions are frames 1–52. Rest, idle, walk, cape, and the 24-bone bind were not edited. Front/side clips at full speed and quarter speed were rendered from this export. The previous hand close-up videos were the old export and have been removed.
