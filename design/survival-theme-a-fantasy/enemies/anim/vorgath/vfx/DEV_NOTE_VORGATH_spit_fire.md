# DEV NOTE: Vorgath spit fire stream (FIX6)

The mouth socket, FireStart / FireStop, and the spit clip are unchanged. FIX6 replaces the stream itself. Body, skeleton, slam, and cape are not touched.

## What the screenshot actually was

On main `02b3660e` the preview does **not** run a ParticleSystem. `VorgathSpitStream` built one `LineRenderer` with `Segments = 10` (evenly spaced) and `Universal Render Pipeline/Particles/Unlit`.

That shader samples `_BaseMap` with `UnityBuildTexture2DStructNoScaleNoTexelSize`, so it **ignores** material scale and offset. `SetFrame()` wrote scale `(0.25, 0.25)` and a cell offset every frame, and that window never reached the sampler. The GPU sampled the line's mesh UVs against the **whole** FIX5 4×4 sheet.

Measured on that sheet (all 16 cells): one rounded slug per cell, opaque-core mean RGB about `(254, 253, 197)`, **0%** orange and **0%** red inside alpha > 200, and a clear gutter. Additive blending plus a white vertex color keeps that core clipped. The only spacing in the effect is the 10 line segments. The polyline stops at the ground, so a pitched stream that hits inside 2.6 m keeps about 8 of them. That count moves as the length ramps and as the head sweep changes the hit. That is the row of 7 or 8 pellets. It is not an emission rate.

`textureMode` is `Stretch`, so mesh U runs 0 at the mouth to 1 at the tip. Because the shader ignores the 0.25 window, that stretch showed the **entire** 16-cell sheet: a 4×4 of gapped yellow-white slugs, then additive white. `SetFrame` animated nothing.

FIX6 keeps that exact shader (`Universal Render Pipeline/Particles/Unlit`, no Sprites fallback) and does not animate scale or offset. Each of eight strips is one 1024×256 flame. Playback swaps the whole texture at 24 fps.

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

## 3. VFX (FIX6): eight whole strips on the same Particles/Unlit line

The prefab and `ApplyClipTime` hook are unchanged. No new animation events.

- Strips in this commit: `Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/vorgath/vfx/strips/VORGATH_fire_f00.png` … `f07.png`. Byte-identical to the uploaded `VORGATH_breath_strip_f00.png` … `f07.png` from `vorgath-fix6-art` `a864f42f` (that filename was the source name; the script loads `VORGATH_fire_f`). 1024×256 RGBA. Mouth at the left edge. Importer: alpha from the PNG, Alpha Is Transparency, uncompressed, Clamp, no mipmaps, NPOT none, max 1024. Not grayscale-to-alpha.
- Body line: `Universal Render Pipeline/Particles/Unlit` only (no Sprites fallback), `_Blend` 0, `SrcAlpha` / `OneMinusSrcAlpha`, `_SURFACE_TYPE_TRANSPARENT`, flipbook keyword off. Vertex color alpha is the fade.
- Core child `FireCore`: same shader and the same strip, `_Blend` 2 (`SrcAlpha` / `One`), vertex color alpha `0.55 × fade`.
- Width 0.55 m at the mouth to 1.25 m at the tip. Length 2.6 m, droop `-0.12·(dist/2.6)²`. The tip stops at world `GroundPlaneY + 0.12` with `GroundPlaneY = 0` (the original floor). Points are world positions. The original tip was `0 + 0.02`.
- Frame = `floor(timeOn × 24) mod 8`. The material texture is replaced. Scale and offset are never animated.
- The FIX5 4×4 PNG stays in the project and is not assigned. If a strip file is missing the stream stays off. It does not fall back to that sheet.

`README_FIX6.md` and the strip generator were not in the upload that carried these eight PNGs, so they are not in this commit. The pixels are.

## 4. What Dev checks

Open Survival → New Characters Preview, Vorgath, `attack_spit`. One stream from the mouth for 1.733–2.467 s, whole strip changing, not a row of pellets.

**Unity UNRUN.** Unity `6000.3.6f1` is not installed. There is no GPU (`/dev/dri` is absent). Editor batchmode Play was not run, and Unity was not downloaded. An external CPU rasterizer (ufbx plus a C z-buffer) was used earlier on a different stand-in texture. That was not Unity, and those pictures are not in this commit.

## 5. What FIX6 did not change

- No skeleton, FBX, slam, cape, mouth-socket numbers, FireStart/FireStop, droop, or length.
- `NewCharsPreview` still calls `ApplyClipTime`. No other character.
