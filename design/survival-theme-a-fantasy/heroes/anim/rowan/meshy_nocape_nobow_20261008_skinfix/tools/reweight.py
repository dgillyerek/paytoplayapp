"""Rowan Meshy skin fix: compute fixed skin weights for Design's Meshy FBX and patch only the FBX skin
clusters (and drop the few triangles that weld the bow / left fist to the body). Everything else in the
file (mesh, UVs, bones, bind pose, animation, take names, units) is kept byte for byte.
Usage: python3 reweight.py <src.fbx> <dst.fbx>   (needs bind data + stretch data in $ROWAN_FIX_WORK, see build.sh)"""
import sys, os, json, collections, numpy as np
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import fbxbin
from seg import bow_mask, seg_dist
WORK=os.environ.get('ROWAN_FIX_WORK','/workspace/rowan-meshy-fix')
# The bow, and the right fist that holds it, ride the right forearm as one rigid piece.
BOW_W={"mixamorig:RightForeArm": 1.0}
FIST_TO_FOREARM=True
HELPER_ON_LIMB=0.07
KEEP_GROW=0.03
W_MIN=0.05; MAX_INF=4; FAR=0.12; FAR_RATIO=2.5; SMOOTH_ITERS=4; SMOOTH_ALPHA=0.5
def clusters(f):
    objs=[n for n in f['root'].children if n.name==b'Objects'][0]
    out={}
    for c in objs.children:
        if c.name==b'Deformer' and fbxbin.props(c)[2][1]==b'Cluster':
            out[fbxbin.props(c)[1][1].split(b'\x00')[0].decode()]=c
    return out
