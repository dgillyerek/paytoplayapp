"""Cape: fit the Meshy cloak around the body in its REST pose (push out of a 3.5 cm shell, smoothed), build its own armature
(3 anchors that copy body Spine2/LeftShoulder/RightShoulder + 7 chains x 3 bones), skin it, save cape.blend + cape_chains.json."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, numpy as np, json, math
from mathutils import Vector
from scipy import ndimage
from scipy.spatial import cKDTree
W_='/workspace/bonequill_gritty/work'
bpy.ops.wm.open_mainfile(filepath=W_+'/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; body=bpy.data.objects['BONEQUILL_body']
arm.animation_data.action=bpy.data.actions['BONEQUILL_rest']; sc.frame_set(0)
POSES=[('BONEQUILL_rest',0)]+[('BONEQUILL_walk',f) for f in (0,4,8,12,15,19,23,27)]
Bs=[]
for an,fr in POSES:
    arm.animation_data.action=bpy.data.actions[an]; sc.frame_set(fr)
    dg=bpy.context.evaluated_depsgraph_get(); eb=body.evaluated_get(dg); m=eb.to_mesh()
    b=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',b); Bs.append(b.reshape(-1,3))
    BF0=np.array([p.vertices[:] for p in m.polygons]); eb.to_mesh_clear()
nv=len(Bs[0]); B=np.concatenate(Bs); BF=np.concatenate([BF0+k*nv for k in range(len(Bs))])
arm.animation_data.action=bpy.data.actions['BONEQUILL_rest']; sc.frame_set(0)
with bpy.data.libraries.load(W_+'/dec_cape.blend') as (src,dst): dst.objects=['BONEQUILL_cape']
cape=dst.objects[0]; sc.collection.objects.link(cape); me=cape.data
N=len(me.vertices); C=np.empty(N*3); me.vertices.foreach_get('co',C); C=C.reshape(-1,3)
E=np.array([e.vertices[:] for e in me.edges]); ea,ebb=E[:,0],E[:,1]
rep={'cape_faces':len(me.polygons),'cape_verts':N}
def ss0(e0,e1,x): t=np.clip((x-e0)/(e1-e0),0,1); return t*t*(3-2*t)
YC=0.03; C[:,1]=YC+(C[:,1]-YC)*0.65   # gritty cloak shell is ~2x deeper than the body: pull it in before fitting
r0=np.hypot(C[:,0],C[:,1]-YC); th0=np.arctan2(C[:,0],C[:,1]-YC)
SC=0.40+0.60*ss0(1.36,1.52,C[:,2]); th1=th0*SC
wr=ss0(0.5,1.0,np.abs(th1))*(1-ss0(1.30,1.42,C[:,2])); r1=np.maximum(r0, 0.30*wr+r0*(1-wr))
C=np.stack([r1*np.sin(th1), YC+r1*np.cos(th1), C[:,2]],1)
rep['front_edge_deg_after']=round(float(np.degrees(np.abs(th1[C[:,2]<1.30]).max())),1)
# ---- body solid in rest pose (5 mm voxels), shell 3.5 cm ----
g=0.005; lo=np.minimum(B.min(0),C.min(0))-0.08; hi=np.maximum(B.max(0),C.max(0))+0.08; shp=np.ceil((hi-lo)/g).astype(int)+1
solid=np.zeros(shp,bool)
for k in range(len(Bs)):
    Bk=Bs[k]; BFk=BF0
    V=np.zeros(shp,bool); A_,B_,C_=Bk[BFk[:,0]],Bk[BFk[:,1]],Bk[BFk[:,2]]
    Lm=np.maximum.reduce([np.linalg.norm(A_-B_,axis=1),np.linalg.norm(B_-C_,axis=1),np.linalg.norm(C_-A_,axis=1)]); nn=np.clip(np.ceil(Lm/(g*0.5)).astype(int),1,40)
    for k in np.unique(nn):
        mk=nn==k; a,b,c=A_[mk],B_[mk],C_[mk]
        for i in range(k+1):
            for j in range(k+1-i):
                p=a+(b-a)*(i/k)+(c-a)*(j/k); ij=np.floor((p-lo)/g).astype(int); V[ij[:,0],ij[:,1],ij[:,2]]=True
    solid|=ndimage.binary_fill_holes(ndimage.binary_closing(V,iterations=3))
dist=ndimage.distance_transform_edt(~solid)*g     # distance outside the body
SHELL=0.04
def axis_pt(P):
    z=np.clip(P[:,2],0.9,1.68); yc=np.interp(z,[0.9,1.68],[0.03,0.035]); return np.stack([np.zeros(len(P)),yc,z],1)
def dval(P):
    ij=np.clip(np.floor((P-lo)/g).astype(int),0,np.array(shp)-1); return dist[ij[:,0],ij[:,1],ij[:,2]]
deg=np.bincount(np.concatenate([ea,ebb]),minlength=N).astype(float)
D=np.zeros_like(C); before=int((dval(C)<SHELL).sum())
for it in range(60):
    Q=C+D; dv=dval(Q); bad=dv<SHELL
    if not bad.any(): break
    dirn=Q-axis_pt(Q); dirn/=np.maximum(np.linalg.norm(dirn,axis=1,keepdims=True),1e-6)
    D[bad]+=dirn[bad]*np.minimum(SHELL-dv[bad]+0.004,0.02)[:,None]
    for s in range(6):   # smooth the displacement field so the cloth moves as a sheet
        acc=np.zeros_like(D); np.add.at(acc,ea,D[ebb]); np.add.at(acc,ebb,D[ea]); D=0.5*D+0.5*acc/np.maximum(deg,1)[:,None]
C2=C+D; rep.update(verts_inside_shell_before=before,verts_inside_shell_after=int((dval(C2)<SHELL*0.8).sum()),fit_iters=it+1,
  max_push_cm=round(float(np.linalg.norm(D,axis=1).max()*100),1),mean_push_cm=round(float(np.linalg.norm(D,axis=1).mean()*100),2))
me.vertices.foreach_set('co',C2.ravel()); me.update(); C=C2
# ---- cape armature ----
ad=bpy.data.armatures.new('BONEQUILL_cape_rig'); crig=bpy.data.objects.new('BONEQUILL_cape_rig',ad); sc.collection.objects.link(crig)
bpy.context.view_layer.objects.active=crig; bpy.ops.object.mode_set(mode='EDIT')
ANCH={'cape_Head':'mixamorig:Head','cape_Spine2':'mixamorig:Spine2','cape_LeftShoulder':'mixamorig:LeftShoulder','cape_RightShoulder':'mixamorig:RightShoulder'}
for cn,bn in ANCH.items():
    bb=arm.data.bones[bn]; e=ad.edit_bones.new(cn); e.head=bb.head_local; e.tail=bb.tail_local; e.matrix=bb.matrix_local.copy(); e.length=bb.length
yc=0.03; th=np.degrees(np.arctan2(C[:,0],C[:,1]-yc))
SECT=[('B',0,'cape_Spine2'),('BL',22,'cape_Spine2'),('L',44,'cape_Spine2'),('FL',64,'cape_LeftShoulder'),
      ('FR',-64,'cape_RightShoulder'),('R',-44,'cape_Spine2'),('BR',-22,'cape_Spine2')]
TOP=1.30
chains=[]
def angd(a,b): return np.abs((a-b+180)%360-180)
for name,tc,par in SECT:
    sel=angd(th,tc)<9
    if sel.sum()<50: print('skip',name); continue
    zmin=float(C[sel,2].min()); hs=[TOP,TOP-(TOP-zmin)*0.33,TOP-(TOP-zmin)*0.66,zmin+0.02]
    pts=[]
    for h in hs:
        q=C[sel&(np.abs(C[:,2]-h)<0.04)]
        if len(q)<5: q=C[sel&(np.abs(C[:,2]-h)<0.10)]
        p=np.median(q,0); p[2]=h; pts.append(p)
    bones=[f'cape_{name}_0{i+1}' for i in range(3)]
    prev=ad.edit_bones[par]
    for i,bn in enumerate(bones):
        e=ad.edit_bones.new(bn); e.head=Vector(pts[i]); e.tail=Vector(pts[i+1]); e.parent=prev; e.use_connect=i>0; e.align_roll(Vector((0,-1,0)) if 'F' in name[:1] else Vector((0,1,0))); prev=e
    chains.append(dict(name='cape_'+name,sector=name,parent=par,bones=bones,joints=[list(map(float,p)) for p in pts],theta=tc,length=float(sum(np.linalg.norm(pts[i+1]-pts[i]) for i in range(3)))))
bpy.ops.object.mode_set(mode='OBJECT')
# ---- weights: mantle/hood rigid on the anchors; below TOP the chains by height, blended between neighbouring sectors by angle ----
names=list(ANCH)+[b for c in chains for b in c['bones']]; bi={n:i for i,n in enumerate(names)}
Wt=np.zeros((N,len(names))); z=C[:,2]
def ss(e0,e1,x): t=np.clip((x-e0)/(e1-e0),0,1); return t*t*(3-2*t)
anchor=ss(TOP-0.10,TOP+0.03,z)          # 1 on the mantle and hood
side=ss(0.10,0.16,np.abs(C[:,0]))*(C[:,1]<yc+0.06)   # front mantle over the shoulders
hood=ss(1.50,1.58,z)*(1-ss(0.17,0.24,np.abs(C[:,0])))   # hood rides the skull (Head), mantle rides Spine2/shoulders
for cn,w in (('cape_Head',hood),('cape_Spine2',(1-side)*(1-hood)),('cape_LeftShoulder',side*(C[:,0]>0)*(1-hood)),('cape_RightShoulder',side*(C[:,0]<=0)*(1-hood))): Wt[:,bi[cn]]+=anchor*w
ang=np.stack([np.clip(1-angd(th,c['theta'])/22.0,0,1)+1e-4 for c in chains],1); ang/=np.maximum(ang.sum(1,keepdims=True),1e-6)
for k,c in enumerate(chains):
    J=np.array(c['joints']); hz=J[:,2]
    w=np.zeros((N,3))
    for i in range(3):
        lo_=1.0 if i==0 else ss(hz[i]+0.06,hz[i]-0.06,z); hi_=1.0 if i==2 else 1-ss(hz[i+1]+0.06,hz[i+1]-0.06,z); w[:,i]=lo_*hi_
    w/=np.maximum(w.sum(1,keepdims=True),1e-9)
    for i,bn in enumerate(c['bones']): Wt[:,bi[bn]]+=(1-anchor)*ang[:,k]*w[:,i]
Wt[Wt<0.02]=0; order=np.argsort(-Wt,1); keep=np.zeros_like(Wt,bool); np.put_along_axis(keep,order[:,:4],True,1); Wt*=keep
z0=Wt.sum(1)<1e-6; Wt[z0,bi['cape_Spine2']]=1; Wt/=Wt.sum(1,keepdims=True)
for n in names:
    vg=cape.vertex_groups.new(name=n); col=Wt[:,bi[n]]
    for i in np.nonzero(col>0)[0]: vg.add([int(i)],float(col[i]),'REPLACE')
cape.parent=crig; md=cape.modifiers.new('Armature','ARMATURE'); md.object=crig
cape.data.materials[0].name='BONEQUILL_cape'
rep.update(chains=len(chains),cape_bones=len(names),max_infl=int((Wt>0).sum(1).max()),anchor_rigid_verts=int((anchor>0.999).sum()))
json.dump(chains,open(W_+'/cape_chains.json','w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=W_+'/cape.blend',compress=True)
json.dump(rep,open(W_+'/cape.json','w'),indent=1); print('CAPE',json.dumps(rep))
