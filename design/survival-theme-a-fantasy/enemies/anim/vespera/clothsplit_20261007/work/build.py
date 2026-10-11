"""Cloth split build: body/cloth separation, hole fill, cloth chains + weights, body leg-bleed cleanup,
walk/attack actions (existing motion, body bones only), export rest/walk/attack FBX + blend."""
import bpy, bmesh, sys, os, json, math
import numpy as np
from mathutils import Vector, Matrix
sys.path.insert(0,'/workspace/attack_20261007/clothsplit')
BASE='/workspace/design/survival-theme-a-fantasy'
cfg=json.load(open(sys.argv[sys.argv.index('--')+1]))
NAME=cfg['name']; OUT=f"{BASE}/{cfg['anim_dir']}/clothsplit_20261007"; os.makedirs(OUT,exist_ok=True); os.makedirs(OUT+'/work',exist_ok=True)
RIG=f"{BASE}/{cfg['rig_blend']}"; ATK=f"{BASE}/{cfg['attack_blend']}"
P='mixamorig:'; LEGB=[P+n for n in ('LeftUpLeg','LeftLeg','LeftFoot','LeftToeBase','RightUpLeg','RightLeg','RightFoot','RightToeBase')]
bpy.ops.wm.open_mainfile(filepath=RIG)
for o in list(bpy.data.objects):
    if o.type in {'CAMERA','LIGHT'}: bpy.data.objects.remove(o,do_unlink=True)
arm=bpy.data.objects[f'{NAME}_rig']; body=bpy.data.objects[f'{NAME}_body']
walk=bpy.data.actions[f'{NAME}_walk']
arm.animation_data.action=None
for pb in arm.pose.bones: pb.matrix_basis.identity()
bpy.context.view_layer.update()
body_bone_order=[b.name for b in arm.data.bones]
assert len(body_bone_order)==22
with bpy.data.libraries.load(ATK) as (src,dst): dst.actions=[f'{NAME}_attack']
attack=bpy.data.actions[f'{NAME}_attack']; attack.use_fake_user=True; walk.use_fake_user=True
fc=np.load(f"fc_{NAME}.npy"); legsj=json.load(open(f'legs_{NAME}.json'))
zcut=legsj['zcut']; z0=legsj['z0']; H=legsj['H']
crotch=max(l['z_hi'] for l in legsj['legs'])*H+z0
assert body.matrix_world==Matrix.Identity(4) or True
MW=body.matrix_world.copy()
# ---------- split ----------
cloth=body.copy(); cloth.data=body.data.copy(); cloth.name=f'{NAME}_cloth'; cloth.data.name=f'{NAME}_cloth'
bpy.context.scene.collection.objects.link(cloth)
def cut(obj,delmask,tag):
    bm=bmesh.new(); bm.from_mesh(obj.data); bm.faces.ensure_lookup_table()
    n0=len(bm.faces)
    bmesh.ops.delete(bm,geom=[bm.faces[i] for i in np.where(delmask)[0]],context='FACES')
    loose=[v for v in bm.verts if not v.link_faces]; bmesh.ops.delete(bm,geom=loose,context='VERTS')
    import cs_lib as L
    nb=sum(1 for e in bm.edges if e.is_boundary)
    fst=L.fill_cycles(bm,bm.loops.layers.uv.active)
    nf=fst['fill_tris']
    bmesh.ops.recalc_face_normals(bm,faces=bm.faces[:])
    left=sum(1 for e in bm.edges if e.is_boundary)
    st={'faces_before':n0,'faces_deleted':int(delmask.sum()),'boundary_edges_after_cut':nb,'fill':fst,'boundary_edges_after_fill':left,
        'faces':len(bm.faces),'verts':len(bm.verts)}
    bm.to_mesh(obj.data); obj.data.update(); bm.free()
    print('CUT',tag,st); return st
st_body=cut(body,fc,'body'); st_cloth=cut(cloth,~fc,'cloth')
# ---------- body weights: leg-bleed cleanup + seam blend ----------
def vg_arrays(obj):
    names=[g.name for g in obj.vertex_groups]; W=np.zeros((len(obj.data.vertices),len(names)),np.float32)
    for v in obj.data.vertices:
        for g in v.groups: W[v.index,g.group]=g.weight
    return names,W
def write_weights(obj,names,W):
    for g in list(obj.vertex_groups): obj.vertex_groups.remove(g)
    groups=[obj.vertex_groups.new(name=n) for n in names]
    for j,g in enumerate(groups):
        idx=np.where(W[:,j]>1e-4)[0]
        for i in idx: g.add([int(i)],float(W[i,j]),'REPLACE')
