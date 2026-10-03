# Sir Aldric — Sword And Shield Slash

**HOLD.** Game-view is SoT. Play: Standard Walk, then Design `SirAldric_body_holefixed_slash_SWORD_SHIELD_ATTACK.fbx` on **its own Mixamo auto-rig**.

74 frames / 30 FPS / Mirror off. Not the 53-frame Sword And Shield Attack. Do not remap this clip onto the walk skeleton. Leftover Scene empties under `SirAldricStrikePath` are **inactive**:

1. `1_Draw_LeftHipPocket`
2. `2_Raise_AboveHeadRight`
3. `3_Strike_Forward`
4. `4_Strike_DownToFoot`

Sword on slash = identity parent on this FBX `mixamorig:RightHand`. No blade pitch. No shield prop. Leftover `SirAldric_DIAG_InwardSlash.anim` unused at Play (clip **name must stay `SirAldric_DIAG_InwardSlash`** if you open it — Attack as clip.name does not match).

The `7958283` held-pose robot clip is discarded. Leftover AimArmAlong / MixamoDiagPlaybackU / MixamoDiagStrikeBladeLocal / ReachRightArmToward unused.

## Animation window (optional leftover)

1. Open `Assets/Survival/Scenes/SirAldric.unity`.
2. **Survival → Sir Aldric → Spawn knight for Animation-window edit**.
3. **Window → Animation → Animation**. Select root **`SirAldricAttackEditKnight`** → **Lock** → **Preview** → scrub `mixamorig:RightArm` / `mixamorig:RightHand`.
