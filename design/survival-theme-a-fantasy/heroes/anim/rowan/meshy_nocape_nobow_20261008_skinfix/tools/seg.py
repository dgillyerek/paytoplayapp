# Shared: bow segmentation on the bind pose of the Meshy Rowan body mesh.
import numpy as np
def seg_dist(P, a, b):
    a=np.array(a); b=np.array(b); ab=b-a
    t=np.clip(((P-a)@ab)/max(ab@ab,1e-9),0,1)
    return np.linalg.norm(P-(a+t[:,None]*ab),axis=1)
def bow_mask(P, bones, edges):
    """P: Nx3 bind-pose world coords (Blender, Z up, character faces -Y). bones: name->(head,tail)."""
    H=lambda n: np.array(bones[n][0]); T=lambda n: np.array(bones[n][1])
    cand = (P[:,0] < -0.27) & (P[:,2] > 0.10)
    body = np.zeros(len(P),bool)
    body |= seg_dist(P, H('mixamorig:RightShoulder'), H('mixamorig:RightArm')) < 0.10
    body |= seg_dist(P, H('mixamorig:RightArm'), H('mixamorig:RightForeArm')) < 0.095
    body |= seg_dist(P, H('mixamorig:RightForeArm'), H('mixamorig:RightHand')) < 0.08
    wrist=H('mixamorig:RightHand'); knuck=H('mixamorig:RightHandMiddle1')
    body |= seg_dist(P, wrist, knuck) < 0.055
    for n in bones:
        if n.startswith('mixamorig:RightHand') and n!='mixamorig:RightHand' and not n.endswith('_end'):
            body |= seg_dist(P, H(n), T(n)) < 0.022
    for a,b in [('mixamorig:RightUpLeg','mixamorig:RightLeg'),('mixamorig:RightLeg','mixamorig:RightFoot'),('mixamorig:RightFoot','mixamorig:RightToeBase')]:
        body |= seg_dist(P, H(a), H(b)) < 0.13
    body |= seg_dist(P, H('mixamorig:Hips'), H('mixamorig:Spine2')) < 0.24
    m = cand & ~body
    body2 = body.copy()
    body2 |= P[:,0] > -0.16
    for a,b in [('mixamorig:RightUpLeg','mixamorig:RightLeg'),('mixamorig:RightLeg','mixamorig:RightFoot'),('mixamorig:RightFoot','mixamorig:RightToeBase')]:
        body2 |= seg_dist(P, H(a), H(b)) < 0.105
    # keep only connected components (within m) of meaningful size
    import collections
    adj=collections.defaultdict(list)
    for a,b in edges:
        if m[a] and m[b]: adj[a].append(b); adj[b].append(a)
    lab=-np.ones(len(P),int); comps=[]
    for s in np.nonzero(m)[0]:
        if lab[s]>=0: continue
        st=[s]; lab[s]=len(comps); c=[s]
        while st:
            x=st.pop()
            for y in adj[x]:
                if lab[y]<0: lab[y]=len(comps); st.append(y); c.append(y)
        comps.append(c)
    keep=np.zeros(len(P),bool)
    sizes=sorted((len(c) for c in comps), reverse=True)
    for c in comps:
        if len(c)>=5: keep[c]=True
    # grow the seed through mesh edges into anything that is not body (no x cut), to catch the
    # lower tip and string that hang beside the boot
    allowed = ~body2 | ((P[:,2] < 0.40) & (P[:,2] > 0.10) & (P[:,0] < -0.205))
    full=collections.defaultdict(list)
    for a,b in edges: full[a].append(b); full[b].append(a)
    st=list(np.nonzero(keep)[0])
    while st:
        x=st.pop()
        for y in full[x]:
            if not keep[y] and allowed[y]:
                keep[y]=True; st.append(y)
    # one ring of dilation at the lower nock where the tip is fused against the boot
    ring = (P[:,2] > 0.12) & (P[:,2] < 0.30) & (P[:,0] < -0.19)
    add=[y for x in np.nonzero(keep)[0] for y in full[x] if not keep[y] and ring[y]]
    keep[add]=True
    return keep, sizes[:15]
