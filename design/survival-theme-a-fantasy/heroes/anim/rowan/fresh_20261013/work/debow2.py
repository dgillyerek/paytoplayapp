import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,bmesh,numpy as np
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/work/dec_body.blend')
o=bpy.data.objects['ROWAN_body']; me=o.data
N=len(me.vertices); P=np.empty(N*3); me.vertices.foreach_get('co',P); P=P.reshape(-1,3)
A=np.array([-0.335,1.80]);B=np.array([0.30,0.64]);ab=B-A;L=np.linalg.norm(ab)
q=P[:,[0,2]]-A; t=q@ab/L**2; d0=np.linalg.norm(q-np.clip(t,0,1)[:,None]*ab,axis=1)
PL=np.array([(-0.345,1.82),(-0.28,1.615),(-0.20,1.48),(-0.10,1.38),(0.05,1.11),(0.16,0.885),(0.24,0.70)])
dp=np.full(len(P),9.)
for a,b in zip(PL[:-1],PL[1:]):
    ab2=b-a; qq=P[:,[0,2]]-a; tt=np.clip(qq@ab2/(ab2@ab2),0,1); dp=np.minimum(dp,np.linalg.norm(qq-tt[:,None]*ab2,axis=1))
d=np.minimum(d0,dp); t=np.clip(t,0,1)
m=np.load('/workspace/rowan_fresh/work/bowmask3.npy')
bm=bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
kill=[f for f in bm.faces if sum(m[v.index] for v in f.verts)>=2]
print('DEL faces',len(kill)); bmesh.ops.delete(bm,geom=kill,context='FACES')
loose=[v for v in bm.verts if not v.link_faces]; bmesh.ops.delete(bm,geom=loose,context='VERTS')
# drop tiny leftover islands
import scipy.sparse as sp, scipy.sparse.csgraph as cg
bm.verts.index_update(); bm.verts.ensure_lookup_table()
E=np.array([(e.verts[0].index,e.verts[1].index) for e in bm.edges]); n=len(bm.verts)
k,lab=cg.connected_components(sp.coo_matrix((np.ones(len(E)),(E[:,0],E[:,1])),shape=(n,n)),directed=False); sz=np.bincount(lab)
small=[v for v in bm.verts if sz[lab[v.index]]<300]; print('islands',k,'small verts',len(small)); bmesh.ops.delete(bm,geom=small,context='VERTS')
bm.to_mesh(me); me.update(); print('FACES',len(me.polygons))
bpy.ops.wm.save_as_mainfile(filepath='/workspace/rowan_fresh/work/dec_nobow.blend')
