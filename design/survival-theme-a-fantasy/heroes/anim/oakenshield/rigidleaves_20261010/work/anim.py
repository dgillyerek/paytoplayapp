"""Oakenshield rigid-leaves rest/walk/attack authoring (2026-10-10) on OAKENSHIELD_rig (refit 22-bone rig).
World-space targets -> per-bone basis, keyed every frame f0..f30 @30fps (local XYZ euler; Hips also location).
Walk (in place, loop f30==f0): body squared to the walk direction (bind pose has pelvis turned 19 deg and chest 28 deg),
two-bone leg IK to treadmill feet, heel-strike -> flat -> toe-off feet pivoting on measured heel/ball points, feet and
knees pointing forward, ~12 cm stance, close arm swing, slight head nod/turn.
Attack (Thorn Spear throw, release f14, starts/ends at the bind pose): feet planted (leg IK), spear conjured in the right
hand f3-8 while the arm lifts, cocked behind the right shoulder at f10 with the left arm aiming forward, chest drives through
and the right arm whips up and over to release at f14 (spear kept pointing forward in the hand), follow-through low across
by f20, back to bind by f30."""
import bpy, math, json, sys
import numpy as np
from mathutils import Matrix, Vector, Quaternion
OUTBLEND, OUTJSON = sys.argv[sys.argv.index('--')+1:][:2]
sc=bpy.context.scene; sc.render.fps=30
arm=bpy.data.objects['OAKENSHIELD_rig']; body=bpy.data.objects['OAKENSHIELD_body']
P='mixamorig:'
B={b.name[len(P):]:b for b in arm.data.bones}
REST={n:b.matrix_local.copy() for n,b in B.items()}
HEAD={n:b.head_local.copy() for n,b in B.items()}; TAIL={n:b.tail_local.copy() for n,b in B.items()}
order=[b.name[len(P):] for b in arm.data.bones]
PARENT={n:(B[n].parent.name[len(P):] if B[n].parent else None) for n in B}
def R3(m): return m.to_3x3().normalized()
def rot_between(a,b): return a.normalized().rotation_difference(b.normalized()).to_matrix()
def Rax(axis,deg): return Matrix.Rotation(math.radians(deg),3,Vector(axis))
X=Vector((1,0,0)); Y=Vector((0,1,0)); Z=Vector((0,0,1)); FWD=Vector((0,-1,0))
NF=30
def cyc(f,f0,period=NF): return math.cos(2*math.pi*(f-f0)/period)
def smooth(t): t=max(0.0,min(1.0,t)); return t*t*(3-2*t)
def ik2(a,t,l1,l2,pole):
    d=t-a; dist=min(d.length,(l1+l2)*0.9995); dn=d.normalized()
    x=(l1*l1-l2*l2+dist*dist)/(2*dist); h=math.sqrt(max(l1*l1-x*x,0.0))
    pv=(pole-dn*pole.dot(dn)).normalized()
    return a+dn*x+pv*h, a+dn*dist
