"""2026-10-11 ORB pass: walk straightens the knees at heel strike with a longer stride, arms hang closer (no flare);
attack = two-hand conjure (palms facing, one circle while the orb grows) then a two-hand push, orb released at f20.
Vespera rest/walk/attack retargeted onto Derek's edited rig (265ec84, 16 bones), 2026-10-10.
Derek's bones are never touched (weights re-solved on his joints, see b_solve.py). Bones he deleted (Hips, Spine, L/R Shoulder, L/R ToeBase) are used only as
VIRTUAL pivots (their 507fb9a rest matrices) to place his real bones; only his 16 real bones are keyed.
Original 507fb9a notes:
World-space targets -> per-bone basis, keyed every frame f0..f30 @30fps (local XYZ euler; Hips also location).
Walk (in place, loop f30==f0): body squared to the walk direction (bind pose has pelvis turned 19 deg and chest 28 deg),
two-bone leg IK to treadmill feet, heel-strike -> flat -> toe-off feet pivoting on measured heel/ball points, feet and
knees pointing forward, ~12 cm stance, close arm swing, slight head nod/turn.
Attack (Shadow Bolt, one-hand cast, release f13, starts/ends at the bind pose): feet planted (leg IK), chest winds to the
right then drives through, right arm cocks beside the shoulder and punches forward to shoulder height at f13, wrist stays
in line with the forearm (<=25 deg), left arm tucks in, head turns to face forward."""
import bpy, math, json, sys
import numpy as np
from mathutils import Matrix, Vector
OUTBLEND, OUTJSON = sys.argv[sys.argv.index('--')+1:][:2]
sc=bpy.context.scene; sc.render.fps=30
arm=bpy.data.objects['VESPERA_rig']; body=bpy.data.objects['VESPERA_body']
if bpy.context.object and bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
P='mixamorig:'
B={b.name[len(P):]:b for b in arm.data.bones}
REST={n:b.matrix_local.copy() for n,b in B.items()}
HEAD={n:b.head_local.copy() for n,b in B.items()}; TAIL={n:b.tail_local.copy() for n,b in B.items()}
order=[b.name[len(P):] for b in arm.data.bones]
REALPARENT={n:(B[n].parent.name[len(P):] if B[n].parent else None) for n in B}
MINE=json.load(open('/workspace/vespera-derek-rig/work/d_mine.json'))['bones']
VIRTUAL=[]
for full,bd in MINE.items():
    n=full[len(P):]
    if n in B: continue
    m=Matrix([bd['roll_mat'][0:3],bd['roll_mat'][3:6],bd['roll_mat'][6:9]]).to_4x4(); m.translation=Vector(bd['head'])
    REST[n]=m; HEAD[n]=Vector(bd['head']); TAIL[n]=Vector(bd['tail']); VIRTUAL.append(n)
PARENT={full[len(P):]:(bd['parent'][len(P):] if bd['parent'] else None) for full,bd in MINE.items()}   # placement hierarchy (507fb9a)
print('VIRTUAL',VIRTUAL)
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
for s in ('Left','Right'):
    fm=(W[:,vg[P+s+'Foot']]+(W[:,vg[P+s+'ToeBase']] if P+s+'ToeBase' in vg else 0))>0.5
    fv=co[fm]; ank=HEAD[s+'Foot']
    tip0=Vector(fv[fv[:,1].argmin()]); d0=Vector((tip0.x-ank.x,tip0.y-ank.y,0)).normalized()
    along=(fv[:,0]-ank.x)*d0.x+(fv[:,1]-ank.y)*d0.y
    tip=Vector(fv[along.argmax()])
    front=fv[along>0.06]; zmin=front[:,2].min(); blk=front[front[:,2]<zmin+0.006]
    ball=Vector(blk.mean(0)); ball.z=float(zmin)
    back=fv[(along<0.0)&(fv[:,2]<0.16)]; heel=Vector(back[back[:,2].argmin()])
    fdir=Vector((ball.x-heel.x,ball.y-heel.y,0)).normalized()
    yfix=yaw_of(FWD)-yaw_of(fdir)
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
            p=REALPARENT[n]
            bm=(REST[n].inverted()@D[n]) if p is None else (REST[n].inverted()@REST[p]@D[p].inverted()@D[n])
            loc,rot,scl=bm.decompose()
            e=rot.to_euler('XYZ',prevE[n]) if n in prevE else rot.to_euler('XYZ')
            if loop and f==len(frames)-1: e=first[n]
            pb=arm.pose.bones[P+n]
            pb.location=loc if (p is None or not B[n].use_connect) else Vector((0,0,0))
            pb.rotation_euler=e; pb.scale=(1,1,1)
            for path in ('location','rotation_euler','scale'): pb.keyframe_insert(path,frame=f,group=pb.name)
            prevE[n]=e.copy()
            if f==0: first[n]=e.copy()
    return act
