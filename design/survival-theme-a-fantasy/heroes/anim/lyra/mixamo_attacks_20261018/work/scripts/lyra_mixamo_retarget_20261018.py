"""Lyra attack 2/3: retarget a Mixamo clip (auto-rig of LYRA.fbx, same 20 mixamorig names) onto the approved
LYRA_tighten_rig by world-space rotation delta from rest, hips translation scaled, horizontal root travel removed,
blend in from rest (left foot steps forward first) and back out to rest, left hand IK'd onto the staff shaft.
Body bones only. Usage: blender -b LYRA_tighten_blenderig.blend --python this.py -- params.json"""
import bpy, sys, json, math
from mathutils import Matrix, Vector, Quaternion
from mathutils.bvhtree import BVHTree
PRM=json.load(open(sys.argv[sys.argv.index('--')+1]))
P='mixamorig:'; sc=bpy.context.scene
arm=bpy.data.objects['LYRA_tighten_rig']; staff=bpy.data.objects['LYRA_staff']; body=bpy.data.objects['LYRA_tighten_body']
ad=arm.animation_data
for t in ad.nla_tracks: t.mute=True
atk=bpy.data.actions['LYRA_tighten_attack']
names=[b.name for b in arm.data.bones]; B=arm.data.bones
AW=arm.matrix_world.copy(); AWi=AW.inverted(); AWq=AW.to_quaternion()
def sm(t): t=min(1.0,max(0.0,t)); return t*t*(3-2*t)
# --- target rest = attack f0 (== rest per NOTE), staff offset, left grip relative to staff (attack f12)
ad.action=atk; sc.frame_set(0)
REST_BASIS={n:arm.pose.bones[n].matrix_basis.copy() for n in names}
O=arm.pose.bones[P+'RightHand'].matrix.inverted()@(AWi@staff.matrix_world)
sc.frame_set(12)
Lrel=(AWi@staff.matrix_world).inverted()@arm.pose.bones[P+'LeftHand'].matrix
sc.frame_set(0)
def fk(basis):
    M={}
    for n in names:
        b=B[n]; Rb=b.matrix_local
        if b.parent: M[n]=M[b.parent.name]@(b.parent.matrix_local.inverted()@Rb)@basis[n]
        else: M[n]=Rb@basis[n]
    return M
def basis_from(Q,hips_head):
    """Q: armature-space rotation per bone; returns basis dict (rotation-only except Hips)."""
    M={}; out={}
    for n in names:
        b=B[n]; Rb=b.matrix_local
        if b.parent:
            C=M[b.parent.name]@(b.parent.matrix_local.inverted()@Rb)
            head=C.translation
        else:
            C=Rb; head=hips_head
        Mn=Matrix.LocRotScale(head,Q[n],None); M[n]=Mn
        bs=C.inverted()@Mn
        if b.parent:
            bs=Matrix.LocRotScale(Vector((0,0,0)),bs.to_quaternion(),None)
        out[n]=bs
    return out,M
MR=fk(REST_BASIS)
QR={n:MR[n].to_quaternion() for n in names}; HR=MR[P+'Hips'].translation.copy()
# --- source
before=set(bpy.data.objects)
bpy.ops.import_scene.fbx(filepath=PRM['src'])
new=[o for o in bpy.data.objects if o not in before]
src=[o for o in new if o.type=='ARMATURE'][0]
sact=src.animation_data.action; f0,f1=[int(round(x)) for x in sact.frame_range]
SW=src.matrix_world.copy(); SWq=SW.to_quaternion()
SR={n:(SWq@src.data.bones[n].matrix_local.to_quaternion()) for n in names}
SRh=SW@src.data.bones[P+'Hips'].head_local
TRw={n:(AWq@arm.data.bones[n].matrix_local.to_quaternion()) for n in names}
TRh=AW@arm.data.bones[P+'Hips'].head_local
k=(TRh.z)/(SRh.z) if abs(SRh.z)>1e-6 else 1.0
raw=[]
for f in range(f0,f1+1):
    sc.frame_set(f); Q={}
    for n in names:
        qs=SWq@src.pose.bones[n].matrix.to_quaternion()
        qt=qs@SR[n].inverted()@TRw[n]
        Q[n]=AWq.inverted()@qt
    hw=SW@src.pose.bones[P+'Hips'].head
    raw.append((Q,hw))