names,W=vg_arrays(body); co=np.array([MW@v.co for v in body.data.vertices])
legcols=[names.index(n) for n in LEGB if n in names]; hip=names.index(P+'Hips'); spn=names.index(P+'Spine')
# leg tube membership from the classification polygons (verts near leg loop polygons below crotch)
import cs_lib as L
inleg=np.zeros(len(co),bool)
for ch in legsj['chains']:
    zs=np.array([c[0] for c in ch])
    for k,(z,pts) in enumerate(ch):
        pts=np.array(pts); lo=z-0.005 if k>0 else -1e9; hi=z+0.005 if k<len(ch)-1 else crotch+0.02
        m=(co[:,2]>=lo)&(co[:,2]<hi)
        if not m.any(): continue
        if k==0:
            cx,cy=pts.mean(0); ins=(np.abs(co[m,0]-cx)<0.10)&(co[m,1]>cy-0.24)&(co[m,1]<cy+0.12)
        else:
            ins=L.point_in_poly(co[m,0],co[m,1],pts)|(L.dist_to_poly(co[m,0],co[m,1],pts)<0.02)
        idx=np.where(m)[0]; inleg[idx[ins]]=True
legw_before=W[:,legcols].sum(1)
torso=co[:,2]>crotch+0.03
band=(~inleg)&(co[:,2]<=crotch+0.03)
fix=(torso|band)&(legw_before>0)
W[fix,hip]+=legw_before[fix]; W[np.ix_(fix,legcols)]=0
# seam blend: non-leg body verts within 0.10 m above zcut -> blend to Spine (cloth roots use Spine)
seam=(~inleg)&(co[:,2]<zcut+0.10)&(co[:,2]>zcut-0.03)
a=np.clip(1-(co[seam,2]-zcut)/0.10,0,1)[:,None]
Ws=W[seam]*(1-a); Ws[:,spn]+=a[:,0]; W[seam]=Ws
W/=np.maximum(W.sum(1,keepdims=True),1e-8)
write_weights(body,names,W)
bodyq={'leg_tube_verts':int(inleg.sum()),'verts_leg_weight_moved_to_Hips':int(fix.sum()),'seam_verts_blended_to_Spine':int(seam.sum()),
       'crotch_z':float(crotch),'zcut':float(zcut),
       'torso_verts_with_leg_weight_after':int(((W[:,legcols].sum(1)>0.01)&(co[:,2]>crotch+0.03)).sum())}
print('BODYW',bodyq)
# ---------- cloth chains ----------
cco=np.array([MW@v.co for v in cloth.data.vertices])
seamring=bpy.data.objects[f'{NAME}_body']
# centre at zcut: mean of leg chain top centroids
tops=[np.array(ch[-1][1]).mean(0) for ch in legsj['chains']]; C=np.mean(tops,0)
ang=np.degrees(np.arctan2(cco[:,0]-C[0],-(cco[:,1]-C[1])))%360   # 0 = front (-Y), 90 = character-left (+X)
SECT=[('F',0),('FL',45),('L',90),('BL',135),('B',180),('BR',225),('R',270),('FR',315)]
pref=cfg['prefix']   # sector -> name prefix, e.g. {"F":"skirt_F",...}
chains=[]
for s,a0 in SECT:
    if s not in pref: continue
    d=np.abs((ang-a0+180)%360-180); m=d<22.5
    if m.sum()<max(150,0.02*len(cco)): continue
    zt=min(cco[m,2].max(),zcut); zb=cco[m,2].min()
    nb=4 if (zt-zb)>0.75 else (3 if (zt-zb)>0.30 else 2)
    zsj=np.linspace(zt,zb+0.03,nb+1)
    pts=[]
    for z in zsj:
        mm=m&(np.abs(cco[:,2]-z)<0.05)
        if mm.sum()<5: mm=m&(np.abs(cco[:,2]-z)<0.12)
        p=np.median(cco[mm],0) if mm.sum() else np.array([C[0],C[1],z]); p[2]=z; pts.append(p)
    chains.append({'sector':s,'angle':a0,'prefix':pref[s],'joints':[list(map(float,p)) for p in pts],'nbones':nb,'nverts':int(m.sum())})
print('CHAINS',[(c['prefix'],c['nbones'],c['nverts']) for c in chains])
PARENT=cfg.get('chain_parent',P+'Spine')
bpy.context.view_layer.objects.active=arm; arm.select_set(True)
bpy.ops.object.mode_set(mode='EDIT')
eb=arm.data.edit_bones; par=eb[PARENT]
for c in chains:
    prev=None; c['bones']=[]
    for k in range(c['nbones']):
        b=eb.new(f"{c['prefix']}_{k+1:02d}"); b.head=Vector(c['joints'][k]); b.tail=Vector(c['joints'][k+1])
        out=Vector((b.head.x-C[0],b.head.y-C[1],0)).normalized()
        b.align_roll(out); b.parent=prev or par; b.use_connect=bool(prev); b.use_deform=True
        prev=b; c['bones'].append(b.name)
