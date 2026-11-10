import sys
from mathutils import Vector
ROOT="C:/Development/paytoplayapp"
FIXDIR=ROOT+"/design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix10"
sys.path.insert(0,ROOT+"/design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix9")
import k9_present as p
p.setup_render(); p.style_character()
front=p.make_cam("F",Vector((0,-6.0,1.40)),Vector((0,0,1.40)),3.05)
side=p.make_cam("S",Vector((-6.0,-.25,1.40)),Vector((0,-.25,1.40)),3.05)
p.render_frames("attack_dualslash",[1,8,11,16,22,28,31,40,58],front,FIXDIR+"/renders/dual_front")
p.render_frames("attack_dualslash",[1,8,11,16,22,28,31],side,FIXDIR+"/renders/dual_side")
p.render_frames("attack2_crossthrust",[1,8,12,16,20,28,48],front,FIXDIR+"/renders/cross_front")
p.render_frames("attack2_crossthrust",[1,8,12,16,20,28],side,FIXDIR+"/renders/cross_side")
print("KEYS_DONE")
