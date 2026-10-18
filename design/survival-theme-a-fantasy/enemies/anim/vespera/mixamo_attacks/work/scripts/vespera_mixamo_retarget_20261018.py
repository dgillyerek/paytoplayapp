# Vespera Mixamo retarget (2026-10-18): her 16-bone, three-root rig (Spine1, LeftUpLeg, RightUpLeg; no Hips).
# Mixamo animated her own uploaded skeleton; the hips motion sits on the armature object, so the source is read in world space.
import bpy, sys, json, math
from mathutils import Vector, Matrix, Quaternion
PRM=json.load(open(sys.argv[sys.argv.index('--')+1]))
sc=bpy.context.scene; T=bpy.data.objects[PRM['rig']]; body=bpy.data.objects[PRM['body']]; P='mixamorig:'
names=[b.name for b in T.data.bones]; ROOTS=[b.name for b in T.data.bones if b.parent is None]
def sm(t): t=min(1.0,max(0.0,t)); return t*t*(3-2*t)
# rest pose (bind) basis
ad=T.animation_data or T.animation_data_create(); ad.action=bpy.data.actions[PRM['rest_action']]; sc.frame_set(0); bpy.context.view_layer.update()
TW=T.matrix_world.copy(); TWi=TW.inverted()
REST={n:T.pose.bones[n].matrix_basis.copy() for n in names}
RESTW={n:(TW@T.pose.bones[n].matrix).copy() for n in names}
ad.action=None
for pb in T.pose.bones: pb.matrix_basis=Matrix()
# source
before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=PRM['src']); new=[o for o in bpy.data.objects if o not in before]
S=[o for o in new if o.type=='ARMATURE'][0]
for o in new:
    if o.type=='MESH': o.hide_render=True; o.hide_viewport=True
# bind reference: armature-space binds; minimal rotation turning the target bind bone onto the source bind bone
REF={}
for n in names:
    sb=S.data.bones[n].matrix_local; tb=T.data.bones[n].matrix_local
    vs=sb.to_3x3().col[1]; vt=tb.to_3x3().col[1]
    REF[n]=(sb.to_quaternion().inverted(), vt.rotation_difference(vs)@tb.to_quaternion())
f0,f1=PRM['frames']
src=[]
for f in range(f0,f1+1):
    sc.frame_set(f); bpy.context.view_layer.update(); SW=S.matrix_world
    Q={}; H={}
    for n in names:
        m=SW@S.pose.bones[n].matrix; Q[n]=m.to_quaternion()@REF[n][0]@REF[n][1]   # world rotation (source object yaw included)
        if n in ROOTS: H[n]=(SW@S.pose.bones[n].head).copy()
    src.append((Q,H,f))
# anchor: the trailing (right) foot keeps its rest spot, so the left foot steps forward into the clip stance
sc.frame_set(f0); bpy.context.view_layer.update()
rf_clip=S.matrix_world@S.pose.bones[P+'RightFoot'].head; rf_rest=RESTW[P+'RightFoot'].translation
off=Vector((rf_rest.x-rf_clip.x, rf_rest.y-rf_clip.y, 0.0))
# drift: remove the linear horizontal drift of Spine1 across the clip
s_a=src[0][1][P+'Spine1']; s_b=src[-1][1][P+'Spine1']; drift=Vector((s_b.x-s_a.x,s_b.y-s_a.y,0))
# time warp: wind-up (clip frames f0..windup_end) stretched by 'windup_scale'
we=PRM.get('windup_end',f0); ws=PRM.get('windup_scale',1.0)
def src_at(t):
    i=int(math.floor(t)); a=min(max(i-f0,0),len(src)-1); b=min(a+1,len(src)-1); u=t-math.floor(t)
    Qa,Ha,_=src[a]; Qb,Hb,_=src[b]
    return ({n:Qa[n].slerp(Qb[n],u) for n in names},{n:Ha[n].lerp(Hb[n],u) for n in ROOTS})
clip_t=[]; t=float(f0)
wl=(we-f0)*ws
k=0
while True:
    if k<=wl: tt=f0+k/ws
    else: tt=we+(k-wl)
    if tt>f1+1e-6: break
    clip_t.append(tt); k+=1
seq=[]
for tt in clip_t:
    Q,H=src_at(tt); fr=(tt-f0)/max(1,(f1-f0))
    H={n:H[n]+off-drift*fr for n in ROOTS}; seq.append((Q,H,tt))
# write: set world matrices root->leaf for each clip frame, read back the basis
def order():
    out=[]
    def rec(b):
        out.append(b.name)
        for c in b.children: rec(c)
    for r in T.data.bones:
        if r.parent is None: rec(r)
    return out
ORD=order()
def pose_basis(Q,H):
    for n in ORD:
        pb=T.pose.bones[n]; bpy.context.view_layer.update()
        head=H[n] if n in ROOTS else (TW@pb.matrix).translation
        pb.matrix=TWi@(Matrix.Translation(head)@Q[n].to_matrix().to_4x4())
    bpy.context.view_layer.update()
    return {n:T.pose.bones[n].matrix_basis.copy() for n in names}
