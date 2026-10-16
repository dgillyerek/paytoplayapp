import bpy,math
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.color_type='TEXTURE'; sc.render.resolution_x=400; sc.render.resolution_y=600
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=2.0
import numpy as np
c=bpy.data.objects['BONEQUILL_cape'].data; z=np.array([v.co.z for v in c.vertices]); print('ZH',np.histogram(z,bins=9,range=(0,1.8))[0])
for k,(loc,rot) in {'front':((0,-5,0.95),(math.pi/2,0,0)),'side':((5,0,0.95),(math.pi/2,0,math.pi/2)),'back':((0,5,0.95),(math.pi/2,0,math.pi))}.items():
    cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'/workspace/bonequill_fresh/work/c_{k}.png'; bpy.ops.render.render(write_still=True)
