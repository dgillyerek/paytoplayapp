"""2026-10-08 attacks (Rowan bow draw+release / Lyra staff cast) on the new blender_rig_*_20261008 rigs.
Keys authored from identity rest (pose-bone matrices -> quaternion keys, BEZIER auto-clamped), f0..f30 @30fps,
f0 == f30 == rest. Character faces Blender -Y; strike/projectile goes -Y (top of screen in rear battle cam).
Rig blend is opened and never saved (work copy saved with save_as copy=True into attack folder)."""
import bpy, sys, os, math, json
import numpy as np
from mathutils import Vector, Matrix, Quaternion
sys.path.insert(0,'/workspace/attack_20261007')
import atk_lib as AL
TAG=sys.argv[sys.argv.index('--')+1]
R='/workspace/rig_20261008'; B='/workspace/design/survival-theme-a-fantasy/heroes/anim'
C={'rowan':dict(NAME='ROWAN_nocape',RIGDIR=f'{B}/rowan/blender_rig_nocape_20261008',STAGE=f'{B}/rowan/attack_20261008'),
   'lyra':dict(NAME='LYRA_tighten',RIGDIR=f'{B}/lyra/blender_rig_tighten_20261008',STAGE=f'{B}/lyra/attack_20261008')}[TAG]
NAME=C['NAME']; STAGE=C['STAGE']; os.makedirs(STAGE,exist_ok=True); os.makedirs(f'{STAGE}/work',exist_ok=True)
P='mixamorig:'; NF=30
REAR_ONLY=bool(os.environ.get('REAR_ONLY'))
def EXP(**kw):
    if REAR_ONLY: return
    return bpy.ops.export_scene.fbx(**kw)
bpy.ops.wm.open_mainfile(filepath=f"{C['RIGDIR']}/{NAME}_blenderig.blend")
sc=bpy.context.scene; sc.render.fps=30; sc.frame_start=0; sc.frame_end=NF
arm=bpy.data.objects[f'{NAME}_rig']; body=bpy.data.objects[f'{NAME}_body']
J=json.load(open(f'{R}/props/{TAG}_props.json'))
with bpy.data.libraries.load(f'{R}/props/{TAG}_props.blend') as (s,d): d.objects=list(s.objects)
PO={}
for o in d.objects: sc.collection.objects.link(o); PO[o.name]=o
OFF_R=Matrix(J['offset_in_hand'])
up=bpy.context.view_layer.update
def reset():
    for b in arm.pose.bones: b.rotation_mode='QUATERNION'; b.matrix_basis=Matrix.Identity(4)
    up()
def pb(n): return arm.pose.bones[P+n]
def Mw(n): return arm.matrix_world@pb(n).matrix
def rotw(n,axis,deg):
    b=pb(n); M=b.matrix.copy(); h=M.translation.copy()
    b.matrix=Matrix.Translation(h)@Matrix.Rotation(math.radians(deg),4,Vector(axis))@Matrix.Translation(-h)@M; up()
def aim(b,target):
    M=b.matrix.copy(); h=M.translation.copy(); y=M.col[1].xyz.normalized(); w=(Vector(target)-h).normalized()
    q=y.rotation_difference(w); b.matrix=Matrix.Translation(h)@q.to_matrix().to_4x4()@Matrix.Translation(-h)@M; up()
def ik(side,wrist,pole):
    u=pb(side+'Arm'); l=pb(side+'ForeArm'); S=u.head.copy(); L1=u.length; L2=l.length
    d=Vector(wrist)-S; dist=max(0.05,min(d.length,L1+L2-1e-3)); dr=d.normalized()
    a=(L1*L1-L2*L2+dist*dist)/(2*dist); h=math.sqrt(max(L1*L1-a*a,0))
    pn=Vector(pole)-S; pn=(pn-dr*pn.dot(dr)).normalized(); E=S+dr*a+pn*h
    aim(u,E); aim(l,S+dr*dist)
def set_hand(side,M):
    b=pb(side+'Hand'); N=M.copy(); N.translation=b.head.copy(); b.matrix=N; up()
def place_hand(side,M,pole):
    ik(side,M.translation,pole); set_hand(side,M)
def hand_frame(c,k,F,K,p):
    """bone-space grip centre c / through-fist axis k (perp to bone Y) -> world: bone Y along F, k along K, c at p"""
    b1=Vector((0,1,0)); b2=Vector(k).normalized(); b3=b1.cross(b2)
    w1=Vector(F).normalized(); w2=(Vector(K)-w1*Vector(K).dot(w1)).normalized(); w3=w1.cross(w2)
    Rm=Matrix((w1,w2,w3)).transposed()@Matrix((b1,b2,b3))
    M=Rm.to_4x4(); M.translation=Vector(p)-Rm@Vector(c); return M
