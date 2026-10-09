import sys, json, numpy as np
from multiprocessing import Pool
exec(open('/workspace/lyra-attack-staff/work/opt.py').read())
PAR=json.load(open(sys.argv[1])); OUT=sys.argv[2]
TC,TA=targets(PAR)
_G={}
def G_(f):
    if f not in _G: _G[f]=frame_geom(f)
    return _G[f]
def solve(args):
    f,x0,xp,xn,wt=args
    G=G_(f)
    r=minimize(cost,x0,args=(f,G,TC[f],TA[f],xp,xn,wt),method='L-BFGS-B',options=dict(maxiter=PAR.get('maxiter',300),eps=1e-5))
    x=r.x
    return f,x
if __name__=='__main__':
    X={f:(x_old(f) if f in (0,30) else np.zeros(9)) for f in range(31)}
    if len(sys.argv)>3: 
        X0=np.load(sys.argv[3]); X={f:X0[f] for f in range(31)}
    wt=tuple(PAR.get('wt',[1,1,0.02]))
    with Pool(7) as pool:
        # pass 1: independent (warm start from old / given)
        for sweep in range(PAR.get('sweeps',3)):
            jobs=[]
            for f in range(1,30):
                sm=sweep>0 or len(sys.argv)>3
                xp=X[f-1] if sm else None; xn=X[f+1] if sm else None
                jobs.append((f,X[f],xp,xn,wt if sm else (wt[0],wt[1],0)))
            for f,x in pool.map(solve,jobs): X[f]=x
            print('sweep',sweep,'done',flush=True)
    A=np.array([X[f] for f in range(31)]); np.save(OUT,A)
    rep=[]
    for f in range(31):
        r=cost(A[f],f,G_(f),TC[f],TA[f],None,None,wt,True)
        rep.append(dict(f=f,cry=np.round(r['cry'],3).tolist(),tcry=np.round(TC[f],3).tolist(),axis_err=round(float(np.degrees(np.arccos(np.clip(np.dot(r['axis'],TA[f]),-1,1)))),1),
            wrist=np.round(r['wrist'],1).tolist(),elbow=np.round(r['elbow'],1).tolist(),armtw=round(float(r['armtw']),1),clear={k:round(v,3) for k,v in r['minclear'].items()}))
        print(rep[-1],flush=True)
    json.dump(rep,open(OUT.replace('.npy','.json'),'w'),indent=0)
