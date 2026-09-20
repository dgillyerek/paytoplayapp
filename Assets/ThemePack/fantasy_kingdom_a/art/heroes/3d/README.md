# Sir Aldric 3D (Theme A) — Blender / FBX

**Path B.** Mid-poly modeled in Blender from `look_targets` (`01_rear_LOCKED` = primary Game-view SoT). Box-atlas iteration is stopped.

Runtime bind (Editor-less VM): `sir_aldric_midpoly.mesh.txt` + `sir_aldric_atlas.png` on `SirAldric3DActor` (same bones / `Evaluate()` clip).

DCC / Unity import: `sir_aldric.fbx` + `sir_aldric.blend`. Humanoid slot map: `sir_aldric_humanoid.json`. Rest pose is the Actor hang (not T-pose) so Walk/Attack locks do not regress.

Must-match:
- Silver plate + gold trim (helm crest, pauldrons, gauntlets, greaves, boots)
- Royal-blue short surcoat: gold lion rampant + gold Greek-key hem
- Brown scabbard + gold fittings on **character-RIGHT** hip only (no back sheath)
- Boots silver+gold (not brown leather)

Look gate is **not claimed** until Derek PASSes the Game-view clip.
Play hub stays `SIR_ALDRIC_REAR_MASTER_LOCKED.png`.