REPORT={'geom':GEOM}
# ======================= WALK =======================
WALK=json.load(open('/workspace/vespera-orb/work/walk_params.json'))
CONTACT={'Right':8.0,'Left':23.0}; STANCE=18.0; SWING=NF-STANCE
HIPS0=HEAD['Hips'].copy()
X_ANK={'Left':HIPS0.x+0.058,'Right':HIPS0.x-0.062}
Y_FRONT=HIPS0.y-WALK['yf']; Y_BACK=HIPS0.y+WALK['yb']
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
    bob=WALK['bob']*math.cos(2*math.pi*(f-WALK['bob_ph'])/15.0)
    return dict(dz=WALK['dz']+bob,dx=0.014*cyc(f,2.0),yawP=-4.5*cyc(f,23.0),rollP=-2.0*cyc(f,2.0),yawC=4.0*cyc(f,23.0),
                nod=1.6*math.cos(2*math.pi*(f-10.0)/15.0),turn=-1.8*cyc(f,23.0),tilt=0.8*cyc(f,2.0))
def walk_pose(f,extra=0.0):
    bp=walk_body(f); pose=Pose()
    hp=HIPS0+Vector((bp['dx'],0,bp['dz']+extra))
    ch=YAW_H+0.30*(YAW_C-YAW_H); hd=YAW_F+0.35*(ch-YAW_F)
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
    # arms (2026-10-11): hang closer to her sides (abduction WALK['abd'] deg, no forearm flare), swing opposite the legs, elbows
    # bend a little more on the forward swing so the hand passes in front of the flared skirt instead of out to the side
    for s,sg,f0 in (('Left',1,8.0),('Right',-1,23.0)):
        phi=WALK['sw0']+WALK['sw']*cyc(f,f0); a=math.radians(WALK['abd']+WALK['abd_back']*max(0.0,-phi)/WALK['sw']); ph=math.radians(phi)
        d=Vector((sg*math.sin(a),-math.sin(ph)*math.cos(a),-math.cos(ph)*math.cos(a)))
        fw=(phi+WALK['sw'])/(2*WALK['sw']); bend=math.radians(WALK['el0']+WALK['el']*fw)
        perp=(FWD-d*FWD.dot(d)).normalized(); fd=(d*math.cos(bend)+perp*math.sin(bend)+X*sg*math.tan(math.radians(WALK['flare']))).normalized()
        arm_dirs(pose,s,d,fd)
    return pose,info
extra=[0.0]*NF
for it in range(3):
    need=[]
    for f in range(NF):
        pose,info=walk_pose(f,extra[f]); r=max(info[s][0] for s in ('Left','Right'))
        need.append(extra[f]-max(0.0,(r-WALK['reach']))*sum(LEN['Left']))
    k=[0.25,0.5,0.25]; extra=[min(need[f],sum(k[j]*need[(f+j-1)%NF] for j in range(3))) for f in range(NF)]
frames=[]; wrep=[]
for f in range(NF):
    pose,info=walk_pose(f,extra[f]); frames.append(pose.D)
    wrep.append(dict(f=f,reach=[round(info[s][0],3) for s in ('Left','Right')],miss=[round(info[s][1],5) for s in ('Left','Right')],extra=round(extra[f],4)))
frames.append(frames[0])
key_action('VESPERA_nocape_walk',frames,loop=True)
REPORT['walk']=wrep
# ======================= ATTACK (2026-10-11 orb conjure) =======================
# Both hands come up in front of her chest, palms facing each other, and trace one circle together (f6-f16) while the
# orb forms and grows between the palms (visible f4); two-hand push f16-f20, orb released at REL=f20 (bolt spawns at the
# orb centre), follow-through to f23, back to the bind pose by f30. Feet planted (leg IK). Hand shape: open (no finger bones).
AP=json.load(open('/workspace/vespera-orb/work/attack_params.json'))
REL=AP['rel']
def ease_keys(f,keys):
    for (f0,v0),(f1,v1) in zip(keys,keys[1:]):
        if f0<=f<=f1:
            t=smooth((f-f0)/(f1-f0)) if f1>f0 else 1.0
            return v0*(1-t)+v1*t if not isinstance(v0,Vector) else v0.lerp(v1,t)
    return keys[-1][1]