S3=Matrix.Diagonal((-1,1,1,1))
OFF_L=S3@OFF_R@S3      # mirrored grip (rig rolls are mirror-built)
cR=OFF_R.translation.copy(); kR=OFF_R.col[2].xyz.copy(); kR.y=0; kR.normalize()
cL=OFF_L.translation.copy(); kL=OFF_L.col[2].xyz.copy(); kL.y=0; kL.normalize()
def T(x,y,z): return Matrix.Translation((x,y,z))
def Wb(g,rot=None):
    M=(rot or Matrix.Identity(3)).to_4x4(); M.translation=Vector(g); return M
def spine(zdeg,xdeg=0.0,head_comp=0.9):
    for n,f in (('Spine',0.4),('Spine1',0.3),('Spine2',0.3)):
        rotw(n,(0,0,1),zdeg*f)
        if xdeg: rotw(n,(1,0,0),xdeg*f)
    for n,f in (('Neck',0.4),('Head',0.6)):
        rotw(n,(0,0,1),-zdeg*head_comp*f)
        if xdeg: rotw(n,(1,0,0),-xdeg*0.8*f)
meta={'name':NAME,'frames':[0,NF],'fps':30,'forward':'-Y (Blender) = top of screen in rear battle cam'}
# ------------------------------------------------------------------ keyposes
if TAG=='rowan':
    LH_AIM_F=Vector((-0.15,-1,0))   # left forearm direction at aim (bow space == world, bow vertical, shoots -Y)
    # constant LH grip offset: hand frame built in bow space
    Hb=hand_frame(cL,kL,LH_AIM_F,(0,0,1),(0,0,0)); OFF_L=Hb.inverted()
    meta['LH_offset_note']='LeftHand grip offset built from mirrored RightHand fist axis, forearm along bow-space (-0.15,-1,0)'
    W_REST=Matrix(J['rest_world'])
    nockL=Vector(J['nock_local']); BRACE=nockL.y
    G_T=(-0.04,-0.36,1.13); G8=(0.03,-0.62,1.50); G13=(0.02,-0.72,1.57)
    ANCH=Vector((0.02,-0.15,1.57))
    FD=Vector((0.40,-1.0,-0.05))
    def rh_on_string(W,D):
        p=W@(nockL+Vector((0,D,0))); return hand_frame(cR,kR,FD,W.col[2].xyz,p)
    def k_transfer():
        spine(-10); W=Wb(G_T)
        place_hand('Right',W@OFF_R.inverted(),(-0.6,0.1,0.9))
        place_hand('Left',W@T(0,0,0.10)@OFF_L.inverted(),(0.6,0.0,0.95))
    def k_aim(g,zs,D,rh_extra=None):
        spine(zs,-3); W=Wb(g)
        place_hand('Left',W@OFF_L.inverted(),(0.55,-0.25,1.25))
        if rh_extra is not None: M=hand_frame(cR,kR,FD,(0,0,1),rh_extra)
        else: M=rh_on_string(W,D)
        place_hand('Right',M,(-0.6,0.55,1.6))
    def k_return():
        spine(-12); W=Wb(G_T)
        place_hand('Left',W@OFF_L.inverted(),(0.6,0.0,0.95))
        place_hand('Right',W@T(0,0,-0.10)@OFF_R.inverted(),(-0.6,0.1,0.9))
    D13=(Vector(G13)+Vector((0,BRACE,0))-ANCH).length
    D13=abs(ANCH.y-(G13[1]+BRACE))
    KEYS=[(0,None),(3,k_transfer),(4,k_transfer),
          (8,lambda:k_aim(G8,-38,0.0)),(13,lambda:k_aim(G13,-45,D13)),(14,lambda:k_aim(G13,-45,D13)),
          (16,lambda:k_aim(G13,-45,0,rh_extra=ANCH+Vector((-0.07,0.09,0.03)))),
          (20,lambda:k_aim(G13,-43,0,rh_extra=ANCH+Vector((-0.09,0.11,0.0)))),
          (22,k_return),(23,k_return),(30,None)]
    meta['keys']={'transfer_RH_to_LH':'f3-f4','anticipation_nock':8,'full_draw':13,'release':14,'follow':20,'transfer_LH_to_RH':'f22-f23','rest':30}
    RELEASE=14
