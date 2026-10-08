"""Joint override used by the per-character hum_analyze wrappers (2026-10-08).
Shared analysis is kept for hips/spine; face direction, arms, neck/head and legs are replaced because the
shared heuristics mis-detect on these meshes (long hair -> face_forward +1 -> toes backwards; quiver / staff-arm
pull the arm PCA; symmetric leg mirroring)."""
import numpy as np
def segd(P,a,b):
    a=np.asarray(a,float); b=np.asarray(b,float); d=b-a; L=np.linalg.norm(d); d=d/L
    s=np.clip((P-a)@d,0,L); return np.linalg.norm(P-(a+np.outer(s,d)),axis=1)
def legs(co, J, arms, ff=-1.0):
    H=J['H']; z0=J['z0']; hz=J['hips_z']
    excl=np.zeros(len(co),bool)
    for A in arms.values():
        excl|=segd(co,A['elbow'],A['wrist'])<0.08; excl|=segd(co,A['wrist'],A['hand'])<0.09
    out={}
    for side,sign in (('L',1.0),('R',-1.0)):
        m=(co[:,2]<hz-0.01*H)&(co[:,2]>z0+0.01*H)&(sign*co[:,0]>0.02)&~excl
        def nar(za,zb,n,hw):
            best=None
            for z in np.linspace(z0+za*H,z0+zb*H,n):
                q=co[m&(np.abs(co[:,2]-z)<hw*H)]
                if len(q)<10: continue
                w=np.ptp(q[:,0])
                c=np.array([0.5*(q[:,0].min()+q[:,0].max()),0.5*(q[:,1].min()+q[:,1].max()),z])
                if best is None or w<best[0]: best=(w,c)
            return best[1]
        def at(z,hw):
            q=co[m&(np.abs(co[:,2]-z)<hw)]
            return np.array([0.5*(q[:,0].min()+q[:,0].max()),0.5*(q[:,1].min()+q[:,1].max()),z])
        kz=0.5*(J['L']['knee'][2]+J['R']['knee'][2]); az=0.5*(J['L']['ankle'][2]+J['R']['ankle'][2])
        knee=at(kz,0.01); ankle=at(az,0.008)
        q=co[m&(co[:,2]>hz-0.12*H)&(co[:,2]<hz-0.05*H)&(np.abs(co[:,0])<0.17*H)]
        x10,x90=np.percentile(q[:,0],[10,90]); y10,y90=np.percentile(q[:,1],[10,90])
        up=np.array([0.5*(x10+x90)*0.85, 0.5*(y10+y90), hz-0.015*H])
        fm=m&(co[:,2]<ankle[2]); fv=co[fm]
        tip=fv[np.argmax(fv[:,1]*ff)].copy(); tip[0]=0.5*(tip[0]+ankle[0])
        toe=ankle+0.6*(tip-ankle); toe[2]=z0+0.02*H; tip[2]=z0+0.012*H
        out[side]=dict(upleg=up,knee=knee,ankle=ankle,toe=toe,toe_tip=tip)
    return out
def apply(J, co, arms, neck_head=None, ff=-1.0):
    J=dict(J); J['face_forward']=ff
    P={k:{kk:np.asarray(vv,float) for kk,vv in v.items()} for k,v in arms.items()}
    J['LA']=P['LA']; J['RA']=P['RA']
    if neck_head:
        for k,v in neck_head.items(): J[k]=np.asarray(v,float)
    L=legs(co,J,P,ff); J['L']=L['L']; J['R']=L['R']
    return J
