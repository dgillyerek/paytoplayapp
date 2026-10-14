import bpy,numpy as np
from mathutils import Vector
sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; arm.animation_data.action=bpy.data.actions['BONEQUILL_attack']; sc.frame_set(10)
bm=lambda n: arm.matrix_world@arm.pose.bones['mixamorig:'+n].matrix
body=[o for o in bpy.data.objects if o.type=='MESH' and o.vertex_groups.get('mixamorig:RightHand')][0]
gi=body.vertex_groups['mixamorig:RightHand'].index; HM=arm.data.bones['mixamorig:RightHand'].matrix_local.inverted()
fl=np.array([(HM@v.co)[:] for v in body.data.vertices if any(g.group==gi and g.weight>0.9 for g in v.groups)]); y=fl[:,1].max()
NL=Vector(fl[(fl[:,1]>0.45*y)&(fl[:,1]<0.8*y)].mean(0)); nk=bm('RightHand')@NL
L=bm('LeftHand'); R=L.to_3x3().normalized(); rest=L.to_translation()+R@Vector((0,0.06,0))+Vector((0,0,0.035))+(R@Vector((1,0,0))).normalized()*0.012
d=(rest-nk).normalized(); print('AIM',tuple(round(v,3) for v in d),'nock',tuple(round(v,3) for v in nk),'rest',tuple(round(v,3) for v in rest))
