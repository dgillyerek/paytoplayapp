# Three attacks per humanoid: Mixamo download list

Scope (Derek, 2026-10-17): humanoids only, premade Mixamo attacks, the same way Blightroot's
were done. Creatures (Emberfang, Stormcrest, Nightfang, Ashwyrm) are on hold and keep their one attack.
**Status 2026-10-18:**
- **Blightroot:** no downloads; wired.
- **Ironhowl:** downloaded and wired: `IRONHOWL_mixamo_mutant_jump_attack.fbx` and `IRONHOWL_mixamo_mutant_roaring.fbx`. Mixamo offered no In Place, so the battle holds the leap on the spot.
- **Sir Aldric:** attack 2 is covered by a repo clip; attack 3 (shield bash) needs one download (see his section).
- **Lyra, Rowan, Oakenshield, Vespera, Bonequill:** upload FBX files are ready.

Blightroot needs no downloads: attacks 2 and 3 are already wired from the creature-pack clips in
the repo (`mutant punch`, `mutant jump attack`). See PR A.

Attack 1 for every character stays the locked, approved attack. Approved rest, walk and attack
files are not touched.

## Download settings (every clip)

| Mixamo setting | Value |
|---|---|
| Format | **FBX for Unity (.fbx)** |
| Skin | **Without Skin** |
| Frames per Second | **30** |
| Keyframe Reduction | **none** |
| In Place | **ticked** wherever Mixamo offers it (any attack with a step or lunge). The battle adds reach itself, as it does for Blightroot. |
| Overdrive | default (50). For Vespera the battle slows the clip; see her section. |
| Character Arm-Space | default. Do **not** widen it for the archers. |
| Mirror | Only if the preview leads with the **right** foot and the character needs a left-foot lead (see the rules below). Note in the filename when Mirror is on. |
| Trim | full clip. The cut to the moving frames happens at import, like `Sword And Shield attack (4)` frames 0–32. |

**Which character to upload to Mixamo**
- **Ironhowl, Sir Aldric:** already native Mixamo/AccuRIG skeletons. Upload the existing body (`IRONHOWL_rig.fbx`, `SirAldric_PILOT_accurig_humanoid.fbx`) and download onto it directly.
- **Lyra, Rowan, Oakenshield, Vespera, Bonequill:** upload the approved rest skin. Copies are ready in `/workspace/three_attacks/uploads/<CHARACTER>.fbx`: LYRA, ROWAN, OAKENSHIELD, VESPERA, BONEQUILL, each copied byte for byte from its ThemePack `*_blenderig.fbx`. These are Theme A Blender rigs with a reduced `mixamorig:` skeleton, so Mixamo will auto-rig them (Standard Skeleton, 65 bones; leave symmetry on).
  - The download comes out on the full Mixamo skeleton. Retarget it in Blender by bone name onto the approved Blender rig.
  - Bake onto the **body bones only**: Hips, Spine, Spine1, Spine2, Neck, Head, Shoulder/Arm/ForeArm/Hand, UpLeg/Leg/Foot/ToeBase.
  - Fingers come from each character's existing grip pose, not from Mixamo.
  - The same pipeline as the current `*_blenderig_attack` bakes then applies: frames, props, VFX tracks and checksums.

**Naming:** save each file as `<CHARACTER>_mixamo_<mixamo_title_snake_case>.fbx` (for example `ROWAN_mixamo_standing_draw_arrow.fbx`). Put it in `design/survival-theme-a-fantasy/<heroes|enemies>/anim/<name>/mixamo_attacks/` and record its md5 in that folder's `CHECKSUMS.md5`.

## Locked rules each clip must respect (check in the Mixamo preview before downloading)

- **Archers (Rowan, Bonequill):**
  - Keep a natural arm and hand. Only the bow rotates in the grip, with the string on the inside of the forearm.
  - The bow follows the existing `BlenderBoneGrip` and roll; it is never re-posed by the Mixamo hand.
  - Reject clips that twist the bow forearm inward.