bpy.ops.object.mode_set(mode='OBJECT')
assert [b.name for b in arm.data.bones if b.name in body_bone_order]==body_bone_order
# cloth weights
cnames=[P+n for n in []]
allb=[b for c in chains for b in c['bones']]; cn=[PARENT]+allb; CW=np.zeros((len(cco),len(cn)),np.float32)
ca=np.array([c['angle'] for c in chains],float)
for i in range(len(cco)):
    d=(ca-ang[i]+180)%360-180; o=np.argsort(np.abs(d))[:2]
    if len(o)==1: ws=[(o[0],1.0)]
    else:
        d0,d1=abs(d[o[0]]),abs(d[o[1]]); t=d0/(d0+d1+1e-9); ws=[(o[0],1-t),(o[1],t)]
    for ci,wa in ws:
        c=chains[ci]; J=np.array(c['joints']); z=cco[i,2]; zt=J[0,2]; Lb=(J[0,2]-J[-1,2])/c['nbones']
        u=np.clip((zt-z)/max(Lb,1e-3),0,c['nbones']-1e-6)       # bone-space param
        k=int(u); f=u-k
        wb=np.zeros(c['nbones']); wb[k]=1.0
        if f>0.75 and k+1<c['nbones']: tt=(f-0.75)/0.5; wb[k]=1-tt; wb[k+1]=tt
        if f<0.25 and k>0: tt=(0.25-f)/0.5; wb[k]=1-tt; wb[k-1]=tt
        if k==0 and f<0.35:   # root ring blends to the chain parent
            pw=1-f/0.35; CW[i,0]+=wa*pw; wb*= (1-pw)
        for kk in range(c['nbones']):
            if wb[kk]>0: CW[i,1+allb.index(c['bones'][kk])]+=wa*wb[kk]
CW/=np.maximum(CW.sum(1,keepdims=True),1e-8)
# limit to 4 influences
for i in range(len(CW)):
    r=CW[i]; nz=np.argsort(r)[::-1]; r[nz[4:]]=0; CW[i]=r/r.sum()
write_weights(cloth,cn,CW)
for m in cloth.modifiers:
    if m.type=='ARMATURE': m.object=arm
# ---------- actions: existing motion, body bones only ----------
def body_only(act):
    bad=[fcu for fcu in act.fcurves if fcu.data_path.startswith('pose.bones["') and fcu.data_path.split('"')[1] not in body_bone_order]
    for fcu in bad: act.fcurves.remove(fcu)
    return len(bad)
rm=body_only(walk)+body_only(attack)
# ---------- export ----------
for o in bpy.data.objects: o.hide_set(False); o.hide_viewport=False
def sel():
    bpy.ops.object.select_all(action='DESELECT')
    for o in (arm,body,cloth): o.select_set(True)
    bpy.context.view_layer.objects.active=arm
hum=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
    mesh_smooth_type='FACE', use_mesh_modifiers=False)
reb=dict(use_selection=True, object_types={'ARMATURE','MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, path_mode='COPY', embed_textures=True,
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL')
for im in bpy.data.images:
    if im.source in {'VIEWER','GENERATED'} or im.name in {'Render Result','Viewer Node'}: continue
    if im.packed_file is None:
        try: im.pack()
        except Exception: pass
sc=bpy.context.scene; sc.render.fps=30
arm.animation_data.action=None
for pb in arm.pose.bones: pb.matrix_basis.identity()
sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/{NAME}_clothsplit.fbx', bake_anim=False, **hum)
def prep_modes(act):
    modes={}
    for fcu in act.fcurves:
        if fcu.data_path.startswith('pose.bones["'):
            bn=fcu.data_path.split('"')[1]
            if fcu.data_path.endswith('rotation_euler'): modes[bn]='XYZ'
            elif fcu.data_path.endswith('rotation_quaternion'): modes[bn]='QUATERNION'
    for pb in arm.pose.bones:
        pb.rotation_mode=modes.get(pb.name,'QUATERNION'); pb.location=(0,0,0); pb.rotation_quaternion=(1,0,0,0); pb.rotation_euler=(0,0,0); pb.scale=(1,1,1)
    return modes
for act,tag in ((walk,'walk'),(attack,'attack')):
    arm.animation_data.action=None; prep_modes(act)
    arm.animation_data.action=act; sc.frame_start=0; sc.frame_end=30
    sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/{NAME}_clothsplit_{tag}.fbx', **reb)
arm.animation_data.action=None; prep_modes(walk); arm.animation_data.action=walk; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=f'{OUT}/{NAME}_clothsplit.blend', compress=True)
meta={'name':NAME,'rig_blend':RIG,'attack_blend':ATK,'zcut':zcut,'crotch_z':crotch,'body_cut':st_body,'cloth_cut':st_cloth,'body_weights':bodyq,
      'chains':[{k:c[k] for k in ('prefix','sector','nbones','nverts','bones','joints')} | {'parent':PARENT} for c in chains],
      'fcurves_removed_non_body':rm,'blender':bpy.app.version_string}
json.dump(meta,open(f'{OUT}/work/build_meta.json','w'),indent=1,default=float)
print('BUILD DONE',NAME)
