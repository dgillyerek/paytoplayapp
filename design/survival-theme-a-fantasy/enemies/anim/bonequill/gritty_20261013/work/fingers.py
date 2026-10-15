"""Left-hand finger bones (Derek 10-14): one combined chain for the four fingers (3 segments) and a thumb chain (2 segments),
parented to LeftHand, on the ORIGINAL Meshy hand. Finger verts weighted smoothly along each chain (palm stays on LeftHand),
then a closed bow grip is searched (rigid bone rotations, so the fingers never stretch): fingers curl around a grip cylinder
parallel to the knuckle line, thumb wraps over. Writes rig.blend (in place) and fingers.json (pose + grip centre in hand space)."""
import bpy, json, math, itertools, numpy as np
from mathutils import Vector, Matrix
W_='/workspace/bonequill_gritty/work'; P_='mixamorig:'
bpy.ops.wm.open_mainfile(filepath=W_+'/rig_nofingers.blend')
arm=bpy.data.objects['BONEQUILL_rig']; body=bpy.data.objects['BONEQUILL_body']; ad=arm.data
B=ad.bones[P_+'LeftHand']; Mh=np.array(B.matrix_local); Mi=np.linalg.inv(Mh)
gi=body.vertex_groups[P_+'LeftHand'].index
V=np.array([v.co[:] for v in body.data.vertices]); N=len(V)
WH=np.zeros(N)
for v in body.data.vertices:
    for g in v.groups:
        if g.group==gi: WH[v.index]=g.weight
L=(np.c_[V,np.ones(N)]@Mi.T)[:,:3]; hand=WH>0.01
def ss(e0,e1,x): t=np.clip((x-e0)/(e1-e0),0,1); return t*t*(3-2*t)
YK=0.085
thumb=hand&(L[:,0]>0.035)&(L[:,1]>0.015)&(L[:,1]<0.135)
fing=hand&~thumb&(L[:,1]>YK-0.02)
zc=float(np.median(L[fing&(L[:,1]>0.12),2]))
# finger centreline (x,y) in the hand plane: centroid per y band
ytip=float(np.percentile(L[fing,1],99.5)); ys=np.linspace(YK,ytip,12)
cl=np.array([[np.median(L[fing&(np.abs(L[:,1]-yy)<0.008),0]),yy] for yy in ys])
seg=np.linalg.norm(np.diff(cl,axis=0),axis=1); arc=np.r_[0,np.cumsum(seg)]; FL=arc[-1]
def at(s): i=np.clip(np.searchsorted(arc,s)-1,0,len(seg)-1); t=(s-arc[i])/seg[i]; return cl[i]+(cl[i+1]-cl[i])*t
JF=[at(FL*r) for r in (0.0,0.45,0.75,1.0)]                     # MCP, PIP, DIP, tip
# thumb chain (3D): base = thumb verts nearest the wrist, tip = farthest from base
tb=L[thumb]; base=tb[tb[:,1]<np.percentile(tb[:,1],12)].mean(0); far=tb[np.argmax(np.linalg.norm(tb-base,axis=1))]
tip=far; mid=base+(tip-base)*0.55; JT=[base,mid,tip]; TL=np.linalg.norm(tip-base)
# ---- weights (fraction of the vertex's LeftHand weight handed to each finger bone) ----
def proj(Q,a,b):
    ab=b-a; return np.clip(((Q-a)@ab)/(ab@ab),-1,2)
BW=0.012
# fingers: arc parameter from nearest centreline sample in (x,y)
d2=((L[:,None,:2]-cl[None])**2).sum(-1); k=np.argmin(d2,1); s=arc[k]+0*L[:,0]
sF=np.where(fing,s,-1.0)
rampF=ss(-BW*0.5,BW*0.5,L[:,1]-YK)*fing
wF1=rampF*(1-ss(JFs:=0,0,0) if False else rampF)
s1,s2=FL*0.45,FL*0.75
wF=np.zeros((N,3))
wF[:,0]=rampF*(1-ss(s1-BW,s1+BW,sF)); wF[:,1]=rampF*ss(s1-BW,s1+BW,sF)*(1-ss(s2-BW,s2+BW,sF)); wF[:,2]=rampF*ss(s2-BW,s2+BW,sF)
tt=proj(L,base,tip); rampT=ss(0.0,0.5,tt)*thumb
wT=np.zeros((N,2)); wT[:,0]=rampT*(1-ss(0.5,0.62,tt)); wT[:,1]=rampT*ss(0.5,0.62,tt)
frac=np.c_[wF,wT]
# smooth the weights over the mesh (hand only) so no edge straddles a hard mask boundary
E0=np.array([e.vertices[:] for e in body.data.edges]); E0=E0[hand[E0].all(1)]
dg=np.bincount(E0.ravel(),minlength=N).astype(float)
for it in range(25):
    acc=np.zeros_like(frac); np.add.at(acc,E0[:,0],frac[E0[:,1]]); np.add.at(acc,E0[:,1],frac[E0[:,0]])
    sm=acc/np.maximum(dg,1)[:,None]; frac=np.where((dg>0)[:,None],0.5*frac+0.5*sm,frac)
