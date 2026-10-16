import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,math
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/w2/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; PB=arm.pose.bones
arm.animation_data.action=bpy.data.actions['ROWAN_attack']
prev={}
for f in range(31):
    sc.frame_set(f); out=[]
    for s in ('Left','Right'):
        for b in ('Arm','ForeArm','Hand'):
            q=PB['mixamorig:'+s+b].matrix.to_quaternion()
            if (s,b) in prev: out.append(f"{s[0]}{b[:2]}={math.degrees(prev[(s,b)].rotation_difference(q).angle):5.1f}")
            prev[(s,b)]=q
        h=PB['mixamorig:'+s+'Hand'].head; out.append(f"{s[0]}H=({h.x:.2f},{h.y:.2f},{h.z:.2f})")
    print(f,' '.join(out))
