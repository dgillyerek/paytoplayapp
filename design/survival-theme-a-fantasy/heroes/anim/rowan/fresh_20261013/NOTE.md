# Rowan fresh rebuild (2026-10-13), HOLD

Derek approved the hero-blue front/back look (`source/ROWAN_FRONT_BACK_BLUE_LOCKED.jpg`). Design made the Meshy model from it (`source/MESHY_SOURCE.md5`; GLB 1.6M tris and zip not committed). This replaces the failed #54/#50/#45/#35 packs. The game setup and the bow attack idea come from `cursor/rowan-nocape-20261008`.

- **Model (`work/dec.py`):** duplicate verts merged, decimated (collapse) to 199,999 faces / 99,753 verts, scaled to 1.80 m, feet on 0. Meshy UVs and maps kept (base colour, normal, metallic-roughness 2048²).
- **Bow on his back:** Meshy fused the slung bow into the coat surface. Deleting it left see-through holes in the back of the coat (`work/nb5_views.png`), so it stays on his back, 100% `Spine2` (3,985 verts). In the attack, a separate hand bow (`props/ROWAN_bow.fbx`) shows from f4 to f24, so **two bows are visible during the draw**. The fix is a Meshy pass from a bow-less image, or a hand repaint/patch.
- **Rig (`work/rig.py`, Blender 5.2.2):** 20 mixamorig bones (no ToeBase), max 3 influences.
  - Face, skull and hair are 100% `Head` above a tilted plane (1.50 m at the front to 1.53 m at the back), with a ±0.8 cm band.
  - Tunic, coat flaps, belt and pouches: everything in the torso part below 1.02 m is 100% `Hips`. Zero leg weight (`loincloth_with_leg_weight: 0`).
  - The short capelet rides the spine chain and the Shoulder bones (the part over the upper arm follows the arm).
  - Arms are ripped from the coat/torso below the armpit (1,539 faces), and the legs from the coat below the hip ramp (1,168 faces), so nothing webs when the arms rise (`work/av_b.png`).
- **Props (`work/props.py`):** `ROWAN_quiver.fbx` is a leather quiver with 7 fletched arrows, rigid on `Spine2` over the right shoulder in every clip (idle prop that stands in for the attack track). Also `ROWAN_bow.fbx` (wooden recurve, grip at the origin) and `ROWAN_arrow.fbx`.
- **Actions (`work/anim.py`):**
  - Rest is the bind pose with arms hanging by the sides.
  - Walk is in place, 1 s, 30 fps, ankles 22–23 cm apart, head forward with a tiny nod.
  - Attack: bow raised and nocked at f6. Full draw at f10, with the chest turned 24° and the left shoulder leading, and RightHand at the jaw by IK. Loose at f14 (14 m/s, hero-blue trail), recoil at f17, rest at f30. Feet planted, head forward.
- **QC (`work/sheet.json`):**
  - Face/skull deviation from the rigid Head = 0.000 cm in every frame.
  - Neck–head band max: 0.55 cm in the walk, 3.55 cm in the attack (neck counter-turn).
  - Arrow direction in Unity: (−0.05, −0.01, 1.00).
- **Known limits:**
  - The coat is rigid on Hips, so the thighs pass through the front flaps at full stride.
  - A few coat tatters that sat behind the fists show at the hips when the arms rise.
  - The bow stave crosses the lower face in the front view at full draw.
- **Sheets:** `sheets/ROWAN_fresh_{rest,walk,attack}_sheet.jpg`, each with front, face close-up, side and back rows.
- **Unity:** ThemePack `heroes/3d/rowan/ROWAN_blenderig*.fbx`, Generic (`animationType: 2`). Open it with Survival → Rowan Demo.