else:
    W_REST=Matrix(J['rest_world']); R0=W_REST.to_3x3()
    def Rx(a): return Matrix.Rotation(math.radians(a),3,'X')
    def k_staff(g,tilt,zs,xs,lh_wrist,lh_pole=(0.6,0.1,1.0)):
        spine(zs,xs); W=Wb(g,Rx(tilt)@R0)
        place_hand('Right',W@OFF_R.inverted(),(-0.6,0.3,1.0))
        if lh_wrist: ik('Left',lh_wrist,lh_pole)
    KEYS=[(0,None),
          (8,lambda:k_staff((-0.42,0.06,1.44),-22,-15,-5,(0.10,-0.34,1.30),(0.5,0.0,1.1))),
          (12,lambda:k_staff((-0.30,-0.40,1.40),27,15,8,(0.32,0.06,1.12))),
          (19,lambda:k_staff((-0.29,-0.43,1.37),30,14,8,(0.33,0.08,1.10))),
          (30,None)]
    meta['keys']={'anticipation':8,'strike_release':12,'follow':19,'rest':30}
    RELEASE=12
# build action
if not arm.animation_data: arm.animation_data_create()
act=bpy.data.actions.new(f'{NAME}_attack'); act.use_fake_user=True; arm.animation_data.action=act
prev=None
for f,fn in KEYS:
    reset()
    if fn: fn()
    snap={b.name:(b.rotation_quaternion.copy(),b.location.copy()) for b in arm.pose.bones}
    for b in arm.pose.bones:
        q,l=snap[b.name]
        if prev is not None and prev[b.name].dot(q)<0: q=-q
        b.rotation_quaternion=q; b.location=l
        b.keyframe_insert('rotation_quaternion',frame=f,group=b.name); b.keyframe_insert('location',frame=f,group=b.name)
        snap[b.name]=(q,l)
    prev={k:v[0] for k,v in snap.items()}
for fc in act.fcurves:
    for k in fc.keyframe_points:
        k.interpolation='BEZIER'; k.handle_left_type='AUTO_CLAMPED'; k.handle_right_type='AUTO_CLAMPED'
    fc.update()
