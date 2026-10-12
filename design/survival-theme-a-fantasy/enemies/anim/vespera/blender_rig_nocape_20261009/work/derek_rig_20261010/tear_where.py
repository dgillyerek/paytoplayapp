import bpy, numpy as np, json, collections
o=bpy.data.objects['VESPERA_body']; a=bpy.data.objects['VESPERA_rig']; me=o.data; sc=bpy.context.scene
names=[g.name for g in o.vertex_groups]; bones=set(b.name for b in a.data.bones)
N=len(me.vertices); E=np.array([e.vertices[:] for e in me.edges]); co0=np.array([v.co[:] for v in me.vertices])
W=np.zeros((N,len(names)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
live=np.array([n in bones for n in names]); orphan=W[:,live].sum(1)<1e-6; partial=(W[:,~live].sum(1)>1e-6)&~orphan
dom=np.array([names[i].split(':')[1] for i in W.argmax(1)])
def coords():
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); m=ob.to_mesh(); c=np.empty(N*3); m.vertices.foreach_get('co',c); ob.to_mesh_clear(); return c.reshape(-1,3)
L0=np.linalg.norm(co0[E[:,0]]-co0[E[:,1]],axis=1)
out={}
for act in ('VESPERA_nocape_walk','VESPERA_nocape_attack'):
    a.animation_data.action=bpy.data.actions[act]; G=np.zeros(len(E))
    for f in range(31):
        sc.frame_set(f); c=coords(); G=np.maximum(G,np.linalg.norm(c[E[:,0]]-c[E[:,1]],axis=1)-L0)
    t=G>0.05; ei=E[t]
    orph=orphan[ei].any(1); part=partial[ei].any(1)&~orph
    C=collections.Counter()
    for (i,j) in ei: C[tuple(sorted({dom[i],dom[j]}))]+=1
    mid=(co0[ei[:,0]]+co0[ei[:,1]])/2
    out[act]={'edges_gt5cm':int(t.sum()),'touch_orphan_vertex':int(orph.sum()),'touch_partly_orphan':int(part.sum()),'clean_weights':int((~orph&~part).sum()),
      'by_main_bone_pair':C.most_common(10),'z_hist_0.1m':np.histogram(mid[:,2],bins=np.arange(0,2.01,0.1))[0].tolist()}
json.dump(out,open('/workspace/vespera-derek-rig/work/tear_where.json','w'),indent=1)
for k,v in out.items(): print('TEAR',k,json.dumps(v))
