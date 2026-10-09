"""Lyra fitted walk v2 (2026-10-08) on Derek's rig. In-place, f0..f30 @30fps (f30 == f0).
Same foot timing as the 10-05 walk it replaces: right foot forward/contact ~f8, left ~f23.
Authored as world-space targets -> per-bone basis (XYZ euler, every frame).
Legs: two-bone IK to planted, heel->flat->toe rolling feet (rigid foot+toes, pivots on mesh sole points).
Arms: left swings forward/back in the sagittal plane at a fixed small abduction; right (staff) is IK'd to
a grip that keeps the staff near vertical and swings it gently. Head held steady."""
import bpy, math, json, sys
import numpy as np
from mathutils import Matrix, Vector, Quaternion, Euler
L='/workspace/lyra-onefile/design/survival-theme-a-fantasy/heroes/anim/lyra'
OUTJSON=sys.argv[sys.argv.index('--')+1]
sc=bpy.context.scene
arm=bpy.data.objects['LYRA_tighten_rig']; body=bpy.data.objects['LYRA_tighten_body']; staff=bpy.data.objects['LYRA_staff']
if arm.mode!='OBJECT': bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='OBJECT')
assert arm.matrix_world==Matrix.Identity(4)
P='mixamorig:'
B={b.name[len(P):]:b for b in arm.data.bones}
REST={n:b.matrix_local.copy() for n,b in B.items()}
HEAD={n:b.head_local.copy() for n,b in B.items()}; TAIL={n:b.tail_local.copy() for n,b in B.items()}
def R3(m): return m.to_3x3().normalized()
def rot_between(a,b):
    a=a.normalized(); b=b.normalized(); return a.rotation_difference(b).to_matrix()
def Rax(axis,deg): return Matrix.Rotation(math.radians(deg),3,Vector(axis))
X=Vector((1,0,0)); Y=Vector((0,1,0)); Z=Vector((0,0,1)); FWD=Vector((0,-1,0))
NF=30
def cyc(f,f0): return math.cos(2*math.pi*(f-f0)/NF)
def smooth(t): t=max(0.0,min(1.0,t)); return t*t*(3-2*t)
# ---------------- mesh sole points (rest) ----------------
co=np.array([v.co[:] for v in body.data.vertices])
vg={g.name:g.index for g in body.vertex_groups}
W=np.zeros((len(co),len(vg)),dtype=np.float32)
for v in body.data.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
SOLE={}
for s in ('Left','Right'):
    fv=co[(W[:,vg[P+s+'Foot']]+W[:,vg[P+s+'ToeBase']])>0.5]
    zmin=fv[:,2].min(); bot=fv[fv[:,2]<zmin+0.02]
    heel=Vector(bot[bot[:,1].argmax()]); toe=Vector(fv[fv[:,1].argmin()])
    heel.z=zmin; toe.z=zmin
    fdir=Vector((toe.x-heel.x,toe.y-heel.y,0)).normalized()
    SOLE[s]=dict(z=float(zmin),heel=heel,toe=toe,fdir=fdir,lat=(-fdir).cross(Z).normalized())
CLEAR=0.003
# staff geometry
sco=np.array([v.co[:] for v in staff.data.vertices]); STAFF_ZMIN=float(sco[:,2].min())
smeta=json.load(open(f'{L}/blender_rig_tighten_20261008/props/LYRA_staff_meta.json'))
OFF=Matrix(smeta['offset_in_hand_blender']); RW=Matrix(smeta['rest_world_blender'])
# ---------------- timing ----------------
CONTACT={'Right':8.0,'Left':23.0}; STANCE=18.0; SWING=NF-STANCE
Y_FRONT=-0.24; Y_BACK=0.28          # ankle y at contact / toe-off (in place treadmill)
P_HEEL=-12.0; P_TOE=24.0; LIFT=0.085
def foot_state(s,f):
    ph=(f-CONTACT[s])%NF
    so=SOLE[s]; rest_ank=HEAD[s+'Foot']
    if ph<STANCE:
        u=ph/STANCE
        gy=Y_FRONT+(Y_BACK-Y_FRONT)*u
        if u<0.14: pitch=P_HEEL*(1-smooth(u/0.14))
        elif u<0.58: pitch=0.0
        else: pitch=P_TOE*smooth((u-0.58)/0.42)**1.3
        lift=0.0
    else:
        u=(ph-STANCE)/SWING
        v=(Y_BACK-Y_FRONT)/STANCE*SWING        # treadmill speed in swing-length units
        h00=2*u**3-3*u**2+1; h10=u**3-2*u**2+u; h01=-2*u**3+3*u**2; h11=u**3-u**2
        gy=h00*Y_BACK+h01*Y_FRONT+(h10+h11)*v*0.6
        # pitch: toe-off value -> slightly toes-up before contact
        pitch=P_TOE*(1-smooth(u/0.55)) + P_HEEL*smooth((u-0.45)/0.55)
        lift=LIFT*math.sin(math.pi*u)**1.2*(1.0 if u<0.5 else 1.0)
    G=Vector((rest_ank.x, gy, 0.0))
    piv_rest=so['toe'] if pitch>0 else so['heel']
    R=Rax(so['lat'],pitch)
    piv=Vector((G.x+piv_rest.x-rest_ank.x, G.y+piv_rest.y-rest_ank.y, CLEAR+lift))
    ank=piv+R@(rest_ank-piv_rest)
    return ank,R,pitch,ph<STANCE
