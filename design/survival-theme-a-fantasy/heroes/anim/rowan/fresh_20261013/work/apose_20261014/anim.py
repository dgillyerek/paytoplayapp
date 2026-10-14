"""Rowan A-pose rig actions (2026-10-14): rest (arms down by the sides from the A-pose bind), walk (body bones only), archer attack.
Arm roll is never left free: every arm is solved as 2-bone IK with an explicit pole, and Arm/ForeArm get a FULL basis (bone Y on the
segment, the rest-pose elbow hinge axis on the IK hinge n = pole x reach), so the forearm can never twist or flip. Hand is keyed every
frame with its rest offset to the forearm (no wrist twist). Per-frame roll check written to anim.json."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, math, json, numpy as np
from mathutils import Vector, Matrix, Quaternion
W_='/workspace/rowan_fresh/w2'
bpy.ops.wm.open_mainfile(filepath=W_+'/rigc.blend')
sc=bpy.context.scene; sc.render.fps=30; sc.frame_start=0; sc.frame_end=30
arm=bpy.data.objects['ROWAN_rig']; PB=arm.pose.bones; P_='mixamorig:'
for pb in PB: pb.rotation_mode='QUATERNION'
arm.animation_data_create()
J={k:Vector(v) for k,v in json.load(open(W_+'/joints.json')).items()}
def upd(): bpy.context.view_layer.update()
def reset():
    for pb in PB: pb.location=(0,0,0); pb.rotation_quaternion=(1,0,0,0); pb.scale=(1,1,1)
    upd()
def RW(n): return arm.data.bones[P_+n].matrix_local.to_3x3()
def rot(n,axis,deg):
    pb=PB[P_+n]; R=pb.bone.matrix_local.to_3x3(); q=Quaternion(Vector(axis),math.radians(deg))
    pb.rotation_quaternion=(R.inverted()@q.to_matrix()@R).to_quaternion()@pb.rotation_quaternion; upd()
def setworld(n,R):
    upd(); pb=PB[P_+n]; M=pb.matrix.copy(); pb.matrix=Matrix.Translation(M.to_translation())@R.to_4x4(); upd()
def keep_rest_world(n): setworld(n,RW(n))
def key(f):
    upd()
    for pb in PB: pb.keyframe_insert('rotation_quaternion',frame=f); pb.keyframe_insert('location',frame=f)
def act(name):
    a=bpy.data.actions.new(name); a.use_fake_user=True; arm.animation_data.action=a; return a
L1=(J['LeftForeArm']-J['LeftArm']).length; L2=(J['LeftHand']-J['LeftForeArm']).length
# rest elbow hinge (world) and its local coords in Arm / ForeArm
NLOC={}
for s in ('Left','Right'):
    S,E,H=J[s+'Arm'],J[s+'ForeArm'],J[s+'Hand']; n=(E-S).cross(H-E).normalized()
    for b in ('Arm','ForeArm'): NLOC[s+b]=(RW(s+b).inverted()@n).normalized()
def basis(nl,yl=Vector((0,1,0))):
    zl=nl.cross(yl); return Matrix((nl,yl,zl)).transposed()
def ik(S,H,pole):
    d=H-S; D=min(d.length,(L1+L2)*0.985); u=d.normalized(); H=S+u*D
    a=(L1*L1-L2*L2+D*D)/(2*D); h=math.sqrt(max(L1*L1-a*a,0)); p=Vector(pole); p=(p-p.dot(u)*u).normalized()
    return S+u*a+p*h, H
def pose_arm(s,H,pole):
    upd(); S=PB[P_+s+'Arm'].head.copy(); E,H=ik(S,Vector(H),pole)
    n=(E-S).cross(H-E).normalized()
    for b,d in (('Arm',(E-S).normalized()),('ForeArm',(H-E).normalized())):
        T=basis(n,d); L=basis(NLOC[s+b]); setworld(s+b,T@L.inverted())
    fa=PB[P_+s+'ForeArm'].matrix.to_3x3(); setworld(s+'Hand',fa@RW(s+'ForeArm').inverted()@RW(s+'Hand'))   # wrist: rest offset, keyed
def head_forward(nod=0.0): keep_rest_world('Head'); rot('Head',(1,0,0),nod)
def twist(a):
    rot('Hips',(0,0,1),a*0.25); rot('Spine',(0,0,1),a*0.20); rot('Spine1',(0,0,1),a*0.20); rot('Spine2',(0,0,1),a*0.35); rot('Neck',(0,0,1),-a*0.5)
    for s in ('Left','Right'): keep_rest_world(s+'UpLeg')      # feet stay planted, facing forward
def ease(t): t=max(0,min(1,t)); return t*t*(3-2*t)
import os
RX=float(os.environ.get('REST_X','0.39')); RZ=float(os.environ.get('REST_Z','0.95'))
RY=float(os.environ.get('REST_Y','-0.04'))
REST={'Left':Vector((RX,RY,RZ)),'Right':Vector((-RX,RY,RZ))}
POLE_REST={'Left':Vector((0.25,1,0)),'Right':Vector((-0.25,1,0))}
def rest_arms(swL=0.0,swR=0.0):
    for s,sw in (('Left',swL),('Right',swR)):
        sg_=1 if s=='Left' else -1
        H=REST[s]+Vector((sg_*(0.012*(abs(sw)>0)+0.6*max(0.0,-sw)+0.2*max(0.0,sw)),-sw,0.25*abs(sw)))   # swing forward (-Y) lifts the hand a little; back swing goes out round the coat tails
        pose_arm(s,H,POLE_REST[s])
# ---- roll check ----
ROLL=[]
def twist_about_y(q):
    q=q.normalized(); a=math.degrees(2*math.atan2(q.y,q.w)); return ((a+180)%360)-180
def roll_check(tag,f):
    upd(); r={'clip':tag,'f':f}
    for s in ('Left','Right'):
        nA=PB[P_+s+'Arm'].matrix.to_3x3()@NLOC[s+'Arm']; nF=PB[P_+s+'ForeArm'].matrix.to_3x3()@NLOC[s+'ForeArm']
        r[s+'_hinge_mismatch_deg']=round(math.degrees(nA.angle(nF)),3)
        r[s+'_forearm_twist_deg']=round(twist_about_y(PB[P_+s+'ForeArm'].matrix_basis.to_quaternion()),2)
        r[s+'_hand_twist_deg']=round(twist_about_y(PB[P_+s+'Hand'].matrix_basis.to_quaternion()),2)
        r[s+'_hand_q']=list(PB[P_+s+'Hand'].matrix.to_quaternion())
    ROLL.append(r)
A={}
A['rest']=act('ROWAN_rest')
for f in (0,30): reset(); rest_arms(); head_forward(); key(f); roll_check('rest',f)
A['walk']=act('ROWAN_walk')
for f in range(31):
    ph=2*math.pi*f/30; s,c=math.sin(ph),math.cos(ph); reset()
    PB[P_+'Hips'].location=(0,-0.015*abs(s),0)
    for side,sg in (('Left',1),('Right',-1)):
        sw=-17*s*sg; knee=5+36*max(0.0,c*sg)**1.3
        rot(side+'UpLeg',(1,0,0),sw); rot(side+'Leg',(1,0,0),knee)
        rot(side+'Foot',(1,0,0),-(sw+knee)*0.6 if c*sg>0 else -sw*0.5)
    rot('Spine1',(0,0,1),3*s); rot('Neck',(0,0,1),-1.5*s)
    rest_arms(swL=0.09*s,swR=-0.09*s)
    head_forward(nod=0.9*(1-math.cos(2*ph))); key(f); roll_check('walk',f)
A['attack']=act('ROWAN_attack')
# Attack v2 (Derek 2026-10-14: an arm came through the chest on the raise). Both hands follow keyed waypoints in the TORSO frame
# (character rotated with the 70 deg chest turn), so the turn carries the arms with it instead of sweeping the chest into them.
# Draw arm: elbow leads out to the side, then up, then the hand comes in to the cheek from outside the shoulder line.
# Bow arm: swings out to the side first (the 70 deg turn makes "out to the side" point at the target), then straight from the shoulder.
TW=-70.0
TGT=Vector((0.0,-1.0,0.05)).normalized()
PIV=Vector((0.0,0.02,0.0))
def Rz(a): return Matrix.Rotation(math.radians(a),3,'Z')
def to_world(p,a): return Rz(a)@(p-PIV)+PIV
def to_torso(p,a): return Rz(-a)@(p-PIV)+PIV
def twist_w(f):   # turn in over f0-7, hold to f17, turn back by f30
    return ease(f/7) if f<=7 else (1.0 if f<=17 else 1-ease((f-17)/13))
ANCH=Vector((float(os.environ.get('AX','-0.18')),float(os.environ.get('AY','0.02')),float(os.environ.get('AZ','-0.01')))); PULL=Vector((-0.07,0.05,0.01))
# right (draw) hand waypoints, torso frame: (frame, hand or 'anchor'/'follow', pole)
# full draw elbow: behind the shoulder in line with the arrow, at shoulder height (+3 cm); pole searched about the reach axis
reset(); twist(TW); head_forward(); upd()
SRf=PB[P_+'RightArm'].head.copy(); HRf=PB[P_+'Head'].head.copy()+ANCH
uR=(HRf-SRf).normalized(); f1_=uR.orthogonal().normalized(); f2_=uR.cross(f1_).normalized(); bestE=None
for k_ in range(72):
    ph=2*math.pi*k_/72; p=f1_*math.cos(ph)+f2_*math.sin(ph); E,_=ik(SRf,HRf,p)
    out_=(Vector((E.x,E.y,0))-Vector((0,0.02,0))).length-(Vector((SRf.x,SRf.y,0))-Vector((0,0.02,0))).length   # elbow farther from the spine than the shoulder
    sc_=abs(E.z-(SRf.z+0.03))+2*max(0.0,0.20-(E.y-SRf.y))
    if bestE is None or sc_<bestE[0]: bestE=(sc_,p.copy(),E.copy())
PD=Rz(-TW)@bestE[1]
print('ELBOWZMIN',round(min(ik(SRf,HRf,f1_*math.cos(2*math.pi*k/72)+f2_*math.sin(2*math.pi*k/72))[0].z for k in range(72)),3)); print('ELBOW',tuple(round(v,3) for v in bestE[2]),'shoulder',tuple(round(v,3) for v in SRf))
RK=[(0,REST['Right'],POLE_REST['Right']),
    (3,Vector((-0.56,-0.03,1.16)),Vector((-0.7,0.7,-0.3))),     # elbow leads out to the side
    (6,Vector((-0.42,-0.10,1.45)),Vector((-1.0,0.2,0.0))),      # then up, still outside the shoulder line
    (8,Vector((-0.24,-0.17,1.58)),Vector((-1.0,0.4,0.15))),     # hand comes in at head height, in front of the shoulder
    (10,'anchor',PD),(14,'anchor',PD),
    (17,'follow',PD),
    (20,Vector((-0.30,-0.15,1.55)),Vector((-1.0,0.4,0.15))),
    (23,Vector((-0.44,-0.08,1.40)),Vector((-1.0,0.2,0.0))),
    (26,Vector((-0.56,-0.03,1.16)),Vector((-0.7,0.7,-0.3))),
    (30,REST['Right'],POLE_REST['Right'])]
# left (bow) arm: direction from the shoulder in the torso frame + reach fraction
DFIN=(Rz(-TW)@TGT).normalized()
LK=[(0,None,1.0,POLE_REST['Left']),
    (4,Vector((0.88,0.02,-0.48)).normalized(),0.96,Vector((0.0,1.0,-0.4))),
    (8,DFIN,1.0,Rz(-TW)@Vector((0.8,0.0,-0.6))),(17,DFIN,1.0,Rz(-TW)@Vector((0.8,0.0,-0.6))),
    (23,Vector((0.88,0.02,-0.48)).normalized(),0.96,Vector((0.0,1.0,-0.4))),
    (30,None,1.0,POLE_REST['Left'])]
# bow upright at full draw: the straight bow arm's roll (its pole about the shoulder->hand axis) is chosen so the fist's grip axis
# (fist.json, the bow's long axis) stands as close to vertical as the wrist allows, elbow kept below the arm line.
FI=json.load(open(W_+'/fist.json')); GR=Matrix(FI['grip_rot_local'])
def bow_axis():
    upd(); return (PB[P_+'LeftHand'].matrix.to_3x3().normalized()@GR).col[2]
reset(); twist(TW); head_forward(); upd()
SLf=PB[P_+'LeftArm'].head.copy(); HLf=SLf+TGT*(L1+L2)
e1=TGT.orthogonal().normalized(); e2=TGT.cross(e1).normalized(); best=None
for k_ in range(72):
    ph=2*math.pi*k_/72; p=e1*math.cos(ph)+e2*math.sin(ph)
    if p.z>0.25: continue
    pose_arm('Left',HLf,p); ax=bow_axis(); tilt=math.degrees(math.acos(min(1,abs(ax.z))))
    if best is None or tilt<best[0]: best=(tilt,p.copy())
BOW_TILT_DRAW=best[0]; PLD=Rz(-TW)@best[1]
print('BOWPOLE',round(best[0],2),tuple(round(v,3) for v in best[1]))
LK=[(k[0],k[1],k[2],(PLD if k[0] in (8,17) else k[3])) for k in LK]
def cr(p0,p1,p2,p3,t):
    return 0.5*((2*p1)+(-p0+p2)*t+(2*p0-5*p1+4*p2-p3)*t*t+(-p0+3*p1-3*p2+p3)*t*t*t)
def seg(keys,f):
    i=max(k for k in range(len(keys)) if keys[k][0]<=f); i=min(i,len(keys)-2); f0,f1=keys[i][0],keys[i+1][0]
    return i,(f-f0)/(f1-f0)
def spline(keys,pts,f):
    i,t=seg(keys,f); n=len(pts)
    if (pts[i]-pts[i+1]).length<1e-6: return pts[i].copy()
    p0=pts[max(i-1,0)]; p3=pts[min(i+2,n-1)]
    # clamp tangents at holds so the hand does not overshoot into the body
    if (p0-pts[i]).length<1e-6 or i==0: p0=pts[i]-(pts[i+1]-pts[i])
    if (p3-pts[i+1]).length<1e-6 or i+2>=n: p3=pts[i+1]+(pts[i+1]-pts[i])
    return cr(p0,pts[i],pts[i+1],p3,ease(t)*0.5+t*0.5)
def lerpkey(keys,vals,f):
    i,t=seg(keys,f); t=ease(t); return vals[i].lerp(vals[i+1],t)
for f in range(31):
    a=TW*twist_w(f)
    reset(); twist(a); head_forward(); upd()
    hp=PB[P_+'Head'].head.copy(); SL=PB[P_+'LeftArm'].head.copy()
    # right
    pts=[]
    for k in RK:
        if isinstance(k[1],str):
            w=hp+ANCH+(PULL if k[1]=='follow' else Vector())
            pts.append(to_torso(w,a))
        else: pts.append(k[1].copy())
    HR=to_world(spline(RK,pts,f),a); pR=Rz(a)@lerpkey(RK,[k[2] for k in RK],f).normalized()
    # left: direction slerp in the torso frame
    SLt=to_torso(SL,a); d0=(REST['Left']-J['LeftArm']).normalized()
    dirs=[(k[1] if k[1] is not None else d0) for k in LK]; reach=[k[2] for k in LK]
    i,t=seg(LK,f); te=ease(t)
    qd=dirs[i].rotation_difference(dirs[i+1]); qd=Quaternion().slerp(qd,te)
    dL=qd@dirs[i]; rL=reach[i]+(reach[i+1]-reach[i])*te
    Lr=(REST['Left']-J['LeftArm']).length; bl=min(1.0,math.degrees(dL.angle(d0))/30.0) if dL.length>0 else 0.0
    HL=to_world(SLt+dL*(Lr+((L1+L2)*rL-Lr)*bl),a)
    pL=Rz(a)@lerpkey(LK,[k[3] for k in LK],f).normalized()
    pose_arm('Left',HL,pL); pose_arm('Right',HR,pR)
    # draw-hand wrist: near the jaw the fingers are bent to point forward along the arrow (not at the neck); flex only, no roll
    wD=ease((f-7)/3) if f<=10 else (1.0 if f<=17 else 1-ease((f-17)/3))
    if wD>0:
        upd(); Mh=PB[P_+'RightHand'].matrix.copy(); R3=Mh.to_3x3(); fd=R3.col[1].copy(); fh=Vector((fd.x,fd.y,0)).normalized()
        want=Vector((-0.25,-1.0,0)).normalized(); ang=fh.angle(want)*(1 if fh.cross(want).z>0 else -1)
        nd=Quaternion(Vector((0,0,1)),ang*wD)@fd; q=fd.rotation_difference(nd); setworld('RightHand',q.to_matrix()@R3)   # swing only (axis normal to the bone): no wrist roll
    head_forward(); key(f); roll_check('attack',f)
# frame-to-frame hand rotation jump (a flip would show as a big jump)
jumps={}
for clip in ('walk','attack'):
    R=[r for r in ROLL if r['clip']==clip]
    for s in ('Left','Right'):
        qs=[Quaternion(r[s+'_hand_q']) for r in R]
        jumps[f'{clip}_{s}_max_hand_step_deg']=round(max(min(a_,360-a_) for a_ in (math.degrees(qs[k].rotation_difference(qs[k+1]).angle) for k in range(len(qs)-1))),2)
summ={'bow_tilt_from_vertical_at_full_draw_deg':round(BOW_TILT_DRAW,2),'max_hinge_mismatch_deg':max(max(r['Left_hinge_mismatch_deg'],r['Right_hinge_mismatch_deg']) for r in ROLL),
      'max_abs_forearm_twist_deg':max(max(abs(r['Left_forearm_twist_deg']),abs(r['Right_forearm_twist_deg'])) for r in ROLL),
      'max_abs_hand_twist_deg':max(max(abs(r['Left_hand_twist_deg']),abs(r['Right_hand_twist_deg'])) for r in ROLL),**jumps}
reset(); arm.animation_data.action=A['rest']; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=W_+'/anim.blend',compress=True)
for r in ROLL: r.pop('Left_hand_q'); r.pop('Right_hand_q')
json.dump({'summary':summ,'frames':ROLL},open(W_+'/anim.json','w'),indent=1); print('ANIM',json.dumps(summ))
