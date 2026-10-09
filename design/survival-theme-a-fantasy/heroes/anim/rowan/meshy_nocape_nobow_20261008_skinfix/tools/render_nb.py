import bpy, sys, math, os
from mathutils import Vector
import os as _os; WORK=_os.environ.get('ROWAN_FIX_WORK','.')
args = sys.argv[sys.argv.index('--')+1:]
fbx, outdir, tag = args[0], args[1], args[2]
frames = [int(x) for x in args[3].split(',')]
REST = os.environ.get('REST')=='1'
views = args[4].split(',') if len(args)>4 else ['front','q34','rear']
texdir = os.environ['ROWAN_TEXDIR']+'/'
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
for o in list(bpy.data.objects):
    if o.name.startswith('Icosphere'): bpy.data.objects.remove(o)
body=bpy.data.objects['output_unwrapped']; arm=bpy.data.objects['target_character']
# Design's bow prop: idle/walk = static bow on RightHand (meta offset); attack = baked bow + arrow FBXs
import json, mathutils
PACK=os.environ['ROWAN_PACK']; meta=json.load(open(PACK+'/ROWAN_meshy_bow_meta.json'))
before=set(bpy.data.objects)
props=[]
if 'attack' in os.path.basename(fbx):
    for p in ('ROWAN_meshy_bow_attack.fbx','ROWAN_meshy_arrow_attack.fbx'):
        bpy.ops.import_scene.fbx(filepath=PACK+'/'+p)
    bow_static=None
else:
    bpy.ops.import_scene.fbx(filepath=PACK+'/props/ROWAN_bow_meshy.fbx')
    bow_static=[o for o in bpy.data.objects if o not in before and o.parent is None]
    OFF=mathutils.Matrix(meta['bow']['offset_in_RightHand_rest_walk'])
props=[o for o in bpy.data.objects if o not in before]
def place_bow():
    if bow_static:
        M=arm.matrix_world @ arm.pose.bones['mixamorig:RightHand'].matrix @ OFF
        for o in bow_static: o.matrix_world=M
mat = bpy.data.materials.new('paint'); mat.use_nodes=True
nt=mat.node_tree; bsdf=nt.nodes['Principled BSDF']
img=bpy.data.images.load(texdir+'Meshy_AI_Azure_Ranger_biped_texture_0.png')
tn=nt.nodes.new('ShaderNodeTexImage'); tn.image=img
nt.links.new(tn.outputs['Color'], bsdf.inputs['Base Color'])
bsdf.inputs['Roughness'].default_value=0.7
for o in [body]:
    if o.type=='MESH':
        o.data.materials.clear(); o.data.materials.append(mat)
sc=bpy.context.scene
sc.render.engine='BLENDER_EEVEE_NEXT'
sc.render.resolution_x=540; sc.render.resolution_y=960
sc.world=bpy.data.worlds.new('w'); sc.world.color=(0.05,0.06,0.05)
sc.view_settings.view_transform='Standard'
def light(rot,e):
    l=bpy.data.lights.new('l','SUN'); l.energy=e
    ob=bpy.data.objects.new('l',l); sc.collection.objects.link(ob); ob.rotation_euler=[math.radians(r) for r in rot]
light((50,0,20),3.0); light((60,0,200),1.5); light((70,0,-100),1.0)
sc.world.use_nodes=True; sc.world.node_tree.nodes['Background'].inputs[0].default_value=(0.25,0.27,0.25,1); sc.world.node_tree.nodes['Background'].inputs[1].default_value=0.6
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam
cam.data.lens=50
tgt=bpy.data.objects.new('t',None); sc.collection.objects.link(tgt); tgt.location=(0,0,0.88)
c=cam.constraints.new('TRACK_TO'); c.target=tgt; c.track_axis='TRACK_NEGATIVE_Z'; c.up_axis='UP_Y'
# Blender: Meshy character faces -Y after import
yaw={'front':-90,'q34':-45,'rear':90,'side':0,'q34r':-135}
os.makedirs(outdir,exist_ok=True)
if REST:
    for o in bpy.data.objects:
        if o.type=='ARMATURE': o.data.pose_position='REST'
yaw['rsclose']=-90

for f in frames:
    sc.frame_set(f); place_bow(); bpy.context.view_layer.update()
    for v in views:
        a=math.radians(yaw[v]); d=3.0
        cam.location=(d*math.cos(a), d*math.sin(a), 1.5 if v=='rear' else 1.1)
        if v=='side': cam.location=(-d,0,1.1)
        sc.render.filepath=f'{outdir}/{tag}_{v}_f{f:03d}.png'
        bpy.ops.render.render(write_still=True)
