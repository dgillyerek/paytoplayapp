import bpy, numpy as np, json, collections, sys
ACTS=sys.argv[sys.argv.index('--')+1].split(',')
o=bpy.data.objects['VESPERA_body']; a=bpy.data.objects['VESPERA_rig']; me=o.data; sc=bpy.context.scene
names=[g.name.split(':')[1] for g in o.vertex_groups]
N=len(me.vertices); E=np.array([e.vertices[:] for e in me.edges]); co0=np.array([v.co[:] for v in me.vertices])
W=np.zeros((N,len(names)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
d=np.load('/workspace/vespera-reweight/work/weights.npz'); part=d['part']; seed=d['seed']
def coords():
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); m=ob.to_mesh(); c=np.empty(N*3); m.vertices.foreach_get('co',c); ob.to_mesh_clear(); return c.reshape(-1,3)
L0=np.linalg.norm(co0[E[:,0]]-co0[E[:,1]],axis=1)
out={}
for act in ACTS:
    a.animation_data.action=bpy.data.actions[act]; G=np.zeros(len(E)); GF=np.zeros(len(E),int)
    for f in range(31):
        sc.frame_set(f); c=coords(); g=np.linalg.norm(c[E[:,0]]-c[E[:,1]],axis=1)-L0; m=g>G; G[m]=g[m]; GF[m]=f
    t=np.nonzero(G>0.02)[0]
    C=collections.Counter(); Z=collections.Counter(); PP=collections.Counter(); L0c=collections.Counter()
    for e in t:
        i,j=E[e]; C[tuple(sorted({names[W[i].argmax()],names[W[j].argmax()]}))]+=1
        Z[round(co0[i,2],1)]+=1; PP[tuple(sorted({int(part[i]),int(part[j])}))]+=1
        L0c['len0<1mm' if L0[e]<0.001 else ('len0<5mm' if L0[e]<0.005 else 'len0>=5mm')]+=1
    top=np.argsort(-G)[:int(sys.argv[-1]) if sys.argv[-1].isdigit() else 8]
    out[act]={'n':len(t),'pairs':C.most_common(12),'z':sorted(Z.items()),'parts':PP.most_common(),'L0':dict(L0c),
      'top':[dict(g=round(float(G[e])*100,1),f=int(GF[e]),L0=round(float(L0[e])*100,2),p=co0[E[e,0]].round(3).tolist(),w=[{names[k]:round(float(W[v,k]),2) for k in np.nonzero(W[v])[0]} for v in E[e]]) for e in top]}
    print('WHERE',act,json.dumps(out[act]))
json.dump(out,open('/workspace/vespera-reweight/work/where.json','w'),indent=1)
