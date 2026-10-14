import bpy,numpy as np
body=bpy.data.objects['BONEQUILL_body']; me=body.data; N=len(me.vertices)
P=np.empty(N*3); me.vertices.foreach_get('co',P); P=P.reshape(-1,3)
F=np.array([p.vertices[:] for p in me.polygons])
names=[g.name for g in body.vertex_groups]; W=np.zeros((N,len(names)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
dom=W.argmax(1)
L=np.max([np.linalg.norm(P[F[:,i]]-P[F[:,(i+1)%3]],axis=1) for i in range(3)],0)
print('faces',len(F),'long>5cm',(L>0.05).sum(),'>10cm',(L>0.10).sum())
for i in np.argsort(-L)[:15]: print(round(L[i],3),[names[dom[v]] for v in F[i]],P[F[i]].round(2).tolist())
