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
REST={'Left':Vector((0.365,-0.01,0.95)),'Right':Vector((-0.365,-0.01,0.95))}
POLE_REST={'Left':Vector((0.25,1,0)),'Right':Vector((-0.25,1,0))}
def rest_arms(swL=0.0,swR=0.0):
    for s,sw in (('Left',swL),('Right',swR)):
        H=REST[s]+Vector((0,-sw,0.25*abs(sw)))   # swing forward (-Y) lifts the hand a little
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
TW=-70.0
TGT=Vector((0.0,-1.0,0.05)).normalized()
def draw_targets(tw_w,pull=0.0,stage='draw'):
    upd(); hp=PB[P_+'Head'].head.copy(); SL=PB[P_+'LeftArm'].head.copy()
    if stage=='draw':
        HL=SL+TGT*(L1+L2); HR=hp+Vector((-0.10,0.02,0.04))+Vector((-0.03*pull/0.06,0.06*pull/0.06,0.0))*(pull>0)
    else:  # nock: bow half raised in front, string hand at the bow
        HL=SL+Vector((-0.10,-0.93,-0.30)).normalized()*(L1+L2)
        HR=HL+Vector((-0.06,0.24,0.02))
    return HL,HR
POLE_DRAW={'Left':Vector((0.8,0.0,-0.6)),'Right':Vector((-0.8,0.3,-0.2))}
# keys: frame -> (twist weight, stage, pull)
KEYS=[(0,'rest'),(5,'nock'),(10,'draw'),(14,'draw'),(17,'follow'),(22,'lower'),(30,'rest')]
def targets_for(stage,hw):
    if stage=='rest': return REST['Left'].copy(),REST['Right'].copy(),POLE_REST['Left'],POLE_REST['Right']
    if stage=='nock': HL,HR=draw_targets(hw,0,'nock'); return HL,HR,POLE_DRAW['Left'],Vector((-0.3,0.6,-0.8))
    if stage=='draw': HL,HR=draw_targets(hw,0); return HL,HR,POLE_DRAW['Left'],POLE_DRAW['Right']
    if stage=='follow': HL,HR=draw_targets(hw,0.06); return HL,HR,POLE_DRAW['Left'],POLE_DRAW['Right']
    if stage=='lower':
        upd(); SL=PB[P_+'LeftArm'].head.copy()
        return SL+Vector((0.15,-0.75,-0.65)).normalized()*(L1+L2), Vector((-0.34,-0.16,1.12)), Vector((0.8,0.3,-0.5)), Vector((-0.6,0.8,0))
def twist_w(f):
    return ease(f/5) if f<=5 else (1.0 if f<=17 else 1-ease((f-17)/13))
for f in range(31):
    i=max(j for j in range(len(KEYS)) if KEYS[j][0]<=f); j=min(i+1,len(KEYS)-1); f0,f1=KEYS[i][0],KEYS[j][0]
    t=((f-f0)/(f1-f0) if KEYS[i][1]=='nock' else ease((f-f0)/(f1-f0))) if f1>f0 else 0.0
    hw=twist_w(f)
    reset(); twist(TW*hw); head_forward()
    a=targets_for(KEYS[i][1],hw); b=targets_for(KEYS[j][1],hw)
    HL=a[0].lerp(b[0],t); HR=a[1].lerp(b[1],t); pL=a[2].lerp(b[2],t); pR=a[3].lerp(b[3],t)
    pose_arm('Left',HL,pL); pose_arm('Right',HR,pR); head_forward(); key(f); roll_check('attack',f)
# frame-to-frame hand rotation jump (a flip would show as a big jump)
jumps={}
for clip in ('walk','attack'):
    R=[r for r in ROLL if r['clip']==clip]
    for s in ('Left','Right'):
        qs=[Quaternion(r[s+'_hand_q']) for r in R]
        jumps[f'{clip}_{s}_max_hand_step_deg']=round(max(math.degrees(qs[k].rotation_difference(qs[k+1]).angle) for k in range(len(qs)-1)),2)
summ={'max_hinge_mismatch_deg':max(max(r['Left_hinge_mismatch_deg'],r['Right_hinge_mismatch_deg']) for r in ROLL),
      'max_abs_forearm_twist_deg':max(max(abs(r['Left_forearm_twist_deg']),abs(r['Right_forearm_twist_deg'])) for r in ROLL),
      'max_abs_hand_twist_deg':max(max(abs(r['Left_hand_twist_deg']),abs(r['Right_hand_twist_deg'])) for r in ROLL),**jumps}
reset(); arm.animation_data.action=A['rest']; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=W_+'/anim.blend',compress=True)
for r in ROLL: r.pop('Left_hand_q'); r.pop('Right_hand_q')
json.dump({'summary':summ,'frames':ROLL},open(W_+'/anim.json','w'),indent=1); print('ANIM',json.dumps(summ))
