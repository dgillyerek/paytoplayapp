import bpy, sys, json, math, os
from mathutils import Vector
A=sys.argv[sys.argv.index('--')+1:]; frames=[int(x) for x in A[0].split(',')]; outdir=A[1]; rig=A[2]; hide=A[3].split(',') if len(A)>3 and A[3] else []
os.makedirs(outdir,exist_ok=True)
sc=bpy.context.scene; arm=bpy.data.objects[rig]; P='mixamorig:'
for o in bpy.data.objects:
    if o.name in hide: o.hide_render=True
sc.render.engine='BLENDER_WORKBENCH'; sh=sc.display.shading
sh.light='STUDIO'; sh.color_type='SINGLE'; sh.single_color=(0.62,0.64,0.70); sh.show_shadows=True; sh.show_cavity=False
sc.render.resolution_x=420; sc.render.resolution_y=560; sc.render.film_transparent=False
sh.background_type="VIEWPORT"; sh.background_color=(0.62,0.66,0.72)
bpy.ops.mesh.primitive_plane_add(size=8,location=(0,0,0)); g=bpy.context.active_object
mat=bpy.data.materials.new('ground'); mat.diffuse_color=(0.35,0.38,0.33,1); g.data.materials.append(mat)
cam_d=bpy.data.cameras.new('c'); cam_d.lens=50; cam=bpy.data.objects.new('cam',cam_d); sc.collection.objects.link(cam); sc.camera=cam
views={'side':(Vector((3.9,0,1.05)),Vector((0,0,0.95))),'q34':(Vector((2.8,-2.8,1.5)),Vector((0,0,0.95)))}
for f in frames:
    sc.frame_set(f)
    for v,(p,t) in views.items():
        cam_d.lens=50; cam.location=p; cam.rotation_euler=(t-p).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=os.path.join(outdir,f'{v}_{f:03d}.png'); bpy.ops.render.render(write_still=True)
    # bow-hand close-up over the right shoulder, looking down the arrow
    lh=arm.matrix_world@arm.pose.bones[P+'LeftHand'].head; rh=arm.matrix_world@arm.pose.bones[P+'RightHand'].head
    tgt=lh.lerp(rh,0.35); p=tgt+Vector((0.75,0.55,0.45)); cam_d.lens=35
    cam.location=p; cam.rotation_euler=(tgt-p).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=os.path.join(outdir,f'hand_{f:03d}.png'); bpy.ops.render.render(write_still=True)
print('SHEET OK')
