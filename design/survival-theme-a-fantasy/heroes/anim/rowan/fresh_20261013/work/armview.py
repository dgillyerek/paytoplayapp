import bpy,math,numpy as np
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/work/anim.blend')
arm=bpy.data.objects['ROWAN_rig']; arm.animation_data.action=bpy.data.actions['ROWAN_attack']
body=bpy.data.objects['ROWAN_body']; me=body.data; N=len(me.vertices)
gi=[body.vertex_groups[n].index for n in body.vertex_groups.keys() if any(k in n for k in ('Arm','Hand','Shoulder'))]
w=np.zeros(N)
for v in me.vertices:
    for g in v.groups:
        if g.group in gi: w[v.index]+=g.weight
ca=me.color_attributes.new('wc','FLOAT_COLOR','POINT'); ca.data.foreach_set('color',np.c_[w,0.2*np.ones(N),1-w,np.ones(N)].ravel())
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.color_type='VERTEX'; sc.render.resolution_x=600; sc.render.resolution_y=900
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=1.95
for f in (0,10):
    sc.frame_set(f)
    for k,(loc,rot) in {'front':((0,-5,0.9),(math.pi/2,0,0)),'side':((5,0,0.9),(math.pi/2,0,math.pi/2))}.items():
        cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'/workspace/rowan_fresh/work/av_{f}_{k}.png'; bpy.ops.render.render(write_still=True)