def L_(a,b): return (HEAD[b]-HEAD[a]).length
LEN={s:(L_(s+'UpLeg',s+'Leg'),L_(s+'Leg',s+'Foot')) for s in ('Left','Right')}
LENA={s:(L_(s+'Arm',s+'ForeArm'),L_(s+'ForeArm',s+'Hand')) for s in ('Left','Right')}
def yaw_of(v): return math.degrees(math.atan2(v.y,v.x))
# ---------- measured rest geometry ----------
co=np.array([v.co[:] for v in body.data.vertices])
vg={g.name:g.index for g in body.vertex_groups}
W=np.zeros((len(co),len(vg)),dtype=np.float32)
for v in body.data.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
FOOT={}
FDIR={'Left':(-0.34,-0.94,0.0),'Right':(-0.17,-0.98,0.0)}
for s in ('Left','Right'):
    fm=(W[:,vg[P+s+'Foot']]+W[:,vg[P+s+'ToeBase']])>0.5
    fv=co[fm]; ank=HEAD[s+'Foot']
    tip0=Vector(fv[fv[:,1].argmin()]); d0=Vector((tip0.x-ank.x,tip0.y-ank.y,0)).normalized()
    along=(fv[:,0]-ank.x)*d0.x+(fv[:,1]-ank.y)*d0.y
    tip=Vector(fv[along.argmax()])
    front=fv[along>0.06]; zmin=front[:,2].min(); blk=front[front[:,2]<zmin+0.006]
    ball=Vector(blk.mean(0)); ball.z=float(zmin)
    back=fv[(along<0.0)&(fv[:,2]<0.16)]; heel=Vector(back[back[:,2].argmin()])
    fdir=Vector((ball.x-heel.x,ball.y-heel.y,0)).normalized()
    yfix=yaw_of(FWD)-yaw_of(fdir)
    # Oakenshield: claw feet fan out, so take the measured foot direction (centre of the toe-claw fan, 2026-10-10)
    fdir=Vector(FDIR[s]).normalized(); yfix=yaw_of(FWD)-yaw_of(fdir)
    a2=Vector((ank.x,ank.y,0))
    def lowest_near(pt,r=0.03):
        d=np.linalg.norm(fv[:,:2]-np.array(pt[:2]),axis=1); m=d<r
        return float(fv[m][:,2].min()) if m.any() else 0.0
    ball=a2+fdir*0.10; ball.z=lowest_near(ball)
    heel=a2-fdir*0.05; heel.z=lowest_near(heel,0.04)
    FOOT[s]=dict(ball=ball,heel=heel,tip=tip,zmin=float(co[fm][:,2].min()),fdir=fdir,yfix=yfix,rel=(fv-np.array(ank[:]))[::3])
# pelvis / chest / face yaw in the bind pose
hipv=HEAD['LeftUpLeg']-HEAD['RightUpLeg']; YAW_H=-yaw_of(Vector((hipv.x,hipv.y,0)))
shv=HEAD['LeftArm']-HEAD['RightArm']; YAW_C=-yaw_of(Vector((shv.x,shv.y,0)))
fm=(co[:,2]>1.62)&(co[:,2]<1.72)&(np.abs(co[:,0]-HEAD['Head'].x)<0.07)
nose=Vector(co[fm][co[fm][:,1].argmin()]); hc=Vector((HEAD['Head'].x,HEAD['Head'].y,nose.z))
YAW_F=yaw_of(FWD)-yaw_of(Vector((nose.x-hc.x,nose.y-hc.y,0)))
GEOM=dict(YAW_H=YAW_H,YAW_C=YAW_C,YAW_F=YAW_F,feet={s:{k:(list(v) if isinstance(v,Vector) else v) for k,v in d.items() if k!='rel'} for s,d in FOOT.items()})
print('GEOM',json.dumps(GEOM,default=float))
# ---------- pose assembly ----------
class Pose:
    def __init__(s): s.D={}
    def place(s,n,Rw,head=None):
        p=PARENT[n]
        if head is None: head=(s.D[p]@REST[p].inverted()@REST[n]).translation if p else HEAD[n]
        m=Rw.to_4x4(); m.translation=head; s.D[n]=m; return m
    def joint(s,n):   # where bone n's head sits given its (already placed) parent
        p=PARENT[n]; return (s.D[p]@REST[p].inverted()@REST[n]).translation
def spine_chain(pose,hips_pos,yaws,leans,rolls):
    pose.place('Hips',Rax(Z,yaws[0])@Rax(X,leans[0])@Rax(Y,rolls[0])@R3(REST['Hips']),head=hips_pos)
    for i,n in enumerate(('Spine','Spine1','Spine2','Neck','Head'),1):
        pose.place(n,Rax(Z,yaws[i])@Rax(X,leans[i])@Rax(Y,rolls[i])@R3(REST[n]))
    dC=R3(pose.D['Spine2'])@R3(REST['Spine2']).inverted()
    for s in ('Left','Right'): pose.place(s+'Shoulder',dC@R3(REST[s+'Shoulder']))
def leg(pose,s,ank,Rf,pole):
    hip=pose.joint(s+'UpLeg')
    knee,end=ik2(hip,ank,*LEN[s],pole)
    dU=rot_between(R3(REST[s+'UpLeg'])@Y,knee-hip); pose.place(s+'UpLeg',dU@R3(REST[s+'UpLeg']))
    dL=rot_between(dU@(R3(REST[s+'Leg'])@Y),end-knee)@dU; pose.place(s+'Leg',dL@R3(REST[s+'Leg']))
    pose.place(s+'Foot',Rf@R3(REST[s+'Foot'])); pose.place(s+'ToeBase',Rf@R3(REST[s+'ToeBase']))
    return (ank-hip).length/sum(LEN[s]), (end-ank).length
