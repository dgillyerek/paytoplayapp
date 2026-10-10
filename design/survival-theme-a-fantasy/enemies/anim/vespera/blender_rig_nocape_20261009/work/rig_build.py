"""Vespera no-cape rig + skin weights (2026-10-09). Input prep.blend (cape-free body). 22 mixamorig bones, joints from
joints.json (measured from mesh cross-sections). Weights: part segmentation (arms/legs flood-filled inside capsules,
head/neck/torso by spine arc), smooth joint blends along each limb, part-local smoothing, <=4 influences, normalized."""
import bpy, bmesh, numpy as np, json, sys, math
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; JOINTS=args[0]; OUTBLEND=args[1]; OUTJSON=args[2]
J={k:np.array(v,float) for k,v in json.load(open(JOINTS)).items()}
P_='mixamorig:'
body=bpy.data.objects['VESPERA_nocape_body']; me=body.data
N=len(me.vertices); co=np.empty(N*3); me.vertices.foreach_get('co',co); P=co.reshape(-1,3)
E=np.array([e.vertices[:] for e in me.edges])
# ---------------- skeleton ----------------
BONES=[('Hips',None,'Hips','Spine'),('Spine','Hips','Spine','Spine1'),('Spine1','Spine','Spine1','Spine2'),('Spine2','Spine1','Spine2','Neck'),
 ('Neck','Spine2','Neck','Head'),('Head','Neck','Head','HeadTop')]
for s in ('Left','Right'):
    BONES+=[(s+'Shoulder','Spine2',s+'Shoulder',s+'Arm'),(s+'Arm',s+'Shoulder',s+'Arm',s+'ForeArm'),(s+'ForeArm',s+'Arm',s+'ForeArm',s+'Hand'),(s+'Hand',s+'ForeArm',s+'Hand',s+'HandEnd')]
for s in ('Left','Right'):
    BONES+=[(s+'UpLeg','Hips',s+'UpLeg',s+'Leg'),(s+'Leg',s+'UpLeg',s+'Leg',s+'Foot'),(s+'Foot',s+'Leg',s+'Foot',s+'ToeBase'),(s+'ToeBase',s+'Foot',s+'ToeBase',s+'ToeEnd')]
# order exactly like the previous rig (MixamoHumanoidBones order): Hips..Head, L arm, R arm, L leg, R leg
ad=bpy.data.armatures.new('VESPERA_nocape_rig'); arm=bpy.data.objects.new('VESPERA_nocape_rig',ad)
bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='EDIT')
eb={}
for n,par,h,t in BONES:
    b=ad.edit_bones.new(P_+n); b.head=Vector(J[h]); b.tail=Vector(J[t])
    if par: b.parent=eb[par]; b.use_connect=(np.allclose(J[h],eb[par].tail) and n not in ('LeftShoulder','RightShoulder','LeftUpLeg','RightUpLeg'))
    if 'Foot' in n or 'Toe' in n: b.align_roll(Vector((0,0,1)))
    else: b.align_roll(Vector((0,-1,0)))
    eb[n]=b
bpy.ops.object.mode_set(mode='OBJECT')
SEG={n:(J[h],J[t]) for n,par,h,t in BONES}
# ---------------- helpers ----------------
def proj(Pa,a,b):
    ab=b-a; L=np.linalg.norm(ab); t=((Pa-a)@ab)/(L*L); return t,L
def segdist(Pa,a,b):
    t,L=proj(Pa,a,b); tc=np.clip(t,0,1); q=a+tc[:,None]*(b-a); return np.linalg.norm(Pa-q,axis=1),t,L
def ss(e0,e1,x): t=np.clip((x-e0)/(e1-e0),0,1); return t*t*(3-2*t)
# ---------------- parts ----------------
from mathutils.bvhtree import BVHTree
def spine_axis(z):
    ks=('Hips','Spine','Spine1','Spine2','Neck','Head'); zs=np.array([J[k][2] for k in ks])
    return np.interp(z,zs,np.array([J[k][0] for k in ks])),np.interp(z,zs,np.array([J[k][1] for k in ks]))
def detect_hair(P,me,armcore):
    bvh=BVHTree.FromPolygons([v.co[:] for v in me.vertices],[p.vertices[:] for p in me.polygons])
    hair=np.zeros(len(P),bool)
    cand=np.nonzero((~armcore)&(P[:,2]>0.95)&(P[:,2]<1.62))[0]
    ax,ay=spine_axis(P[cand,2])
    for k,i in enumerate(cand):
        c=Vector(P[i]); A=Vector((ax[k],ay[k],P[i,2])); dv=A-c; L=dv.length
        if L<0.04: continue
        if P[i,1]<ay[k]-0.02 and abs(P[i,0]-ax[k])<0.16: continue
        hit=bvh.ray_cast(c+dv/L*0.006,dv/L,L-0.006)
        if hit[0] is not None and hit[3]>0.012: hair[i]=True
    return hair
