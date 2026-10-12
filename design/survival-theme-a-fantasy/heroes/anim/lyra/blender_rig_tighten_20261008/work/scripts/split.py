import bpy, bmesh, sys, os, json, numpy as np
from mathutils import Vector
TAG=sys.argv[sys.argv.index('--')+1]
D=f'/workspace/rig_20261008/{TAG}'
PROPNAME={'rowan':'ROWAN_bow','lyra':'LYRA_staff'}[TAG]
PROPFACES={'rowan':16000,'lyra':14000}[TAG]
bpy.ops.wm.open_mainfile(filepath=f'{D}/merged.blend')
IMG=[im for im in bpy.data.images if im.colorspace_settings.name=='sRGB' and im.size[0]>0][0]
PX=np.empty(IMG.size[0]*IMG.size[1]*4,np.float32); IMG.pixels.foreach_get(PX); PX=PX.reshape(IMG.size[1],IMG.size[0],4)
def texcol(uv):
    x=int((uv[0]%1.0)*(IMG.size[0]-1)); y=int((uv[1]%1.0)*(IMG.size[1]-1)); return PX[y,x,:3]
me=bpy.data.objects['RAW']; me.name=f'{TAG.upper()}_body'
W=np.load(f'{D}/mask_weapon.npy'); S=np.load(f'{D}/mask_string.npy') if os.path.exists(f'{D}/mask_string.npy') else np.zeros_like(W)
n=len(me.data.vertices); assert len(W)==n
tri=np.empty(len(me.data.polygons)*3,np.int32); me.data.polygons.foreach_get('vertices',tri); tri=tri.reshape(-1,3)
wc=W[tri].sum(1); sc_=S[tri].sum(1)
prop_f=wc>=2; del_f=prop_f&(sc_>=2)
print('prop faces',prop_f.sum(),'string faces deleted',del_f.sum())
bm=bmesh.new(); bm.from_mesh(me.data); bm.faces.ensure_lookup_table(); bm.verts.ensure_lookup_table()
uvl=bm.loops.layers.uv.active
# delete string faces
bmesh.ops.delete(bm, geom=[bm.faces[i] for i in np.where(del_f)[0]], context='FACES_ONLY')
bm.faces.ensure_lookup_table()
# split prop faces off: tag via face index map (indices shifted after delete) -> use a custom int layer set before delete
bm.free()
# redo with layer approach
bm=bmesh.new(); bm.from_mesh(me.data); bm.faces.ensure_lookup_table()
lay=bm.faces.layers.int.new('part')
for i,f in enumerate(bm.faces): f[lay]=2 if del_f[i] else (1 if prop_f[i] else 0)
bmesh.ops.delete(bm, geom=[f for f in bm.faces if f[lay]==2], context='FACES_ONLY')
# split edges between parts
edges=[e for e in bm.edges if len(e.link_faces)==2 and e.link_faces[0][lay]!=e.link_faces[1][lay]]
bmesh.ops.split_edges(bm, edges=edges)
bm.to_mesh(me.data); bm.free()
# separate by part attribute
bpy.context.view_layer.objects.active=me; me.select_set(True)
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='DESELECT'); bpy.ops.object.mode_set(mode='OBJECT')
part=np.empty(len(me.data.polygons),np.int32); me.data.attributes['part'].data.foreach_get('value',part)
sel=(part==1); me.data.polygons.foreach_set('select',sel)
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.separate(type='SELECTED'); bpy.ops.object.mode_set(mode='OBJECT')
prop=[o for o in bpy.data.objects if o.type=='MESH' and o!=me][0]; prop.name=PROPNAME
for o in (me,prop):
    if 'part' in o.data.attributes: o.data.attributes.remove(o.data.attributes['part'])