frac[~hand]=0; palm=1-frac.sum(1)
# ---- bones ----
def W3(p2,z=zc): return Vector((Mh@np.r_[p2[0],p2[1],z,1])[:3]) if len(p2)==2 else Vector((Mh@np.r_[p2,1])[:3])
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='EDIT'); eb=ad.edit_bones
zax=Vector(Mh[:3,2])
FN=[P_+'LeftHandFingers1',P_+'LeftHandFingers2',P_+'LeftHandFingers3']; TN=[P_+'LeftHandThumb1',P_+'LeftHandThumb2']
par=eb[P_+'LeftHand']
for i,n in enumerate(FN):
    b=eb.new(n); b.head=W3(JF[i]); b.tail=W3(JF[i+1]); b.align_roll(zax); b.parent=par; b.use_connect=i>0; par=b
par=eb[P_+'LeftHand']
for i,n in enumerate(TN):
    b=eb.new(n); b.head=W3(JT[i]); b.tail=W3(JT[i+1]); b.align_roll(zax); b.parent=par; b.use_connect=i>0; par=b
bpy.ops.object.mode_set(mode='OBJECT')
vgH=body.vertex_groups[P_+'LeftHand']
for j,n in enumerate(FN+TN):
    vg=body.vertex_groups.new(name=n); col=WH*frac[:,j]
    for i in np.nonzero(col>1e-4)[0]: vg.add([int(i)],float(col[i]),'REPLACE')
for i in np.nonzero(hand)[0]:
    w=WH[i]*palm[i]
    if w>1e-4: vgH.add([int(i)],float(w),'REPLACE')
    else: vgH.remove([int(i)])
# ---- grip search in hand space (numpy LBS of the hand verts, rotations about the knuckle axis z for the fingers) ----
def Rz(a): c,s_=math.cos(a),math.sin(a); return np.array([[c,-s_,0],[s_,c,0],[0,0,1]])
def Rax(axis,a):
    axis=np.asarray(axis,float); axis/=np.linalg.norm(axis); K=np.array([[0,-axis[2],axis[1]],[axis[2],0,-axis[0]],[-axis[1],axis[0],0]])
    return np.eye(3)+math.sin(a)*K+(1-math.cos(a))*K@K
P3F=[np.r_[JF[i],zc] for i in range(4)]
def chain_T(joints,rots):
    T=[]; Racc=np.eye(3); tacc=np.zeros(3)
    for j,R in zip(joints,rots):
        jw=Racc@j+tacc; Racc=R@Racc; tacc=jw-Racc@j; T.append((Racc.copy(),tacc.copy()))   # x -> Racc x + tacc
    return T
hidx=np.nonzero(hand)[0]; LH=L[hidx]; WHh=WH[hidx]; fr=frac[hidx]; pl=palm[hidx]
def pose(a,th):
    TF=chain_T(P3F[:3],[Rz(-math.radians(x)) for x in a]); TT=chain_T(JT[:2],th)
    out=LH*pl[:,None]
    for j,(R,t) in enumerate(TF+TT): out=out+fr[:,j:j+1]*(LH@R.T+t)
    return out
zb=np.abs(LH[:,2]-zc)<0.035
gx,gy=np.meshgrid(np.arange(0.02,0.09,0.002),np.arange(0.05,0.14,0.002)); G=np.c_[gx.ravel(),gy.ravel()]
def hole(Q):
    q=Q[zb][:,:2]; d=np.sqrt(((G[:,None,:]-q[None])**2).sum(-1)).min(1); i=int(np.argmax(d)); return d[i],G[i]
