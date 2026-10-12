import bpy, bmesh, numpy as np, json, sys
from mathutils import Vector, kdtree
from mathutils.bvhtree import BVHTree
OUT=sys.argv[sys.argv.index('--')+1]
if bpy.context.object and bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
body=bpy.data.objects['VESPERA_body']; arm=bpy.data.objects['VESPERA_rig']; me=body.data
V0=65195; F0=len(me.polygons)-5276
rep={'verts_in':len(me.vertices),'faces_in':len(me.polygons)}
tw_ok=all(min(p.vertices)>=V0 for p in me.polygons[F0:]) and all(max(p.vertices)<V0 for p in me.polygons[:F0])
rep['twin_split_ok']=tw_ok
bm=bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
orig=[v for v in bm.verts if v.index<V0]
# coincident pairs among originals = 507fb9a split seams
kd=kdtree.KDTree(len(orig))
for v in orig: kd.insert(v.co,v.index)
kd.balance(); pairs=set()
for v in orig:
    for (co,i,d) in kd.find_range(v.co,1e-6):
        if i!=v.index: pairs.add(tuple(sorted((v.index,i))))
rep['seam_pairs_before']=len(pairs)
res=bmesh.ops.remove_doubles(bm,verts=orig,dist=1e-6)
bm.to_mesh(me); me.update(); bm.free()
N=len(me.vertices); co=np.array([v.co[:] for v in me.vertices])
rep['verts_after_weld']=N
# twin vertices = those used only by twin faces (the last 5276 faces keep their order)
F=len(me.polygons); F0=F-5276
twin=np.zeros(N,bool)
for p in me.polygons[F0:]: twin[list(p.vertices)]=True
for p in me.polygons[:F0]: twin[list(p.vertices)]=False
idx_orig=np.nonzero(~twin)[0]
kd=kdtree.KDTree(len(idx_orig))
for i in idx_orig: kd.insert(Vector(co[i]),int(i))
kd.balance()
tmap=np.full(N,-1)
for i in np.nonzero(twin)[0]:
    c,j,d=kd.find(Vector(co[i])); tmap[i]=j
rep['twin_verts']=int(twin.sum()); rep['twin_map_maxdist']=float(max(np.linalg.norm(co[i]-co[tmap[i]]) for i in np.nonzero(twin)[0]))
E=np.array([e.vertices[:] for e in me.edges]); E=E[~twin[E].any(1)]
# remaining coincident original pairs after weld
kd2=kdtree.KDTree(len(idx_orig))
for i in idx_orig: kd2.insert(Vector(co[i]),int(i))
kd2.balance(); left=0
for i in idx_orig:
    if len(kd2.find_range(Vector(co[i]),1e-6))>1: left+=1
rep['coincident_left']=left
# hair detection (as 507fb9a) on original faces
bvh=BVHTree.FromPolygons([v.co[:] for v in me.vertices],[p.vertices[:] for p in me.polygons[:F0]])
bones={b.name.split(':')[1]:(np.array(b.head_local),np.array(b.tail_local)) for b in arm.data.bones}
ks=['Spine1','Spine2','Neck','Head']; zs=np.array([bones[k][0][2] for k in ks])
axx=np.array([bones[k][0][0] for k in ks]); axy=np.array([bones[k][0][1] for k in ks])
hair=np.zeros(N,bool)
cand=np.nonzero((~twin)&(co[:,2]>0.95)&(co[:,2]<1.62))[0]
for i in cand:
    z=co[i,2]; ax=np.interp(z,zs,axx); ay=np.interp(z,zs,axy)
    c=Vector(co[i]); A=Vector((ax,ay,z)); dv=A-c; L=dv.length
    if L<0.04: continue
    if co[i,1]<ay-0.02 and abs(co[i,0]-ax)<0.16: continue
    hit=bvh.ray_cast(c+dv/L*0.006,dv/L,L-0.006)
    if hit[0] is not None and hit[3]>0.012: hair[i]=True
np.savez(OUT+'/mesh.npz',co=co,E=E,twin=twin,tmap=tmap,hair=hair)
json.dump({k:[v[0].tolist(),v[1].tolist()] for k,v in bones.items()},open(OUT+'/bones.json','w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/welded.blend',compress=True)
json.dump(rep,open(OUT+'/weld.json','w'),indent=1); print('WELD',json.dumps(rep))