# --------------------------------------------------------------- props per frame
import props as PR
track={}   # name -> {f: (Matrix, visible)}
if TAG=='rowan':
    bow=PO['ROWAN_bow']; brig=PO['ROWAN_bow_rig']
    arrow=PR.arrow('ROWAN_arrow_blue_fletch',(0.36,0.22,0.12),(0.18,0.19,0.22),PR.ROYAL,PR.GOLD)
    trail=PR.trail('ROWAN_arrow_trail_vfx',(0.5,0.7,1.0),0.9,0.012,0.45)
    arrow_fl=arrow.copy(); arrow_fl.name='ROWAN_arrow_blue_fletch_inflight'; sc.collection.objects.link(arrow_fl)
    def sm(t): t=max(0,min(1,t)); return t*t*(3-2*t)
    bowW={}; D={}
    for f in range(NF+1):
        sc.frame_set(f); up()
        if f<=3: W=Mw('RightHand')@OFF_R; hold='RightHand'
        elif f<=22: W=Mw('LeftHand')@OFF_L@T(0,0,-0.10*(1-sm((f-4)/4))); hold='LeftHand'
        else: W=Mw('RightHand')@OFF_R@T(0,0,0.10*(1-sm((f-23)/4))); hold='RightHand'
        bowW[f]=(W,hold)
        if 8<=f<=RELEASE:
            c=Mw('RightHand')@cR; D[f]=max(0.0,(W.inverted()@c).y-BRACE)
        else: D[f]={RELEASE+1:-0.012,RELEASE+2:0.005}.get(f,0.0)
    sc.frame_set(8); up(); W8=bowW[8][0]; A8=Matrix.Translation(W8@(nockL+Vector((0,D[8],0))))@W8.to_3x3().to_4x4()
    off_arrow_RH=Mw('RightHand').inverted()@A8
    arrowM={}
    for f in range(NF+1):
        sc.frame_set(f); up(); W=bowW[f][0]
        if 5<=f<8:
            s=min(1.0,(f-4)/4.0); arrowM[f]=(Mw('RightHand')@off_arrow_RH@Matrix.Scale(s,4),True)
        elif 8<=f<=RELEASE:
            arrowM[f]=(Matrix.Translation(W@(nockL+Vector((0,D[f],0))))@W.to_3x3().to_4x4(),True)
        else: arrowM[f]=(Matrix.Identity(4),False)
    SPAWN=arrowM[RELEASE][0].copy(); fwd=(SPAWN.to_3x3()@Vector((0,-1,0))).normalized()
    flM={f:((Matrix.Translation(fwd*14.0*(f-RELEASE)/30.0)@SPAWN),f>RELEASE) for f in range(NF+1)}
    trM={f:((Matrix.Translation(fwd*14.0*(f-RELEASE)/30.0)@SPAWN),f>RELEASE) for f in range(NF+1)}
    meta['props']={'bow':{'object':'ROWAN_bow (mesh) + ROWAN_bow_rig (bones bow_grip root, bow_nock child)','parent_windows':{'RightHand':'f0-f3 and f23-f30 (f23-f26 hand slides 10cm up the lower limb back to the grip)','LeftHand':'f4-f22 (f4-f7 hand slides 10cm down the upper limb onto the grip)'},
        'offset_RightHand':[list(r) for r in OFF_R],'offset_LeftHand':[list(r) for r in OFF_L],
        'baked_track':'ROWAN_bow_attack.fbx: bow_grip carries the full per-frame character-space transform; bow_nock local +Y = string draw (m)',
        'draw_m':{f:round(D[f],4) for f in range(NF+1)},'world_y_check_release':list(fwd)},
        'arrow':{'object':'ROWAN_arrow_blue_fletch','held':'f5-f7 in RightHand (scale-in), f8-f14 nocked on the string (origin at nock)','offset_RightHand_f8':[list(r) for r in off_arrow_RH]},
        'arrow_inflight':{'release':RELEASE,'speed_mps':14.0,'spawn_world':[list(r) for r in SPAWN],'dir':list(fwd)},
        'trail':{'object':'ROWAN_arrow_trail_vfx','release':RELEASE,'speed_mps':14.0,'note':'same spawn as arrow, trails on +Y'}}
    def apply_props(f):
        W,h=bowW[f]; brig.matrix_world=Matrix.Identity(4)
        g=brig.pose.bones['bow_grip']; g.matrix=W@brig.data.bones['bow_grip'].matrix_local; brig.pose.bones['bow_nock'].location=(0,D[f],0)
        for o,(M,v) in ((arrow,arrowM[f]),(arrow_fl,flM[f]),(trail,trM[f])):
            o.matrix_world=M; o.hide_render=not v; o.hide_viewport=not v
        up()
    PROPOBJ=[bow,brig,arrow,arrow_fl,trail]
else:
    staff=PO['LYRA_staff']
    bolt=PR.build_orb('LYRA_arcane_bolt_vfx',core=(0.85,0.92,1.0),mid=(0.2,0.45,1.0),outer=(0.05,0.2,1.0),ring=PR.GOLD,R=0.13,trailL=0.9,crystal=True)
    zs=[v.co.z for v in staff.data.vertices]; ztop=max(zs)
    # crystal centre = mean of verts in the top 10 cm
    top=[v.co for v in staff.data.vertices if v.co.z>ztop-0.12]; CRY=sum(top,Vector())/len(top)
    stM={}
    for f in range(NF+1):
        sc.frame_set(f); up(); stM[f]=Mw('RightHand')@OFF_R
    SPAWN=Matrix.Translation(stM[RELEASE]@CRY)
    boltM={f:((Matrix.Translation(Vector((0,-1,0))*9.0*(f-RELEASE)/30.0)@SPAWN@Matrix.Scale(min(1.0,0.4+0.6*(f-RELEASE)/3.0),4)),f>=RELEASE) for f in range(NF+1)}
    meta['props']={'staff':{'object':'LYRA_staff','parent':'RightHand f0-f30 (constant offset)','offset_RightHand':[list(r) for r in OFF_R],'crystal_local':list(CRY)},
        'bolt':{'object':'LYRA_arcane_bolt_vfx','release':RELEASE,'speed_mps':9.0,'dir':[0,-1,0],'spawn_world':list(SPAWN.translation),'note':'spawns at staff crystal, grows 40%->100% over 3 frames, travels -Y'}}
    def apply_props(f):
        staff.matrix_world=stM[f]; M,v=boltM[f]; bolt.matrix_world=M; bolt.hide_render=not v; bolt.hide_viewport=not v; up()
    PROPOBJ=[staff,bolt]
