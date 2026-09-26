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