# ---------------- body ----------------
def body_params(f):
    bob=0.011*math.cos(2*math.pi*(f-0.5)/15.0)          # high at passing (f0.5, f15.5), low at contact (f8, f23)
    return dict(dz=-0.022+bob, dx=0.016*cyc(f,2.0),       # shift over the stance leg (L mid-stance ~f2)
        yawP=-5.0*cyc(f,23.0),                            # left hip forward when left leg forward
        rollP=-2.5*cyc(f,2.0),                            # swing-side hip drops
        yawC=4.5*cyc(f,23.0))                             # chest counter-rotates
def ik2(a,t,l1,l2,pole):
    d=t-a; dist=d.length; dist=min(dist,(l1+l2)*0.9995); dn=d.normalized()
    x=(l1*l1-l2*l2+dist*dist)/(2*dist); h=math.sqrt(max(l1*l1-x*x,0.0))
    pv=(pole-dn*pole.dot(dn)).normalized()
    knee=a+dn*x+pv*h; end=a+dn*dist
    return knee,end
def L_(n1,n2): return (HEAD[n2]-HEAD[n1]).length
LEN={s:(L_(s+'UpLeg',s+'Leg'),L_(s+'Leg',s+'Foot')) for s in ('Left','Right')}
LENA={'Right':(L_('RightArm','RightForeArm'),L_('RightForeArm','RightHand'))}
order=[b.name[len(P):] for b in arm.data.bones]   # parents before children
PARENT={n:(B[n].parent.name[len(P):] if B[n].parent else None) for n in B}
def pose_frame(f, hips_extra_dz=0.0):
    bp=body_params(f); D={}   # desired armature-space matrices
    def place(n,Rw):
        p=PARENT[n]
        head=(D[p]@REST[p].inverted()@REST[n]).translation if p else HEAD[n]
        m=Rw.to_4x4(); m.translation=head; D[n]=m; return m
    # hips
    Rh=Rax(Z,bp['yawP'])@Rax(Y,bp['rollP'])@R3(REST['Hips'])
    m=Rh.to_4x4(); m.translation=HEAD['Hips']+Vector((bp['dx'],0,bp['dz']+hips_extra_dz)); D['Hips']=m
    dP=Rax(Z,bp['yawP'])@Rax(Y,bp['rollP'])
    spine={'Spine':(0.4*bp['yawP'],1.0,0.5*bp['rollP']),'Spine1':(0.5*bp['yawC'],2.0,0.0),'Spine2':(bp['yawC'],3.0,-0.3*bp['rollP']),
           'Neck':(0.4*bp['yawC'],1.5,0.0),'Head':(0.0,0.0,0.0)}
    for n,(yw,lean,rl) in spine.items():
        place(n,Rax(Z,yw)@Rax(X,lean)@Rax(Y,rl)@R3(REST[n]))
    dC=R3(D['Spine2'])@R3(REST['Spine2']).inverted()
    for s in ('Left','Right'): place(s+'Shoulder',dC@R3(REST[s+'Shoulder']))
    # legs
    info={}
    for s in ('Left','Right'):
        hip=(D['Hips']@REST['Hips'].inverted()@REST[s+'UpLeg']).translation
        ank,Rf,pitch,stance=foot_state(s,f)
        pole=(SOLE[s]['fdir']*1.0+Z*0.15).normalized()
        knee,end=ik2(hip,ank,*LEN[s],pole)
        dU=rot_between(R3(REST[s+'UpLeg'])@Vector((0,1,0)),knee-hip)
        place(s+'UpLeg',dU@R3(REST[s+'UpLeg']))
        shin_rest=dU@(R3(REST[s+'Leg'])@Vector((0,1,0)))
        dL=rot_between(shin_rest,end-knee)@dU
        place(s+'Leg',dL@R3(REST[s+'Leg']))
        place(s+'Foot',Rf@R3(REST[s+'Foot']))
        place(s+'ToeBase',Rf@R3(REST[s+'ToeBase']))
        info[s]=dict(reach=(ank-hip).length/sum(LEN[s]),miss=(end-ank).length,stance=stance)
    # left arm: sagittal swing, fixed small abduction, more elbow bend forward
    phi=3.0+19.0*cyc(f,9.0)       # +forward deg; left arm forward when right leg forward (f8)
    a=math.radians(11.0); ph=math.radians(phi)
    d=Vector((math.sin(a),-math.sin(ph)*math.cos(a),-math.cos(ph)*math.cos(a)))
    up_rest=R3(REST['LeftArm'])@Vector((0,1,0))
    dU=rot_between(up_rest,d)
    place('LeftArm',dU@R3(REST['LeftArm']))
    fwdness=(phi-(3.0-19.0))/38.0
    bend=math.radians(12.0+20.0*fwdness)
    perp=(FWD-d*FWD.dot(d)).normalized()
    fd=(d*math.cos(bend)+perp*math.sin(bend))
    fd=(Rax(perp.cross(d).normalized() if False else Z,0)@fd)
    fd=(fd+X*math.tan(math.radians(5.0))).normalized()      # forearm a touch further out than the upper arm (clears the hip)
    fore_rest=dU@(R3(REST['LeftForeArm'])@Vector((0,1,0)))
    dF=rot_between(fore_rest,fd)@dU
    place('LeftForeArm',dF@R3(REST['LeftForeArm']))
    place('LeftHand',dF@Rax(X,-4.0*cyc(f,9.0))@R3(REST['LeftHand']) if False else dF@R3(REST['LeftHand']))
    # right arm: staff hand. Staff stays near vertical, swings gently forward/back (forward when left leg forward, f23)
    th=4.0*cyc(f,24.0)
    S_rot=Rax(X,th)@R3(RW)
    Hrot=S_rot@R3(OFF).inverted()
    wrist=Vector((-0.440,-0.140,1.272))+Vector((0,-0.028*cyc(f,24.0),0.008*cyc(f,24.0*2)))
    # keep the staff foot off the ground
    Hm=Hrot.to_4x4(); Hm.translation=wrist
    foot=(Hm@OFF)@Vector((0,0,STAFF_ZMIN))
    if foot.z<0.03: wrist.z+=0.03-foot.z
    sh=(D['RightShoulder']@REST['RightShoulder'].inverted()@REST['RightArm']).translation
    pole=Vector((0.15,0.30,-1.0)).normalized()
    elbow,end=ik2(sh,wrist,*LENA['Right'],pole)
    dU=rot_between(R3(REST['RightArm'])@Vector((0,1,0)),elbow-sh)
    place('RightArm',dU@R3(REST['RightArm']))
    dF=rot_between(dU@(R3(REST['RightForeArm'])@Vector((0,1,0))),end-elbow)@dU
    place('RightForeArm',dF@R3(REST['RightForeArm']))
    place('RightHand',Hrot)
    rigid_hand=dF@R3(REST['RightHand'])
    info['wrist_bend_deg']=math.degrees((rigid_hand@Vector((0,1,0))).angle(Hrot@Vector((0,1,0))))
    info['staff_foot_z']=((D['RightHand'].normalized()@OFF)@Vector((0,0,STAFF_ZMIN))).z
    info['wrist_miss']=(end-wrist).length
    return D,info
