# Sir Aldric Hole-Fixed Separate-Portrait Mixamo Animations (2026-09-28)

## Overview
- Character: Sir Aldric (Pilot Look, hole-fixed source, no cape, empty right hand)
- Source Model: `../meshy_body_holefixed/SirAldric_body_holefixed_mid280k.fbx` (280,000 faces, Blender COLLAPSE decimation preserving UVs)
- Account / Session: Derek (signed in at mixamo.com)
- Auto-Rig Status: **PASS** (completed on marker attempt 1 using custom marker placement: Chin, Wrists, Elbows, Knees, Groin; Standard 65-bone skeleton)

## Animations & Exports
Both animations exported in FBX format "With Skin" at 30 fps without keyframe reduction:

1. **Walk**:
   - Mixamo clip: `Standard Walk`
   - File: `SirAldric_body_holefixed_walk.fbx` (alias `Standard Walk.fbx`)
   - Frames: 34 frames (looping walk cycle)
   - Stills: `stills/walk_stride_front.png`, `stills/walk_stride_34.png`, `stills/walk_stride_rear.png` (captured at mid-stride frame 18)

2. **Planted RH Sword Slash**:
   - Mixamo clip: `Stable Sword Inward Slash`
   - File: `SirAldric_body_holefixed_slash.fbx` (alias `Stable Sword Inward Slash.fbx`)
   - Frames: 67 frames (planted stance one-beat inward sword slash)
   - Stills: `stills/slash_peak_front.png`, `stills/slash_peak_34.png`, `stills/slash_peak_rear.png` (captured at peak impact frame 32)

## QC & Verification
- Armature & Bones: Clean 65-bone Mixamo standard hierarchy (`Armature`, `mixamorig:Hips`, `mixamorig:Spine`, etc.)
- Mesh: Preserved single mesh component (`Mesh_0`), 279,998 faces, vertex weights cleanly bound
- Visual Stills: High-fidelity Cycles renders with original Meshy PBR diffuse texture applied. Both animations confirm solid foot placement on ground, natural deformation across shoulders and hips, empty right hand cleanly curved to receive the weapon prop, and zero cape or unintended geometry.
- Props: Cape and sword remain separate; neither prop was merged or attached during rigging.
