"""Build held-weapon props from the split Meshy weapon meshes (2026-10-08).
- bridge cylinder through the fist gap (single texel from the cap = solid colour, no blur)
- Rowan: Meshy string removed in split; new generated bowstring + 2-bone prop armature (bow_grip -> bow_nock)
- prop frame: origin at grip centre, +Z along weapon long axis, projectile/shoot direction local -Y
Writes /workspace/rig_20261008/props/{TAG}_props.blend + {TAG}_props.json (rest world matrix + hand offsets)."""
import bpy, bmesh, sys, json, math, numpy as np
from mathutils import Vector, Matrix
TAG=sys.argv[sys.argv.index('--')+1]
R='/workspace/rig_20261008'; B='/workspace/design/survival-theme-a-fantasy/heroes/anim'
CFG={'rowan':dict(NAME='ROWAN_nocape',RIG=f'{B}/rowan/blender_rig_nocape_20261008/ROWAN_nocape_blenderig.blend',PROP='ROWAN_bow',OUTN='ROWAN_bow',HAND='mixamorig:RightHand'),
     'lyra':dict(NAME='LYRA_tighten',RIG=f'{B}/lyra/blender_rig_tighten_20261008/LYRA_tighten_blenderig.blend',PROP='LYRA_staff',OUTN='LYRA_staff',HAND='mixamorig:RightHand')}[TAG]
meta=json.load(open(f'{R}/{TAG}/split_meta.json')); SH=Vector(meta['shift'])
bpy.ops.wm.open_mainfile(filepath=CFG['RIG'])
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
for o in list(bpy.data.objects):
    if o.type=='MESH': bpy.data.objects.remove(o,do_unlink=True)
with bpy.data.libraries.load(f'{R}/{TAG}/split.blend') as (src,dst): dst.objects=[CFG['PROP']]
ob=dst.objects[0]; bpy.context.scene.collection.objects.link(ob); ob.name=CFG['OUTN']+'_tmp'
me=ob.data
bm=bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
# islands
seen=set(); isl=[]
for v in bm.verts:
    if v.index in seen: continue
    st=[v]; comp=[]; seen.add(v.index)
    while st:
        x=st.pop(); comp.append(x)
        for e in x.link_edges:
            y=e.other_vert(x)
            if y.index not in seen: seen.add(y.index); st.append(y)
    isl.append(comp)
isl.sort(key=len,reverse=True)
drop=[v for c in isl[2:] for v in c]
print('islands',[len(c) for c in isl],'dropping',len(drop))
bmesh.ops.delete(bm,geom=drop,context='VERTS'); bm.verts.ensure_lookup_table(); bm.faces.ensure_lookup_table()
uvl=bm.loops.layers.uv.active
co=np.array([v.co[:] for v in bm.verts])
def capw(p): return Vector(p)+SH   # raw split coords -> new coords
if TAG=='rowan':
    A=capw(meta['caps_prop'][3][1]); Bc=capw(meta['caps_prop'][0][1])   # lower-limb top cap / upper-limb bottom cap
    s0=capw(meta['caps_prop'][5][1]); s1=capw(meta['caps_prop'][2][1])  # string attach points (lower / upper nock cuts)
else:
    A=capw(meta['caps_prop'][1][1]); Bc=capw(meta['caps_prop'][0][1])   # shaft top cap / head bottom cap
print('bridge',tuple(A),tuple(Bc),'len',(Bc-A).length)
ax=(Bc-A).normalized()
def radius_at(c,t0,t1):
    d=co-np.array(c); t=d@np.array(ax); perp=np.linalg.norm(d-np.outer(t,np.array(ax)),axis=1)
    m=(t>t0)&(t<t1)&(perp<0.05); return float(np.median(perp[m])) if m.sum()>5 else 0.02
