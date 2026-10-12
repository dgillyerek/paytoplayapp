import bpy,sys,json,math,numpy as np
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]
arm=bpy.data.objects['LYRA_tighten_rig']; sc=bpy.context.scene; ad=arm.animation_data
for t in ad.nla_tracks: t.mute=True
ad.action=bpy.data.actions['LYRA_tighten_attack']; P='mixamorig:'
def hz(v): v=Vector((v.x,v.y,0)); return v.normalized() if v.length>1e-6 else v
def sang(a,b): return math.degrees(math.atan2(a.x*b.y-a.y*b.x, a.dot(b)))
res=[]
for f in range(31):
    sc.frame_set(f); r={'f':f}
    for s in ('Left','Right'):
        ul=arm.pose.bones[P+s+'UpLeg']; lg=arm.pose.bones[P+s+'Leg']; ft=arm.pose.bones[P+s+'Foot']
        H=ul.head; K=lg.head; A=ft.head; T=ft.tail
        fd=hz(T-A)
        ax=(A-H).normalized(); kp=(K-H)-(K-H).dot(ax)*ax
        kd_plane=hz(kp)
        kz=hz(arm.matrix_world.to_3x3()@lg.matrix.to_3x3()@Vector((0,0,1)))  # shin bone local Z
        r[s]=dict(foot_yaw=round(math.degrees(math.atan2(fd.x,-fd.y)),1),knee_vs_foot_plane=round(sang(fd,kd_plane),1),kneebow_cm=round(kp.length*100,2),shinZ_vs_foot=round(sang(fd,kz),1),
                  thighZ_vs_foot=round(sang(fd,hz(ul.matrix.to_3x3()@Vector((0,0,1)))),1),toe=[round(x,4) for x in T],heel=[round(x,4) for x in A])
    hips=arm.pose.bones[P+'Hips'].matrix.to_3x3()@Vector((0,0,1)); ch=arm.pose.bones[P+'Spine2'].matrix.to_3x3()@Vector((0,0,1))
    r['hips_yaw']=round(math.degrees(math.atan2(hz(hips).x,-hz(hips).y)),1); r['chest_yaw']=round(math.degrees(math.atan2(hz(ch).x,-hz(ch).y)),1)
    res.append(r)
json.dump(res,open(OUT,'w'))
for r in res: print('LEG',r['f'],'hips',r['hips_yaw'],'chest',r['chest_yaw'],{s:(r[s]['foot_yaw'],r[s]['knee_vs_foot_plane'],r[s]['kneebow_cm'],r[s]['shinZ_vs_foot'],r[s]['thighZ_vs_foot']) for s in ('Left','Right')})
