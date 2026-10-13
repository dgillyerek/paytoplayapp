import sys,bpy,math
args=sys.argv[sys.argv.index('--')+1:]; out=args[0]; scale=float(args[1]) if len(args)>1 else 2.1; cz=float(args[2]) if len(args)>2 else 0
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
sc.render.resolution_x=600; sc.render.resolution_y=900
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam
cam.data.type='ORTHO'; cam.data.ortho_scale=scale
for k,(loc,rot) in {'front':((0,-5,cz),(math.pi/2,0,0)),'side':((5,0,cz),(math.pi/2,0,math.pi/2)),'back':((0,5,cz),(math.pi/2,0,math.pi))}.items():
    cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'{out}_{k}.png'; bpy.ops.render.render(write_still=True)
