import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, numpy as np, json, scipy.sparse as sp, scipy.sparse.csgraph as cg
from scipy.spatial import cKDTree
args=sys.argv[sys.argv.index('--')+1:]; ACTS=args[0].split(','); OUT=args[1]
o=bpy.data.objects['OAKENSHIELD_body']; me=o.data; arm=bpy.data.objects['OAKENSHIELD_rig']; sc=bpy.context.scene
N=len(me.vertices); E=np.array([e.vertices[:] for e in me.edges])
def coords():
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); m=ob.to_mesh(); c=np.empty(N*3); m.vertices.foreach_get('co',c); ob.to_mesh_clear(); return c.reshape(-1,3)
arm.data.pose_position='REST'; bpy.context.view_layer.update(); c0=coords(); arm.data.pose_position='POSE'
n,lab=cg.connected_components(sp.coo_matrix((np.ones(len(E)),(E[:,0],E[:,1])),shape=(N,N)),directed=False)
sz=np.bincount(lab); main=sz.argmax(); mi=np.nonzero(lab==main)[0]; tree=cKDTree(c0[mi])
pairs=[]
for k in range(n):
    if k==main: continue
    idx=np.nonzero(lab==k)[0]; d,j=tree.query(c0[idx]); pairs.append((k,idx,mi[j],d))
rep={}
for a in ACTS:
    arm.animation_data.action=bpy.data.actions[a]; worst={}
    for f in range(31):
        sc.frame_set(f); c=coords()
        for k,idx,jj,d0 in pairs:
            g=np.abs(np.linalg.norm(c[idx]-c[jj],axis=1)-d0).max(); worst[k]=max(worst.get(k,0),g)
    v=np.array(list(worst.values()))
    rep[a]={'islands':len(pairs),'gap_open_gt_1cm':int((v>0.01).sum()),'gap_open_gt_2cm':int((v>0.02).sum()),'max_gap_open_cm':round(float(v.max()*100),2),
            'worst':[(int(k),int(sz[k]),round(float(g*100),1),np.round(c0[lab==k].mean(0),3).tolist()) for k,g in sorted(worst.items(),key=lambda x:-x[1])[:6]]}
    print('GAP',a,json.dumps(rep[a]))
json.dump(rep,open(OUT,'w'),indent=1)
