# Sir Aldric Painted Midpoly Unity Handoff

Generated 2026-09-25 11:55 PM ET under this package root. The painted GLBs were imported in Blender 4.2.3 and reduced with a **Decimate / COLLAPSE** modifier (edge collapse; no voxel/remesh/melt). Rest pose/A-pose was not changed and no rigging was performed.

## Inputs
- `assets/SirAldric_SEP_body_nosword_PAINTED.glb`
- `assets/SirAldric_SEP_sword_scabbard_PAINTED.glb`
- Extracted PBR maps under `assets/textures/sep_body_*` and `assets/textures/sword_*`

## Outputs and verification
| Asset | Output files | Faces before -> after | UV/material verification |
|---|---|---:|---|
| Painted body | `assets/SirAldric_SEP_body_nosword_PAINTED_mid200k.glb` and `.fbx` | 2,137,452 -> 200,000 | UV present (1 layer), 1 material slot; GLB roundtrip has embedded 3 x 2048 maps |
| Painted sword + scabbard | `assets/SirAldric_SEP_sword_scabbard_PAINTED_mid.glb` and `.fbx` | 247,420 -> 50,000 | UV present (1 layer), 1 material slot; GLB roundtrip has embedded 3 x 2048 maps |

Both GLB and FBX exports were re-imported in Blender and verified at the listed face counts with UVs intact; raw GLB inspection confirms `TEXCOORD_0` on both meshes. FBX roundtrip also retains basecolor/normal image data; use the GLB when the full embedded metallic/roughness PBR set is required.

## Stills
- `stills/body_painted_midpoly_viewport.png`
- `stills/sword_painted_midpoly_front.png`
- `stills/sword_painted_midpoly_34.png`
- `stills/sword_painted_midpoly_rear.png`

The stills show the painted materials (blue/gold armor, metal, leather, and scabbard), not a clay/remeshed result.

## Animation / Unity handoff
- `assets/SirAldric_SEP_meshy_animate_walk.fbx` was not overwritten; it remains the clay motion SoT.
- `assets/SirAldric_SEP_meshy_animate_attack.fbx` was not overwritten; attack remains **FAIL pending re-export**.
- Dev binds the painted atlas/materials onto the AccuRIG Humanoid and parents the scabbard at the character-RIGHT hip.
