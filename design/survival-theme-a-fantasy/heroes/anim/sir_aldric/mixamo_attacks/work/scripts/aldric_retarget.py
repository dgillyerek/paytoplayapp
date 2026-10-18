# Sir Aldric battle clips (2026-10-18): retarget onto the Lite Sword And Shield avatar (sword_and_shield_idle.fbx, 41-bone
# mixamorig rig, Armature root, take Scene) so they play through PlayNamedClip on the lite instance like the 17 lite clips.
import bpy, sys, json, math
from mathutils import Vector, Matrix, Quaternion
PRM=json.load(open(sys.argv[sys.argv.index('--')+1]))
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene; sc.render.fps=30
def imp(fp):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=fp); new=[o for o in bpy.data.objects if o not in before]
    return [o for o in new if o.type=='ARMATURE'][0], new
T,tnew=imp(PRM['target_fbx']); T.name='Armature'
tbody=[o for o in tnew if o.type=='MESH']
idle_act=T.animation_data.action
S,snew=imp(PRM['src'])
for o in snew:
    if o.type=='MESH': o.hide_render=True; o.hide_viewport=True
MAP=PRM.get('map') or {b.name:b.name for b in T.data.bones if b.name in S.data.bones}
inv={t:s for s,t in MAP.items()}
names=[b.name for b in T.data.bones]; P='mixamorig:'
TW=T.matrix_world.copy(); TWi=TW.inverted(); TWq=TW.to_quaternion()
# idle pose (battle rest) basis + world, at idle_frame
sc.frame_set(PRM.get('idle_frame',2)); bpy.context.view_layer.update()
IDLE={n:T.pose.bones[n].matrix_basis.copy() for n in names}
IDLEW={n:(TW@T.pose.bones[n].matrix).copy() for n in names}
# bind references in world
REF={}
for t,s in inv.items():
    sb=S.matrix_world@S.data.bones[s].matrix_local; tb=TW@T.data.bones[t].matrix_local
    sq=sb.to_quaternion(); tq=tb.to_quaternion()
    vs=(sb.to_3x3().normalized().col[1]); vt=(tb.to_3x3().normalized().col[1])
    REF[t]=(sq.inverted(), vt.rotation_difference(vs)@tq)
def bw(o,n): return o.matrix_world@o.data.bones[n].head_local
def leglen(o,up,lo,ft): return (bw(o,up)-bw(o,lo)).length+(bw(o,lo)-bw(o,ft)).length
k=PRM.get('scale') or (leglen(T,P+'LeftUpLeg',P+'LeftLeg',P+'LeftFoot')/leglen(S,inv[P+'LeftUpLeg'],inv[P+'LeftLeg'],inv[P+'LeftFoot']))
f0,f1=PRM['frames']
src=[]
for f in range(f0,f1+1):
    sc.frame_set(f); bpy.context.view_layer.update(); SW=S.matrix_world
    Q={t:(SW@S.pose.bones[s].matrix).to_quaternion()@REF[t][0]@REF[t][1] for t,s in inv.items()}
    H=(SW@S.pose.bones[inv[P+'Hips']].head).copy()
    LH=min((SW@S.pose.bones[inv[P+side+'Foot']].head).z for side in ('Left','Right'))
    src.append((Q,H,LH))
# yaw: turn the travel (or the given yaw) onto -Y (his battle forward)
tr=src[-1][1]-src[0][1]; tr.z=0
yaw=PRM.get('yaw_deg')
if yaw is None: yaw=math.degrees(math.atan2(tr.x,-tr.y)) if tr.length>0.3 else 0.0
RY=Quaternion((0,0,1),math.radians(yaw))
ankle_src=min(x[2] for x in src)
ankle_t=(IDLEW[P+'LeftFoot'].translation.z+IDLEW[P+'RightFoot'].translation.z)/2
H0=src[0][1].copy()
seq=[]; travel=[]
for (Q,H,LH) in src:
    d=RY@(H-H0); travel.append([round(d.x,3),round(d.y,3)])
    if PRM.get('cancel_travel',True): d.x=0; d.y=0
    p=Vector((IDLEW[P+'Hips'].translation.x+k*d.x, IDLEW[P+'Hips'].translation.y+k*d.y, ankle_t+k*(H.z-ankle_src)))
    seq.append(({t:RY@q for t,q in Q.items()},p,ankle_t+k*(LH-ankle_src)))