rA,rB=radius_at(A,-0.04,-0.02),radius_at(Bc,0.02,0.04); print('radii raw',rA,rB)
rA=min(rA,0.024); rB=min(rB,0.024); print('radii',rA,rB)
# texel: uv of nearest loop to cap A rim
def nearest_uv(p, maxd=0.06):
    best=None
    for f in bm.faces:
        c=f.calc_center_median()
        d=(c-p).length
        if d<maxd and (best is None or d<best[0]): best=(d,f.loops[0][uvl].uv.copy())
    return best[1]
nt=me.materials[0].node_tree; bsdf=[n for n in nt.nodes if n.type=='BSDF_PRINCIPLED'][0]
img=bsdf.inputs['Base Color'].links[0].from_node.image; print('basecolor image',img.name)
IW,IH=img.size; PX=np.array(img.pixels[:],dtype=np.float32).reshape(IH,IW,4)
def texlum(uv):
    x=int((uv.x%1)*IW); y=int((uv.y%1)*IH); c=PX[min(y,IH-1),min(x,IW-1),:3]; return float(c.mean()),c
cands=[]
for f in bm.faces:
    cc=f.calc_center_median()
    for P0 in (A-ax*0.03,Bc+ax*0.03):
        if (cc-P0).length<0.03: cands.append(f.loops[0][uvl].uv.copy())
lums=np.array([texlum(u)[0] for u in cands]); med=np.median(lums)
uvA=cands[int(np.abs(lums-med).argmin())]; print('texel',len(cands),'lum',texlum(uvA))
# bridge cylinder (extends 6mm into both limbs)
N=16; p0=A-ax*0.006; p1=Bc+ax*0.006
u=ax.orthogonal().normalized(); w=ax.cross(u)
rings=[]
for k,(p,r) in enumerate(((p0,rA),(p1,rB))):
    rings.append([bm.verts.new(p+(u*math.cos(2*math.pi*i/N)+w*math.sin(2*math.pi*i/N))*r) for i in range(N)])
newf=[]
for i in range(N):
    f=bm.faces.new((rings[0][i],rings[0][(i+1)%N],rings[1][(i+1)%N],rings[1][i])); newf.append(f)
for rg in rings:
    c=bm.verts.new(sum((v.co for v in rg),Vector())/N)
    for i in range(N): newf.append(bm.faces.new((c,rg[(i+1)%N],rg[i])))
for f in newf:
    f.smooth=True
    for l in f.loops: l[uvl].uv=uvA
bmesh.ops.recalc_face_normals(bm,faces=newf)
grip=(A+Bc)/2
# frame
if TAG=='rowan':
    Z=(s1-s0).normalized()
    sp=s0+Z*((grip-s0).dot(Z))            # point on string level with grip (nock point)
    d=grip-sp; d=d-Z*d.dot(Z); shoot=d.normalized()   # string -> grip = arrow flight
    Y=-shoot
else:
    pts=np.array([v.co[:] for v in bm.verts]); c=pts.mean(0); _,_,vt=np.linalg.svd(pts-c,full_matrices=False)
    Z=Vector(vt[0].tolist()).normalized()
    if Z.z<0: Z=-Z
    Y=Vector((0,1,0)); Y=(Y-Z*Y.dot(Z)).normalized()
X=Y.cross(Z).normalized(); Y=Z.cross(X)
W=Matrix((X,Y,Z)).transposed().to_4x4(); W.translation=grip
Wi=W.inverted()
bm.transform(Wi)
bm.to_mesh(me); bm.free()
for p in me.polygons: p.use_smooth=True
try: me.free_normals_split()
except Exception: pass
if me.has_custom_normals:
    bpy.context.view_layer.objects.active=ob; bpy.ops.mesh.customdata_custom_splitnormals_clear()
