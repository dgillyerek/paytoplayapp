#!/bin/bash
# Freed right hand chain (paths are the build box's). Input: f41ef77 VESPERA_nocape.blend as f41.blend.
set -e; cd /workspace/vespera-hand/work; BL=/workspace/tools/blender-5.2.2-linux-x64/blender
python3 cut_label.py                                             # min-cut: right forearm/hand vs skirt/belt/hip/flank
$BL -b f41.blend --python cut_fill.py -- $PWD/cut.blend           # delete joining faces, fill holes, dump mesh
python3 b_solve.py                                               # weights (hand piece = right arm only; body near it = no right arm)
$BL -b cut.blend --python c_apply.py -- $PWD/rw.blend
$BL -b rw.blend --python anim_rw.py -- $PWD/anim.blend $PWD/anim.json
$BL -b anim.blend --python export_derek.py -- $PWD/final
$BL -b final/VESPERA_nocape.blend --python stretch_final.py -- $PWD/stretch.json VESPERA_nocape_rest,VESPERA_nocape_walk,VESPERA_nocape_attack
$BL -b final/VESPERA_nocape.blend --python clear.py -- VESPERA_nocape_attack clear_attack.json
$BL -b final/VESPERA_nocape.blend --python clear.py -- VESPERA_nocape_walk clear_walk.json
$BL -b final/VESPERA_nocape.blend --python qc_hand.py -- hand_qc.json
