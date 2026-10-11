import bpy, bmesh, numpy as np, sys
args=sys.argv[sys.argv.index('--')+1:]; CAPE=args[0]; OUTBLEND=args[1]
cape=np.load(CAPE)
bpy.data.objects.remove(bpy.data.objects['VESPERA_cloth'])
o=bpy.data.objects['VESPERA_body']; me=o.data
bm=bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
def nbound(bm): return sum(1 for e in bm.edges if e.is_boundary)
print('boundary before',nbound(bm))
bmesh.ops.delete(bm,geom=[bm.faces[i] for i in np.nonzero(cape)[0]],context='FACES')
# loose components
bm.faces.ensure_lookup_table(); seen=set(); comps=[]
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
print('components',[len(c) for c in comps[:15]], 'n',len(comps))
import json
info=[]
for c in comps[1:]:
    zs=[f.calc_center_median().z for f in c]; info.append((len(c),min(zs),max(zs)))
rm=[f for c in comps[1:] for f in c]
bmesh.ops.delete(bm,geom=rm,context='FACES')
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS')
print('removed loose faces',len(rm),'boundary after',nbound(bm))
bm.to_mesh(me); me.update()
print('faces now',len(me.polygons),'verts',len(me.vertices))
bpy.ops.wm.save_as_mainfile(filepath=OUTBLEND,compress=True)