ob.name=CFG['OUTN']; me.name=CFG['OUTN']+'_mesh'; ob.matrix_world=Matrix.Identity(4)
out={'tag':TAG,'rest_world':[list(r) for r in W],'grip_world':list(grip),'bridge':[list(A),list(Bc),rA,rB]}
objs=[ob]
if TAG=='rowan':
    # bowstring: 8-sided tube, 40 segments, local coords
    l0=Wi@s0; l1=Wi@s1; ln=Wi@sp
    sm=bpy.data.materials.new('ROWAN_bowstring'); sm.use_nodes=True
    pr=sm.node_tree.nodes['Principled BSDF']; pr.inputs['Base Color'].default_value=(0.80,0.74,0.60,1); pr.inputs['Roughness'].default_value=0.6
    me.materials.append(sm); si=len(me.materials)-1
    bm=bmesh.new(); bm.from_mesh(me); uvl=bm.loops.layers.uv.active
    NS=40; r=0.0028; Ls=(l1-l0).length; sd=(l1-l0)/Ls; su=Vector((1,0,0)); sw=sd.cross(su).normalized(); su=sw.cross(sd)
    sn=(ln-l0).dot(sd)/Ls
    rings=[]; params=[]
    for k in range(NS+1):
        t=k/NS; p=l0+(l1-l0)*t; params.append(t)
        rings.append([bm.verts.new(p+(su*math.cos(2*math.pi*i/8)+sw*math.sin(2*math.pi*i/8))*r) for i in range(8)])
    sf=[]
    for k in range(NS):
        for i in range(8):
            sf.append(bm.faces.new((rings[k][i],rings[k][(i+1)%8],rings[k+1][(i+1)%8],rings[k+1][i])))
    for f in sf: f.material_index=si; f.smooth=True
    bm.verts.index_update(); bm.to_mesh(me)
    sverts=[[v.index for v in rg] for rg in rings]; bm.free()
    # armature
    ad=bpy.data.armatures.new('ROWAN_bow_rig'); ao=bpy.data.objects.new('ROWAN_bow_rig',ad); bpy.context.scene.collection.objects.link(ao)
    bpy.context.view_layer.objects.active=ao; bpy.ops.object.mode_set(mode='EDIT')
    g=ad.edit_bones.new('bow_grip'); g.head=(0,0,0); g.tail=(0,0,0.12); g.roll=0
    n=ad.edit_bones.new('bow_nock'); n.head=ln; n.tail=ln+Vector((0,0.08,0)); n.parent=g; n.use_connect=False; n.roll=0
    bpy.ops.object.mode_set(mode='OBJECT')
    vg_g=ob.vertex_groups.new(name='bow_grip'); vg_n=ob.vertex_groups.new(name='bow_nock')
    vg_g.add(list(range(len(me.vertices))),1.0,'REPLACE')
    for k,rg in enumerate(sverts):
        t=params[k]; wn=(t/sn) if t<=sn else ((1-t)/(1-sn)); wn=max(0.0,min(1.0,wn))
        vg_n.add(rg,wn,'REPLACE'); vg_g.add(rg,1-wn,'REPLACE')
    ob.parent=ao; mod=ob.modifiers.new('Armature','ARMATURE'); mod.object=ao
    out.update(string_local=[list(l0),list(l1)],nock_local=list(ln),nock_param=sn,draw_axis_local=[0,1,0])
    objs.append(ao)
# hand offset at rest
hb=arm.data.bones[CFG['HAND']]; HM=arm.matrix_world@hb.matrix_local
out['hand_bone']=CFG['HAND']; out['offset_in_hand']=[list(r) for r in (HM.inverted()@W)]
for nm in ('mixamorig:LeftHand','mixamorig:RightHand'):
    out.setdefault('hand_rest',{})[nm]=[list(r) for r in (arm.matrix_world@arm.data.bones[nm].matrix_local)]
out['faces']=len(me.polygons); out['verts']=len(me.vertices)
bpy.data.objects.remove(arm,do_unlink=True)
for o in list(bpy.data.objects):
    if o not in objs: bpy.data.objects.remove(o,do_unlink=True)
bpy.ops.wm.save_as_mainfile(filepath=f'{R}/props/{TAG}_props.blend')
json.dump(out,open(f'{R}/props/{TAG}_props.json','w'),indent=1,default=float)
print('PROPDONE',out['faces'],out['verts'])
