"""Side-on attack author: whole body yaws ~YAW deg left about the hips, legs stay aligned with the pelvis
(knee pole + foot yaw follow pelvis, feet step), analytic 2-bone IK arms holding the staff with the left hand low."""
import bpy, sys, json, math, numpy as np
from mathutils import Matrix, Vector, Quaternion
A=sys.argv[sys.argv.index('--')+1:]
PRM=json.load(open(A[0])); MODE=A[1]  # 'search' | 'apply'
OUTB=A[2] if len(A)>2 else None; OUTF=A[3] if len(A)>3 else None; SAMP=A[4] if len(A)>4 else None; META=A[5] if len(A)>5 else None
P='mixamorig:'; sc=bpy.context.scene
arm=bpy.data.objects['LYRA_tighten_rig']; staff=bpy.data.objects['LYRA_staff']; bolt=bpy.data.objects['LYRA_arcane_bolt_vfx']
ad=arm.animation_data
MUTE0=[t.mute for t in ad.nla_tracks]
for t in ad.nla_tracks: t.mute=True
act=bpy.data.actions['LYRA_tighten_attack']; ad.action=act
pbs=arm.pose.bones; names=[b.name for b in arm.data.bones]
bone_snapshot={b.name:(tuple(b.head_local),tuple(b.tail_local)) for b in arm.data.bones}
sc.frame_set(0); M0={n:pbs[n].matrix.copy() for n in names}
S0=staff.matrix_world.copy(); O=M0[P+'RightHand'].inverted()@S0
sc.frame_set(12); Lrel=staff.matrix_world.inverted()@pbs[P+'LeftHand'].matrix
sc.frame_set(0)
def sm(t): t=min(1,max(0,t)); return t*t*(3-2*t)
def rz(a,c=Vector((0,0,0))): return Matrix.Translation(c)@Matrix.Rotation(math.radians(a),4,'Z')@Matrix.Translation(-c)
def rot_between(a,b):
    a=a.normalized(); b=b.normalized(); return a.rotation_difference(b).to_matrix()
YAW=PRM['yaw']; DROP=PRM['drop']; C=Vector((M0[P+'Hips'].translation.x,M0[P+'Hips'].translation.y,0))
if PRM.get('pivot')=='leftfoot':
    _lt=M0[P+'LeftFoot']@Vector((0,arm.data.bones[P+'LeftFoot'].length,0)); C=Vector((_lt.x,_lt.y,0))
def wbody(f):
    if f<=PRM['hold_end']: return sm(f/PRM['turn_end'])
    return 1-sm((f-PRM['hold_end'])/(30-PRM['ret_pad']-PRM['hold_end']))
def wleft(f):
    a,b,c,d=PRM['left_win']
    if f<=b: return sm((f-a)/(b-a))
    return 1-sm((f-c)/(d-c))
TH=PRM['thrust']  # dict frame->metres along target dir
AIM=PRM.get('aim',0.0)  # target direction in armature space, deg about Z (Unity actor yaw compensation)
dW=Matrix.Rotation(math.radians(AIM),3,'Z')@Vector((0,-1,0))
def body(f,w=None):
    w=wbody(f) if w is None else w
    th=TH.get(str(f),0.0)
    return Matrix.Translation(Vector((0,0,-DROP*w))+dW*(th*PRM['body_follow']))@rz(YAW*w,C)
# staff hold (world, at full turn)
Bh=body(10,1.0)
ax=(dW+Vector((0,0,math.tan(math.radians(PRM['pitch']))))).normalized()
def staff_hold(roll):
    HRl=Vector(PRM['hand_r_local']); HR=Bh@HRl
    z=ax; x0=Vector((0,0,1)).cross(z).normalized(); y0=z.cross(x0)
    R=Matrix((x0,y0,z)).transposed()@Matrix.Rotation(math.radians(roll),3,'Z')
    # right hand head = HR ; staff origin = hand @ O
    Ms=R.to_4x4(); Ms.translation=Vector((0,0,0))
    # find staff translation so that (Ms @ O^-1).translation == HR
    Hm=Ms@O.inverted(); Ms.translation=HR-Hm.translation
    return Ms
