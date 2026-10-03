# Sir Aldric — Scene-marker strike path

**HOLD.** Game-view is SoT. Walk stays the Standard Walk FBX. Attack is **not** Mixamo DIAG playback.

Drag these Hierarchy empties in **Scene** view (Gizmos on). Next Play follows the new positions — no code change.

1. `SirAldricStrikePath` / **`1_Draw_LeftHipPocket`** — left hip pocket (draw)
2. `SirAldricStrikePath` / **`2_Raise_AboveHeadRight`** — above the head, right side
3. `SirAldricStrikePath` / **`3_Strike_Forward`** — forward strike
4. `SirAldricStrikePath` / **`4_Strike_DownToFoot`** — down toward his foot

Colored gizmo spheres + labels. Right hand / sword follows a Catmull arc; shoulder and elbow bend toward each point. No frozen holds. No Mixamo Mirror. Leftover `SirAldric_DIAG_InwardSlash.anim` is unused at Play (clip **name must stay `SirAldric_DIAG_InwardSlash`** if you open it — Attack as clip.name does not match).

The `7958283` held-pose robot clip is discarded. Leftover AimArmAlong / MixamoDiagPlaybackU / MixamoDiagStrikeBladeLocal unused.

## Animation window (optional leftover)

1. Open `Assets/Survival/Scenes/SirAldric.unity`.
2. **Survival → Sir Aldric → Spawn knight for Animation-window edit**.
3. **Window → Animation → Animation**. Select root **`SirAldricAttackEditKnight`** → **Lock** → **Preview** → scrub `mixamorig:RightArm` / `mixamorig:RightHand`.
