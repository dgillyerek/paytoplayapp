import numpy as np, json, sys
from scipy.spatial import cKDTree
from scipy.spatial.transform import Rotation as Ro
from scipy.optimize import minimize
DUMP=globals().get('DUMP','dumpA.npz')
d=dict(np.load(DUMP)); n=[str(x) for x in d['names']]; R=d['rest']; P=d['pose']
I={k.replace('mixamorig:',''):i for i,k in enumerate(n)}
inv=np.linalg.inv
OFF=d['stm0']  # staff in RightHand frame (head-based pose matrix)
CRY=np.array([0.00277,0.00764,0.525,1.0])
LG=np.array([[0.01,0.05,-0.03],[0.926,-0.327,0.189]])
LC=LG[0]; LU=LG[1]/np.linalg.norm(LG[1])
Y=np.array([0,1,0.])
def swing_twist(Rm,ax):
    q=Ro.from_matrix(Rm).as_quat(); v=q[:3]; p=np.dot(v,ax)*ax; tw=np.array([*p,q[3]]); nt=np.linalg.norm(tw)
    tw=np.array([0,0,0,1.]) if nt<1e-9 else tw/nt
    twr=Ro.from_quat(tw); sw=Ro.from_quat(q)*twr.inv(); return sw.as_rotvec(), np.degrees(np.dot(twr.as_rotvec(),ax))
# hinge axes / wrist axes per side
SIDE={}
for s in ('Right','Left'):
    iS,iA,iF,iH=I[s+'Shoulder'],I[s+'Arm'],I[s+'ForeArm'],I[s+'Hand']
    ud=R[iF][:3,3]-R[iA][:3,3]; fd=R[iH][:3,3]-R[iF][:3,3]; hn=np.cross(ud,fd); hn/=np.linalg.norm(hn)
    if s=='Right':
        sa=OFF[:3,2].copy()
    else: sa=LU.copy()
    sa=sa-np.dot(sa,Y)*Y; FX=sa/np.linalg.norm(sa); SX=np.cross(Y,FX)
    SIDE[s]=dict(idx=(iS,iA,iF,iH),HN=R[iF][:3,:3].T@hn,FX=FX,SX=SX,LA=np.linalg.norm(ud),LF=np.linalg.norm(fd))
def wrist_angles(s,Bh):
    sw,tw=swing_twist(Bh,Y); S=SIDE[s]
    return np.degrees(np.dot(sw,S['FX'])), np.degrees(np.dot(sw,S['SX'])), tw, np.degrees(np.linalg.norm(Ro.from_matrix(Bh).as_rotvec()))
def elbow_angles(s,Bf):
    sw,tw=swing_twist(Bf,Y); HN=SIDE[s]['HN']; return np.degrees(np.dot(sw,HN)), np.degrees(np.linalg.norm(sw-np.dot(sw,HN)*HN)), tw
def fk(f,s,x):
    iS,iA,iF,iH=SIDE[s]['idx']
    Ba=np.eye(4); Ba[:3,:3]=Ro.from_rotvec(x[0:3]).as_matrix()
    Bf=np.eye(4); Bf[:3,:3]=Ro.from_rotvec(x[3:6]).as_matrix()
    Bh=np.eye(4); Bh[:3,:3]=Ro.from_rotvec(x[6:9]).as_matrix()
    Ms=P[f,iS]; Ma=Ms@inv(R[iS])@R[iA]@Ba; Mf=Ma@inv(R[iA])@R[iF]@Bf; Mh=Mf@inv(R[iF])@R[iH]@Bh
    return Ma,Mf,Mh,Ba[:3,:3],Bf[:3,:3],Bh[:3,:3]
sv=d['sv']
zs=np.arange(-1.29,0.16,0.025)
rad=np.array([np.hypot(*sv[(abs(sv[:,2]-z)<0.0125)][:,:2].T).max() if (abs(sv[:,2]-z)<0.0125).any() else 0.03 for z in zs])
head=sv[sv[:,2]>0.15][::12]
SP=np.vstack([np.c_[np.zeros((len(zs),2)),zs],head]); SR=np.r_[rad,np.zeros(len(head))]; SZ=SP[:,2]; SPh=np.c_[SP,np.ones(len(SP))]
def shaft_r(z): return float(np.interp(z,zs,rad))
dom=np.array([s.replace('mixamorig:','') for s in d['dom']])
ARMB=[s+b for s in ('Right','Left') for b in ('Shoulder','Arm','ForeArm','Hand')]
def frame_geom(f):
    bv=d['bv'][f].astype(float); bn=d['bn'][f].astype(float)
    m=~np.isin(dom,ARMB); G={'body':(cKDTree(bv[m]),bv[m],bn[m])}
    for b in ARMB:
        mm=np.where(dom==b)[0][::2]; Mi=inv(P[f,I[b]]); G[b]=((Mi[:3,:3]@bv[mm].T).T+Mi[:3,3],(Mi[:3,:3]@bn[mm].T).T)
    return G
def sdist(tree,pts,nrm,q):
    dd,ii=tree.query(q); s=np.sign(np.einsum('ij,ij->i',q-pts[ii],nrm[ii])); s[s==0]=1; s[dd>0.12]=1; return dd*s, ii
def segdist(p1,q1,p2,q2):
    d1=q1-p1; d2=q2-p2; r=p1-p2; a=d1@d1; e=d2@d2; f_=d2@r; c=d1@r; b=d1@d2; den=a*e-b*b
    s=np.clip((b*f_-c*e)/den if den>1e-9 else 0,0,1); t=np.clip((b*s+f_)/e,0,1); s=np.clip((b*t-c)/a,0,1)
    return np.linalg.norm(p1+d1*s-(p2+d2*t))