def interp(Ma,Mb,w):
    qa=Ma.to_quaternion(); qb=Mb.to_quaternion(); q=qa.slerp(qb,w)
    M=q.to_matrix().to_4x4(); M.translation=Ma.translation.lerp(Mb.translation,w); return M
def ik2(S,W,l1,l2,pole):
    d=W-S; L=d.length; Lc=min(L,l1+l2-1e-4); e=d.normalized()
    a=(l1*l1-l2*l2+Lc*Lc)/(2*Lc); h=math.sqrt(max(0,l1*l1-a*a))
    p=(pole-pole.dot(e)*e).normalized()
    E=S+e*a+p*h; return E, L-Lc, p, e
def basis_from(y_loc,h_loc,u,n):
    A_=Matrix((y_loc,h_loc,y_loc.cross(h_loc))).transposed(); B_=Matrix((u,n,u.cross(n))).transposed()
    return B_@A_.inverted()
def swing_twist_y(Rm):
    q=Rm.to_quaternion(); p=Vector((0,q.y,0)); tw=Quaternion((q.w,0,q.y,0)); 
    if tw.magnitude<1e-9: return 0.0
    tw.normalize(); a=2*math.atan2(tw.y,tw.w); return (a+math.pi)%(2*math.pi)-math.pi
# rest arm data
ARM={}
for s in ('Left','Right'):
    Sa=M0[P+s+'Arm'].translation; Ea=M0[P+s+'ForeArm'].translation; Wa=M0[P+s+'Hand'].translation
    Ra=M0[P+s+'Arm'].to_3x3(); Rf=M0[P+s+'ForeArm'].to_3x3(); Rh=M0[P+s+'Hand'].to_3x3()
    n0=(Ea-Sa).cross(Wa-Ea).normalized(); e0=(Wa-Sa).normalized(); p0=((Ea-Sa)-(Ea-Sa).dot(e0)*e0).normalized()
    ARM[s]=dict(l1=(Ea-Sa).length,l2=(Wa-Ea).length,ha=Ra.inverted()@n0,hf=Rf.inverted()@n0,W0=Rf.inverted()@Rh,p0=p0,Ra=Ra,Rf=Rf)
def solve_arm(s,Sw,Hm,pole,k_tw):
    a=ARM[s]; W=Hm.translation
    E,miss,p,e=ik2(Sw,W,a['l1'],a['l2'],pole)
    n=p.cross(e).normalized()
    # orientation sign: rest n0 == normalize(cross(p0,e0))? enforce
    Ra=basis_from(Vector((0,1,0)),a['ha'],(E-Sw).normalized(),n)
    Rf=basis_from(Vector((0,1,0)),a['hf'],(W-E).normalized(),n)
    Rh=Hm.to_3x3()
    D=Rf.inverted()@Rh@a['W0'].inverted(); tw=swing_twist_y(D)
    Rf=Rf@Matrix.Rotation(k_tw*tw,3,'Y')
    D2=Rf.inverted()@Rh@a['W0'].inverted()
    wr=math.degrees(D2.to_quaternion().angle); wr=wr if wr<=180 else 360-wr
    # elbow height relative to wrist-shoulder line: want pole pointing down-ish
    return dict(E=E,miss=miss,Ra=Ra,Rf=Rf,Rh=Rh,wrist=wr,ftw=math.degrees(k_tw*tw),W=W,S=Sw)
