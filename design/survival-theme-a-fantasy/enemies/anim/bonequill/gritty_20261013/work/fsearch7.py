import numpy as np,math,itertools,json
from scipy.spatial import cKDTree
exec(open('/workspace/bonequill_gritty/work/fsearch.py').read().split('zb=')[0])
zb=np.abs(LH[:,2]-zc)<0.04; fing=fr[:,:3].sum(1)>0.3; thm=fr[:,3:].sum(1)>0.3
za=np.abs(LH[:,2]-zc)<0.09
gx,gy=np.meshgrid(np.arange(0.0,0.11,0.002),np.arange(-0.01,0.17,0.002)); G=np.c_[gx.ravel(),gy.ravel()]
def covr(v):
    if len(v)==0: return 0
    ang=np.degrees(np.arctan2(v[:,1],v[:,0]))%360; return len(np.unique((ang//10).astype(int)))*10
TH=[((0,0,-1),(t,u)) for t in (35,45,55) for u in (0,15,30)]
res=[]
for a in itertools.product(range(5,41,5),range(5,51,5),range(10,71,10)):
    for tax,tt in TH:
        Q=pose(a,tax,tt); qa=Q[za][:,:2]; tr=cKDTree(qa); dm,_=tr.query(G)
        ok=np.nonzero((dm>=0.0255)&(dm<=0.030))[0]
        if not len(ok): continue
        bestc=None
        for i in ok[::2]:
            idx=tr.query_ball_point(G[i],dm[i]+0.015); c=covr(qa[idx]-G[i])
            if bestc is None or c>bestc[0]: bestc=(c,float(dm[i]),G[i])
        res.append((bestc[0],round(bestc[1],4),a,tax,tt,bestc[2].round(3).tolist()))
res.sort(key=lambda r:(-r[0],abs(r[1]-0.027)))
[print(r) for r in res[:15]]
json.dump(res[:40],open('/workspace/bonequill_gritty/work/fs7.json','w'))