def arm_dirs(pose,s,d_up,d_fore,hand_extra=None):
    dU=rot_between(R3(REST[s+'Arm'])@Y,d_up); pose.place(s+'Arm',dU@R3(REST[s+'Arm']))
    dF=rot_between(dU@(R3(REST[s+'ForeArm'])@Y),d_fore)@dU; pose.place(s+'ForeArm',dF@R3(REST[s+'ForeArm']))
    Rh=dF@R3(REST[s+'Hand'])
    if hand_extra is not None: Rh=hand_extra@Rh
    pose.place(s+'Hand',Rh)
    return math.degrees((dF@R3(REST[s+'Hand'])@Y).angle(Rh@Y)) if hand_extra is not None else 0.0
def arm_ik(pose,s,wrist,pole,hand_extra=None):
    sh=pose.joint(s+'Arm'); el,end=ik2(sh,wrist,*LENA[s],pole)
    return arm_dirs(pose,s,el-sh,end-el,hand_extra), (end-wrist).length
def key_action(name,frames,loop):
    ad=arm.animation_data or arm.animation_data_create()
    if name in bpy.data.actions: bpy.data.actions.remove(bpy.data.actions[name])
    act=bpy.data.actions.new(name); act.use_fake_user=True; ad.action=act
    for pb in arm.pose.bones: pb.rotation_mode='XYZ'
    prevE={}; first={}
    for f,D in enumerate(frames):
        for n in order:
            p=PARENT[n]
            bm=(REST[n].inverted()@D[n]) if p is None else (REST[n].inverted()@REST[p]@D[p].inverted()@D[n])
            loc,rot,scl=bm.decompose()
            e=rot.to_euler('XYZ',prevE[n]) if n in prevE else rot.to_euler('XYZ')
            if loop and f==len(frames)-1: e=first[n]
            pb=arm.pose.bones[P+n]
            pb.location=loc if (n=='Hips' or not B[n].use_connect) else Vector((0,0,0))
            pb.rotation_euler=e; pb.scale=(1,1,1)
            for path in ('location','rotation_euler','scale'): pb.keyframe_insert(path,frame=f,group=pb.name)
            prevE[n]=e.copy()
            if f==0: first[n]=e.copy()
    return act
REPORT={'geom':GEOM}
ARM_OUT=float(__import__('os').environ.get('ARM_OUT','12'))
FORE_OUT=float(__import__('os').environ.get('FORE_OUT','7'))
# ======================= WALK =======================
CONTACT={'Right':8.0,'Left':23.0}; STANCE=18.0; SWING=NF-STANCE
HIPS0=HEAD['Hips'].copy()
X_ANK={'Left':HIPS0.x+0.062,'Right':HIPS0.x-0.062}
Y_FRONT=HIPS0.y-0.185; Y_BACK=HIPS0.y+0.205
P_HEEL=-14.0; P_TOE=26.0; LIFT=0.075; CLEAR=0.002
for s in FOOT:
    fd=FOOT[s]; Ry=Rax(Z,fd['yfix']); ank=HEAD[s+'Foot']
    fd['o_ball']=Ry@(fd['ball']-ank); fd['o_heel']=Ry@(fd['heel']-ank); fd['o_tip']=Ry@(fd['tip']-ank)
    fd['ank_flat_z']=ank.z-fd['ball'].z+CLEAR