# check sign convention at rest
for s in ('Left','Right'):
    a=ARM[s]; Sa=M0[P+s+'Arm'].translation; Wa=M0[P+s+'Hand'].translation; e0=(Wa-Sa).normalized()
    r=solve_arm(s,Sa,M0[P+s+'Hand'],a['p0'],0.0)
    print('RESTCHK',s,round((r['Ra']-a['Ra']).to_quaternion().magnitude if False else max(abs(x) for row in (r['Ra']-a['Ra']) for x in row),5),round(max(abs(x) for row in (r['Rf']-a['Rf']) for x in row),5),round(r['wrist'],3))
def pole_vec(Bm,ang_deg,S,W):
    # body-local pole: rotate 'down' about shoulder->wrist axis by ang
    e=(W-S).normalized(); dn=Vector((0,0,-1)); dn=(dn-dn.dot(e)*e).normalized()
    return Matrix.Rotation(math.radians(ang_deg),3,e)@dn
# legs rest
LEG={}
for s in ('Left','Right'):
    H=M0[P+s+'UpLeg'].translation; K=M0[P+s+'Leg'].translation; Aa=M0[P+s+'Foot'].translation
    e=(Aa-H).normalized(); kp=(K-H)-(K-H).dot(e)*e
    toe=M0[P+s+'Foot']@Vector((0,arm.data.bones[P+s+'Foot'].length,0))
    LEG[s]=dict(l1=(K-H).length,l2=(Aa-K).length,kp=kp.normalized(),toe0=toe.copy())
def foot_state(s,f):
    """toe position & foot matrix (armature space)."""
    toe0=LEG[s]['toe0']; toe1=rz(YAW,C)@toe0
    sc_=PRM.get('stance_scale',1.0)
    if sc_!=1.0: toe1=C+(toe1-C)*sc_; toe1.z=toe0.z
    steps=PRM['steps'][s]  # [[a,b,dir], ...]  dir +1 = go, -1 = return
    y=YAW*wbody(f)
    # planted position before/after steps
    pos=toe0.copy(); lift=0.0
    st_out,st_back=steps
    if f<=st_out[0]: pos=toe0
    elif f<st_out[1]: u=(f-st_out[0])/(st_out[1]-st_out[0]); pos=toe0.lerp(toe1,sm(u)); lift=(PRM['lift'] if (toe1-toe0).length>0.02 else 0.0)*math.sin(math.pi*u)
    elif f<=st_back[0]: pos=toe1
    elif f<st_back[1]: u=(f-st_back[0])/(st_back[1]-st_back[0]); pos=toe1.lerp(toe0,sm(u)); lift=(PRM['lift'] if (toe1-toe0).length>0.02 else 0.0)*math.sin(math.pi*u)
    else: pos=toe0
    pos=pos+Vector((0,0,lift))
    Fm=Matrix.Translation(pos)@Matrix.Rotation(math.radians(y),4,'Z')@Matrix.Translation(-toe0)@M0[P+s+'Foot']
    return Fm
