Z=dict(np.load('zone.npz')); ZB=[str(b) for b in Z['bn']]
assert ZB==n, 'bone order'
SUB=globals().get('SKIN_SUB',4)
for s in ('Left','Right'):
    e=Z[s+'_e'][::SUB]; used=np.unique(e); remap=-np.ones(len(Z[s+'_v']),int); remap[used]=np.arange(len(used))
    Z[s+'_v']=Z[s+'_v'][used]; Z[s+'_w']=Z[s+'_w'][used]; Z[s+'_e']=remap[e]
    Z[s+'_rl']=np.linalg.norm(Z[s+'_v'][Z[s+'_e'][:,0]]-Z[s+'_v'][Z[s+'_e'][:,1]],axis=1)
    Z[s+'_bi']=np.where(Z[s+'_w'].sum(0)>0)[0]
    Z[s+'_vh']=np.c_[Z[s+'_v'],np.ones(len(Z[s+'_v']))]
RINV=np.array([inv(r) for r in R])
def stretch(f,s,Ma,Mf,Mh):
    iS,iA,iF,iH=SIDE[s]['idx']; V=np.zeros((len(Z[s+'_v']),3))
    for b in Z[s+'_bi']:
        M=Ma if b==iA else Mf if b==iF else Mh if b==iH else SH[s] if b==iS else P[f,b]
        V+=Z[s+'_w'][:,b:b+1]*((M@RINV[b])@Z[s+'_vh'].T).T[:,:3]
    e=Z[s+'_e']; pl=np.linalg.norm(V[e[:,0]]-V[e[:,1]],axis=1)/Z[s+'_rl']
    return pl
