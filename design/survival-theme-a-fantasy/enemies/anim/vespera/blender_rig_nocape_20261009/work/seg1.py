import numpy as np, colorsys, scipy.sparse as sp, sys
from scipy.sparse.csgraph import connected_components
d=np.load('body0.npz'); F=d['F']; co=d['co']; fc=d['fcol']; cent=co[F].mean(1); nF=len(F)
c=np.load('cls0.npz'); uvl=np.load('uvlab.npy')
s=np.clip(fc,0,1)**(1/2.2)
mx=s.max(1); mn=s.min(1); S=(mx-mn)/np.maximum(mx,1e-6); V=mx
sig=lambda x:1/(1+np.exp(-x))
colc=sig((S-0.30)/0.05)*sig((V-0.45)/0.05)
AX=np.array([[0.85,-0.015,-0.17],[0.95,-0.02,-0.17],[1.10,-0.05,-0.19],[1.30,-0.09,-0.21],[1.45,-0.06,-0.18],[1.60,-0.05,-0.17]])
ax=np.interp(cent[:,2],AX[:,0],AX[:,1]); ay=np.interp(cent[:,2],AX[:,0],AX[:,2])
z=cent[:,2]
band=(z>0.80)&(z<1.47)
front=(cent[:,1]<ay-0.03)&(np.abs(cent[:,0]-ax)<0.17)
outer=(c['res']==1)
arm=c['armd']<0.065
fill=(uvl==26)|(uvl==42)
u=np.where(band&outer&~arm&~front, 2*colc-1, -1.0)
u[fill&band&~front&(z<0.95)]=1.0
u[(z>1.47)|(z<0.80)|arm|front]=-1.5
# face adjacency via shared edges
e=np.concatenate([F[:,[0,1]],F[:,[1,2]],F[:,[2,0]]]); e.sort(1); fid=np.tile(np.arange(nF),3)
key=e[:,0]*200000+e[:,1]; o=np.argsort(key); ks=key[o]; same=ks[1:]==ks[:-1]
a=fid[o][:-1][same]; b=fid[o][1:][same]
A=sp.coo_matrix((np.ones(len(a)),(a,b)),shape=(nF,nF)); A=(A+A.T).tocsr(); deg=np.asarray(A.sum(1)).ravel()
x=u.copy()
for it in range(60):
    x=0.35*u+0.65*(A@x)/np.maximum(deg,1)
lab=x>0.0
# keep components (over cape faces) that contain a seed near the seam
Ac=A[lab][:,lab]; n,cl=connected_components(Ac,directed=False)
idx=np.nonzero(lab)[0]; seed=(z[idx]<0.97)
keep=np.isin(cl,np.unique(cl[seed]))
cape=np.zeros(nF,bool); cape[idx[keep]]=True
print('cape faces',cape.sum(),'comps',n)
np.save('cape1.npy',cape); np.save('lab1.npy',cape.astype(np.int8))