RA_rest_w=HEAD['RightHand'].copy(); LA_rest_w=HEAD['LeftHand'].copy()
HANDY={s:(R3(REST[s+'Hand'])@Y).normalized() for s in ('Left','Right')}
# palm axis sign: which way local X points at the bind pose relative to "toward her centre line"
_cx=0.5*(HEAD['LeftArm'].x+HEAD['RightArm'].x)
PSIGN={}
for s in ('Left','Right'):
    lx=R3(REST[s+'Hand'])@X; inward=Vector((_cx-HEAD[s+'Hand'].x,0,0)).normalized()
    PSIGN[s]=-1.0 if lx.dot(inward)<0 else 1.0     # palm normal = -PSIGN*localX? -> palm normal = PSIGN*... see below
# palm normal (pointing out of the palm) in bone axes: at the bind both palms face her thighs (= inward)
PALM_LOCAL={s:(X*(1.0 if (R3(REST[s+'Hand'])@X).dot(Vector((_cx-HEAD[s+'Hand'].x,0,0)))>0 else -1.0)) for s in ('Left','Right')}
PALM_OFF=AP['palm_off']        # palm surface centre in bone axes: along fingers, out of palm
def hand_frame(Fd,nd,s):
    """world rotation of the hand bone with fingers (bone Y) along Fd and palm normal along nd"""
    Fd=Fd.normalized(); n=(nd-Fd*nd.dot(Fd)).normalized()
    xs=PALM_LOCAL[s].x                      # +1: palm normal is +X; -1: palm normal is -X
    Xd=n*xs; Zd=Xd.cross(Fd)
    return Matrix((Xd,Fd,Zd)).transposed()
def slerp3(A,Bm,t):
    return A.to_quaternion().slerp(Bm.to_quaternion(),t).to_matrix()
def orb_center(f,hp):
    """design path of the orb centre (Blender world), relative to the hips"""
    base=Vector((HIPS0.x+AP['ox'],HIPS0.y+AP['oy'],AP['oz']))
    th=math.radians(-90.0+360.0*smooth((f-AP['c0'])/(AP['c1']-AP['c0']))**1.0 if AP['c0']<=f<=AP['c1'] else (-90.0 if f<AP['c0'] else 270.0))
    if AP['c0']<=f<=AP['c1']:
        u=(f-AP['c0'])/(AP['c1']-AP['c0']); th=math.radians(-90.0+360.0*(0.5-0.5*math.cos(math.pi*u))*0.35+360.0*u*0.65)
    r=AP['cr']*ease_keys(f,[(0,0.0),(AP['c0'],1.0),(AP['c1'],1.0),(REL,0.0)])
    circ=Vector((r*math.cos(th),-AP['cdepth']*r/AP['cr']*math.sin(th) if AP['cr']>0 else 0,r*math.sin(th)+r))
    push=ease_keys(f,[(0,0.0),(AP['c1'],0.0),(REL,1.0),(REL+3,1.06),(30,1.06)])
    pv=Vector((AP['px'],-AP['py'],AP['pz']))*push
    return base+circ+pv, math.degrees(th)
