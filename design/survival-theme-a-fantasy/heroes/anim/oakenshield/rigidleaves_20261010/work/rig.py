"""Oakenshield rigid-leaves re-rig (2026-10-10). Input prep.blend (cloth object + 14 cloth bones removed).
1. Refit the 22 mixamorig joints to the mesh (joints_new.json, measured from voxel cross-section cores: the bind pose has
   the pelvis turned ~50 deg to her right with the left leg in front, so the old straight-down leg bones sat 10-18 cm off).
2. Parts: arms flood-filled inside capsules, legs below the belt by leg chain, torso/head the rest.
3. Welds between parts outside the shoulder/hip joint zones are split; the openings are filled (UVs from the rim -> nearby paint).
4. Weights: chain weights with joint blends only, smoothing inside each part, far bones masked, <=4 influences.
5. Rigid pieces: every protruding piece (>1.5 cm outside the voxel core: leaves, spikes, claws, vines) that sits mostly on one
   bone is skinned 100% to that bone."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, bmesh, numpy as np, json, math
from mathutils import Vector
from scipy import ndimage
args=sys.argv[sys.argv.index('--')+1:]; JOINTS=args[0]; OUTBLEND=args[1]; OUTJSON=args[2]
J={k:np.array(v,float) for k,v in json.load(open(JOINTS)).items()}
P_='mixamorig:'
arm=bpy.data.objects['OAKENSHIELD_rig']; body=bpy.data.objects['OAKENSHIELD_body']; me=body.data
rep={}
# ---------------- skeleton: same 22 bones / names / hierarchy, new joints ----------------
BONES=[('Hips',None,'Hips','Spine'),('Spine','Hips','Spine','Spine1'),('Spine1','Spine','Spine1','Spine2'),('Spine2','Spine1','Spine2','Neck'),
 ('Neck','Spine2','Neck','Head'),('Head','Neck','Head','HeadTop')]
for s in ('Left','Right'):
    BONES+=[(s+'Shoulder','Spine2',s+'Shoulder',s+'Arm'),(s+'Arm',s+'Shoulder',s+'Arm',s+'ForeArm'),(s+'ForeArm',s+'Arm',s+'ForeArm',s+'Hand'),(s+'Hand',s+'ForeArm',s+'Hand',s+'HandEnd')]
for s in ('Left','Right'):
    BONES+=[(s+'UpLeg','Hips',s+'UpLeg',s+'Leg'),(s+'Leg',s+'UpLeg',s+'Leg',s+'Foot'),(s+'Foot',s+'Leg',s+'Foot',s+'ToeBase'),(s+'ToeBase',s+'Foot',s+'ToeBase',s+'ToeEnd')]
old_heads={b.name:list(b.head_local) for b in arm.data.bones}
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
ebs=arm.data.edit_bones
for b in ebs: b.use_connect=False
for n,par,h,t in BONES:
    b=ebs[P_+n]; b.head=Vector(J[h]); b.tail=Vector(J[t])
    if 'Foot' in n or 'Toe' in n: b.align_roll(Vector((0,0,1)))
    else: b.align_roll(Vector((0,-1,0)))
for n,par,h,t in BONES:
    b=ebs[P_+n]
    if par and np.allclose(J[h],J[[x for x in BONES if x[0]==par][0][3]]) and n not in ('LeftShoulder','RightShoulder','LeftUpLeg','RightUpLeg'): b.use_connect=True
bpy.ops.object.mode_set(mode='OBJECT')
rep['joint_moves_cm']={n:round(float(np.linalg.norm(np.array(old_heads[P_+n])-J[n]))*100,1) for n,_,_,_ in BONES}
SEG={n:(J[h],J[t]) for n,par,h,t in BONES}
def proj(Pa,a,b):
    ab=b-a; L=np.linalg.norm(ab); t=((Pa-a)@ab)/(L*L); return t,L
def segdist(Pa,a,b):
    t,L=proj(Pa,a,b); tc=np.clip(t,0,1); q=a+tc[:,None]*(b-a); return np.linalg.norm(Pa-q,axis=1),t,L
def ss(e0,e1,x): t=np.clip((x-e0)/(e1-e0),0,1); return t*t*(3-2*t)
def arrays():
    N=len(me.vertices); co=np.empty(N*3); me.vertices.foreach_get('co',co); P=co.reshape(-1,3)
    E=np.array([e.vertices[:] for e in me.edges]); return N,P,E
N,P,E=arrays()
bm0=bmesh.new(); bm0.from_mesh(me); rep['boundary_edges_before']=sum(1 for e in bm0.edges if e.is_boundary); bm0.free()
LEGZ=0.80   # below the belt everything that is not hand is leg
def compute_parts(P,E):
    nbr_i=np.concatenate([E[:,0],E[:,1]]); nbr_j=np.concatenate([E[:,1],E[:,0]])
    def flood(seed,allowed):
        lab=seed&allowed
        for _ in range(800):
            m=lab[nbr_i]&allowed[nbr_j]&~lab[nbr_j]
            if not m.any(): break
            lab[nbr_j[m]]=True
        return lab
    N=len(P); part=np.zeros(N,np.int8); dist_chain={}
    dsp=np.min(np.stack([segdist(P,*SEG[k])[0] for k in ('Hips','Spine','Spine1','Spine2','Neck')],1),1)
    for s,pid in (('Left',1),('Right',2)):
        d=np.stack([segdist(P,*SEG[s+k])[0] for k in ('Arm','ForeArm','Hand')],1); dm=d.min(1)
        allowed=(d<np.array([0.065,0.065,0.085])).any(1)&((dm<dsp-0.02)|(dm<0.035))
        seed=(d<np.array([0.040,0.035,0.04])).any(1)&allowed
        lab=flood(seed,allowed)
        # pieces glued onto the forearm/upper arm (bracer leaves, vine loops) ride the arm, never above the shoulder
        # (bracer leaves stick out up to ~20 cm from the forearm; near the hands stay tight so belt leaves never ride the hand)
        allowed2=(dm<dsp-0.04)&(P[:,2]<SEG[s+'Arm'][0][2]-0.03)&((((d[:,:2]).min(1)<0.22)&(P[:,2]>1.02))|(dm<0.075))
        lab=flood(lab,lab|allowed2)
        part[lab&(part==0)]=pid; dist_chain[s+'arm']=d
    for s,pid in (('Left',3),('Right',4)):
        d=np.stack([segdist(P,*SEG[s+k])[0] for k in ('UpLeg','Leg','Foot','ToeBase')],1)
        dist_chain[s+'leg']=d
    dl=dist_chain['Leftleg'].min(1); dr=dist_chain['Rightleg'].min(1)
    legish=(part==0)&(((np.minimum(dl,dr)<0.10)&(P[:,2]<0.90))|(P[:,2]<LEGZ))
    seed=legish&(P[:,2]<0.70); lab=flood(seed,legish)
    part[lab&(dl<dr)]=3; part[lab&(dr<=dl)]=4
    # leg side by connectivity: flood each leg from its own foot inside the leg set, then nearest chain for the rest
    for it in range(2):
        L=flood((part==3)&(P[:,2]<0.35)&(dl<dr),(part==3)|(part==4)); R=flood((part==4)&(P[:,2]<0.35)&(dr<dl),(part==3)|(part==4))
        only=L&~R; part[only]=3; only=R&~L; part[only]=4
    return part,dist_chain
part,dist_chain=compute_parts(P,E)
rep['parts_initial']={k:int((part==v).sum()) for k,v in (('torso_head',0),('L_arm',1),('R_arm',2),('L_leg',3),('R_leg',4))}
# ---------------- split welds ----------------
def face_parts(part):
    fv=np.array([p.vertices[:] for p in me.polygons]); fp=np.zeros(len(fv),int)
    c=np.stack([(part[fv]==k).sum(1) for k in range(5)],1).astype(float); c[:,0]-=0.5
    return c.argmax(1)
fp=face_parts(part)
bm=bmesh.new(); bm.from_mesh(me); bm.edges.ensure_lookup_table(); bm.faces.ensure_lookup_table()
Jl=J['LeftArm']; Jr=J['RightArm']
cut=[]; cutkind={}
for e in bm.edges:
    if len(e.link_faces)<2: continue
    ps=set(int(fp[f.index]) for f in e.link_faces)
    if len(ps)<2: continue
    m=np.array((e.verts[0].co+e.verts[1].co)/2)
    lims={p for p in ps if p!=0}
    if all(p in (1,2) for p in lims) and 0 in ps and min(np.linalg.norm(m-Jl),np.linalg.norm(m-Jr))<0.20: continue   # shoulder joint
    if all(p>=3 for p in lims) and 0 in ps and m[2]>0.74: continue        # hip joint (torso<->one leg)
    if lims=={3,4} and m[2]>0.78: continue                                 # crotch
    k='-'.join(map(str,sorted(ps))); cutkind[k]=cutkind.get(k,0)+1
    cut.append(e)
rep['weld_edges_split']=len(cut); rep['weld_split_by_parts']=cutkind
bmesh.ops.split_edges(bm,edges=cut)
# fill the openings the split leaves (small loops), UVs from the rim so the patch takes nearby paint
bm.edges.ensure_lookup_table(); bm.faces.ensure_lookup_table(); bm.verts.ensure_lookup_table()
uvl=bm.loops.layers.uv.active
nf0=len(bm.faces)
bnd=[e for e in bm.edges if e.is_boundary]
rep['boundary_edges_after_split']=len(bnd)
res=bmesh.ops.holes_fill(bm,edges=bnd,sides=0)
newf=res['faces']
bmesh.ops.triangulate(bm,faces=newf)
bm.faces.ensure_lookup_table()
newf=[f for f in bm.faces if f.index>=nf0] if False else [f for f in bm.faces if f.index>=nf0]
for f in newf:
    for l in f.loops:
        v=l.vert; src=[ll for ll in v.link_loops if ll.face.index<nf0]
        if src: l[uvl].uv=src[0][uvl].uv.copy()
rep['fill_faces']=len(newf)
bm.faces.ensure_lookup_table()
fpn=list(fp)
for f in bm.faces[nf0:]:
    c=np.bincount([fpn[g.index] for v in f.verts for g in v.link_faces if g.index<nf0],minlength=5); fpn.append(int(np.argmax(c)) if c.sum() else 0)
# open split rims that are not closed loops (slits along the arm-on-belt contact): give the two rings of faces along the rim a
# flipped twin (same UVs/paint), so the inside of the cut surface shows paint instead of see-through
bnd=[e for e in bm.edges if e.is_boundary]
rimf=set(f for e in bnd for f in e.link_faces)
rim2=set(rimf)
for f in rimf:
    for v in f.verts: rim2.update(v.link_faces)
rim2=[f for f in rim2]
srcpart=[fpn[f.index] for f in rim2]
nf1=len(bm.faces)
res=bmesh.ops.duplicate(bm,geom=rim2)
fmap=res['face_map']
newtw=[fmap[f] for f in rim2]
bmesh.ops.reverse_faces(bm,faces=newtw)
bm.faces.ensure_lookup_table()
fpn=np.array(fpn+[0]*(len(bm.faces)-len(fpn)))
for f,p in zip(rim2,srcpart): fpn[fmap[f].index]=p
rep['rim_twin_faces']=len(newtw)
bm.verts.index_update(); bm.verts.ensure_lookup_table()
TWIN_SRC=[(res['vert_map'][v].index,v.index) for v in set(v for f in rim2 for v in f.verts)]
rep['boundary_edges_after_fill']=sum(1 for e in bm.edges if e.is_boundary)
part_v=np.zeros(len(bm.verts),int)
for v in bm.verts:
    c=np.bincount([fpn[f.index] for f in v.link_faces],minlength=5) if v.link_faces else np.array([1])
    part_v[v.index]=int(np.argmax(c)) if c.sum() else 0
bm.to_mesh(me); me.update(); bm.free()
fill_verts=np.zeros(len(me.vertices),bool)
for p in me.polygons:
    if p.index>=nf0: fill_verts[list(p.vertices)]=True
N,P,E=arrays(); part=part_v
# small loose islands (claw tips, vine bits cut off by the split) belong to the part they sit next to
import scipy.sparse as sp0, scipy.sparse.csgraph as cg0
from scipy.spatial import cKDTree
nI,ilab=cg0.connected_components(sp0.coo_matrix((np.ones(len(E)),(E[:,0],E[:,1])),shape=(N,N)),directed=False)
isz=np.bincount(ilab); main=isz.argmax()
small_isl=[k for k in range(nI) if isz[k]<300]
tree=cKDTree(P[ilab==main]); mainidx=np.nonzero(ilab==main)[0]
ISL_NEAR={}
for k in small_isl:
    idx=np.nonzero(ilab==k)[0]; d,j=tree.query(P[idx]); jj=mainidx[j[d.argmin()]]
    part[idx]=part[jj]; ISL_NEAR[k]=(idx,mainidx[j])
rep['small_islands_reattached']=len(small_isl)
_,dist_chain=compute_parts(P,E)
rep['parts']={k:int((part==v).sum()) for k,v in (('torso_head',0),('L_arm',1),('R_arm',2),('L_leg',3),('R_leg',4))}
names=[P_+n for n,_,_,_ in BONES]; bi={n:i for i,n in enumerate(names)}
W=np.zeros((N,len(names)))
def add(name,w,mask=None):
    W[:,bi[P_+name]]+=w if mask is None else w*mask
def chain_weights(u,U,b):
    nseg=len(U)-1; S=[ss(U[k]-b[k-1],U[k]+b[k-1],u) for k in range(1,nseg)]
    out=[]
    for k in range(nseg):
        w=np.ones_like(u)
        if k>0: w*=S[k-1]
        if k<nseg-1: w*=1-S[k]
        out.append(w)
    return np.stack(out,1)
# ---------------- torso / head ----------------
SP=['Hips','Spine','Spine1','Spine2','Neck','Head']
zj=np.array([J[k][2] for k in SP+['HeadTop']]); z=P[:,2]; torso=(part==0)
tw=chain_weights(z,zj,[0.05,0.06,0.06,0.045,0.05])
for k,n in enumerate(SP): add(n,tw[:,k]*torso)
for s in ('Left','Right'):
    a,b=SEG[s+'Shoulder']; t,L=proj(P,a,b); d,_,_=segdist(P,a,b)
    wc=ss(0.2,0.8,t)*ss(1.36,1.44,z)*(d<0.13)*torso
    W*=(1-wc)[:,None]; add(s+'Shoulder',wc)
# ---------------- arms ----------------
for s,pid in (('Left',1),('Right',2)):
    m=part==pid
    A,F,H,He=J[s+'Arm'],J[s+'ForeArm'],J[s+'Hand'],J[s+'HandEnd']
    L1=np.linalg.norm(F-A); L2=np.linalg.norm(H-F); L3=np.linalg.norm(He-H)
    d=dist_chain[s+'arm']; k=d.argmin(1)
    t1,_=proj(P,A,F); t2,_=proj(P,F,H); t3,_=proj(P,H,He)
    u=np.where(k==0,t1*L1,np.where(k==1,L1+np.clip(t2,0,1.5)*L2,L1+L2+np.clip(t3,0,2)*L3))
    cw=chain_weights(u,np.array([0,L1,L1+L2,L1+L2+L3]),[0.045,0.03])
    add(s+'Arm',cw[:,0]*m); add(s+'ForeArm',cw[:,1]*m); add(s+'Hand',cw[:,2]*m)
# shoulder seam blend: geodesic distance to the arm/torso boundary
def geo_from(src,mask,lim=0.12,iters=80):
    D=np.where(src,0.0,np.inf); em=mask[E[:,0]]&mask[E[:,1]]; a_,b_=E[em,0],E[em,1]
    Le=np.linalg.norm(P[a_]-P[b_],axis=1)
    for _ in range(iters):
        nd=np.minimum(D[a_]+Le,lim*2); np.minimum.at(D,b_,nd)
        nd=np.minimum(D[b_]+Le,lim*2); np.minimum.at(D,a_,nd)
    return D
AZONE={}
for s_,pid in (('Left',1),('Right',2)):
    near=(np.linalg.norm(P-J[s_+'Arm'],axis=1)<0.40)&(P[:,2]>J[s_+'ForeArm'][2]+0.02)
    ea,eb=E[:,0],E[:,1]
    cross=(((part[ea]==pid)&(part[eb]==0))|((part[ea]==0)&(part[eb]==pid)))&near[ea]&near[eb]
    bnd=np.zeros(N,bool); bnd[ea[cross]]=True; bnd[eb[cross]]=True
    Da=geo_from(bnd&(part==pid),(part==pid)&near); Dt=geo_from(bnd&(part==0),(part==0)&near)
    sg=np.where(part==pid,Da,np.where(part==0,-Dt,np.nan))
    a=np.clip((sg+0.05)/0.11,0,1); a=a*a*(3-2*a); a=np.nan_to_num(a,nan=0.0)
    zone=near&((part==pid)|(part==0))&np.isfinite(np.where(part==pid,Da,Dt))&(np.where(part==pid,Da,Dt)<0.2)
    am=zone&(part==pid); tm=zone&(part==0)
    armcols=[bi[P_+s_+n] for n in ('Arm','ForeArm','Hand')]
    W[np.ix_(am,armcols)]=W[am][:,armcols]*a[am][:,None]
    W[am,bi[P_+s_+'Shoulder']]=(1-a[am])*0.6; W[am,bi[P_+'Spine2']]=(1-a[am])*0.4
    W[tm]*=(1-a[tm])[:,None]; W[tm,bi[P_+s_+'Arm']]+=a[tm]
    AZONE[s_]=(a,zone)
# ---------------- legs ----------------
for s,pid in (('Left',3),('Right',4)):
    m=part==pid
    Hh,K,A,T,Te=J[s+'UpLeg'],J[s+'Leg'],J[s+'Foot'],J[s+'ToeBase'],J[s+'ToeEnd']
    L1=np.linalg.norm(K-Hh); L2=np.linalg.norm(A-K)
    d=dist_chain[s+'leg']; k=d.argmin(1)
    t1,_=proj(P,Hh,K); t2,_=proj(P,K,A)
    u=np.where(k==0,t1*L1,L1+np.clip(t2,0,1.3)*L2)
    u=np.where(k>=2,np.maximum(u,L1+L2),u)
    cw=chain_weights(u,np.array([0,L1,L1+L2,L1+L2+0.4]),[0.05,0.035])
    root=1-ss(-0.02,0.10,u)
    tf,Lf=proj(P,A,T); wt=ss(0.85,1.15,tf)
    add(s+'UpLeg',cw[:,0]*(1-root)*m); add('Hips',cw[:,0]*root*m); add(s+'Leg',cw[:,1]*m)
    add(s+'Foot',cw[:,2]*(1-wt)*m); add(s+'ToeBase',cw[:,2]*wt*m)
# belt band in the torso part follows the nearer thigh a little so the thighs don't poke out
for s in ('Left','Right'):
    o='Right' if s=='Left' else 'Left'
    d,_,_=segdist(P,*SEG[s+'UpLeg']); do,_,_=segdist(P,*SEG[o+'UpLeg'])
    wl=torso*(d<do)*ss(0.95,0.84,z)*ss(0.16,0.08,d)*0.4
    W*=(1-wl)[:,None]; add(s+'UpLeg',wl)
# ---------------- smooth inside parts (+ joint zones), mask far bones, prune ----------------
ea,eb=E[:,0],E[:,1]
okx=(part[ea]==part[eb])|((np.minimum(part[ea],part[eb])==0)&(P[ea,2]>0.74))   # joint zones keep their blend
ei=np.concatenate([ea[okx],eb[okx]]); ej=np.concatenate([eb[okx],ea[okx]])
deg=np.bincount(ei,minlength=N).astype(float)
s_=W.sum(1); W[s_<1e-6,bi[P_+'Hips']]=1.0; W/=W.sum(1,keepdims=True)
for it in range(10):
    acc=np.zeros_like(W); np.add.at(acc,ei,W[ej])
    W=0.5*W+0.5*np.where(deg[:,None]>0,acc/np.maximum(deg,1)[:,None],W)
def cols(*ns): return [bi[P_+n] for n in ns]
allow=np.zeros_like(W,bool)
t=part==0
allow[np.ix_(t,cols('Hips','Spine','Spine1','Spine2','Neck','Head','LeftShoulder','RightShoulder'))]=True
for sd,pid,lp in (('Left',1,3),('Right',2,4)):
    a_,zn=AZONE[sd]
    allow[np.ix_(t&zn&(a_>0.001),cols(sd+'Arm'))]=True
    allow[np.ix_(t&(P[:,2]<0.97),cols(sd+'UpLeg'))]=True
    a=part==pid
    allow[np.ix_(a,cols(sd+'Arm',sd+'ForeArm',sd+'Hand'))]=True
    allow[np.ix_(a&zn&(a_<0.999),cols(sd+'Shoulder','Spine2'))]=True
    l=part==lp
    allow[np.ix_(l,cols(sd+'UpLeg',sd+'Leg',sd+'Foot',sd+'ToeBase'))]=True
    allow[np.ix_(l&(P[:,2]>0.72),cols('Hips'))]=True
rep['masked_far_bone_verts']=int(((W>0.02)&~allow).any(1).sum())
W=W*allow; W[W<0.02]=0
order=np.argsort(-W,1); keep=np.zeros_like(W,bool); np.put_along_axis(keep,order[:,:4],True,1); W*=keep
z0=W.sum(1)<1e-6
for i in np.nonzero(z0)[0]: W[i,np.nonzero(allow[i])[0][0]]=1.0
W/=W.sum(1,keepdims=True)
# ---------------- rigid pieces ----------------
F=np.array([p.vertices[:] for p in me.polygons if len(p.vertices)==3])
g=0.005; lo=P.min(0)-0.03; shp=np.ceil((P.max(0)+0.03-lo)/g).astype(int)+1
V=np.zeros(shp,bool); A_,B_,C_=P[F[:,0]],P[F[:,1]],P[F[:,2]]
Lm=np.maximum.reduce([np.linalg.norm(A_-B_,axis=1),np.linalg.norm(B_-C_,axis=1),np.linalg.norm(C_-A_,axis=1)])
nn=np.clip(np.ceil(Lm/(g*0.5)).astype(int),1,40)
for k in np.unique(nn):
    mk=nn==k; a,b,c=A_[mk],B_[mk],C_[mk]
    for i in range(k+1):
        for j in range(k+1-i):
            p=a+(b-a)*(i/k)+(c-a)*(j/k); ij=np.floor((p-lo)/g).astype(int); V[ij[:,0],ij[:,1],ij[:,2]]=True
solid=ndimage.binary_erosion(ndimage.binary_fill_holes(ndimage.binary_dilation(V,iterations=1)),iterations=1)|V
ball=np.zeros((9,9,9),bool); zz,yy,xx=np.mgrid[-4:5,-4:5,-4:5]; ball=(xx*xx+yy*yy+zz*zz)<=16
core=ndimage.binary_opening(solid,structure=ball)
dcore=ndimage.distance_transform_edt(~core)*g
ij=np.clip(np.floor((P-lo)/g).astype(int),0,np.array(shp)-1)
dv=dcore[ij[:,0],ij[:,1],ij[:,2]]
orn=dv>0.015
rep['ornament_verts']=int(orn.sum())
# components of ornament verts (mesh edges inside the ornament set)
em=orn[ea]&orn[eb]
import scipy.sparse as sp_, scipy.sparse.csgraph as cg
G=sp_.coo_matrix((np.ones(int(em.sum())),(ea[em],eb[em])),shape=(N,N))
nc,lab=cg.connected_components(G,directed=False)
lab=np.where(orn,lab,-1)
rigid=0; kept_smooth=0; rigid_by_bone={}; big=[]
W0=W.copy()
core_v=~orn
# root = ornament verts with a core neighbour; the piece goes 100% to the bone that dominates where it is attached
rootw=np.zeros_like(W); rootn=np.zeros(N)
m1=orn[ea]&core_v[eb]; np.add.at(rootw,ea[m1],W0[eb[m1]]); np.add.at(rootn,ea[m1],1)
m2=orn[eb]&core_v[ea]; np.add.at(rootw,eb[m2],W0[ea[m2]]); np.add.at(rootn,eb[m2],1)
piece_bone=-np.ones(N,int)
for c in np.unique(lab[lab>=0]):
    idx=np.nonzero(lab==c)[0]
    if len(idx)<3: continue
    r=idx[rootn[idx]>0]
    tot=rootw[r].sum(0) if len(r) else W0[idx].sum(0)
    if (part[idx]==0).all() and P[idx,2].min()>1.56 and P[idx,2].mean()>1.62: tot=tot*0; tot[bi[P_+'Head']]=1
    b=int(tot.argmax()); share=tot[b]/max(tot.sum(),1e-9)
    if share>=0.5:
        W[idx]=0; W[idx,b]=1.0; rigid+=1; rigid_by_bone[names[b]]=rigid_by_bone.get(names[b],0)+1; piece_bone[idx]=b
    else:
        kept_smooth+=1; big.append((len(idx),round(float(share),2),names[b]))
# small loose islands: 100% to the bone that dominates the surface they touch
for k,(idx,nb) in ISL_NEAR.items():
    tot=W[nb].mean(0); tot[tot<0.05]=0; top=np.argsort(-tot)[4:]; tot[top]=0; tot/=tot.sum()
    W[idx]=tot[None,:]; piece_bone[idx]=-1   # one shared weight set: moves as one piece, follows its contact
# skin ring around each root eases into the piece bone (3 cm), so the attachment seam does not open
src=piece_bone>=0
D=np.where(src,0.0,np.inf); PB=piece_bone.copy(); Le=np.linalg.norm(P[ea]-P[eb],axis=1)
for it in range(40):
    for x,y in ((ea,eb),(eb,ea)):
        nd=D[x]+Le; upd=(nd<D[y])&core_v[y]&(nd<0.03)
        if upd.any():
            yy=y[upd]; D[yy]=nd[upd]; PB[yy]=PB[x[upd]]
ring=core_v&np.isfinite(D)&(PB>=0)
fall=np.zeros(N); fall[ring]=1-ss(0.0,0.03,D[ring])
oh=np.zeros_like(W); oh[np.nonzero(ring)[0],PB[ring]]=1
allowed_ring=allow[np.nonzero(ring)[0],PB[ring]]
fr=fall.copy(); fr[np.nonzero(ring)[0][~allowed_ring]]=0
W=W*(1-fr[:,None])+oh*fr[:,None]
W[W<0.02]=0
order=np.argsort(-W,1); keep=np.zeros_like(W,bool); np.put_along_axis(keep,order[:,:4],True,1); W*=keep
W/=W.sum(1,keepdims=True)
# rim twin strips (flipped copies along the cuts) copy the weights of the surface they double, vertex by vertex
TS=np.array(TWIN_SRC); W[TS[:,0]]=W[TS[:,1]]
rep['rigid_pieces']=rigid; rep['rigid_by_bone']=rigid_by_bone; rep['root_ring_verts']=int((fr>0).sum())
rep['pieces_spanning_joints_kept_smooth']=kept_smooth; rep['spanning_largest']=sorted(big,reverse=True)[:8]
rigid_mask=np.zeros(N,bool)
for c in np.unique(lab[lab>=0]):
    pass
rigid_mask=(W.max(1)>0.999)&orn
rep['rigid_piece_verts']=int(rigid_mask.sum())
# ---------------- write ----------------
body.vertex_groups.clear()
for n in names:
    gq=body.vertex_groups.new(name=n); col=W[:,bi[n]]
    for i in np.nonzero(col>0)[0]: gq.add([int(i)],float(col[i]),'REPLACE')
fg=body.vertex_groups.new(name='DESIGN_paint_fill'); fg.add([int(i) for i in np.nonzero(fill_verts)[0]],1.0,'REPLACE')
mod=[m for m in body.modifiers if m.type=='ARMATURE'][0]; mod.object=arm
far=0
for n in names:
    a,b=SEG[n[len(P_):]]; d,_,_=segdist(P,a,b); far+=int(((W[:,bi[n]]>0.05)&(d>0.30)).sum())
legtorso=int((((part==3)|(part==4))[:,None]&(W>0)&np.isin(np.arange(len(names)),cols('Spine','Spine1','Spine2','Neck','Head','LeftShoulder','RightShoulder','LeftArm','RightArm','LeftForeArm','RightForeArm','LeftHand','RightHand'))[None,:]).any(1).sum())
armtorso=int((t[:,None]&(W>0.02)&np.isin(np.arange(len(names)),cols('LeftForeArm','RightForeArm','LeftHand','RightHand'))[None,:]).any(1).sum())
rep.update({'bones':len(names),'verts':N,'faces':len(me.polygons),'max_influences':int((W>0).sum(1).max()),'unweighted':int((W.sum(1)<0.999).sum()),
 'influence_hist':np.bincount((W>0).sum(1),minlength=5).tolist(),'verts_weight_gt0.05_farther_than_30cm_from_bone':far,
 'leg_verts_with_torso_or_arm_weight':legtorso,'torso_verts_with_forearm_or_hand_weight':armtorso,'paint_fill_verts':int(fill_verts.sum())})
np.save(OUTBLEND.replace('.blend','_part.npy'),part); np.save(OUTBLEND.replace('.blend','_rigid.npy'),rigid_mask)
bpy.ops.wm.save_as_mainfile(filepath=OUTBLEND,compress=True)
json.dump(rep,open(OUTJSON,'w'),indent=1); print('RIG',json.dumps(rep))
