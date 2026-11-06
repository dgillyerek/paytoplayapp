# Seraphine FIX5 — motion target, still a draft

Branch `cursor/seraphine-fix5-attacks-d044`. Same PR. Not merged. Not Derek-ready. Unity was not run.

The review of `22ec83db` failed both attacks. This note is the motion that was authored after that, and what the triangle checks actually say. It is not a pass.

## Motion target

Thrust, 52 frames, 1.73 s. One shaft. The right palm is the upper grip and the left palm is 22 cm toward the butt on that same line. Both stay on it from frame 8 through frame 42, including the drive and the recovery. The line is steep and forward, and the pair moves forward together, because a flat line puts the 0.80 m butt through the cape and a point 22 cm up a steep line puts the left hand in the braid. The left foot lifts and steps, the right heel rises on a planted toe while the hips turn, then the lead foot lifts back to the idle mark. The right wrist stays the straight across-the-palm grip.

Throw, 54 frames, 1.80 s. The right hand rises to the shoulder and the staff head goes up and back, out to her right of the wing, with the same straight wrist. The release turns the shaft forward over about ten frames. The trailing foot lifts, plants, and returns to the idle mark. The left arm stays wide on her left as a counterbalance. Recovery brings the right hand down on her right, in front of the wing.

## What was measured

Two passes. The author samples the evaluated meshes in `SERAPHINE.blend`. `fix5_verify_fbx.py` imports the exported FBX in an empty scene and builds a BVH from the staff triangles, decorative collar included, against the body triangles, the hair, the wings, and the floor.

On the FBX reload:

- Thrust lowest staff vertex is 37 cm. No staff vertex is under 1 cm. At 0.55 s, which is frame 17, the low point is 46 cm, the left ankle is 33 cm up, and the right ankle is on its plant. The ground hit from the reviewed clip does not reproduce. It is not a floor-line illusion on this export: the vertices are above the floor.
- Throw lowest staff vertex is 42 cm. No staff vertex is under 1 cm. Wing distance stays at least 17 cm.
- Left-palm distance to the thrust shaft axis, frames 8 through 42, max 0.2 cm. There is no four-frame hole where the hand leaves and snaps back.
- Both ankles are never both in a high step at the same frame. A heel rise keeps the other foot down. One frame at the start of each lead step moves the ankle 2.2 cm while the heel is still coming up. That is the ankle orbiting the toe, not a planted skate.
- Wrist angle stays under 2° on the thrust and under 5° on the throw. Bind matrices match the rest rig. Cape is the original 11,773 faces.

The blend playback and the FBX reload agree that the staff does not meet the floor. The stills are the FBX reload, white body, dark staff, gray floor with a dark contact row. Straight front, strict side, and front three-quarter.

## What is still wrong

Throw staff-to-body distance is 0 cm on frames 14–19 and 35–37. That is the shaft against the shoulder and upper arm during the cock and the start of the recovery. The wing and the floor are clear there. It is not a clean body clearance, and it is not being reported as one.

The thrust left forearm still has samples about 1 cm from the torso while the palm is on the shaft. The hands are on the weapon. The forearm is close to the armor.

Fingers have no bones. The thrust line stays steep. No Unity playback.

This is not a visual pass.