def attack_pose(f):
    pose=Pose()
    sq=ease_keys(f,[(0,0.0),(6,1.0),(22,1.0),(30,0.0)])
    dz=ease_keys(f,[(0,0.0),(6,-0.008),(16,-0.012),(REL,-0.022),(REL+3,-0.02),(30,0.0)])
    dy=ease_keys(f,[(0,0.0),(6,0.01),(16,0.012),(REL,-0.03),(REL+3,-0.028),(30,0.0)])
    lean=ease_keys(f,[(0,0.0),(6,-1.5),(16,-2.0),(REL,7.0),(REL+3,6.0),(30,0.0)])
    look=ease_keys(f,[(0,0.0),(6,7.0),(16,6.0),(REL,0.0),(30,0.0)])
    hp=HEAD['Hips']+Vector((0,dy,dz))
    yH=YAW_H*0.75*sq; yC=yH+(YAW_C-YAW_H)*0.3*sq*0.0
    yC=yH+(0.0-yH)*sq*AP['chest_square']+(YAW_C-YAW_H)*(1-sq)*0.0
    yF=YAW_F*sq+0.45*(yC-YAW_F*sq)
    yw=[yH, yH+0.33*(yC-yH), yH+0.66*(yC-yH), yC, 0.5*(yC+yF), yF]
    ln=[0.3*lean,0.4*lean,0.5*lean,0.5*lean,-0.6*lean+0.4*look,-0.4*lean+0.6*look]
    spine_chain(pose,hp,yw,ln,[0,0,0,0,0,0])
    info={}
    for s in ('Left','Right'):
        ank=HEAD[s+'Foot'].copy(); Rf=Matrix.Identity(3)
        rk=HEAD[s+'Leg']-(HEAD[s+'UpLeg']+HEAD[s+'Foot'])*0.5
        pole=(rk.normalized()*0.7+FWD*0.3+Z*0.05).normalized()
        info[s]=leg(pose,s,ank,Rf,pole)
    # conjure weight: 0 at the bind pose, 1 while the hands work the orb
    w=ease_keys(f,[(0,0.0),(AP['up'],1.0),(REL+3,1.0),(30,0.0)])
    O,th=orb_center(f,hp)
    osc=ease_keys(f,[(0,0.0),(AP['vis'],0.18),(AP['c1'],1.0),(30,1.0)])
    roll=AP['roll']*math.sin(math.radians(th))*ease_keys(f,[(0,0.0),(AP['c0'],1.0),(AP['c1'],1.0),(REL,0.0)])
    axis=(Rax(FWD,roll)@X).normalized()            # right palm -> left palm
    half=AP['orb_r']*max(osc,AP['min_open'])+AP['gap']
    spread=ease_keys(f,[(0,0.0),(REL,0.0),(REL+3,AP['spread']),(30,AP['spread'])])
    half+=spread
    tilt=ease_keys(f,[(0,AP['tilt0']),(AP['c1'],AP['tilt0']),(REL,AP['tilt_push']),(REL+3,AP['tilt_push']),(30,AP['tilt0'])])
    dC=R3(pose.D['Spine2'])@R3(REST['Spine2']).inverted()
    for s,sg in (('Left',1.0),('Right',-1.0)):
        ga=math.radians(AP['wrap'])                 # palms cup the orb from behind/outside: normal = toward the orb centre
        n=(-axis*sg*math.sin(ga)-FWD*math.cos(ga)).normalized()
        inward=-sg*AP['toe_in']
        Fdes=(FWD+Z*math.tan(math.radians(tilt))+X*inward).normalized()
        sh=pose.joint(s+'Arm')
        rest_off=(LA_rest_w if s=='Left' else RA_rest_w)-HEAD[s+'Arm']
        wrist_r=sh+dC@rest_off
        bul=math.sin(math.pi*w)**0.4
        pole=(X*sg*AP['pole_out']+Y*0.35-Z*0.9).normalized()
        Fd=Fdes
        for it in range(3):       # fingers follow the forearm part-way (natural wrist), palms keep facing the orb
            Fd=(Fd-n*Fd.dot(n)).normalized()
            Rd=hand_frame(Fd,n,s)
            ps=O-n*half                                # palm surface centre on the orb, facing its centre
            wrist_c=ps-(Rd@Vector((0,PALM_OFF[0],0)))-n*PALM_OFF[1]
            wrist=wrist_r.lerp(wrist_c,w)+(X*sg*AP['bul_out']+FWD*AP['bul_fwd'])*bul
            el,end=ik2(sh,wrist,*LENA[s],pole)
            dU=rot_between(R3(REST[s+'Arm'])@Y,el-sh)
            dF=rot_between(dU@(R3(REST[s+'ForeArm'])@Y),end-el)@dU
            nat=(dF@R3(REST[s+'Hand'])@Y).normalized()
            Fd=(Fdes*(1-AP['f_follow'])+nat*AP['f_follow']).normalized()
        pose.place(s+'Arm',dU@R3(REST[s+'Arm'])); pose.place(s+'ForeArm',dF@R3(REST[s+'ForeArm']))
        if False: print('NAT',f,s,[round(x,2) for x in nat],'fore',[round(x,2) for x in (end-el).normalized()],'Fd',[round(x,2) for x in Fd],'n',[round(x,2) for x in n],'el',[round(x,3) for x in el])
        Rn=dF@R3(REST[s+'Hand'])                   # hand following the forearm (natural)
        # limit the wrist: blend the designed hand toward the natural one if it bends too far
        fore=(end-el).normalized()
        Rh=slerp3(Rn,Rd,w)
        if False: print('DBG',s,'Rd.Y',[round(x,2) for x in Rd@Y],'Rn.Y',[round(x,2) for x in Rn@Y],'Rh.Y',[round(x,2) for x in Rh@Y],'w',round(w,2),'b0',round(math.degrees((Rh@Y).angle(Rn@Y)),1),'Rd.X',[round(x,2) for x in Rd@X],'Rn.X',[round(x,2) for x in Rn@X])
        bend=math.degrees((Rh@Y).angle(Rn@Y))
        if bend>AP['max_bend']:
            k=1.0
            for it in range(12):
                k*=0.85; Rh=slerp3(Rn,Rd,w*k)
                bend=math.degrees((Rh@Y).angle(Rn@Y))
                if bend<=AP['max_bend']: break
        # twist about the forearm vs the natural hand
        Rt=rot_between(Rn@Y,Rh@Y)@Rn
        twist=math.degrees((Rt@X).angle(Rh@X))
        pose.place(s+'Hand',Rh)
        info['bend_'+s[0]]=bend; info['twist_'+s[0]]=twist; info['miss_'+s[0]]=(end-wrist).length
    # actual orb centre = midpoint of the two palm surfaces as posed
    pts=[]
    for s,sg in (('Left',1.0),('Right',-1.0)):
        Rh=R3(pose.D[s+'Hand']); wp=pose.D[s+'Hand'].translation
        pts.append(wp+Rh@Vector((0,PALM_OFF[0],0))+(Rh@PALM_LOCAL[s])*PALM_OFF[1])
    for s,sg in (('Left',1.0),('Right',-1.0)):
        Rh=R3(pose.D[s+'Hand']); info['pn_'+s[0]]=[round(x,2) for x in (Rh@PALM_LOCAL[s])]; info['fy_'+s[0]]=[round(x,2) for x in (Rh@Y)]; info['wr_'+s[0]]=[round(x,3) for x in pose.D[s+'Hand'].translation]
    pns=[(R3(pose.D[s+'Hand'])@PALM_LOCAL[s]).normalized() for s in ('Left','Right')]
    info['orb']=list(((pts[0]+pns[0]*half)+(pts[1]+pns[1]*half))*0.5); info['orb_gap']=(pts[0]-pts[1]).length; info['orb_half']=half
    info['orb_scale']=osc if AP['vis']<=f<REL else (1.0 if f>=REL else 0.0)
    info['spin']=th+90.0 if f>=AP['c0'] else 0.0
    return pose,info