def foot_state(s,f):
    fd=FOOT[s]; ph=(f-CONTACT[s])%NF
    if ph<STANCE:
        u=ph/STANCE; gy=Y_FRONT+(Y_BACK-Y_FRONT)*u
        if u<0.16: pitch=P_HEEL*(1-smooth(u/0.16))
        elif u<0.60: pitch=0.0
        else: pitch=P_TOE*smooth((u-0.60)/0.40)**1.3
        lift=0.0
    else:
        u=(ph-STANCE)/SWING; v=(Y_BACK-Y_FRONT)/STANCE*SWING
        h00=2*u**3-3*u**2+1; h10=u**3-2*u**2+u; h01=-2*u**3+3*u**2; h11=u**3-u**2
        gy=h00*Y_BACK+h01*Y_FRONT+(h10+h11)*v*0.6
        pitch=P_TOE*(1-smooth(u/0.55))+P_HEEL*smooth((u-0.45)/0.55)
        lift=LIFT*math.sin(math.pi*u)**1.2
    R=Rax(X,pitch)@Rax(Z,fd['yfix'])
    G=Vector((X_ANK[s],gy,fd['ank_flat_z']))
    if pitch>=0:
        o=fd['o_ball']; piv=Vector((G.x+o.x,G.y+o.y,fd['ball'].z*0+CLEAR+lift))
        ank=piv+Rax(X,pitch)@(-o)
    else:
        o=fd['o_heel']; k=pitch/P_HEEL
        hz=(G.z+o.z)*(1-k)+CLEAR*k+lift
        piv=Vector((G.x+o.x,G.y+o.y,hz)); ank=piv+Rax(X,pitch)@(-o)
    # whole-sole floor clamp: the platform heel tip / toe never goes below the floor
    M=np.array(R); zs=(fd['rel']@M.T)[:,2]+ank.z
    if zs.min()<CLEAR: ank=ank+Vector((0,0,CLEAR-float(zs.min())))
    return ank,R,pitch,ph<STANCE
def walk_body(f):
    bob=0.010*math.cos(2*math.pi*(f-0.5)/15.0)
    return dict(dz=-0.028+bob,dx=0.014*cyc(f,2.0),yawP=-4.5*cyc(f,23.0),rollP=-2.0*cyc(f,2.0),yawC=4.0*cyc(f,23.0),
                nod=1.6*math.cos(2*math.pi*(f-10.0)/15.0),turn=-1.8*cyc(f,23.0),tilt=0.8*cyc(f,2.0))
def walk_pose(f,extra=0.0):
    bp=walk_body(f); pose=Pose()
    hp=HIPS0+Vector((bp['dx'],0,bp['dz']+extra))
    ch=YAW_C*0.8; hd=YAW_F*0.45   # shoulders nearly square, face keeps a little of her bind turn (neck twist kept small)
    yb=[YAW_H, YAW_H+0.33*(ch-YAW_H), YAW_H+0.66*(ch-YAW_H), ch, 0.5*(ch+hd), hd]
    yw=[yb[0]+bp['yawP'], yb[1]+0.4*bp['yawP'], yb[2]+0.5*bp['yawC'], yb[3]+bp['yawC'], yb[4]+0.4*bp['yawC'], yb[5]+bp['turn']]
    ln=[0.0,1.0,1.5,2.0,1.0+0.5*bp['nod'],bp['nod']]
    rl=[bp['rollP'],0.5*bp['rollP'],0.0,-0.3*bp['rollP'],0.0,bp['tilt']]
    spine_chain(pose,hp,yw,ln,rl)
    info={}
    for s in ('Left','Right'):
        ank,Rf,pitch,stance=foot_state(s,f)
        pole=(FWD+Z*0.12).normalized()
        info[s]=leg(pose,s,ank,Rf,pole)+(stance,)
    # arms: close to the body, swing opposite the legs (left arm forward when right foot forward at f8)
    for s,sg,f0 in (('Left',1,8.0),('Right',-1,23.0)):
        phi=2.0+16.0*cyc(f,f0); a=math.radians(ARM_OUT); ph=math.radians(phi)
        d=Vector((sg*math.sin(a),-math.sin(ph)*math.cos(a),-math.cos(ph)*math.cos(a)))
        fw=(phi+14.0)/32.0; bend=math.radians(10.0+22.0*fw)
        perp=(FWD-d*FWD.dot(d)).normalized(); fd=(d*math.cos(bend)+perp*math.sin(bend)+X*sg*math.tan(math.radians(FORE_OUT))).normalized()
        arm_dirs(pose,s,d,fd)
    return pose,info
extra=[0.0]*NF
for it in range(3):
    need=[]
    for f in range(NF):
        pose,info=walk_pose(f,extra[f]); r=max(info[s][0] for s in ('Left','Right'))
        need.append(extra[f]-max(0.0,(r-0.985))*sum(LEN['Left']))
    k=[0.25,0.5,0.25]; extra=[min(need[f],sum(k[j]*need[(f+j-1)%NF] for j in range(3))) for f in range(NF)]