def compute(src):
    d=json.load(open(WORK+'/bind_w.json')); B=json.load(open(WORK+'/bind_pts.json'))['bones']
    P=np.array([v[0] for v in d['v']]); N=len(P)
    f=fbxbin.read(src); C=clusters(f)
    W=[dict() for _ in range(N)]
    for name,c in C.items():
        if c.find(b'Indexes'):
            idx=fbxbin.props(c.find(b'Indexes')[0])[0][1]; wt=fbxbin.props(c.find(b'Weights')[0])[0][1]
            for i,w in zip(idx,wt):
                if w>0: W[i][name]=W[i].get(name,0)+w
    # order check vs Blender's groups
    bad=sum(1 for i in range(N) if set(k for k,v in W[i].items() if v>1e-6)!=set(k for k,v in d['v'][i][1].items() if v>1e-6))
    assert bad==0, 'vertex order mismatch %d'%bad
    live=[n for n,c in C.items() if c.find(b'Indexes')]
    targets=[n for n in live if n.startswith('mixamorig:') and not n.lower().endswith('_end')]
    D=np.stack([seg_dist(P,np.array(B[n][0]),np.array(B[n][1])) for n in targets],1)
    nearest=np.array(targets)[D.argmin(1)]; dmin=D.min(1); ti={n:i for i,n in enumerate(targets)}
    stats=collections.Counter()
    bow,_=bow_mask(P,B,d['e'])
    parents=json.load(open(WORK+'/bind_pts.json'))['parents']
    def anc(b):
        while b not in ti: b=parents[b]
        return b
    def capsule(side):
        S='Left' if side=='L' else 'Right'; H=lambda n: np.array(B['mixamorig:'+S+n][0]); T=lambda n: np.array(B['mixamorig:'+S+n][1])
        m = seg_dist(P,H('Arm'),H('ForeArm')) < 0.13
        m |= seg_dist(P,H('Shoulder'),H('Arm')) < 0.11
        m |= seg_dist(P,H('ForeArm'),H('Hand')) < 0.09
        m |= seg_dist(P,H('Hand'),H('HandMiddle1')) < 0.06
        for n in B:
            if n.startswith('mixamorig:'+S+'Hand') and not n.endswith('_end') and n!='mixamorig:'+S+'Hand':
                m |= seg_dist(P,np.array(B[n][0]),np.array(B[n][1])) < 0.028
        return m
    capL, capR = capsule('L'), capsule('R')
    zone=np.array([None]*N,dtype=object); zone[capL]='L'; zone[capR]='R'
    # grow each arm zone along mesh edges into finger tips / glove edges that are mostly weighted to that arm
    nbr=collections.defaultdict(list)
    for a_,b_ in d['e']: nbr[a_].append(b_); nbr[b_].append(a_)
    for side,S in (('L','Left'),('R','Right')):
        chain=[n for n in targets if n.startswith('mixamorig:'+S) and ('Arm' in n or 'Hand' in n)]
        dch=D[:,[ti[n] for n in chain]].min(1)
        armw=np.array([sum(v for k,v in W[i].items() if k.startswith('mixamorig:'+S+'Arm') or k.startswith('mixamorig:'+S+'ForeArm') or k.startswith('mixamorig:'+S+'Hand')) for i in range(N)])
        ok=(armw>=0.6)&(dch<0.06)&(zone==None)
        st=[i for i in np.nonzero(zone==side)[0]]
        grown=0
        while st:
            x=st.pop()
            for y in nbr[x]:
                if ok[y] and zone[y] is None:
                    zone[y]=side; st.append(y); grown+=1
        stats['zone_grow_'+side]=grown
    # outer sleeve: beyond the shoulder joint (armpit and shoulder keep their trunk blend)
    lx=np.array(B['mixamorig:LeftArm'][0])[0]; rx=np.array(B['mixamorig:RightArm'][0])[0]
    lateral=((zone=='L')&(P[:,0]>lx+0.05))|((zone=='R')&(P[:,0]<rx-0.05))
    ARM={s:[n for n in targets if n.startswith('mixamorig:'+('Left' if s=='L' else 'Right')) and ('Arm' in n or 'Hand' in n)] for s in 'LR'}
    TRUNK=[n for n in targets if n.split(':')[1] in ('Hips','Spine','Spine1','Spine2','Neck','Head','LeftShoulder','RightShoulder','LeftUpLeg','RightUpLeg')]
    def near_in(i,names): return min(names,key=lambda n: D[i,ti[n]])
    def arm_side(b):
        n=b.split(':')[-1]
        if 'Shoulder' in n: return None
        if n.startswith('LeftArm') or n.startswith('LeftForeArm') or n.startswith('LeftHand'): return 'L'
        if n.startswith('RightArm') or n.startswith('RightForeArm') or n.startswith('RightHand'): return 'R'
        return None
    np.save(WORK+'/zone.npy', np.where(bow,3,np.where(zone=='L',1,np.where(zone=='R',2,0))))
    stats['zone_L']=int(capL.sum()); stats['zone_R']=int(capR.sum()); stats['lateral']=int(lateral.sum())
    def is_distal(b):
        return any(k in b for k in ('Hand','ForeArm','Leg','Foot','Toe')) and 'UpLeg' not in b
    NW=[]
    part=np.array(['']*N,dtype=object); part[bow]='bow'
    ELBOW_Z={'L':B['mixamorig:LeftForeArm'][0][2]-0.02,'R':B['mixamorig:RightForeArm'][0][2]-0.02}
    # left fist/forearm vs belt pouch contact: arm or body per vertex (arm-chain weight >= 0.5, helpers on the
    # arm count as arm), then a neighbour majority filter so no lone vertex is left on the wrong side
    cz=(zone=='L')&(P[:,2]<ELBOW_Z['L'])
    contact_arm=np.array([sum(v for k,v in W[i].items() if arm_side(k)=='L' or k not in ti)>=0.5 for i in range(N)])
    for _ in range(3):
        nxt=contact_arm.copy()
        for i in np.nonzero(cz)[0]:
            ns=[j for j in nbr[i] if cz[j]]
            if ns:
                frac=np.mean([contact_arm[j] for j in ns])
                if frac>=0.75: nxt[i]=True
                elif frac<=0.25: nxt[i]=False
        contact_arm=nxt
    # where Meshy's own skin never stretched (all incident edges grow < KEEP_GROW in every clip), keep it
    og=np.zeros(N)
    for c in ['rest','walk','attack']:
        st=np.load(f'{WORK}/stretch_orig/st_{c}.npz')
        np.maximum.at(og, st['E'][:,0], st['grow']); np.maximum.at(og, st['E'][:,1], st['grow'])
    rhand=np.array([sum(v for k,v in W[i].items() if k.startswith('mixamorig:RightHand') or k=='Bone_057' or k=='Bone_025')>=0.3 for i in range(N)])
    chain=np.array([sum(v for k,v in W[i].items() if k in ('Bone_025','Bone_026','Bone_027','Bone_028','Bone_029'))>=0.1 for i in range(N)])
    keep_orig=(og<KEEP_GROW)&~bow&~rhand&(zone!='R')&~chain
    stats['kept_meshy']=int(keep_orig.sum())
    for i in range(N):
        if keep_orig[i] and not bow[i]:
            nw=collections.defaultdict(float)
            for b,w in W[i].items(): nw[anc(b) if b not in ti else b]+=w
            NW.append(dict(nw)); continue
        if bow[i]: NW.append(dict(BOW_W)); stats['bow']+=1; continue
        nw=collections.defaultdict(float)
        z=zone[i]
        for b,w in W[i].items():
            if b not in ti:
                # static helper bone (never animated). The Bone_025-029 chain hangs off Spine2 but skins the
                # right shoulder, sleeve and bow: blend it from Spine2 (inside the shoulder) to the right arm
                # (outside the shoulder joint). Other helpers: on an arm -> that arm, else their ancestor.
                if anc(b)=='mixamorig:Spine2' and (z=='R' or P[i,0] < rx+0.06):
                    t=float(np.clip((rx+0.06-P[i,0])/0.12,0,1)) if z!='R' or P[i,2]>1.05 else 1.0
                    nw[near_in(i,ARM['R'])]+=w*t; nw['mixamorig:Spine2']+=w*(1-t); stats['helper_grad']+=1
                elif z: nw[near_in(i,ARM[z])]+=w; stats['helper_to_limb']+=1
                else: nw[anc(b)]+=w; stats['helper_to_parent']+=1
                continue
            side=arm_side(b)
            if side and side!=z:
                # arm/hand/finger weight on a vertex that is not on that arm (pouch, belt, hip, leg, bow)
                nw[near_in(i,TRUNK)]+=w; stats['arm_off_arm']+=1
            elif z and b in TRUNK and lateral[i]:
                # trunk weight on the outer sleeve: the sleeve sails when the arm lifts
                nw[near_in(i,ARM[z])]+=w; stats['trunk_on_sleeve']+=1
            elif is_distal(b) and D[i,ti[b]]>max(FAR,FAR_RATIO*dmin[i]):
                nw[nearest[i]]+=w; stats['far_moved']+=1
            else: nw[b]+=w
        if z=='L' and P[i,2] < ELBOW_Z[z]:
            if contact_arm[i]:
                for k in [k for k in nw if arm_side(k)!=z]:
                    nw[near_in(i,ARM[z])]+=nw.pop(k)
                part[i]=z; stats['contact_arm']+=1
            else:
                for k in [k for k in nw if arm_side(k)==z]:
                    nw[near_in(i,TRUNK)]+=nw.pop(k)
                part[i]='body'; stats['contact_body']+=1
        if FIST_TO_FOREARM:
            # the bow-holding fist is one rigid unit with the bow: right hand + finger weights ride the forearm
            for b in [k for k in nw if k.startswith('mixamorig:RightHand')]:
                nw['mixamorig:RightForeArm']+=nw.pop(b); stats['fist_moved']+=1
        NW.append(dict(nw))
    def clean(w):
        w={k:v for k,v in w.items() if v>=W_MIN} or {max(w,key=w.get):1.0}
        w=dict(sorted(w.items(),key=lambda kv:-kv[1])[:MAX_INF]); s=sum(w.values())
        return {k:v/s for k,v in w.items()}
    NW=[clean(w) for w in NW]
    nb=collections.defaultdict(list)
    for a,b in d['e']: nb[a].append(b); nb[b].append(a)
    for _ in range(SMOOTH_ITERS):
        out=[]
        for i in range(N):
            if bow[i]: out.append(NW[i]); continue
            ns=[j for j in nb[i] if part[j]==part[i]]
            if not ns: out.append(NW[i]); continue
            avg=collections.defaultdict(float)
            for j in ns:
                for k,v in NW[j].items(): avg[k]+=v/len(ns)
            m=collections.defaultdict(float)
            for k,v in NW[i].items(): m[k]+=(1-SMOOTH_ALPHA)*v
            for k,v in avg.items(): m[k]+=SMOOTH_ALPHA*v
            out.append(clean(m))
        NW=out
    np.save(WORK+'/part.npy', np.where(part=='bow',3,np.where(part=='L',1,np.where(part=='R',2,np.where(part=='body',4,0)))))
    return f,C,W,NW,bow,stats
