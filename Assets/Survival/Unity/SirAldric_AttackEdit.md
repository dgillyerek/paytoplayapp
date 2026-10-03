# Sir Aldric — Mixamo DIAG attack (`SirAldric_DIAG_InwardSlash`)

**HOLD.** Game-view is SoT. PlayableGraph plays the Mixamo **DIAG** human take (`Stable Sword Inward Slash`, md5 `72412be4`) as `Assets/Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.anim`.

**Compile:** clip **name must be `SirAldric_DIAG_InwardSlash`**, same as the file. Naming it `Attack` does not match.

The `7958283` held-pose robot clip is discarded. Arm / shoulder / torso move together from the Mixamo take — no snap between frozen poses, no post-Evaluate AimArmAlong.

Story (rear cam / Derek POV):

1. Walk straight (Walk FBX).
2. Sword starts at the **LEFT hip** and is drawn (`u < 0.18`).
3. Mixamo rise **above the head, RIGHT** (DIAG TOP ~f59/85).
4. Strike **FORWARD**, then **DOWN toward the foot**.

Walk after draw: `mixamorig:RightHand`. Orbit 1/2/3 unchanged. No DIAG_REV / DIAG_MIRR / MILD / Mixamo Mirror.

## Animation window (optional)

1. Open `Assets/Survival/Scenes/SirAldric.unity`.
2. **Survival → Sir Aldric → Spawn knight for Animation-window edit**.
3. **Window → Animation → Animation**. Select root **`SirAldricAttackEditKnight`** → **Lock** → **Preview** → scrub `mixamorig:RightArm` / `mixamorig:RightHand`.