frames=[]; wrep=[]
for f in range(NF):
    pose,info=walk_pose(f,extra[f]); frames.append(pose.D)
    wrep.append(dict(f=f,reach=[round(info[s][0],3) for s in ('Left','Right')],miss=[round(info[s][1],5) for s in ('Left','Right')],extra=round(extra[f],4)))
frames.append(frames[0])
key_action('OAKENSHIELD_walk',frames,loop=True)
REPORT['walk']=wrep
# ======================= ATTACK: Thorn Spear throw =======================
# f0 bind -> f3-8 spear conjured in the right hand as it lifts -> f10 cocked high behind the right shoulder, left arm aims
# forward -> f14 release: chest drives through, right arm whips forward overhead -> f20 follow-through low across -> f30 bind.
# Feet stay planted on their bind spots (leg IK); hips turn back toward the bind twist at the end.
REL=14
def ease_keys(f,keys):
    for (f0,v0),(f1,v1) in zip(keys,keys[1:]):
        if f0<=f<=f1:
            t=smooth((f-f0)/(f1-f0)) if f1>f0 else 1.0
            return v0.lerp(v1,t) if isinstance(v0,Vector) else v0*(1-t)+v1*t
    return keys[-1][1]
RA_rest=HEAD['RightHand']-HEAD['RightArm']; LA_rest=HEAD['LeftHand']-HEAD['LeftArm']
SPEAR_DIR_REF=Vector((0,-1,0.12)).normalized()   # spear tip forward, slightly up, at the cocked frame
def spear_rot(tip):
    tip=tip.normalized(); y=-tip; x=Vector((1,0,0)); x=(x-x.dot(y)*y).normalized(); z=x.cross(y)
    return Matrix((x,y,z)).transposed()
