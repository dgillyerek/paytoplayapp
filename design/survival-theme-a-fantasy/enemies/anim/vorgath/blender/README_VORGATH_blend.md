# VORGATH.blend (Derek 20-bone skeleton)

Single-file layout: object `VORGATH_rig` (armature, Derek's skeleton unchanged) + `VORGATH_body` (skinned mesh).
File saved in OBJECT mode, rest pose, no active action.
Also contains the empty `VORGATH_mouth_socket` (bone-parented to `Bone.004`, +Z = fire-stream direction). It is NOT exported with the character FBXs and is not a bone. See `../vfx/DEV_NOTE_VORGATH_spit_fire.md`.

## Skeleton (20 bones, 5 roots)
- Spine/head: `Bone` (root, carries root motion) > `Bone.001` > `Bone.002` > `Bone.003` > {`Bone.004` head/snout, `Bone.005` jaw}
- Arms: `mixamorig:{Left,Right}Shoulder` > `.001` > `.002` > `.003`
- Legs: `mixamorig:{Left,Right}UpLeg` > `.001` > `.002`

## Actions (fake user, each on its own muted NLA track)
| action | frames |
|---|---|
| attack_slam | 1-112 (leap, hands impact floor at f52) |
| attack_spit | 1-163 (head+jaw lunge peak f60) |
| idle | 1-160 |
| walk | 1-44 (loop) |

20 vertex groups, one per bone; weights normalized (sum 1.0).

## FBX export
`Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/vorgath/VORGATH_{rig_rest,idle,walk,attack_slam,attack_spit}.fbx`
scale 1.0, forward -Z, up Y, armature + VORGATH_body only, baked animation per action.
Note: bones no longer use Mixamo Hips/Spine/Arm names; `NewCharsPreview.cs` lookups such as `mixamorig:Hips` won't resolve for Vorgath.

## Fix3 (2026-11-05)
- `attack_slam`: from the impact (f44-50 ease-in) through the end (f112), only the forearm twist (`*Shoulder.002`) and wrist (`*Shoulder.003`) rotation keys changed. The claws now hang forward and down instead of bending back and out beside the hips. Skeleton, weights, mesh and the other clips are unchanged.
- `attack_spit`: the animation is unchanged. Fire-stream window f53-75 (full f56-70). Mouth socket and VFX flipbook are in `../vfx/`, and the reference FBX with the socket node is `../final/VORGATH_mouth_socket_REF.fbx`.

## Two-handed clasp slam (2026-11-15)
- `attack_slam` arms: both arm chains (`*Shoulder`, `.001`, `.002`, `.003`) were baked from IK. After the push-off (eased in f17-26) the hands meet palm to palm above his head and stay together through the apex (f26-40). They swing forward over the head (f40-48) and smash down together onto one spot on the ground in front of his snout at f52 (contact about -1 cm, small rebound, settle again at f60). Then they lift and release back to the original recovery pose (f60-74, blended out by f82). The hands are offset half a finger width in opposite directions so their fingers alternate. Left arm: Blender IK (chain 3) with a pole out to the side. Right arm: Blender's IK pole flipped that elbow across behind the head (the two arms have different bone rolls), so `.001`/`.002` are solved analytically instead. The elbow is held in the plane of a pole 0.9 m out to his right, 0.3 m forward and 0.35 m down from the shoulder, and the bones' twist is kept as close as possible to the original animation, so the two elbows mirror each other. The right clavicle is unchanged. Legs, spine and head are unchanged.
- New shape keys `Clasp_25`, `Clasp_50`, `Clasp_75`, `Clasp` on `VORGATH_body` (skeleton unchanged, still 20 bones, no finger bones). Each of the three fingers rolls toward its own palm about its own three knuckles (85/75/50° at full), the claws are carried by the last segment, and the thumb folds in (75°/30°). With the hands palm to palm, each hand's fingers wrap over the back of the other hand between its fingers.
- The stages exist because blend shapes interpolate in straight lines. One open-to-curled key collapses the fingers into the palm halfway, so the clip cross-fades neighbouring stages instead.
- New action `attack_slam_clasp` (shape-key action, fake user, muted NLA track on the Key, keyed every frame, linear): open f1-22, fingers lace f22-30 as the hands meet overhead, held through the swing and the f52 impact until f62, unlaced by f74.
- Export: `VORGATH_rig_rest.fbx` carries the four `Clasp*` blend shapes; `VORGATH_attack_slam.fbx` bakes the rig action and `attack_slam_clasp` (take `Scene`). The other clips don't key the shapes, so `VorgathActor` / `NewCharsPreview` zero blend shapes before each sample (`BlendShapeReset`).
