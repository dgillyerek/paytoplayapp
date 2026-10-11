"""Free Vespera's right hand: delete the faces that join the right forearm/hand surface to the skirt/belt/hip (min-cut
edges from cut_label.py), close the new holes with nearby paint, then dump the mesh for the weight solve."""
import bpy, bmesh, numpy as np, json, sys
from mathutils import Vector, kdtree
from mathutils.bvhtree import BVHTree
OUT=sys.argv[sys.argv.index('--')+1]
W='/workspace/vespera-hand/work/'
if bpy.context.object and bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
body=bpy.data.objects['VESPERA_body']; arm=bpy.data.objects['VESPERA_rig']; me=body.data
m=np.load('/workspace/vespera-reweight/work/mesh.npz'); twin=m['twin']; tmap=m['tmap']
assert len(me.vertices)==len(twin)
armside=np.load(W+'armside_full.npy'); cutE=np.load(W+'cutE.npy'); R=np.load(W+'region.npy')
src=np.where(twin,tmap,np.arange(len(twin)))          # twin vertex -> the surface vertex it lines
cutset=set(map(tuple,np.sort(cutE,1).tolist()))
bm=bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table()
uv=bm.loops.layers.uv.active
tw=bm.verts.layers.int.new('is_twin'); asd=bm.verts.layers.int.new('armside')
for v in bm.verts: v[tw]=int(twin[v.index]); v[asd]=int(armside[src[v.index]] and R[src[v.index]])
dele=[]
for f in bm.faces:
    vs=[src[v.index] for v in f.verts]
    if any(tuple(sorted((vs[i],vs[(i+1)%len(vs)]))) in cutset for i in range(len(vs))): dele.append(f)
rep={'faces_deleted':len(dele),'faces_deleted_lining':sum(1 for f in dele if all(twin[v.index] for v in f.verts))}
nbr=set(e for f in dele for e in f.edges)
bmesh.ops.delete(bm,geom=dele,context='FACES_ONLY')
loose_e=[e for e in bm.edges if not e.link_faces]; bmesh.ops.delete(bm,geom=loose_e,context='EDGES')
loose_v=[v for v in bm.verts if not v.link_edges]; rep['loose_verts_removed']=len(loose_v); bmesh.ops.delete(bm,geom=loose_v,context='VERTS')
newb=[e for e in nbr if e.is_valid and len(e.link_faces)==1]
rep['new_border_edges']=len(newb)
# group the new border into loops; fill only loops made entirely of new border (true holes), leave sheet edges open
from collections import defaultdict
adj=defaultdict(list)
for e in newb:
    adj[e.verts[0]].append(e); adj[e.verts[1]].append(e)
seen=set(); loops=[]
for e in newb:
    if e in seen: continue
    comp=[]; stack=[e]
    while stack:
        x=stack.pop()
        if x in seen: continue
        seen.add(x); comp.append(x)
        for v in x.verts:
            for y in adj[v]:
                if y not in seen: stack.append(y)
    loops.append(comp)
filled=[]; open_=[]
for comp in loops:
    vs=set(v for e in comp for v in e.verts)
    closed=all(sum(1 for e in comp if v in e.verts)==2 for v in vs) and all(sum(1 for e in v.link_edges if len(e.link_faces)==1)==2 for v in vs)
    side='hand' if np.mean([v[asd] for v in vs])>0.5 else 'body'
    twn=np.mean([v[tw] for v in vs])>0.5
    c=np.mean([list(v.co) for v in vs],0)
    if closed and len(vs)>=3:
        r=bmesh.ops.holes_fill(bm,edges=comp,sides=0)
        nf=r['faces']
        if nf:
            t=bmesh.ops.triangulate(bm,faces=nf); nf=t['faces']
            for f in nf:
                if twn: f.normal_flip()
                for l in f.loops:
                    # nearby paint: uv of this vertex in an existing face
                    olds=[ol for ol in l.vert.link_loops if ol.face not in nf]
                    if olds: l[uv].uv=olds[0][uv].uv.copy()
                f.material_index=olds[0].face.material_index if olds else 0
                f.smooth=False
            filled.append(dict(side=side,lining=bool(twn),edges=len(comp),faces=len(nf),center=[round(float(x),3) for x in c]))
    elif not twn and len(vs)>=3:
        # not a clean loop for holes_fill (touches an older border / pinched vertex): close it with a fan to its centre
        deg={v:sum(1 for e in comp if v in e.verts) for v in vs}
        cv=bm.verts.new(Vector(c)); cv[tw]=0; cv[asd]=1 if side=='hand' else 0
        nf=[]
        for e in comp:
            lf=e.link_faces[0]; a_,b_=e.verts
            # keep winding consistent with the existing face across the edge
            for l in lf.loops:
                if l.edge==e: a_,b_=(l.vert,l.link_loop_next.vert); break
            try: nf.append(bm.faces.new((b_,a_,cv)))
            except ValueError: pass
        for f in nf:
            for l in f.loops:
                olds=[ol for ol in l.vert.link_loops if ol.face not in nf]
                if olds: l[uv].uv=olds[0][uv].uv.copy()
                elif l.vert is cv:
                    pass
            f.material_index=0; f.smooth=False
        # an open chain leaves two fan spokes on the border: bridge their outer ends to close the hand
        sp_=[e for e in cv.link_edges if len(e.link_faces)==1]
        if len(sp_)==2:
            p_=sp_[0].other_vert(cv); q_=sp_[1].other_vert(cv)
            l0=[l for l in sp_[0].link_faces[0].loops if l.edge==sp_[0]][0]
            tri=(l0.link_loop_next.vert,l0.vert,q_) if l0.vert is not q_ else None
            try:
                f=bm.faces.new((cv,p_,q_) if l0.vert is cv else (p_,cv,q_)); nf.append(f)
            except ValueError: pass
        # centre vertex paint: average of the ring
        for f in nf:
            for l in f.loops:
                if l.vert is cv:
                    ring=[ol[uv].uv for ff in nf for ol in ff.loops if ol.vert is not cv]
                    l[uv].uv=sum((Vector(u) for u in ring),Vector((0,0)))/len(ring)
        filled.append(dict(side=side,lining=False,edges=len(comp),faces=len(nf),center=[round(float(x),3) for x in c],fan=True,open_chain=any(dg!=2 for dg in deg.values())))
    else:
        open_.append(dict(side=side,lining=bool(twn),edges=len(comp),center=[round(float(x),3) for x in c]))