hx=[h.x for _,h in raw]; hy=[h.y for _,h in raw]
travel={'hips_dx_range':[min(hx)-hx[0],max(hx)-hx[0]],'hips_dy_range':[min(hy)-hy[0],max(hy)-hy[0]],'net':[hx[-1]-hx[0],hy[-1]-hy[0]]}
# root strip: remove the hips' horizontal carry relative to the clip's first frame, minus a fraction kept as weight shift
keep=PRM.get('keep_weight_shift',1.0)
seq=[]
for i,(Q,hw) in enumerate(raw):
    d=(hw-SRh)*k
    lin=Vector((hx[0]+(hx[-1]-hx[0])*i/max(1,len(raw)-1), hy[0]+(hy[-1]-hy[0])*i/max(1,len(raw)-1),0))-Vector((hx[0],hy[0],0))
    off=Vector(((hw.x-hx[0])*k*keep, (hw.y-hy[0])*k*keep, 0))-lin*k*keep  # linear drift removed (net travel)
    base=Vector(((hx[0]-SRh.x)*k*PRM.get('keep_stance_offset',0.0),(hy[0]-SRh.y)*k*PRM.get('keep_stance_offset',0.0),0))
    hwt=Vector((TRh.x,TRh.y,TRh.z+d.z))+off+base
    seq.append((Q,AWi@hwt))
for o in new: bpy.data.objects.remove(o,do_unlink=True)
# --- frame schedule: blend in, clip, blend out
BI=PRM['blend_in']; BO=PRM['blend_out']
frames=[]
def lerpQ(Qa,Qb,w): return {n:Qa[n].slerp(Qb[n],w) for n in names}
for i in range(BI):
    w=i/BI; frames.append(('in',w,lerpQ(QR,seq[0][0],sm(w)),HR.lerp(seq[0][1],sm(w))))
for i,(Q,h) in enumerate(seq): frames.append(('clip',i,Q,h))
for i in range(1,BO+1):
    w=i/BO; frames.append(('out',w,lerpQ(seq[-1][0],QR,sm(w)),seq[-1][1].lerp(HR,sm(w))))
N=len(frames)
def rot_between(a,b):
    a=a.normalized(); b=b.normalized(); return a.rotation_difference(b)
def ik2(Q,h,up,lo,end,T,Qend,w,pole_hint=None):
    _,M=basis_from(Q,h)
    S=M[up].translation; E0=M[lo].translation; W0=M[end].translation
    l1=(E0-S).length; l2=(W0-E0).length
    Tt=W0.lerp(T,w); D=Tt-S; d=min(D.length,l1+l2-1e-4); d=max(d,abs(l1-l2)+1e-4); u=D.normalized()
    pv=(pole_hint if pole_hint is not None else (E0-S)); p=pv-u*pv.dot(u)
    if p.length<1e-6: p=u.orthogonal()
    p.normalize(); a=(l1*l1-l2*l2+d*d)/(2*d); hh=math.sqrt(max(l1*l1-a*a,0)); E=S+u*a+p*hh
    r1=rot_between(E0-S,E-S); Q[up]=r1@Q[up]
    _,M=basis_from(Q,h); W1=M[end].translation; E1=M[lo].translation
    r2=rot_between(W1-E1,(S+u*d)-E1); Q[lo]=r2@Q[lo]
    if Qend is not None: Q[end]=Q[end].slerp(Qend,w)
    _,M=basis_from(Q,h)
    return (M[end].translation-T).length if w>0.999 else 0.0, (D.length-(l1+l2))
# --- feet: during blend in/out step each foot along an arc (lift) between rest and stance, left first in, right first out
_,Mr=basis_from(QR,HR); _,Ms=basis_from(seq[0][0],seq[0][1]); _,Me=basis_from(seq[-1][0],seq[-1][1])
feet={}
for side in ('Left','Right'):
    fn=P+side+'Foot'
    feet[side]=(Mr[fn].translation.copy(),Ms[fn].translation.copy(),Me[fn].translation.copy(),Mr[fn].to_quaternion(),Ms[fn].to_quaternion(),Me[fn].to_quaternion())
