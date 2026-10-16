"""Rowan A-pose sheets + Unity tracks (2026-10-14). Coat tails get a Blender spring PREVIEW of the Unity ClothSpringRig
(stiffness 0.3 falling to the hem, damping 0.45, 35 deg limit, velocity clamp, leg + hand colliders) so the sheets show what the
springs do; nothing on the coat is baked. Bow: one prop rigid on LeftHand in every clip, its grip inside the sculpted left fist (fingers.py); bow vs hand/body checked every frame."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, math, json, numpy as np
sys.path.insert(0,'/workspace/rowan_fresh/w2/bowgrip'); from bowroll import bowrot   # v5: bow rolled per frame (carry 270)
from mathutils import Vector, Matrix
from scipy.spatial import cKDTree
W='/workspace/rowan_fresh/w2'
bpy.ops.wm.open_mainfile(filepath=W+'/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; body=bpy.data.objects['ROWAN_body']; P_='mixamorig:'
coat=bpy.data.objects['ROWAN_coat']; crig=bpy.data.objects['ROWAN_coat_rig']; CP=crig.pose.bones
for pb in CP: pb.rotation_mode='QUATERNION'
CH=json.load(open(W+'/coat_chains.json'))
def imp(fp):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=fp); o=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]
    for x in set(bpy.data.objects)-before:
        if x is not o: bpy.data.objects.remove(x)
    M=o.matrix_world.copy(); o.parent=None; o.data.transform(M); o.rotation_mode='QUATERNION'; o.matrix_world=Matrix(); return o
bow=imp(W+'/ROWAN_bow.fbx'); arrow=imp(W+'/ROWAN_arrow.fbx'); proj=imp(W+'/ROWAN_arrow.fbx')
A={k:bpy.data.actions['ROWAN_'+k] for k in ('rest','walk','attack')}
def bm(n): return arm.matrix_world@arm.pose.bones[P_+n].matrix
def upd(): bpy.context.view_layer.update()
rep={}
# ---------- coat spring preview ----------
HipsRest=arm.data.bones[P_+'Hips'].matrix_local.copy()
CHJ=[np.array(c['joints']) for c in CH]
KJ=[0.55,0.40,0.30]; DAMP=0.45; LIM=math.radians(35); GRAV=0.15; VMAX=0.08; CR=0.04
COLL=[('LeftUpLeg','LeftLeg',0.085),('LeftLeg','LeftFoot',0.065),('RightUpLeg','RightLeg',0.085),('RightLeg','RightFoot',0.065),
      ('LeftHand',None,0.055),('RightHand',None,0.055)]
def colliders():
    out=[]
    for a,b,r in COLL:
        Ma=bm(a); pa=np.array(Ma.to_translation())
        pb_=np.array(bm(b).to_translation()) if b else pa+np.array(Ma.to_3x3()@Vector((0,0.17,0)))
        out.append((pa,pb_,r+CR))
    out.append((np.array(bm('Hips').to_translation()),)*2+(0.14+CR,))
    return out
def push(p,cols):
    for a,b,r in cols:
        ab=b-a; L2=ab@ab; t=0 if L2<1e-9 else np.clip((p-a)@ab/L2,0,1); q=a+t*ab; d=p-q; n=np.linalg.norm(d)
        if n<r: p=q+(d/max(n,1e-6))*r
    return p
class Coat:
    def __init__(s): s.p=None
    def targets(s):
        M=np.array(bm('Hips')@HipsRest.inverted())
        return [ (np.c_[J,np.ones(len(J))]@M.T)[:,:3] for J in CHJ ]
    def step(s,sub=2):
        T=s.targets(); cols=colliders()
        if s.p is None: s.p=[t.copy() for t in T]; s.q=[t.copy() for t in T]
        for _ in range(sub):
            for k,t in enumerate(T):
                p,q=s.p[k],s.q[k]; npn=p.copy(); npn[0]=t[0]
                for j in range(1,len(t)):
                    v=(p[j]-q[j])*(1-DAMP); vn=np.linalg.norm(v)
                    if vn>VMAX: v*=VMAX/vn
                    x=p[j]+v+(t[j]-p[j])*KJ[j-1]*0.5+np.array([0,0,-9.81*GRAV])*(1/60)**2
                    # length + angle limit vs the animated rest segment
                    rd=t[j]-t[j-1]; L=np.linalg.norm(rd); rd/=L; d=x-npn[j-1]; d/=max(np.linalg.norm(d),1e-9)
                    ang=math.acos(np.clip(d@rd,-1,1))
                    if ang>LIM:
                        ax=np.cross(rd,d); ax/=max(np.linalg.norm(ax),1e-9)
                        d=rd*math.cos(LIM)+np.cross(ax,rd)*math.sin(LIM)
                    x=npn[j-1]+d*L; x=push(x,cols); d=x-npn[j-1]; x=npn[j-1]+d/np.linalg.norm(d)*L
                    npn[j]=x
                s.q[k]=p; s.p[k]=npn
        s.apply(T)
    def apply(s,T):
        upd(); CP['coat_Hips'].matrix=arm.pose.bones[P_+'Hips'].matrix.copy(); upd()
        for k,c in enumerate(CH):
            for i,bn in enumerate(c['bones']):
                pb=CP[bn]; pb.rotation_quaternion=(1,0,0,0); upd()
                M=pb.matrix.copy(); cur=(M.to_3x3()@Vector((0,1,0))).normalized()
                want=Vector(s.p[k][i+1]-s.p[k][i]).normalized(); q=cur.rotation_difference(want)
                pb.matrix=Matrix.Translation(Vector(s.p[k][i]))@(q.to_matrix()@M.to_3x3()).to_4x4(); upd()
    def static(s):
        s.p=s.targets(); s.q=[t.copy() for t in s.p]; s.apply(s.p)
COATSIM=Coat()
# ---------- metrics helpers ----------
me=body.data; N=len(me.vertices); P0=np.array([v.co[:] for v in me.vertices]); part=np.load(W+'/part.npy')
gidx={g.name:g.index for g in body.vertex_groups}
def wcol(name):
    w=np.zeros(N); gi=gidx[P_+name]
    for v in me.vertices:
        for g in v.groups:
            if g.group==gi: w[v.index]=g.weight
    return w
hw=wcol('Head'); face=np.nonzero(hw>0.999)[0]; band=np.nonzero((hw>0.01)&(hw<0.999))[0]; Hrest=arm.data.bones[P_+'Head'].matrix_local
handv={'Left':np.nonzero((wcol('LeftHand')+sum(wcol(x) for x in ('LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2')))>0.999)[0],'Right':np.nonzero(wcol('RightHand')>0.999)[0]}
bodyv=np.nonzero((part==0)|(part==3)|(part==4))[0]
FF=np.array([p.vertices[:] for p in me.polygons]); E=np.unique(np.sort(np.vstack([FF[:,[0,1]],FF[:,[1,2]],FF[:,[2,0]]]),1),axis=0); L0=np.linalg.norm(P0[E[:,0]]-P0[E[:,1]],axis=1)
armE=np.isin(part[E[:,0]],(1,2))
def evalQ(o):
    dg=bpy.context.evaluated_depsgraph_get(); e=o.evaluated_get(dg); m=e.to_mesh(); Q=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',Q); e.to_mesh_clear(); return Q.reshape(-1,3)
from mathutils.bvhtree import BVHTree
handL=handv['Left']; HS=set(handL.tolist())
hpolys=[tuple(p.vertices) for p in me.polygons if all(i in HS for i in p.vertices)]
nonhand=[tuple(p.vertices) for p in me.polygons if not any(i in HS for i in p.vertices)]
cpolys=[tuple(p.vertices) for p in coat.data.polygons]
def dense(V,polys,step=0.004):
    out=[]
    for p in polys:
        for k in range(1,len(p)-1):
            A_,B_,C_=V[p[0]],V[p[k]],V[p[k+1]]; L=max(np.linalg.norm(B_-A_),np.linalg.norm(C_-A_),np.linalg.norm(C_-B_)); n_=max(1,int(L/step))
            for i in range(n_+1):
                for j in range(n_+1-i): out.append(A_+(B_-A_)*i/n_+(C_-A_)*j/n_)
    return np.array(out)
BOWV=np.array([v.co[:] for v in bow.data.vertices]); BOWP=[tuple(p.vertices) for p in bow.data.polygons]; BOWD=dense(BOWV,BOWP)
def inside_count(tree,pts,tol=0.001,maxd=0.05):
    c=0
    for p in pts:
        r=tree.find_nearest(Vector(p),maxd)
        if r[0] is not None and (Vector(p)-r[0]).dot(r[1])<-tol: c+=1
    return c
def bow_checks(Q,C):
    M=np.array(bow.matrix_world); D=BOWD@M[:3,:3].T+M[:3,3]
    th=BVHTree.FromPolygons([tuple(v) for v in Q],hpolys); tb=BVHTree.FromPolygons([tuple(v) for v in D],[]) if False else None
    # the hand mesh is open at the wrist cut, so only bow samples within 9 cm of the grip centre can be inside the hand
    near=np.linalg.norm(D-np.array(bow.matrix_world.to_translation()),axis=1)<0.09
    bh=inside_count(th,D[near])
    tbody=BVHTree.FromPolygons([tuple(v) for v in Q],nonhand); tc=BVHTree.FromPolygons([tuple(v) for v in C],cpolys)
    bb=inside_count(tbody,D[::3],tol=0.004); bc=inside_count(tc,D[::3],tol=0.004)
    # hand verts inside the bow
    Bw=BOWV@M[:3,:3].T+M[:3,3]; tbw=BVHTree.FromPolygons([tuple(v) for v in Bw],BOWP); hb=inside_count(tbw,Q[handL])
    return dict(bow_in_hand=bh,hand_in_bow=hb,bow_in_body=bb,bow_in_coat=bc)
def metrics():
    Q=evalQ(body)
    M=np.array(arm.pose.bones[P_+'Head'].matrix@Hrest.inverted()); R=(np.c_[P0,np.ones(N)]@M.T)[:,:3]; d=np.linalg.norm(Q-R,axis=1)
    tr=cKDTree(Q[bodyv]); hd={s:float(tr.query(Q[v])[0].min()) for s,v in handv.items()}
    st=np.linalg.norm(Q[E[:,0]]-Q[E[:,1]],axis=1)/np.maximum(L0,1e-6)
    C=evalQ(coat); legs=[]
    for a,b,r in COLL[:4]:
        pa=np.array(bm(a).to_translation()); pb_=np.array(bm(b).to_translation()); ab=pb_-pa; t=np.clip((C-pa)@ab/(ab@ab),0,1); legs.append(np.linalg.norm(C-(pa+t[:,None]*ab),axis=1)<r-0.02)
    return dict(face=float(d[face].max()),band=float(d[band].max()) if len(band) else 0.0,hand_gap_L=hd['Left'],hand_gap_R=hd['Right'],
                arm_stretch=float(st[armE].max()),coat_in_legs=int(np.any(legs,0).sum()))
# ---------- Unity convention ----------
Cv=Matrix(((-1,0,0),(0,0,1),(0,-1,0)))
def U(v): return [round(-v.x,5),round(v.z,5),round(-v.y,5)]
def UQ(Rb):
    q=(Cv@Rb@Cv.inverted()).to_quaternion().normalized()
    if q.w<0: q=-q
    return [round(q.x,5),round(q.y,5),round(q.z,5),round(q.w,5)]
REF,REL,V0=10,14,8; SPD=14.0
arm.animation_data.action=A['attack']; sc.frame_set(REF)
# bow grip inside the left fist (grip.json: grip axis through the palm centre, bow long axis along it, string toward the wrist)
FI=json.load(open(W+'/grip.json')); bow_off=(Vector(FI['grip_local']),Matrix(FI['grip_rot_local']))
ML=bm('LeftHand'); RL=ML.to_3x3().normalized(); grip=ML.to_translation()+RL@bow_off[0]
MR=bm('RightHand'); RR=MR.to_3x3().normalized(); nock=MR.to_translation()+RR@Vector((0,0.05,0))
fw=(grip-nock).normalized(); yax=-fw; xax=Vector((1,0,0)); xax=(xax-xax.dot(yax)*yax).normalized(); zax=xax.cross(yax); Rar=Matrix((xax,yax,zax)).transposed()
ar_off=(RR.inverted()@(nock-MR.to_translation()), RR.inverted()@Rar)
def place(M,off): R=M.to_3x3().normalized(); return M.to_translation()+R@off[0], R@off[1]
def place_bow(M,clip,f): R=M.to_3x3().normalized(); return M.to_translation()+R@bow_off[0], R@bowrot(clip,f)
held=[]; bowtr=[]; projtr=[]; arpose={}
for f in range(31):
    sc.frame_set(f); p,R=place(bm('RightHand'),ar_off); held.append(U(p)+UQ(R)+[1.0,V0<=f<REL]); arpose[f]=(p.copy(),R.copy())
    bp,bR=place_bow(bm('LeftHand'),'attack',f); bowtr.append(U(bp)+UQ(bR)+[1.0,True])
sp,sR=arpose[REL-1]
for f in range(31):
    p=sp+fw*max(0,f-REL+1)*SPD/30.0; projtr.append(U(p)+UQ(sR)+[1.0,f>=REL])
arm.animation_data.action=A['rest']; sc.frame_set(0); bp,bR=place_bow(bm('LeftHand'),'rest',0)
rep['bow_idle_rest_world_unity']=U(bp)+UQ(bR)
rep['calibration_unity']={n:U(arm.matrix_world@arm.data.bones[P_+n].head_local) for n in ('Hips','Head','LeftHand','RightHand')}
rep.update(arrow_held_unity=held,arrow_proj_unity=projtr,bow_attack_unity=bowtr,arrow_dir_unity=U(fw))
# bow extent (Unity axes) for the C# spec
Bv=np.array([v.co[:] for v in bow.data.vertices]); rep['bow_extent_unity']=[round(float(v),4) for v in (Bv.max(0)-Bv.min(0))[[0,2,1]]]
Av=np.array([v.co[:] for v in arrow.data.vertices]); rep['arrow_extent_unity']=[round(float(v),4) for v in (Av.max(0)-Av.min(0))[[0,2,1]]]
# ---------- render ----------
w_=bpy.data.worlds.new('bg'); sc.world=w_; w_.color=(0.45,0.45,0.45)
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'
VIEWS={'full':((0,-5,0.95),(math.pi/2,0,0)),'side':((5,0,0.95),(math.pi/2,0,math.pi/2)),'back':((0,5,0.95),(math.pi/2,0,math.pi))}
def shot(path,view):
    if view=='face':
        sc.render.resolution_x=360; sc.render.resolution_y=360; cam.data.ortho_scale=0.45; h=bm('Head').to_translation()
        cam.location=(h.x,-5,h.z+0.12); cam.rotation_euler=(math.pi/2,0,0)
    elif view.startswith('hand'):
        s='Left' if view[4]=='L' else 'Right'; sc.render.resolution_x=360; sc.render.resolution_y=360; cam.data.ortho_scale=0.42
        h=bm(s+'Hand')@Vector((0,0.06,0))
        if view.endswith('F'): cam.location=(h.x,-5,h.z); cam.rotation_euler=(math.pi/2,0,0)
        else: sg=1 if s=='Left' else -1; cam.location=(5*sg,h.y,h.z); cam.rotation_euler=(math.pi/2,0,math.pi/2*sg)
    elif view in ('bigfront','bigside'):
        sc.render.resolution_x=720; sc.render.resolution_y=900; cam.data.ortho_scale=1.6
        cam.location=(0,-5,1.15) if view=='bigfront' else (5,0,1.15); cam.rotation_euler=(math.pi/2,0,0) if view=='bigfront' else (math.pi/2,0,math.pi/2)
    else:
        sc.render.resolution_x=360; sc.render.resolution_y=640; cam.data.ortho_scale=2.1; cam.location,cam.rotation_euler=VIEWS[view]
    sc.render.filepath=path; bpy.ops.render.render(write_still=True)
FR={'rest':[0],'walk':[0,4,8,12,15,19,23,27],'attack':[0,3,5,8,10,12,14,16,18,22,26,30]}
met={}
for k,frs in FR.items():
    arm.animation_data.action=A[k]; COATSIM.p=None
    pre=[] if k=='rest' else (list(range(31))*2 if k=='walk' else [0]*20)
    for f in pre: sc.frame_set(f); COATSIM.step()
    for f in range(31):
        sc.frame_set(f)
        if k=='rest': COATSIM.static()
        else: COATSIM.step()
        bp,bR=place_bow(bm('LeftHand'),k,f); bow.matrix_world=Matrix.Translation(bp)@bR.to_4x4(); upd()
        m=metrics(); m.update(bow_checks(evalQ(body),evalQ(coat))); met.setdefault(k,[]).append(m)
        if f not in frs: continue
        p,R=place(bm('RightHand'),ar_off); arrow.matrix_world=Matrix.Translation(p)@R.to_4x4(); arrow.hide_render=not(k=='attack' and V0<=f<REL)
        proj.matrix_world=Matrix.Translation(sp+fw*max(0,f-REL+1)*SPD/30.0)@sR.to_4x4(); proj.hide_render=not(k=='attack' and f>=REL)
        views=['full','face','side','back']+(['handLF','handRF','handLS','handRS'] if k=='walk' else [])+(['bigfront','bigside'] if (k=='attack' and f==REF) else [])
        for v in views: shot(f'{W}/sh_{k}_{f:02d}_{v}.png',v)
rep['metrics_max']={k:{q:round(max(m[q] for m in v)*(100 if q in ('face','band') else 1),4) for q in ('face','band','arm_stretch','coat_in_legs')} for k,v in met.items()}
rep['bow_checks_max']={k:{q:max(m[q] for m in v) for q in ('bow_in_hand','hand_in_bow','bow_in_body','bow_in_coat')} for k,v in met.items()}
rep['hand_gap_min_cm']={k:{s:round(min(m['hand_gap_'+s] for m in v)*100,2) for s in ('L','R')} for k,v in met.items()}
rep['metrics_note']='face/band = max vertex drift from rigid Head (cm); hand_gap = min distance from 100%-Hand vertices to torso/leg vertices; arm_stretch = max edge length ratio on arm edges; coat_in_legs = coat verts >2 cm inside a leg capsule (with spring preview)'
json.dump(rep,open(W+'/sheet.json','w'),indent=1)
print('SHEET',json.dumps({k:v for k,v in rep.items() if not k.startswith(('arrow_held','arrow_proj','bow_att'))}))