CLEAR=0.03
def evaluate(f,G,xr,xl,zL):
    Mr=fk(f,'Right',xr); Ml=fk(f,'Left',xl)
    Ms=Mr[2]@OFF; W=(Ms@SPh.T).T[:,:3]
    tb,pb,nb=G['body']; sb,_=sdist(tb,pb,nb,W)
    V=[];N=[];T=[]
    bm={'RightShoulder':P[f,I['RightShoulder']],'LeftShoulder':P[f,I['LeftShoulder']],'RightArm':Mr[0],'RightForeArm':Mr[1],'RightHand':Mr[2],'LeftArm':Ml[0],'LeftForeArm':Ml[1],'LeftHand':Ml[2]}
    for b in ARMB:
        L,Ln=G[b]; M=bm[b]; V.append((M[:3,:3]@L.T).T+M[:3,3]); N.append((M[:3,:3]@Ln.T).T); T+=[b]*len(L)
    AV=np.vstack(V); AN=np.vstack(N); AT=np.array(T); sa,ia=sdist(cKDTree(AV),AV,AN,W)
    ua=np.abs(sa)<np.abs(sb); sd=np.where(ua,sa,sb)-SR; reg=np.where(ua,AT[ia],'body')
    ex=(np.abs(SZ)<0.10)&ua&np.isin(reg,['RightHand','RightForeArm'])
    sd[ex]=1.0
    nearL=(np.abs(SZ-zL)<0.16)&ua
    req=np.full(len(sd),CLEAR)
    req[nearL&(reg=='LeftHand')]=-0.008
    req[nearL&(reg=='LeftForeArm')]=0.004
    sd=sd-req+CLEAR
    caps=[];cr=[]
    for M in (Mr,Ml):
        for Mb,L,r,u0 in ((M[0],0.31,0.05,0.45),(M[1],0.24,0.042,0.1)):
            for u in np.linspace(u0,1.0,5): caps.append((Mb@np.array([0,L*u,0,1]))[:3]); cr.append(r)
    cs,_=sdist(tb,pb,nb,np.array(caps)); cs=cs-np.array(cr)
    # forearm-forearm / hand separation
    fr=segdist(Mr[1][:3,3],Mr[2][:3,3],Ml[1][:3,3],Ml[2][:3,3])
    return dict(Mr=Mr,Ml=Ml,Ms=Ms,staff=sd,armcap=cs,ff=fr,reg=reg)
def hinge(v,lo,hi): return np.maximum(0,v-hi)**2+np.maximum(0,lo-v)**2
def arm_cost(s,Ma,Mf,Ba,Bf,Bh,info,key):
    J=0
    fl,sd,tw,tot=wrist_angles(s,Bh)
    J+=hinge(tot,0,24)*3+hinge(tw,-12,12)*2+0.002*(fl**2+sd**2+tw**2)
    ef,ec,et=elbow_angles(s,Bf)
    J+=hinge(ec,0,5)*3+hinge(et,-60,60)+0.0004*et**2
    ya=Ma[:3,1]/np.linalg.norm(Ma[:3,1]); yf=Mf[:3,1]/np.linalg.norm(Mf[:3,1]); bend=np.degrees(np.arccos(np.clip(ya@yf,-1,1)))
    J+=hinge(bend,10,135)*2
    sw,atw=swing_twist(Ba,Y); swa=np.degrees(np.linalg.norm(sw))
    J+=hinge(swa,0,info['swmax'])*3+hinge(atw,-info.get('twmax',30),info.get('twmax',30))*2+0.0005*atw**2+0.0006*swa**2
    key[s]=dict(wrist=(fl,sd,tw,tot),elbow=(ef,ec,et,bend),shoulder=(swa,atw))
    return J
def cost(x,f,G,tg,ret=False):
    xr,xl,zL=x[:9],x[9:18],x[18]
    e=evaluate(f,G,xr,xl,zL); Ms=e['Ms']
    c=(Ms@CRY)[:3]; a=Ms[:3,2]
    J=tg['wc']*np.sum(((c-tg['cry'])*np.array([0.4,1,1]))**2)/0.02**2 + tg['wa']*(1-np.dot(a,tg['axis']))/0.003
    key={}
    for s,M in (('Right',e['Mr']),('Left',e['Ml'])):
        J+=arm_cost(s,M[0],M[1],M[3],M[4],M[5],tg,key)
    # left grip
    wl=tg['wl']
    Mh=e['Ml'][2]; gp=(Mh@np.array([*LC,1]))[:3]; gu=Mh[:3,:3]@LU
    Msi=inv(Ms); gl=(Msi@np.array([*gp,1]))[:3]
    gl=gl-tg.get('goff',np.zeros(3))
    radial=np.hypot(gl[0],gl[1]); along=gl[2]-zL; al=abs(np.dot(gu,a))
    grip_err=(radial/0.006)**2+(along/0.01)**2+(1-al)/0.004+hinge(zL,-0.78,-0.2)*1e4
    J+=wl*grip_err
    if wl<1: J+=(1-wl)*np.sum(xl**2)*40
    pen=np.sum(np.maximum(0,CLEAR-e['staff'])**2)+np.sum(np.maximum(0,0.0-e['armcap'])**2)+max(0,0.09-e['ff'])**2
    J+=pen*4e5
    if tg.get('xp') is not None: J+=tg['ws']*np.sum((x[:18]-tg['xp'][:18])**2)*50
    if tg.get('xn') is not None: J+=tg['ws']*np.sum((x[:18]-tg['xn'][:18])**2)*50
    if ret:
        return dict(J=J,cry=c,axis=a,key=key,grip=(radial,along,al),minclear=float(e['staff'].min()),armcap=float(e['armcap'].min()),ff=e['ff'],pen=pen)
    return J