LIFT=PRM.get('lift',0.07)
def foot_u(kind,w,side):
    # in: left 0..0.6, right 0.4..1 ; out: right 0..0.6, left 0.4..1
    first = 'Left' if kind=='in' else 'Right'
    a,b=(0.0,0.6) if side==first else (0.4,1.0)
    return min(1,max(0,(w-a)/(b-a)))
meta={'frames':N,'blend_in':BI,'blend_out':BO,'src_frames':[f0,f1],'hips_scale':k,'travel_src_m':travel}
grip_err=[]; reach=[]

# ---------- stage A: body pose per frame (retarget + foot steps)
poses=[]
for fi,(kind,w,Q,h) in enumerate(frames):
    Q=dict(Q)
    if kind in ('in','out'):
        for side in ('Left','Right'):
            r0,s0,e0,qr,qs,qe=feet[side]
            u=sm(foot_u(kind,w,side))
            A,Bp,qa,qb=(r0,s0,qr,qs) if kind=='in' else (e0,r0,qe,qr)
            T=A.lerp(Bp,u)+Vector((0,0,LIFT*math.sin(math.pi*u)))
            _,M=basis_from(Q,h)
            pole=(M[P+side+'Leg'].translation-M[P+side+'UpLeg'].translation)
            ik2(Q,h,P+side+'UpLeg',P+side+'Leg',P+side+'Foot',T,qa.slerp(qb,u),1.0,pole)
    gw=1.0
    if kind=='in': gw=sm(min(1,w/PRM.get('grip_in',0.8)))
    if kind=='out': gw=1-sm(min(1,w/PRM.get('grip_out',0.6)))
    poses.append([kind,w,Q,h,gw])
# ---------- stage B: two-handed staff solve. Right hand keeps Derek's grip (staff = RightHand @ O); the staff axis
# is searched per frame (near the axis the Mixamo hand implies, continuous in time) so the left hand reaches a grip
# point on the shaft, the shaft clears the torso/legs/head and the ground, and the wrists stay within limits
# (forearm twist takes up to TW of the hand's twist).
EXC={P+'LeftForeArm',P+'LeftHand',P+'RightForeArm',P+'RightHand'}
vg={g.index:g.name for g in body.vertex_groups}
dom=[]
for v in body.data.vertices:
    best=max(v.groups,key=lambda g:g.weight,default=None)
    dom.append(vg[best.group] if best else '')
keepv=[i for i,n in enumerate(dom) if n not in EXC]
keepset=set(keepv)
polys=[tuple(p.vertices) for p in body.data.polygons if all(i in keepset for i in p.vertices)]
def body_bvh(bs):
    for n in names:
        pb=arm.pose.bones[n]; pb.rotation_mode='QUATERNION'; pb.matrix_basis=bs[n]
    bpy.context.view_layer.update()
    dg=bpy.context.evaluated_depsgraph_get(); oe=body.evaluated_get(dg); me=oe.to_mesh()
    vs=[v.co.copy() for v in me.vertices]; oe.to_mesh_clear()
    return BVHTree.FromPolygons(vs,polys)
Oq=O.to_quaternion(); Ot=O.translation.copy(); Lq=Lrel.to_quaternion(); Lt=Lrel.translation.copy()
ZS=[z/100 for z in range(-125,58,6)]  # staff sample points along local Z (butt -1.30 .. crystal top 0.59)
RAD=PRM.get('shaft_radius',0.035); CLR=PRM.get('clear',0.03)
GZ=PRM.get('grip_z_options',[-0.35,-0.45,-0.55,Lt.z])
TW=math.radians(PRM.get('forearm_twist_max',60)); WB=math.radians(PRM.get('wrist_free',30))
def fib(n):
    out=[]; g=math.pi*(3-math.sqrt(5))
    for i in range(n):
        y=1-2*(i+0.5)/n; r=math.sqrt(1-y*y); t=g*i; out.append(Vector((math.cos(t)*r,y,math.sin(t)*r)))
    return out
DIRS=fib(PRM.get('n_dirs',700))
def bend_of(Fq,Hq,F0,H0):
    L=Fq.inverted()@Hq; L0=F0.inverted()@H0; D=L@L0.inverted()
    t=Quaternion((D.w,0,D.y,0)); 
    if t.magnitude<1e-9: t=Quaternion()
    t.normalize(); th=2*math.atan2(t.y,t.w); th=(th+math.pi)%(2*math.pi)-math.pi
    k=max(-TW,min(TW,th)); tk=Quaternion((0,1,0),k)
    D2=tk.inverted()@D
    return D2.angle if D2.angle<=math.pi else 2*math.pi-D2.angle, k