def coverage(Q,C,r):
    q=Q[zb][:,:2]-C; dd=np.linalg.norm(q,axis=1); a=np.degrees(np.arctan2(q[dd<r+0.022,1],q[dd<r+0.022,0]))%360
    return len(np.unique((a//10).astype(int)))*10
BOWR=0.024
# grip chosen by fsearch7.py (fine grid over the three finger curls and the thumb swing, cylinder centre free in the hand plane):
# best angular closure of the hand around a bow-grip-sized cylinder with zero penetration -> palm behind the grip, fingers curled
# under and around it, thumb over the top
a=(15,35,20); tax=(0,0,-1); t1,t2=35,0; C=np.array([0.042,0.094])
Q=pose(a,[Rax(tax,math.radians(t1)),Rax(tax,math.radians(t2))])
za=np.abs(Q[:,2]-zc)<0.11
# re-centre the grip within +-4 mm to the point of maximum clearance (the grip sleeve is faceted, up to ~2.6 cm radius)
best_=None
for ox in np.arange(-0.004,0.0041,0.0005):
    for oy in np.arange(-0.004,0.0041,0.0005):
        c_=C+np.array([ox,oy]); m_=float(np.linalg.norm(Q[za][:,:2]-c_,axis=1).min())
        if best_ is None or m_>best_[0]: best_=(m_,c_)
C=best_[1]; dd=np.linalg.norm(Q[za][:,:2]-C,axis=1); r=float(dd.min())
v=Q[za][dd<r+0.015][:,:2]-C; cov=len(np.unique(((np.degrees(np.arctan2(v[:,1],v[:,0]))%360)//10).astype(int)))*10
# finger length check (centreline through the same weights) and edge stretch on the finger/thumb verts
PL=np.array([np.r_[at(FL*u),zc] for u in np.linspace(0,1,40)])
TF=chain_T(P3F[:3],[Rz(-math.radians(x)) for x in a]); s_=np.array([u*FL for u in np.linspace(0,1,40)])
wc=np.c_[1-ss(s1-BW,s1+BW,s_),ss(s1-BW,s1+BW,s_)*(1-ss(s2-BW,s2+BW,s_)),ss(s2-BW,s2+BW,s_)]
Pc=sum(wc[:,j:j+1]*(PL@R.T+t) for j,(R,t) in enumerate(TF)); FLp=np.linalg.norm(np.diff(Pc,axis=0),axis=1).sum()
E=np.array([e.vertices[:] for e in body.data.edges]); m=np.isin(E,hidx).all(1); E=E[m]; pos=np.full(N,-1); pos[hidx]=np.arange(len(hidx))
el0=np.linalg.norm(LH[pos[E[:,0]]]-LH[pos[E[:,1]]],axis=1); el1=np.linalg.norm(Q[pos[E[:,0]]]-Q[pos[E[:,1]]],axis=1)
movedE=(fr[pos[E[:,0]]].sum(1)+fr[pos[E[:,1]]].sum(1))>0.02; ratio=el1[movedE]/np.maximum(el0[movedE],1e-6)
rep={'finger_curl_deg':list(a),'thumb_axis':list(tax),'thumb_deg':[t1,t2],'grip_local':[float(C[0]),float(C[1]),zc],'hole_radius':float(r),
     'grip_coverage_deg':int(cov),'finger_len_rest_m':float(FL),'finger_len_posed_m':float(FLp),'finger_len_change_pct':float((FLp/FL-1)*100),
     'edge_stretch_p99':float(np.percentile(ratio,99)),'edge_stretch_max':float(ratio.max()),'edge_compress_p1':float(np.percentile(ratio,1)),
     'fingers_verts':int(fing.sum()),'thumb_verts':int(thumb.sum()),'zc':zc,'joints_fingers':[list(map(float,j)) for j in JF],'joints_thumb':[list(map(float,j)) for j in JT]}
near=np.linalg.norm(Q[:,:2]-C,axis=1)<0.06; rep['fist_top_local_z']=float(Q[near,2].max())
json.dump(rep,open(W_+'/fingers.json','w'),indent=1); json.dump({'grip_local':rep['grip_local'],'fist_top_local_z':rep['fist_top_local_z'],'source':'fingers.json (posed finger bones, no sculpt)'},open(W_+'/fist.json','w'),indent=1); print('FINGREP',json.dumps({k:v for k,v in rep.items() if not k.startswith('joints')}))
bpy.ops.wm.save_as_mainfile(filepath=W_+'/rig.blend',compress=True)
