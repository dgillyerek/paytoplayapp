import bpy,math,sys
args=sys.argv[sys.argv.index('--')+1:]
sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'; sc.render.resolution_x=400; sc.render.resolution_y=600
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=2.0
for spec in args:
    a,f=spec.split(':'); arm.animation_data.action=bpy.data.actions[a]; sc.frame_set(int(f))
    for k,(loc,rot) in {'front':((0,-5,0.95),(math.pi/2,0,0)),'side':((5,0,0.95),(math.pi/2,0,math.pi/2))}.items():
        cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'/workspace/bonequill_gritty/work/q_{a}_{f}_{k}.png'; bpy.ops.render.render(write_still=True)
