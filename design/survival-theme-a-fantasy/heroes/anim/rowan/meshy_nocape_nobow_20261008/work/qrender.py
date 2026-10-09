import bpy, sys, json, math
from mathutils import Vector
a=sys.argv[sys.argv.index('--')+1:]
clip, frames, views, outdir, eng, res = a[0], [int(x) for x in a[1].split(',')], a[2].split(','), a[3], a[4], int(a[5])
sc=bpy.context.scene; arm=bpy.data.objects['target_character']; brig=bpy.data.objects['ROWAN_bow_rig']; ar=bpy.data.objects['ROWAN_arrow_blue_fletch']
arm.animation_data.action=bpy.data.actions['ROWAN_meshy_'+clip]; brig.animation_data.action=bpy.data.actions['ROWAN_meshy_bow_'+clip]
ar.hide_render = clip!='attack'
H=1.707; c=Vector((0.0792,-0.1123,0)) if clip=='attack' else Vector((0,0,0))
if eng=='WB':
    sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
else:
    sc.render.engine='BLENDER_EEVEE_NEXT'; sc.eevee.taa_render_samples=8
    w=bpy.data.worlds.new('w'); sc.world=w; w.use_nodes=True; w.node_tree.nodes['Background'].inputs[0].default_value=(0.55,0.58,0.62,1); w.node_tree.nodes['Background'].inputs[1].default_value=1.0
    for nm,rot,en in (('key',(math.radians(50),0,math.radians(30)),3.5),('fill',(math.radians(60),0,math.radians(200)),1.5)):
        ld=bpy.data.lights.new(nm,'SUN'); ld.energy=en; lo=bpy.data.objects.new(nm,ld); lo.rotation_euler=rot; sc.collection.objects.link(lo)
# ground
bpy.ops.mesh.primitive_plane_add(size=60,location=(0,0,0)); g=bpy.context.object
gm=bpy.data.materials.new('g'); gm.diffuse_color=(0.35,0.38,0.33,1); gm.use_nodes=True; gm.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.3,0.33,0.28,1); g.data.materials.append(gm)
sc.render.resolution_x=res; sc.render.resolution_y=int(res*4/3); sc.render.film_transparent=False
cd=bpy.data.cameras.new('c'); cam=bpy.data.objects.new('c',cd); sc.collection.objects.link(cam); sc.camera=cam
def place(v):
    if v=='rear':  eye=Vector((c.x,c.y+1.55*H,1.6*H)); look=Vector((c.x,c.y-0.73*H,0.28*H)); cd.lens=50
    elif v=='side': eye=c+Vector((4.2,0,1.0)); look=c+Vector((0,0,0.9)); cd.lens=50
    elif v=='q34': eye=c+Vector((-2.9,-3.0,1.4)); look=c+Vector((0,0,0.9)); cd.lens=50
    elif v=='rearwide': eye=Vector((c.x,c.y+2.2*H,1.9*H)); look=Vector((c.x,c.y-1.8*H,0.2*H)); cd.lens=40
    d=look-eye; cam.location=eye; cam.rotation_euler=d.to_track_quat('-Z','Y').to_euler()
for v in views:
    place(v)
    for f in frames:
        sc.frame_set(f); sc.render.filepath=f'{outdir}/{clip}_{v}_{f:03d}.png'; bpy.ops.render.render(write_still=True)
