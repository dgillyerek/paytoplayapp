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
    yH=0.25*tw; yC=YAW_C*0.6*sq+tw; yF=YAW_F   # lower body stays on its bind stance; chest squares toward the throw
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
pose=Pose()   # rest: bind pose with the face turned forward (neck half, head the rest)
for n in order: pose.D[n]=REST[n].copy()
pose.place('Neck',Rax(Z,0.5*YAW_F)@R3(REST['Neck'])); pose.place('Head',Rax(Z,YAW_F)@R3(REST['Head']))
key_action('OAKENSHIELD_rest',[pose.D],loop=False)
arm.animation_data.action=bpy.data.actions['OAKENSHIELD_walk']
sc.frame_start=0; sc.frame_end=30; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=OUTBLEND,compress=True)
json.dump(REPORT,open(OUTJSON,'w'),indent=0,default=float)
print('ANIM DONE')