def twist_fix(Q,fore,hand,F0,H0):
    _,k=bend_of(Q[fore],Q[hand],F0,H0)
    Q[fore]=Q[fore]@Quaternion((0,1,0),k)
FR={s:(QR[P+s+'ForeArm'],QR[P+s+'Hand']) for s in ('Left','Right')}
lenL=None
def seg_cost(bvh,pts):
    c=0.0; mind=9
    for p in pts:
        if p.z<0.02: c+=(0.02-p.z)*4
        r=bvh.find_nearest(p,0.25)
        if r[0] is None: continue
        d=r[3]; sgn=(p-r[0]).dot(r[1])
        sd=d if sgn>=0 else -d
        sd-=RAD; mind=min(mind,sd)
        if sd<CLR: c+=(CLR-sd)
    return c,mind

EXC_ALL=EXC|{P+'LeftArm',P+'RightArm'}
polys_t=[tuple(p.vertices) for p in body.data.polygons if all(dom[i] not in EXC_ALL for i in p.vertices)]
def body_bvh_t(bs):
    for n in names:
        pb=arm.pose.bones[n]; pb.rotation_mode='QUATERNION'; pb.matrix_basis=bs[n]
    bpy.context.view_layer.update()
    dg=bpy.context.evaluated_depsgraph_get(); oe=body.evaluated_get(dg); me=oe.to_mesh()
    vs=[v.co.copy() for v in me.vertices]; oe.to_mesh_clear()
    return BVHTree.FromPolygons(vs,polys_t)
YV=Vector((0,1,0))
def seg_pt_dist(p,a,b):
    ab=b-a; t=max(0,min(1,(p-a).dot(ab)/max(ab.length_squared,1e-9))); return (p-(a+ab*t)).length
SW=[math.radians(x) for x in range(-90,91,22)]
def solve_arm(side,M,Q,T,Hq,pts,hand_pt,prev_phi):
    S=M[P+side+'Arm'].translation; E0=M[P+side+'ForeArm'].translation; W0=M[P+side+'Hand'].translation
    l1=(E0-S).length; l2=(W0-E0).length
    D=T-S; d=max(min(D.length,l1+l2-1e-4),abs(l1-l2)+1e-4); u=D.normalized()
    pv=E0-S; p0=pv-u*pv.dot(u)
    if p0.length<1e-6: p0=u.orthogonal()
    p0.normalize(); p1=u.cross(p0)
    a_=(l1*l1-l2*l2+d*d)/(2*d); hh=math.sqrt(max(l1*l1-a_*a_,0))
    Qu0=Q[P+side+'Arm']; Qf0=Q[P+side+'ForeArm']
    best=None
    for phi in SW:
        E=S+u*a_+(p0*math.cos(phi)+p1*math.sin(phi))*hh
        Wt=S+u*d
        Qu=(Qu0@YV).rotation_difference(E-S)@Qu0
        Qf=(Qf0@YV).rotation_difference(Wt-E)@Qf0
        bend,_=bend_of(Qf,Hq,*FR[side])
        c=max(0,bend-WB)*3+bend*0.2+abs(phi)*0.25
        if prev_phi is not None: c+=abs(phi-prev_phi)*1.0
        if E.z>S.z+0.05: c+=(E.z-S.z)*4   # elbow above shoulder
        cl=9
        for p in pts[::2]:
            if (p-hand_pt).length<0.14: continue
            dd=min(seg_pt_dist(p,S,E),seg_pt_dist(p,E,Wt))-0.05-RAD
            cl=min(cl,dd)
        if cl<CLR: c+=(CLR-cl)*40
        if best is None or c<best[0]: best=(c,phi,Qu,Qf,bend,cl)
    return best, max(0,D.length-(l1+l2)*0.97)