rep['holes_filled']=filled; rep['open_borders_left']=open_
bm.normal_update(); bm.to_mesh(me); me.update(); bm.free()
# ---- dump for the solver ----
N=len(me.vertices); co=np.array([v.co[:] for v in me.vertices])
TW=np.zeros(N,np.int32); me.attributes['is_twin'].data.foreach_get('value',TW); twin2=TW.astype(bool)
AS=np.zeros(N,np.int32); me.attributes['armside'].data.foreach_get('value',AS)
idx=np.nonzero(~twin2)[0]; kd=kdtree.KDTree(len(idx))
for i in idx: kd.insert(Vector(co[i]),int(i))
kd.balance(); tm2=np.full(N,-1)
for i in np.nonzero(twin2)[0]: c_,j,d_=kd.find(Vector(co[i])); tm2[i]=j
E=np.array([e.vertices[:] for e in me.edges]); E=E[~twin2[E].any(1)]
F0=[p for p in me.polygons if not twin2[list(p.vertices)].all()]
bvh=BVHTree.FromPolygons([v.co[:] for v in me.vertices],[p.vertices[:] for p in F0])
bones={b.name.split(':')[1]:(np.array(b.head_local),np.array(b.tail_local)) for b in arm.data.bones}
ks=['Spine1','Spine2','Neck','Head']; zs=np.array([bones[k][0][2] for k in ks]); axx=np.array([bones[k][0][0] for k in ks]); axy=np.array([bones[k][0][1] for k in ks])
hair=np.zeros(N,bool)
for i in np.nonzero((~twin2)&(co[:,2]>0.95)&(co[:,2]<1.62))[0]:
    z=co[i,2]; ax=np.interp(z,zs,axx); ay=np.interp(z,zs,axy); c_=Vector(co[i]); A=Vector((ax,ay,z)); dv=A-c_; L=dv.length
    if L<0.04: continue
    if co[i,1]<ay-0.02 and abs(co[i,0]-ax)<0.16: continue
    hit=bvh.ray_cast(c_+dv/L*0.006,dv/L,L-0.006)
    if hit[0] is not None and hit[3]>0.012: hair[i]=True
np.savez(W+'mesh.npz',co=co,E=E,twin=twin2,tmap=tm2,hair=hair,armside=AS.astype(bool))
json.dump({k:[v[0].tolist(),v[1].tolist()] for k,v in bones.items()},open(W+'bones.json','w'),indent=1)
rep['verts']=N; rep['faces']=len(me.polygons); rep['hand_side_verts']=int(AS.sum())
# closed-hand check: border edges on the hand piece (verts with armside)
bm=bmesh.new(); bm.from_mesh(me); lay=bm.verts.layers.int['armside']
rep['hand_side_border_edges']=sum(1 for e in bm.edges if len(e.link_faces)==1 and all(v[lay] for v in e.verts)); bm.free()
bpy.ops.wm.save_as_mainfile(filepath=OUT,compress=True)
json.dump(rep,open(W+'cut.json','w'),indent=1); print('CUT',json.dumps({k:v for k,v in rep.items() if k not in ('holes_filled','open_borders_left')}),'filled',len(filled),'open',len(open_))
