import bpy, sys, json, os
from mathutils import Matrix
TAG=sys.argv[sys.argv.index('--')+1]
R='/workspace/rig_20261008'; B='/workspace/design/survival-theme-a-fantasy/heroes/anim'
OUT={'rowan':f'{B}/rowan/blender_rig_nocape_20261008','lyra':f'{B}/lyra/blender_rig_tighten_20261008'}[TAG]
os.makedirs(f'{OUT}/props',exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=f'{R}/props/{TAG}_props.blend')
J=json.load(open(f'{R}/props/{TAG}_props.json'))
objs=[o for o in bpy.data.objects]
for o in objs:
    if o.parent is None: o.matrix_world=Matrix.Identity(4)
bpy.ops.object.select_all(action='DESELECT')
for o in objs: o.select_set(True)
bpy.context.view_layer.objects.active=[o for o in objs if o.type=='ARMATURE'][0] if TAG=='rowan' else objs[0]
name='ROWAN_bow' if TAG=='rowan' else 'LYRA_staff'
bpy.ops.export_scene.fbx(filepath=f'{OUT}/props/{name}.fbx',use_selection=True,object_types={'ARMATURE','MESH'},axis_forward='-Z',axis_up='Y',apply_unit_scale=True,
    apply_scale_options='FBX_SCALE_ALL',add_leaf_bones=False,mesh_smooth_type='FACE',bake_anim=False,path_mode='COPY',embed_textures=True,use_armature_deform_only=False)
bpy.ops.wm.save_as_mainfile(filepath=f'{OUT}/props/{name}_props.blend',compress=True,copy=True)
meta={'prop':name,'fbx':f'{OUT}/props/{name}.fbx','faces':J['faces'],'verts':J['verts'],'frame':'origin at grip centre, +Z long axis (bow: lower->upper nock; staff: foot->crystal), local -Y = shoot/cast direction',
 'parent_bone':J['hand_bone'],'offset_in_hand_blender':J['offset_in_hand'],'rest_world_blender':J['rest_world'],'bridge_through_fist':{'p0':J['bridge'][0],'p1':J['bridge'][1],'r0':J['bridge'][2],'r1':J['bridge'][3]}}
if TAG=='rowan': meta.update(bones=['bow_grip (root, at grip)','bow_nock (child, on string at grip height; translate local +Y = draw in metres, string weights fall off linearly to the nocks)'],nock_local=J['nock_local'],string_local=J['string_local'])
json.dump(meta,open(f'{OUT}/props/{name}_meta.json','w'),indent=1)
print('RIGPROPS',meta['fbx'])
