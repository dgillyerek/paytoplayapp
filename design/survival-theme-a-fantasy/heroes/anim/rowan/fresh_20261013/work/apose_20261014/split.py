"""Split the long coat tails (blue, below the belt) off the A-pose body into ROWAN_coat; body keeps everything else."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,bmesh,numpy as np,json, scipy.sparse as sp, scipy.sparse.csgraph as cg
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/w2/dec_body.blend')
body=bpy.data.objects['ROWAN_body']; me=body.data
keep=np.load('coat_faces.npy'); P=np.load('P_body.npy'); z=P[:,2]
F=np.empty(len(me.polygons)*3,int); me.polygons.foreach_get('vertices',F); F=F.reshape(-1,3)
# body faces left over: tiny islands below the belt (hem trim slivers) go to the coat
rest=~keep; N=len(P)
def comps(mask):
    fi=np.nonzero(mask)[0]; vf=sp.coo_matrix((np.ones(len(fi)*3),(F[fi].ravel(),np.repeat(np.arange(len(fi)),3))),shape=(N,len(fi))).tocsr()
    n,lab=cg.connected_components(vf.T@vf,directed=False); return fi,lab,np.bincount(lab)
fi,lab,sz=comps(rest); moved=0
for k in np.nonzero(sz<3000)[0]:
    f=fi[lab==k]
    if z[F[f]].mean()<0.99: keep[f]=True; moved+=len(f)
rep={'coat_faces':int(keep.sum()),'trim_faces_moved_to_coat':moved}
coat=body.copy(); coat.data=me.copy(); coat.name='ROWAN_coat'; coat.data.name='ROWAN_coat'; bpy.context.scene.collection.objects.link(coat)
for ob,kill in ((body,keep),(coat,~keep)):
    bm=bmesh.new(); bm.from_mesh(ob.data); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.faces[i] for i in np.nonzero(kill)[0]],context='FACES'); bm.to_mesh(ob.data); bm.free(); ob.data.update()
for ob in (body,coat):
    Q=np.empty(len(ob.data.vertices)*3); ob.data.vertices.foreach_get('co',Q); np.save(f'P_{ob.name}.npy',Q.reshape(-1,3))
    rep[ob.name]={'faces':len(ob.data.polygons),'verts':len(ob.data.vertices)}
bpy.ops.wm.save_as_mainfile(filepath='/workspace/rowan_fresh/w2/split.blend')
json.dump(rep,open('split.json','w'),indent=1); print('SPLIT',rep)
