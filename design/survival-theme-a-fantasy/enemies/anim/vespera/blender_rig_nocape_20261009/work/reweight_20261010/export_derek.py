import bpy, sys, json, os
from mathutils import Vector
OUT=sys.argv[sys.argv.index('--')+1]; os.makedirs(OUT,exist_ok=True)
sc=bpy.context.scene; sc.render.fps=30
if bpy.context.object and bpy.context.object.mode!='OBJECT': bpy.ops.object.mode_set(mode='OBJECT')
arm=bpy.data.objects['VESPERA_rig']; body=bpy.data.objects['VESPERA_body']; me=body.data
rep={'bones':[b.name for b in arm.data.bones],'roots':[b.name for b in arm.data.bones if b.parent is None]}
for im in bpy.data.images:
    if im.source in {'VIEWER','GENERATED'} or im.name in {'Render Result','Viewer Node'}: continue
    if im.packed_file is None:
        try: im.pack()
        except Exception as ex: print('PACKFAIL',im.name,ex)
def sel():
    bpy.ops.object.select_all(action='DESELECT')
    for o in (arm,body): o.select_set(True)
    bpy.context.view_layer.objects.active=arm
hum=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
    mesh_smooth_type='FACE', use_mesh_modifiers=False)
reb=dict(use_selection=True, object_types={'ARMATURE','MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, path_mode='COPY', embed_textures=True,
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL')
A={k:bpy.data.actions['VESPERA_nocape_'+k] for k in ('rest','walk','attack')}
def reset():
    arm.animation_data.action=None
    for pb in arm.pose.bones: pb.matrix_basis.identity()
reset(); sc.frame_set(0)
def U(v): return [round(-v.x,5),round(v.z,5),round(-v.y,5)]
cal={n:U(arm.matrix_world@arm.pose.bones['mixamorig:'+n].head) for n in ('Spine1','Head','LeftHand','RightHand')}
arm.animation_data.action=A['attack']; sc.frame_set(13); pb=arm.pose.bones['mixamorig:RightHand']
spawn=arm.matrix_world@pb.tail+Vector((0,-0.12,0))
rep['calibration_unity']=cal; rep['bolt_spawn_unity_f13']=U(spawn)
reset(); sc.frame_set(0)
sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/VESPERA_nocape.fbx', bake_anim=False, **hum)
for tag in ('walk','attack'):
    reset(); arm.animation_data.action=A[tag]; sc.frame_start=0; sc.frame_end=30
    sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/VESPERA_nocape_{tag}.fbx', **reb)
reset(); arm.animation_data.action=A['walk']; sc.frame_start=0; sc.frame_end=30; sc.frame_set(0)
bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); bpy.context.view_layer.objects.active=arm
bpy.ops.wm.save_as_mainfile(filepath=f'{OUT}/VESPERA_nocape.blend', compress=True)
rep['faces']=len(me.polygons); rep['verts']=len(me.vertices); rep['blender']=bpy.app.version_string
rep['max_influences']=max(len(v.groups) for v in me.vertices)
json.dump(rep,open(f'{OUT}/export.json','w'),indent=1); print('EXPORT',json.dumps(rep))
