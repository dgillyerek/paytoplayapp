import numpy as np, json, collections
from scipy.spatial import cKDTree
dd=json.load(open('/workspace/vespera-derek-rig/work/d_derek.json'))
G=[g.split(':')[1] for g in dd['objects']['VESPERA_body']['vgroups']]
W0=np.load('/workspace/vespera-derek-rig/work/d_derek_VESPERA_body_W.npy')[:65195]; C0=np.load('/workspace/vespera-derek-rig/work/d_derek_VESPERA_body_co.npy')[:65195]
d=np.load('weights.npz'); X=d['X']; names=list(d['names']); m=np.load('mesh.npz'); P=m['co']; twin=m['twin']
oi=np.nonzero(~twin)[0]; dist,j=cKDTree(P[oi]).query(C0); nid=oi[j]; assert dist.max()<1e-6
dead=['Hips','Spine','LeftShoulder','RightShoulder','LeftToeBase','RightToeBase']; rep={}
for b in dead:
    gi=G.index(b); full=W0[:,gi]>0.5; mass=X[nid[full]].sum(0)/max(full.sum(),1)
    rep[b]={'verts_mostly_on_it':int(full.sum()),'verts_touching':int((W0[:,gi]>0.01).sum()),'z':[round(float(C0[full,2].min()),2),round(float(C0[full,2].max()),2)],
            'now':{names[k]:round(float(mass[k]),3) for k in np.argsort(-mass)[:5] if mass[k]>0.01}}
live=[i for i,g in enumerate(G) if g not in dead]; orph=W0[:,live].sum(1)<1e-6
rep['fully_orphaned_before']=int(orph.sum())
rep['fully_orphaned_now_main_bone']=dict(collections.Counter(names[k] for k in X[nid[orph]].argmax(1)).most_common())
infl=(X[~twin]>0).sum(1); rep['influences_per_vertex']={str(k):int((infl==k).sum()) for k in range(1,5)}
print(json.dumps(rep)); json.dump(rep,open('orphan_moved.json','w'),indent=1)