def smoothq(qs,sig):
    out=[]
    for i in range(len(qs)):
        acc=None; ws=0
        for j in range(max(0,i-4),min(len(qs),i+5)):
            wj=math.exp(-((i-j)**2)/(2*sig*sig)); q=qs[j].copy()
            if acc is None: acc=Vector((q.w,q.x,q.y,q.z))*wj; ref=q
            else:
                if Vector((q.w,q.x,q.y,q.z)).dot(Vector((ref.w,ref.x,ref.y,ref.z)))<0: q.negate()
                acc+=Vector((q.w,q.x,q.y,q.z))*wj
            ws+=wj
        acc/=ws; out.append(Quaternion((acc[0],acc[1],acc[2],acc[3])).normalized())
    return out
chosen=[]; prev=None; solve_log=[]
for fi,(kind,w,Q,h,gw) in enumerate(poses):
    bs,M=basis_from(Q,h)
    nat=(M[P+'RightHand'].to_quaternion()@Oq)
    if gw<=0.0:
        chosen.append(None); prev=None; continue
    bvh=body_bvh_t(bs)
    RH0=M[P+'RightHand'].translation.copy(); SL=M[P+'LeftArm'].translation; SR_=M[P+'RightArm'].translation
    lL=(M[P+'LeftForeArm'].translation-SL).length+(M[P+'LeftHand'].translation-M[P+'LeftForeArm'].translation).length
    lR=(M[P+'RightForeArm'].translation-SR_).length+(M[P+'RightHand'].translation-M[P+'RightForeArm'].translation).length
    # grip midpoint: the Mixamo hands' midpoint pulled in toward the shoulders' midpoint, kept within reach
    shm=(SL+SR_)/2; mm=(RH0+M[P+'LeftHand'].translation)/2
    # in front of the chest: forward FWD, down DN, plus half the Mixamo hands' sideways/vertical sway
    cq=Q[P+'Spine2']@QR[P+'Spine2'].inverted()
    fwd=cq@Vector((0,-1,0)); fwd.z=0; fwd.normalize(); up=Vector((0,0,1)); side=fwd.cross(up)
    rel=mm-shm
    Pm=shm+fwd*PRM.get('fwd',0.40)-up*PRM.get('dn',0.22)+side*max(-0.18,min(0.18,rel.dot(side)*0.5))+up*max(-0.25,min(0.30,(rel.dot(up)+PRM.get('dn',0.22))*0.5))
    nz=nat@Vector((0,0,1))
    cands=[]
    for dvec in DIRS:
        ang=nz.angle(dvec)
        Sq=nz.rotation_difference(dvec)@nat
        base=ang*PRM.get('w_nat',0.4)
        st=PRM.get('style','')
        if st=='horizontal': base+=max(0,abs(math.asin(max(-1,min(1,dvec.z))))-math.radians(20))*PRM.get('w_style',2.0)
        elif st=='up': base+=max(0,math.acos(max(-1,min(1,dvec.z)))-math.radians(35))*PRM.get('w_style',1.0)
        if prev is not None: base+=prev['Sq'].rotation_difference(Sq).angle*PRM.get('w_cont',3.0)
        if prev is not None and prev['Sq'].rotation_difference(Sq).angle>math.radians(PRM.get('max_step_deg',22))+math.radians(60): continue
        for gz in GZ:
            Spos=Pm-Sq@Vector((0,0,gz/2))
            T=Spos+Sq@Vector((Lt.x,Lt.y,gz))
            RHt=Spos-(Sq@Oq.inverted())@Ot
            reach=max((T-SL).length-lL*0.97,(RHt-SR_).length-lR*0.97)
            if reach>0.12: continue
            c=base+max(0,reach)*60+abs(gz-Lt.z)*0.3
            if prev is not None: c+=abs(gz-prev['gz'])*4
            cands.append((c,Sq,gz,Spos))
    cands.sort(key=lambda x:x[0])
    best=None
    ROLLS=[math.radians(r) for r in PRM.get('rolls',[-60,-30,0,30,60])]
    ev=[]
    for c,Sq0,gz,Spos0 in cands[:PRM.get('n_eval',80)*3]:
        ax=Sq0@Vector((0,0,1))
        for rl in ROLLS:
            Sq=Quaternion(ax,rl)@Sq0
            Spos=Pm-Sq@Vector((0,0,gz/2))
            cr=c+abs(rl)*PRM.get('w_roll',0.3)
            if prev is not None: cr+=prev['Sq'].rotation_difference(Sq).angle*PRM.get('w_cont',3.0)-prev['Sq'].rotation_difference(Sq0).angle*PRM.get('w_cont',3.0)
            if prev is not None and prev['Sq'].rotation_difference(Sq).angle>math.radians(PRM.get('max_step_deg',22)): continue
            ev.append((cr,Sq,gz,Spos))
    if not ev and prev is not None:
        ev=[(0.0,prev['Sq'],prev['gz'],Pm-prev['Sq']@Vector((0,0,prev['gz']/2)))]
    ev.sort(key=lambda x:x[0])
    for c,Sq,gz,Spos in ev[:PRM.get('n_eval2',160)]:
        pts=[Spos+Sq@Vector((0,0,z)) for z in ZS]
        cc,mind=seg_cost(bvh,[p for p,z in zip(pts,ZS) if not (abs(z-gz)<0.08 or abs(z)<0.08)])
        tot=c+cc*PRM.get('w_clear',40)
        if best is not None and tot>=best['tot']: continue
        HR=Sq@Oq.inverted(); HL=Sq@Lq
        TL=Spos+Sq@Vector((Lt.x,Lt.y,gz))
        RHt=Spos-HR@Ot
        r,overR=solve_arm('Right',M,Q,RHt,HR,pts,Spos,prev['phiR'] if prev else None)
        l,over=solve_arm('Left',M,Q,TL,HL,pts,TL,prev['phiL'] if prev else None)
        over=max(over,overR)
        tot+=r[0]+l[0]+over*60
        if best is None or tot<best['tot']:
            best={'tot':tot,'Sq':Sq,'gz':gz,'phiR':r[1],'phiL':l[1],'QuR':r[2],'QfR':r[3],'QuL':l[2],'QfL':l[3],'HR':HR,'HL':HL,'bendR':r[4],'bendL':l[4],'clear':mind,'armclear':min(r[5],l[5]),'over':over}
    chosen.append(best); prev=best
    solve_log.append({'f':fi,'cost':round(best['tot'],3),'clear_min':round(best['clear'],3),'arm_clear':round(best['armclear'],3),'bendR':round(math.degrees(best['bendR']),1),'bendL':round(math.degrees(best['bendL']),1),'reach_over':round(best['over'],3),'grip_z':best['gz']})
