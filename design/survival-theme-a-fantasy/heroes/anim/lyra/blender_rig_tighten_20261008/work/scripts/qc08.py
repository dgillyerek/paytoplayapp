"""Rest compare stills + walk frames (with held prop on RightHand) for 2026-10-08 packs."""
import bpy, sys, os, json, math
from mathutils import Vector, Matrix
sys.path.insert(0,'/workspace/attack_20261007'); import atk_lib as AL
TAG=sys.argv[sys.argv.index('--')+1]
R='/workspace/rig_20261008'; B='/workspace/design/survival-theme-a-fantasy/heroes/anim'
C={'rowan':dict(NAME='ROWAN_nocape',RIGDIR=f'{B}/rowan/blender_rig_nocape_20261008'),'lyra':dict(NAME='LYRA_tighten',RIGDIR=f'{B}/lyra/blender_rig_tighten_20261008')}[TAG]
NAME=C['NAME']; OD=f"{C['RIGDIR']}/work/qc_frames"; os.makedirs(OD,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=f"{C['RIGDIR']}/{NAME}_blenderig.blend")
sc=bpy.context.scene; arm=bpy.data.objects[f'{NAME}_rig']; body=bpy.data.objects[f'{NAME}_body']
J=json.load(open(f'{R}/props/{TAG}_props.json')); OFF=Matrix(J['offset_in_hand'])
with bpy.data.libraries.load(f'{R}/props/{TAG}_props.blend') as (s,d): d.objects=list(s.objects)
PO={}
for o in d.objects: sc.collection.objects.link(o); PO[o.name]=o
up=bpy.context.view_layer.update
def place(f):
    sc.frame_set(f); up(); W=arm.matrix_world@arm.pose.bones['mixamorig:RightHand'].matrix@OFF
    if TAG=='rowan':
        br=PO['ROWAN_bow_rig']; br.matrix_world=Matrix.Identity(4); g=br.pose.bones['bow_grip']; g.matrix=W@br.data.bones['bow_grip'].matrix_local
    else: PO['LYRA_staff'].matrix_world=W
    up()
for b in arm.pose.bones: b.rotation_mode='QUATERNION'; b.matrix_basis=Matrix.Identity(4)
arm.animation_data_create(); arm.animation_data.action=None; up()
mn,mx=AL.bbox_world([body]); H=mx.z-mn.z; c=(mn+mx)/2
arm.hide_render=True
bpy.ops.mesh.primitive_plane_add(size=12,location=(0,0,mn.z-0.001)); g=bpy.context.object
gm=bpy.data.materials.new('g'); gm.use_nodes=True; gm.node_tree.nodes['Principled BSDF'].inputs[0].default_value=(0.03,0.032,0.04,1); g.data.materials.append(gm)
cam=AL.setup_scene('EEVEE',(720,960)); sc.eevee.taa_render_samples=32
cam.data.type='ORTHO'; cam.data.ortho_scale=H*1.12
LZ=mn.z+0.5*H
REST={'front':(Vector((c.x,-6,LZ)),Vector((c.x,0,LZ))),'side_left':(Vector((6,c.y,LZ)),Vector((0,c.y,LZ))),'back':(Vector((c.x,6,LZ)),Vector((c.x,0,LZ))),'side_right':(Vector((-6,c.y,LZ)),Vector((0,c.y,LZ)))}
place(0)
for k,(e,l) in REST.items():
    if os.path.exists(f'{OD}/rest_{k}.png') and os.environ.get('SKIPREST'): continue
    cam.location=e; cam.rotation_euler=(l-e).to_track_quat('-Z','Y').to_euler(); AL.render(f'{OD}/rest_{k}.png')
# walk
act=bpy.data.actions.get(f'{NAME}_walk')
for b in arm.pose.bones: b.rotation_mode='XYZ'
arm.animation_data.action=act
print('WALK_ACTION',act.name,act.frame_range[:],len(act.fcurves))
cam=AL.setup_scene('EEVEE',(540,540)); sc.eevee.taa_render_samples=8
V={'rear':((c.x,c.y+1.55*H,mn.z+1.6*H),(c.x,c.y-0.73*H,mn.z+0.28*H)),'side':((c.x+2.0*H,c.y-0.45*H,mn.z+0.6*H),(c.x,c.y-0.45*H,mn.z+0.48*H)),
   'q34front':((c.x-1.15*H,c.y-1.45*H,mn.z+0.8*H),(c.x,c.y-0.2*H,mn.z+0.48*H))}
for f in range(0,30):
    place(f)
    for v in ('rear','q34front','side'):
        if v=='side' and f%3: continue
        p=f'{OD}/walk_{v}_{f:03d}.png'
        if os.path.exists(p) and os.environ.get('SKIPREST'): continue
        AL.aim(cam,*V[v]); AL.render(p)
arm.animation_data.action=None
print('QC_DONE',OD)
