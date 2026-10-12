import bpy, json
arm=bpy.data.objects['OAKENSHIELD_rig']; sc=bpy.context.scene
bpy.context.view_layer.objects.active=arm
if arm.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
arm.animation_data.action=bpy.data.actions['OAKENSHIELD_walk']
r=[]
for f in range(30):
    sc.frame_set(f); P=lambda n: arm.matrix_world@arm.pose.bones['mixamorig:'+n].head
    r.append([P('LeftFoot').x-P('RightFoot').x, P('LeftLeg').x-P('RightLeg').x, P('LeftUpLeg').x-P('RightUpLeg').x, P('LeftFoot').x-P('Hips').x, P('RightFoot').x-P('Hips').x])
import numpy as np; a=np.array(r)
print('FEET ankle_sep min/mean/max',a[:,0].min().round(3),a[:,0].mean().round(3),a[:,0].max().round(3),'knee_sep',a[:,1].min().round(3),a[:,1].mean().round(3),'hip_sep',a[:,2].mean().round(3),'L-of-hips min',a[:,3].min().round(3),'R-of-hips max',a[:,4].max().round(3))
