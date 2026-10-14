import bpy,math,sys
bpy.ops.wm.open_mainfile(filepath=sys.argv[-2]); out=sys.argv[-1]
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'; sc.display.shading.show_backface_culling=True
sc.render.resolution_x=600; sc.render.resolution_y=900
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=1.95
for k,(loc,rot) in {'front':((0,-5,0.9),(math.pi/2,0,0)),'side':((5,0,0.9),(math.pi/2,0,math.pi/2)),'back':((0,5,0.9),(math.pi/2,0,math.pi))}.items():
    cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'{out}_{k}.png'; bpy.ops.render.render(write_still=True)
