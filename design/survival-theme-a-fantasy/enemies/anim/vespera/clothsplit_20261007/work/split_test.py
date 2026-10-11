import bpy, bmesh, sys, numpy as np
sys.path.insert(0,'/workspace/attack_20261007/clothsplit'); import cs_lib as L
src,label=sys.argv[sys.argv.index('--')+1:][:2]
bpy.ops.wm.open_mainfile(filepath=src)
body=next(o for o in bpy.data.objects if o.type=='MESH')
d=np.load(f'cls_{label}.npz'); cl=d['cloth']
bm=bmesh.new(); bm.from_mesh(body.data); bm.faces.ensure_lookup_table(); bm.verts.ensure_lookup_table()
fc=np.array([sum(cl[v.index] for v in f.verts)>=2 for f in bm.faces])
# island cleanup on face adjacency: small cloth islands -> body, small body islands below zcut -> cloth
def islands(mask):
    lab=-np.ones(len(mask),int); n=0
    for f in bm.faces:
        if not mask[f.index] or lab[f.index]>=0: continue
        st=[f]; lab[f.index]=n
        while st:
            x=st.pop()
            for e in x.edges:
                for g in e.link_faces:
                    if mask[g.index] and lab[g.index]<0: lab[g.index]=n; st.append(g)
        n+=1
    return lab,n
for it in range(2):
    lab,n=islands(fc); sz=np.bincount(lab[lab>=0],minlength=n)
    small=[i for i in range(n) if sz[i]<300]; fc[np.isin(lab,small)]=False
    lab,n=islands(~fc); sz=np.bincount(lab[lab>=0],minlength=n); big=np.argmax(sz)
    fc[(lab>=0)&(lab!=big)]=True    # every body island not connected to the main body becomes cloth
print('FACES cloth',fc.sum(),'body',(~fc).sum())
def report(mask,name):
    b=bm.copy(); b.faces.ensure_lookup_table()
    bmesh.ops.delete(b,geom=[b.faces[i] for i in np.where(mask)[0]],context='FACES')
    bnd=[e for e in b.edges if e.is_boundary]
    # loops
    adj={}
    for e in bnd:
        for v in e.verts: adj.setdefault(v,[]).append(e)
    seen=set(); loops=[]
    for e in bnd:
        if e in seen: continue
        st=[e]; comp=[]; seen.add(e)
        while st:
            x=st.pop(); comp.append(x)
            for v in x.verts:
                for y in adj[v]:
                    if y not in seen: seen.add(y); st.append(y)
        vs={v for x in comp for v in x.verts}; P=np.array([v.co[:] for v in vs])
        loops.append((len(comp),P.mean(0).round(3).tolist(),(P.max(0)-P.min(0)).round(3).tolist()))
    loops.sort(key=lambda x:-x[0])
    print(name,'boundary edges',len(bnd),'loops',len(loops)); [print('   ',l) for l in loops[:15]]
    b.free()
report(fc,'BODY')
report(~fc,'CLOTH')
np.save(f'fc_{label}.npy',fc)
