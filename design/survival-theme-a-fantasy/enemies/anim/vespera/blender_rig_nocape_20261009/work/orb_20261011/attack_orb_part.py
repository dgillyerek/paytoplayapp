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
    push=ease_keys(f,[(0,0.0),(AP['c1'],0.0),(REL,1.0),(REL+3,1.15),(30,1.15)])
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
    half=AP['orb_r']*max(osc,0.35)+AP['gap']
    spread=ease_keys(f,[(0,0.0),(REL,0.0),(REL+3,AP['spread']),(30,AP['spread'])])
    half+=spread
    tilt=ease_keys(f,[(0,AP['tilt0']),(AP['c1'],AP['tilt0']),(REL,AP['tilt_push']),(REL+3,AP['tilt_push']),(30,AP['tilt0'])])
    dC=R3(pose.D['Spine2'])@R3(REST['Spine2']).inverted()
    for s,sg in (('Left',1.0),('Right',-1.0)):
        n=-axis*sg                                  # palm normal points at the orb (left palm faces -X, right palm +X)
        inward=-sg*AP['toe_in']
        Fd=(FWD+Z*math.tan(math.radians(tilt))+X*inward).normalized()
        Fd=(Fd-n*Fd.dot(n)).normalized()
        Rd=hand_frame(Fd,n,s)
        ps=O+axis*half*sg                          # palm surface centre
        po=PALM_OFF
        wrist_c=ps-(Rd@Vector((0,po[0],0)))-n*po[1]
        sh=pose.joint(s+'Arm')
        rest_off=(LA_rest_w if s=='Left' else RA_rest_w)-HEAD[s+'Arm']
        wrist_r=sh+dC@rest_off
        bul=math.sin(math.pi*w)*(1.0 if f<=AP['up'] else 0.6)
        wrist=wrist_r.lerp(wrist_c,w)+(X*sg*AP['bul_out']+FWD*AP['bul_fwd'])*bul
        pole=(X*sg*1.0+Y*0.35-Z*0.75).normalized()
        el,end=ik2(sh,wrist,*LENA[s],pole)
        dU=rot_between(R3(REST[s+'Arm'])@Y,el-sh); pose.place(s+'Arm',dU@R3(REST[s+'Arm']))
        dF=rot_between(dU@(R3(REST[s+'ForeArm'])@Y),end-el)@dU; pose.place(s+'ForeArm',dF@R3(REST[s+'ForeArm']))
        Rn=dF@R3(REST[s+'Hand'])                   # hand following the forearm (natural)
        # limit the wrist: blend the designed hand toward the natural one if it bends too far
        fore=(end-el).normalized()
        Rh=slerp3(Rn,Rd,w)
        bend=math.degrees((Rh@Y).angle(fore))
        if bend>AP['max_bend']:
            k=1.0
            for it in range(12):
                k*=0.85; Rh=slerp3(Rn,Rd,w*k)
                bend=math.degrees((Rh@Y).angle(fore))
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
    info['orb']=list((pts[0]+pts[1])*0.5); info['orb_gap']=(pts[0]-pts[1]).length
    info['orb_scale']=osc if AP['vis']<=f<REL else (1.0 if f>=REL else 0.0)
    info['spin']=th+90.0 if f>=AP['c0'] else 0.0
    return pose,info
