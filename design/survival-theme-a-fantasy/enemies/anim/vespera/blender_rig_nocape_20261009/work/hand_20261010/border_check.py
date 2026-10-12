import bpy, bmesh, numpy as np, sys, json
me=bpy.data.objects['VESPERA_body'].data
bm=bmesh.new(); bm.from_mesh(me)
if 'is_twin' in bm.verts.layers.int:
    tw=bm.verts.layers.int['is_twin']; asd=bm.verts.layers.int['armside']
    T=lambda v:v[tw]; A=lambda v:v[asd]
else:
    m=np.load('/workspace/vespera-reweight/work/mesh.npz'); twin=m['twin']; tmap=m['tmap']
    a=np.load('/workspace/vespera-hand/work/armside_full.npy')&np.load('/workspace/vespera-hand/work/region.npy')
    src=np.where(twin,tmap,np.arange(len(twin)))
    T=lambda v:twin[v.index]; A=lambda v:a[src[v.index]]
import json as _j
_B={k:[np.array(x) for x in v] for k,v in _j.load(open('/workspace/vespera-hand/work/bones.json')).items()}
_a,_b=_B['RightForeArm']
def low(v):
    c=np.array(v.co); return ((c-_a)@(_b-_a))/((_b-_a)@(_b-_a))>0.45
out={'hand_border_nontwin':0,'hand_border_twin':0,'hand_lowforearm_border_nontwin':0}
pts=[]
for e in bm.edges:
    if len(e.link_faces)!=1: continue
    if not all(A(v) for v in e.verts): continue
    if all(T(v) for v in e.verts): out['hand_border_twin']+=1
    else:
        out['hand_border_nontwin']+=1
        if all(low(v) for v in e.verts): out['hand_lowforearm_border_nontwin']+=1
        pts.append([round(x,3) for x in ((e.verts[0].co+e.verts[1].co)/2)])
out['nontwin_pts']=pts
print('BORDER',json.dumps(out))
