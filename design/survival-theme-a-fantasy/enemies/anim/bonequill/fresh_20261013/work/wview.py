import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,math,numpy as np
body=bpy.data.objects['BONEQUILL_body']; me=body.data
names=[g.name for g in body.vertex_groups]; N=len(me.vertices)
W=np.zeros((N,len(names)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
rng=np.random.default_rng(3); pal=rng.random((len(names),3))*0.8+0.2
col=W@pal
ca=me.color_attributes.new('wcol','FLOAT_COLOR','POINT'); ca.data.foreach_set('color',np.c_[col,np.ones(N)].ravel())
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='VERTEX'
sc.render.resolution_x=600; sc.render.resolution_y=900
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam
cam.data.type='ORTHO'; cam.data.ortho_scale=1.95
for k,(loc,rot) in {'front':((0,-5,0.9),(math.pi/2,0,0)),'side':((5,0,0.9),(math.pi/2,0,math.pi/2)),'back':((0,5,0.9),(math.pi/2,0,math.pi))}.items():
    cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'/workspace/bonequill_fresh/work/w_{k}.png'; bpy.ops.render.render(write_still=True)
