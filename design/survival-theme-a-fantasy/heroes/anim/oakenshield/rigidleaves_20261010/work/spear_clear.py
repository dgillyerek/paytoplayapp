import bpy, sys, json
from mathutils import Vector, Matrix
arm=bpy.data.objects['OAKENSHIELD_rig']; sc=bpy.context.scene
arm.animation_data.action=bpy.data.actions['OAKENSHIELD_attack']
def spear_rot(tip):
    tip=tip.normalized(); y=-tip; x=Vector((1,0,0)); x=(x-x.dot(y)*y).normalized(); z=x.cross(y); return Matrix((x,y,z)).transposed()
sc.frame_set(10); pb=arm.pose.bones['mixamorig:RightHand']; M=arm.matrix_world@pb.matrix; R=M.to_3x3().normalized()
Roff=R.inverted()@spear_rot(Vector((0,-1,0.12))); poff=Vector((0,0.07,0))
out=[]
for f in range(3,14):
    sc.frame_set(f); M=arm.matrix_world@pb.matrix; R=M.to_3x3().normalized(); c=M.to_translation()+R@poff; Rw=R@Roff
    a=c+Rw@Vector((0,-0.775,0)); b=c+Rw@Vector((0,0.626,0))
    hb=arm.pose.bones['mixamorig:Head']; hc=arm.matrix_world@(hb.head+(hb.tail-hb.head)*0.5)
    best=9
    for k in range(41):
        p=a.lerp(b,k/40); best=min(best,(p-hc).length)
    out.append((f,round(best,3),[round(v,2) for v in c]))
print('CLEAR',out)
