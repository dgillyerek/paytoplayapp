import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,numpy as np,json
from mathutils import Vector
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/work/anim.blend')
arm=bpy.data.objects['ROWAN_rig']; body=bpy.data.objects['ROWAN_body']; me=body.data; P_='mixamorig:'
N=len(me.vertices); P=np.empty(N*3); me.vertices.foreach_get('co',P); P=P.reshape(-1,3)
names=[g.name for g in body.vertex_groups]; bi={n:i for i,n in enumerate(names)}
W=np.zeros((N,len(names)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
hips=W[:,bi[P_+'Hips']]; handL=W[:,bi[P_+'LeftHand']]; handR=W[:,bi[P_+'RightHand']]
faL=W[:,bi[P_+'LeftForeArm']]; faR=W[:,bi[P_+'RightForeArm']]
# hand region: near Hand joint
JL=np.array(arm.data.bones[P_+'LeftHand'].head_local); JR=np.array(arm.data.bones[P_+'RightHand'].head_local)
dL=np.linalg.norm(P-JL,axis=1); dR=np.linalg.norm(P-JR,axis=1)
nearL=dL<0.12; nearR=dR<0.12
print('L hand region verts',nearL.sum(),'hips weight>0.1',((hips>0.1)&nearL).sum(),'max hips',hips[nearL].max().round(3),'hand mean',handL[nearL].mean().round(3),'fa mean',faL[nearL].mean().round(3))
print('R hand region verts',nearR.sum(),'hips weight>0.1',((hips>0.1)&nearR).sum(),'max hips',hips[nearR].max().round(3),'hand mean',handR[nearR].mean().round(3),'fa mean',faR[nearR].mean().round(3))
# fingertips: lowest z in hand region
tipL=nearL&(P[:,2]<0.88); tipR=nearR&(P[:,2]<0.88)
print('L tips',tipL.sum(),'hips',hips[tipL].mean().round(3) if tipL.any() else None,'hand',handL[tipL].mean().round(3) if tipL.any() else None)
print('R tips',tipR.sum(),'hips',hips[tipR].mean().round(3) if tipR.any() else None,'hand',handR[tipR].mean().round(3) if tipR.any() else None)
# attack f10 bone positions
arm.animation_data.action=bpy.data.actions['ROWAN_attack']; bpy.context.scene.frame_set(10); bpy.context.view_layer.update()
def wpos(n): return (arm.matrix_world@arm.pose.bones[P_+n].head)[:]
print('f10 LeftShoulder',tuple(round(x,3) for x in wpos('LeftShoulder')),'LeftArm',tuple(round(x,3) for x in wpos('LeftArm')),'LeftHand',tuple(round(x,3) for x in wpos('LeftHand')))
print('f10 RightShoulder',tuple(round(x,3) for x in wpos('RightShoulder')),'RightArm',tuple(round(x,3) for x in wpos('RightArm')),'RightHand',tuple(round(x,3) for x in wpos('RightHand')))
print('f10 Head',tuple(round(x,3) for x in wpos('Head')),'Hips',tuple(round(x,3) for x in wpos('Hips')))
# rest
arm.animation_data.action=bpy.data.actions['ROWAN_rest']; bpy.context.scene.frame_set(0); bpy.context.view_layer.update()
print('rest LHand',tuple(round(x,3) for x in wpos('LeftHand')),'RHand',tuple(round(x,3) for x in wpos('RightHand')))
