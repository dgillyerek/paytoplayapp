import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,math
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/w2/dec_body.blend')
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
w=bpy.data.worlds.new('bg'); sc.world=w; w.color=(0.45,0.45,0.45)
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=1.95
sc.render.resolution_x=900; sc.render.resolution_y=900
for n,loc,rot in (('front',(0,-5,0.9),(math.pi/2,0,0)),('side',(5,0,0.9),(math.pi/2,0,math.pi/2)),('back',(0,5,0.9),(math.pi/2,0,math.pi))):
    cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'/workspace/rowan_fresh/w2/raw_{n}.png'; bpy.ops.render.render(write_still=True)
