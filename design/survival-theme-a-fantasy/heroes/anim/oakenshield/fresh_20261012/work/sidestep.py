import bpy, math, json
from mathutils import Vector, Matrix, Quaternion
D='/workspace/paytoplayapp/design/survival-theme-a-fantasy/heroes/anim/oakenshield/fresh_20261012'
OUT='/workspace/battle_scene/oak_step'
bpy.ops.wm.open_mainfile(filepath=D+'/OAKENSHIELD_fresh.blend')
sc=bpy.context.scene; sc.render.fps=30; sc.frame_start=0; sc.frame_end=30
arm=bpy.data.objects['OAKENSHIELD_rig']; body=bpy.data.objects['OAKENSHIELD_body']; PB=arm.pose.bones; P_='mixamorig:'
for pb in PB: pb.rotation_mode='QUATERNION'
def upd(): bpy.context.view_layer.update()
def reset():
    for pb in PB: pb.location=(0,0,0); pb.rotation_quaternion=(1,0,0,0); pb.scale=(1,1,1)
    upd()
def rot(n,axis,deg):
    pb=PB[P_+n]; R=pb.bone.matrix_local.to_3x3()
    q=Quaternion(Vector(axis),math.radians(deg)); pb.rotation_quaternion=(R.inverted()@q.to_matrix()@R).to_quaternion()@pb.rotation_quaternion
def aim(n,d,twist=0):
    upd(); pb=PB[P_+n]; pb.rotation_quaternion=(1,0,0,0); upd()
    M=pb.matrix.copy(); y=M.to_3x3().col[1].normalized(); q=y.rotation_difference(Vector(d).normalized())
    R=q.to_matrix()@M.to_3x3()
    pb.matrix=Matrix.Translation(M.to_translation())@R.to_4x4(); upd()
def head_forward():
    upd(); pb=PB[P_+'Head']; pb.rotation_quaternion=(1,0,0,0); upd()
    M=pb.matrix.copy(); R0=pb.bone.matrix_local.to_3x3()
    pb.matrix=Matrix.Translation(M.to_translation())@R0.to_4x4(); upd()
def ease(t): t=max(0,min(1,t)); return t*t*(3-2*t)
def nl(a,b,t): return Vector(a).lerp(Vector(b),t).normalized()
ARM_DOWN=20
def rest_arms():
    rot('LeftArm',(0,1,0),ARM_DOWN); rot('RightArm',(0,1,0),-ARM_DOWN)
    rot('LeftForeArm',(1,0,0),-8); rot('RightForeArm',(1,0,0),-8)
old=bpy.data.actions['OAKENSHIELD_attack']
# reference: original f10 RightHand armature rotation (spear points forward), and original rest foot matrices
arm.animation_data.action=old; sc.frame_set(10); upd()
Rref=PB[P_+'RightHand'].matrix.to_3x3().to_quaternion()
sc.frame_set(0); upd()
FOOT0={s:PB[P_+s+'Foot'].matrix.copy() for s in ('Left','Right')}
reset(); rest_arms(); upd()
RD={n:(PB[P_+n].matrix.to_3x3().col[1]).normalized() for n in ('RightArm','RightForeArm','LeftArm','LeftForeArm')}
KNEE0={}
for s_ in ('Left','Right'):
    hip=PB[P_+s_+'UpLeg'].matrix.to_translation(); kn=PB[P_+s_+'Leg'].matrix.to_translation(); an=PB[P_+s_+'Foot'].matrix.to_translation()
    dn=(an-hip).normalized(); pv=(kn-hip)-dn*(kn-hip).dot(dn); KNEE0[s_]=pv.normalized() if pv.length>1e-6 else Vector((0,-1,0))
