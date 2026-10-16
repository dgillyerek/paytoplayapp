import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,numpy as np, json
import scipy.sparse as sp, scipy.sparse.csgraph as cg
tag=sys.argv[-2]; target=int(sys.argv[-1])
bpy.ops.wm.open_mainfile(filepath=f'/workspace/bonequill_gritty/work/raw_{tag}.blend')
o=[o for o in bpy.data.objects if o.type=='MESH'][0]; o.name='BONEQUILL_'+tag
bpy.context.view_layer.objects.active=o; o.select_set(True)
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT'); bpy.ops.mesh.remove_doubles(threshold=0.0001); bpy.ops.object.mode_set(mode='OBJECT')
m=o.modifiers.new('dec','DECIMATE'); m.ratio=target/len(o.data.polygons); m.use_collapse_triangulate=True
bpy.ops.object.modifier_apply(modifier='dec')
me=o.data; N=len(me.vertices); P=np.empty(N*3); me.vertices.foreach_get('co',P); P=P.reshape(-1,3)
# shared transform from the body: feet on 0, height 1.80, centred x
zmin,zmax,S=-0.951,0.95,1.80/(0.95+0.951)
if tag=='cape':  # hem 3 cm above the floor, scaled about the hood top
    k=(1.80-0.03)/((zmax+0.951)*S); top=zmax
    P[:,2]=top-(top-P[:,2])*k* (1.0)
P[:,2]=(P[:,2]-zmin)*S; P[:,0]*=S; P[:,1]*=S
if tag=='cape': P[:,2]=np.minimum(P[:,2],P[:,2])  # no-op marker
me.vertices.foreach_set('co',P.ravel()); me.update()
E=np.array([e.vertices[:] for e in me.edges]); n,lab=cg.connected_components(sp.coo_matrix((np.ones(len(E)),(E[:,0],E[:,1])),shape=(N,N)),directed=False)
sz=np.bincount(lab); rep={'faces':len(me.polygons),'verts':N,'islands':int(n),'zmin':float(P[:,2].min()),'zmax':float(P[:,2].max())}
rep['big']=[(int(k),int(sz[k]),P[lab==k].min(0).round(3).tolist(),P[lab==k].max(0).round(3).tolist()) for k in np.argsort(-sz)[:30]]
np.save(f'lab_{tag}.npy',lab); np.save(f'P_{tag}.npy',P)
bpy.ops.wm.save_as_mainfile(filepath=f'/workspace/bonequill_gritty/work/dec_{tag}.blend')
json.dump(rep,open(f'dec_{tag}.json','w'),indent=0); print('DEC',tag,rep['faces'],rep['islands'],rep['zmin'],rep['zmax'])