# apply, smooth arm rotations over time, then re-enforce the exact grip with the left arm
ARMB=[P+'RightArm',P+'RightForeArm',P+'RightHand',P+'LeftArm',P+'LeftForeArm',P+'LeftHand']
solvedQ=[]
for fi,(kind,w,Q,h,gw) in enumerate(poses):
    Q=dict(Q); c=chosen[fi]
    if c is not None:
        tgt={P+'RightArm':c['QuR'],P+'RightForeArm':c['QfR'],P+'RightHand':c['HR'],P+'LeftArm':c['QuL'],P+'LeftForeArm':c['QfL'],P+'LeftHand':c['HL']}
        for n,q in tgt.items(): Q[n]=Q[n].slerp(q,gw) if gw<1 else q
    solvedQ.append(Q)
for n in ARMB:
    qs=smoothq([Q[n] for Q in solvedQ],PRM.get('smooth',1.2))
    for Q,q in zip(solvedQ,qs): Q[n]=q
grip_err=[]; reach=[]; wrist=[]; out=[]
GZs=[(c['gz'] if c else Lt.z) for c in chosen]
GZs=[sum(GZs[j]*math.exp(-((i-j)**2)/8) for j in range(max(0,i-4),min(len(GZs),i+5)))/sum(math.exp(-((i-j)**2)/8) for j in range(max(0,i-4),min(len(GZs),i+5))) for i in range(len(GZs))]
for fi,(kind,w,_,h,gw) in enumerate(poses):
    Q=solvedQ[fi]
    bs,M=basis_from(Q,h)
    St=M[P+'RightHand']@O
    Tg=St@Matrix.LocRotScale(Vector((Lt.x,Lt.y,GZs[fi])),Lq,None)
    pole=M[P+'LeftForeArm'].translation-M[P+'LeftArm'].translation
    if gw>0:
        e,r=ik2(Q,h,P+'LeftArm',P+'LeftForeArm',P+'LeftHand',Tg.translation,Tg.to_quaternion(),gw,pole)
        if gw>0.999: grip_err.append((fi,round(e,4))); reach.append(r)
    for s_ in ('Left','Right'): twist_fix(Q,P+s_+'ForeArm',P+s_+'Hand',*FR[s_])
    wr={s_:round(math.degrees(bend_of(Q[P+s_+'ForeArm'],Q[P+s_+'Hand'],*FR[s_])[0]),1) for s_ in ('Left','Right')}
    wrist.append(wr)
    out.append(basis_from(Q,h)[0])
