"""Harmonic (heat-style) skin weights on Derek's 16 bones over the re-welded mesh.
Seeds = vertices that clearly belong to one bone; joint bands, pelvis band, shoulder tops are free and solved
as a smooth (Laplace) blend between their neighbouring seeds -> smooth falloff, bends at his joints."""
import numpy as np, json, scipy.sparse as sp, scipy.sparse.linalg as sla
from scipy.sparse.csgraph import connected_components
from scipy.spatial import cKDTree
d=np.load('mesh.npz'); P=d['co']; E=d['E']; twin=d['twin']; tmap=d['tmap']; hair=d['hair']; armside=d['armside']&~d['twin']
B={k:(np.array(v[0]),np.array(v[1])) for k,v in json.load(open('bones.json')).items()}
NAMES=list(B.keys()); bi={n:i for i,n in enumerate(NAMES)}; N=len(P); NB=len(NAMES)
def proj(X,a,b): ab=b-a; L=np.linalg.norm(ab); return ((X-a)@ab)/(L*L),L
def segdist(X,a,b):
    t,L=proj(X,a,b); q=a+np.clip(t,0,1)[:,None]*(b-a); return np.linalg.norm(X-q,axis=1),t,L
ni=np.concatenate([E[:,0],E[:,1]]); nj=np.concatenate([E[:,1],E[:,0]])
def flood(seed,allowed):
    lab=seed&allowed
    for _ in range(800):
        m=lab[ni]&allowed[nj]&~lab[nj]
        if not m.any(): break
        lab[nj[m]]=True
    return lab
orig=~twin
dsp=np.min(np.stack([segdist(P,*B[k])[0] for k in ('Spine1','Spine2','Neck')],1),1)
part=np.zeros(N,np.int8); armt={}
for s,pid in (('Left',1),('Right',2)):
    dd=np.stack([segdist(P,*B[s+k])[0] for k in ('Arm','ForeArm','Hand')],1); dm=dd.min(1)
    allowed=orig&(dd<np.array([0.065,0.06,0.065])).any(1)&((dm<dsp-0.02)|(dm<0.035))&(~hair|(dm<0.035))
    seed=(dd<np.array([0.040,0.035,0.035])).any(1)&allowed
    lab=flood(seed,allowed); part[lab&(part==0)]=pid
for s,pid in (('Left',3),('Right',4)):
    dd=np.stack([segdist(P,*B[s+k])[0] for k in ('UpLeg','Leg','Foot')],1)
    allowed=orig&(part==0)&(((dd<np.array([0.12,0.09,0.11])).any(1)&(P[:,2]<0.90))|(P[:,2]<0.78))
    lab=flood((P[:,2]<0.68)&allowed,allowed); part[lab&(part==0)]=pid
dl=np.stack([segdist(P,*B['Left'+k])[0] for k in ('UpLeg','Leg','Foot')],1).min(1)
dr=np.stack([segdist(P,*B['Right'+k])[0] for k in ('UpLeg','Leg','Foot')],1).min(1)
lg=(part>=3); part[lg&(dl<dr)]=3; part[lg&(dr<=dl)]=4
# freed right hand (cut_fill.py): the cut-off forearm/hand piece is right arm; nearby body pieces are body
_h0,_h1=B['RightHand']; _h1x=_h1+(_h1-_h0)/np.linalg.norm(_h1-_h0)*0.06
_dh,_=segdist(P,_h0,_h1x)[:2]; _df,_tf,_=segdist(P,*B['RightForeArm']); DAXR=np.minimum(_dh,_df); TFR=_tf
part[armside]=2
bodyR=(~armside)&(~twin)&(DAXR<0.16)&(TFR>0.35)
part[bodyR&(part==2)]=0
seed=np.full(N,-1)
def S(mask,b): seed[mask&(seed<0)&orig]=bi[b]
# legs: thigh / shin / foot, free band +-7cm around his knees and +-4cm at his ankles; pelvis above the leg part is free
for s,pid in (('Left',3),('Right',4)):
    m=part==pid; kz=B[s+'Leg'][0][2]; az=B[s+'Foot'][0][2]
    S(m&(P[:,2]>kz+0.07)&(P[:,2]<0.83),s+'UpLeg'); S(m&(P[:,2]<kz-0.07)&(P[:,2]>az+0.05),s+'Leg'); S(m&(P[:,2]<az-0.03),s+'Foot')