def compute_parts(P,E,me):
    nbr_i=np.concatenate([E[:,0],E[:,1]]); nbr_j=np.concatenate([E[:,1],E[:,0]])
    def flood(seed,allowed):
        lab=seed&allowed
        for _ in range(600):
            m=lab[nbr_i]&allowed[nbr_j]&~lab[nbr_j]
            if not m.any(): break
            lab[nbr_j[m]]=True
        return lab
    N=len(P); part=np.zeros(N,np.int8); dist_chain={}; uarm={}
    dsp=np.min(np.stack([segdist(P,*SEG[k])[0] for k in ('Spine','Spine1','Spine2','Hips','Neck')],1),1)
    core=np.zeros(N,bool)
    for s in ('Left','Right'):
        d=np.stack([segdist(P,*SEG[s+k])[0] for k in ('Arm','ForeArm','Hand')],1); core|=(d<np.array([0.055,0.05,0.055])).any(1)
    hair=detect_hair(P,me,core)
    for s,pid in (('Left',1),('Right',2)):
        d=np.stack([segdist(P,*SEG[s+k])[0] for k in ('Arm','ForeArm','Hand')],1)
        dm=d.min(1)
        allowed=(d<np.array([0.060,0.055,0.060])).any(1)&((dm<dsp-0.02)|(dm<0.035))&(~hair|(dm<0.035))
        seed=(d<np.array([0.040,0.035,0.035])).any(1)&allowed
        lab=flood(seed,allowed)
        # second ring: pieces glued onto the arm (cape remnants, sleeve frills) ride the arm, never above the shoulder
        allowed2=(d[:,:2]<np.array([0.10,0.09])).any(1)&(dm<dsp-0.04)&(P[:,2]<SEG[s+'Arm'][0][2]-0.03)&~hair&(P[:,2]>SEG[s+'Hand'][0][2]+0.03)
        lab=flood(lab,lab|allowed2)
        part[lab&(part==0)]=pid; dist_chain[s+'arm']=d
        t1,L1=proj(P,*SEG[s+'Arm']); uarm[s]=t1*L1
    for s,pid in (('Left',3),('Right',4)):
        d=np.stack([segdist(P,*SEG[s+k])[0] for k in ('UpLeg','Leg','Foot','ToeBase')],1)
        allowed=((d<np.array([0.11,0.085,0.10,0.07])).any(1)&(P[:,2]<0.92))|(P[:,2]<0.80)
        seed=(P[:,2]<0.70)&allowed
        lab=flood(seed,allowed); part[lab&(part==0)]=pid; dist_chain[s+'leg']=d
    both=(part==3)|(part==4)
    dl=dist_chain['Leftleg'].min(1); dr=dist_chain['Rightleg'].min(1)
    part[both&(dl<dr)]=3; part[both&(dr<=dl)]=4
    part[hair&(part>=3)]=part[hair&(part>=3)]
    return part,dist_chain,uarm,hair
part,dist_chain,uarm,hair=compute_parts(P,E,me)
# ---------------- split glued contacts: every cross-part edge outside the shoulder / hip joint zones is a weld
# between separate surfaces (hand on skirt, hair on sleeve, frills); disconnect them, relabel, repeat until stable
def joint_ok(P,E,part):
    pa,pb=part[E[:,0]],part[E[:,1]]
    nearsh=np.minimum(np.linalg.norm(P-J['LeftArm'],axis=1),np.linalg.norm(P-J['RightArm'],axis=1))
    return (pa==pb)|(((pa<=2)&(pb<=2))&(nearsh[E[:,0]]<0.20)&(nearsh[E[:,1]]<0.20))|((((pa==0)&(pb>=3))|((pb==0)&(pa>=3)))&(P[E[:,0],2]>0.74)&(P[E[:,1],2]>0.74))
# label faces by vertex-majority, split every edge between faces of different parts outside the joint zones
def face_parts(part):
    fp=np.zeros(len(me.polygons),int)
    for f in me.polygons:
        c=np.bincount(part[list(f.vertices)],minlength=6); c[0]-=0.5  # ties go to the limb
        fp[f.index]=int(np.argmax(c))
    return fp