# reach check -> extra hip drop per frame (smoothed, cyclic)
extra=[0.0]*NF
for it in range(3):
    need=[]
    for f in range(NF):
        D,info=pose_frame(f,extra[f])
        r=max(info[s]['reach'] for s in ('Left','Right'))
        need.append(extra[f]-max(0.0,(r-0.985))*sum(LEN['Left']))
    k=[0.25,0.5,0.25]
    extra=[min(need[f],sum(k[j]*need[(f+j-1)%NF] for j in range(3))) for f in range(NF)]
# ---------------- key ----------------
ad=arm.animation_data
old=bpy.data.actions['LYRA_tighten_walk']; old.name='LYRA_tighten_walk__prev'
act=bpy.data.actions.new('LYRA_tighten_walk'); act.use_fake_user=True
ad.action=act
for pb in arm.pose.bones: pb.rotation_mode='XYZ'
prevE={}
REPORT={'frames':[]}
basisF={}
for f in range(NF):
    D,info=pose_frame(f,extra[f]); basisF[f]={}
    for n in order:
        p=PARENT[n]
        if p: bm=REST[n].inverted()@REST[p]@D[p].inverted()@D[n]
        else: bm=REST[n].inverted()@D[n]
        basisF[f][n]=bm
    REPORT['frames'].append(dict(f=f,reach=[round(info[s]['reach'],4) for s in ('Left','Right')],leg_miss=[round(info[s]['miss'],5) for s in ('Left','Right')],
        wrist_bend=round(info['wrist_bend_deg'],1),staff_foot_z=round(info['staff_foot_z'],3),wrist_miss=round(info['wrist_miss'],5),extra_dz=round(extra[f],4)))
basisF[NF]=basisF[0]
for f in range(NF+1):
    for n in order:
        pb=arm.pose.bones[P+n]; bm=basisF[f][n]
        loc,rot,scl=bm.decompose()
        e=rot.to_euler('XYZ',prevE[n]) if n in prevE else rot.to_euler('XYZ')
        if f==NF: e=prevE0[n]
        pb.location=loc if n=='Hips' else Vector((0,0,0)) if B[n].use_connect else loc
        pb.rotation_euler=e; pb.scale=(1,1,1)
        for path in ('location','rotation_euler','scale'): pb.keyframe_insert(path,frame=f,group=pb.name)
        prevE[n]=e.copy()
    if f==0: prevE0={k:v.copy() for k,v in prevE.items()}
bpy.data.actions.remove(old)
ad.action=act
sc.frame_start=0; sc.frame_end=30; sc.frame_set(0)
json.dump(REPORT,open(OUTJSON,'w'),indent=0)
print('WALK authored', len(order),'bones')
