# Rowan Meshy no-bow: skin fix (2026-10-08)

Derek's FAIL on PR #54: "rowan is all distorted and doesn't look good" (bent bow, lumpy arm).

## Root cause
Not Unity. The import settings are fine (Generic, file scale, no compression, 4 bones/vertex). The body
skin Meshy's auto-rigger produced is broken, and Meshy's own preview shows the same warp:

* **The bow was part of the body mesh**, weighted to the upper arm, the fingers and the right leg, so it
  bent every frame. Design's no-bow pack (`meshy_nocape_nobow_20261008`) already fixes this by cutting
  the bow out and using a rigid bow prop. **That part is fixed upstream; nothing here touches the bow.**
* **The body still had Meshy's broken weights** (Design's NOTE: "not fixed because brief said not to re-skin"):
  * Static helper bones that no clip animates: Bone_025-029 hang under Spine2 but skin the back of the
    right arm and shoulder, which pins them to the chest (the lumpy arm and the right-arm flap at attack f25-27).
    Bone_020/021 hang under Head but skin part of the left hand. Bone_056/057 sit under the hands.
  * Cross-limb bleed: left torso and hip verts weighted to LeftArm, plus pouch and belt verts weighted to
    fingers. These become the shards under the left forearm while the bow is drawn.
  * Up to 12 influences per vertex and 12k weights under 0.05.
  * The fists are welded to the belt pouches by a few triangles that tear into slivers when the arm moves.

## Fix (weights + 63 weld triangles only)
`tools/port_nobow.py` runs the same cleanup as the fused-bow attempt (`tools/reweight.py`) on the untouched
Meshy source, then copies the weights onto Design's no-bow body by bind position (14,631 of 14,635 verts
match exactly; the 4 fist-cap centres take their ring's average). The right fist keeps its hand weights so
Design's bow prop stays in the hand. The steps:
* Helpers on an arm go to that arm, and otherwise to their animated ancestor. The Spine2 chain blends from
  Spine2 into the right arm across the shoulder.
* Arm weights on verts that are not on that arm move to the nearest trunk bone.
* Below the elbow, the arm and body are split where the left hand touches the body.
* Weights under 0.05 are dropped, max 4 influences, smoothed 4x inside each part.
* Wherever Meshy's skin never stretched more than 3 cm in any frame, Meshy's weights are kept.
* Triangles that join a fist to the belt/pouch and still stretch more than 3 cm are removed (63).

Vertices, UVs, normals, paint, bones, bind pose and every animation curve are byte-identical to Design's
files. Only the cluster Indexes/Weights arrays and the face lists change (custom FBX binary writer; an
unchanged read/write round-trip reproduces Design's files byte for byte).

Edges that grow more than 10 cm in any frame (Blender 4.3, every frame):

| clip   | Design no-bow | skin fix |
|--------|--------------:|---------:|
| rest   | 48            | 0        |
| walk   | 71            | 0        |
| attack | 676           | 22       |

The 22 left in attack are the right armpit when the right arm goes up to draw.

## Files
* `rowan_meshy_nobow_skinfix_contact_sheet.png`: before/after, front and rear battle camera, with
  Design's bow prop (rest/walk on RightHand via the meta offset; attack uses the baked bow + arrow).
* `tools/build.sh <repo> <out>` rebuilds the three ThemePack bodies byte for byte (see CHECKSUMS.md5).
* Not pushed: the earlier fused-bow skin fix (rigid bow on the forearm, commit 0193f35) was overtaken
  by the no-bow pack. It is kept as local branch `backup/rowan-skinfix-fused-0193f35` on the agent box.

## Only Unity Game view can confirm
The rendered look in Game view (1080x1920), that the shards and right-arm flap are gone at runtime, that the
63 removed weld triangles leave no visible holes at the fist/pouch contacts, and that the idle bow still
sits in the right fist (hand weights were cleaned, so the fist surface may shift by a few mm).
