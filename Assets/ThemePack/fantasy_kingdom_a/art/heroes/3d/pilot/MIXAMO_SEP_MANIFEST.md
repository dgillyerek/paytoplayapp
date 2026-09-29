# Sir Aldric Mixamo separate-portrait package (2026-09-28)

Supersedes AccuRIG clips-only on #27 for this tip.

## Files
- body/SirAldric_body_holefixed_mid280k.fbx — Mixamo-ready holefixed look mesh (no cape, empty RH; **no armature** — Play instance is the Mixamo-skinned walk FBX, same 139k verts)
- textures/ — Meshy PBR maps for body bind
- walk/SirAldric_body_holefixed_walk.fbx — Mixamo Standard Walk (With Skin, 30fps, ~34 frames)
- slash/SirAldric_body_holefixed_slash.fbx — Mixamo Stable Sword Inward Slash (With Skin, 30fps, ~67 frames)
- props/SirAldric_PILOT_sword.fbx — parent to RH
- props/SirAldric_PILOT_cape.fbx — optional soft
- stills/ — Design lean stills (walk stride + slash peak)

## Dev rules
- Clips on Mixamo body. NEVER ship AccuRIG as playable body for this tip.
- Generic (not Humanoid): Humanoid Playable + Armature 0.01 × FileScale 0.01 = ~2 cm body / sword-only FAIL on bf5a5fa.
- Discard embedded mesh from walk/slash FBXs if Unity duplicates — do not use clip FBX mesh as body.
- Soft: Mixamo skeleton has no fingers. Soft fused leftovers OK elsewhere; HOLD Path A / ClipSword / AimChain / Dev weight-paint.
- Derek Unity lacks ModelImporter.addHumanoidExtraRoot and HumanDescription.legTwist — do not set them.
