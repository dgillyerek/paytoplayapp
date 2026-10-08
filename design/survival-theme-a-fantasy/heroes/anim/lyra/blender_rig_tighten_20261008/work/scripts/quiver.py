import numpy as np
SEGS=[(np.array([0.02,0.225,0.88]),np.array([0.29,0.205,1.58]),0.058),(np.array([0.29,0.205,1.58]),np.array([0.41,0.175,1.80]),0.075)]
def segd(P,a,b):
    d=b-a; L=np.linalg.norm(d); d=d/L; s=np.clip((P-a)@d,0,L); return np.linalg.norm(P-(a+np.outer(s,d)),axis=1)
def quiver_weight(co, falloff=0.02):
    w=np.zeros(len(co))
    for a,b,r in SEGS:
        d=segd(co,a,b); x=np.clip(1-(d-r)/falloff,0,1); w=np.maximum(w,x*x*(3-2*x))
    return w
