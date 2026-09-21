# Path 2 — Meshy Flagship Image-to-3D (LOOK)

Design drop: Meshy Flagship from locked rear SoT, Blender Decimate to ~50k faces
because the original (~57MB / 1.77M faces) exceeded upload cap.

- `out/aldric_meshy_retopo.glb` — 8babba7 drop + paint iterate (UV bleed + SoT lion cards)
- ThemePack copy: `Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_meshy_retopo.glb`
- Unity-oriented DCC: `sir_aldric_meshy.blend` (Y-up, +Z face, scabbard +X)

Rebuild look: `blender --background --python scripts/blender/repair_path2_paint.py` then `import_sir_aldric_meshy.py`  
Skin (old Bone1, failed twice): `scripts/blender/skin_sir_aldric_meshy.py`  
Skin (premise B hang-heat, failed e56aeb1): `scripts/blender/skin_sir_aldric_hangheat.py`  
Skin (escalation remesh+FBX, Derek STOP as hero look): `scripts/blender/skin_sir_aldric_retopo.py`  
Skin (Meshy-look e5b132f hang-skin): `blender --background --python scripts/blender/skin_sir_aldric_meshylook.py`  
Census: `blender --background --python scripts/blender/census_path2_bind.py`

**Look PASS** on `e5b132f`. Remesh/capsule Game-view STOP'd. **Bind NOT claimed. Walk-with-look NOT claimed.** Play hub PNG stays locked.
