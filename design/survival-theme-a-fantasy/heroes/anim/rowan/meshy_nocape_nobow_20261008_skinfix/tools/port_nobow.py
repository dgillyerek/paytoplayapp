"""Port the Meshy skin cleanup onto Design's no-bow bodies (meshy_nocape_nobow_20261008).
The no-bow body is the Meshy body with the bow faces deleted and 4 cap verts added (same weights, metres,
Blender re-export). Weights are computed on the untouched Meshy source (reweight.compute) with the
fist->forearm rule OFF (the bow prop rides RightHand, so the hand keeps hand weights), then copied by
bind position. Only the cluster Indexes/Weights change; with SPLIT=1 the few fist-to-pouch weld triangles
that still tear are dropped too.
  python3 port_nobow.py <meshy source walk fbx> <nobow fbx in> <nobow fbx out>
Env: SRC_WORK (source bind_w/bind_pts/stretch_orig), NB_WORK (nobow bind_w/bind_pts, stretch/ for SPLIT)."""
import os, sys, json, collections, numpy as np
SRC_WORK=os.environ['SRC_WORK']; NB_WORK=os.environ['NB_WORK']
os.environ['ROWAN_FIX_WORK']=SRC_WORK
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fbxbin, reweight
reweight.FIST_TO_FOREARM=False
_seg_bow=reweight.bow_mask
def _nobow_mask(P,B,E):
    # "bow" = only the verts Design actually deleted; the ~60 grip/stub verts Design kept are body skin here
    m,x=_seg_bow(P,B,E); idx,ok,_=vmap(); m=m.copy(); m[idx[ok]]=False; return m,x
reweight.bow_mask=_nobow_mask
def vmap():
    a=json.load(open(SRC_WORK+'/bind_w.json')); b=json.load(open(NB_WORK+'/bind_w.json'))
    PA=np.array([v[0] for v in a['v']]); PB=np.array([v[0] for v in b['v']])
    idx=np.zeros(len(PB),int); d=np.zeros(len(PB))
    for s in range(0,len(PB),500):
        D=((PB[s:s+500,None,:]-PA[None])**2).sum(-1); idx[s:s+500]=D.argmin(1); d[s:s+500]=np.sqrt(D.min(1))
    return idx, d<1e-4, b
def split_welds(f, part, P, low_z, grow_min=0.03):
    g={}
    for c in ['rest','walk','attack']:
        s=np.load(NB_WORK+f'/stretch/st_{c}.npz')
        for (a,b),x in zip(s['E'],s['grow']):
            k=(min(a,b),max(a,b)); g[k]=max(g.get(k,0),x)
    low=P[:,2]<low_z
    hand=(part==1)|(part==2); body=(part==4)|((part==0)&low)
    objs=[n for n in f['root'].children if n.name==b'Objects'][0]
    geo=[c for c in objs.children if c.name==b'Geometry' and b'output_unwrapped' in fbxbin.props(c)[1][1]][0]
    pvi=fbxbin.props(geo.find(b'PolygonVertexIndex')[0])[0][1]
    V=lambda x: x if x>=0 else ~x
    polys=[]; cur=[]
    for k,v in enumerate(pvi):
        cur.append(k)
        if v<0: polys.append(cur); cur=[]
    keep=[]; keepi=[]; dropped=0
    for pi,poly in enumerate(polys):
        vs=np.array([V(pvi[k]) for k in poly])
        mx=max(g.get((min(a,b),max(a,b)),0) for a,b in zip(vs,np.roll(vs,-1)))
        if hand[vs].any() and body[vs].any() and low[vs].all() and mx>grow_min: dropped+=1
        else: keep.append(poly); keepi.append(pi)
    ks=[k for poly in keep for k in poly]; new_pvi=[pvi[k] for k in ks]
    # Edges keep their original order: each surviving edge points at a polygon-vertex that starts it
    oldE=fbxbin.props(geo.find(b'Edges')[0])[0][1]
    def nxt(arr,k):  # vertex after polygon-vertex k
        return V(arr[k+1]) if arr[k]>=0 else None
    start_of={}; s=0
    for poly in keep:
        n=len(poly)
        for j in range(n):
            a=V(new_pvi[s+j]); b=V(new_pvi[s+(j+1)%n]); start_of.setdefault((min(a,b),max(a,b)),s+j)
        s+=n
    ostart=[]; s=0
    for poly in polys:
        n=len(poly)
        for j in range(n): ostart.append((V(pvi[poly[j]]),V(pvi[poly[(j+1)%n]])))
    newE=[]
    for e in oldE:
        a,b=ostart[e]; k=(min(a,b),max(a,b))
        if k in start_of: newE.append(start_of[k])
    fbxbin.set_prop(geo.find(b'PolygonVertexIndex')[0],0,new_pvi)
    for le,arr in ((b'LayerElementNormal',b'NormalsIndex'),(b'LayerElementUV',b'UVIndex')):
        node=geo.find(le)[0].find(arr)[0]; old=fbxbin.props(node)[0][1]
        fbxbin.set_prop(node,0,[old[k] for k in ks])
    sm=geo.find(b'LayerElementSmoothing')
    if sm:
        node=sm[0].find(b'Smoothing')[0]; old=fbxbin.props(node)[0][1]
        fbxbin.set_prop(node,0,[old[i] for i in keepi])
    fbxbin.set_prop(geo.find(b'Edges')[0],0,newE)
    return dropped, len(polys), len(oldE)-len(newE)