LEN={s:((PB[P_+s+'Leg'].matrix.to_translation()-PB[P_+s+'UpLeg'].matrix.to_translation()).length,(PB[P_+s+'Foot'].matrix.to_translation()-PB[P_+s+'Leg'].matrix.to_translation()).length) for s in ('Left','Right')}
a=bpy.data.actions.new('OAKENSHIELD_attack_sidestep'); a.use_fake_user=True; arm.animation_data.action=a
# her right = -X, forward = -Y. yaw + = to her left. Side-on = turn to her right (left shoulder to the target).
# frame, hips yaw, spine yaw (extra), lean, hips offset (x,y,z), R upper, R fore, L upper, L fore
K=[(0, 0,0,0,(0,0,0), None,None,None,None),
   (5, -40,-6,0,(0,0.02,-0.03), (-0.55,-0.45,-0.70),(-0.25,-0.92,0.30),None,None),
   (10,-62,-18,-4,(0.02,-0.04,-0.07), (-0.45,0.60,0.62),(-0.12,0.15,0.98),(0.20,-0.95,0.20),(0.20,-0.97,0.10)),
   (14,-8,14,10,(0.04,-0.15,-0.08), (-0.30,-0.70,0.65),(-0.12,-0.97,0.20),(0.55,0.15,-0.80),(0.45,-0.30,-0.85)),
   (20,-4,10,8,(0.03,-0.12,-0.06), (-0.10,-0.72,-0.68),(0.20,-0.80,-0.56),(0.60,0.10,-0.80),(0.40,-0.20,-0.90)),
   (30,0,0,0,(0,0,0), None,None,None,None)]
K=[tuple(list(k[:5])+[Vector(k[5+i]) if k[5+i] else RD[n] for i,n in enumerate(('RightArm','RightForeArm','LeftArm','LeftForeArm'))]) for k in K]
def interp(f):
    i=max(j for j in range(len(K)) if K[j][0]<=f); j=min(i+1,len(K)-1)
    f0,f1=K[i][0],K[j][0]; t=ease((f-f0)/(f1-f0)) if f1>f0 else 0
    L=lambda x,y: x+(y-x)*t
    return (L(K[i][1],K[j][1]),L(K[i][2],K[j][2]),L(K[i][3],K[j][3]),Vector(K[i][4]).lerp(Vector(K[j][4]),t),
            [nl(K[i][5+q],K[j][5+q],t) for q in range(4)])
# left foot step: lift f2, plant f8 forward; hold; lift f21, back down f28
LSTEP=Vector((0.0,-0.34,0.0)); LYAW=-25
def left_foot(f):
    if f<=2: u=0; lift=0
    elif f<=8: s=(f-2)/6; u=ease(s); lift=0.09*math.sin(math.pi*s)
    elif f<=21: u=1; lift=0
    elif f<=28: s=(f-21)/7; u=1-ease(s); lift=0.07*math.sin(math.pi*s)
    else: u=0; lift=0
    return u,lift
def foot_target(side,f,hyaw):
    M0=FOOT0[side]; p0=M0.to_translation(); R0=M0.to_3x3()
    if side=='Left':
        u,lift=left_foot(f)
        R=Quaternion((0,0,1),math.radians(LYAW*u)).to_matrix()@R0
        p=p0+LSTEP*u+Vector((0,0,lift))
        return p,R
    # back foot pivots on the ball: ball stays put, ankle orbits it (no slide at the contact)
    yaw=math.radians(0.55*hyaw)
    ball=Vector((p0.x,p0.y-0.11,p0.z))
    q=Quaternion((0,0,1),yaw)
    p=ball+q@(p0-ball)
    return p,q.to_matrix()@R0
