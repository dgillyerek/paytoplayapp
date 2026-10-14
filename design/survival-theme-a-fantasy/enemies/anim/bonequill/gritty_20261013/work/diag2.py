import bpy,numpy as np
sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; body=bpy.data.objects['BONEQUILL_body']; me=body.data; N=len(me.vertices)
P=np.empty(N*3); me.vertices.foreach_get('co',P); P=P.reshape(-1,3)
E=np.array([e.vertices[:] for e in me.edges]); L0=np.linalg.norm(P[E[:,0]]-P[E[:,1]],axis=1)
names=[g.name for g in body.vertex_groups]; W=np.zeros((N,len(names)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
import sys; A_,F_=sys.argv[-1].split(':'); arm.animation_data.action=bpy.data.actions[A_]; sc.frame_set(int(F_))
dg=bpy.context.evaluated_depsgraph_get(); e=body.evaluated_get(dg); m=e.to_mesh(); Q=np.empty(N*3); m.vertices.foreach_get('co',Q); Q=Q.reshape(-1,3)
L1=np.linalg.norm(Q[E[:,0]]-Q[E[:,1]],axis=1); r=L1-L0
print('stretched >5cm',(r>0.05).sum())
for i in np.argsort(-r)[:10]:
    a,b=E[i]; print(round(r[i],3),P[a].round(2),P[b].round(2),{names[j]:round(W[a,j],2) for j in np.nonzero(W[a])[0]},{names[j]:round(W[b,j],2) for j in np.nonzero(W[b])[0]})
