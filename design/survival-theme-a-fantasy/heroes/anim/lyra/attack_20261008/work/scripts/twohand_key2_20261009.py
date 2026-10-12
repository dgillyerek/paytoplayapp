import sys,json,numpy as np
from multiprocessing import Pool
exec(open('opt2.py').read())
F=int(sys.argv[1]); TG=json.loads(sys.argv[2]); IN=sys.argv[3]; OUT=sys.argv[4]
tg=dict(cry=np.array(TG['cry']),axis=np.array(TG['axis'])/np.linalg.norm(TG['axis']),wc=TG.get('wc',1),wa=TG.get('wa',1),wl=1,swmax=TG.get('swmax',50),twmax=TG.get('twmax',30))
G=frame_geom(F)
Xin=np.load(IN)
def leftfit(xr,seed):
    Mr=fk(F,'Right',xr); Ms=Mr[2]@OFF; a=Ms[:3,2]; Msi=inv(Ms)
    def lc(x):
        Ma,Mf,Mh,Ba,Bf,Bh=fk(F,'Left',x[:9]); key={}; J=arm_cost('Left',Ma,Mf,Ba,Bf,Bh,tg,key)
        gp=(Mh@np.array([*LC,1]))[:3]; gu=Mh[:3,:3]@LU; gl=(Msi@np.array([*gp,1]))[:3]
        return J+(np.hypot(gl[0],gl[1])/0.006)**2+((gl[2]-x[9])/0.01)**2+(1-abs(gu@a))/0.004+hinge(x[9],-0.78,-0.2)*1e4
    rng=np.random.default_rng(seed); best=None
    for s in range(25):
        r=minimize(lc,np.r_[rng.normal(scale=0.7,size=9),rng.uniform(-0.7,-0.25)],method='L-BFGS-B',options=dict(maxiter=300))
        if best is None or r.fun<best.fun: best=r
    return best.x
def run(i):
    xr=Xin[i][:9]; xl=leftfit(xr,i)
    x0=np.r_[xr,xl]
    r=minimize(cost,x0,args=(F,G,tg),method='L-BFGS-B',options=dict(maxiter=500,eps=1e-5))
    return i,r.fun,r.x
if __name__=='__main__':
    with Pool(7) as p: res=p.map(run,range(min(7,len(Xin))))
    res.sort(key=lambda r:r[1])
    for s,fun,x in res:
        rr=cost(x,F,G,tg,True); print(s,round(fun,2),np.round(rr['cry'],3),np.round(rr['axis'],2),{k:{a:np.round(b,1).tolist() for a,b in v.items()} for k,v in rr['key'].items()},np.round(rr['grip'],3),round(x[18],3),round(rr['minclear'],3),round(rr['armcap'],3),round(rr['ff'],3))
    np.save(OUT,np.array([r[2] for r in res]))
