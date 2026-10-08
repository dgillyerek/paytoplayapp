import numpy as np, json
from scipy.spatial import cKDTree
from scipy.sparse.csgraph import connected_components
from scipy.sparse import coo_matrix
co=np.load('lyra/m_verts.npy'); tr=np.load('lyra/m_tris.npy'); n=len(co)
g=json.load(open('lyra/grip2.json')); c1=np.array(g['c1']); nd=np.array(g['c2'])-c1; nd/=np.linalg.norm(nd)
t=(co-c1)@nd; r=np.linalg.norm((co-c1)-np.outer(t,nd),axis=1)
zone=(r<0.10)&(t>0.04)&(t<0.21)
tube=((t<0.17)&(r<0.0255))|((t>=0.17)&(r<0.0275))
blocked=zone&~tube
e=np.concatenate([tr[:,[0,1]],tr[:,[1,2]],tr[:,[2,0]]]); keep=~blocked; ee=e[keep[e[:,0]]&keep[e[:,1]]]
A=coo_matrix((np.ones(len(ee)),(ee[:,0],ee[:,1])),shape=(n,n)); k,lab=connected_components(A,directed=False)
c=json.load(open('lyra/seg_cfg.json')); a=np.array(c['axis_p0']); b=np.array(c['axis_p1'])
f=lambda z: a+(b-a)*(z-a[2])/(b[2]-a[2])
T=cKDTree(co); seeds=[T.query(f(z))[1] for z in (-0.3,0.6)]
comps=set(lab[seeds]); weap=np.isin(lab,list(comps))
s=co[weap]; print('weapon',weap.sum(),s.min(0).round(3),s.max(0).round(3), 'comps',[(lab==q).sum() for q in comps])
np.save('lyra/mask_weapon.npy',weap)
json.dump(dict(c,grip_c1=c1.tolist(),grip_dir=nd.tolist()),open('lyra/seg_cfg.json','w'),indent=1)
