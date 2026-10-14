"""Rowan A-pose body rig (2026-10-14): split.blend ROWAN_body (coat tails already split off) -> rig.blend. 20 mixamorig bones, Generic.
Arms: A-pose gap to the torso; every arm vertex is on Arm/ForeArm/Hand only (elbow blend +-3 cm, hard wrist: hand+fingers 100% Hand,
lower forearm 100% ForeArm). Face/skull 100% Head above a tilted band; coat collar (blue) never on Head. Belt, pouches, pelvis 100% Hips."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import os; HK=float(os.environ.get("HK","0.9"))
import bpy, numpy as np, json, bmesh
from mathutils import Vector
import scipy.sparse as sp, scipy.sparse.csgraph as cg
W_='/workspace/rowan_fresh/w2'
bpy.ops.wm.open_mainfile(filepath=W_+'/split.blend')
body=bpy.data.objects['ROWAN_body']; me=body.data
for o in list(bpy.data.objects):
    if o.name!='ROWAN_body': bpy.data.objects.remove(o)
N=len(me.vertices); P=np.empty(N*3); me.vertices.foreach_get('co',P); P=P.reshape(-1,3)
rep={'faces':len(me.polygons),'verts':N}
J={'Hips':(0,0.02,0.96),'Spine':(0,0.02,1.08),'Spine1':(0,0.02,1.18),'Spine2':(0,0.02,1.29),'Neck':(0,0.02,1.48),'Head':(0,0.01,1.555),'HeadTop':(0,0.0,1.80)}
for s_,x in (('Left',1),('Right',-1)):
    J[s_+'Shoulder']=(0.04*x,0.03,1.44); J[s_+'Arm']=(0.195*x,0.03,1.42); J[s_+'ForeArm']=(0.37*x,0.025,1.15)
    J[s_+'Hand']=(0.452*x,-0.025,1.005); J[s_+'HandEnd']=(0.49*x,-0.09,0.83)
    J[s_+'UpLeg']=(0.10*x,0.02,0.90); J[s_+'Leg']=(0.125*x,-0.01,0.50); J[s_+'Foot']=(0.155*x,0.06,0.11); J[s_+'FootEnd']=(0.17*x,-0.15,0.02)
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
x,y,z=P.T
# vertex colour (blue = coat cloth)
col=np.load(W_+'/vcol.npy'); PB=np.load(W_+'/P_body.npy')
from scipy.spatial import cKDTree
_,ii=cKDTree(PB).query(P); r,g,b=col[ii].T; blue=(b>r*1.12)&(b>g*1.02)
part=np.zeros(N,np.int8)
for s,pid,sg in (('Left',1,1),('Right',2,-1)):
    A0=J[s+'Arm']; dU=(J[s+'ForeArm']-A0); dU/=np.linalg.norm(dU)
    thr=np.interp(z,[0.78,1.04,1.10,1.16,1.20,1.24,1.30],[0.36,0.33,0.29,0.25,0.225,0.21,0.20])
    low=(z<1.30)&(z>0.78)&(x*sg>thr)
    high=(z>=1.30)&(x*sg>0.15)&(((P-A0)@dU)>-0.005)
    part[low|high]=pid
for s,pid,sg in (('Left',3,1),('Right',4,-1)):
    d,_,_=segdist(P,*SEG[s+'UpLeg']); dl,_,_=segdist(P,*SEG[s+'Leg']); df,_,_=segdist(P,*SEG[s+'Foot'])
    part[(x*sg>0.0)&(part==0)&((z<0.40)|((z<0.86)&(np.minimum.reduce([d,dl,df])<0.14)))]=pid
rep['parts']={k:int((part==v).sum()) for k,v in (('torso_head',0),('L_arm',1),('R_arm',2),('L_leg',3),('R_leg',4))}
F=np.empty(len(me.polygons)*3,int); me.polygons.foreach_get('vertices',F); F=F.reshape(-1,3)
pf=part[F]; mixed=(pf.min(1)!=pf.max(1)); zf=z[F].mean(1)
armmix=mixed&((pf==1)|(pf==2)).any(1)&(zf<1.20)
legmix=mixed&((pf==3)|(pf==4)).any(1)&(pf==0).any(1)&(zf<0.80)
rep['arm_torso_faces_cut']=int(armmix.sum()); rep['leg_torso_faces_cut']=int(legmix.sum())
kill=np.nonzero(armmix|legmix)[0]
if len(kill):
    bm=bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table()
    bmesh.ops.delete(bm,geom=[bm.faces[i] for i in kill],context='FACES_ONLY')
    bm.verts.ensure_lookup_table(); keepv=np.array([len(v.link_faces)>0 for v in bm.verts])
    bmesh.ops.delete(bm,geom=[v for v in bm.verts if not v.link_faces],context='VERTS'); bm.to_mesh(me); bm.free(); me.update()
    P=P[keepv]; part=part[keepv]; blue=blue[keepv]; N=len(P); x,y,z=P.T
rep['faces']=len(me.polygons); rep['verts']=N
W=np.zeros((N,NB))
def add(n,w): W[:,bi[P_+n]]+=w
def chain(u,knots,bl):
    w=np.zeros((len(u),len(knots)-1))
    for i in range(len(knots)-1):
        lo=1.0 if i==0 else ss(knots[i]-bl,knots[i]+bl,u); hi=1.0 if i==len(knots)-2 else 1-ss(knots[i+1]-bl,knots[i+1]+bl,u); w[:,i]=lo*hi
    return w/np.maximum(w.sum(1,keepdims=True),1e-9)
t=part==0
zc=1.505+np.clip(y+0.12,0,0.25)*0.12; hd=ss(zc-0.008,zc+0.008,z)*(~blue)
cw=chain(z,[-9,1.08,1.18,1.29,1.48,9],0.05)*(1-hd)[:,None]
for i,n in enumerate(('Hips','Spine','Spine1','Spine2','Neck')): add(n,cw[:,i]*t)
add('Head',hd*t)
for s,sg in (('Left',1),('Right',-1)):   # clavicle / top of the deltoid on the torso side rides the Shoulder bone
    d,_,_=segdist(P,*SEG[s+'Shoulder']); w=t*(x*sg>0.07)*(z>1.30)*ss(0.08,0.04,d)*(1-hd)
    W[:]*=(1-w)[:,None]; add(s+'Shoulder',w)
for s,pid in (('Left',1),('Right',2)):
    m=part==pid; A,Fo,H,He=J[s+'Arm'],J[s+'ForeArm'],J[s+'Hand'],J[s+'HandEnd']
    ds=[segdist(P,a,b) for a,b in ((A,Fo),(Fo,H),(H,He))]; L1,L2=ds[0][2],ds[1][2]
    # position along the arm: projection on the elbow->wrist axis decides forearm vs hand (hard wrist cut on the wrist plane)
    dF=(H-Fo)/L2; uF=(P-Fo)@dF; dA=(Fo-A)/L1; uA=(P-A)@dA
    elbow=ss(-0.03,0.03,uF)                       # 0 upper arm, 1 forearm
    hand=(uF>=L2-0.005)                            # beyond the wrist plane: 100% Hand
    upper=(1-elbow); root=ss(-0.02,0.03,uA)
    wA=upper*root; wS=upper*(1-root); wF=elbow*(~hand); wH=hand.astype(float)
    wA[hand]=0; wS[hand]=0
    lowfore=m&~hand&(uF>0.04)                       # lower forearm: 100% ForeArm
    wA[lowfore]=0; wS[lowfore]=0; wF[lowfore]=1
    # armpit helper (2026-10-14 v2): underarm sleeve 10-20 cm from the shoulder joint, on the side facing the ribs, shares up to
    # 55% with the Shoulder (clavicle, arm chain) so lowering the arm from the A-pose does not squash the sleeve into the ribs
    dsh=np.linalg.norm(P-A,axis=1); radial=(P-A)-np.outer(uA,dA); inn=ss(0.0,0.03,radial@np.array([-sg_,0,0])) if False else None
    side=np.array([-1.0,0,0]) if s=='Left' else np.array([1.0,0,0]); side=side-side.dot(dA)*dA; side/=np.linalg.norm(side)
    inner=ss(-0.025,0.01,radial@side); helper=HK*inner*(1-ss(0.16,0.23,dsh))*ss(0.08,0.11,dsh)*(~hand)*(uF<-0.05)
    wS=wS+wA*helper; wA=wA*(1-helper)
    tot=wA+wS+wF+wH; tot[tot==0]=1
    for n,w in (('Shoulder',wS),('Arm',wA),('ForeArm',wF),('Hand',wH)): add(s+n,(w/tot)*m)
    rep.setdefault('arm',{})[s]={'verts':int(m.sum()),'hand_100':int((m&hand).sum()),'lower_forearm_100':int(lowfore.sum())}
for s,pid in (('Left',3),('Right',4)):
    m=part==pid; Hh,K,A=J[s+'UpLeg'],J[s+'Leg'],J[s+'Foot']
    u=Hh[2]-z; L1=Hh[2]-K[2]; L2=K[2]-A[2]
    cw=chain(u,[-9,L1,L1+L2,9],0.02); root=ss(-0.01,0.05,u)
    add('Hips',cw[:,0]*(1-root)*m); add(s+'UpLeg',cw[:,0]*root*m); add(s+'Leg',cw[:,1]*m); add(s+'Foot',cw[:,2]*m)
s_=W.sum(1); W[s_<1e-6,bi[P_+'Hips']]=1; W/=W.sum(1,keepdims=True)
belt=t&(z<1.02); W[belt]=0; W[belt,bi[P_+'Hips']]=1
headlock=t&(hd>0.999); W[headlock]=0; W[headlock,bi[P_+'Head']]=1
W[W<0.02]=0; order=np.argsort(-W,1); keep=np.zeros_like(W,bool); np.put_along_axis(keep,order[:,:4],True,1); W*=keep; W/=W.sum(1,keepdims=True)
armb=[bi[P_+s+k] for s in ('Left','Right') for k in ('Shoulder','Arm','ForeArm','Hand')]
nonarm=[i for i in range(NB) if i not in armb]
am=(part==1)|(part==2)
rep['arm_verts_with_torso_or_hips_weight']=int((W[am][:,nonarm]>0).any(1).sum())
hb=[bi[P_+'LeftHand'],bi[P_+'RightHand']]
rep['hand_verts_not_100pct_hand']=int(((W[:,hb].max(1)>0)&(W[:,hb].max(1)<0.999)).sum())
legb=[bi[P_+s+k] for s in ('Left','Right') for k in ('UpLeg','Leg','Foot')]
rep.update(belt_hips_verts=int(belt.sum()),belt_with_leg_weight=int((W[belt][:,legb]>0).any(1).sum()),head_locked=int(headlock.sum()),
  head_band=int((t&(hd>0.001)&(hd<0.999)).sum()),max_infl=int((W>0).sum(1).max()))
for n in names:
    vg=body.vertex_groups.new(name=n); colw=W[:,bi[n]]
    for i in np.nonzero(colw>0)[0]: vg.add([int(i)],float(colw[i]),'REPLACE')
body.parent=arm; md=body.modifiers.new('Armature','ARMATURE'); md.object=arm
json.dump({k:v.tolist() for k,v in J.items()},open(W_+'/joints.json','w'))
np.save(W_+'/part.npy',part)
bpy.ops.wm.save_as_mainfile(filepath=W_+'/rig.blend',compress=True)
json.dump(rep,open(W_+'/rig.json','w'),indent=1); print('RIG',json.dumps(rep))