if __name__=='__main__':
    src,nin,nout=sys.argv[1:4]
    _,_,W,NW,bow,stats=reweight.compute(src)
    spart=np.load(SRC_WORK+'/part.npy')
    idx,ok,b=vmap(); N=len(idx)
    assert not bow[idx[ok]].any(), 'no-bow body maps onto bow verts'
    nb=collections.defaultdict(list)
    for x,y in b['e']: nb[x].append(y); nb[y].append(x)
    NWb=[dict(NW[idx[i]]) if ok[i] else None for i in range(N)]
    part=np.array([spart[idx[i]] if ok[i] else 0 for i in range(N)])
    for i in np.where(~ok)[0]:   # cap centre verts: average of their ring
        ns=[j for j in nb[i] if ok[j]]; m=collections.defaultdict(float)
        for j in ns:
            for k,v in NWb[j].items(): m[k]+=v/len(ns)
        NWb[i]=reweight_clean=dict(sorted(m.items(),key=lambda kv:-kv[1])[:reweight.MAX_INF])
        t=sum(NWb[i].values()); NWb[i]={k:v/t for k,v in NWb[i].items()}
        part[i]=collections.Counter(part[j] for j in ns).most_common(1)[0][0]
    f=fbxbin.read(nin); C=reweight.clusters(f)
    # sanity: the no-bow file carries exactly Meshy's original weights on the matched verts
    Wb=[dict() for _ in range(N)]
    for name,c in C.items():
        if c.find(b'Indexes'):
            for i,w in zip(fbxbin.props(c.find(b'Indexes')[0])[0][1], fbxbin.props(c.find(b'Weights')[0])[0][1]): Wb[i][name]=w
    mism=sum(1 for i in range(N) if ok[i] and (set(Wb[i])!=set(W[idx[i]]) or max(abs(Wb[i][k]-W[idx[i]][k]) for k in Wb[i])>1e-4))
    assert mism==0, f'{mism} no-bow verts differ from Meshy source weights'
    reweight.patch(f,C,NWb)
    out=dict(verts=N, cap_verts=int((~ok).sum()), fist_rule=False)
    if os.environ.get('SPLIT','1')=='1':
        B=json.load(open(NB_WORK+'/bind_pts.json'))['bones']
        P=np.array([v[0] for v in b['v']])
        lz=min(B['mixamorig:LeftForeArm'][0][2],B['mixamorig:RightForeArm'][0][2])-0.08
        out['welds_dropped'],out['tris_before'],out['edges_removed']=split_welds(f,part,P,lz)
    fbxbin.write(f,nout)
    print(json.dumps(out))
