"""Bonequill body actions on rig.blend -> anim.blend: rest (arms down, bow upright in the left hand), walk (19 cm, head forward,
tiny nod), attack (bow draw f0-10, loose f14, recover f30). Every frame solved explicitly; body bones only."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, math, json, numpy as np
from mathutils import Vector, Matrix, Quaternion
bpy.ops.wm.open_mainfile(filepath='/workspace/bonequill_gritty/work/rig.blend')
# ---- bow-hand fist (Derek 10-14): no finger bones, so sculpt the LeftHand fingers into a fist curled around a grip cylinder.
# Hand-bone space: y wrist->fingers, palm normal +x, knuckle width along z. Fingers past the knuckle line wrap on a circle
# around the grip axis (parallel to z) at C; the thumb is pushed out radially so it closes over the outside of the grip.
BOW_R=0.024; FIST={}
def sculpt_fist():
    arm_=bpy.data.objects['BONEQUILL_rig']; body=bpy.data.objects['BONEQUILL_body']; B=arm_.data.bones['mixamorig:LeftHand']
    Mi=np.array(B.matrix_local.inverted()); Mw=np.array(B.matrix_local); gi=body.vertex_groups['mixamorig:LeftHand'].index
    V=np.array([v.co[:] for v in body.data.vertices]); W=np.zeros(len(V))
    for v in body.data.vertices:
        for g in v.groups:
            if g.group==gi: W[v.index]=g.weight
    L=(np.c_[V,np.ones(len(V))]@Mi.T)[:,:3]; hand=W>0.5
    yk=0.085; band=hand&(np.abs(L[:,1]-yk)<0.012)&(L[:,0]<0.03); xf=float(np.median(L[band,0])); zc=float(np.median(L[hand&(L[:,1]>0.12),2]))
    th_=float(np.percentile(L[band,0],90)-np.percentile(L[band,0],10))/2   # finger half-thickness
    Rc=BOW_R+th_+0.006; C=np.array([xf+Rc,yk])
    fing=hand&(L[:,1]>yk)&~((L[:,0]>0.035)&(L[:,1]<0.135))                # fingers (thumb excluded: palm-side and short)
    thumb=hand&(L[:,0]>0.035)&(L[:,1]>0.02)&(L[:,1]<0.135)
    N=L.copy(); s_=L[fing,1]-yk; t_=L[fing,0]-xf; th=s_/Rc; r=Rc-t_
    wv=np.clip(W[fing],0,1)
    nx=C[0]-r*np.cos(th); ny=C[1]+r*np.sin(th)
    N[fing,0]=L[fing,0]+(nx-L[fing,0])*wv; N[fing,1]=L[fing,1]+(ny-L[fing,1])*wv
    d=N[:,:2]-C; dn=np.linalg.norm(d,axis=1); need=thumb&(dn<BOW_R+0.010)
    N[need,:2]=C+d[need]/np.maximum(dn[need],1e-6)[:,None]*(BOW_R+0.010)
    # anything else of the hand still inside the grip cylinder (palm pad) gets pushed to its surface
    d=N[:,:2]-C; dn=np.linalg.norm(d,axis=1); need=hand&(dn<BOW_R+0.006)&(np.abs(N[:,2]-zc)<0.09)
    N[need,:2]=C+d[need]/np.maximum(dn[need],1e-6)[:,None]*(BOW_R+0.006)
    Vn=(np.c_[N,np.ones(len(N))]@Mw.T)[:,:3]; body.data.vertices.foreach_set('co',Vn.ravel()); body.data.update()
    zt=float(N[hand&(np.linalg.norm(N[:,:2]-C,axis=1)<0.06),2].max())
    FIST.update(grip_local=[C[0],C[1],zc],rc=Rc,xf=xf,zc=zc,fist_top_local_z=zt,fingers=int(fing.sum()),thumb=int(thumb.sum()))
    json.dump(FIST,open('/workspace/bonequill_gritty/work/fist.json','w'))
sculpt_fist()
sc=bpy.context.scene; sc.render.fps=30; sc.frame_start=0; sc.frame_end=30
arm=bpy.data.objects['BONEQUILL_rig']; PB=arm.pose.bones; P_='mixamorig:'
for pb in PB: pb.rotation_mode='QUATERNION'
arm.animation_data_create()
def upd(): bpy.context.view_layer.update()
def reset():
    for pb in PB: pb.location=(0,0,0); pb.rotation_quaternion=(1,0,0,0); pb.scale=(1,1,1)
    upd()
def rot(n,axis,deg):
    pb=PB[P_+n]; R=pb.bone.matrix_local.to_3x3(); q=Quaternion(Vector(axis),math.radians(deg))
    pb.rotation_quaternion=(R.inverted()@q.to_matrix()@R).to_quaternion()@pb.rotation_quaternion
def aim(n,d):
    upd(); pb=PB[P_+n]; pb.rotation_quaternion=(1,0,0,0); upd()
    M=pb.matrix.copy(); q=M.to_3x3().col[1].normalized().rotation_difference(Vector(d).normalized())
    pb.matrix=Matrix.Translation(M.to_translation())@(q.to_matrix()@M.to_3x3()).to_4x4(); upd()
def setrot(n,q):
    upd(); pb=PB[P_+n]; M=pb.matrix.copy(); pb.matrix=Matrix.Translation(M.to_translation())@q.to_matrix().to_4x4(); upd()
def head_forward(nod=0.0):
    pb=PB[P_+'Head']; setrot('Head',pb.bone.matrix_local.to_3x3().to_quaternion()); rot('Head',(1,0,0),nod); upd()
def key(f):
    upd()
    for pb in PB: pb.keyframe_insert('rotation_quaternion',frame=f); pb.keyframe_insert('location',frame=f)
def act(name):
    a=bpy.data.actions.new(name); a.use_fake_user=True; arm.animation_data.action=a; return a
J={k:Vector(v) for k,v in json.load(open('/workspace/bonequill_gritty/work/joints.json')).items()}
ADD=math.degrees(math.atan((0.19-0.15)/(0.92-0.12)))
ARM_DOWN=6
def ik(S,H,L1,L2,pole):
    S,H=np.array(S),np.array(H); d=H-S; D=np.linalg.norm(d); D=min(D,L1+L2-1e-4); u=d/np.linalg.norm(d)
    a=(L1*L1-L2*L2+D*D)/(2*D); h=math.sqrt(max(L1*L1-a*a,0)); p=np.array(pole,float); p=p-(p@u)*u; p/=np.linalg.norm(p)
    E=S+u*a+p*h; return Vector(E-S),Vector(H-E)
LA=(J['LeftForeArm']-J['LeftArm']).length; LF=(J['LeftHand']-J['LeftForeArm']).length
# ---------- draw pose (reference f10): archer stance, torso turned side-on so the bow shoulder points at the target ----------
def legs(stance=True):
    for s,sg in (('Left',1),('Right',-1)): rot(s+'UpLeg',(0,1,0),ADD*sg); rot(s+'Foot',(0,1,0),-ADD*sg)
YAW=-55.0
def spine_yaw(deg):
    for n,k in (('Spine',0.3),('Spine1',0.35),('Spine2',0.35)): rot(n,(0,0,1),deg*k)
    rot('Neck',(0,0,1),-deg*0.5)   # neck takes half the counter-turn so the head band never twists hard
    upd()
HDX,HDZ=0.0,0.0
RDX4=Vector((0,0,0))
BDX,BDZ=0.119,-0.010
def draw_pose(pull=0.0,yaw=YAW):
    reset(); legs(); spine_yaw(yaw)
    S=PB[P_+'LeftArm'].head.copy(); D=(Vector((-0.07+BDX,-0.62,1.52+BDZ))-S).normalized()   # bow hand straight ahead of the face, so the arrow flies forward
    aim('LeftArm',D); aim('LeftForeArm',D)                    # bow arm straight out from the shoulder toward the target
    G=PB[P_+'LeftHand'].head.copy()
    H=Vector((-0.09+HDX-pull,-0.17+pull*0.4,1.52+HDZ))   # string hand at the right jaw; elbow out behind at shoulder height
    S2=PB[P_+'RightArm'].head; up,fo=ik(S2,H,LA,LF,(-1.0,0.35,-0.2)); aim('RightArm',up); aim('RightForeArm',fo)
draw_pose(); upd()
_M=PB[P_+'LeftHand'].matrix.to_3x3(); _y=_M.col[1].normalized(); _z=Vector((0,0,1)); _z=(_z-_z.dot(_y)*_y).normalized(); _x=_y.cross(_z)
RL=Matrix((_x,_y,_z)).transposed().to_quaternion()   # hand roll: knuckle width (local z) vertical so the grip sits across the palm, palm (+x) toward the midline      # left-hand armature rotation at full draw (bow upright)
def lefthand_hold(q=None): setrot('LeftHand',q if q is not None else RL)
# rest/walk bow carry (Derek 10-14): bow held lower, tipped 45 deg out and slightly back at the top so it clears the shoulder,
# shoulder guard, cloak and leg on every frame; left arm 8 deg further out and the forearm bent 30 deg more (hand forward of the hip)
IDLE_OUT,IDLE_FWD,IDLE_ABD,IDLE_ELB=45.0,-8.0,8.0,30.0
RL_IDLE=Quaternion((0,1,0),math.radians(IDLE_OUT))@Quaternion((1,0,0),math.radians(IDLE_FWD))@RL
def rest_arms(swL=0,swR=0):
    rot('LeftArm',(0,1,0),ARM_DOWN); rot('RightArm',(0,1,0),-ARM_DOWN)
    rot('LeftArm',(1,0,0),swL); rot('RightArm',(1,0,0),swR)
    rot('LeftForeArm',(1,0,0),-12); rot('RightForeArm',(1,0,0),-6)
    rot('LeftArm',(0,1,0),IDLE_ABD); rot('LeftForeArm',(1,0,0),-IDLE_ELB); upd()
    lefthand_hold(RL_IDLE)
A={}
A['rest']=act('BONEQUILL_rest')
for f in (0,30): reset(); legs(); rest_arms(); head_forward(); key(f)
A['walk']=act('BONEQUILL_walk')
for f in range(31):
    ph=2*math.pi*f/30; s,c=math.sin(ph),math.cos(ph); reset()
    PB[P_+'Hips'].location=(0,-0.015*abs(s),0)
    for side,sg in (('Left',1),('Right',-1)):
        sw=-17*s*sg; knee=5+36*max(0.0,c*sg)**1.3
        rot(side+'UpLeg',(0,1,0),ADD*sg); rot(side+'UpLeg',(1,0,0),sw); rot(side+'Leg',(1,0,0),knee)
        rot(side+'Foot',(0,1,0),-ADD*sg); rot(side+'Foot',(1,0,0),-(sw+knee)*0.6 if c*sg>0 else -sw*0.5)
    rest_arms(swL=5*s,swR=-10*s)
    rot('Spine1',(0,0,1),3*s); rot('Neck',(0,0,1),-1.5*s)
    head_forward(nod=0.9*(1-math.cos(2*ph))); key(f)      # tiny nod: 0..1.8 deg, peaks on the footfalls
A['attack']=act('BONEQUILL_attack')
def ease(t): t=max(0,min(1,t)); return t*t*(3-2*t)
# capture rest arm directions and the draw directions, then blend: f0 rest -> f6 raise/nock -> f10 full draw -> f14 loose -> f17 follow -> f30 rest
def dirs():
    upd(); return [PB[P_+n].matrix.to_3x3().col[1].normalized().copy() for n in ('LeftArm','LeftForeArm','RightArm','RightForeArm')]
reset(); legs(); rest_arms(); D0=dirs()
draw_pose(); D10=dirs()
draw_pose(yaw=YAW*0.6); 
S=PB[P_+'LeftArm'].head; Dn=Vector((0.15,-0.95,-0.25)).normalized(); aim('LeftArm',Dn); aim('LeftForeArm',Dn)
G=PB[P_+'LeftHand'].head.copy(); up,fo=ik(PB[P_+'RightArm'].head,Vector((-0.13,-0.27,1.42)),LA,LF,(-1.0,0.35,-0.2)); aim('RightArm',up); aim('RightForeArm',fo); D6=dirs()
draw_pose(pull=0.08); D16=dirs()
# ---- quiver draw (Derek 10-14): f0 rest -> f2 lift out to the side -> f4 grab an arrow at the quiver mouth over the right
# shoulder (quiver rides Spine2) -> f6 pull it up out of the quiver -> f8 hand up over the right shoulder, forearm vertical ->
# f10 nock -> f14 full draw -> f17 loose -> f20 recoil -> f30 rest. Elbow stays high and out so the arm never crosses the hood,
# cloak or ribcage; the f8 key keeps the forearm from swinging through its antipode (no roll flip).
QTOP=Vector((-0.15,0.317,1.52)); QBOT=Vector((0.07,0.317,1.02)); QAX=(QTOP-QBOT).normalized()
QGRAB=QTOP+QAX*0.12
RH={2:(Vector((-0.46,-0.04,1.50)),(-1,0.0,-0.3)),4:(Vector((-0.29,0.23,1.62)),(-1,0.2,0.1)),
    6:(Vector((-0.38,0.20,1.80)),(-1,0.2,0.1)),8:(Vector((-0.50,-0.12,1.62)),(-1,0.0,0.3))}
def rpose(H,pole):
    reset(); legs(); rest_arms(); S2=PB[P_+'RightArm'].head; up,fo=ik(S2,H,LA,LF,pole); aim('RightArm',up); aim('RightForeArm',fo); return dirs()[2:]
DR={k:rpose(*v) for k,v in RH.items()}
KL=[(0,D0[:2]),(10,D6[:2]),(14,D10[:2]),(17,D10[:2]),(20,D16[:2]),(30,D0[:2])]
KR=[(0,D0[2:]),(2,DR[2]),(4,DR[4]),(6,DR[6]),(8,DR[8]),(10,D6[2:]),(14,D10[2:]),(17,D10[2:]),(20,D16[2:]),(30,D0[2:])]
KY=[(0,0.0),(8,0.0),(10,YAW*0.6),(14,YAW),(20,YAW),(30,0.0)]
def untwist(n):
    # drop the forearm's twist about its own axis relative to the upper arm (swing only): the elbow stays a hinge and the
    # forearm can never roll-flip, whatever path the aim directions take
    upd(); pb=PB[P_+n]; q=pb.rotation_quaternion.copy(); tw=Quaternion((q.w,0,q.y,0)); 
    if tw.magnitude>1e-6: tw.normalize(); pb.rotation_quaternion=q@tw.inverted()
    upd()
def samp(K,f):
    i=max(j for j in range(len(K)) if K[j][0]<=f); j=min(i+1,len(K)-1); f0,f1=K[i][0],K[j][0]; t=ease((f-f0)/(f1-f0)) if f1>f0 else 0
    a,b=K[i][1],K[j][1]
    if isinstance(a,float): return a+(b-a)*t
    return [a[k].lerp(b[k],t).normalized() for k in range(len(a))]
for f in range(31):
    reset(); legs(); spine_yaw(samp(KY,f)); dl=samp(KL,f); dr=samp(KR,f)
    for n,d in zip(('LeftArm','LeftForeArm','RightArm','RightForeArm'),dl+dr): aim(n,d)
    untwist('RightForeArm')
    tb=ease(min(1.0,f/10.0)) if f<=10 else (1.0 if f<=20 else 1.0-ease((f-20)/10.0)); lefthand_hold(RL_IDLE.slerp(RL,tb)); head_forward(); key(f)
reset(); arm.animation_data.action=A['rest']; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath='/workspace/bonequill_gritty/work/anim.blend',compress=True); bpy.ops.wm.save_as_mainfile(filepath='/workspace/bonequill_gritty/work/anim_nq.blend',compress=True,copy=True); print('ANIM ok ADD',ADD)
