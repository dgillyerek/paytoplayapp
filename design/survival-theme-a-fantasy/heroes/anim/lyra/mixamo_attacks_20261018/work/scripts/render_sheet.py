import bpy, sys, json, math, os
from mathutils import Vector
A=sys.argv[sys.argv.index('--')+1:]; frames=[int(x) for x in A[0].split(',')]; outdir=A[1]; os.makedirs(outdir,exist_ok=True)
sc=bpy.context.scene; arm=bpy.data.objects['LYRA_tighten_rig']
for o in bpy.data.objects:
    if o.name=='LYRA_arcane_bolt_vfx': o.hide_render=True
sc.render.engine='BLENDER_WORKBENCH'; sh=sc.display.shading
sh.light='STUDIO'; sh.color_type='TEXTURE'; sh.show_shadows=True; sh.show_cavity=False
sc.render.resolution_x=420; sc.render.resolution_y=560; sc.render.film_transparent=False
sh.background_type="VIEWPORT"; sh.background_color=(0.62,0.66,0.72)
bpy.ops.mesh.primitive_plane_add(size=8,location=(0,0,0)); g=bpy.context.active_object
mat=bpy.data.materials.new('ground'); mat.diffuse_color=(0.35,0.38,0.33,1); g.data.materials.append(mat)
cam_d=bpy.data.cameras.new('c'); cam_d.lens=50; cam=bpy.data.objects.new('cam',cam_d); sc.collection.objects.link(cam); sc.camera=cam
views={'side':(Vector((3.9,0,1.05)),Vector((0,0,0.95))),'q34':(Vector((2.8,-2.8,1.5)),Vector((0,0,0.95)))}
for f in frames:
    sc.frame_set(f)
    for v,(p,t) in views.items():
        cam.location=p; cam.rotation_euler=(t-p).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=os.path.join(outdir,f'{v}_{f:03d}.png'); bpy.ops.render.render(write_still=True)
print('SHEET OK')
