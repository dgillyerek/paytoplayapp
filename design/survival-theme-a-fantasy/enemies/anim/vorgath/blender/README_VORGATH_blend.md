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
