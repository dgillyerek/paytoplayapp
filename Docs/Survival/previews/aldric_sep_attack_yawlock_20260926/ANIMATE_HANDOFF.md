# Meshy Animate handoff — Sir Aldric SEP

- Character: `SirAldric_SEP_body_nosword` (existing Meshy asset; not re-uploaded).
- Walk clip: `Walking` preset, downloaded FBX with skin, 30 FPS.
  - `assets/SirAldric_SEP_meshy_animate_walk.fbx`
  - Stills: `assets/stills/walk_front.png`, `walk_34.png`, `walk_rear.png`
- RH-clear observation: **Yes for walk** — front and 3/4 stills show the character's right hand empty; no hip sword mesh is visible. Rear still also shows no sword/weapon at the right hip.
- Attack intent attempted: Text to Motion prompt/name `Sir Aldric Draw Slash Forward` — draw sword from right hip, then committed forward slash toward enemy directly ahead, finish combat stance; no walking/turning.
- Attack status: **PASS**. Reused the existing authored Meshy motion `Sir Aldric Draw Slash Forward` on the library character; no CLEAN re-upload, Path A, Mixamo, Unity, or melt path used. Meshy export used FBX + `MeshyRig` skeleton template, Current animation, With Skin, 30 FPS.
- Attack FBX: `assets/SirAldric_SEP_meshy_animate_attack.fbx` (contains animation stacks; primary clip range 3–92 at 30 FPS).
- Attack stills: `assets/stills/attack_draw.png`, `assets/stills/attack_strike.png`; angle set also available at `assets/stills/front.png`, `assets/stills/34.png`, and `assets/stills/rear.png`.
- Prior Add to Character `estimate-pose` HTTP 500 screenshot retained at `assets/animate_attack_blocked.png`; export succeeded from the authored motion without retrying a second alternate motion.

## NoSpin re-author attempt (2026-09-26)
- Meshy Animate motion name: `Sir Aldric Draw Slash Forward NoSpin` on the existing `SirAldric_SEP_body_nosword` AccuRIG avatar (no remesh/re-rig/re-upload).
- Prompt emphasized: face straight forward away from camera for the entire clip; torso/hips/feet aligned to forward axis; draw with RH; one clean slash straight ahead; no turn-around, 180-degree rotation, body spin, face-camera, or walking.
- Export settings: FBX, `MeshyRig`, Rigged Character ON, Current animation, With Skin, 30 FPS.
- New export: `assets/SirAldric_SEP_meshy_animate_attack_nospin.fbx`
- New stills: `stills/attack_nospin_draw.png`, `stills/attack_nospin_strike.png`, `stills/attack_nospin_front.png`, `stills/attack_nospin_34.png`, `stills/attack_nospin_rear.png`.
- Verification: **FAIL / not rear-play safe**. Meshy generated a large hips/root rotation (export action `target_character|rigify_clip|BaseLayer`, frames 3–92; Hips quaternion changes approximately 174 degrees), and the rear still does not reliably read as a strike away from camera. Do not use this clip for Unity rear Play without correction.
- The prior `assets/SirAldric_SEP_meshy_animate_attack.fbx` remains untouched and is still the FAIL reference. Walk FBX remains untouched.

## Yawlock post-process (2026-09-26)

Meshy NoSpin re-author still spun ~180° on Hips. Fixed in Blender headless (no walk/attack FAIL refs overwritten).

- **Input:** `assets/SirAldric_SEP_meshy_animate_attack_nospin.fbx` (FAIL reference kept)
- **Output FBX:** `assets/SirAldric_SEP_meshy_animate_attack_yawlock.fbx`
- **Output GLB:** `assets/SirAldric_SEP_meshy_animate_attack_yawlock.glb`
- **Method:** Measured Hips world rotation (Euler XYZ). World **Z** carried the spin (~179.9° max |Δ|). For every frame 3–92: take Hips world matrix → Euler XYZ → set **Z = start-frame yaw (1.859°)** → keep X/Y pitch/roll and location/scale → write `pose.bones["Hips"].rotation_quaternion` (LINEAR). Arm/spine/hand fcurves untouched. Armature object had no rotation keys. Start forward (Hips local Z) ≈ world −Y.
- **Before → after:** max |Hips yaw Δ| **179.93° → 0.00°** (re-import verify of exported FBX also **0.00°**; threshold &lt;15°). Max ground-forward deviation from start ≈ 0.53°. RightHand relative travel retained ≈ 1.02 m (slash not authored-as-turn-only).
- **Stills:** `stills/attack_yawlock_draw.png` (f8), `stills/attack_yawlock_strike.png` (f25), `stills/attack_yawlock_front.png`, `stills/attack_yawlock_34.png`, `stills/attack_yawlock_rear.png` (cam at +Y behind back; character faces −Y / top-of-frame; strike reads away-from-camera).
- **Log:** `logs/ATTACK_YAWLOCK_VERIFY.txt`, `logs/ATTACK_YAWLOCK_SUMMARY.json`
- **Result:** **PASS** — yawlocked clip is the preferred attack export for Unity rear Play.
