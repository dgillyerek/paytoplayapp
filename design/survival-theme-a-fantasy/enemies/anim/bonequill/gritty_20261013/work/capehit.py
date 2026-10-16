import bpy,numpy as np
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
cape,crig=d.objects
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P_+bn
arm.animation_data.action=bpy.data.actions['BONEQUILL_attack']
def segd(Q,a,b):
    ab=b-a; t=np.clip(((Q-a)@ab)/(ab@ab),0,1); return np.linalg.norm(Q-(a+t[:,None]*ab),axis=1)
out=[]
for f in range(0,31,2):
    sc.frame_set(f); dg=bpy.context.evaluated_depsgraph_get(); e=cape.evaluated_get(dg); m=e.to_mesh()
    Q=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',Q); Q=Q.reshape(-1,3); e.to_mesh_clear()
    Q=(np.c_[Q,np.ones(len(Q))]@np.array(cape.matrix_world).T)[:,:3]
    n=0
    for s in ('Left','Right'):
        pts=[np.array(arm.matrix_world@arm.pose.bones[P_+s+k].head) for k in ('Arm','ForeArm','Hand')]
        hb=arm.pose.bones[P_+s+'Hand']; pts.append(np.array(arm.matrix_world@hb.tail))
        for a,b,r in ((pts[0],pts[1],0.05),(pts[1],pts[2],0.045),(pts[2],pts[3],0.045)):
            n+=int((segd(Q,a,b)<r).sum())
    out.append((f,n))
print('CAPEHIT',out)
sc.frame_set(10); dg=bpy.context.evaluated_depsgraph_get(); e=cape.evaluated_get(dg); m=e.to_mesh()
Q=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',Q); Q=Q.reshape(-1,3); e.to_mesh_clear()
R=np.array([v.co[:] for v in cape.data.vertices]); names=[g.name for g in cape.vertex_groups]
for s in ('Left','Right'):
    pts=[np.array(arm.pose.bones[P_+s+k].head) for k in ('Arm','ForeArm','Hand')]
    for a,b in ((pts[0],pts[1]),(pts[1],pts[2])):
        idx=np.nonzero(segd(Q,a,b)<0.05)[0]
        if len(idx): 
            v=cape.data.vertices[int(idx[0])]
            print('HIT',s,len(idx),'rest',R[idx].mean(0).round(2),R[idx].min(0).round(2),R[idx].max(0).round(2),{names[g.group]:round(g.weight,2) for g in v.groups})
