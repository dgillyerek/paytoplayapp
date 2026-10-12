#!/bin/bash
set -e; cd /workspace/vespera-reweight/work; BL=/workspace/tools/blender-5.2.2-linux-x64/blender
python3 b_solve.py >/dev/null
$BL -b welded.blend --python c_apply.py -- $PWD/rw.blend >/dev/null 2>&1
$BL -b rw.blend --python anim_rw.py -- $PWD/anim.blend $PWD/anim.json 2>&1 | grep -E "Error|Trace" || true
$BL -b anim.blend --python where.py -- VESPERA_nocape_walk,VESPERA_nocape_attack 2>&1 | grep WHERE | cut -c1-900