def leg_ik(side,ankle,Rfoot,fyaw=0.0):
    up=PB[P_+side+'UpLeg']; upd()
    hip=up.matrix.to_translation(); l1,l2=LEN[side]
    d=ankle-hip; dist=min(d.length,l1+l2-1e-4)
    dn=d.normalized()
    a1=math.acos(max(-1,min(1,(l1*l1+dist*dist-l2*l2)/(2*l1*dist))))
    pole=Quaternion((0,0,1),fyaw)@KNEE0[side]
    pole=pole-dn*pole.dot(dn)
    if pole.length<1e-6: pole=Vector((0,-1,0))
    pole.normalize()
    knee=hip+dn*(l1*math.cos(a1))+pole*(l1*math.sin(a1))
    aim(side+'UpLeg',knee-hip); aim(side+'Leg',ankle-knee)
    fb=PB[P_+side+'Foot']; upd(); M=fb.matrix.copy()
    fb.matrix=Matrix.Translation(M.to_translation())@Rfoot.to_4x4(); upd()
    return knee
log=[]
for f in range(31):
    hyaw,syaw,lean,hoff,dirs=interp(f)
    reset()
    hp=PB[P_+'Hips']; upd(); M=hp.matrix.copy()
    R=Quaternion((0,0,1),math.radians(hyaw)).to_matrix()@M.to_3x3()
    hp.matrix=Matrix.Translation(M.to_translation()+hoff)@R.to_4x4(); upd()
    for b,w in (('Spine',0.3),('Spine1',0.35),('Spine2',0.35)): rot(b,(0,0,1),syaw*w); rot(b,(1,0,0),lean*w)
    rot('Neck',(0,0,1),-(hyaw+syaw)*0.5)
    upd()
    for n_,dv in zip(('RightArm','RightForeArm','LeftArm','LeftForeArm'),dirs): aim(n_,dv)
    for side in ('Left','Right'):
        p,Rf=foot_target(side,f,hyaw)
        fy=math.radians(LYAW*left_foot(f)[0]) if side=='Left' else math.radians(0.55*hyaw)
        leg_ik(side,p,Rf,fy)
    head_forward()
    # spear hand: hold the original f10 armature rotation f8-14 so the spear points at the target
    w=ease((f-5)/3) if f<8 else (1.0 if f<=14 else 1-ease((f-14)/4))
    if 5<=f<19:
        pb=PB[P_+'RightHand']; upd(); M=pb.matrix.copy()
        q=M.to_3x3().to_quaternion().slerp(Rref,w); pb.matrix=Matrix.Translation(M.to_translation())@q.to_matrix().to_4x4(); upd()
    upd()
    for pb in PB:
        pb.keyframe_insert('rotation_quaternion',frame=f); pb.keyframe_insert('location',frame=f)
    lf=PB[P_+'LeftFoot'].matrix.to_translation(); rf=PB[P_+'RightFoot'].matrix.to_translation()
    log.append(dict(f=f,lf=list(lf),rf=list(rf)))
# QC on the keyed action
qc=[]
for f in range(31):
    sc.frame_set(f); upd()
    row={'f':f}
    for n in ('LeftFoot','RightFoot','Head','RightHand','Hips','Spine2'):
        m=PB[P_+n].matrix; row[n]=[list(m.to_translation()),list(m.to_quaternion())]
    row['RightHandY']=list(PB[P_+'RightHand'].matrix.to_3x3().col[1].normalized())
    qc.append(row)
json.dump(dict(qc=qc,Rref=list(Rref)),open(OUT+'/qc.json','w'))
# export attack FBX with Design's export settings
old.name='OAKENSHIELD_attack_fresh_20261012'; a.name='OAKENSHIELD_attack'
for o in bpy.context.view_layer.objects: o.select_set(False)
arm.select_set(True); body.select_set(True); bpy.context.view_layer.objects.active=arm
base=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, mesh_smooth_type='FACE', use_armature_deform_only=False)
sc.frame_start=0; sc.frame_end=30
bpy.ops.export_scene.fbx(filepath=OUT+'/OAKENSHIELD_blenderig_attack.fbx', use_mesh_modifiers=True, bake_anim=True, bake_anim_use_all_bones=True,
    bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **base)
arm.animation_data.action=bpy.data.actions['OAKENSHIELD_rest']; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/OAKENSHIELD_fresh_sidestep.blend',compress=True)
print('STEP ok')
