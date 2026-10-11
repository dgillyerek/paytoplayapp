"""Freed right hand QC on the final blend: closed hand, no duplicate surface verts, clean weights either side of the cut."""
import bpy, bmesh, numpy as np, json, sys
from mathutils import kdtree
OUT=sys.argv[-1]
o=bpy.data.objects['VESPERA_body']; me=o.data; N=len(me.vertices)
AS=np.zeros(N,np.int32); me.attributes['armside'].data.foreach_get('value',AS); AS=AS.astype(bool)
TW=np.zeros(N,np.int32); me.attributes['is_twin'].data.foreach_get('value',TW); TW=TW.astype(bool)
co=np.array([v.co[:] for v in me.vertices])
B={k:[np.array(x) for x in v] for k,v in json.load(open('/workspace/vespera-hand/work/bones.json')).items()}
a,b=B['RightForeArm']; t=((co-a)@(b-a))/((b-a)@(b-a))
names=[g.name.split(':')[1] for g in o.vertex_groups]; W=np.zeros((N,len(names)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
R=[names.index(k) for k in ('RightArm','RightForeArm','RightHand')]
other=np.ones(len(names),bool); other[R]=False
h0,h1=B['RightHand']; 
def segd(X,p,q):
    ab=q-p; s=np.clip(((X-p)@ab)/(ab@ab),0,1); return np.linalg.norm(X-(p+s[:,None]*ab),axis=1)
near=(~AS)&(np.minimum(segd(co,h0,h1+(h1-h0)*0.5),segd(co,a,b))<0.16)&(t>0.35)
rep={}
rep['hand_piece_verts']=int((AS&~TW).sum())
rep['hand_piece_max_nonarm_weight']=round(float(W[AS][:,other].max()),4)
low=AS&(t>=0.35)
rep['hand_forearm_max_upperarm_weight']=round(float(W[low,names.index('RightArm')].max()),4)
rep['body_near_hand_verts']=int(near.sum())
rep['body_near_hand_max_rightarm_weight']=round(float(W[near][:,R].max()),4)
bm=bmesh.new(); bm.from_mesh(me); bm.verts.ensure_lookup_table()
rep['hand_lowforearm_open_edges']=sum(1 for e in bm.edges if len(e.link_faces)==1 and all(AS[v.index] and not TW[v.index] and t[v.index]>0.45 for v in e.verts))
idx=np.nonzero(~TW)[0]; kd=kdtree.KDTree(len(idx))
for i in idx: kd.insert(co[i],int(i))
kd.balance(); dup=0
for i in idx:
    for (_,j,_) in kd.find_range(co[i],1e-6):
        if j>i: dup+=1
rep['coincident_surface_pairs']=dup
rep['max_influences']=int(max(len(v.groups) for v in me.vertices)); rep['unweighted']=int(sum(1 for v in me.vertices if not v.groups))
json.dump(rep,open(OUT,'w'),indent=1); print('QCH',json.dumps(rep))
