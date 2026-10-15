import pickle,numpy as np,math,itertools,json
d=pickle.load(open('/workspace/bonequill_gritty/work/fing_dbg.pkl','rb'))
LH,fr,pl,zc,th,P3F,JT=d['LH'],d['fr'],d['pl'],d['zc'],d['thumb'],d['P3F'],d['JT']
def Rax(axis,a):
    axis=np.asarray(axis,float); axis/=np.linalg.norm(axis); K=np.array([[0,-axis[2],axis[1]],[axis[2],0,-axis[0]],[-axis[1],axis[0],0]])
    return np.eye(3)+math.sin(a)*K+(1-math.cos(a))*K@K
def chain_T(joints,rots):
    T=[]; Racc=np.eye(3); tacc=np.zeros(3)
    for j,R in zip(joints,rots):
        jw=Racc@j+tacc; Racc=R@Racc; tacc=jw-Racc@j; T.append((Racc.copy(),tacc.copy()))
    return T
def pose(a,tax,t):
    TF=chain_T(P3F[:3],[Rax((0,0,-1),math.radians(x)) for x in a]); TT=chain_T(JT[:2],[Rax(tax,math.radians(x)) for x in t])
    out=LH*pl[:,None]
    for j,(R,tt) in enumerate(TF+TT): out=out+fr[:,j:j+1]*(LH@R.T+tt)
    return out
zb=np.abs(LH[:,2]-zc)<0.035
gx,gy=np.meshgrid(np.arange(0.03,0.10,0.002),np.arange(0.06,0.16,0.002)); G=np.c_[gx.ravel(),gy.ravel()]
def hole(Q):
    q=Q[zb][:,:2]; dd=np.sqrt(((G[:,None,:]-q[None])**2).sum(-1)).min(1); i=int(np.argmax(dd)); return dd[i],G[i]
def coverage(Q,C,r,band=0.02):
    q=Q[zb][:,:2]-C; dd=np.linalg.norm(q,axis=1); a=np.degrees(np.arctan2(q[dd<r+band,1],q[dd<r+band,0]))%360
    return len(np.unique((a//10).astype(int)))*10
res=[]
for t1,t2 in itertools.product((20,30,40,50),(0,20)):
  for a in itertools.product((30,45,60,75,90),(40,60,80,100),(20,40,60)):
    Q=pose(a,(0,0,-1),(t1,t2)); r,C=hole(Q)
    if r<0.0255: continue
    res.append((coverage(Q,C,r),round(float(r),4),a,t1,t2,C.round(3).tolist()))
res.sort(key=lambda x:(-x[0],abs(x[1]-0.028)))
print(len(res)); [print(x) for x in res[:10]]
