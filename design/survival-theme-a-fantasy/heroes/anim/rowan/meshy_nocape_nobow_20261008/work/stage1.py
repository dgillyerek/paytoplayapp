import bpy, bmesh, numpy as np
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath='src/ROWAN_meshy_rest.fbx')
for o in list(bpy.data.objects):
    if o.name.startswith('Icosphere'): bpy.data.objects.remove(o, do_unlink=True)
me=bpy.data.objects['output_unwrapped']; arm=bpy.data.objects['target_character']
import os
THR=int(os.environ.get("THR","1"))
lab=np.load('m_lab_w.npy')|np.load('m_lab_s.npy')
img=None
for n in me.active_material.node_tree.nodes:
    if n.type=='TEX_IMAGE' and n.image: print('tex node',n.image.name, n.image.colorspace_settings.name)
for n in me.active_material.node_tree.nodes:
    if n.type=='TEX_IMAGE' and n.image and n.image.colorspace_settings.name=='sRGB': img=n.image
print('basecolor img',img.name,img.size[:])
W,H=img.size; px=np.empty(W*H*4,np.float32); img.pixels.foreach_get(px); px=px.reshape(H,W,4)
def lum(uv):
    x=int(np.clip(uv[0]%1*W,0,W-1)); y=int(np.clip(uv[1]%1*H,0,H-1)); c=px[y,x]; return 0.3*c[0]+0.59*c[1]+0.11*c[2]
bm=bmesh.new(); bm.from_mesh(me.data)
oid=bm.verts.layers.int.new('oid')
for v in bm.verts: v[oid]=v.index
deform=bm.verts.layers.deform.active; uvl=bm.loops.layers.uv.active
dele=[f for f in bm.faces if sum(lab[v.index] for v in f.verts)>=THR]
touched=set(v.index for f in dele for v in f.verts)
bmesh.ops.delete(bm, geom=dele, context='FACES_ONLY')
loose=[v for v in bm.verts if not v.link_faces]; bmesh.ops.delete(bm, geom=loose, context='VERTS')
print('deleted faces',len(dele),'loose verts',len(loose))
def islands():
    bm.faces.ensure_lookup_table(); seen=set(); isl=[]
    for f in bm.faces:
        if f.index in seen: continue
        st=[f]; comp=[]; seen.add(f.index)
        while st:
            g=st.pop(); comp.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen: seen.add(h.index); st.append(h)
        isl.append(comp)
    isl.sort(key=len, reverse=True); return isl
isl=islands(); print('islands',[len(c) for c in isl[:8]],'n',len(isl))
small=[f for c in isl[1:] for f in c]
if small:
    for f in small:
        for v in f.verts: touched.add(v[oid])
    bmesh.ops.delete(bm, geom=small, context='FACES_ONLY')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
print('removed non-main island faces',len(small))
# boundary edges near touched verts
bnd=[e for e in bm.edges if len(e.link_faces)==1]
print('boundary edges total',len(bnd))
bnd=set(e for e in bnd if e.verts[0][oid] in touched or e.verts[1][oid] in touched)
print('boundary edges at cut',len(bnd))
# order edges into loops
adj={}
for e in bnd:
    for v in e.verts: adj.setdefault(v,[]).append(e)
nonman=[v for v,l in adj.items() if len(l)!=2]; print('non-2-valent boundary verts',len(nonman))
used=set(); loops=[]
for e0 in bnd:
    if e0 in used: continue
    # orient by existing face: face loop goes a->b, so the cap goes b->a
    f=e0.link_faces[0]
    for l in f.loops:
        if l.edge==e0: a,b=l.vert,l.link_loop_next.vert
    seq=[b,a]; used.add(e0); cur=a; prev=e0; ok=True
    while True:
        cand=[e for e in adj[cur] if e not in used]
        if not cand:
            ok = (seq[0] in [v for e in adj[cur] for v in e.verts]); break
        e=cand[0]; used.add(e); nv=e.other_vert(cur)
        if nv==seq[0]: break
        seq.append(nv); cur=nv
    loops.append((seq,ok))
print('loops',[(len(s),ok) for s,ok in loops])
capped=0
for seq,ok in loops:
    if len(seq)<3: continue
    co=sum((v.co for v in seq),Vector())/len(seq)
    c=bm.verts.new(co)
    # weights: average rim
    acc={}
    for v in seq:
        for g,w in v[deform].items(): acc[g]=acc.get(g,0)+w/len(seq)
    top=sorted(acc.items(), key=lambda x:-x[1])[:4]; ssum=sum(w for _,w in top)
    for g,w in top: c[deform][g]=w/ssum
    c[oid]=-1
    # rim uv: median luminance among rim loops
    uvs=[]
    for v in seq:
        for l in v.link_loops: uvs.append(tuple(l[uvl].uv))
    uvs.sort(key=lum); uv=uvs[len(uvs)//2]
    for i in range(len(seq)):
        v1,v2=seq[i],seq[(i+1)%len(seq)]
        try:
            f=bm.faces.new((v1,v2,c))
        except ValueError: continue
        f.smooth=True
        for l in f.loops: l[uvl].uv=uv
        capped+=1
print('cap faces',capped)
bm.to_mesh(me.data); bm.free()
print('final faces',len(me.data.polygons),'verts',len(me.data.vertices))
# remove helper attr
a=me.data.attributes.get('oid')
if a: me.data.attributes.remove(a)
bpy.ops.wm.save_as_mainfile(filepath='/workspace/rig_meshy08/out/stage1.blend')