meta['release']=RELEASE
if os.environ.get('PREVIEW'):
    from PIL import Image
    reset(); arm.animation_data.action=act; sc.frame_set(0); mn,mx=AL.bbox_world([body]); Hh=mx.z-mn.z; c=(mn+mx)/2
    V={'rear':((c.x,c.y+1.25*Hh,mn.z+1.45*Hh),(c.x,c.y-0.9*Hh,mn.z+0.2*Hh)),'side':((c.x+2.0*Hh,c.y-0.45*Hh,mn.z+0.6*Hh),(c.x,c.y-0.45*Hh,mn.z+0.48*Hh)),
       'q34front':((c.x-1.15*Hh,c.y-1.45*Hh,mn.z+0.8*Hh),(c.x,c.y-0.2*Hh,mn.z+0.48*Hh)),'front':((c.x,c.y-2.0*Hh,mn.z+0.6*Hh),(c.x,c.y,mn.z+0.55*Hh))}
    arm.hide_render=True; cam=AL.setup_scene('WORKBENCH',(300,300)); od=f'{R}/atk/pv_{TAG}'; os.makedirs(od,exist_ok=True)
    FR=[int(x) for x in os.environ['PREVIEW'].split(',')]
    rows=[]
    for f in FR:
        row=[]
        for v in ('rear','side','q34front','front'):
            sc.frame_set(f); up(); apply_props(f)
            if os.environ.get('CLOSE'):
                ch=Mw('Spine2').translation; e={'rear':(1.2,-1.6,0.3),'side':(1.8,-0.3,0.1),'q34front':(-1.3,-1.4,0.2),'front':(0.2,1.9,0.5)}[v]
                AL.aim(cam,tuple(ch+Vector(e)),tuple(ch+Vector((0,-0.3,0))),lens=60)
            else: AL.aim(cam,*V[v])
            p=f'{od}/f{f:02d}_{v}.png'; AL.render(p); row.append(p)
        rows.append(row)
    S=Image.new('RGB',(1200,300*len(rows)))
    for i,row in enumerate(rows):
        for j,p in enumerate(row): S.paste(Image.open(p).convert('RGB'),(300*j,300*i))
    S.save(f'{od}/sheet.png'); print('PV_DONE'); sys.exit(0)
# ------------------------------------------------------------------ export
for im in bpy.data.images:
    if im.source in {'VIEWER','GENERATED'} or im.name in {'Render Result','Viewer Node'}: continue
    if not im.filepath_raw: im.filepath_raw=f'//textures/{im.name}.jpg'
common=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
    mesh_smooth_type='FACE', use_mesh_modifiers=False)
