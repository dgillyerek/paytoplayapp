# DEV NOTE: Vorgath spit fire stream (fix3)

Design delivers the art, the socket numbers and the timing. **Dev has to wire the VFX hook in Unity.** Design made no `.cs` or Unity scene edits. The character FBXs still have the same 20 bones, and no socket bone was added.

## 1. When the stream is on (`VORGATH_attack_spit.fbx`, 163 frames, 30 fps)
The window comes from the mouth opening: the angle between the jaw (`Bone.005`) and the snout (`Bone.004`) is 58° at rest. It rises from f52, holds at +25° from f57 to f67, and is back to rest by f77.

| phase | frames | clip time (s) = (f-1)/30 | normalized (f-1)/162 |
|---|---|---|---|
| stream ON, ramp up (length grows 0 to full over 4 frames) | f53 | 1.733 | 0.321 |
| full intensity | f56 to f70 | 1.833 to 2.300 | 0.340 to 0.426 |
| ramp down (fade alpha and shrink) | f71 to f75 | 2.333 to 2.467 | 0.432 to 0.457 |
| OFF | f76 | 2.500 | 0.463 |

The stream should stay on, unbroken, from f53 to f75. During that window the head sweeps from his left to his right (f53 to f75), so the stream sweeps across the ground in front of him. It aims about 30° below horizontal and meets the ground about 2 to 2.5 m out. A small ground-impact glow or decal at the hit point sells it (see the preview).
For Animation Events, use two events: `FireStart` at 1.733 s and `FireStop` at 2.467 s, with a fade of about 0.17 s on each.

## 2. Mouth socket (not a bone)
- Parent: **`Bone.004`** (the head/snout and upper-jaw bone, a child of `Bone.003`).
- In `blender/VORGATH.blend` it is the empty **`VORGATH_mouth_socket`**, bone-parented to `Bone.004`. Its +Z arrow points along the stream.
- Rest-pose position in Blender armature space (Z up, the character faces -Y): **(0.030, -0.500, 1.980) m**. Stream direction: **(0.00, -0.96, -0.28)**, which is forward and 16° down. This point sits just inside the open mouth, behind the lip line. A winding-number test confirms it lies in the mouth cavity and not inside the mesh on any spit frame.
- Offset in **`Bone.004` local axes, measured from the bone HEAD** (Blender bone frame, Y = along the bone): **(-0.0018, 0.3530, 0.1983) m**. Direction in the same frame: **(-0.034, 0.947, 0.319)**. Bone length is 0.3447 m, so from the bone tail the offset is (-0.002, 0.008, 0.198).
- Unity model space at bind pose, assuming the usual Blender (-Z fwd, Y up) to Unity mapping (x,y,z) -> (-x, z, -y): **position (-0.030, 1.980, 0.500)**, **forward (0.00, -0.28, 0.96)**. Unity's FBX import changes bone local axes, so **don't copy the Blender bone-local numbers straight into localPosition.** Pick one of these instead:
  1. Easiest: import `final/VORGATH_mouth_socket_REF.fbx`. It is a bind-pose reference: the same rig and mesh with no animation, plus the empty as a child node of `Bone.004`. Unity creates `VORGATH_mouth_socket` as a child GameObject of `Bone.004`. Copy that child's localPosition and localRotation onto a new child of `Bone.004` in the real prefab, then drop the reference model. Use it only as a reference, never as the character.
  2. Or, with the prefab at bind pose: create an empty child of `Bone.004`, set its world position from the model-space point above and its forward (+Z) from the direction above, then keep it parented.
- Check: on spit f60 to f62 the socket gizmo should sit inside the open mouth, with +Z pointing out between the teeth.

## 3. VFX asset: `vfx/VORGATH_fire_stream_flipbook_4x4.png`
- 1024x1024 RGBA. 4x4 = 16 frames, 256 px cells, read row by row from the top-left. It loops seamlessly, so play it at 20 to 30 fps (16 frames at 24 fps is about 0.67 s per loop).
- Cell layout: the **mouth end is at the left edge (U=0)** and the stream runs to the right (U=1). V=0.5 is the centerline. It is narrow and white-gold hot near the mouth, with an orange body and red, broken-up edges and tail. The alpha is soft, with no hard cell borders.
- Colors follow Theme A fantasy: core about #FFF7C7 to #FFDB66, body #FFA81F to #FF6B0D, edges #D9290A to #8C0A05.
- **Suggested setup (pick one):**
  - **Stretched mesh strip (recommended):** a quad strip or LineRenderer of 8 to 12 segments from the socket along socket +Z, about 2.6 m long, with a little droop (about -0.12·t² m) and clipped at the ground. Width runs from about 0.18 m at the mouth to about 0.9 m at the end. Map U along the length and V across it. Use the flipbook via `_MainTex` tiling (0.25, 0.25) with a frame offset, or a Flipbook node. Use Additive or Alpha-blend Premultiplied with the Particles/Unlit shader, and billboard it around the length axis (LineRenderer Alignment = View).
  - **Particle System:** shape Cone (angle 6 to 8°, radius 0.05) at the socket. Start speed 6 to 8 m/s, lifetime 0.35 to 0.45 s, emission about 60 to 80/s. Renderer = Stretched Billboard (Speed Scale about 0.05 to 0.08, Length Scale 1 to 2). Texture Sheet Animation 4x4, cycles 1, random start frame. Size over lifetime grows from 0.25 to 1.0, and color over lifetime runs gold, orange, red, then fade.
  - Add an optional point light (orange, intensity 2 to 3, range 3 m) and a ground glow or scorch decal at the ray hit.
- At phone size, keep the core bright and the stream at least about 40 px wide on screen.

## 4. Unity work Dev owns (not done by Design)
- Create the socket child under `Bone.004` (above), add the fire VFX prefab under it, and toggle it with `FireStart`/`FireStop` events or the clip-time window in `NewCharsPreview` or the enemy controller. These are `.cs` edits, so Dev does them.
- Add `VORGATH_fire_stream_flipbook_4x4.png` to Assets with the import settings Alpha Is Transparency, Wrap Clamp, no mipmaps or with a mip bias, and Max Size 1024.
