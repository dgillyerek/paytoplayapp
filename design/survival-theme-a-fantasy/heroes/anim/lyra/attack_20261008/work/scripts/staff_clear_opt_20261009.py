import numpy as np, json, sys
from scipy.spatial import cKDTree
from scipy.spatial.transform import Rotation as Ro
from scipy.optimize import minimize
d=dict(np.load('dump_before.npz')); n=[str(x) for x in d['names']]; R=d['rest']; P=d['pose']; S=d['stm']
I={k.replace('mixamorig:',''):i for i,k in enumerate(n)}
iRS,iA,iF,iH=I['RightShoulder'],I['RightArm'],I['RightForeArm'],I['RightHand']
OFF=np.linalg.inv(R[iH])@S[0]
CRY=np.array([0.00277,0.00764,0.525,1.0])
inv=np.linalg.inv
def basis(f,i,j): return inv(R[i])@R[j]@inv(P[f,j])@P[f,i]
# elbow hinge axis in forearm local
ud=R[iF][:3,3]-R[iA][:3,3]; fd=R[iH][:3,3]-R[iF][:3,3]
hn=np.cross(ud,fd); hn/=np.linalg.norm(hn); HN=R[iF][:3,:3].T@hn
REST_BEND=np.degrees(np.arccos(np.dot(ud,fd)/np.linalg.norm(ud)/np.linalg.norm(fd)))
# hand axes: flex axis = staff axis orthogonalised against Y
sa=OFF[:3,2].copy(); sa[1]=0; FX=sa/np.linalg.norm(sa); SX=np.cross(np.array([0,1,0.]),FX)
def swing_twist(Rm,ax):
    q=Ro.from_matrix(Rm).as_quat()  # x,y,z,w
    v=q[:3]; p=np.dot(v,ax)*ax; tw=np.array([*p,q[3]]); nt=np.linalg.norm(tw)
    if nt<1e-9: tw=np.array([0,0,0,1.]); 
    else: tw/=nt
    twr=Ro.from_quat(tw); sw=Ro.from_quat(q)*twr.inv()
    ta=twr.as_rotvec(); return sw.as_rotvec(), np.degrees(np.dot(ta,ax))
def wrist_angles(Bh):
    sw,tw=swing_twist(Bh,np.array([0,1,0.]))
    return np.degrees(np.dot(sw,FX)), np.degrees(np.dot(sw,SX)), tw, np.degrees(np.linalg.norm(Ro.from_matrix(Bh).as_rotvec()))
def elbow_angles(Bf):
    sw,tw=swing_twist(Bf,np.array([0,1,0.]))
    return np.degrees(np.dot(sw,HN)), np.degrees(np.linalg.norm(sw-np.dot(sw,HN)*HN)), tw
# staff samples
sv=d['sv']
zs=np.arange(-1.29,0.16,0.025)
rad=np.array([np.hypot(*sv[(abs(sv[:,2]-z)<0.0125)][:,:2].T).max() if (abs(sv[:,2]-z)<0.0125).any() else 0.03 for z in zs])
head=sv[sv[:,2]>0.15][::6]
SP=np.vstack([np.c_[np.zeros((len(zs),2)),zs],head]); SR=np.r_[rad,np.zeros(len(head))]; SZ=SP[:,2]
SPh=np.c_[SP,np.ones(len(SP))]
dom=d['dom']; dom=np.array([s.replace('mixamorig:','') for s in dom])
ARMB=['RightShoulder','RightArm','RightForeArm','RightHand']
def frame_geom(f):
    bv=d['bv'][f].astype(float); bn=d['bn'][f].astype(float)
    m=~np.isin(dom,ARMB); G={'body':(cKDTree(bv[m]),bv[m],bn[m])}
    for b in ['RightShoulder','RightArm','RightForeArm','RightHand']:
        mm=dom==b; Mi=inv(P[f,I[b]]); L=(Mi[:3,:3]@bv[mm].T).T+Mi[:3,3]; Ln=(Mi[:3,:3]@bn[mm].T).T
        G[b]=(L,Ln)
    G['RS']=P[f,iRS]
    return G
def sdist(tree,pts,nrm,q):
    dd,ii=tree.query(q); s=np.sign(np.einsum('ij,ij->i',q-pts[ii],nrm[ii])); s[s==0]=1; s[dd>0.12]=1; return dd*s, ii
def posed_arm(G,Ma,Mf,Mh):
    V=[];N=[];T=[]
    for b,M in (('RightShoulder',G['RS']),('RightArm',Ma),('RightForeArm',Mf),('RightHand',Mh)):
        L,Ln=G[b]; V.append((M[:3,:3]@L.T).T+M[:3,3]); N.append((M[:3,:3]@Ln.T).T); T+= [b]*len(L)
    return np.vstack(V),np.vstack(N),np.array(T)
def fk(f,x):
    Ba=np.eye(4); Ba[:3,:3]=Ro.from_rotvec(x[0:3]).as_matrix(); Ba[:3,3]=basis(f,iA,iRS)[:3,3]
    Bf=np.eye(4); Bf[:3,:3]=Ro.from_rotvec(x[3:6]).as_matrix()
    Bh=np.eye(4); Bh[:3,:3]=Ro.from_rotvec(x[6:9]).as_matrix()
    Ma=P[f,iRS]@inv(R[iRS])@R[iA]@Ba; Mf=Ma@inv(R[iA])@R[iF]@Bf; Mh=Mf@inv(R[iF])@R[iH]@Bh
    return Ma,Mf,Mh,Bf[:3,:3],Bh[:3,:3]
