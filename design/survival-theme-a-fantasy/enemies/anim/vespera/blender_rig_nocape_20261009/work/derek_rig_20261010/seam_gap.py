import bpy, numpy as np, json, collections
o=bpy.data.objects['VESPERA_body']; a=bpy.data.objects['VESPERA_rig']; me=o.data; sc=bpy.context.scene
N=len(me.vertices); co0=np.array([v.co[:] for v in me.vertices])
names=[g.name for g in o.vertex_groups]; bones=set(b.name for b in a.data.bones)
W=np.zeros((N,len(names)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
dom=np.array([names[i].split(':')[1] for i in W.argmax(1)])
q=np.round(co0/1e-5).astype(np.int64); keys={}
pairs=[]
for i,k in enumerate(map(tuple,q)):
    if k in keys: pairs.append((keys[k],i))
    else: keys[k]=i
pairs=np.array(pairs)
def coords():
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); m=ob.to_mesh(); c=np.empty(N*3); m.vertices.foreach_get('co',c); ob.to_mesh_clear(); return c.reshape(-1,3)
out={'coincident_pairs':len(pairs)}
for act in ('VESPERA_nocape_walk','VESPERA_nocape_attack'):
    a.animation_data.action=bpy.data.actions[act]; G=np.zeros(len(pairs))
    for f in range(31):
        sc.frame_set(f); c=coords(); G=np.maximum(G,np.linalg.norm(c[pairs[:,0]]-c[pairs[:,1]],axis=1))
    t=G>0.01; C=collections.Counter(tuple(sorted({dom[i],dom[j]})) for i,j in pairs[t])
    P=co0[pairs[t][:,0]]
    R={}
    for k,_ in C.most_common(8):
        m=np.array([tuple(sorted({dom[i],dom[j]}))==k for i,j in pairs[t]]); Q=P[m]
        R['+'.join(k)]='x %.2f..%.2f y %.2f..%.2f z %.2f..%.2f'%(Q[:,0].min(),Q[:,0].max(),Q[:,1].min(),Q[:,1].max(),Q[:,2].min(),Q[:,2].max())
    out[act+'_where']=R
    out[act]={'seam_pairs_open_gt1cm':int(t.sum()),'gt3cm':int((G>0.03).sum()),'max_cm':round(float(G.max())*100,1),'by_bone_pair':C.most_common(8)}
json.dump(out,open('/workspace/vespera-derek-rig/work/seam_gap.json','w'),indent=1); print('SEAM',json.dumps(out))
