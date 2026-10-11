"""Vespera no-cape mesh prep (2026-10-09). Input: clothsplit_20261007 VESPERA_clothsplit.blend (read-only).
Removes the loose cloth object, the remaining upper cape sheet (cape1.npy face mask from seg1.py), a cape vine ornament
left hanging behind the right hip, all loose leftover pieces; fills closed boundary holes < 1 m perimeter with one rim texel."""
import bpy, bmesh, numpy as np, sys, json
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; CAPE=args[0]; OUTBLEND=args[1]; OUTJSON=args[2]
rep={}
cape=np.load(CAPE)
bpy.data.objects.remove(bpy.data.objects['VESPERA_cloth'])
rep['cloth_object_removed']={'name':'VESPERA_cloth','faces':56542}
o=bpy.data.objects['VESPERA_body']; me=o.data
cent=np.array([p.center[:] for p in me.polygons])
stick=(cent[:,2]<1.16)&(cent[:,2]>0.8)&(cent[:,1]>0.048)&(cent[:,0]>-0.17)&(cent[:,0]<-0.06)
dele=cape|stick
rep['faces_in']=len(me.polygons); rep['cape_sheet_faces']=int(cape.sum()); rep['ornament_faces']=int((stick&~cape).sum())
bm=bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
uvl=bm.loops.layers.uv.active
def nbound(): return sum(1 for e in bm.edges if e.is_boundary)
rep['boundary_edges_before']=nbound()
bmesh.ops.delete(bm,geom=[bm.faces[i] for i in np.nonzero(dele)[0]],context='FACES')
bm.faces.ensure_lookup_table()
seen=set(); comps=[]
for f in bm.faces:
    if f.index in seen: continue
    st=[f]; comp=[]; seen.add(f.index)
    while st:
        g=st.pop(); comp.append(g)
        for e in g.edges:
            for h in e.link_faces:
                if h.index not in seen: seen.add(h.index); st.append(h)
    comps.append(comp)
comps.sort(key=len,reverse=True)
rm=[f for c in comps[1:] for f in c]
rep['loose_pieces_removed']=len(comps)-1; rep['loose_faces_removed']=len(rm)
bmesh.ops.delete(bm,geom=rm,context='FACES')
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
rep['boundary_edges_after_cut']=nbound()
# boundary loops
def loops():
    be=[e for e in bm.edges if e.is_boundary]; adj={}
    for e in be:
        for v in e.verts: adj.setdefault(v,[]).append(e)
    seen=set(); out=[]
    for e in be:
        if e in seen: continue
        st=[e]; comp=[]; seen.add(e)
        while st:
            g=st.pop(); comp.append(g)
            for v in g.verts:
                for h in adj[v]:
                    if h not in seen: seen.add(h); st.append(h)
        vs={v for g in comp for v in g.verts}
        out.append((comp, all(len(adj[v])==2 for v in vs), sum(g.calc_length() for g in comp)))
    return out
fills=[]
for comp,closed,L in loops():
    if not closed or L>1.0 or len(comp)<3: continue
    rimv={v for g in comp for v in g.verts}
    c=sum((v.co for v in rimv),Vector())/len(rimv)
    near=min(rimv,key=lambda v:(v.co-c).length)
    uv=None
    for l in near.link_loops: uv=l[uvl].uv.copy(); break
    res=bmesh.ops.holes_fill(bm,edges=comp,sides=0)
    nf=res['faces']
    if not nf: continue
    tri=bmesh.ops.triangulate(bm,faces=nf)['faces']
    for f in tri:
        for l in f.loops: l[uvl].uv=uv
    # wind consistently with the neighbours across the rim
    flip=0; tot=0
    for g in comp:
        fs=g.link_faces
        if len(fs)==2:
            a,b=fs; la=[l for l in a.loops if l.edge==g][0]; lb=[l for l in b.loops if l.edge==g][0]
            tot+=1; flip+= (la.vert==lb.vert)
    if tot and flip>tot/2: bmesh.ops.reverse_faces(bm,faces=tri)
    fills.append({'perimeter_m':round(L,3),'rim_edges':len(comp),'faces':len(tri),'center':[round(x,3) for x in c],'uv':[round(uv.x,4),round(uv.y,4)]})
rep['holes_filled']=fills
rep['boundary_edges_final']=nbound()
bm.to_mesh(me); me.update()
rep['faces_out']=len(me.polygons); rep['verts_out']=len(me.vertices)
# drop the old rig
o.parent=None; o.modifiers.clear(); o.vertex_groups.clear()
for a in list(bpy.data.actions): bpy.data.actions.remove(a)
bpy.data.objects.remove(bpy.data.objects['VESPERA_rig'])
o.name='VESPERA_nocape_body'; me.name='VESPERA_nocape_body'
bpy.ops.outliner.orphans_purge(do_recursive=True)
bpy.ops.wm.save_as_mainfile(filepath=OUTBLEND,compress=True)
json.dump(rep,open(OUTJSON,'w'),indent=1)
print('PREP',json.dumps({k:v for k,v in rep.items() if k!='holes_filled'}),'fills',len(fills))