def clean_and_patch(o, min_island=150):
    bm=bmesh.new(); bm.from_mesh(o.data); bm.faces.ensure_lookup_table()
    uv=bm.loops.layers.uv.active
    # drop loose verts
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    # islands
    seen=set(); islands=[]
    for f in bm.faces:
        if f.index in seen: continue
        stack=[f]; isl=[]; seen.add(f.index)
        while stack:
            g=stack.pop(); isl.append(g)
            for e in g.edges:
                for h in e.link_faces:
                    if h.index not in seen: seen.add(h.index); stack.append(h)
        islands.append(isl)
    small=[f for isl in islands if len(isl)<min_island for f in isl]
    print(o.name,'islands',len(islands),'sizes',sorted([len(i) for i in islands])[-5:],'removing small faces',len(small))
    bmesh.ops.delete(bm, geom=small, context='FACES')
    bmesh.ops.delete(bm, geom=[v for v in bm.verts if not v.link_faces], context='VERTS')
    # holes: walk boundary loops, fan-fill each with a centre vertex; cap UV = one texel from the rim
    bm.edges.ensure_lookup_table()
    used=set(); loops=[]
    for e in bm.edges:
        if not e.is_boundary or e.index in used: continue
        L=e.link_loops[0]; start=L; seq=[]; guard=0
        while True:
            seq.append(L); used.add(L.edge.index)
            x=L.link_loop_next
            while not x.edge.is_boundary:
                x=x.link_loop_radial_next.link_loop_next; guard+=1
                if guard>10**6: break
            L=x
            if L==start or L.edge.index in used or guard>10**6: break
        loops.append(seq)
    print(o.name,'boundary loops',len(loops),'sizes',sorted(len(q) for q in loops)[-10:])
    caps=[]
    for seq in loops:
        if len(seq)<3: continue
        vs=[l.vert for l in seq]
        cen=sum((v.co for v in vs),Vector())/len(vs)
        cols=np.array([texcol(l[uv].uv) for l in seq]); med=np.median(cols,0)
        texel=seq[int(np.argmin(((cols-med)**2).sum(1)))][uv].uv.copy()
        c=bm.verts.new(cen)
        for l in seq:
            a=l.vert; b=l.link_loop_next.vert
            try:
                f=bm.faces.new((b,a,c))
            except ValueError:
                continue
            for fl in f.loops: fl[uv].uv=texel
            f.smooth=True; f.material_index=l.face.material_index
        caps.append((len(seq),cen.copy()))
    bm.normal_update()
    print(o.name,'holes filled',len(caps))
    rem=sum(1 for e in bm.edges if e.is_boundary); print(o.name,'boundary edges after',rem)
    bm.to_mesh(o.data); bm.free()
    return [(c[0],list(c[1])) for c in caps]
for o in (me,prop):
    bpy.context.view_layer.objects.active=o
    try: bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception as ex: print('cn clear',ex)
caps_b=clean_and_patch(me, 1500); caps_p=clean_and_patch(prop, 150)
# recenter x (neck centre + leg midpoint) and ground z
co=np.empty(len(me.data.vertices)*3,np.float32); me.data.vertices.foreach_get('co',co); co=co.reshape(-1,3)
z0,z1=co[:,2].min(),co[:,2].max(); H=z1-z0
nk=co[co[:,2]>z0+0.975*H]
neck_x=0.5*(nk[:,0].min()+nk[:,0].max())
kn=co[(co[:,2]>z0+0.27*H)&(co[:,2]<z0+0.29*H)]
xs=np.sort(kn[:,0]); gaps=np.diff(xs); mid=len(xs)//4+np.argmax(gaps[len(xs)//4:3*len(xs)//4])
L=kn[kn[:,0]>xs[mid]]; R=kn[kn[:,0]<=xs[mid]]
legmid=0.5*(0.5*(L[:,0].min()+L[:,0].max())+0.5*(R[:,0].min()+R[:,0].max()))
cx=0.5*(neck_x+legmid); dz=-z0
print(f'neck_x {neck_x:.4f} legmid {legmid:.4f} -> shift x {-cx:.4f}, z {dz:.4f}, H {H:.4f}')
for o in (me,prop):
    o.location=(-cx,0,dz)
    bpy.ops.object.select_all(action='DESELECT'); o.select_set(True); bpy.context.view_layer.objects.active=o
    bpy.ops.object.transform_apply(location=True,rotation=True,scale=True)
# decimate prop
nf=len(prop.data.polygons)
if nf>PROPFACES*1.1:
    m=prop.modifiers.new('Dec','DECIMATE'); m.ratio=PROPFACES/nf
    bpy.context.view_layer.objects.active=prop; bpy.ops.object.modifier_apply(modifier='Dec')
print('prop faces final',len(prop.data.polygons),'body faces',len(me.data.polygons))
json.dump(dict(shift=[-float(cx),0.0,float(dz)],H=float(H),neck_x=float(neck_x),legmid=float(legmid),caps_body=caps_b,caps_prop=caps_p),open(f'{D}/split_meta.json','w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=f'{D}/split.blend')
# export body-only GLB for hum_pipeline
bpy.ops.object.select_all(action='DESELECT'); me.select_set(True); bpy.context.view_layer.objects.active=me
bpy.ops.export_scene.gltf(filepath=f'{D}/{TAG}_body_split.glb', use_selection=True, export_format='GLB', export_image_format='AUTO')
print('DONE')