clipB=[pose_basis(Q,H) for (Q,H,tt) in seq]
BI=PRM['blend_in']; BO=PRM['blend_out']
def blend(A,B,w):
    out={}
    for n in names:
        la,ra,_=A[n].decompose(); lb,rb,_=B[n].decompose()
        out[n]=Matrix.Translation(la.lerp(lb,w))@ra.slerp(rb,w).to_matrix().to_4x4()
    return out
frames=[('in',i/BI,blend(REST,clipB[0],sm(i/BI)),None) for i in range(BI)]
frames+=[('clip',0,b,seq[i][2]) for i,b in enumerate(clipB)]
frames+=[('out',i/BO,blend(clipB[-1],REST,sm(i/BO)),None) for i in range(1,BO+1)]
N=len(frames)
act=bpy.data.actions.new(PRM['action']); ad.action=act
for fi,(kind,w,B,tt) in enumerate(frames):
    for n in names:
        pb=T.pose.bones[n]; pb.rotation_mode='QUATERNION'; pb.matrix_basis=B[n]
        pb.keyframe_insert('rotation_quaternion',frame=fi); pb.keyframe_insert('location',frame=fi)
sc.frame_start=0; sc.frame_end=N-1; sc.render.fps=30
# feet: world foot positions at clip ends, then IK steps during the blends (left first in, right first out)
def footW(fi,side):
    sc.frame_set(fi); bpy.context.view_layer.update(); return (TW@T.pose.bones[P+side+'Foot'].matrix).translation.copy()
F={s:(RESTW[P+s+'Foot'].translation.copy(),footW(BI,s),footW(BI+len(clipB)-1,s)) for s in ('Left','Right')}
LIFT=PRM.get('lift',0.07)
emp={}
for s in ('Left','Right'):
    e=bpy.data.objects.new('IK_'+s,None); sc.collection.objects.link(e); emp[s]=e
    c=T.pose.bones[P+s+'Leg'].constraints.new('IK'); c.target=e; c.chain_count=2; c.use_tail=True
    for fi,(kind,w,B,tt) in enumerate(frames):
        r,a,b=F[s]
        if kind=='in':
            lo,hi=(0.0,0.6) if s=='Left' else (0.4,1.0); u=sm(min(1,max(0,(w-lo)/(hi-lo)))); p=r.lerp(a,u)+Vector((0,0,LIFT*math.sin(math.pi*u)*((a-r).length>0.03)))
            infl=1.0
        elif kind=='out':
            lo,hi=(0.0,0.6) if s=='Right' else (0.4,1.0); u=sm(min(1,max(0,(w-lo)/(hi-lo)))); p=b.lerp(r,u)+Vector((0,0,LIFT*math.sin(math.pi*u)*((b-r).length>0.03)))
            infl=1.0
        else:
            p=Vector((0,0,0)); infl=0.0
        e.location=p; e.keyframe_insert('location',frame=fi); c.influence=infl; c.keyframe_insert('influence',frame=fi)
for o in bpy.context.view_layer.objects: o.select_set(False)
T.select_set(True); bpy.context.view_layer.objects.active=T
bpy.ops.object.mode_set(mode='POSE'); bpy.ops.pose.select_all(action='SELECT')
bpy.ops.nla.bake(frame_start=0,frame_end=N-1,only_selected=False,visual_keying=True,clear_constraints=True,use_current_action=True,bake_types={'POSE'})
bpy.ops.object.mode_set(mode='OBJECT')
for e in emp.values(): bpy.data.objects.remove(e)
# QC + events
meta={'frames':N,'blend_in':BI,'blend_out':BO,'src':PRM['src'].split('/')[-1],'src_frames':[f0,f1],'windup_end':we,'windup_scale':ws,
      'anchor_offset':[round(x,3) for x in off],'drift_removed':[round(x,3) for x in drift],'clip_out_start':BI,
      'out_of_src':{str(f):next(i for i,(k_,w_,B_,tt_) in enumerate(frames) if k_=='clip' and tt_>=f-1e-6) for f in PRM.get('events',[])}}
path=[]
for fi in range(0,N,2):
    sc.frame_set(fi); bpy.context.view_layer.update()
    path.append({'f':fi,**{s:[round(x,3) for x in (TW@T.pose.bones[P+s].head)] for s in ('LeftFoot','RightFoot','LeftHand','RightHand','Spine1')}})
meta['path']=path
json.dump(meta,open(PRM['meta_out'],'w'),indent=1)
if PRM.get('blend_out_path'): bpy.ops.wm.save_as_mainfile(filepath=PRM['blend_out_path'],compress=True)
if PRM.get('fbx_out'):
    for o in list(bpy.data.objects):
        if o in new: bpy.data.objects.remove(o)
    base=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
        apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, mesh_smooth_type='FACE', use_armature_deform_only=False)
    for o in bpy.context.view_layer.objects: o.select_set(False)
    for o in (T,body): o.select_set(True)
    bpy.context.view_layer.objects.active=T; sc.frame_set(0)
    bpy.ops.export_scene.fbx(filepath=PRM['fbx_out'], use_mesh_modifiers=True, bake_anim=True, bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **base)
print('VRETARGET OK',N,meta['out_of_src'])
