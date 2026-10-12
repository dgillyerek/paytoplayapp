"""Oakenshield 2026-10-11 on Derek's blend (329ae0c): bones untouched.
1. ToeBase bones were deleted by Derek: their vertex groups move onto the Foot groups (else the toe claws freeze).
2. Hip/thigh blend: the Hips:UpLeg ratio is smoothed over the body skin around each hip (rigid pieces keep their one bone),
   so the thigh does not fold on a hard line.
3. Web faces: 46 source faces with an edge over 12 cm (a sheet under the skirt and a web between the thighs, spanning
   hip-to-thigh weights) are removed; they sheared into a hard line when the right thigh moved."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, bmesh, json, numpy as np, scipy.sparse as sp, scipy.sparse.csgraph as cg
from scipy.spatial import cKDTree
a=sys.argv[sys.argv.index('--')+1:]; OUT,REP=a[0],a[1]
o=bpy.data.objects['OAKENSHIELD_body']; me=o.data; arm=bpy.data.objects['OAKENSHIELD_rig']
rep={'derek_saved_mode':arm.mode}
# Derek saved with the rig in Edit Mode, which stops the pose from deforming the mesh: back to Object Mode (bones unchanged)
bpy.context.view_layer.objects.active=arm
if arm.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
N=len(me.vertices); co=np.array([v.co[:] for v in me.vertices]); G=[g.name for g in o.vertex_groups]
W=np.zeros((N,len(G)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
bones=set(b.name for b in arm.data.bones)
# 1. toe groups -> foot
for s in ('Left','Right'):
    t,f=G.index(f'mixamorig:{s}ToeBase'),G.index(f'mixamorig:{s}Foot')
    rep[f'{s}ToeBase_verts_moved_to_Foot']=int((W[:,t]>0).sum()); W[:,f]+=W[:,t]; W[:,t]=0
orphan=[g for g in G if g.startswith('mixamorig:') and g not in bones]; rep['groups_without_bone']=orphan
# 2. hip ratio smoothing
E=np.array([e.vertices[:] for e in me.edges])
rig=np.load('rig_rigid.npy'); rigid=np.zeros(N,bool); rigid[:len(rig)]=rig
n,lab=cg.connected_components(sp.coo_matrix((np.ones(len(E)),(E[:,0],E[:,1])),shape=(N,N)),directed=False)
main=lab==np.bincount(lab).argmax()
ih=G.index('mixamorig:Hips'); RAD=0.18; SMOOTHED=np.zeros(N,bool)
for s in ('Right','Left'):
    iu=G.index(f'mixamorig:{s}UpLeg'); j=np.array(arm.data.bones[f'mixamorig:{s}UpLeg'].head_local)
    S=W[:,ih]+W[:,iu]
    dom=main&~rigid&(np.linalg.norm(co-j,axis=1)<RAD)&(S>0.05)
    r=np.where(S>0,W[:,iu]/np.maximum(S,1e-9),0.0); r0=r.copy()
    m=dom[E[:,0]]&dom[E[:,1]]; ee=E[m]; L=np.linalg.norm(co[ee[:,0]]-co[ee[:,1]],axis=1); w=1/np.maximum(L,1e-3)
    A=sp.coo_matrix((np.r_[w,w],(np.r_[ee[:,0],ee[:,1]],np.r_[ee[:,1],ee[:,0]])),shape=(N,N)).tocsr(); deg=np.asarray(A.sum(1)).ravel()
    idx=np.nonzero(dom&(deg>0))[0]
    # fade: full smoothing near the joint, none at the region edge
    fade=np.clip(1-(np.linalg.norm(co-j,axis=1)-0.10)/0.08,0,1)
    for it in range(60):
        nr=(A@r)/np.maximum(deg,1e-9); r[idx]=r[idx]+0.6*fade[idx]*(nr[idx]-r[idx])
    W[idx,iu]=S[idx]*r[idx]; W[idx,ih]=S[idx]*(1-r[idx]); SMOOTHED[idx[np.abs(r-r0)[idx]>1e-3]]=True
    rep[f'{s}_hip_smoothed_verts']=int(len(idx)); rep[f'{s}_hip_ratio_change_max']=round(float(np.abs(r-r0)[idx].max()),3)
# twins/duplicates off the main island follow the coincident main vertex
tree=cKDTree(co[main]); mi=np.nonzero(main)[0]
jl=np.array(arm.data.bones['mixamorig:LeftUpLeg'].head_local); jr=np.array(arm.data.bones['mixamorig:RightUpLeg'].head_local)
hipzone=(np.linalg.norm(co-jl,axis=1)<RAD)|(np.linalg.norm(co-jr,axis=1)<RAD)
off=np.nonzero(~main&hipzone)[0]; d,jj=tree.query(co[off],k=2)
src=mi[jj[:,0]]
hit=(d[:,0]<1e-6)&(d[:,1]>1e-6)&SMOOTHED[src]   # unique coincident source whose hip weights were smoothed (not the cut rims)
W[off[hit]]=W[src[hit]]; rep['coincident_offisland_copied']=int(hit.sum())
# max 4, normalize
W[W<0.01]=0; order=np.argsort(-W,1); keep=np.zeros_like(W,bool); np.put_along_axis(keep,order[:,:4],True,1); W*=keep
dg=[k for k,g in enumerate(G) if g.startswith('mixamorig:')]; Ssum=W[:,dg].sum(1,keepdims=True); W[:,dg]/=np.maximum(Ssum,1e-9)
for k in dg:
    g=o.vertex_groups[G[k]]; g.remove(list(range(N)))
    nz=np.nonzero(W[:,k]>0)[0]
    for i in nz: g.add([int(i)],float(W[i,k]),'REPLACE')
for s in ('Left','Right'): o.vertex_groups.remove(o.vertex_groups[f'mixamorig:{s}ToeBase'])
# 3. web faces
bm=bmesh.new(); bm.from_mesh(me)
web=[f for f in bm.faces if max(e.calc_length() for e in f.edges)>0.12]
rep['web_faces_removed']=len(web); rep['web_face_centres_z']=sorted(round(f.calc_center_median().z,2) for f in web)
bmesh.ops.delete(bm,geom=web,context='FACES_ONLY')
lose=[e for e in bm.edges if not e.link_faces]; bmesh.ops.delete(bm,geom=lose,context='EDGES')
lv=[v for v in bm.verts if not v.link_edges]; rep['loose_verts_removed']=len(lv); bmesh.ops.delete(bm,geom=lv,context='VERTS')
bm.to_mesh(me); me.update(); bm.free()
rep['verts']=len(me.vertices); rep['faces']=len(me.polygons)
mx=max(sum(1 for g in v.groups if o.vertex_groups[g.group].name.startswith('mixamorig:')) for v in me.vertices)
un=sum(1 for v in me.vertices if sum(g.weight for g in v.groups if o.vertex_groups[g.group].name.startswith('mixamorig:'))<0.999)
rep['max_influences']=mx; rep['unweighted']=un
bpy.ops.wm.save_as_mainfile(filepath=OUT,compress=True)
json.dump(rep,open(REP,'w'),indent=1); print('PREP2',json.dumps(rep))
