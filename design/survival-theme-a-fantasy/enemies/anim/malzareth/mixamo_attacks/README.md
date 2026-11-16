# Malzareth standing melee

Mixamo Standing Melee Attack Horizontal, from the Lyra download, retargeted onto the existing 20-bone rig. The Mixamo right hand drives `RightForeArm.001`, which already holds the greatsword. No bones were added.

73 frames, 30 fps. Open `MALZARETH_standing_melee.blend` and press Space.

Unity preview: `Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/malzareth/MALZARETH_attack_standing_melee.fbx`, button STANDING_MELEE. Dying stays a separate one-shot.

# Great Sword Casting

Keyed by `raise_great_sword_casting.py`, not retargeted any more. Starting from his idle stance, he winds the sword up with both hands, straight through the centre line with no sideways turn. The lift gets moving quickly, then slows into the top, and the sword keeps creeping back to 74 degrees past vertical, nearly flat behind his head. The blade stays about 9 cm clear of the crown and horns. Then he chops it forward and down, three times faster than the lift and accelerating into the hit, until the tip is about 6 cm off the ground. On the chop he bends forward and drops his hips 22 cm into bent knees, with his feet planted, and his weight keeps sinking for a moment after the impact. Then he recovers to the guard so the preview loop doesn't pop.

The left hand is solved onto the handle just below the right on every frame. Both wrists are locked to the grip pose of the first frame: the roll is held exactly and the bend stays within about 15 degrees. The swing comes from the elbows and shoulders, and the fists may move off the planned path to make that work.

Wind-up 15 frames, creep 5, chop 5, sink 7, recover 18. 51 frames, 30 fps. Open `MALZARETH_great_sword_casting.blend` and press Space. The old Mixamo retarget (`retarget_great_sword_casting.py`, 145 frames) is kept for reference.

Unity preview button GREAT_SWORD_CASTING. The old overhead cleave file is still on disk and is no longer in the button list.
