import bpy, numpy as np, json
body=bpy.data.objects['OAKENSHIELD_body']; arm=bpy.data.objects['OAKENSHIELD_rig']; me=body.data
VG=body.vertex_groups; H=VG['mixamorig:Head'].index; N=VG['mixamorig:Neck'].index
deform={g.index for g in VG if g.name.startswith('mixamorig:')}
nv=len(me.vertices); co=np.array([v.co[:] for v in me.vertices])
# islands
par=np.arange(nv)
def f(a):
    while par[a]!=a: par[a]=par[par[a]]; a=par[a]
    return a
for e in me.edges:
    a,b=f(e.vertices[0]),f(e.vertices[1])
    if a!=b: par[a]=b
root=np.array([f(i) for i in range(nv)])
ax=np.array([0.005,0.03])
r=np.linalg.norm(co[:,:2]-ax,axis=1)
# boundary plane: chin front (y=-0.06,z=1.585) rising toward nape
base=1.585+0.15*(co[:,1]+0.06)
zr=co[:,2]-base
s=np.clip((zr+0.025)/0.04,0,1)   # 0 at 2.5cm below chin line, 1 at 1.5cm above
s[r>0.16]=0
# whole islands: rigid to head if island lies mostly above the neck
isl={}
for i,k in enumerate(root): isl.setdefault(k,[]).append(i)
rig_isl=0; rig_v=0
for k,idx in isl.items():
    idx=np.array(idx)
    if len(idx)>0.3*nv: continue   # main body
    if np.median(zr[idx])>0.0 and np.median(r[idx])<0.32:
        s[idx]=1; rig_isl+=1; rig_v+=len(idx)
ch=0
for v in me.vertices:
    t=s[v.index]
    if t<=0: continue
    gs=[(g.group,g.weight) for g in v.groups if g.group in deform]
    tot=sum(w for _,w in gs) or 1
    new={g:w/tot*(1-t) for g,w in gs}
    new[H]=new.get(H,0)+t
    for g,w in gs:
        if new.get(g,0)<1e-4: VG[g].remove([v.index])
    for g,w in new.items():
        if w>=1e-4: VG[g].add([v.index],w,'REPLACE')
    ch+=1
print('REWEIGHT',json.dumps(dict(changed=ch,full_head=int((s>=1).sum()),blend=int(((s>0)&(s<1)).sum()),rigid_islands=rig_isl,island_verts=rig_v)))
bpy.ops.wm.save_as_mainfile(filepath='/workspace/oak-face/fixed.blend',compress=True)
