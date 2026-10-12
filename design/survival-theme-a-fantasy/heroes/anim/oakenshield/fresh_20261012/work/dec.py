import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,bmesh,numpy as np
bpy.ops.wm.open_mainfile(filepath='raw.blend')
o=bpy.data.objects['Mesh_0']; o.name='OAKENSHIELD_body'
bpy.context.view_layer.objects.active=o; o.select_set(True)
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.remove_doubles(threshold=0.0001); bpy.ops.object.mode_set(mode='OBJECT')
m=o.modifiers.new('dec','DECIMATE'); m.ratio=200000/len(o.data.polygons); m.use_collapse_triangulate=True
bpy.ops.object.modifier_apply(modifier='dec')
me=o.data; print('faces',len(me.polygons),'verts',len(me.vertices))
bm=bmesh.new(); bm.from_mesh(me)
import scipy.sparse as sp, scipy.sparse.csgraph as cg
E=np.array([e.verts[0].index for e in bm.edges]),np.array([e.verts[1].index for e in bm.edges])
N=len(bm.verts); n,lab=cg.connected_components(sp.coo_matrix((np.ones(len(E[0])),E),shape=(N,N)),directed=False)
co=np.array([v.co[:] for v in bm.verts]); sz=np.bincount(lab)
print('islands',n,'boundary',sum(e.is_boundary for e in bm.edges))
for k in np.argsort(-sz)[:25]:
    c=co[lab==k]; print(k,sz[k],c.min(0).round(2),c.max(0).round(2))
np.save('lab.npy',lab)
bpy.ops.wm.save_as_mainfile(filepath='dec.blend')