def pose_frame(f,roll,poleR,poleL,rollL,ktw):
    w=wbody(f); Bm=body(f); y=YAW*w; Ry=Matrix.Rotation(math.radians(y),3,'Z')
    M={}
    M[P+'Hips']=Bm@M0[P+'Hips']
    for n in ('Spine','Spine1','Spine2','LeftShoulder','RightShoulder','LeftUpLeg','RightUpLeg'): M[P+n]=Bm@M0[P+n]
    c=PRM['counter']*w
    Nk=Bm@M0[P+'Neck']; pN=Nk.translation.copy(); Rn=rz(-c*0.4,pN)
    M[P+'Neck']=Rn@Nk; Hd=Rn@Bm@M0[P+'Head']; M[P+'Head']=rz(-c*0.6,Hd.translation.copy())@Hd
    # staff
    ws=wbody(f); Sl0=S0; Slh=Bh.inverted()@staff_hold(roll)
    Sl=interp(Sl0,Slh,ws); th=TH.get(str(f),0.0)
    Sl.translation=Sl.translation+Vector(PRM.get('draw_bump',[0,0,0]))*math.sin(math.pi*ws)
    Sw=Matrix.Translation(dW*th*(1-PRM['body_follow']))@Bm@Sl
    HmR=Sw@O.inverted()
    info={}
    # right arm
    Sa=M[P+'RightShoulder']@M0[P+'RightShoulder'].inverted()@M0[P+'RightArm']
    SR=Sa.translation
    pR=Bm.to_3x3()@ARM['Right']['p0'].slerp(pole_vec(None,poleR,Bm.inverted()@SR,Bm.inverted()@HmR.translation),ws) if False else None
    p_rest=Bm.to_3x3()@ARM['Right']['p0']; p_hold=Bm.to_3x3()@pole_vec(None,poleR,Bm.inverted()@SR,Bm.inverted()@HmR.translation)
    pR=p_rest.slerp(p_hold,ws) if hasattr(p_rest,'slerp') else (p_rest*(1-ws)+p_hold*ws)
    rR=solve_arm('Right',SR,HmR,pR,ktw*ws)
    # left arm
    wl=wleft(f)
    Lg=Sw@Matrix.Rotation(math.radians(rollL),4,'Z')@Lrel
    rad=Lg.translation-(Sw@Vector((0,0,(Sw.inverted()@Lg.translation).z))); rad=rad.normalized() if rad.length>1e-6 else rad
    Lg=Matrix.Translation(rad*PRM.get('approach',0.0)*min(1.0,(1-wl)*PRM.get('ap_k',1.0)))@Lg
    Lr=Bm@M0[P+'LeftHand']
    if wl>0:
        wr_=min(1.0,wl*PRM.get('rot_lead',1.0))
        if PRM.get('staff_space'):
            a_=Sw.inverted()@Lr; b_=Sw.inverted()@Lg; q=a_.to_quaternion().slerp(b_.to_quaternion(),wr_); X_=q.to_matrix().to_4x4(); X_.translation=a_.translation.lerp(b_.translation,wl); HmL=Sw@X_
        else:
            q=Lr.to_quaternion().slerp(Lg.to_quaternion(),wr_); HmL=q.to_matrix().to_4x4(); HmL.translation=Lr.translation.lerp(Lg.translation,wl)
    else: HmL=Lr
    SaL=M[P+'LeftShoulder']@M0[P+'LeftShoulder'].inverted()@M0[P+'LeftArm']; SL=SaL.translation
    p_rest=Bm.to_3x3()@ARM['Left']['p0']; p_hold=Bm.to_3x3()@pole_vec(None,poleL,Bm.inverted()@SL,Bm.inverted()@HmL.translation)
    pL=p_rest*(1-wl)+p_hold*wl
    rL=solve_arm('Left',SL,HmL,pL,ktw*wl)
    for s,r in (('Right',rR),('Left',rL)):
        Ma=r['Ra'].to_4x4(); Ma.translation=r['S']; M[P+s+'Arm']=Ma
        Mf=r['Rf'].to_4x4(); Mf.translation=r['E']; M[P+s+'ForeArm']=Mf
        Mh=r['Rh'].to_4x4(); Mh.translation=r['E']+r['Rf']@Vector((0,ARM[s]['l2'],0)); M[P+s+'Hand']=Mh
        info[s]=dict(wrist=round(r['wrist'],1),ftw=round(r['ftw'],1),miss=round(r['miss'],4),elbowz=round((r['E']-(r['S']+r['W'])/2).z,3))
    # legs
    fwd=Ry@Vector((0,-1,0))
    for s in ('Left','Right'):
        H=M[P+s+'UpLeg'].translation; Fm=foot_state(s,f); Aa=Fm.translation
        kp_rest=(Bm.to_3x3()@LEG[s]['kp']); wp=sm(min(f,30-f)/PRM.get('kp_frames',2.0)); pole=(kp_rest*(1-wp)+fwd*wp) if PRM.get('knee_fwd',1)==3 else fwd if PRM.get('knee_fwd',1)==2 else ((kp_rest*(1-w)+fwd*w) if PRM.get('knee_fwd',1) else kp_rest)
        K,miss,p,e=ik2(H,Aa,LEG[s]['l1'],LEG[s]['l2'],pole)
        Rb=Ry@M0[P+s+'UpLeg'].to_3x3(); Rt=rot_between(Rb@Vector((0,1,0)),K-H)@Rb
        Rb2=Ry@M0[P+s+'Leg'].to_3x3(); Rs=rot_between(Rb2@Vector((0,1,0)),Aa-K if miss<1e-6 else (Aa-H).normalized())@Rb2
        Mt=Rt.to_4x4(); Mt.translation=H; M[P+s+'UpLeg']=Mt
        Ms_=Rs.to_4x4(); Ms_.translation=H+Rt@Vector((0,LEG[s]['l1'],0)); M[P+s+'Leg']=Ms_
        Mft=Fm.to_3x3().to_4x4(); Mft.translation=Ms_.translation+Rs@Vector((0,LEG[s]['l2'],0)); M[P+s+'Foot']=Mft
        info[s+'Leg']=dict(miss=round(miss,4))
    return M,Sw,info
