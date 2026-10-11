import numpy as np, json
from scipy.spatial import cKDTree
from scipy.sparse.csgraph import connected_components, dijkstra
from scipy.sparse import coo_matrix
co=np.load('rowan/m_verts.npy'); tr=np.load('rowan/m_tris.npy'); n=len(co)
c=json.load(open('rowan/seg_cfg.json'))
def segdist(P,a,b):
    d=b-a; L=np.linalg.norm(d); d=d/L; s=np.clip((P-a)@d,0,L); return np.linalg.norm(P-(a+np.outer(s,d)),axis=1)
dstr=segdist(co,np.array(c['string_p0']),np.array(c['string_p1']))
dgrip=segdist(co,np.array(c['grip_p0']),np.array(c['grip_p1']))
ga=np.array(c['grip_p0']); gb=np.array(c['grip_p1']); gd=(gb-ga)/np.linalg.norm(gb-ga); gt=(co-ga)@gd
gr=np.linalg.norm((co-ga)-np.outer(gt,gd),axis=1); HANDCYL=(gr<0.11)&(gt>0.08)&(gt<0.36)
sa=np.array(c['string_p0']); sb=np.array(c['string_p1']); sd=(sb-sa)/np.linalg.norm(sb-sa); st=(co-sa)@sd
sz=sa+np.outer(st,sd); STRZONE0=(dstr<0.06)&(sz[:,2]>-0.10)&(sz[:,2]<0.16)
g2=json.load(open('rowan/grip2.json')); c1=np.array(g2['c1']); nd=np.array(g2['c2'])-c1; nd/=np.linalg.norm(nd)
t2=(co-c1)@nd; r2=np.linalg.norm((co-c1)-np.outer(t2,nd),axis=1)
fn=np.cross(co[tr[:,1]]-co[tr[:,0]],co[tr[:,2]]-co[tr[:,0]]); vn=np.zeros_like(co)
for k in range(3): np.add.at(vn,tr[:,k],fn)
vn/=np.linalg.norm(vn,axis=1,keepdims=True)+1e-12
sp_=sa+np.outer(st,sd); rad_=co-sp_; rad_/=np.linalg.norm(rad_,axis=1,keepdims=True)+1e-12; al_=(vn*rad_).sum(1)
STRV=np.where(STRZONE0,(dstr<0.0135)&(al_>0.3),dstr<0.011)
for zz in np.arange(-0.13,0.19,0.002):
    mm=np.where((co[:,2]>=zz)&(co[:,2]<zz+0.002)&(dstr<0.03))[0]
    if len(mm)<3: continue
    q=co[mm]; Tq=cKDTree(q[:,:2]); pq=Tq.query_pairs(0.004,output_type='ndarray')
    Aq=coo_matrix((np.ones(len(pq)),(pq[:,0],pq[:,1])),shape=(len(q),len(q))); _,lq=connected_components(Aq,directed=False)
    Lz=sa+sd*((zz+0.001-sa[2])/sd[2])
    for cc_ in np.unique(lq):
        qq=q[lq==cc_]; cen_=0.5*(qq.min(0)+qq.max(0))
        if np.linalg.norm(cen_[:2]-Lz[:2])<0.010 and np.ptp(qq[:,0])<0.016 and np.ptp(qq[:,1])<0.016: STRV[mm[lq==cc_]]=True
above=((t2>0.19)|(t2<0.028))&(r2<0.055)
slab=(t2>0.148)&(t2<=0.19)&((r2<0.037)|((r2<0.05)&(np.abs(vn@nd)<0.55)))
e_=np.concatenate([tr[:,[0,1]],tr[:,[1,2]],tr[:,[2,0]]]); ok=above|slab; e_=e_[ok[e_[:,0]]&ok[e_[:,1]]]
A_=coo_matrix((np.ones(len(e_)),(e_[:,0],e_[:,1])),shape=(n,n)); _,lb=connected_components(A_,directed=False)
alab=set(np.unique(lb[above&(t2>0.19)&(t2<0.22)]))
slabbow=slab&np.isin(lb,list(alab))
print('slab cand',slab.sum(),'-> bow',slabbow.sum())
tube=above|slabbow|STRV
sa=np.array(c['string_p0']); sb=np.array(c['string_p1']); sd=(sb-sa)/np.linalg.norm(sb-sa); st=(co-sa)@sd
sz=sa+np.outer(st,sd); STRZONE=(dstr<0.06)&(sz[:,2]>-0.10)&(sz[:,2]<0.16)
hand=HANDCYL|STRZONE
contacts=[np.array([-0.222,0.085,-0.73])]
cz=np.zeros(n,bool)
for p in contacts: cz|=np.linalg.norm(co-p,axis=1)<0.022
blocked=(hand&~tube)|cz
e=np.concatenate([tr[:,[0,1]],tr[:,[1,2]],tr[:,[2,0]]])
keep=~blocked; ee=e[keep[e[:,0]]&keep[e[:,1]]]
A=coo_matrix((np.ones(len(ee)),(ee[:,0],ee[:,1])),shape=(n,n)); k,lab=connected_components(A,directed=False)
T=cKDTree(co); seeds=[T.query(s)[1] for s in ([-0.475,-0.15,0.40],[-0.40,-0.065,-0.30])]
comps=set(lab[seeds]); weap=np.isin(lab,list(comps))
s=co[weap]; print('weapon',weap.sum(),s.min(0).round(3),s.max(0).round(3))
body_lab=np.bincount(lab[~blocked&~weap]).argmax(); body=lab==body_lab
print('body',body.sum(),'other',(~weap&~body&~blocked).sum())
# assign contact-zone verts to nearest (graph) side
cidx=np.where(cz)[0]
if len(cidx):
    reg=cz.copy()
    # neighbourhood ring
    for _ in range(2):
        nb=np.zeros(n,bool); m=reg[e[:,0]]|reg[e[:,1]]; nb[e[m].ravel()]=True; reg|=nb
    ids=np.where(reg)[0]; mp=-np.ones(n,int); mp[ids]=np.arange(len(ids))
    er=e[reg[e[:,0]]&reg[e[:,1]]]; w=np.linalg.norm(co[er[:,0]]-co[er[:,1]],axis=1)
    G=coo_matrix((w,(mp[er[:,0]],mp[er[:,1]])),shape=(len(ids),len(ids))).tocsr()
    src_w=mp[np.where(reg&~cz&weap)[0]]; src_b=mp[np.where(reg&~cz&body)[0]]
    dw=dijkstra(G,directed=False,indices=src_w,min_only=True); db=dijkstra(G,directed=False,indices=src_b,min_only=True)
    to_w=ids[(dw<db)&cz[ids]]; weap[to_w]=True
    print('contact verts',len(cidx),'-> weapon',len(to_w))
# hand zone: tube verts that are connected already included. String
strv=weap&STRV
np.save('rowan/mask_weapon.npy',weap); np.save('rowan/mask_string.npy',strv)
print('string',strv.sum())