CLEAR=0.03
def collide(f,G,Ma,Mf,Mh,detail=False):
    Ms=Mh@OFF; W=(Ms@SPh.T).T[:,:3]
    out={}
    t,p,nn=G['body']; sb,ib=sdist(t,p,nn,W)
    AV,AN,AT=posed_arm(G,Ma,Mf,Mh); at=cKDTree(AV); sa,ia=sdist(at,AV,AN,W)
    # region of closest
    use_arm=np.abs(sa)<np.abs(sb)
    sd=np.where(use_arm,sa,sb)-SR
    reg=np.where(use_arm,AT[ia],'body')
    grip=(np.abs(SZ)<0.10)&use_arm&np.isin(reg,['RightHand','RightForeArm'])
    sd[grip]=1.0
    out['staff']=sd
    # arm capsules vs body
    caps=[];cr=[]
    for M,L,r in ((Ma,0.32,0.05),(Mf,0.238,0.042)):
        for u in np.linspace(0.45 if L>0.3 else 0.1,1.0,6): caps.append((M@np.array([0,L*u,0,1]))[:3]); cr.append(r)
    caps=np.array(caps); cs,_=sdist(G['body'][0],G['body'][1],G['body'][2],caps); out['armcap']=cs-np.array(cr)
    if detail: out['reg']=reg; out['bodyreg']=ib
    return out
def hinge(v,lo,hi): return np.maximum(0,v-hi)**2+np.maximum(0,lo-v)**2
def cost(x,f,G,tc,ta,xprev,xnext,wt,ret=False):
    Ma,Mf,Mh,Bf,Bh=fk(f,x); Ms=Mh@OFF
    c=(Ms@CRY)[:3]; a=Ms[:3,2]
    J=wt[0]*np.sum((c-tc)**2)/0.02**2 + wt[1]*(1-np.dot(a,ta))/0.003
    fw=-Ms[:3,1].copy(); fw-=np.dot(fw,a)*a; fw/=max(1e-6,np.linalg.norm(fw))
    J+=0.5*(1-fw[1]*-1)  # crescent faces character-forward (-Y)
    fl,sd,tw,tot=wrist_angles(Bh)
    J+=hinge(fl,-28,28)*2+hinge(sd,-16,16)*2+hinge(tw,-12,12)*2+0.002*(fl**2+sd**2+tw**2)
    ef,ec,et=elbow_angles(Bf)
    J+=hinge(ec,0,6)*2+hinge(et,-70,70)*1+0.0005*et**2
    ya=Ma[:3,1]/np.linalg.norm(Ma[:3,1]); yf=Mf[:3,1]/np.linalg.norm(Mf[:3,1]); bend=np.degrees(np.arccos(np.clip(np.dot(ya,yf),-1,1)))
    J+=hinge(bend,8,140)*2
    ra=Ro.from_rotvec(x[0:3]); sw,atw=swing_twist(ra.as_matrix(),np.array([0,1,0.]))
    J+=hinge(atw,-50,50)*1+0.0003*atw**2
    co=collide(f,G,Ma,Mf,Mh)
    pen=0
    for k,v in co.items():
        cl=0.0 if k=='armcap' else CLEAR
        pen+=np.sum(np.maximum(0,cl-v)**2)
    J+=pen*4e5
    if xprev is not None: J+=wt[2]*np.sum((x-xprev)**2)*50
    if xnext is not None: J+=wt[2]*np.sum((x-xnext)**2)*50
    if ret: return dict(J=J,cry=c,axis=a,wrist=(fl,sd,tw,tot),elbow=(ef,ec,et,bend),armtw=atw,minclear={k:float(v.min()) for k,v in co.items()},pen=pen)
    return J

# ---------------- targets ----------------
from scipy.interpolate import PchipInterpolator
def smooth(f,a,b): t=np.clip((f-a)/(b-a),0,1); return t*t*(3-2*t)
def targets(params):
    TC=[];TA=[]
    lk=PchipInterpolator(params['lean_f'],params['lean_v'])
    tk=PchipInterpolator(params['tilt_f'],params['tilt_v'])
    for f in range(31):
        c=(S[f]@CRY)[:3].copy(); a=S[f][:3,2].copy()
        s=smooth(f,0,params['ramp'])*(1-smooth(f,30-params['ramp2'],30))
        c[0]+=params['dx']*s
        a[0]+=lk(f); a[1]*=tk(f); a/=np.linalg.norm(a)
        TC.append(c); TA.append(a)
    return np.array(TC),np.array(TA)
def x_old(f):
    return np.r_[Ro.from_matrix(basis(f,iA,iRS)[:3,:3]).as_rotvec(),Ro.from_matrix(basis(f,iF,iA)[:3,:3]).as_rotvec(),Ro.from_matrix(basis(f,iH,iF)[:3,:3]).as_rotvec()]
