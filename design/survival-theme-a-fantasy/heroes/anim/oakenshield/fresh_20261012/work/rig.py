"""Oakenshield fresh rig (2026-10-12). dec.blend (Meshy 200k) -> rig.blend: scale to 1.80 m, feet on 0, 20 mixamorig bones,
part-split weights, face/skull 100% Head (narrow tilted Neck->Head band), every protruding piece rigid on one bone,
skirt on Hips, thighs with a smooth Hips->UpLeg ramp (no crease)."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, bmesh, numpy as np, json
from mathutils import Vector
from scipy import ndimage
import scipy.sparse as sp, scipy.sparse.csgraph as cg
bpy.ops.wm.open_mainfile(filepath='/workspace/oakenshield_fresh/work/dec.blend')
body=bpy.data.objects['OAKENSHIELD_body']; me=body.data
co=np.empty(len(me.vertices)*3); me.vertices.foreach_get('co',co); P=co.reshape(-1,3)
S=0.9477246; P[:,2]-=P[:,2].min(); P*=S; P[:,0]-=(P[:,0].max()+P[:,0].min())/2
me.vertices.foreach_set('co',P.ravel()); me.update()
N=len(P); E=np.array([e.vertices[:] for e in me.edges]); ea,eb=E[:,0],E[:,1]
rep={'faces':len(me.polygons),'verts':N}
# ---------- skeleton ----------
J={'Hips':(0,0,0.95),'Spine':(0,0,1.03),'Spine1':(0,0,1.13),'Spine2':(0,0,1.24),'Neck':(0,0.005,1.37),'Head':(0,0.0,1.455),'HeadTop':(0,0,1.70)}
for s,x in (('Left',1),('Right',-1)):
    J[s+'Shoulder']=(0.04*x,-0.01,1.37); J[s+'Arm']=(0.165*x,-0.01,1.30); J[s+'ForeArm']=(0.345*x,-0.015,1.095)
    J[s+'Hand']=(0.48*x,-0.02,0.945); J[s+'HandEnd']=(0.565*x,-0.02,0.835)
    J[s+'UpLeg']=(0.09*x,0,0.91); J[s+'Leg']=(0.12*x,-0.005,0.48); J[s+'Foot']=(0.13*x,0.015,0.085); J[s+'FootEnd']=(0.135*x,-0.15,0.01)
J={k:np.array(v,float) for k,v in J.items()}
BONES=[('Hips',None,'Spine'),('Spine','Hips','Spine1'),('Spine1','Spine','Spine2'),('Spine2','Spine1','Neck'),('Neck','Spine2','Head'),('Head','Neck','HeadTop')]
for s in ('Left','Right'):
    BONES+=[(s+'Shoulder','Spine2',s+'Arm'),(s+'Arm',s+'Shoulder',s+'ForeArm'),(s+'ForeArm',s+'Arm',s+'Hand'),(s+'Hand',s+'ForeArm',s+'HandEnd')]
for s in ('Left','Right'):
    BONES+=[(s+'UpLeg','Hips',s+'Leg'),(s+'Leg',s+'UpLeg',s+'Foot'),(s+'Foot',s+'Leg',s+'FootEnd')]
P_='mixamorig:'
ad=bpy.data.armatures.new('OAKENSHIELD_rig'); arm=bpy.data.objects.new('OAKENSHIELD_rig',ad); bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='EDIT')
for n,par,t in BONES:
    b=ad.edit_bones.new(P_+n); b.head=Vector(J[n]); b.tail=Vector(J[t])
    b.align_roll(Vector((0,0,1)) if 'Foot' in n else Vector((0,-1,0)))
    if par: b.parent=ad.edit_bones[P_+par]; b.use_connect=bool(np.allclose(J[n],J[[x for x in BONES if x[0]==par][0][2]]))
bpy.ops.object.mode_set(mode='OBJECT')
names=[P_+n for n,_,_ in BONES]; bi={n:i for i,n in enumerate(names)}; NB=len(names)
SEG={n:(J[n],J[t]) for n,_,t in BONES}
def segdist(Q,a,b):
    ab=b-a; L=np.linalg.norm(ab); t=((Q-a)@ab)/(L*L); q=a+np.clip(t,0,1)[:,None]*ab; return np.linalg.norm(Q-q,axis=1),t,L
def ss(e0,e1,x): t=np.clip((x-e0)/(e1-e0),0,1); return t*t*(3-2*t)
x,y,z=P[:,0],P[:,1],P[:,2]; W=np.zeros((N,NB))
def add(n,w): W[:,bi[P_+n]]+=w
# ---------- parts: 0 torso/head, 1 L arm, 2 R arm, 3 L leg, 4 R leg ----------
part=np.zeros(N,np.int8)
hw=np.interp(z,[1.0,1.06,1.10,1.14,1.18,1.21,1.30],[0.20,0.135,0.10,0.115,0.13,0.145,0.17])
for s,pid,sg in (('Left',1,1),('Right',2,-1)):
    d=np.min(np.stack([segdist(P,*SEG[s+k])[0] for k in ('Arm','ForeArm','Hand')],1),1)
    m=(d<0.11)&(x*sg>hw+0.012)&(z<1.33)&(z>0.75)
    part[m]=pid
for s,pid,sg in (('Left',3,1),('Right',4,-1)):
    d,_,_=segdist(P,*SEG[s+'UpLeg']); dl,_,_=segdist(P,*SEG[s+'Leg'])
    r=np.interp(z,[0.45,0.66,0.80,0.95],[0.075,0.08,0.09,0.10])
    m=(x*sg>0)&(part==0)&(((z<0.45))|((z<0.93)&(np.minimum(d,dl)<r)))
    part[m]=pid
# leg pieces must connect to the foot inside the leg set (drop skirt bits that landed in the radius but are not leg)
for pid in (3,4):
    msk=part==pid; em=msk[ea]&msk[eb]
    n,lab=cg.connected_components(sp.coo_matrix((np.ones(int(em.sum())),(ea[em],eb[em])),shape=(N,N)),directed=False)
    footlab=np.bincount(lab[msk&(z<0.1)]).argmax(); part[msk&(lab!=footlab)]=0
for pid in (1,2):
    msk=part==pid; em=msk[ea]&msk[eb]
    n,lab=cg.connected_components(sp.coo_matrix((np.ones(int(em.sum())),(ea[em],eb[em])),shape=(N,N)),directed=False)
    hl=np.bincount(lab[msk&(np.abs(x)>0.45)]).argmax(); part[msk&(lab!=hl)]=0
rep['parts']={k:int((part==v).sum()) for k,v in (('torso_head',0),('L_arm',1),('R_arm',2),('L_leg',3),('R_leg',4))}
def chain(u,knots,blends):
    w=np.zeros((len(u),len(knots)-1))
    for i in range(len(knots)-1):
        lo=1.0 if i==0 else ss(knots[i]-blends[i-1],knots[i]+blends[i-1],u)
        hi=1.0 if i==len(knots)-2 else 1-ss(knots[i+1]-blends[i],knots[i+1]+blends[i],u)
        w[:,i]=lo*hi
    return w/np.maximum(w.sum(1,keepdims=True),1e-9)
# torso: z chain; head by a tilted plane (chin front 1.44, skull base back 1.49), narrow band +-1.2 cm
t=part==0
zc=1.44+np.clip(y+0.11,0,0.22)*0.23
hd=ss(zc-0.012,zc+0.012,z)
cw=chain(z,[-9,1.03,1.13,1.24,1.37,9],[0.05,0.05,0.05,0.04])
tw=cw*(1-hd)[:,None]
for i,n in enumerate(('Hips','Spine','Spine1','Spine2','Neck')): add(n,tw[:,i]*t)
add('Head',hd*t)
# shoulders: torso verts near the shoulder joint and above the armpit share the Shoulder bone
for s,sg in (('Left',1),('Right',-1)):
    d,_,_=segdist(P,*SEG[s+'Shoulder']); w=t*(x*sg>0.05)*ss(0.14,0.06,d)*(1-hd)*0.7
    W[:]*=(1-w)[:,None]; add(s+'Shoulder',w)
# arms
for s,pid in (('Left',1),('Right',2)):
    m=part==pid; A,F,H,He=J[s+'Arm'],J[s+'ForeArm'],J[s+'Hand'],J[s+'HandEnd']
    ds=[segdist(P,a,b) for a,b in ((A,F),(F,H),(H,He))]; L1,L2=ds[0][2],ds[1][2]
    k=np.argmin(np.stack([d[0] for d in ds],1),1)
    u=np.where(k==0,ds[0][1]*L1,np.where(k==1,L1+np.clip(ds[1][1],0,1.3)*L2,L1+L2+np.clip(ds[2][1],0,2)*ds[2][2]))
    cw=chain(u,[-9,L1,L1+L2,9],[0.045,0.03])
    root=ss(-0.02,0.07,u)   # near the shoulder ease from Shoulder/Spine2 into Arm
    add(s+'Shoulder',cw[:,0]*(1-root)*0.7*m); add('Spine2',cw[:,0]*(1-root)*0.3*m)
    add(s+'Arm',cw[:,0]*root*m); add(s+'ForeArm',cw[:,1]*m); add(s+'Hand',cw[:,2]*m)
# legs: thigh ramps from Hips to UpLeg over 12 cm along the leg (no crease)
for s,pid in (('Left',3),('Right',4)):
    m=part==pid; Hh,K,A=J[s+'UpLeg'],J[s+'Leg'],J[s+'Foot']
    L1=np.linalg.norm(K-Hh); L2=np.linalg.norm(A-K)
    u=(Hh[2]-z)  # vertical distance down the leg (legs near vertical)
    cw=chain(u,[-9,L1*0.995,L1+L2*0.99,9],[0.05,0.03])
    root=ss(-0.02,0.12,u)
    add('Hips',cw[:,0]*(1-root)*m); add(s+'UpLeg',cw[:,0]*root*m); add(s+'Leg',cw[:,1]*m); add(s+'Foot',cw[:,2]*m)
s_=W.sum(1); W[s_<1e-6,bi[P_+'Hips']]=1; W/=W.sum(1,keepdims=True)
# smooth within parts; across part borders only at the shoulder and hip joint zones
same=part[ea]==part[eb]
jz=((np.minimum(part[ea],part[eb])==0)&(np.maximum(part[ea],part[eb])<=2)&(z[ea]>1.18))|((np.minimum(part[ea],part[eb])==0)&(z[ea]>0.80))
ok=same|jz; ei=np.concatenate([ea[ok],eb[ok]]); ej=np.concatenate([eb[ok],ea[ok]]); deg=np.bincount(ei,minlength=N).astype(float)
headlock=(t&(hd>0.999))
for it in range(8):
    acc=np.zeros_like(W); np.add.at(acc,ei,W[ej]); W=0.5*W+0.5*np.where(deg[:,None]>0,acc/np.maximum(deg,1)[:,None],W)
    W[headlock]=0; W[headlock,bi[P_+'Head']]=1
# ---------- rigid protruding pieces (voxel core) ----------
F=np.array([p.vertices[:] for p in me.polygons])
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
zz,yy,xx=np.mgrid[-4:5,-4:5,-4:5]; ball=(xx*xx+yy*yy+zz*zz)<=16
core=ndimage.binary_opening(solid,structure=ball)
dcore=ndimage.distance_transform_edt(~core)*g
ij=np.clip(np.floor((P-lo)/g).astype(int),0,np.array(shp)-1); dv=dcore[ij[:,0],ij[:,1],ij[:,2]]
orn=dv>0.015
# skirt: everything in the torso part below the belt (z<0.95) is Hips; it is not split into pieces
skirt=(part==0)&(z<0.95)
W[skirt]=0; W[skirt,bi[P_+'Hips']]=1
em=orn[ea]&orn[eb]&~skirt[ea]&~skirt[eb]
nc,lab=cg.connected_components(sp.coo_matrix((np.ones(int(em.sum())),(ea[em],eb[em])),shape=(N,N)),directed=False)
lab=np.where(orn&~skirt,lab,-1)
core_v=~orn; W0=W.copy()
rootw=np.zeros_like(W)
m1=orn[ea]&core_v[eb]; np.add.at(rootw,ea[m1],W0[eb[m1]])
m2=orn[eb]&core_v[ea]; np.add.at(rootw,eb[m2],W0[ea[m2]])
pb=-np.ones(N,int); rigid_by={}; nrig=0
for c in np.unique(lab[lab>=0]):
    idx=np.nonzero(lab==c)[0]
    if len(idx)<3: continue
    tot=rootw[idx].sum(0)
    if tot.sum()<1e-9: tot=W0[idx].sum(0)
    if (part[idx]==0).all() and z[idx].mean()>1.45: tot=tot*0; tot[bi[P_+'Head']]=1
    b=int(tot.argmax()); W[idx]=0; W[idx,b]=1; pb[idx]=b; nrig+=1; rigid_by[names[b]]=rigid_by.get(names[b],0)+1
# 2.5 cm root ring eases into the piece bone so attachments do not open
src=pb>=0; D=np.where(src,0.0,np.inf); PB=pb.copy(); Le=np.linalg.norm(P[ea]-P[eb],axis=1)
for it in range(30):
    for a_,b_ in ((ea,eb),(eb,ea)):
        nd=D[a_]+Le; upd=(nd<D[b_])&core_v[b_]&(nd<0.025)&~skirt[b_]&~headlock[b_]
        if upd.any(): D[b_[upd]]=nd[upd]; PB[b_[upd]]=PB[a_[upd]]
ring=core_v&np.isfinite(D)&(PB>=0)&(D>0)
fr=np.zeros(N); fr[ring]=1-ss(0,0.025,D[ring])
oh=np.zeros_like(W); oh[np.nonzero(ring)[0],PB[ring]]=1
W=W*(1-fr[:,None])+oh*fr[:,None]
# head lock last: face/skull above the band is 100% Head
W[headlock]=0; W[headlock,bi[P_+'Head']]=1
W[W<0.02]=0; order=np.argsort(-W,1); keep=np.zeros_like(W,bool); np.put_along_axis(keep,order[:,:4],True,1); W*=keep
W/=W.sum(1,keepdims=True)
rep.update(rigid_pieces=nrig,rigid_by_bone=rigid_by,ornament_verts=int(orn.sum()),skirt_verts_on_hips=int(skirt.sum()),
  head_locked_verts=int(headlock.sum()),head_band_verts=int((t&(hd>0.001)&(hd<0.999)).sum()),max_influences=int((W>0).sum(1).max()))
legbone=[bi[P_+s+k] for s in ('Left','Right') for k in ('UpLeg','Leg','Foot')]
rep['skirt_verts_with_leg_weight']=int((W[skirt][:,legbone]>0).any(1).sum())
for n in names:
    vg=body.vertex_groups.new(name=n); col=W[:,bi[n]]
    for i in np.nonzero(col>0)[0]: vg.add([int(i)],float(col[i]),'REPLACE')
body.parent=arm; md=body.modifiers.new('Armature','ARMATURE'); md.object=arm
np.save('/workspace/oakenshield_fresh/work/part.npy',part)
bpy.ops.wm.save_as_mainfile(filepath='/workspace/oakenshield_fresh/work/rig.blend',compress=True)
json.dump(rep,open('/workspace/oakenshield_fresh/work/rig.json','w'),indent=1); print('RIG',json.dumps(rep))
