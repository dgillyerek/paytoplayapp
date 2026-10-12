"""Approximate Unity Humanoid root extraction with bake-into-pose off and applyRootMotion off:
root yaw = body (hips) yaw, root XZ = body (hips) ground position, root Y = original; these deltas from f0 are
stripped. Report what is lost and how far the planted left toe would slide."""
import bpy, math, json
from mathutils import Matrix, Vector
P='mixamorig:'; sc=bpy.context.scene; arm=bpy.data.objects['LYRA_tighten_rig']; ad=arm.animation_data
for t in ad.nla_tracks: t.mute=True
pb=arm.pose.bones
def state(f):
    sc.frame_set(f); H=pb[P+'Hips'].matrix
    fw=H.to_3x3()@Vector((0,0,1)); # hips bone local Z ~ forward? measure from hip line instead
    L=pb[P+'LeftUpLeg'].head; R=pb[P+'RightUpLeg'].head; d=L-R; yaw=math.degrees(math.atan2(d.y,d.x))
    ft=pb[P+'LeftFoot']; toe=ft.matrix@Vector((0,ft.length,0)); rft=pb[P+'RightFoot']; rtoe=rft.matrix@Vector((0,rft.length,0))
    return yaw, H.translation.copy(), toe, rtoe
out=[]
for an in ('LYRA_tighten_attack','LYRA_tighten_walk'):
    ad.action=bpy.data.actions[an]; y0,h0,_,_=state(0); rows=[]
    for f in range(0,31,2):
        y,h,toe,rtoe=state(f); dy=y-y0; dh=h-h0
        # strip: undo yaw about hips vertical and the XZ (Blender XY) offset
        S=Matrix.Translation(Vector((h0.x,h0.y,0)))@Matrix.Rotation(math.radians(-dy),4,'Z')@Matrix.Translation(-Vector((h.x,h.y,0)))
        t2=S@toe; r2=S@rtoe
        rows.append(dict(f=f,body_yaw=round(dy,1),hips_xy_cm=round(Vector((dh.x,dh.y)).length*100,1),ltoe_blender=[round(v,3) for v in toe],ltoe_unity_humanoid=[round(v,3) for v in t2],
             ltoe_shift_cm=round((t2-toe).length*100,1),rtoe_shift_cm=round((r2-rtoe).length*100,1)))
    out.append((an,rows))
for an,rows in out:
    print('SIM',an)
    for r in rows: print('SIM',r['f'],'yaw',r['body_yaw'],'hipsXY_cm',r['hips_xy_cm'],'Ltoe_shift_cm',r['ltoe_shift_cm'],'Rtoe_shift_cm',r['rtoe_shift_cm'])
json.dump(out,open('rootsim.json','w'),indent=0)
