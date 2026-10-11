import bpy, sys, numpy as np, json
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]; ACTS=args[1].split(','); OBJS=args[2].split(',')
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
if arm.animation_data is None: arm.animation_data_create()
sc=bpy.context.scene
objs=[bpy.data.objects[n] for n in OBJS]
def coords(o):
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); m=ob.to_mesh(); N=len(o.data.vertices)
    c=np.empty(N*3); m.vertices.foreach_get('co',c); ob.to_mesh_clear(); return (np.array(o.matrix_world)[:3,:3]@c.reshape(-1,3).T).T
arm.data.pose_position='REST'; bpy.context.view_layer.update()
base={o.name:(np.array([e.vertices[:] for e in o.data.edges]),coords(o)) for o in objs}
arm.data.pose_position='POSE'
rep={}; perv={}
for a in ACTS:
    act=bpy.data.actions[a]; arm.animation_data.action=act; f0,f1=map(int,act.frame_range)
    for o in objs:
        E,c0=base[o.name]; L0=np.linalg.norm(c0[E[:,0]]-c0[E[:,1]],axis=1)
        g=np.zeros(len(E)); mx=np.zeros(len(E)); gf=np.zeros(len(E),int)
        for f in range(f0,f1+1):
            sc.frame_set(f); c=coords(o); L=np.linalg.norm(c[E[:,0]]-c[E[:,1]],axis=1)
            d=L-L0; upd=d>g; gf[upd]=f; g=np.maximum(g,d); mx=np.maximum(mx,L/np.maximum(L0,1e-6))
        k=f'{a}:{o.name}'
        mid=(c0[E[:,0]]+c0[E[:,1]])/2
        bad=g>0.05
        rep[k]={'edges':int(len(E)),'grow_gt_2cm':int((g>0.02).sum()),'grow_gt_5cm':int(bad.sum()),'max_grow_cm':round(float(g.max()*100),2),'max_ratio':round(float(mx.max()),2),
          'gt5_z_hist(0-2m,0.2)':np.histogram(mid[bad,2],bins=10,range=(0,2))[0].tolist() if bad.any() else []}
        v=np.zeros(len(c0)); np.maximum.at(v,E[:,0],g); np.maximum.at(v,E[:,1],g); perv[k.replace(':','__')]=v
        print('STRETCH',k,json.dumps(rep[k]),flush=True)
np.savez(OUT.replace('.json','_v.npz'),**perv)
json.dump(rep,open(OUT,'w'),indent=1)
