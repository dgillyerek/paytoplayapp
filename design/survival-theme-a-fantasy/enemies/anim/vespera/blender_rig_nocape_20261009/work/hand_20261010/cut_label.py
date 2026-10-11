"""Min-cut labelling: which surface belongs to the right forearm/hand and which to the skirt/belt/hip it was fused to."""
import numpy as np, json, scipy.sparse as sp
from scipy.sparse.csgraph import maximum_flow
d=np.load('/workspace/vespera-reweight/work/weights.npz'); m=np.load('/workspace/vespera-reweight/work/mesh.npz')
P=m['co']; E=m['E']; twin=m['twin']; part=d['part']; N=len(P)
B={k:(np.array(v[0]),np.array(v[1])) for k,v in json.load(open('/workspace/vespera-reweight/work/bones.json')).items()}
def segdist(X,a,b):
    ab=b-a; t=((X-a)@ab)/(ab@ab); q=a+np.clip(t,0,1)[:,None]*ab; return np.linalg.norm(X-q,axis=1),t
h0,h1=B['RightHand']; h1x=h1+(h1-h0)/np.linalg.norm(h1-h0)*0.06     # fingers reach past the bone tail
dh,_=segdist(P,h0,h1x); dfa,tf=segdist(P,*B['RightForeArm']); dua,_=segdist(P,*B['RightArm'])
dua_,tu=segdist(P,*B['RightArm'])
dax=np.minimum(np.minimum(dh,dfa),np.where(tu>0.5,dua_,9))   # hand + whole forearm + elbow / lower upper arm
R=(~twin)&(dax<0.16)
ml=np.load('meanlen.npy')
core=R&(((part==2)&(dax<0.045))|(dax<0.022)|((part==2)&(dax<0.09)&(ml<0.009)))
far=R&(dax>0.12)
# skirt/belt shards fused to the hand: coarse flat triangles sticking out past the hand volume -> body side
shard=R&(tf>0.6)&(((ml>0.016)&(dax>0.045))|((ml>0.011)&(dax>0.07)))
far|=shard
# region border: arm side (upper arm / elbow) -> source, everything else -> sink
ni=np.concatenate([E[:,0],E[:,1]]); nj=np.concatenate([E[:,1],E[:,0]])
nb=np.zeros(N,bool); nb[nj[R[ni]&~R[nj]]]=True
src_out=nb&((part==2)|(dua<0.07))&(P[:,2]>B['RightArm'][0][2]-0.16); snk_out=nb&~src_out
S=N; T=N+1
L=np.linalg.norm(P[E[:,0]]-P[E[:,1]],axis=1)
cap=np.maximum((L*1e4).astype(np.int64),1)
inE=(R[E[:,0]]|nb[E[:,0]])&(R[E[:,1]]|nb[E[:,1]])
rows=[E[inE,0],E[inE,1]]; cols=[E[inE,1],E[inE,0]]; caps=[cap[inE],cap[inE]]
BIG=10**9
s_idx=np.nonzero(core|src_out)[0]; t_idx=np.nonzero(far|snk_out)[0]
rows+= [np.full(len(s_idx),S), t_idx]; cols+=[s_idx, np.full(len(t_idx),T)]; caps+=[np.full(len(s_idx),BIG),np.full(len(t_idx),BIG)]
G=sp.csr_matrix((np.concatenate(caps).astype(np.int32 if False else np.int64),(np.concatenate(rows),np.concatenate(cols))),shape=(N+2,N+2))
G.sum_duplicates()
G=G.astype(np.int32) if G.max()<2**31 else G
res=maximum_flow(G,S,T)
F=res.flow
Rg=(G-F).tocsr(); Rg.data[Rg.data<0]=0; Rg.eliminate_zeros()
# reachable from S in residual
from scipy.sparse.csgraph import breadth_first_order
order=breadth_first_order(Rg,S,directed=True,return_predecessors=False)
armside=np.zeros(N+2,bool); armside[order]=True; armside=armside[:N]
ARM=armside&R
cutE=inE&(ARM[E[:,0]]!=ARM[E[:,1]])
print('flow',res.flow_value,'region',int(R.sum()),'core',int(core.sum()),'far',int(far.sum()),'arm side',int(ARM.sum()),'cut edges',int(cutE.sum()),'cut len m',round(float(L[cutE].sum()),3))
np.save('arm_lab.npy',ARM); np.save('armside_full.npy',armside); np.save('cutE.npy',E[cutE]); np.save('region.npy',R)
lab=np.zeros(N,int); lab[ARM]=1; lab[R&~ARM]=2; lab[core]=3; lab[far]=4
np.save('lab_cut.npy',lab)
