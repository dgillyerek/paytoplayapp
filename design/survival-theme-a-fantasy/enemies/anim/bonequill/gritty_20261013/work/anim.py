"""Bonequill body actions on rig.blend -> anim.blend: rest (arms down, bow upright in the left hand), walk (19 cm, head forward,
tiny nod), attack (bow draw f0-10, loose f14, recover f30). Every frame solved explicitly; body bones only."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, math, json, numpy as np
from mathutils import Vector, Matrix, Quaternion
bpy.ops.wm.open_mainfile(filepath='/workspace/bonequill_gritty/work/rig.blend')
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
# ---------- draw pose (reference f10) ----------
def legs(stance=True):
    for s,sg in (('Left',1),('Right',-1)): rot(s+'UpLeg',(0,1,0),ADD*sg); rot(s+'Foot',(0,1,0),-ADD*sg)
def draw_pose(pull=0.0):
    reset(); legs()
    aim('LeftArm',(-0.35,-0.94,0.02)); aim('LeftForeArm',(-0.35,-0.94,0.02))
    S=PB[P_+'RightArm'].head; H=Vector((-0.02,-0.10+pull,1.45))
    up,fo=ik(S,H,LA,LF,(-1,0.6,0.25)); aim('RightArm',up); aim('RightForeArm',fo)
draw_pose(); upd()
RL=PB[P_+'LeftHand'].matrix.to_3x3().to_quaternion()      # left-hand armature rotation at full draw (bow upright)
def lefthand_hold(): setrot('LeftHand',RL)
def rest_arms(swL=0,swR=0):
    rot('LeftArm',(0,1,0),ARM_DOWN); rot('RightArm',(0,1,0),-ARM_DOWN)
    rot('LeftArm',(1,0,0),swL); rot('RightArm',(1,0,0),swR)
    rot('LeftForeArm',(1,0,0),-12); rot('RightForeArm',(1,0,0),-6)
    lefthand_hold()
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
reset(); legs(); aim('LeftArm',(-0.30,-0.95,-0.05)); aim('LeftForeArm',(-0.30,-0.95,-0.05))
S=PB[P_+'RightArm'].head; up,fo=ik(S,Vector((0.03,-0.33,1.42)),LA,LF,(-1,0.2,0.1)); aim('RightArm',up); aim('RightForeArm',fo); D6=dirs()
draw_pose(pull=0.08); D16=dirs()
KEYS=[(0,D0),(6,D6),(10,D10),(14,D10),(17,D16),(30,D0)]
for f in range(31):
    i=max(j for j in range(len(KEYS)) if KEYS[j][0]<=f); j=min(i+1,len(KEYS)-1); f0,f1=KEYS[i][0],KEYS[j][0]
    t=ease((f-f0)/(f1-f0)) if f1>f0 else 0
    reset(); legs()
    for k,n in enumerate(('LeftArm','LeftForeArm','RightArm','RightForeArm')): aim(n,KEYS[i][1][k].lerp(KEYS[j][1][k],t).normalized())
    lefthand_hold(); head_forward(); key(f)
reset(); arm.animation_data.action=A['rest']; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath='/workspace/bonequill_gritty/work/anim.blend',compress=True); print('ANIM ok ADD',ADD)
