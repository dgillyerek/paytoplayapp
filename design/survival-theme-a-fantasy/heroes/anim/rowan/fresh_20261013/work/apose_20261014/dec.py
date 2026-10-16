import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,numpy as np, json
import scipy.sparse as sp, scipy.sparse.csgraph as cg
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/w2/raw_body.blend')
o=[o for o in bpy.data.objects if o.type=='MESH'][0]; o.name='ROWAN_body'
bpy.context.view_layer.objects.active=o; o.select_set(True)
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.remove_doubles(threshold=0.0001); bpy.ops.object.mode_set(mode='OBJECT')
m=o.modifiers.new('dec','DECIMATE'); m.ratio=200000/len(o.data.polygons); m.use_collapse_triangulate=True
bpy.ops.object.modifier_apply(modifier='dec')
me=o.data; N=len(me.vertices); P=np.empty(N*3); me.vertices.foreach_get('co',P); P=P.reshape(-1,3)
zmin,zmax=P[:,2].min(),P[:,2].max(); S=1.80/(zmax-zmin)
P[:,2]=(P[:,2]-zmin)*S; P[:,0]*=S; P[:,1]*=S
me.vertices.foreach_set('co',P.ravel()); me.update()
E=np.array([e.vertices[:] for e in me.edges]); n,lab=cg.connected_components(sp.coo_matrix((np.ones(len(E)),(E[:,0],E[:,1])),shape=(N,N)),directed=False)
sz=np.bincount(lab); rep={'faces':len(me.polygons),'verts':N,'islands':int(n)}
rep['big']=[(int(k),int(sz[k]),P[lab==k].min(0).round(3).tolist(),P[lab==k].max(0).round(3).tolist()) for k in np.argsort(-sz)[:20]]
np.save('lab_body.npy',lab); np.save('P_body.npy',P)
bpy.ops.wm.save_as_mainfile(filepath='/workspace/rowan_fresh/w2/dec_body.blend')
json.dump(rep,open('dec_body.json','w')); print('DEC',json.dumps(rep))
