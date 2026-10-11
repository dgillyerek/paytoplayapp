import bpy, sys, os
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]; SHOTS=args[1].split(';'); VW=args[2].split(','); HIDE=args[3].split(',') if len(args)>3 and args[3] else []
sc=bpy.context.scene; arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
if arm.animation_data is None: arm.animation_data_create()
for n in HIDE:
    if n in bpy.data.objects: bpy.data.objects[n].hide_render=True
for o in bpy.data.objects:
    if o.type in ('CAMERA','LIGHT'): o.hide_render=True
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
sc.render.resolution_x=360; sc.render.resolution_y=560
bpy.ops.mesh.primitive_plane_add(size=8,location=(0,0,0)); g=bpy.context.object
m=bpy.data.materials.new('g'); m.diffuse_color=(0.3,0.32,0.3,1); g.data.materials.append(m)
cam=bpy.data.objects.new('c',bpy.data.cameras.new('c')); sc.collection.objects.link(cam); sc.camera=cam
V={'front':((0,-3.8,1.25),(0,-0.1,0.92),50),'q34':((2.6,-2.9,1.45),(0,-0.1,0.92),50),'rearbattle':((0,3.6,2.4),(0,-0.3,0.9),50),
   'side':((3.8,0,1.1),(0,0,0.92),50),'legs':((1.3,-1.9,0.55),(0,0,0.42),50)}
os.makedirs(OUT,exist_ok=True)
for s in SHOTS:
    tag,act,f=s.split(':'); f=int(f)
    if act=='-':
        arm.animation_data.action=None
        for pb in arm.pose.bones: pb.matrix_basis.identity()
    else: arm.animation_data.action=bpy.data.actions[act]
    sc.frame_set(f)
    for v in VW:
        l,t,lens=V[v]; l,t=Vector(l),Vector(t); cam.location=l; cam.rotation_euler=(t-l).to_track_quat('-Z','Y').to_euler(); cam.data.lens=lens
        sc.render.filepath=f'{OUT}/{v}_{tag}_f{f:02d}.png'; bpy.ops.render.render(write_still=True)
