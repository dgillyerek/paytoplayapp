# Seraphine FIX5 — hand contact correction, still a draft

Branch `cursor/seraphine-fix5-attacks-d044`. Same PR. Not merged. Not Derek-ready. No independent video pass on this export yet.

Resumed from the authored samples already in `/tmp/sera_fix5` and the script on this branch. No screenshot checkpoint file was on disk. The solver was not rewritten. One contact change: the left palm is 36 cm toward the butt on the shaft the right hand owns, and the right arm is a two-bone elbow instead of a forearm locked square to the staff.

## What that does

Thrust, 52 frames. From frame 8 through 42 the left palm stays on that line. Measured distance from the left hand to the shaft axis in that span is 0 to 4.6 cm. At frame 14 it is 3.5 cm and the wrist is 23°. At frame 24 it is on the axis and the wrist is 28°. The largest right-wrist angle in the thrust is 31°. The throw's largest is 53°. Neither crosses 70°, and the close-ups at those frames do not show the hand flipped back on itself. Fingers have no bones, so the palms do not wrap.

Hips are not held at one height. At the thrust load the hip is about 10 cm under the idle height, which is the knee bend. Horizontal hip position stays on the idle mark, which is what HoldHips pins. The chest turns the right shoulder forward by frame 14 so the shaft sits in front of the armor.

Floor test is every staff vertex, collar included, on a fresh FBX import. Thrust low point 46 cm. Throw low point 47 cm. No vertex under 1 cm. The blend playback and the FBX reload agree on that.

## Still wrong

Throw frame 28: the staff surface is 0.2 mm from a wing and touching the body. That is the shaft at the shoulder as the cock turns forward, not a floor hit. The side still shows the shaft in front of the torso. It is not a clean wing clearance.

The thrust forearm is still close to the armor on some frames. The cape is the original mesh. 24 bones, bind matches the rest rig. No Unity playback.

This is not a visual pass.
