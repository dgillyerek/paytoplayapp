import bpy,numpy as np
W='/workspace/bonequill_fresh/work'
bpy.ops.wm.open_mainfile(filepath=W+'/anim.blend'); sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
crig=bpy.data.objects['BONEQUILL_cape_rig']; cape=bpy.data.objects['BONEQUILL_cape']
for cn,bn in (('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P_+bn
arm.animation_data.action=bpy.data.actions['BONEQUILL_rest']; sc.frame_set(0)
P0=np.array([v.co[:] for v in cape.data.vertices])
dg=bpy.context.evaluated_depsgraph_get(); e=cape.evaluated_get(dg); m=e.to_mesh(); Q=np.array([v.co[:] for v in m.vertices]); e.to_mesh_clear()
d=np.linalg.norm(Q-P0,axis=1); print('DISP max',d.max(),'n>2cm',(d>0.02).sum())
for n in ('cape_Spine2','cape_LeftShoulder','cape_RightShoulder'):
    pb=crig.pose.bones[n]; print(n,(pb.matrix@crig.data.bones[n].matrix_local.inverted()).to_euler(), (arm.pose.bones[P_+n[5:]].matrix@arm.data.bones[P_+n[5:]].matrix_local.inverted()).to_euler())
i=np.argsort(-d)[:5]; vg={g.index:g.name for g in cape.vertex_groups}
for k in i: print(P0[k],d[k],[(vg[g.group],round(g.weight,2)) for g in cape.data.vertices[k].groups])
