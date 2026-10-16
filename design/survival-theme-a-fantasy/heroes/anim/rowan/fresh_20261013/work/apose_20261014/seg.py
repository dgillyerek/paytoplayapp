import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,numpy as np, scipy.sparse as sp, scipy.sparse.csgraph as cg, json
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/w2/dec_body.blend')
o=bpy.data.objects['ROWAN_body']; me=o.data; N=len(me.vertices)
P=np.load('P_body.npy'); C=np.load('vcol.npy'); x,y,z=P.T; r,g,b=C.T
F=np.empty(len(me.polygons)*3,int); me.polygons.foreach_get('vertices',F); F=F.reshape(-1,3)
E=np.vstack([F[:,[0,1]],F[:,[1,2]],F[:,[2,0]]]); A=sp.coo_matrix((np.ones(len(E)),(E[:,0],E[:,1])),shape=(N,N)); A=((A+A.T)>0).astype(float)
A=A+sp.identity(N); Dg=np.asarray(A.sum(1)).ravel()
bl=((b>r*1.12)&(b>g*1.02)).astype(float)
for i in range(6): bl=(A@bl)/Dg
arm=(np.abs(x)>0.33)&(z>0.80)
coatv=(bl>0.5)&(z<0.975)&~arm
fz=z[F].mean(1); fc=coatv[F].sum(1)>=2
# components among coat faces
fi=np.nonzero(fc)[0]; FE=[]
vf=sp.coo_matrix((np.ones(len(fi)*3),(F[fi].ravel(),np.repeat(np.arange(len(fi)),3))),shape=(N,len(fi))).tocsr()
ff=(vf.T@vf); n,lab=cg.connected_components(ff,directed=False); sz=np.bincount(lab)
print('coat comps',n,sorted(sz)[-8:])
keep=np.zeros(len(F),bool); big=np.nonzero(sz>2000)[0]; keep[fi[np.isin(lab,big)]]=True
# fill small non-coat holes inside coat (faces surrounded): faces with >=2 coat verts and neighbours mostly coat -> already. 
np.save('coat_faces.npy',keep)
cv=np.zeros(N,bool); cv[F[keep].ravel()]=True
print('coat faces',keep.sum(),'verts',cv.sum(),'z',z[cv].min().round(3),z[cv].max().round(3))
json.dump({'coat_faces':int(keep.sum()),'coat_verts':int(cv.sum())},open('seg.json','w'))
