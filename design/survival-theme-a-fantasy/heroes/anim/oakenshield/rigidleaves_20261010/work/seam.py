import bpy, sys, json, numpy as np
a=sys.argv[sys.argv.index('--')+1:]; SIDE=a[0]; RAD=float(a[1])
o=bpy.data.objects['OAKENSHIELD_body']; me=o.data; arm=bpy.data.objects['OAKENSHIELD_rig']
N=len(me.vertices); co=np.array([v.co[:] for v in me.vertices]); G=[g.name for g in o.vertex_groups]
W=np.zeros((N,len(G)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
E=np.array([e.vertices[:] for e in me.edges])
j=np.array(arm.data.bones['mixamorig:'+SIDE+'UpLeg'].head_local)
near=np.linalg.norm((co[E[:,0]]+co[E[:,1]])/2-j,axis=1)<RAD
iu=G.index('mixamorig:'+SIDE+'UpLeg'); ih=G.index('mixamorig:Hips')
dU=np.abs(W[E[:,0],iu]-W[E[:,1],iu]); L=np.linalg.norm(co[E[:,0]]-co[E[:,1]],axis=1)
grad=dU/np.maximum(L,1e-4)   # weight change per metre
m=near
print('SEAM',SIDE,'edges',int(m.sum()),'jump>0.5',int((m&(dU>0.5)).sum()),'jump>0.9',int((m&(dU>0.9)).sum()),'grad>20/m',int((m&(grad>20)).sum()),'grad_p99',round(float(np.percentile(grad[m],99)),1))
bad=m&(dU>0.5); 
if bad.any():
    mid=(co[E[bad,0]]+co[E[bad,1]])/2; print(' bad mid z range',mid[:,2].min().round(3),mid[:,2].max().round(3),' mean',mid.mean(0).round(3))
    # are these on 1-bone rigid verts?
    one=(W>0).sum(1)==1
    print(' bad edges with a 1-bone vertex',int((one[E[bad,0]]|one[E[bad,1]]).sum()))
# blend width along the thigh: verts with 0.1<wU<0.9
v=np.linalg.norm(co-j,axis=1)<RAD
print(' blend verts (0.1<w<0.9)',int((v&(W[:,iu]>0.1)&(W[:,iu]<0.9)).sum()),'of',int(v.sum()))
