import bpy, numpy as np, sys
body=bpy.data.objects['OAKENSHIELD_body']; arm=bpy.data.objects['OAKENSHIELD_rig']; sc=bpy.context.scene
co0=np.array([v.co[:] for v in body.data.vertices])
r=np.linalg.norm(co0[:,:2]-[0.005,0.03],axis=1)
face=(co0[:,2]>1.60)&(r<0.13)&(co0[:,1]<0)   # front of head: face
hb=arm.data.bones['mixamorig:Head']
for act in ('walk','attack'):
    arm.animation_data.action=bpy.data.actions['OAKENSHIELD_'+act]; worst=0
    for fr in range(0,31,3):
        sc.frame_set(fr); dg=bpy.context.evaluated_depsgraph_get()
        ev=body.evaluated_get(dg); m=ev.to_mesh()
        p=np.array([v.co[:] for v in m.vertices]); ev.to_mesh_clear()
        M=np.array(arm.matrix_world@arm.pose.bones['mixamorig:Head'].matrix@hb.matrix_local.inverted())
        q=(np.c_[co0,np.ones(len(co0))]@M.T)[:,:3]
        d=np.linalg.norm(p[face]-q[face],axis=1); worst=max(worst,d.max())
    print('METRIC',act,'face verts',int(face.sum()),'max dev from rigid head (mm)',round(worst*1000,2))