def sel(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.hide_set(False); o.hide_viewport=False; o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
sc.frame_set(0)
sel([arm,body]); fbx=f'{STAGE}/{NAME}_blenderig_attack.fbx'
EXP(filepath=fbx, bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **common)
meta['attack_fbx']=fbx
pcommon=dict(use_selection=True, axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL', add_leaf_bones=False,
    mesh_smooth_type='FACE', path_mode='COPY', embed_textures=True, use_armature_deform_only=False)
if TAG=='rowan':
    # baked bow track
    if not brig.animation_data: brig.animation_data_create()
    ba=bpy.data.actions.new('ROWAN_bow_attack'); ba.use_fake_user=True; brig.animation_data.action=ba
    for b in brig.pose.bones: b.rotation_mode='QUATERNION'
    pq=None
    for f in range(NF+1):
        apply_props(f); g=brig.pose.bones['bow_grip']; q=g.rotation_quaternion.copy()
        if pq is not None and pq.dot(q)<0: g.rotation_quaternion=-q
        pq=g.rotation_quaternion.copy()
        for b in brig.pose.bones:
            b.keyframe_insert('location',frame=f,group=b.name); b.keyframe_insert('rotation_quaternion',frame=f,group=b.name)
    for fc in ba.fcurves:
        for k in fc.keyframe_points: k.interpolation='LINEAR'
    sel([brig,bow]); EXP(filepath=f'{STAGE}/ROWAN_bow_attack.fbx', object_types={'ARMATURE','MESH'}, bake_anim=True, bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **pcommon)
    for o in (arrow,trail):
        M=o.matrix_world.copy(); o.matrix_world=Matrix.Identity(4); o.hide_render=False
        sel([o]); EXP(filepath=f'{STAGE}/{o.name}.fbx', object_types={'MESH'}, bake_anim=False, **pcommon); o.matrix_world=M
    meta['prop_fbx']=[f'{STAGE}/ROWAN_bow_attack.fbx',f'{STAGE}/ROWAN_arrow_blue_fletch.fbx',f'{STAGE}/ROWAN_arrow_trail_vfx.fbx']
    keep=[bow,brig,arrow,trail]
else:
    for o in (staff,bolt):
        M=o.matrix_world.copy(); o.matrix_world=Matrix.Identity(4); o.hide_render=False
        sel([o]); EXP(filepath=f'{STAGE}/{o.name}.fbx', object_types={'MESH'}, bake_anim=False, **pcommon); o.matrix_world=M
    meta['prop_fbx']=[f'{STAGE}/LYRA_staff.fbx',f'{STAGE}/LYRA_arcane_bolt_vfx.fbx']
    keep=[staff,bolt]
# ------------------------------------------------------------------ renders
def bbox_body():
    mn,mx=AL.bbox_world([body]); return mn,mx
reset(); arm.animation_data.action=act; sc.frame_set(0); mn,mx=bbox_body(); Hh=mx.z-mn.z; c=(mn+mx)/2
VIEWS={'rear':((c.x,c.y+1.25*Hh,mn.z+1.45*Hh),(c.x,c.y-0.9*Hh,mn.z+0.2*Hh)) if not REAR_ONLY else ((c.x,c.y+1.55*Hh,mn.z+1.6*Hh),(c.x,c.y-0.73*Hh,mn.z+0.28*Hh)),
       'side':((c.x+2.0*Hh,c.y-0.45*Hh,mn.z+0.6*Hh),(c.x,c.y-0.45*Hh,mn.z+0.48*Hh)),
       'q34front':((c.x-1.15*Hh,c.y-1.45*Hh,mn.z+0.8*Hh),(c.x,c.y-0.2*Hh,mn.z+0.48*Hh))}
arm.hide_render=True
def frame(f): sc.frame_set(f); up(); apply_props(f)
qd=f'{STAGE}/work/frames'; os.makedirs(qd,exist_ok=True)
cam=AL.setup_scene('EEVEE',(720,720)); sc.eevee.taa_render_samples=24
gp=bpy.data.objects.get('ATK_ground')
bpy.ops.mesh.primitive_plane_add(size=12,location=(0,0,mn.z-0.001)); g=bpy.context.object; g.name='ATK_ground'
gm=bpy.data.materials.new('g'); gm.use_nodes=True; gm.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.03,0.032,0.04,1); g.data.materials.append(gm)
SHEETF=[0,4,8,11,13,14,16,18,20,23,26,30] if TAG=='rowan' else [0,4,8,10,12,14,16,19,23,26,30]
for f in SHEETF:
    for v in (('rear','side','q34front') if not REAR_ONLY else ('rear',)):
        frame(f); AL.aim(cam,*VIEWS[v]); AL.render(f'{qd}/s_{v}_f{f:02d}.png')
cam=AL.setup_scene('EEVEE',(540,540)); sc.eevee.taa_render_samples=8
for f in range(NF+1):
    for v in (('rear','q34front') if not REAR_ONLY else ('rear',)):
        frame(f); AL.aim(cam,*VIEWS[v]); AL.render(f'{qd}/m_{v}_{f:03d}.png')
meta['sheet_frames']=SHEETF; meta['views']={k:[list(a),list(b)] for k,(a,b) in VIEWS.items()}
# props blend + work blend
if REAR_ONLY:
    m=json.load(open(f'{STAGE}/work/attack_meta.json')); m['views']['rear']=[list(VIEWS['rear'][0]),list(VIEWS['rear'][1])]; m['rear_cam_note']='rear battle cam re-rendered wider so the arrow/bolt stays in frame'
    json.dump(m,open(f'{STAGE}/work/attack_meta.json','w'),indent=1); print('REAR_DONE'); sys.exit(0)
pb_path=f'{STAGE}/{NAME}_attack_props.blend'
bpy.data.libraries.write(pb_path,set(keep),fake_user=True,compress=True); meta['props_blend']=pb_path
arm.hide_render=False
wb=f'{STAGE}/{NAME}_attack_work.blend'; bpy.ops.wm.save_as_mainfile(filepath=wb,compress=True,copy=True); meta['work_blend']=wb
json.dump(meta,open(f'{STAGE}/work/attack_meta.json','w'),indent=1,default=lambda o:[list(r) for r in o] if isinstance(o,Matrix) else (list(o) if isinstance(o,Vector) else str(o)))
print('ATK_DONE',fbx)
