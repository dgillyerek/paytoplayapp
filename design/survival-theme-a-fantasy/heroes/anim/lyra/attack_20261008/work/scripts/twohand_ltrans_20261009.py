import sys,json,numpy as np
from multiprocessing import Pool
exec(open('opt2.py').read())
X=np.load('X4.npy'); K=np.load('key12g.npy')[0]
TG=dict(cry=np.zeros(3),axis=np.array([0,0,1.]),wc=0,wa=0,wl=1,swmax=35,twmax=8)
# palm direction at key, in staff local
G12=frame_geom(12); e=evaluate(12,G12,K[:9],K[9:18],K[18]); Msi=inv(e['Mr'][2]@OFF); Mh=e['Ml'][2]
ho=(Msi@np.array([*Mh[:3,3],1]))[:3]; gc=(Msi@np.array([*(Mh@np.array([*LC,1]))[:3],1]))[:3]
pdir=ho-gc; pdir[2]=0; pdir/=np.linalg.norm(pdir); print('palmdir',pdir)
OFFS={5:0.07,6:0.05,7:0.03,21:0.025,22:0.045,23:0.07}
def run(f):
    G=frame_geom(f); tg=dict(TG); tg['goff']=pdir*OFFS[f]+np.array([0,0,K[18]])*0  # zL handled by x[18]
    xr=X[f][:9]
    fun=lambda y: cost(np.r_[xr,y,K[18]],f,G,tg)+np.sum((y-X[f][9:18])**2)*5
    r=minimize(fun,X[f][9:18],method='L-BFGS-B',options=dict(maxiter=400,eps=1e-5))
    x=np.r_[xr,r.x,K[18]]; rr=cost(x,f,G,tg,True)
    return f,x,rr['grip'],rr['minclear'],rr['armcap'],rr['key']['Left']
if __name__=='__main__':
    with Pool(6) as p: res=p.map(run,list(OFFS))
    for f,x,g,mc,ac,k in res:
        X[f]=x; print(f,np.round(g,3),round(mc,3),round(ac,3),{a:np.round(b,1).tolist() for a,b in k.items()})
    np.save('X5.npy',X)
