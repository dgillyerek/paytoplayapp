# Armature space of IRONHOWL_rig.fbx: +Y up, +Z forward, +X his left (cm, degrees).
# Rotations are about world axes through the bone head, applied in order on top of the stance.
X=(1,0,0); Y=(0,1,0); Z=(0,0,1)
def mir(d): return (-d[0],d[1],d[2])
def arms(L=None,R=None,mirror=False):
    out=[]
    if L:
        for b,d in zip(('LeftArm','LeftForeArm','LeftHand'),L): out.append((b,d))
    if mirror: R=[mir(d) for d in L]
    if R:
        for b,d in zip(('RightArm','RightForeArm','RightHand'),R): out.append((b,d))
    return out
def k(f, hips=(0,0,0), rot=(), aim=(), ik=(), feet=None, footrot=None, interp='BEZIER', pr=False):
    d={'f':f,'hips':hips,'rot':list(rot),'aim':list(aim),'ik':list(ik),'interp':interp,'print':pr}
    if feet: d['feet']=feet
    if footrot: d['footrot']=footrot
    return d
SQUARE=[('Spine',Y,12),('Spine1',Y,10),('Spine2',Y,8)]
# --- two-fist ground pound
def ik2(L,R,pole,hd):
    return [('L',L,pole,hd),('R',R,mir(pole),mir(hd) if hd else None)]
GP_UP=dict(rot=SQUARE+[('Spine',X,-10),('Spine1',X,-8),('Spine2',X,-6),('Head',X,-12)],
           ik=ik2((5,190,-2),(-5,190,-2),(1,0,-0.3),(-0.3,1,0)))
GP_UP2=dict(rot=GP_UP['rot']+[('Spine1',X,-4)], ik=ik2((5,192,-6),(-5,192,-6),(1,0,-0.3),(-0.3,1,0)))
GP_DN=dict(rot=SQUARE+[('Spine',X,22),('Spine1',X,14),('Spine2',X,10),('Head',X,-22)],
           ik=ik2((10,7,62),(-10,7,62),(1,0.4,-0.4),(0,-0.7,0.7)))
GP_DN2=dict(rot=GP_DN['rot']+[('Spine',X,3)], ik=ik2((11,6,63),(-11,6,63),(1,0.4,-0.4),(0,-0.7,0.7)))
GROUND_POUND=dict(name='ground_pound', contact=22, keys=[
    k(0),
    k(13,hips=(0,2,-6),**GP_UP),
    k(17,hips=(0,3,-7),interp='QUAD:EASE_IN',pr=True,**GP_UP2),
    k(22,hips=(0,-24,14),pr=True,**GP_DN),
    k(26,hips=(0,-27,15),**GP_DN2),
    k(32,hips=(0,-25,14),**GP_DN),
    k(52)])
# --- lead-arm backhand sweep
GUARD_R=[(-0.25,-0.9,0.25),(0.75,0.5,0.4),(0.8,0.45,0.35)]
BH_WIND=dict(rot=[('Hips',Y,-10),('Spine',Y,-14),('Spine1',Y,-12),('Spine2',Y,-10),('Head',Y,14)],
             aim=arms(L=[(-0.55,-0.25,0.8),(-0.55,0.55,-0.6),(-0.5,0.6,-0.6)],R=GUARD_R))
BH_WIND2=dict(rot=BH_WIND['rot']+[('Spine1',Y,-5)],aim=BH_WIND['aim'])
BH_HIT=dict(rot=[('Hips',Y,12),('Spine',Y,12),('Spine1',Y,12),('Spine2',Y,10),('Spine',X,8),('Head',Y,-14)],
            aim=arms(L=[(0.75,0.05,0.65),(0.85,0.05,0.5),(0.9,0.0,0.4)],R=GUARD_R))
BH_FOL=dict(rot=[('Hips',Y,20),('Spine',Y,18),('Spine1',Y,16),('Spine2',Y,14),('Spine',X,8),('Head',Y,-30)],
            aim=arms(L=[(0.95,0.12,-0.2),(0.85,0.15,-0.5),(0.75,0.1,-0.65)],R=GUARD_R))
BACKHAND=dict(name='backhand_sweep', contact=15, keys=[
    k(0),
    k(9,hips=(0,-4,-3),**BH_WIND),
    k(12,hips=(0,-5,-4),interp='QUAD:EASE_IN',**BH_WIND2),
    k(15,hips=(0,-6,6),interp='LINEAR',**BH_HIT),
    k(19,hips=(0,-6,7),**BH_FOL),
    k(24,hips=(0,-5,6),**BH_FOL),
    k(40)])
# --- lead-shoulder charge
SC_TUCK_IK=[('L',(-16,-26,10),(0.6,-1,-0.2),None,'LeftArm'),('R',(16,-30,12),(-0.6,-1,-0.2),None,'RightArm')]
SC_WIND=dict(rot=[('Hips',Y,-15),('Spine',Y,-12),('Spine1',Y,-10),('Spine',X,-4)],ik=SC_TUCK_IK)
SC_DRIVE=dict(rot=[('Hips',Y,-18),('Spine',Y,-12),('Spine1',Y,-10),('Spine',X,18),('Spine1',X,8),('Head',X,-12)],ik=SC_TUCK_IK)
SC_HIT=dict(rot=[('Hips',Y,-20),('Spine',Y,-14),('Spine1',Y,-12),('Spine',X,28),('Spine1',X,12),('Head',X,-22),('LeftShoulder',Y,-15)],ik=SC_TUCK_IK)
SHOULDER=dict(name='shoulder_charge', contact=20, keys=[
    k(0),
    k(10,hips=(0,-12,-10),interp='QUAD:EASE_IN',**SC_WIND),
    k(16,hips=(0,-14,38),feet={'L':(0,10,30),'R':(0,2,16)},**SC_DRIVE),
    k(20,hips=(0,-12,62),feet={'L':(0,0,55),'R':(0,0,28)},pr=True,**SC_HIT),
    k(24,hips=(0,-14,64),feet={'L':(0,0,55),'R':(0,0,28)},**SC_HIT),
    k(32,hips=(0,-8,40),feet={'L':(0,6,32),'R':(0,0,14)},**SC_WIND),
    k(46)])
SPECS=[GROUND_POUND,BACKHAND,SHOULDER]