frames=[]; arep=[]
for f in range(NF+1):
    pose,info=attack_pose(f); frames.append(pose.D)
    arep.append(dict(f=f,reach=[round(info[s][0],3) for s in ('Left','Right')],leg_miss=[round(info[s][1],5) for s in ('Left','Right')],
        wrist_bend=[round(info['bend_L'],1),round(info['bend_R'],1)],wrist_twist=[round(info['twist_L'],1),round(info['twist_R'],1)],
        wrist_miss=[round(info['miss_L'],4),round(info['miss_R'],4)],orb=[round(x,4) for x in info['orb']],orb_gap=round(info['orb_gap'],4),orb_scale=round(info['orb_scale'],3),orb_spin=round(info['spin'],1),pn=[info['pn_L'],info['pn_R']],fy=[info['fy_L'],info['fy_R']],wr=[info['wr_L'],info['wr_R']]))
key_action('VESPERA_nocape_attack',frames,loop=False)
REPORT['attack']=arep
# rest action (single key, bind pose) so the blend carries all three
pose=Pose()
for n in order: pose.D[n]=REST[n].copy()
key_action('VESPERA_nocape_rest',[pose.D],loop=False)
arm.animation_data.action=bpy.data.actions['VESPERA_nocape_walk']
bpy.context.view_layer.objects.active=arm
sc.frame_start=0; sc.frame_end=30; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=OUTBLEND,compress=True)
json.dump(REPORT,open(OUTJSON,'w'),indent=0,default=float)
print('ANIM DONE')