fp=face_parts(part)
bm=bmesh.new(); bm.from_mesh(me); bm.edges.ensure_lookup_table(); bm.faces.ensure_lookup_table()
Jl=np.array(J['LeftArm']); Jr=np.array(J['RightArm'])
cut=[]
for e in bm.edges:
    if len(e.link_faces)<2: continue
    ps=set(fp[f.index] for f in e.link_faces)
    if len(ps)<2: continue
    m=np.array((e.verts[0].co+e.verts[1].co)/2)
    lims={p for p in ps if p!=0}
    if all(p in (1,2) for p in lims) and min(np.linalg.norm(m-Jl),np.linalg.norm(m-Jr))<0.20: continue
    if all(p>=3 for p in lims) and m[2]>0.74 and len(lims)==1: continue
    cut.append(e)
SPLIT=len(cut)
bmesh.ops.split_edges(bm,edges=cut)
bm.faces.ensure_lookup_table()
part=np.zeros(len(bm.verts),int)
for v in bm.verts:
    c=np.bincount([fp[f.index] for f in v.link_faces],minlength=6) if v.link_faces else np.array([1])
    part[v.index]=int(np.argmax(c))
bm.to_mesh(me); me.update(); bm.free()
N=len(me.vertices); co=np.empty(N*3); me.vertices.foreach_get('co',co); P=co.reshape(-1,3)
E=np.array([e.vertices[:] for e in me.edges])
_,dist_chain,uarm,hair=compute_parts(P,E,me)
RESID=int((~joint_ok(P,E,part)).sum())
HAIR=int(hair.sum())
names=[P_+n for n,_,_,_ in BONES]; bi={n:i for i,n in enumerate(names)}
W=np.zeros((N,len(names)))
def add(name,w,mask=None):
    if mask is None: W[:,bi[P_+name]]+=w
    else: W[mask,bi[P_+name]]+=w[mask] if np.ndim(w) else w
# ---------------- torso / head: spine arc ----------------
SP=['Hips','Spine','Spine1','Spine2','Neck','Head']
pts=[J['Hips'],J['Spine'],J['Spine1'],J['Spine2'],J['Neck'],J['Head'],J['HeadTop']]
# arc coordinate by height (spine is near vertical)
zj=np.array([p[2] for p in pts])
z=P[:,2]
# bone influence: hat functions centred on the bone joints so each bone owns its own segment, blending across joints
def chain_weights(u,U,b):
    # U: joint arc positions (len nseg+1), b: blend half width per inner joint; returns (N,nseg)
    nseg=len(U)-1; S=[ss(U[k]-b[k-1],U[k]+b[k-1],u) for k in range(1,nseg)]
    out=[]
    for k in range(nseg):
        w=np.ones_like(u)
        if k>0: w*=S[k-1]
        if k<nseg-1: w*=1-S[k]
        out.append(w)
    return np.stack(out,1)
torso=(part==0)
tw=chain_weights(z,zj,[0.05,0.05,0.06,0.04,0.035])
hair=hair&torso
body_t=torso&~hair
for k,n in enumerate(SP): add(n,tw[:,k]*body_t)
wn=ss(1.40,1.56,z); wh=ss(1.58,1.66,z)
add('Spine2',(1-wn)*hair); add('Neck',wn*(1-wh)*hair); add('Head',wn*wh*hair)
# clavicles: torso vertices high and toward each arm root
for s in ('Left','Right'):
    a,b=SEG[s+'Shoulder']; t,L=proj(P,a,b); d,_,_=segdist(P,a,b)
    wc=ss(0.25,0.85,t)*ss(1.32,1.42,z)*(d<0.12)*torso
    for k in range(W.shape[1]): W[:,k]*=(1-wc)
    add(s+'Shoulder',wc)
# ---------------- arms ----------------
for s,pid in (('Left',1),('Right',2)):
    m=part==pid
    A,F,H,He=J[s+'Arm'],J[s+'ForeArm'],J[s+'Hand'],J[s+'HandEnd']
    L1=np.linalg.norm(F-A); L2=np.linalg.norm(H-F); L3=np.linalg.norm(He-H)
    d=dist_chain[s+'arm']; k=d.argmin(1)
    t1,_=proj(P,A,F); t2,_=proj(P,F,H); t3,_=proj(P,H,He)
    u=np.where(k==0,t1*L1,np.where(k==1,L1+np.clip(t2,0,1.5)*L2,L1+L2+np.clip(t3,0,2)*L3))
    u=np.where((k==0),t1*L1,u)
    cw=chain_weights(u,np.array([0,L1,L1+L2,L1+L2+L3]),[0.045,0.03])
    add(s+'Arm',cw[:,0]*m); add(s+'ForeArm',cw[:,1]*m); add(s+'Hand',cw[:,2]*m)