GRID=PRM.get('grid')
if MODE=='search':
    best=[]
    for hx in (-0.30,-0.34,-0.38):
     for hy in (-0.24,-0.30,-0.36):
      for hz in (1.13,1.20,1.27):
       PRM['hand_r_local']=[hx,hy,hz]
       ktw=PRM['ktw']
       for roll in range(0,360,15):
         for poleR in range(-60,91,15):
          ws=[];pen=0
          for fr in (4,6,8,10,12):
            M,Sw,info=pose_frame(fr,roll,poleR,0,0,ktw); ws.append(info['Right']['wrist']); pen+=(100 if info['Right']['elbowz']>0 else 0)+1000*info['Right']['miss']
          best.append((max(ws)+pen,roll,poleR,[hx,hy,hz],ktw,ws))
    best.sort(key=lambda r:r[0]); print('BESTR',best[:6])
    roll,poleR=best[0][1],best[0][2]; PRM['hand_r_local']=best[0][3]; PRM['ktw']=best[0][4]
    bl=[]
    for rollL in range(-180,180,15):
      for poleL in range(-90,91,15):
        ws=[];pen=0
        for fr in (6,8,10,12):
          M,Sw,info=pose_frame(fr,roll,poleR,poleL,rollL,PRM['ktw']); ws.append(info['Left']['wrist']); pen+=(100 if info['Left']['elbowz']>0 else 0)+1000*info['Left']['miss']
        bl.append((max(ws)+pen,rollL,poleL,ws))
    bl.sort(key=lambda r:r[0]); print('BESTL',bl[:5])
    sys.exit(0)
roll,poleR,poleL,rollL=PRM['roll'],PRM['poleR'],PRM['poleL'],PRM['rollL']
FR={}; SW=[]
def leg_roll(M):
    for bn,key in (('LeftUpLeg','lroll_thigh'),('LeftLeg','lroll_shin')):
        a=PRM.get(key,0.0)
        if a:
            m=M[P+bn]; R=m.to_3x3()@Matrix.Rotation(math.radians(a),3,'Y'); t=m.translation.copy(); M[P+bn]=R.to_4x4(); M[P+bn].translation=t
    return M
for f in range(31):
    if f in (0,30): FR[f]=leg_roll({n:M0[n].copy() for n in names}); SW.append(S0.copy()); print('F',f,'rest'); continue
    M,Sw,info=pose_frame(f,roll,poleR,poleL,rollL,PRM['ktw']); FR[f]=leg_roll(M); SW.append(Sw); print('F',f,json.dumps(info))
# write keys: convert pose matrices to basis
def FC(a):
    out=[]
    for Ly in a.layers:
        for st in Ly.strips:
            for cb in st.channelbags: out+=[fc for fc in cb.fcurves]
    return out
