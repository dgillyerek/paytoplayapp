"""Coat tails: own armature ROWAN_coat_rig = anchor coat_Hips (copy of body Hips, reparented onto it in Unity) + 7 chains x 3 bones
hanging from it (B, BL, L, FL, BR, R, FR). Belt band rigid on the anchor, tails by height on the chains, blended between neighbouring
sectors by angle. No leg bones anywhere: the legs only push the tails through ClothSpringRig colliders."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, numpy as np, json
from mathutils import Vector
W_='/workspace/rowan_fresh/w2'
bpy.ops.wm.open_mainfile(filepath=W_+'/rig.blend')
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']
with bpy.data.libraries.load(W_+'/split.blend') as (src,dst): dst.objects=['ROWAN_coat']
coat=dst.objects[0]; sc.collection.objects.link(coat); me=coat.data
N=len(me.vertices); C=np.empty(N*3); me.vertices.foreach_get('co',C); C=C.reshape(-1,3)
rep={'coat_faces':len(me.polygons),'coat_verts':N}
ad=bpy.data.armatures.new('ROWAN_coat_rig'); crig=bpy.data.objects.new('ROWAN_coat_rig',ad); sc.collection.objects.link(crig)
bpy.context.view_layer.objects.active=crig; bpy.ops.object.mode_set(mode='EDIT')
bb=arm.data.bones['mixamorig:Hips']; e=ad.edit_bones.new('coat_Hips'); e.head=bb.head_local; e.tail=bb.tail_local; e.matrix=bb.matrix_local.copy(); e.length=bb.length
yc=0.02; th=np.degrees(np.arctan2(C[:,0],C[:,1]-yc))
SECT=[('B',0),('BL',45),('L',80),('FL',110),('FR',-110),('R',-80),('BR',-45)]
TOP=0.90
def angd(a,b): return np.abs((a-b+180)%360-180)
chains=[]
for name,tc in SECT:
    sel=angd(th,tc)<18
    zmin=float(C[sel,2].min()); hs=[TOP,TOP-(TOP-zmin)*0.33,TOP-(TOP-zmin)*0.66,zmin+0.02]
    pts=[]
    for h in hs:
        q=C[sel&(np.abs(C[:,2]-h)<0.03)]
        if len(q)<5: q=C[sel&(np.abs(C[:,2]-h)<0.08)]
        p=np.median(q,0); p[2]=h; pts.append(p)
    bones=[f'coat_{name}_0{i+1}' for i in range(3)]; prev=ad.edit_bones['coat_Hips']
    for i,bn in enumerate(bones):
        e=ad.edit_bones.new(bn); e.head=Vector(pts[i]); e.tail=Vector(pts[i+1]); e.parent=prev; e.use_connect=i>0
        out=Vector((pts[i][0],pts[i][1]-yc,0)).normalized(); e.align_roll(out); prev=e
    chains.append(dict(name='coat_'+name,sector=name,parent='coat_Hips',bones=bones,joints=[list(map(float,p)) for p in pts],theta=tc,
                       length=float(sum(np.linalg.norm(pts[i+1]-pts[i]) for i in range(3))),verts=int(sel.sum())))
bpy.ops.object.mode_set(mode='OBJECT')
names=['coat_Hips']+[b for c in chains for b in c['bones']]; bi={n:i for i,n in enumerate(names)}
Wt=np.zeros((N,len(names))); z=C[:,2]
def ss(e0,e1,x): t=np.clip((x-e0)/(e1-e0),0,1); return t*t*(3-2*t)
anchor=ss(TOP-0.05,TOP+0.03,z)
Wt[:,0]+=anchor
ang=np.stack([np.clip(1-angd(th,c['theta'])/40.0,0,1) for c in chains],1)
far=ang.sum(1)<1e-6; ang[far,np.argmin(np.stack([angd(th[far],c['theta']) for c in chains],1),1)]=1
ang/=ang.sum(1,keepdims=True)
for k,c in enumerate(chains):
    Jc=np.array(c['joints']); hz=Jc[:,2]; w=np.zeros((N,3))
    for i in range(3):
        lo_=1.0 if i==0 else ss(hz[i]+0.05,hz[i]-0.05,z); hi_=1.0 if i==2 else 1-ss(hz[i+1]+0.05,hz[i+1]-0.05,z); w[:,i]=lo_*hi_
    w/=np.maximum(w.sum(1,keepdims=True),1e-9)
    for i,bn in enumerate(c['bones']): Wt[:,bi[bn]]+=(1-anchor)*ang[:,k]*w[:,i]
Wt[Wt<0.02]=0; order=np.argsort(-Wt,1); keep=np.zeros_like(Wt,bool); np.put_along_axis(keep,order[:,:4],True,1); Wt*=keep
z0=Wt.sum(1)<1e-6; Wt[z0,0]=1; Wt/=Wt.sum(1,keepdims=True)
for n in names:
    vg=coat.vertex_groups.new(name=n); colw=Wt[:,bi[n]]
    for i in np.nonzero(colw>0)[0]: vg.add([int(i)],float(colw[i]),'REPLACE')
coat.parent=crig; md=coat.modifiers.new('Armature','ARMATURE'); md.object=crig
rep.update(chains=len(chains),coat_bones=len(names),max_infl=int((Wt>0).sum(1).max()),anchor_rigid_verts=int((anchor>0.999).sum()),
           chain_info=[(c['name'],c['verts'],round(c['length'],3),round(c['joints'][-1][2],3)) for c in chains])
json.dump(chains,open(W_+'/coat_chains.json','w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=W_+'/rigc.blend',compress=True)
json.dump(rep,open(W_+'/coat.json','w'),indent=1); print('COAT',json.dumps(rep))
