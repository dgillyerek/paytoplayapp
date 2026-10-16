import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,numpy as np
import scipy.sparse as sp, scipy.sparse.csgraph as cg
bpy.ops.wm.open_mainfile(filepath='raw_body.blend'); me=[o for o in bpy.data.objects if o.type=='MESH'][0].data
N=len(me.vertices); P=np.empty(N*3); me.vertices.foreach_get('co',P); P=P.reshape(-1,3)
z0=P[:,2].min(); S=1.80/(P[:,2].max()-z0); P[:,2]=(P[:,2]-z0)*S; P[:,:2]*=S
E=np.empty(len(me.edges)*2,int); me.edges.foreach_get('vertices',E); E=E.reshape(-1,2)
n,lab=cg.connected_components(sp.coo_matrix((np.ones(len(E)),(E[:,0],E[:,1])),shape=(N,N)),directed=False)
np.save('raw_lab.npy',lab); np.save('raw_P.npy',P)
for k in range(n):
    Q=P[lab==k]; lo,hi=Q.min(0),Q.max(0)
    if (hi[2]>1.62 and lo[0]<-0.2) or (Q[:,1].mean()>0.05 and np.ptp(Q[:,2])>0.15 and len(Q)<4000):
        print(k,len(Q),lo.round(2),hi.round(2),Q.mean(0).round(2))