cur={}
for fc in FC(act):
    if fc.data_path.startswith('pose.bones["'): cur[(fc.data_path.split('"')[1],fc.data_path.rsplit('.',1)[1],fc.array_index)]=fc
def setk(bn,ch,i,f,v):
    fc=cur[(bn,ch,i)]; kp=[k for k in fc.keyframe_points if abs(k.co[0]-f)<1e-4][0]
    dv=v-kp.co[1]; kp.co[1]=v; kp.handle_left[1]+=dv; kp.handle_right[1]+=dv
maxloc=0
prev={}
for f in range(31):
    M=FR[f]
    for b in arm.data.bones:
        n=b.name
        if b.parent is None: Bas=b.matrix_local.inverted()@M[n]
        else: Bas=(b.parent.matrix_local.inverted()@b.matrix_local).inverted()@M[b.parent.name].inverted()@M[n]
        loc,q,scl=Bas.decompose()
        e=q.to_euler('XYZ',prev[n]) if n in prev else q.to_euler('XYZ'); prev[n]=e
        if f in (0,30):
            sc.frame_set(f)  # exact rest values from current action
        for i in range(3):
            setk(n,'rotation_euler',i,f,e[i]); setk(n,'location',i,f,loc[i]); setk(n,'scale',i,f,1.0)
        if n!=P+'Hips': maxloc=max(maxloc,loc.length)
for fc in cur.values(): fc.update()
print('MAXLOC_nonhips',maxloc)
# bolt
ameta=json.load(open(META)); CRY=Vector(ameta['props']['staff']['crystal_local']); REL=ameta['props']['bolt']['release']; SPD=ameta['props']['bolt']['speed_mps']
sc.frame_set(REL); spawn=staff.matrix_world@CRY
for fc in FC(bolt.animation_data.action):
    if fc.data_path=='location':
        for kk in fc.keyframe_points:
            t=max(0,kk.co[0]-REL); v=spawn[fc.array_index]+dW[fc.array_index]*SPD*t/30.0
            dv=v-kk.co[1]; kk.co[1]=v; kk.handle_left[1]+=dv; kk.handle_right[1]+=dv
        fc.update()
S={'staff':[],'crystal':[],'bolt':[]}; err=0
for f in range(31):
    sc.frame_set(f)
    for n in names: err=max(err,max(abs(x) for r in (pbs[n].matrix-FR[f][n]) for x in r))
    S['staff'].append([list(r) for r in staff.matrix_world]); S['crystal'].append(list(staff.matrix_world@CRY)); S['bolt'].append(list(bolt.matrix_world.translation))
print('POSE_ERR',err)
S['spawn']=list(spawn); S['bones_unchanged']=all(bone_snapshot[b.name]==(tuple(b.head_local),tuple(b.tail_local)) for b in arm.data.bones)
json.dump(S,open(SAMP,'w'))
ameta['props']['bolt']['spawn_world']=list(spawn); json.dump(ameta,open(META,'w'),indent=1)
ad.action=bpy.data.actions['LYRA_tighten_walk']; sc.frame_set(0)
for t,m in zip(ad.nla_tracks,MUTE0): t.mute=m
bpy.ops.wm.save_as_mainfile(filepath=OUTB,compress=True)
common=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
    mesh_smooth_type='FACE', use_mesh_modifiers=False)
anim=dict(bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
body_o=bpy.data.objects['LYRA_tighten_body']
for t in ad.nla_tracks: t.mute=True
bpy.ops.object.select_all(action='DESELECT')
for o in (arm,body_o): o.hide_set(False); o.select_set(True)
bpy.context.view_layer.objects.active=arm
ad.action=act; sc.frame_set(0)
bpy.ops.export_scene.fbx(filepath=OUTF,**anim,**common)
print('AUTHOR OK')