ROFF=None   # spear rotation in the RightHand frame (fixed: the spear is rigid in the hand); taken from the natural f10 hand
def attack_pose(f,spear=True):
    pose=Pose()
    sq=ease_keys(f,[(0,0.0),(5,0.85),(30,0.0)]) if f<=5 else ease_keys(f,[(5,0.85),(26,0.85),(30,0.0)])
    tw=ease_keys(f,[(0,0.0),(10,-16.0),(REL,14.0),(20,10.0),(30,0.0)])      # chest wind (neg = right shoulder back)
    lean=ease_keys(f,[(0,0.0),(10,-4.0),(REL,8.0),(20,6.0),(30,0.0)])
    dz=ease_keys(f,[(0,0.0),(10,-0.015),(REL,-0.04),(20,-0.03),(30,0.0)])
    dy=ease_keys(f,[(0,0.0),(10,0.02),(REL,-0.03),(20,-0.025),(30,0.0)])
    hp=HEAD['Hips']+Vector((0,dy,dz))
    yH=0.25*tw; yC=YAW_C*0.6*sq+tw; yF=YAW_F*0.45*min(1.0,sq/0.85)   # lower body stays on its bind stance; chest squares toward the throw
    yw=[yH, yH+0.33*(yC-yH), yH+0.66*(yC-yH), yC, 0.5*(yC+yF), yF]
    ln=[0.3*lean,0.4*lean,0.5*lean,0.5*lean,-0.6*lean,-0.4*lean]
    spine_chain(pose,hp,yw,ln,[0,0,0,0,0,0])
    p=ease_keys(f,[(0,0.0),(10,1.0),(REL,0.6),(20,0.3),(30,0.0)])
    dC=R3(pose.D['Spine2'])@R3(REST['Spine2']).inverted()
    pose.place('RightShoulder',Rax(Y,-10.0*p)@dC@R3(REST['RightShoulder']))
    info={}
    for s in ('Left','Right'):
        ank=HEAD[s+'Foot'].copy(); Rf=Matrix.Identity(3)
        rk=HEAD[s+'Leg']-(HEAD[s+'UpLeg']+HEAD[s+'Foot'])*0.5
        pole=(rk.normalized()*0.7+FWD*0.3+Z*0.05).normalized()
        info[s]=leg(pose,s,ank,Rf,pole)
    # right (throwing) arm: wrist targets relative to the moving shoulder, chest frame
    chest=R3(pose.D['Spine2'])@R3(REST['Spine2']).inverted()@Rax(Z,-YAW_C)   # chest frame with bind twist removed
    cock=Vector((-0.26,0.16,0.04)); strike=Vector((-0.17,-0.24,0.24)); follow=Vector((0.10,-0.42,-0.22))
    rest_off=chest.inverted()@(RA_rest)
    off=ease_keys(f,[(0,rest_off),(3,rest_off.lerp(cock,0.25)),(10,cock),(REL,strike),(20,follow),(30,rest_off)])
    sh=pose.joint('RightArm'); wrist=sh+chest@off
    pl=ease_keys(f,[(0,Vector((-0.6,0.4,-1.0))),(8,Vector((-1.0,0.5,-0.4))),(10,Vector((-1.0,0.4,-0.5))),(REL,Vector((-0.8,0.0,-0.6))),(20,Vector((-0.6,0.2,-1.0))),(30,Vector((-0.6,0.4,-1.0)))])
    pole=(chest@pl).normalized()
    bend,miss=arm_ik(pose,'Right',wrist,pole)
    info['wrist_miss']=miss; info['wrist_bend']=0.0
    if spear and ROFF is not None:
        # keep the javelin pointing forward while it is in the hand (conjure f3-8, cocked f10, release f14): the hand turns
        # around the rigid spear instead of the spear flipping with the forearm
        w=ease_keys(f,[(0,0.0),(5,0.0),(10,1.0),(REL,1.0),(17,0.0),(30,0.0)])
        tipd=ease_keys(f,[(0,SPEAR_DIR_REF),(10,SPEAR_DIR_REF),(REL,Vector((0,-1,0.04)).normalized()),(30,Vector((0,-1,0.04)).normalized())])
        Rnat=R3(pose.D['RightHand']).normalized()
        Rdes=spear_rot(tipd)@ROFF.inverted()
        corr=Quaternion().slerp((Rdes@Rnat.inverted()).to_quaternion(),w).to_matrix()
        bend,miss=arm_ik(pose,'Right',wrist,pole,hand_extra=corr); info['wrist_bend']=bend
    # left arm: lifts to aim forward at the cock, sweeps down/back through the release
    la=ease_keys(f,[(0,0.0),(10,1.0),(REL,0.55),(20,0.2),(30,0.0)])
    restL=chest.inverted()@LA_rest; aim=Vector((0.12,-0.36,-0.10)); back=Vector((0.12,0.0,-0.40))
    tgt=restL.lerp(aim,la) if f<=10 else (aim.lerp(back,smooth((f-10)/10)) if f<=20 else back.lerp(restL,smooth((f-20)/10)))
    shL=pose.joint('LeftArm'); bendL,missL=arm_ik(pose,'Left',shL+chest@tgt,(chest@Vector((0.7,0.4,-0.7))).normalized())
    info['left_miss']=missL
    return pose,info
_p10,_=attack_pose(10,spear=False); _p14,_=attack_pose(REL,spear=False)
_o10=(R3(_p10.D['RightHand']).normalized().inverted()@spear_rot(SPEAR_DIR_REF)).to_quaternion()
_o14=(R3(_p14.D['RightHand']).normalized().inverted()@spear_rot(Vector((0,-1,0.04)))).to_quaternion()
ROFF=_o10.slerp(_o14,0.5).to_matrix()   # grip halfway between the natural cocked and release hands: wrist turn split between them
frames=[]; arep=[]
for f in range(NF+1):
    pose,info=attack_pose(f); frames.append(pose.D)
    arep.append(dict(f=f,reach=[round(info[s][0],3) for s in ('Left','Right')],leg_miss=[round(info[s][1],5) for s in ('Left','Right')],
        wrist_miss=round(info['wrist_miss'],4),wrist_bend=round(info['wrist_bend'],1),left_miss=round(info['left_miss'],4)))
key_action('OAKENSHIELD_attack',frames,loop=False)
REPORT['attack']=arep
pose=Pose()
for n in order: pose.D[n]=REST[n].copy()
key_action('OAKENSHIELD_rest',[pose.D],loop=False)
arm.animation_data.action=bpy.data.actions['OAKENSHIELD_walk']
sc.frame_start=0; sc.frame_end=30; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=OUTBLEND,compress=True)
json.dump(REPORT,open(OUTJSON,'w'),indent=0,default=float)
print('ANIM DONE')
