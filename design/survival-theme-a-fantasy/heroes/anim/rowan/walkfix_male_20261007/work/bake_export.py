"""Rowan male walk rebake (10-05 method: new procedural walk authored from identity rest,
local-euler keys, frame30==frame0) on ROWAN_male_blenderig.blend. Source blend is opened read-only
(never saved); output goes to walkfix_male_20261007/. Export = hum_pipeline stage_export walk settings."""
import bpy, sys, os, math, json, importlib.util
BASE='/workspace/design/survival-theme-a-fantasy'
SRC=f'{BASE}/heroes/anim/rowan/blender_rig_male_20261007/ROWAN_male_blenderig.blend'
OUT=f'{BASE}/heroes/anim/rowan/walkfix_male_20261007'
os.makedirs(OUT, exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=SRC)
# load the 10-05 authoring functions verbatim (module has no side effects beyond makedirs of its own existing OUT)
spec=importlib.util.spec_from_file_location('rb', f'{BASE}/handoffs/anim_previews_20261005/_rebake_clips.py')
rb=importlib.util.module_from_spec(spec); spec.loader.exec_module(rb)
arm=bpy.data.objects['ROWAN_male_rig']; me=bpy.data.objects['ROWAN_male_body']
old=bpy.data.actions.get('ROWAN_male_walk')
print('OLD_ACTION', old.name if old else None, old.frame_range[:] if old else None)
act=rb.make_action(arm,'ROWAN_male_walk')   # same action/take name as the original clip (in-memory only)
rb.author_humanoid_walk(arm)
travels, mean=rb.measure_limb_travel(arm,'humanoid_walk')
vert=rb.measure_mesh_vert_travel([me],arm)
print('TRAVEL', {k:round(v,3) for k,v in travels.items()}, round(mean,3), round(vert,3))
# ---- export: identical to hum_pipeline.stage_export walk call ----
for im in bpy.data.images:
    if im.source in {'VIEWER','GENERATED'} or im.name in {'Render Result','Viewer Node'}: continue
    if not im.filepath_raw: im.filepath_raw=f'//textures/{im.name}.jpg'
    if im.packed_file is None:
        try: im.pack()
        except Exception: pass
    print('IMG', im.name, im.packed_file is not None)
common=dict(use_selection=True, object_types={'ARMATURE','MESH'},
    axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False,
    path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
    mesh_smooth_type='FACE', use_mesh_modifiers=False)
bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); me.select_set(True)
bpy.context.view_layer.objects.active=arm
arm.animation_data.action=act
sc=bpy.context.scene; sc.frame_start=0; sc.frame_end=30; sc.render.fps=30
fbx=os.path.join(OUT,'ROWAN_male_blenderig_walk.fbx')
bpy.ops.export_scene.fbx(filepath=fbx, bake_anim=True,
    bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **common)
print('EXPORTED', fbx, os.path.getsize(fbx))
# work copy (new file in OUT; source blend untouched)
bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT,'work','ROWAN_male_walkfix_work.blend'), copy=True, compress=True)
json.dump({'limb_travels':travels,'mean_limb_travel':mean,'vert_mean_travel':vert,'blender':bpy.app.version_string},
          open('/workspace/attack_20261007/walkfix/bake_meta.json','w'), indent=1, default=float)
