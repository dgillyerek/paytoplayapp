#!/bin/bash
# iter.sh name contact frames
cd /workspace/ironhowl/work
T=/workspace/ptp-ironhowl/Assets/ThemePack/fantasy_kingdom_a/art/enemies/3d/ironhowl/IRONHOWL_mixamo_standing_melee_attack_downward.fbx
/workspace/attack_20261007/b42.sh -b --python author.py -- /workspace/ironhowl/work/out $1 2>&1 | grep -E "POS|Error|Traceback|line [0-9]"
python3 retarget.py $T out/${1}_blender.fbx out/IRONHOWL_${1}.fbx
rm -f pv/IRONHOWL_${1}_*
UP=1 /workspace/attack_20261007/b42.sh -b --python render_new.py -- /workspace/ironhowl/work/pv out/IRONHOWL_${1}.fbx:$3 2>&1 | grep -E "Error|Traceback"
python3 sheet.py pv IRONHOWL_$1 sh_$1.png $2
