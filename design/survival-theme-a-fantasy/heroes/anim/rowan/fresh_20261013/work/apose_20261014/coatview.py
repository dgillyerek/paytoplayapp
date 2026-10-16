import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,numpy as np,math
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/w2/dec_body.blend')
o=bpy.data.objects['ROWAN_body']; me=o.data
keep=np.load('coat_faces.npy')
red=bpy.data.materials.new('red'); red.diffuse_color=(1,0.1,0.1,1); me.materials.append(red)
mi=np.zeros(len(me.polygons),int); mi[keep]=1; me.polygons.foreach_set('material_index',mi); me.update()
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='MATERIAL'
w=bpy.data.worlds.new('bg'); sc.world=w; w.color=(0.45,0.45,0.45)
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=1.95
sc.render.resolution_x=600; sc.render.resolution_y=600
import sys
tag=sys.argv[-1]
for n,loc,rot in (('front',(0,-5,0.9),(math.pi/2,0,0)),('side',(5,0,0.9),(math.pi/2,0,math.pi/2)),('back',(0,5,0.9),(math.pi/2,0,math.pi)),('lside',(-5,0,0.9),(math.pi/2,0,-math.pi/2))):
    cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'/workspace/rowan_fresh/w2/cv_{n}.png'; bpy.ops.render.render(write_still=True)
