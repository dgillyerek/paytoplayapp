"""Rest / walk / attack actions on rig.blend -> anim.blend. Every frame is solved explicitly and keyed on all bones."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, math, json, numpy as np
from mathutils import Vector, Matrix, Quaternion, Euler
bpy.ops.wm.open_mainfile(filepath='/workspace/oakenshield_fresh/work/rig.blend')
sc=bpy.context.scene; sc.render.fps=30; sc.frame_start=0; sc.frame_end=30
arm=bpy.data.objects['OAKENSHIELD_rig']; PB=arm.pose.bones; P_='mixamorig:'
for pb in PB: pb.rotation_mode='QUATERNION'
if arm.animation_data is None: arm.animation_data_create()
def upd(): bpy.context.view_layer.update()
def reset():
    for pb in PB: pb.location=(0,0,0); pb.rotation_quaternion=(1,0,0,0); pb.scale=(1,1,1)
    upd()
def rot(n,axis,deg):
    """delta rotation about an armature axis, applied in the frame the parent carries"""
    pb=PB[P_+n]; R=pb.bone.matrix_local.to_3x3()
    q=Quaternion(Vector(axis),math.radians(deg)); pb.rotation_quaternion=(R.inverted()@q.to_matrix()@R).to_quaternion()@pb.rotation_quaternion
def aim(n,d,twist=0):
    upd(); pb=PB[P_+n]; pb.rotation_quaternion=(1,0,0,0); upd()
    M=pb.matrix.copy(); y=M.to_3x3().col[1].normalized(); q=y.rotation_difference(Vector(d).normalized())
    R=q.to_matrix()@M.to_3x3()
    if twist: R=Quaternion(Vector(d).normalized(),math.radians(twist)).to_matrix()@R
    pb.matrix=Matrix.Translation(M.to_translation())@R.to_4x4(); upd()
def head_forward(k=1.0):
    upd(); pb=PB[P_+'Head']; pb.rotation_quaternion=(1,0,0,0); upd()
    M=pb.matrix.copy(); R0=pb.bone.matrix_local.to_3x3()
    Rt=M.to_3x3().to_quaternion().slerp(R0.to_quaternion(),k).to_matrix()
    pb.matrix=Matrix.Translation(M.to_translation())@Rt.to_4x4(); upd()
def key(f):
    upd()
    for pb in PB:
        pb.keyframe_insert('rotation_quaternion',frame=f); pb.keyframe_insert('location',frame=f)
def new_action(name):
    a=bpy.data.actions.new(name); a.use_fake_user=True; arm.animation_data.action=a; return a
ARM_DOWN=20
def rest_arms(swingL=0,swingR=0,elbow=8):
    rot('LeftArm',(0,1,0),ARM_DOWN); rot('RightArm',(0,1,0),-ARM_DOWN)
    rot('LeftArm',(1,0,0),swingL); rot('RightArm',(1,0,0),swingR)
    rot('LeftForeArm',(1,0,0),-elbow); rot('RightForeArm',(1,0,0),-elbow)
# ---------- rest ----------
A={}
A['rest']=new_action('OAKENSHIELD_rest')
for f in (0,30):
    reset(); rest_arms(); head_forward(); key(f)
# ---------- walk: in place, 1 s, ankles 19 cm apart, head forward ----------
A['walk']=new_action('OAKENSHIELD_walk')
ADD=math.degrees(math.atan((0.13-0.095)/0.825))
for f in range(31):
    ph=2*math.pi*f/30; s,c=math.sin(ph),math.cos(ph)
    reset()
    PB[P_+'Hips'].location=(0,-0.018*abs(s),0)          # Hips local Y = up: dip at double support
    for side,sg in (('Left',1),('Right',-1)):
        sw=-18*s*sg                                     # thigh swing (negative = forward)
        fwd=(c*sg>0)                                    # leg travelling forward = swing phase
        knee=6+(38*max(0.0,c*sg)**1.3)
        rot(side+'UpLeg',(0,1,0),ADD*sg); rot(side+'UpLeg',(1,0,0),sw)
        rot(side+'Leg',(1,0,0),knee)
        rot(side+'Foot',(1,0,0),-(sw+knee)*0.6 if fwd else -sw*0.5)
    rest_arms(swingL=14*s,swingR=-14*s,elbow=10+4*abs(s))
    rot('Spine1',(0,0,1),4*s); rot('Neck',(0,0,1),-2*s)
    head_forward(); key(f)
# ---------- attack: thorn spear (beats from #48: conjure f3-8, cock f10, throw f14, follow f20, recover f30) ----------
A['attack']=new_action('OAKENSHIELD_attack')
def ease(t): t=max(0,min(1,t)); return t*t*(3-2*t)
def nl(a,b,t): v=Vector(a).lerp(Vector(b),t); return v.normalized()
# armature-space directions; her right is -X, forward is -Y
KEYS=[ # frame, spine yaw (deg, + = to her left), lean (deg forward), R upper, R fore, L upper, L fore
 (0, 0,0, None,None,None,None),
 (5, -6,0, (-0.55,-0.45,-0.70),(-0.25,-0.92,0.30),None,None),
 (10,-26,-4,(-0.93,0.20,0.30),(-0.10,0.30,0.95),(0.35,-0.90,0.10),(0.25,-0.96,0.10)),
 (14,14,10,(-0.30,-0.70,0.65),(-0.12,-0.97,0.20),(0.55,0.15,-0.80),(0.45,-0.30,-0.85)),
 (20,10,8,(-0.10,-0.72,-0.68),(0.20,-0.80,-0.56),(0.60,0.10,-0.80),(0.40,-0.20,-0.90)),
 (30,0,0, None,None,None,None)]
def rest_dirs():
    reset(); rest_arms(); upd()
    return {n:(PB[P_+n].matrix.to_3x3().col[1]).normalized() for n in ('RightArm','RightForeArm','LeftArm','LeftForeArm')}
RD=rest_dirs()
for k in range(len(KEYS)):
    KEYS[k]=tuple(list(KEYS[k][:3])+[Vector(KEYS[k][3+i]) if KEYS[k][3+i] else RD[n] for i,n in enumerate(('RightArm','RightForeArm','LeftArm','LeftForeArm'))])
for f in range(31):
    i=max(j for j in range(len(KEYS)) if KEYS[j][0]<=f); j=min(i+1,len(KEYS)-1)
    f0,f1=KEYS[i][0],KEYS[j][0]; t=ease((f-f0)/(f1-f0)) if f1>f0 else 0
    yaw=KEYS[i][1]+(KEYS[j][1]-KEYS[i][1])*t; lean=KEYS[i][2]+(KEYS[j][2]-KEYS[i][2])*t
    reset()
    for b,w in (('Spine',0.3),('Spine1',0.35),('Spine2',0.35)): rot(b,(0,0,1),yaw*w); rot(b,(1,0,0),lean*w)
    rot('Neck',(0,0,1),-yaw*0.5)
    for n_,ix in (('RightArm',3),('RightForeArm',4),('LeftArm',5),('LeftForeArm',6)):
        aim(n_,nl(KEYS[i][ix],KEYS[j][ix],t))
    head_forward(); key(f)

# hand hold: RightHand keeps its f10 (cocked) armature-space rotation f8-f14 so the spear stays pointing forward while the arm whips over
arm.animation_data.action=A['attack']; sc.frame_set(10); upd(); Rref=PB[P_+'RightHand'].matrix.to_3x3().to_quaternion()
def holdw(f): return ease((f-5)/3) if f<8 else (1.0 if f<=14 else 1-ease((f-14)/4))
for f in range(5,19):
    sc.frame_set(f); upd(); pb=PB[P_+'RightHand']; M=pb.matrix.copy()
    q=M.to_3x3().to_quaternion().slerp(Rref,holdw(f)); pb.matrix=Matrix.Translation(M.to_translation())@q.to_matrix().to_4x4(); upd()
    pb.keyframe_insert('rotation_quaternion',frame=f)
# keep the body square at rest; leave the rest action on
reset(); arm.animation_data.action=A['rest']; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath='/workspace/oakenshield_fresh/work/anim.blend',compress=True)
print('ANIM ok', ADD)
