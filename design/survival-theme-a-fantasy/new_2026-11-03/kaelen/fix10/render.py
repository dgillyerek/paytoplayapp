import os,sys
import bpy
from mathutils import Vector
ROOT="C:/Development/paytoplayapp"
FIXDIR=ROOT+"/design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix10"
sys.path.insert(0,ROOT+"/design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix9")
import k9_present as p
p.setup_render(); p.style_character()
front=p.make_cam("FIX10_FRONT",Vector((0,-6.0,1.40)),Vector((0,0,1.40)),3.05)
side=p.make_cam("FIX10_SIDE",Vector((-6.0,-.25,1.40)),Vector((0,-.25,1.40)),3.05)
for name in ("attack_dualslash","attack2_crossthrust"):
    action=bpy.data.actions[name]; start=int(action.frame_range[0]); end=int(action.frame_range[1])
    p.render_frames(name,list(range(start,end+1)),front,FIXDIR+"/renders/"+name+"_front")
    p.render_frames(name,list(range(start,end+1)),side,FIXDIR+"/renders/"+name+"_side")
print("RENDER_DONE",FIXDIR+"/renders")