- **Stance:** attacks step forward with the **left foot** into a side-on body, including the lower body. Hips and legs turn with the torso; Rowan and Oakenshield already do this, so match it. If a clip leads with the right foot, use Mirror only when the mirrored clip keeps the weapon in the correct hand. Otherwise pick the alternative listed.
- **Cloth:** stays on the spring chains on spine and shoulders (`ClothSpring`). Nothing from Mixamo drives coat, cape or skirt bones, and no Unity Cloth component.
- **Palettes:** hero VFX blue, villain VFX purple.
- **Character notes:** Lyra holds the staff **two-handed**. Bonequill **draws from the quiver** before each shot. Vespera's conjure plays **1.5x slower**.

## Per character (exact Mixamo titles; the first one listed is the pick, the rest are fallbacks)

### Sir Aldric (hero, sword and shield; plays Lite-pack clips on `SirAldricMeshyAnimateActor`)
| # | Attack | Mixamo title | Notes |
|---|---|---|---|
| 1 | sword and shield thrust | *(locked: "Sword And Shield attack (4)")* | unchanged |
| 2 | diagonal slash | **Stable Sword Inward Slash**. **Covered by the repo; no download needed.** | Already in the repo as `SirAldric_body_holefixed_slash_Stable_Sword_Inward_Slash_DIAG.fbx` (pilot, holefixed body). Download fresh onto the AccuRIG body only if the pilot take doesn't retarget cleanly. Fallback: **Sword And Shield Slash**. Impact: SwordSteel (blue arc of sparks). |
| 3 | shield bash | **NEEDS A DOWNLOAD.** I measured the repo Lite clips: `Sword And Shield attack`, (2) and (3) are sword-led lunges (left-hand reach 0.35–0.54 vs right-hand 0.58–0.86 hips-to-head lengths) that carry the hips 3–5 body lengths forward, so none is a shield bash. Search Mixamo for **Sword And Shield Attack** (the shield-punch variant of the Pro Sword And Shield pack; confirm in preview, Mixamo lists it as "Sword And Shield Bash" in some library builds) | `sword_and_shield_attack.fbx` (2) and (3) are already in the Lite pack in the repo; check them first. In Place on. Impact: new ShieldShock (blue ring), with a short extra recoil. |

### Lyra (hero mage, staff held **two-handed**)
| # | Attack | Mixamo title | Notes |
|---|---|---|---|
| 1 | staff cast arcane bolt | *(locked)* | unchanged |
| 2 | staff sweep (melee) | **Standing Melee Attack Horizontal**. **DOWNLOADED + WIRED 2026-10-18** (`LYRA_blenderig_attack2_staff_sweep.fbx`, hero-blue HeroStaffSweep). | Two-handed horizontal swing. Fallback: **Standing Melee Attack 360 Low**. In Place on. The staff stays on RightHand with the left hand on the shaft (existing two-hand grip). Impact: StaffLight arc. |
| 3 | arcane nova (area) | **Standing 2H Magic Area Attack 01**. **DOWNLOADED + WIRED 2026-10-18** (`LYRA_blenderig_attack3_arcane_nova.fbx`, hero-blue HeroArcaneNova). | Two hands raise, then drive down. Fallback: **Standing 2H Magic Area Attack 02**. Impact: blue ground ring at the target (ArcaneConjure variant). |

### Rowan (hero archer: natural arm, only the bow rotates, left-foot step, side-on including the hips)
| # | Attack | Mixamo title | Notes |
|---|---|---|---|
| 1 | bow shot | *(locked)* | unchanged |
| 2 | double shot | **Standing Draw Arrow** then **Standing Aim Recoil** (Pro Longbow pack). **DOWNLOADED + WIRED 2026-10-18** (`ROWAN_blenderig_attack2_double_shot.fbx`, looses f44/f80). | The battle plays the draw, release, draw, release. Bake only the body bones; the bow keeps its v5g grip and roll (8° rest, eases to 50°). Two RangerArrow hits 0.25 s apart. |
| 3 | volley (area) | **Standing Aim Overdraw** (Pro Longbow pack), released with **Standing Aim Recoil**. **DOWNLOADED + WIRED 2026-10-18** (`ROWAN_blenderig_attack3_volley.fbx`, spine +35°, looses f84). | Overdraw with the release angled up about 35° (added in the bake on Spine and Spine1 only, not the bow arm). Three arrows land in a spread. Fallback: **Standing Aim Recoil** with the same spine lift. |