def patch(f,C,NW):
    per=collections.defaultdict(list)
    for i,w in enumerate(NW):
        for k,v in w.items(): per[k].append((i,v))
    for name,c in C.items():
        rows=sorted(per.get(name,[]))
        ix=c.find(b'Indexes'); wt=c.find(b'Weights')
        if rows and not ix: raise RuntimeError('no cluster arrays for '+name)
        if not ix: continue
        if not rows:
            c.children=[k for k in c.children if k.name not in (b'Indexes',b'Weights')]; continue
        fbxbin.set_prop(ix[0],0,[r[0] for r in rows]); fbxbin.set_prop(wt[0],0,[float(r[1]) for r in rows])
def split_welds(f, grow_min=0.03):
    """Drop the few triangles that weld rigid parts to the body (bow tip to boot, fists to belt pouches)."""
    d=json.load(open(WORK+'/bind_w.json')); P=np.array([v[0] for v in d['v']])
    z=np.load(WORK+'/part.npy')
    g={}
    for c in ['rest','walk','attack']:
        s=np.load(os.environ.get('STRETCH_DIR',WORK+'/stretch')+f'/st_{c}.npz')
        for (a,b),x in zip(s['E'],s['grow']):
            k=(min(a,b),max(a,b)); g[k]=max(g.get(k,0),x)
    B=json.load(open(WORK+'/bind_pts.json'))['bones']
    low=P[:,2] < min(B['mixamorig:LeftForeArm'][0][2],B['mixamorig:RightForeArm'][0][2])-0.08
    bow=z==3; hand=(z==1)|(z==2); body=(z==4)|((z==0)&low)
    def crosses(vs):
        return (bow[vs].any() and (~bow[vs]).any()) or (hand[vs].any() and body[vs].any() and low[vs].all())
    objs=[n for n in f['root'].children if n.name==b'Objects'][0]
    geo=[c for c in objs.children if c.name==b'Geometry' and b'output_unwrapped' in fbxbin.props(c)[1][1]][0]
    pvi=fbxbin.props(geo.find(b'PolygonVertexIndex')[0])[0][1]
    polys=[]; cur=[]
    for k,v in enumerate(pvi):
        cur.append(k)
        if v<0: polys.append(cur); cur=[]
    keep=[]; dropped=[]
    for poly in polys:
        vs=np.array([pvi[k] if pvi[k]>=0 else ~pvi[k] for k in poly])
        mx=max(g.get((min(a,b),max(a,b)),0) for a,b in zip(vs,np.roll(vs,-1)))
        if crosses(vs) and mx>grow_min: dropped.append(poly)
        else: keep.append(poly)
    ks=[k for poly in keep for k in poly]
    new_pvi=[pvi[k] for k in ks]
    fbxbin.set_prop(geo.find(b'PolygonVertexIndex')[0],0,new_pvi)
    for le,arr in ((b'LayerElementNormal',b'NormalsIndex'),(b'LayerElementUV',b'UVIndex')):
        node=geo.find(le)[0].find(arr)[0]; old=fbxbin.props(node)[0][1]
        fbxbin.set_prop(node,0,[old[k] for k in ks])
    # Edges: first polygon-vertex of each unique edge, in order of first use (FBX SDK layout)
    seen=set(); edges=[]; start=0
    for poly in keep:
        n=len(poly)
        for j in range(n):
            a=new_pvi[start+j]; b=new_pvi[start+(j+1)%n]; a=a if a>=0 else ~a; b=b if b>=0 else ~b
            k=(min(a,b),max(a,b))
            if k not in seen: seen.add(k); edges.append(start+j)
        start+=n
    fbxbin.set_prop(geo.find(b'Edges')[0],0,edges)
    return len(dropped), len(polys)
