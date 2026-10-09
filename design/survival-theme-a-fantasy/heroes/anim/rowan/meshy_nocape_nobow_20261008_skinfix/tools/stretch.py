import bpy, sys, numpy as np, json
args=sys.argv[sys.argv.index('--')+1:]
fbx,out,step=args[0],args[1],int(args[2])
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
o=bpy.data.objects['output_unwrapped']; me=o.data
arm=bpy.data.objects['target_character']; ac=arm.animation_data.action; f0,f1=map(int,ac.frame_range)
E=np.array([e.vertices[:] for e in me.edges]); N=len(me.vertices)
def coords():
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); m=ob.to_mesh()
    c=np.empty(N*3); m.vertices.foreach_get('co',c); ob.to_mesh_clear(); return c.reshape(-1,3)
arm.data.pose_position='REST'; bpy.context.view_layer.update(); c0=coords(); L0=np.linalg.norm(c0[E[:,0]]-c0[E[:,1]],axis=1)
arm.data.pose_position='POSE'
mx=np.zeros(len(E)); mxabs=np.zeros(len(E))
for f in range(f0,f1+1,step):
    bpy.context.scene.frame_set(f); c=coords(); L=np.linalg.norm(c[E[:,0]]-c[E[:,1]],axis=1)
    mx=np.maximum(mx,L/np.maximum(L0,1e-5)); mxabs=np.maximum(mxabs,L-L0)
np.savez(out,E=E,L0=L0,ratio=mx,grow=mxabs)
print("STRETCH", fbx, "edges ratio>2:",int((mx>2).sum()),"ratio>3:",int((mx>3).sum()),"grow>5cm:",int((mxabs>0.05).sum()),"grow>10cm:",int((mxabs>0.10).sum()))