# arms: free band at shoulder top (first 35% of his upper arm), elbow +-5cm, wrist +-3cm
for s,pid in (('Left',1),('Right',2)):
    m=part==pid
    t1,L1=proj(P,*B[s+'Arm']); t2,L2=proj(P,*B[s+'ForeArm']); t3,L3=proj(P,*B[s+'Hand'])
    S(m&(t1>0.35)&(t1*L1<L1-0.05),s+'Arm')
    S(m&(t2*L2>0.05)&(t2*L2<L2-0.03),s+'ForeArm')
    S(m&(t3*L3>0.03),s+'Hand')
# fused limb/torso contacts (the re-welded seams): torso vertices within RT of an arm vertex and arm vertices within RA of
# a torso vertex stay unseeded; the Laplace solve then spreads the blend only along the real (welded) surface
RT,RA=0.20,0.05
from scipy.sparse.csgraph import dijkstra
armv=(part==1)|(part==2); torv=(part==0)
Lg=np.linalg.norm(P[E[:,0]]-P[E[:,1]],axis=1)
Gm=sp.coo_matrix((np.concatenate([Lg,Lg]),(ni,nj)),shape=(N,N)).tocsr()
SP=np.load('seam_pos.npy'); dd_,jj_=cKDTree(P).query(SP); bnd=np.unique(jj_[dd_<1e-6])   # the re-welded seam vertices
# plus the shoulder: where the upper arm surface meets the chest/shoulder top (above the elbows)
xe=((armv[E[:,0]]&torv[E[:,1]])|(torv[E[:,0]]&armv[E[:,1]]))&(P[E[:,0],2]>1.33)&~hair[E[:,0]]&~hair[E[:,1]]
sbnd=np.unique(E[xe].ravel())
dgeo=dijkstra(Gm,directed=False,indices=bnd,limit=RT,min_only=True)              # welded seams: wide blend
dsh=dijkstra(Gm,directed=False,indices=sbnd,limit=0.08,min_only=True)            # shoulder: chest <-> upper arm
contact=(torv&(dgeo<RT))|(armv&(dgeo<RA))|((part>=3)&(dgeo<RA))|(torv&(dsh<0.07))|(armv&(dsh<0.05))
seed[contact&(part>=1)]=-1
# hands resting on the hip/skirt: a wider free ring on the hand side so the hand->hip web blends over more edges
for s_ in ('Left','Right'): seed[(seed==bi[s_+'Hand'])&(dgeo<0.11)]=-1
# torso: Spine1 (root) band, chest core, neck, head; waist/pelvis band 0.86..1.15 and the shoulder tops stay free
ax=lambda z,c: np.interp(z,[B[k][0][2] for k in ('Spine1','Spine2','Neck','Head')],[B[k][0][c] for k in ('Spine1','Spine2','Neck','Head')])
lat=np.hypot(P[:,0]-ax(P[:,2],0),P[:,1]-ax(P[:,2],1))
t0=(part==0)&~hair&~contact
S(t0&(P[:,2]>1.15)&(P[:,2]<1.25),'Spine1')
_karm=cKDTree(P[((part==1)|(part==2))&~twin]); _d3a=_karm.query(P)[0]
S(t0&(P[:,2]>1.32)&(P[:,2]<1.50)&(lat<0.17)&(_d3a>0.06),'Spine2')
S((part==0)&(P[:,2]>1.52)&(P[:,2]<1.58)&(lat<0.07),'Neck')
S((part==0)&(P[:,2]>1.65),'Head')
# hair: scalp/neck part fixed, long strands free (smoothly follow whatever they are attached to)
S(hair&(P[:,2]>1.66),'Head')
# ---- harmonic solve per connected component that has seeds ----
Le=np.linalg.norm(P[E[:,0]]-P[E[:,1]],axis=1); w=np.ones_like(Le)   # uniform: long bridging triangles as stiff as small ones
A=sp.coo_matrix((np.concatenate([w,w]),(ni,nj)),shape=(N,N)).tocsr()
L=sp.diags(np.asarray(A.sum(1)).ravel())-A
ncomp,comp=connected_components(A,directed=False)
hasseed=np.zeros(ncomp,bool); hasseed[np.unique(comp[(seed>=0)])]=True
solve=orig&hasseed[comp]
fixed=solve&(seed>=0); free=solve&(seed<0)
X=np.zeros((N,NB)); X[np.nonzero(fixed)[0],seed[fixed]]=1
fi=np.nonzero(free)[0]; si=np.nonzero(fixed)[0]
Lff=L[fi][:,fi].tocsc(); Lfs=L[fi][:,si]
lu=sla.splu(Lff); X[fi]=lu.solve(-(Lfs@X[si]))
# components with no seed at all (loose rigid bits): copy the weights of the nearest solved vertex
loose=orig&~solve
if loose.any():
    kt=cKDTree(P[solve]); _,j=kt.query(P[loose]); src=np.nonzero(solve)[0][j]
    # rigid: one weight set per component = the nearest solved vertex to the component centroid
    for c in np.unique(comp[loose]):
        m=loose&(comp==c); cen=P[m].mean(0); _,jj=kt.query(cen); X[m]=X[np.nonzero(solve)[0][jj]]
