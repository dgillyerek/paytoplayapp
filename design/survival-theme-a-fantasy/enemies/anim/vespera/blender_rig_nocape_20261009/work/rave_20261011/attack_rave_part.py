# ======================= ATTACK (2026-10-11 rave conjure) =======================
# Derek on a470cea: "make her hands go in different directions as if she's outlining the orb. almost like a rave dance."
# f0-5 hands lift off her sides to either side of the orb; f5-17 contrary "gloving" sweeps: the hands stay on opposite
# sides of the orb (antipodal on its outline) and sweep in contrary directions - left over the top while right goes under,
# then swap back, then again - each sweep snaps into a hit, palms facing the orb surface at orb radius while the orb grows;
# a small shoulder/hip groove on the hits; f17-21 two-hand push; orb released at REL=f21; follow-through to f24; rest by f30.
# Forearm roll carries part of the palm turn so the wrist twist stays moderate (no twist bones).
AP=json.load(open('/workspace/vespera-rave/work/attack_params.json'))
REL=AP['rel']
def ease_keys(f,keys,fn=None):
    fn=fn or smooth
    for (f0,v0),(f1,v1) in zip(keys,keys[1:]):
        if f0<=f<=f1:
            t=fn((f-f0)/(f1-f0)) if f1>f0 else 1.0
            return v0*(1-t)+v1*t if not isinstance(v0,Vector) else v0.lerp(v1,t)
    return keys[-1][1]
def snap(t):
    t=max(0.0,min(1.0,t)); return t*t*t*(t*(6*t-15)+10)      # smootherstep: quick middle, crisp stop on the hit
RA_rest_w=HEAD['RightHand'].copy(); LA_rest_w=HEAD['LeftHand'].copy()
_cx=0.5*(HEAD['LeftArm'].x+HEAD['RightArm'].x)
PALM_LOCAL={s:(X*(1.0 if (R3(REST[s+'Hand'])@X).dot(Vector((_cx-HEAD[s+'Hand'].x,0,0)))>0 else -1.0)) for s in ('Left','Right')}
PALM_OFF=AP['palm_off']
def hand_frame(Fd,nd,s):
    Fd=Fd.normalized(); n=(nd-Fd*nd.dot(Fd)).normalized()
    Xd=n*PALM_LOCAL[s].x; Zd=Xd.cross(Fd)
    return Matrix((Xd,Fd,Zd)).transposed()
def slerp3(A,Bm,t): return A.to_quaternion().slerp(Bm.to_quaternion(),t).to_matrix()
def signed_twist(Ra,Rb,axis):
    a=Ra@X; b=Rb@X; a=(a-axis*a.dot(axis)).normalized(); b=(b-axis*b.dot(axis)).normalized()
    return math.degrees(math.atan2(axis.dot(a.cross(b)),a.dot(b)))
SWEEPS=AP['sweeps']           # [[f0,f1,phiL0,phiL1],...] left-hand angle on the orb outline (0 = her left side, +90 = top)
def phi_left(f):
    if f<=SWEEPS[0][0]: return SWEEPS[0][2]
    for f0,f1,a0,a1 in SWEEPS:
        if f0<=f<=f1: return a0+(a1-a0)*snap((f-f0)/(f1-f0))
    return SWEEPS[-1][3]
HITS=[s[1] for s in SWEEPS]
def groove(f):
    """0..1 bounce that peaks on each hit (sharp attack, soft release)"""
    g=0.0
    for h in HITS:
        d=f-h
        if -1.5<=d<=3.0: g=max(g,(1-abs(d)/1.5) if d<0 else math.exp(-d*0.9))
    return g*ease_keys(f,[(0,0.0),(AP['c0'],1.0),(AP['c1'],1.0),(REL,0.0)])
def orb_center(f):
    base=Vector((HIPS0.x+AP['ox'],HIPS0.y+AP['oy'],AP['oz']))
    push=ease_keys(f,[(0,0.0),(AP['c1'],0.0),(REL,1.0),(REL+3,1.06),(30,1.06)])
    return base+Vector((AP['px'],-AP['py'],AP['pz']))*push+Vector((0,0,-AP['groove_dz_orb']*groove(f)))
