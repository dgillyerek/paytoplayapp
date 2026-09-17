# Sir Aldric — Unity anim handoff (LOCKED rear master)

**Derek ask (2026-09-17):** One rear view SoT; Dev animates walk + attack in Unity. Generative multi-frame packs are STOPPED (look drift).

## Locked asset (use this only)
- `SIR_ALDRIC_REAR_MASTER_LOCKED.png` — 1024² transparent, high-angle rear, short cape (legs readable)
- `SIR_ALDRIC_REAR_MASTER_LOCKED_512.png` — 512² same

Do **not** regenerate new AI frames for the cycle. Motion must come from this single sprite (rig / mesh deform / engineered flipbook from this art / procedural).

## Camera / orientation (hard)
- High-angle third-person rear
- Character faces / walks / attacks **away from camera** toward **TOP of screen** (enemy = top)
- Never strike toward camera / bottom of screen

## Look locks (must not change frame-to-frame)
- Silver plate + gold trim helm/pauldrons/gauntlets/greaves/boots
- Short royal-blue waist cape: gold lion rampant + gold Greek-key hem
- Sword **sheathed** on viewer-RIGHT hip while walking (brown scabbard + gold fittings)
- Attack: draw → strike toward TOP → recover/re-sheath (no snap)
- No rocks / ground props

## Deliverable Derek wants to see
Phone / Game-view clip (or Editor capture) of:
1. Readable walk loop from this master
2. Interval: walk → attack (up-screen) → recover → walk
Same look every frame (pixel-consistent with this master).

## Paths on Design box
`/workspace/design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_DROP_LOCKED/`

## Contact
Derek is waiting to see Unity motion. Ping Design when a clip is ready; Design will not generate more multi-frame AI packs unless Derek reopens.
