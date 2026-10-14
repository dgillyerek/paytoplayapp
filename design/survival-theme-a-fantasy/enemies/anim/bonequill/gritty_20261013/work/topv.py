import bpy,math,sys
sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; arm.animation_data.action=bpy.data.actions['BONEQUILL_attack']; sc.frame_set(10)
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.color_type='TEXTURE'; sc.render.resolution_x=400; sc.render.resolution_y=400
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=1.4
cam.location=(0,-0.2,4); cam.rotation_euler=(0,0,0); sc.render.filepath=sys.argv[-1]; bpy.ops.render.render(write_still=True)
