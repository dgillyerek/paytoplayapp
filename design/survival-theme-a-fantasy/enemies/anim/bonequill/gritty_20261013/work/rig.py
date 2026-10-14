"""Bonequill fresh body rig (2026-10-13): dec_body.blend (already 1.80 m, feet on 0) -> rig.blend. 20 mixamorig bones.
Skeleton = rigid segments: every vertex on its nearest bone of its part, narrow blends only at the joints (+-2 cm),
skull/jaw 100% Head above a tilted band (+-0.8 cm), loincloth + pelvis 100% Hips (never legs), hips->thigh ramp 6 cm."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, numpy as np, json
from mathutils import Vector
import scipy.sparse as sp, scipy.sparse.csgraph as cg
bpy.ops.wm.open_mainfile(filepath='/workspace/bonequill_gritty/work/dec_body.blend')
body=bpy.data.objects['BONEQUILL_body']; me=body.data
N=len(me.vertices); P=np.empty(N*3); me.vertices.foreach_get('co',P); P=P.reshape(-1,3)
E=np.array([e.vertices[:] for e in me.edges]); ea,eb=E[:,0],E[:,1]
rep={'faces':len(me.polygons),'verts':N}
J={'Hips':(0,0.03,1.00),'Spine':(0,0.03,1.12),'Spine1':(0,0.035,1.22),'Spine2':(0,0.04,1.32),'Neck':(0,0.045,1.50),'Head':(0,0.035,1.575),'HeadTop':(0,0.02,1.80)}
for s,x in (('Left',1),('Right',-1)):
    J[s+'Shoulder']=(0.05*x,0.04,1.47); J[s+'Arm']=(0.21*x,0.04,1.42); J[s+'ForeArm']=(0.30*x,0.05,1.19)
    J[s+'Hand']=(0.335*x,0.03,0.97); J[s+'HandEnd']=(0.345*x,0.0,0.80)
    J[s+'UpLeg']=(0.13*x,0.03,0.92); J[s+'Leg']=(0.18*x,0.035,0.56); J[s+'Foot']=(0.19*x,0.06,0.12); J[s+'FootEnd']=(0.20*x,-0.12,0.02)
J={k:np.array(v,float) for k,v in J.items()}
BONES=[('Hips',None,'Spine'),('Spine','Hips','Spine1'),('Spine1','Spine','Spine2'),('Spine2','Spine1','Neck'),('Neck','Spine2','Head'),('Head','Neck','HeadTop')]
for s in ('Left','Right'): BONES+=[(s+'Shoulder','Spine2',s+'Arm'),(s+'Arm',s+'Shoulder',s+'ForeArm'),(s+'ForeArm',s+'Arm',s+'Hand'),(s+'Hand',s+'ForeArm',s+'HandEnd')]
for s in ('Left','Right'): BONES+=[(s+'UpLeg','Hips',s+'Leg'),(s+'Leg',s+'UpLeg',s+'Foot'),(s+'Foot',s+'Leg',s+'FootEnd')]
P_='mixamorig:'
ad=bpy.data.armatures.new('BONEQUILL_rig'); arm=bpy.data.objects.new('BONEQUILL_rig',ad); bpy.context.scene.collection.objects.link(arm)
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
# legs + arms: geodesic split from seeds (hands touch the hip armour and the belt flap touches the thighs, so no plane works)
L=np.linalg.norm(P[ea]-P[eb],axis=1); G=sp.coo_matrix((L,(ea,eb)),shape=(N,N)).tocsr()
seedlab={}
for v in np.nonzero((np.abs(x)<0.16)&(z>0.95)&(z<1.45))[0]: seedlab[v]=0
for v in np.nonzero((np.abs(x)<0.045)&(z>0.60)&(z<1.0))[0]: seedlab[v]=0
for s,pid,sg in (('Left',3,1),('Right',4,-1)):
    d,_,_=segdist(P,*SEG[s+'UpLeg']); dl,_,_=segdist(P,*SEG[s+'Leg'])
    for v in np.nonzero(((x*sg>0.0)&(z<0.50))|((np.minimum(d,dl)<0.06)&(z<0.84)&(x*sg>0.11)))[0]: seedlab[v]=pid
for s,pid,sg in (('Left',1,1),('Right',2,-1)):
    dF,_,_=segdist(P,*SEG[s+'ForeArm']); dH,_,_=segdist(P,*SEG[s+'Hand']); dA,_,_=segdist(P,*SEG[s+'Arm'])
    for v in np.nonzero((x*sg>0.27)&(np.minimum(dF,dH)<0.05)|((x*sg>0.24)&(dA<0.03)&(z<1.38)))[0]: seedlab[v]=pid
src=np.array(list(seedlab)); _,_,srcs=cg.dijkstra(G,directed=False,indices=src,min_only=True,return_predecessors=True)
lab=np.array([seedlab.get(int(k),0) if k>=0 else 0 for k in srcs]); lab[src]=[seedlab[int(v)] for v in src]
part[:]=lab
rep['parts']={k:int((part==v).sum()) for k,v in (('torso_head',0),('L_arm',1),('R_arm',2),('L_leg',3),('R_leg',4))}
def chain(u,knots,bl):
    w=np.zeros((len(u),len(knots)-1))
    for i in range(len(knots)-1):
        lo=1.0 if i==0 else ss(knots[i]-bl,knots[i]+bl,u); hi=1.0 if i==len(knots)-2 else 1-ss(knots[i+1]-bl,knots[i+1]+bl,u); w[:,i]=lo*hi
    return w/np.maximum(w.sum(1,keepdims=True),1e-9)
t=part==0
zc=1.57+np.clip(y+0.08,0,0.2)*0.10; hd=ss(zc-0.008,zc+0.008,z)*(1-ss(0.09,0.11,np.abs(x)))   # skull only: pauldron tips beside the jaw stay on the torso
cw=chain(z,[-9,1.12,1.22,1.32,1.50,9],0.02)*(1-hd)[:,None]
for i,n in enumerate(('Hips','Spine','Spine1','Spine2','Neck')): add(n,cw[:,i]*t)
add('Head',hd*t)
for s,sg in (('Left',1),('Right',-1)):   # clavicle/scapula region rides the Shoulder bone
    d,_,_=segdist(P,*SEG[s+'Shoulder']); w=t*(x*sg>0.08)*(z>1.32)*ss(0.08,0.04,d)*(1-hd)
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
loin=t&(z<1.10); W[loin]=0; W[loin,bi[P_+'Hips']]=1
headlock=t&(hd>0.999); W[headlock]=0; W[headlock,bi[P_+'Head']]=1
# armpit/shoulder seam: diffuse weights inside 11 cm of each Arm joint so a raised arm stretches a band, not a seam
deg_=np.bincount(np.concatenate([ea,eb]),minlength=N).astype(float)
for s_n in ('Left','Right'):
    rr=np.linalg.norm(P-J[s_n+'Arm'],axis=1); m=(rr<0.16)&~headlock; fall=ss(0.16,0.07,rr)[:,None]
    for it in range(40):
        acc=np.zeros_like(W); np.add.at(acc,ea,W[eb]); np.add.at(acc,eb,W[ea]); Ws=acc/np.maximum(deg_,1)[:,None]
        W[m]=(W*(1-fall)+Ws*fall)[m]
W[W<0.02]=0; order=np.argsort(-W,1); keep=np.zeros_like(W,bool); np.put_along_axis(keep,order[:,:4],True,1); W*=keep; W/=W.sum(1,keepdims=True)
legb=[bi[P_+s+k] for s in ('Left','Right') for k in ('UpLeg','Leg','Foot')]
rep.update(loincloth_hips_verts=int(loin.sum()),loincloth_with_leg_weight=int((W[loin][:,legb]>0).any(1).sum()),head_locked=int(headlock.sum()),
  head_band=int((t&(hd>0.001)&(hd<0.999)).sum()),max_infl=int((W>0).sum(1).max()),single_bone_verts=int((W.max(1)>0.999).sum()))
for n in names:
    vg=body.vertex_groups.new(name=n); col=W[:,bi[n]]
    for i in np.nonzero(col>0)[0]: vg.add([int(i)],float(col[i]),'REPLACE')
body.parent=arm; md=body.modifiers.new('Armature','ARMATURE'); md.object=arm
json.dump({k:v.tolist() for k,v in J.items()},open('/workspace/bonequill_gritty/work/joints.json','w'))
bpy.ops.wm.save_as_mainfile(filepath='/workspace/bonequill_gritty/work/rig.blend',compress=True)
json.dump(rep,open('/workspace/bonequill_gritty/work/rig.json','w'),indent=1); print('RIG',json.dumps(rep))
