import sys,json,numpy as np
from multiprocessing import Pool
exec(open('key2.py').read().split("Xin=np.load")[0].replace("F=int(sys.argv[1]); TG=json.loads(sys.argv[2]); IN=sys.argv[3]; OUT=sys.argv[4]","F=12; TG=json.loads(sys.argv[1]); IN=sys.argv[2]; OUT=sys.argv[3]"))
exec("def leftfit"+open('key2.py').read().split("def leftfit")[1].split("def run")[0])
X0=np.load(IN)[0]; xr=X0[:9]
def run(seed):
    xl=leftfit(xr,seed)
    f=lambda y: cost(np.r_[xr,y],F,G,tg)
    r=minimize(f,xl,method='L-BFGS-B',options=dict(maxiter=400,eps=1e-5))
    return r.fun,np.r_[xr,r.x]
if __name__=='__main__':
    with Pool(7) as p: res=p.map(run,range(7))
    res.sort(key=lambda r:r[0])
    for fun,x in res[:4]:
        rr=cost(x,F,G,tg,True); print(round(fun,2),np.round(rr['grip'],3),round(rr['minclear'],3),round(rr['armcap'],3),{a:np.round(b,1).tolist() for a,b in rr['key']['Left'].items()})
    np.save(OUT,np.array([r[1] for r in res]))