# skirt flaps below the waist that hang in front of / behind a thigh follow it partly (legs don't poke through)
for s,pid in (('Left',3),('Right',4)):
    dlg,_,_=segdist(P,*B[s+'UpLeg'])
    m=orig&(part==0)&(P[:,2]<1.0)&(P[:,2]>0.55)
    f=np.clip((0.20-dlg)/0.10,0,1)*np.clip((1.0-P[:,2])/0.10,0,1)*0.45
    other='Right' if s=='Left' else 'Left'
    f=f*(1-np.clip(X[:,bi[other+'UpLeg']]*2,0,1))
    g=f[m]; X[m]*=(1-g)[:,None]; X[m,bi[s+'UpLeg']]+=g
# a little Laplacian relax only on the free + skirt verts to remove kinks from the skirt bias
movable=orig&(seed<0)
Wn=sp.diags(1/np.maximum(np.asarray(A.sum(1)).ravel(),1e-9))@A
for _ in range(10): X[movable]=0.5*X[movable]+0.5*(Wn@X)[movable]
X=np.clip(X,0,None)
# locality: limb bones fade out with 3D distance from their own chain (no far-away arm/leg weights on the back or chest)
for s_ in ('Left','Right'):
    da=np.min(np.stack([segdist(P,*B[s_+k])[0] for k in ('Arm','ForeArm','Hand')],1),1)
    fa=np.clip((0.20-da)/0.08,0,1); fa=fa*fa*(3-2*fa)
    for k in ('Arm','ForeArm','Hand'): X[:,bi[s_+k]]*=fa
    dlg=np.min(np.stack([segdist(P,*B[s_+k])[0] for k in ('UpLeg','Leg','Foot')],1),1)
    fl=np.clip((0.26-dlg)/0.10,0,1); fl=fl*fl*(3-2*fl)
    for k in ('UpLeg','Leg','Foot'): X[:,bi[s_+k]]*=fl