def edges_match_original(f):
    objs=[n for n in f['root'].children if n.name==b'Objects'][0]
    geo=[c for c in objs.children if c.name==b'Geometry' and b'output_unwrapped' in fbxbin.props(c)[1][1]][0]
    pvi=fbxbin.props(geo.find(b'PolygonVertexIndex')[0])[0][1]; orig=fbxbin.props(geo.find(b'Edges')[0])[0][1]
    seen=set(); edges=[]; start=0; polys=[]; cur=[]
    for k,v in enumerate(pvi):
        cur.append(k)
        if v<0: polys.append(cur); cur=[]
    for poly in polys:
        n=len(poly)
        for j in range(n):
            a=pvi[start+j]; b=pvi[start+(j+1)%n]; a=a if a>=0 else ~a; b=b if b>=0 else ~b
            k=(min(a,b),max(a,b))
            if k not in seen: seen.add(k); edges.append(start+j)
        start+=n
    return edges==list(orig)
if __name__=='__main__':
    src,dst=sys.argv[1],sys.argv[2]
    f,C,W,NW,bow,stats=compute(src)
    assert edges_match_original(f), 'edge layout differs from the FBX SDK order'
    patch(f,C,NW)
    if os.environ.get('SPLIT','1')=='1':  # build.sh pass 1 runs with SPLIT=0
        stats['welds_dropped'],stats['tris_before']=split_welds(f)
    fbxbin.write(f,dst)
    ninf=collections.Counter(len(w) for w in NW); oinf=collections.Counter(sum(1 for v in w.values() if v>0) for w in W)
    small=sum(1 for w in W for v in w.values() if 0<v<W_MIN)
    print(json.dumps(dict(stats=stats, bow_verts=int(bow.sum()), before_influences=sorted(oinf.items()), before_weights_under_005=small, after_influences=sorted(ninf.items()))))
