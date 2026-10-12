import sys,json,numpy as np
from scipy.spatial.transform import Rotation as Ro, Slerp
K=np.load(sys.argv[1])[0]; PAR=json.loads(sys.argv[2]); OUT=sys.argv[3]
def ss(t): t=min(1,max(0,t)); return t*t*(3-2*t)
X=np.zeros((31,19)); X[:,18]=K[18]
for f in range(31):
    for j in range(6):
        if j<3: a,b,c,dd=PAR['R']  # right arm envelope
        else: a,b,c,dd=PAR['L']
        s=ss((f-a)/(b-a))*(1-ss((f-c)/(dd-c)))
        q=Ro.from_rotvec([np.zeros(3),K[3*j:3*j+3]]); X[f,3*j:3*j+3]=Slerp([0,1],q)(s).as_rotvec()
np.save(OUT,X); print('ok')