# a vertex never mixes left- and right-arm weights: fade each side out across the midline (by which arm chain is nearer)
dA={s_:np.min(np.stack([segdist(P,*B[s_+k])[0] for k in ('Arm','ForeArm','Hand')],1),1) for s_ in ('Left','Right')}
sd=dA['Right']-dA['Left']            # >0: nearer the left arm
fL=np.clip(sd/0.06,0,1); fL=fL*fL*(3-2*fL); fR=np.clip(-sd/0.06,0,1); fR=fR*fR*(3-2*fR)   # both 0 on the midline
for k in ('Arm','ForeArm','Hand'): X[:,bi['Left'+k]]*=fL; X[:,bi['Right'+k]]*=fR
# head/neck only drive the head, neck and the scalp end of the hair (not shoulder tops or hair lying on the chest/back)
fh=np.where(hair,np.clip((P[:,2]-1.30)/0.28,0,1),np.clip((P[:,2]-1.48)/0.08,0,1))   # hair: long, gentle fade; fh=fh*fh*(3-2*fh); X[:,bi['Head']]*=fh
fn=np.clip((P[:,2]-1.40)/0.08,0,1); fn=fn*fn*(3-2*fn); X[:,bi['Neck']]*=fn
# off the hair, head/neck stay off the shoulder tops: fade by distance from the neck/head axis
hx=np.clip((0.15-lat)/0.06,0,1); hx=hx*hx*(3-2*hx); hs=hair.astype(float)
for _ in range(12): hs=0.5*hs+0.5*(Wn@hs)       # soft hair mask (no hard switch between neighbours)
hs=np.maximum(hs,np.clip((P[:,2]-1.58)/0.04,0,1)*(part==0)); hx=hs+(1-hs)*hx
X[:,bi['Head']]*=hx; X[:,bi['Neck']]*=hx
# hand piece: hand/forearm only (upper arm only near the elbow); body pieces near the hand: no right-arm weights
keepH=[bi['RightArm'],bi['RightForeArm'],bi['RightHand']]
mH=np.zeros(NB,bool); mH[keepH]=True
X[np.ix_(armside,~mH)]*=0                       # freed piece: right arm bones only
X[armside&(TFR>=0.35),bi['RightArm']]=0         # below the elbow band: forearm/hand only
z=armside&(X.sum(1)<1e-6); X[z,bi['RightHand']]=1.0
for k in ('RightArm','RightForeArm','RightHand'): X[bodyR,bi[k]]=0
X[orig&(X.sum(1)<1e-6),bi['Spine2']]=1.0
X[orig]/=X[orig].sum(1)[:,None]
# <=4 influences without popping: truncate, relax the free vertices a little, repeat; final truncate
def trunc(X):
    o=np.argsort(-X,1); keep=np.zeros_like(X,bool); np.put_along_axis(keep,o[:,:4],True,1)
    X=np.where(keep&(X>=0.02),X,0); X[orig]/=X[orig].sum(1)[:,None]; return X
for _ in range(3):
    X=trunc(X)
    for _ in range(4): X[movable]=0.5*X[movable]+0.5*(Wn@X)[movable]
X=trunc(X)
# re-assert the cut after smoothing: piece = right arm bones only (forearm/hand below the elbow band); body near it = no right arm
X[np.ix_(armside,~mH)]=0; X[armside&(TFR>=0.35),bi['RightArm']]=0
for k in ('RightArm','RightForeArm','RightHand'): X[bodyR,bi[k]]=0
z=armside&(X.sum(1)<1e-6); X[z,bi['RightHand']]=1.0
z=bodyR&(X.sum(1)<1e-6); X[z,bi['Spine1']]=1.0
X[orig]/=X[orig].sum(1)[:,None]
X[twin]=X[tmap[twin]]
rep={'hand_piece_verts':int(armside.sum()),'body_near_hand_verts':int(bodyR.sum()),'contact_unseeded':int(contact.sum()),'parts':{str(k):int((part==k).sum()) for k in range(5)},'seeded':int((seed>=0).sum()),'free':int(free.sum()),
 'components':int(ncomp),'loose_components':int(len(np.unique(comp[loose]))) if loose.any() else 0,
 'max_influences':int((X>0).sum(1).max()),'unweighted':int((X.sum(1)<0.99).sum()),
 'dominant':{n:int((X.argmax(1)==i).sum()) for i,n in enumerate(NAMES)}}
np.savez('weights.npz',X=X,names=np.array(NAMES),part=part,seed=seed)
json.dump(rep,open('weights.json','w'),indent=1); print(json.dumps(rep))