ORD=[]
def rec(b):
    ORD.append(b.name)
    for c in b.children: rec(c)
for b in T.data.bones:
    if b.parent is None: rec(b)
T.animation_data.action=None
def pose_basis(Q,hp):
    for n in names: T.pose.bones[n].matrix_basis=IDLE[n].copy()
    bpy.context.view_layer.update()
    for n in ORD:
        if n not in Q: continue
        pb=T.pose.bones[n]; bpy.context.view_layer.update()
        if n==P+'Hips': pa=TWi@hp
        else: pa=pb.matrix.translation.copy()
        pb.matrix=Matrix.Translation(pa)@(TWq.inverted()@Q[n]).to_matrix().to_4x4()
    bpy.context.view_layer.update()
    return {n:T.pose.bones[n].matrix_basis.copy() for n in names}
clipB=[]
for (Q,hp,want) in seq:
    pose_basis(Q,hp)
    got=min((TW@T.pose.bones[P+side+'Foot'].head).z for side in ('Left','Right'))
    hp=hp+Vector((0,0,want-got)); clipB.append(pose_basis(Q,hp))
def sm(t): t=min(1.0,max(0.0,t)); return t*t*(3-2*t)
def blend(A,B,w):
    out={}
    for n in names:
        la,ra,sa=A[n].decompose(); lb,rb,sb_=B[n].decompose()
        out[n]=Matrix.Translation(la.lerp(lb,w))@ra.slerp(rb,w).to_matrix().to_4x4()@Matrix.Diagonal(sa.lerp(sb_,w).to_4d())
    return out
BI=PRM['blend_in']; BO=PRM['blend_out']
frames=[blend(IDLE,clipB[0],sm(i/BI)) for i in range(BI)]+clipB+[blend(clipB[-1],IDLE,sm(i/BO)) for i in range(1,BO+1)]
N=len(frames)
act=bpy.data.actions.new(PRM['action']); T.animation_data.action=act
for fi,B in enumerate(frames):
    for n in names:
        pb=T.pose.bones[n]; pb.rotation_mode='QUATERNION'; pb.matrix_basis=B[n]
        pb.keyframe_insert('rotation_quaternion',frame=fi); pb.keyframe_insert('location',frame=fi); pb.keyframe_insert('scale',frame=fi)
sc.frame_start=0; sc.frame_end=N-1
meta={'frames':N,'src':PRM['src'].split('/')[-1],'src_frames':[f0,f1],'scale_k':round(k,4),'yaw_deg':round(yaw,2),'blend_in':BI,'blend_out':BO,
      'src_hips_travel_m':round(tr.length,3),'cancel_travel':PRM.get('cancel_travel',True)}
path=[]
for fi in range(N):
    sc.frame_set(fi); bpy.context.view_layer.update()
    path.append({'f':fi,**{s:[round(x,3) for x in (TW@T.pose.bones[P+s].head)] for s in ('Hips','LeftHand','RightHand','LeftFoot','RightFoot')}})
meta['path']=path; meta['src_travel_rotated']=travel
json.dump(meta,open(PRM['meta_out'],'w'),indent=1)
for o in list(snew): bpy.data.objects.remove(o)
for a in list(bpy.data.actions):
    if a is not act: bpy.data.actions.remove(a)
if PRM.get('blend_out_path'): bpy.ops.wm.save_as_mainfile(filepath=PRM['blend_out_path'],compress=True)
if PRM.get('fbx_out'):
    for o in bpy.context.view_layer.objects: o.select_set(False)
    for o in [T]+tbody: o.select_set(True)
    bpy.context.view_layer.objects.active=T; sc.frame_set(0)
    bpy.ops.export_scene.fbx(filepath=PRM['fbx_out'], use_selection=True, object_types={'ARMATURE','MESH'}, add_leaf_bones=False,
        bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, path_mode='COPY', embed_textures=True,
        **PRM.get('export',{}))
print('ARETARGET OK',N,meta['scale_k'],meta['yaw_deg'])
