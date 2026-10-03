# Sir Aldric — attack clip (`SirAldric_DIAG_InwardSlash`)

**HOLD.** Game-view is SoT. PlayableGraph plays `Assets/Survival/Unity/Anims/SirAldric_DIAG_InwardSlash.anim`.

**Compile:** the AnimationClip’s **name must be `SirAldric_DIAG_InwardSlash`**, same as the file. Naming it `Attack` does not match `SirAldric_DIAG_InwardSlash` and Unity will not compile the asset.

Derek is done hand-keying. Dev owns the motion. Sequence (rear cam / Derek POV):

1. Walk straight (Walk FBX).
2. Reach down into the **LEFT hip pocket** and pull the sword out.
3. Raise the sword **directly above the head, RIGHT side**.
4. Strike **FORWARD**, then **DOWN toward his foot**.

Rebuilt 3.20s mixer clip with **held beats** (not the ddda018 unit-sphere smear):

| Beat | Clip u | Time | Pose |
| --- | --- | --- | --- |
| Draw | 0.10–0.28 | **0.32–0.90s** | RH in LEFT hip pocket, sword on left hip |
| Raise | 0.40–0.56 | **1.28–1.79s** | sword above head, RIGHT side |
| Forward | 0.68–0.80 | **2.18–2.56s** | strike FORWARD (+Z / enemy) |
| Foot | 0.90–1.00 | **2.88–3.20s** | DOWN toward his foot |

No post-Evaluate AimArmAlong. No DIAG_REV / DIAG_MIRR / MILD / Mixamo Mirror.

Sword: sheath/draw at **LEFT hip**; strike and walk-after-draw on `mixamorig:RightHand`. Orbit 1/2/3 unchanged.

## Animation window (optional inspect)

1. Open `Assets/Survival/Scenes/SirAldric.unity`.
2. **Survival → Sir Aldric → Spawn knight for Animation-window edit**.
3. **Window → Animation → Animation**. Select root **`SirAldricAttackEditKnight`** → **Lock** Animation → **Preview** on → scrub `mixamorig:RightArm` / `mixamorig:RightHand`.

Do not rename the clip to `Attack`.
