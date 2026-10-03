# Sir Aldric — walk only

**HOLD.** Game-view is SoT. Play loops the Standard Walk FBX. **No attack.**

Derek reverted ALL attacks. Play does not play Mixamo DIAG, the rebuilt clip, the four-marker path, blade pitch, or any sword swing. Leftover Scene empties under `SirAldricStrikePath` are **inactive** and do not drive motion:

1. `1_Draw_LeftHipPocket`
2. `2_Raise_AboveHeadRight`
3. `3_Strike_Forward`
4. `4_Strike_DownToFoot`

Sword stays the walk `mixamorig:RightHand` grip. Leftover `SirAldric_DIAG_InwardSlash.anim` unused at Play (clip **name must stay `SirAldric_DIAG_InwardSlash`** if you open it — Attack as clip.name does not match).

The `7958283` held-pose robot clip is discarded. Leftover AimArmAlong / MixamoDiagPlaybackU / MixamoDiagStrikeBladeLocal / ReachRightArmToward unused.

## Animation window (optional leftover)

1. Open `Assets/Survival/Scenes/SirAldric.unity`.
2. **Survival → Sir Aldric → Spawn knight for Animation-window edit**.
3. **Window → Animation → Animation**. Select root **`SirAldricAttackEditKnight`** → **Lock** → **Preview** → scrub `mixamorig:RightArm` / `mixamorig:RightHand`.
