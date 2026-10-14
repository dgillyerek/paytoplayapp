"""Rowan fresh body rig (2026-10-13): dec_body.blend (already 1.80 m, feet on 0) -> rig.blend. 20 mixamorig bones.
Skeleton = rigid segments: every vertex on its nearest bone of its part, narrow blends only at the joints (+-2 cm),
skull/jaw 100% Head above a tilted band (+-0.8 cm), loincloth + pelvis 100% Hips (never legs), hips->thigh ramp 6 cm."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, numpy as np, json
from mathutils import Vector
import scipy.sparse as sp, scipy.sparse.csgraph as cg
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/work/dec_body.blend')
body=bpy.data.objects['ROWAN_body']; me=body.data
N=len(me.vertices); P=np.empty(N*3); me.vertices.foreach_get('co',P); P=P.reshape(-1,3)
E=np.array([e.vertices[:] for e in me.edges]); ea,eb=E[:,0],E[:,1]
rep={'faces':len(me.polygons),'verts':N}
c=0.015
J={'Hips':(c,-0.02,0.96),'Spine':(c,-0.02,1.08),'Spine1':(c,-0.02,1.18),'Spine2':(c,-0.02,1.29),'Neck':(c,-0.01,1.47),'Head':(c,-0.02,1.535),'HeadTop':(c,-0.03,1.80)}
for s_,x in (('Left',1),('Right',-1)):
    J[s_+'Shoulder']=(c+0.04*x,-0.01,1.43); J[s_+'Arm']=(c+0.19*x,-0.01,1.40); J[s_+'ForeArm']=(c+0.255*x,-0.03,1.14)
    J[s_+'Hand']=(c+0.27*x,-0.08,0.93); J[s_+'HandEnd']=(c+0.275*x,-0.11,0.79)
    J[s_+'UpLeg']=(c+0.10*x,0.0,0.88); J[s_+'Leg']=(c+0.14*x,0.0,0.49); J[s_+'Foot']=(c+0.18*x,0.02,0.10); J[s_+'FootEnd']=(c+0.19*x,-0.17,0.02)
J={k:np.array(v,float) for k,v in J.items()}
BONES=[('Hips',None,'Spine'),('Spine','Hips','Spine1'),('Spine1','Spine','Spine2'),('Spine2','Spine1','Neck'),('Neck','Spine2','Head'),('Head','Neck','HeadTop')]
for s in ('Left','Right'): BONES+=[(s+'Shoulder','Spine2',s+'Arm'),(s+'Arm',s+'Shoulder',s+'ForeArm'),(s+'ForeArm',s+'Arm',s+'Hand'),(s+'Hand',s+'ForeArm',s+'HandEnd')]
for s in ('Left','Right'): BONES+=[(s+'UpLeg','Hips',s+'Leg'),(s+'Leg',s+'UpLeg',s+'Foot'),(s+'Foot',s+'Leg',s+'FootEnd')]
P_='mixamorig:'
ad=bpy.data.armatures.new('ROWAN_rig'); arm=bpy.data.objects.new('ROWAN_rig',ad); bpy.context.scene.collection.objects.link(arm)
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='EDIT')
for n,par,t in BONES:
    b=ad.edit_bones.new(P_+n); b.head=Vector(J[n]); b.tail=Vector(J[t]); b.align_roll(Vector((0,0,1)) if 'Foot' in n else Vector((0,-1,0)))
    if par: b.parent=ad.edit_bones[P_+par]; b.use_connect=bool(np.allclose(J[n],J[[x for x in BONES if x[0]==par][0][2]]))
bpy.ops.object.mode_set(mode='OBJECT')
names=[P_+n for n,_,_ in BONES]; bi={n:i for i,n in enumerate(names)}; NB=len(names); SEG={n:(J[n],J[t]) for n,_,t in BONES}
def segdist(Q,a,b):
    ab=b-a; L=np.linalg.norm(ab); t=((Q-a)@ab)/(L*L); q=a+np.clip(t,0,1)[:,None]*ab; return np.linalg.norm(Q-q,axis=1),t,L
def ss(e0,e1,x): t=np.clip((x-e0)/(e1-e0),0,1); return t*t*(3-2*t)
x,y,z=P.T; W=np.zeros((N,NB))
def add(n,w): W[:,bi[P_+n]]+=w
part=np.zeros(N,np.int8)
for s,pid,sg in (('Left',1,1),('Right',2,-1)):
    da,df,dh=[segdist(P,*SEG[s+k])[0] for k in ('Arm','ForeArm','Hand')]
    part[(((np.minimum(da,df)<0.10)&(z>1.05))|((np.minimum(df,dh)<0.085)&(z<=1.05)&(abs(x-0.015)>0.245)))&((x-0.015)*sg>0.17)&(z>0.74)&(z<1.42)]=pid
for s,pid,sg in (('Left',3,1),('Right',4,-1)):
    d,_,_=segdist(P,*SEG[s+'UpLeg']); dl,_,_=segdist(P,*SEG[s+'Leg'])
    part[((x-0.015)*sg>0.0)&(part==0)&((z<0.40)|((z<0.86)&(np.minimum(d,dl)<0.12)&(y>-0.16)&(y<0.065)))]=pid
rep['parts']={k:int((part==v).sum()) for k,v in (('torso_head',0),('L_arm',1),('R_arm',2),('L_leg',3),('R_leg',4))}
# rip: delete faces that join an arm to the torso/coat below the armpit, or a leg to the coat below the hip ramp
import bmesh
F=np.empty(len(me.polygons)*3,int); me.polygons.foreach_get('vertices',F); F=F.reshape(-1,3)
pf=part[F]; mixed=(pf.min(1)!=pf.max(1)); zf=z[F].mean(1)
armmix=mixed&((pf==1)|(pf==2)).any(1)&(zf<1.32)
legmix=mixed&((pf==3)|(pf==4)).any(1)&(pf==0).any(1)&(zf<0.80)
kill=np.nonzero(armmix|legmix)[0]; rep['ripped_faces_arm']=int(armmix.sum()); rep['ripped_faces_leg']=int(legmix.sum())
bm=bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
bmesh.ops.delete(bm,geom=[bm.faces[i] for i in kill],context='FACES_ONLY')
bm.verts.ensure_lookup_table(); keepv=np.array([len(v.link_faces)>0 for v in bm.verts])
bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS'); bm.to_mesh(me); bm.free(); me.update()
P=P[keepv]; part=part[keepv]; N=len(P); x,y,z=P.T; W=np.zeros((N,NB)); rep['faces']=len(me.polygons); rep['verts']=N
assert N==len(me.vertices)
import scipy.sparse as sp2, scipy.sparse.csgraph as cg2
from scipy.spatial import cKDTree
E2=np.empty(len(me.edges)*2,int); me.edges.foreach_get('vertices',E2); E2=E2.reshape(-1,2)
nc,cl=cg2.connected_components(sp2.coo_matrix((np.ones(len(E2)),(E2[:,0],E2[:,1])),shape=(N,N)),directed=False); csz=np.bincount(cl)
nz=np.nonzero(part>0)[0]; tr=cKDTree(P[nz]); moved=0
for k in np.nonzero(csz<1500)[0]:
    idx=np.nonzero(cl==k)[0]
    if (part[idx]!=0).any(): continue
    d,j=tr.query(P[idx].mean(0))
    if d<0.10: part[idx]=part[nz[j]]; moved+=len(idx)
rep['islands_after_rip']=int(nc); rep['small_islands_reassigned_verts']=int(moved)
def chain(u,knots,bl):
    w=np.zeros((len(u),len(knots)-1))
    for i in range(len(knots)-1):
        lo=1.0 if i==0 else ss(knots[i]-bl,knots[i]+bl,u); hi=1.0 if i==len(knots)-2 else 1-ss(knots[i+1]-bl,knots[i+1]+bl,u); w[:,i]=lo*hi
    return w/np.maximum(w.sum(1,keepdims=True),1e-9)
t=part==0
zc=1.50+np.clip(y+0.12,0,0.25)*0.12; hd=ss(zc-0.008,zc+0.008,z)
cw=chain(z,[-9,1.08,1.18,1.29,1.47,9],0.03)*(1-hd)[:,None]
for i,n in enumerate(('Hips','Spine','Spine1','Spine2','Neck')): add(n,cw[:,i]*t)
add('Head',hd*t)
for s,sg in (('Left',1),('Right',-1)):   # clavicle/scapula region rides the Shoulder bone
    d,_,_=segdist(P,*SEG[s+'Shoulder']); w=t*((x-0.015)*sg>0.07)*(z>1.30)*ss(0.08,0.04,d)*(1-hd)
    W[:]*=(1-w)[:,None]; add(s+'Shoulder',w)
for s,pid in (('Left',1),('Right',2)):
    m=part==pid; A,F,H,He=J[s+'Arm'],J[s+'ForeArm'],J[s+'Hand'],J[s+'HandEnd']
    ds=[segdist(P,a,b) for a,b in ((A,F),(F,H),(H,He))]; L1,L2=ds[0][2],ds[1][2]
    k=np.argmin(np.stack([d[0] for d in ds],1),1)
    u=np.where(k==0,ds[0][1]*L1,np.where(k==1,L1+np.clip(ds[1][1],0,1.3)*L2,L1+L2+np.clip(ds[2][1],0,2)*ds[2][2]))
    cw=chain(u,[-9,L1,L1+L2,9],0.02); root=ss(-0.02,0.03,u)
    add(s+'Shoulder',cw[:,0]*(1-root)*m); add(s+'Arm',cw[:,0]*root*m); add(s+'ForeArm',cw[:,1]*m); add(s+'Hand',cw[:,2]*m)
for s,pid in (('Left',3),('Right',4)):
    m=part==pid; Hh,K,A=J[s+'UpLeg'],J[s+'Leg'],J[s+'Foot']
    u=Hh[2]-z; L1=Hh[2]-K[2]; L2=K[2]-A[2]
    cw=chain(u,[-9,L1,L1+L2,9],0.02); root=ss(-0.01,0.05,u)
    add('Hips',cw[:,0]*(1-root)*m); add(s+'UpLeg',cw[:,0]*root*m); add(s+'Leg',cw[:,1]*m); add(s+'Foot',cw[:,2]*m)
s_=W.sum(1); W[s_<1e-6,bi[P_+'Hips']]=1; W/=W.sum(1,keepdims=True)
# loincloth + pelvis: everything in the torso part below the belt
loin=t&(z<1.02); W[loin]=0; W[loin,bi[P_+'Hips']]=1
headlock=t&(hd>0.999); W[headlock]=0; W[headlock,bi[P_+'Head']]=1
bowm=np.load('/workspace/rowan_fresh/work/bowmask3.npy')[keepv]|((x<-0.165)&(z>1.45)&(part==0)); W[bowm]=0; W[bowm,bi[P_+'Spine2']]=1; rep['bow_on_back_spine2']=int(bowm.sum())
W[W<0.02]=0; order=np.argsort(-W,1); keep=np.zeros_like(W,bool); np.put_along_axis(keep,order[:,:4],True,1); W*=keep; W/=W.sum(1,keepdims=True)
legb=[bi[P_+s+k] for s in ('Left','Right') for k in ('UpLeg','Leg','Foot')]
rep.update(loincloth_hips_verts=int(loin.sum()),loincloth_with_leg_weight=int((W[loin][:,legb]>0).any(1).sum()),head_locked=int(headlock.sum()),
  head_band=int((t&(hd>0.001)&(hd<0.999)).sum()),max_infl=int((W>0).sum(1).max()),single_bone_verts=int((W.max(1)>0.999).sum()))
for n in names:
    vg=body.vertex_groups.new(name=n); col=W[:,bi[n]]
    for i in np.nonzero(col>0)[0]: vg.add([int(i)],float(col[i]),'REPLACE')
body.parent=arm; md=body.modifiers.new('Armature','ARMATURE'); md.object=arm
json.dump({k:v.tolist() for k,v in J.items()},open('/workspace/rowan_fresh/work/joints.json','w'))
bpy.ops.wm.save_as_mainfile(filepath='/workspace/rowan_fresh/work/rig.blend',compress=True)
json.dump(rep,open('/workspace/rowan_fresh/work/rig.json','w'),indent=1); print('RIG',json.dumps(rep))