### Oakenshield (hero, thorn spear: left-foot step, side-on including the hips)
| # | Attack | Mixamo title | Notes |
|---|---|---|---|
| 1 | thorn spear throw | *(locked)* | unchanged |
| 2 | spear thrust (melee) | ~~Stabbing~~ REJECTED 2026-10-18 (holds her spear upright at her grip; see oakenshield/mixamo_attacks/NOTE.md). Wanted: search **"Advancing And Stabbing With Rifle Bayonet Or Spear"** | One-handed forward stab. In Place on. Fallback: **Sword And Shield Attack** (thrust variant). Spear stays in hand. Impact: LeafEarth splinters. |
| 3 | root snare (area) | **Standing 2H Magic Area Attack 02** (WIRED 2026-10-18, contact f52) | Ground-strike gesture. Fallback: **Mutant Jump Attack** with In Place on (a heavier stamp). The left foot plants on the strike. Impact: blue-tinted thorn roots under the target. |

### Vespera (villain sorceress, orb; conjure plays **1.5x slower**)
| # | Attack | Mixamo title | Notes |
|---|---|---|---|
| 1 | orb conjure shadow bolt | *(locked, already 1.5x slowed)* | unchanged |
| 2 | shadow lash (melee whip from the hand) | **Standing 1H Magic Attack 01** (WIRED 2026-10-18, contact f50) | One-hand lash. Fallback: **Standing 1H Magic Attack 03**. Download at default Overdrive; the bake time-stretches the conjure/wind-up part 1.5x, as for attack 1. Impact: ShadowRake (purple). |
| 3 | hex rain (area) | **Standing 2H Magic Area Attack 02** (WIRED 2026-10-18, hits f84/88/92) | Arms raise the orb, shards fall. Fallback: **Standing 2H Magic Attack 05**. Wind-up stretched 1.5x. Impact: purple shards on the target. |

### Bonequill (villain archer: **quiver draw**, natural arm, only the bow rotates)
| # | Attack | Mixamo title | Notes |
|---|---|---|---|
| 1 | bone arrow shot | *(locked)* | unchanged |
| 2 | rapid quiver shot | **Standing Draw Arrow** (Pro Longbow pack). **DOWNLOADED + WIRED 2026-10-18** (`BONEQUILL_blenderig_attack2_rapid_quiver_shot.fbx`, 1.3x, released with Rowan's Aim Recoil). | The right hand reaches back to the quiver; keep it. Fast release. Bow grip and roll as the approved attack. Impact: BoneShard. |
| 3 | bone volley (area) | **Standing Aim Overdraw** (Pro Longbow pack). **DOWNLOADED + WIRED 2026-10-18** (`BONEQUILL_blenderig_attack3_bone_volley.fbx`, spine +35°). | Quiver draw, overdraw, spread of 3. Upward angle via Spine and Spine1 only. Impact: BoneShard x3. |

### Ironhowl (villain werewolf; native Mixamo skeleton, generic import like Blightroot)
| # | Attack | Mixamo title | Notes |
|---|---|---|---|
| 1 | claw swipe | *(locked)* | unchanged |
| 2 | overhead smash | **Standing Melee Attack Downward**. **DOWNLOADED + WIRED 2026-10-18.** | Derek 2026-10-18: no mutant moves on Ironhowl (they are Blightroot's), so Mutant Jump Attack / Mutant Roaring were dropped (kept only as rejected design copies). Hips travel held in place, 0.3 m reach slide. Impact: new GroundSlam (purple crack ring), 0.09 m shake. |
| 3 | claw combo | **Standing Melee Combo Attack Ver. 2**. **DOWNLOADED + WIRED 2026-10-18.** | Four hits (R 29, L 46, L 59, R 82), purple ShadowRake claw sparks on each. Hips travel held in place, 0.3 m reach slide. |

## Totals
- Downloads: Ironhowl 2 (done, replaced 2026-10-18), Lyra 2 (done), Oakenshield 2, Vespera 2, Bonequill 2, Rowan 2–3 (Draw Arrow, Aim Recoil, Aim Overdraw), Sir Aldric 1 (shield bash; the diagonal slash is covered by the repo). About 12–15 FBX files.
- Each download is about 1–3 MB without skin.