# ---------------- shoulder seam blend: geodesic distance to the arm/torso boundary, both sides ----------------
def geo_from(src,mask,lim=0.12,iters=80):
    D=np.where(src,0.0,np.inf); em=mask[E[:,0]]&mask[E[:,1]]; a_,b_=E[em,0],E[em,1]
    Le=np.linalg.norm(P[a_]-P[b_],axis=1)
    for _ in range(iters):
        nd=np.minimum(D[a_]+Le,lim*2); np.minimum.at(D,b_,nd)
        nd=np.minimum(D[b_]+Le,lim*2); np.minimum.at(D,a_,nd)
    return D
okx0=np.ones(len(E),bool)   # every edge still connected after the face split is real surface
AZONE={}
SH_BLEND={}
for s_,pid in (('Left',1),('Right',2)):
    near=(np.linalg.norm(P-J[s_+'Arm'],axis=1)<0.45)&(P[:,2]>J[s_+'ForeArm'][2]+0.02)
    ea,eb=E[:,0],E[:,1]
    cross=okx0&(((part[ea]==pid)&(part[eb]==0))|((part[ea]==0)&(part[eb]==pid)))&near[ea]&near[eb]
    bnd=np.zeros(N,bool); bnd[ea[cross]]=True; bnd[eb[cross]]=True
    Da=geo_from(bnd&(part==pid),(part==pid)&near); Dt=geo_from(bnd&(part==0),(part==0)&near)
    sg=np.where(part==pid,Da,np.where(part==0,-Dt,np.nan))
    a=np.clip((sg+0.05)/0.11,0,1); a=a*a*(3-2*a); a=np.nan_to_num(a,nan=0.0)
    zone=near&((part==pid)|(part==0))&np.isfinite(np.where(part==pid,Da,Dt))&(np.where(part==pid,Da,Dt)<0.2)
    am=zone&(part==pid); tm=zone&(part==0)
    armcols=[bi[P_+s_+n] for n in ('Arm','ForeArm','Hand')]
    # arm side: limb weights * a, the rest to clavicle/chest
    lim_w=W[am][:,armcols].copy()
    W[np.ix_(am,armcols)]=lim_w*a[am][:,None]
    W[am,bi[P_+s_+'Shoulder']]=W[am,bi[P_+s_+'Shoulder']]*0+(1-a[am])*0.6
    W[am,bi[P_+'Spine2']]=(1-a[am])*0.4
    # torso side: torso weights * (1-a), upper arm a
    W[tm]*= (1-a[tm])[:,None]
    W[tm,bi[P_+s_+'Arm']]+=a[tm]
    AZONE[s_]=(a,zone)
    SH_BLEND[s_]=int(((a>0.02)&(a<0.98)&zone).sum())
# ---------------- legs ----------------
for s,pid in (('Left',3),('Right',4)):
    m=part==pid
    Hh,K,A,T,Te=J[s+'UpLeg'],J[s+'Leg'],J[s+'Foot'],J[s+'ToeBase'],J[s+'ToeEnd']
    L1=np.linalg.norm(K-Hh); L2=np.linalg.norm(A-K)
    d=dist_chain[s+'leg']; k=d.argmin(1)
    t1,_=proj(P,Hh,K); t2,_=proj(P,K,A)
    u=np.where(k==0,t1*L1,L1+np.clip(t2,0,1.2)*L2)
    # foot: vertices below the ankle go to Foot/ToeBase by distance along the foot
    cw=chain_weights(u,np.array([0,L1,L1+L2,L1+L2+0.3]),[0.06,0.035])
    root=1-ss(-0.03,0.12,u)
    footm=(k>=2)|(u>L1+L2-0.01)
    tf,Lf=proj(P,A,T)
    wt=ss(0.75,1.05,tf)          # toe bone takes the front of the shoe past the ball
    wfoot=cw[:,2]
    add(s+'UpLeg',cw[:,0]*(1-root)*m); add('Hips',cw[:,0]*root*m); add(s+'Leg',cw[:,1]*m)
    add(s+'Foot',wfoot*(1-wt)*m); add(s+'ToeBase',wfoot*wt*m)
