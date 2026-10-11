import bpy, sys, os
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]; PRE=args[1]   # PRE: action prefix
sc=bpy.context.scene; arm=bpy.data.objects['VESPERA_rig']
if arm.animation_data is None: arm.animation_data_create()
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'; sc.render.resolution_x=360; sc.render.resolution_y=560
w=sc.world or bpy.data.worlds.new('w'); sc.world=w; w.use_nodes=True; w.node_tree.nodes['Background'].inputs[0].default_value=(0.3,0.3,0.32,1); w.node_tree.nodes['Background'].inputs[1].default_value=1.6
bpy.ops.mesh.primitive_plane_add(size=8,location=(0,0,0)); g=bpy.context.object
m=bpy.data.materials.new('g'); m.use_nodes=True; m.node_tree.nodes['Principled BSDF'].inputs['Base Color'].default_value=(0.35,0.37,0.35,1); g.data.materials.append(m)
sun=bpy.data.objects.new('s',bpy.data.lights.new('s','SUN')); sun.data.energy=3; sun.rotation_euler=(0.7,0.2,0.6); sc.collection.objects.link(sun)
cam=bpy.data.objects.new('c',bpy.data.cameras.new('c')); sc.collection.objects.link(cam); sc.camera=cam
V={'front':((0,-3.8,1.25),(0,-0.15,0.92)),'q34':((2.6,-2.9,1.45),(0,-0.15,0.92)),'rearbattle':((0,3.6,2.4),(0,-0.3,0.9)),'hand':None}
ONLY=args[2].split(',') if len(args)>2 else None
os.makedirs(OUT,exist_ok=True)
shots=[('walk',PRE+'walk',f) for f in (0,8,15,23)]+[('attack',PRE+'attack',f) for f in (0,4,7,9,11,13,17,22,26)]
for tag,act,f in shots:
    if ONLY and f'{tag}{f}' not in ONLY: continue
    arm.animation_data.action=bpy.data.actions[act] if act else None
    if act is None:
        for pb in arm.pose.bones: pb.matrix_basis.identity()
    sc.frame_set(f)
    for v,(l,t) in [(k,(x if x else ((0,0,0),(0,0,0)))) for k,x in V.items()]:
        if v=='hand':
            h=arm.matrix_world@arm.pose.bones['mixamorig:RightHand'].head; t=h+Vector((0,0,-0.03)); l=t+Vector((-0.75,-0.75,0.25))
        l,t=Vector(l),Vector(t); cam.location=l; cam.rotation_euler=(t-l).to_track_quat('-Z','Y').to_euler(); cam.data.lens=50 if v in ('front','q34','rearbattle') else 85
        sc.render.filepath=f'{OUT}/{v}_{tag}_f{f:02d}.png'; bpy.ops.render.render(write_still=True)
