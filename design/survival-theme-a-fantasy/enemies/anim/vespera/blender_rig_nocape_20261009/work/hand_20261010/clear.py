"""Per-frame clearance of the right forearm/hand piece against the rest of the body (triangle overlap + nearest distance)."""
import bpy, numpy as np, json, sys
from mathutils.bvhtree import BVHTree
ACT=sys.argv[sys.argv.index('--')+1]; OUT=sys.argv[-1]
o=bpy.data.objects['VESPERA_body']; me=o.data; a=bpy.data.objects['VESPERA_rig']; sc=bpy.context.scene
N=len(me.vertices)
AS=np.zeros(N,np.int32); me.attributes['armside'].data.foreach_get('value',AS); AS=AS.astype(bool)
TW=np.zeros(N,np.int32); me.attributes['is_twin'].data.foreach_get('value',TW); TW=TW.astype(bool)
m=np.load('/workspace/vespera-hand/work/mesh.npz'); hair=m['hair']
names=[g.name.split(':')[1] for g in o.vertex_groups]; W=np.zeros((N,len(names)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
rarm=np.isin(np.array(names)[W.argmax(1)],['RightArm','RightForeArm','RightHand'])
F=[list(p.vertices) for p in me.polygons]
body_f=[f for f in F if not (AS[f].any() or rarm[f].any() or TW[f].all() or hair[f].any())]
hair_f=[f for f in F if hair[f].all() and not (AS[f].any() or rarm[f].any())]
hv=np.nonzero(AS&~TW)[0]
import json as _j
_B={k:[np.array(x) for x in v] for k,v in _j.load(open('/workspace/vespera-hand/work/bones.json')).items()}
_a,_b=_B['RightForeArm']; _co=np.array([v.co[:] for v in me.vertices]); _t=((_co-_a)@(_b-_a))/((_b-_a)@(_b-_a))
lowmask=(_t[hv]>0.45)
fa=AS&(_t>0.05)
hand_f=[f for f in F if fa[f].all() and not TW[f].all()]
hv=np.nonzero(fa&~TW)[0]; lowmask=(_t[hv]>0.45)     # hand + lower forearm (the upper forearm sits against the flank at the elbow by design)
def coords():
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); mm=ob.to_mesh(); c=np.empty(N*3); mm.vertices.foreach_get('co',c); ob.to_mesh_clear(); return c.reshape(-1,3)
a.animation_data.action=bpy.data.actions[ACT]; rep=[]
for f in range(31):
    sc.frame_set(f); c=coords(); cl=c.tolist()
    Bh=BVHTree.FromPolygons(cl,hand_f); Bb=BVHTree.FromPolygons(cl,body_f); Bhair=BVHTree.FromPolygons(cl,hair_f)
    ov=Bh.overlap(Bb); ovh=Bh.overlap(Bhair)
    d=np.array([Bb.find_nearest(c[i])[3] for i in hv]); dh=np.array([(Bhair.find_nearest(c[i])[3] or 9) for i in hv])
    ctr=[]
    for hi,bi_ in ov[:400]:
        ctr.append(np.mean([c[k] for k in body_f[bi_]],0))
    where_=np.round(np.mean(ctr,0),3).tolist() if ctr else None
    rep.append(dict(f=f,overlap_body_centroid=where_,body_tri_overlaps=len(ov),hair_tri_overlaps=len(ovh),min_body_cm=round(float(d.min())*100,1),min_hair_cm=round(float(dh.min())*100,1),
                    hand_verts_within_3cm=int((d<0.03).sum()),min_body_hand_lowforearm_cm=round(float(d[lowmask].min())*100,1),min_hair_hand_lowforearm_cm=round(float(dh[lowmask].min())*100,1)))
json.dump(rep,open(OUT,'w'),indent=0)
for r in rep: print('CLR',json.dumps(r))