meta['wrist_per_frame']=wrist
meta['solve']=solve_log; meta['wrist_bend_deg_max']={k_:max(x[k_] for x in wrist) for k_ in ('Left','Right')}
meta['grip_err_max_m']=max([e for _,e in grip_err] or [0]); meta['grip_err_frames_over_1cm']=[f for f,e in grip_err if e>0.01]
meta['left_reach_over_max_m']=max(reach or [0])
# --- write action
act=bpy.data.actions.new(PRM['action']); ad.action=act
for fi,bs in enumerate(out):
    for n in names:
        pb=arm.pose.bones[n]; pb.rotation_mode='QUATERNION'; pb.matrix_basis=bs[n]
        pb.keyframe_insert('rotation_quaternion',frame=fi)
        if n==P+'Hips': pb.keyframe_insert('location',frame=fi)
        else: pb.location=(0,0,0)
for fc in act.fcurves if hasattr(act,'fcurves') else []:
    for kp in fc.keyframe_points: kp.interpolation='LINEAR'
sc.frame_start=0; sc.frame_end=N-1; sc.render.fps=30
# --- QC: staff vs body clearance, crystal path, feet slide
def bvh_of(o,ps=None):
    dg=bpy.context.evaluated_depsgraph_get(); oe=o.evaluated_get(dg); me=oe.to_mesh()
    vs=[oe.matrix_world@v.co for v in me.vertices]; ps=ps if ps is not None else [tuple(p.vertices) for p in me.polygons]; oe.to_mesh_clear(); return BVHTree.FromPolygons(vs,ps),vs
CRY=Vector(PRM['crystal_local']); qc=[]
hand_parts=None
for fi in range(0,N,PRM.get('qc_step',3)):
    sc.frame_set(fi)
    tb,_=bvh_of(body,polys); sb,svs=bvh_of(staff)
    hits=len(tb.overlap(sb))
    md=9; 
    for v in svs[::PRM.get('vstep',25)]:
        r=tb.find_nearest(v)
        if r[0] is not None: md=min(md,r[3])
    cr=staff.matrix_world@CRY
    lf=AW@arm.pose.bones[P+'LeftFoot'].head; rf=AW@arm.pose.bones[P+'RightFoot'].head
    qc.append({'f':fi,'overlap_pairs':hits,'staff_min_dist':round(md,3),'crystal':[round(x,3) for x in cr],'lfoot':[round(x,3) for x in lf],'rfoot':[round(x,3) for x in rf],'hips':[round(x,3) for x in AW@arm.pose.bones[P+'Hips'].head]})
meta['qc']=qc
# crystal & right hand per frame for contact frame
path=[]
for fi in range(N):
    sc.frame_set(fi); cr=staff.matrix_world@CRY; rh=AW@arm.pose.bones[P+'RightHand'].head; lh=AW@arm.pose.bones[P+'LeftHand'].head
    path.append([fi]+[round(x,3) for x in cr]+[round(x,3) for x in rh]+[round(x,3) for x in lh])
meta['path_f_crystal_rhand_lhand']=path
json.dump(meta,open(PRM['meta_out'],'w'),indent=1)
if PRM.get('blend_out_path'): bpy.ops.wm.save_as_mainfile(filepath=PRM['blend_out_path'],compress=True)
if PRM.get('fbx_out'):
    common=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
        apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
        mesh_smooth_type='FACE', use_mesh_modifiers=False)
    anim=dict(bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
    bpy.ops.object.select_all(action='DESELECT')
    for o in (arm,body): o.hide_set(False); o.select_set(True)
    bpy.context.view_layer.objects.active=arm; sc.frame_set(0)
    bpy.ops.export_scene.fbx(filepath=PRM['fbx_out'],**anim,**common)
print('RETARGET OK',N)
