import bpy, sys, numpy as np, json
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]; ACTS=args[1].split(',')
o=bpy.data.objects['VESPERA_nocape_body']; me=o.data; arm=bpy.data.objects['VESPERA_nocape_rig']
E=np.array([e.vertices[:] for e in me.edges]); N=len(me.vertices)
def coords():
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); m=ob.to_mesh()
    c=np.empty(N*3); m.vertices.foreach_get('co',c); ob.to_mesh_clear(); return c.reshape(-1,3)
arm.data.pose_position='REST'; bpy.context.view_layer.update(); c0=coords(); L0=np.linalg.norm(c0[E[:,0]]-c0[E[:,1]],axis=1)
arm.data.pose_position='POSE'
rep={}; perv={}
for a in ACTS:
    arm.animation_data.action=bpy.data.actions[a]; f0,f1=map(int,bpy.data.actions[a].frame_range)
    mx=np.zeros(len(E)); g=np.zeros(len(E)); mn=np.full(len(E),9.0)
    for f in range(f0,f1+1):
        bpy.context.scene.frame_set(f); c=coords(); L=np.linalg.norm(c[E[:,0]]-c[E[:,1]],axis=1)
        r=L/np.maximum(L0,1e-6); mx=np.maximum(mx,r); mn=np.minimum(mn,r); g=np.maximum(g,L-L0)
    rep[a]={'frames':[f0,f1],'edges':int(len(E)),'stretch_gt_1.25x':int((mx>1.25).sum()),'stretch_gt_1.5x':int((mx>1.5).sum()),'stretch_gt_2x':int((mx>2).sum()),
            'grow_gt_1cm':int((g>0.01).sum()),'grow_gt_2cm':int((g>0.02).sum()),'grow_gt_5cm':int((g>0.05).sum()),'max_ratio':round(float(mx.max()),3),'max_grow_cm':round(float(g.max()*100),2),
            'compress_lt_0.5x':int((mn<0.5).sum()),'stretched_50pct_and_5mm':int(((mx>1.5)&(g>0.005)).sum()),'stretched_25pct_and_5mm':int(((mx>1.25)&(g>0.005)).sum())}
    v=np.zeros(N); np.maximum.at(v,E[:,0],mx); np.maximum.at(v,E[:,1],mx); perv[a]=v
    print('STRETCH',a,json.dumps(rep[a]))
np.savez(OUT.replace('.json','_v.npz'),**perv)
json.dump(rep,open(OUT,'w'),indent=1)