def attack_pose(f):
    pose=Pose(); g=groove(f); side=1.0
    k=0
    for i,h in enumerate(HITS):
        if f>=h-1.5: k=i
    side=1.0 if k%2==0 else -1.0                # alternate the groove lean hit to hit
    sq=ease_keys(f,[(0,0.0),(5,1.0),(REL+2,1.0),(30,0.0)])
    dz=ease_keys(f,[(0,0.0),(5,-0.008),(AP['c1'],-0.012),(REL,-0.022),(REL+3,-0.02),(30,0.0)])-AP['groove_dz']*g
    dy=ease_keys(f,[(0,0.0),(5,0.01),(AP['c1'],0.012),(REL,-0.03),(REL+3,-0.028),(30,0.0)])
    lean=ease_keys(f,[(0,0.0),(5,-1.5),(AP['c1'],-2.0),(REL,7.0),(REL+3,6.0),(30,0.0)])
    look=ease_keys(f,[(0,0.0),(5,7.0),(AP['c1'],6.0),(REL,0.0),(30,0.0)])
    hp=HEAD['Hips']+Vector((0,dy,dz))
    yH=YAW_H*0.75*sq
    yC=yH+(0.0-yH)*sq*AP['chest_square']
    yF=YAW_F*sq+0.45*(yC-YAW_F*sq)
    yw=[yH, yH+0.33*(yC-yH), yH+0.66*(yC-yH), yC, 0.5*(yC+yF), yF]
    ln=[0.3*lean,0.4*lean,0.5*lean,0.5*lean+AP['groove_pitch']*g,-0.6*lean+0.4*look,-0.4*lean+0.6*look]
    rh=AP['groove_hip_roll']*g*side; rc=AP['groove_chest_roll']*g*side
    rl=[rh,0.3*rh,-0.4*rc,-rc,0.5*rc,0.3*rc]
    spine_chain(pose,hp,yw,ln,rl)
    info={}
    for s in ('Left','Right'):
        ank=HEAD[s+'Foot'].copy(); Rf=Matrix.Identity(3)
        rk=HEAD[s+'Leg']-(HEAD[s+'UpLeg']+HEAD[s+'Foot'])*0.5
        pole=(rk.normalized()*0.7+FWD*0.3+Z*0.05).normalized()
        info[s]=leg(pose,s,ank,Rf,pole)
    w=ease_keys(f,[(0,0.0),(AP['up'],1.0),(REL+3,1.0),(30,0.0)])
    O=orb_center(f)
    osc=ease_keys(f,[(0,0.0),(AP['vis'],0.18),(AP['c1'],1.0),(30,1.0)])
    half=AP['orb_r']*max(osc,AP['min_open'])+AP['gap']
    spread=ease_keys(f,[(0,0.0),(REL,0.0),(REL+3,AP['spread']),(30,AP['spread'])])
    phL=math.radians(phi_left(f)); tiltP=math.radians(AP['plane_tilt'])
    # orbit plane: frontal, tipped forward by plane_tilt (top of the loop a little toward the target)
    up_dir=(Z*math.cos(tiltP)+FWD*math.sin(tiltP)).normalized()
    dirs={'Left':(X*math.cos(phL)+up_dir*math.sin(phL)).normalized()}
    dirs['Right']=-dirs['Left']                  # antipodal: the right hand always sweeps the contrary way
    tilt=ease_keys(f,[(0,AP['tilt0']),(AP['c1'],AP['tilt0']),(REL,AP['tilt_push']),(REL+3,AP['tilt_push']),(30,AP['tilt0'])])
    dC=R3(pose.D['Spine2'])@R3(REST['Spine2']).inverted()
    for s,sg in (('Left',1.0),('Right',-1.0)):
        d=dirs[s]
        # palm normal: toward the orb centre, eased a bit toward her neutral palm-in so the forearm turn stays natural
        n_in=(-X*sg*math.sin(math.radians(AP['wrap']))-FWD*math.cos(math.radians(AP['wrap']))).normalized()
        n=(-d*(1-AP['palm_relax'])+n_in*AP['palm_relax']).normalized()
        Fdes=(FWD+Z*math.tan(math.radians(tilt))).normalized()
        sh=pose.joint(s+'Arm')
        rest_off=(LA_rest_w if s=='Left' else RA_rest_w)-HEAD[s+'Arm']
        wrist_r=sh+dC@rest_off
        bul=math.sin(math.pi*w)**0.4
        pole=(X*sg*AP['pole_out']+Y*0.35-Z*0.9).normalized()
        Fd=Fdes
        for it in range(3):
            Fd=(Fd-n*Fd.dot(n)).normalized()
            Rd=hand_frame(Fd,n,s)
            ps=O+d*(half+spread)
            wrist_c=ps-(Rd@Vector((0,PALM_OFF[0],0)))-n*PALM_OFF[1]
            wrist=wrist_r.lerp(wrist_c,w)+(X*sg*AP['bul_out']+FWD*AP['bul_fwd'])*bul
            el,end=ik2(sh,wrist,*LENA[s],pole)
            dU=rot_between(R3(REST[s+'Arm'])@Y,el-sh)
            dF=rot_between(dU@(R3(REST[s+'ForeArm'])@Y),end-el)@dU
            nat=(dF@R3(REST[s+'Hand'])@Y).normalized()
            Fd=(Fdes*(1-AP['f_follow'])+nat*AP['f_follow']).normalized()
        fore=(end-el).normalized()
        Rn0=dF@R3(REST[s+'Hand'])
        Rdes=slerp3(Rn0,Rd,w)
        tw_total=signed_twist(Rn0,Rdes,fore)
        roll=AP['fore_roll']*tw_total               # forearm carries part of the palm turn
        dF=Matrix.Rotation(math.radians(roll),3,fore)@dF
        pose.place(s+'Arm',dU@R3(REST[s+'Arm'])); pose.place(s+'ForeArm',dF@R3(REST[s+'ForeArm']))
        Rn=dF@R3(REST[s+'Hand'])
        Rh=Rdes
        bend=math.degrees((Rh@Y).angle(Rn@Y))
        if bend>AP['max_bend']:
            kk=1.0
            for it in range(14):
                kk*=0.85; Rh=slerp3(Rn,Rdes,kk)
                bend=math.degrees((Rh@Y).angle(Rn@Y))
                if bend<=AP['max_bend']: break
        twist=abs(signed_twist(Rn,Rh,(Rh@Y).normalized()))
        pose.place(s+'Hand',Rh)
        info['bend_'+s[0]]=bend; info['twist_'+s[0]]=twist; info['roll_'+s[0]]=roll; info['miss_'+s[0]]=(end-wrist).length
    pts=[]; pns=[]
    for s in ('Left','Right'):
        Rh=R3(pose.D[s+'Hand']); wp=pose.D[s+'Hand'].translation; pn=(Rh@PALM_LOCAL[s]).normalized()
        pts.append(wp+Rh@Vector((0,PALM_OFF[0],0))+pn*PALM_OFF[1]); pns.append(pn)
        info['pn_'+s[0]]=[round(x,2) for x in pn]; info['fy_'+s[0]]=[round(x,2) for x in (Rh@Y)]; info['wr_'+s[0]]=[round(x,3) for x in wp]
    info['orb']=list((pts[0]+pts[1])*0.5); info['orb_design']=list(O); info['orb_gap']=(pts[0]-pts[1]).length; info['orb_half']=half
    info['palm_face']=[round(math.degrees(pns[i].angle((O-pts[i]).normalized())),1) for i in range(2)]
    info['orb_scale']=osc if AP['vis']<=f<REL else (1.0 if f>=REL else 0.0)
    info['spin']=phi_left(f) if f>=AP['c0'] else 0.0
    info['phiL']=phi_left(f); info['groove']=g
    return pose,info
