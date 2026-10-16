import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,math,json
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/w2/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; PB=arm.pose.bones
def tw(q):
    q=q.normalized(); a=math.degrees(2*math.atan2(q.y,q.w)); return ((a+180)%360)-180
res={}
for clip in ('rest','walk','attack'):
    arm.animation_data.action=bpy.data.actions['ROWAN_'+clip]; rows=[]
    for f in range(31):
        sc.frame_set(f); rows.append([round(tw(PB['mixamorig:'+s+'Arm'].matrix_basis.to_quaternion()),1) for s in ('Left','Right')])
    res[clip]=rows; print(clip,rows)
json.dump(res,open('/workspace/rowan_fresh/w2/upperarm_twist.json','w'))