# skirt / hip band in torso part: let the lower skirt follow the thighs a little so legs don't poke through
for s in ('Left','Right'):
    Hh,K=J[s+'UpLeg'],J[s+'Leg']; d,t,_=segdist(P,Hh,K)
    other=J['RightUpLeg'] if s=='Left' else J['LeftUpLeg']
    closer=d<segdist(P,other,J['RightLeg'] if s=='Left' else J['LeftLeg'])[0]
    wl=torso*closer*ss(0.93,0.80,z)*ss(0.20,0.10,d)*0.45
    for k in range(W.shape[1]): W[:,k]*=(1-wl)
    add(s+'UpLeg',wl)
# ---------------- smooth inside parts, prune, normalize ----------------
# smoothing graph: inside a part, plus across the shoulder and hip joint zones
dA={s:np.linalg.norm(P-J[s+'Arm'],axis=1) for s in ('Left','Right')}
okx=np.ones(len(E),bool)
ei=np.concatenate([E[okx,0],E[okx,1]]); ej=np.concatenate([E[okx,1],E[okx,0]])
deg=np.bincount(ei,minlength=N).astype(float)
s=W.sum(1); W[s<1e-6,bi[P_+'Hips']]=1.0; W/=W.sum(1,keepdims=True)
for it in range(10):
    acc=np.zeros_like(W); np.add.at(acc,ei,W[ej])
    Wn=np.where(deg[:,None]>0,acc/np.maximum(deg,1)[:,None],W)
    W=0.5*W+0.5*Wn
# allowed bones per part (no weight on far-away bones)
def cols(*ns): return [bi[P_+n] for n in ns]
allow=np.zeros_like(W,bool)
sp=cols('Hips','Spine','Spine1','Spine2','Neck','Head','LeftShoulder','RightShoulder')
t=part==0
allow[np.ix_(t,sp)]=True
for sd,pid,lp in (('Left',1,3),('Right',2,4)):
    a_,zn=AZONE[sd]; near=zn&(a_<0.999)
    allow[np.ix_(t&zn&(a_>0.001),cols(sd+'Arm'))]=True
    allow[np.ix_(t&(P[:,2]<1.0),cols(sd+'UpLeg'))]=True
    a=part==pid
    allow[np.ix_(a,cols(sd+'Arm',sd+'ForeArm',sd+'Hand'))]=True
    allow[np.ix_(a&near,cols(sd+'Shoulder','Spine2','Spine1'))]=True
    l=part==lp
    allow[np.ix_(l,cols(sd+'UpLeg',sd+'Leg',sd+'Foot',sd+'ToeBase'))]=True
    allow[np.ix_(l&(P[:,2]>0.70),cols('Hips','Spine'))]=True
LEAK=int(((W>0.02)&~allow).any(1).sum())
W=W*allow
W[W<0.02]=0
order=np.argsort(-W,1); keep=np.zeros_like(W,bool); np.put_along_axis(keep,order[:,:4],True,1); W*=keep
z0=W.sum(1)<1e-6
if z0.any():
    for i in np.nonzero(z0)[0]: W[i,np.nonzero(allow[i])[0][0]]=1.0
W/=W.sum(1,keepdims=True)
# ---------------- write groups + modifier ----------------
body.vertex_groups.clear()
for n in names:
    g=body.vertex_groups.new(name=n); col=W[:,bi[n]]; idx=np.nonzero(col>0)[0]
    for i in idx: g.add([int(i)],float(col[i]),'REPLACE')
body.parent=arm
mod=body.modifiers.new('Armature','ARMATURE'); mod.object=arm; mod.use_vertex_groups=True
# ---------------- report ----------------
far=0
heads={n:SEG[n[len(P_):]] for n in names}
for n in names:
    a,b=heads[n]; d,_,_=segdist(P,a,b); m=W[:,bi[n]]>0.05; far+=int((m&(d>0.30)).sum())
rep={'shoulder_blend_verts':SH_BLEND,'residual_cross_part_edges':RESID,'masked_far_bone_verts':LEAK,'split_contact_edges':SPLIT,'hair_verts':HAIR,'bones':len(names),'verts':N,'max_influences':int((W>0).sum(1).max()),'unweighted':int((W.sum(1)<0.999).sum()),
 'parts':{k:int((part==v).sum()) for k,v in (('torso_head',0),('L_arm',1),('R_arm',2),('L_leg',3),('R_leg',4))},
 'verts_weight_gt0.05_farther_than_30cm_from_bone':far,'influence_hist':np.bincount((W>0).sum(1),minlength=5).tolist()}
np.save(OUTBLEND.replace('.blend','_part.npy'),part+5*hair)
bpy.ops.wm.save_as_mainfile(filepath=OUTBLEND,compress=True)
json.dump(rep,open(OUTJSON,'w'),indent=1); print('RIG',json.dumps(rep))
