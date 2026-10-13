import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,math
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; out=args[0]
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='FLAT'; sc.display.shading.color_type='TEXTURE'
sc.render.resolution_x=600; sc.render.resolution_y=900
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam
cam.data.type='ORTHO'; cam.data.ortho_scale=2.1
views={'front':((0,-5,0),(math.pi/2,0,0)),'side':((5,0,0),(math.pi/2,0,math.pi/2)),'back':((0,5,0),(math.pi/2,0,math.pi))}
for k,(loc,rot) in views.items():
    cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'{out}_{k}.png'; bpy.ops.render.render(write_still=True)
