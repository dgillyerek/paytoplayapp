"""Rest + walk bake for 2026-10-08 packs. Walk = 10-05 author_humanoid_walk (identity rest, local XYZ euler,
f0..f30, f30==f0) loaded verbatim from handoffs/anim_previews_20261005/_rebake_clips.py.
Export = hum_pipeline.stage_export rest + walk settings."""
import bpy, sys, os, importlib.util, json
from mathutils import Matrix
a=sys.argv[sys.argv.index('--')+1:]; NAME,OUT=a[0],a[1]
BASE='/workspace/design/survival-theme-a-fantasy'
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT,f'{NAME}_blenderig.blend'))
spec=importlib.util.spec_from_file_location('rb', f'{BASE}/handoffs/anim_previews_20261005/_rebake_clips.py')
rb=importlib.util.module_from_spec(spec); spec.loader.exec_module(rb)
arm=bpy.data.objects[f'{NAME}_rig']; me=bpy.data.objects[f'{NAME}_body']
for x in list(bpy.data.actions): bpy.data.actions.remove(x)
# atlas textures -> files
td=os.path.join(OUT,'textures'); os.makedirs(td,exist_ok=True)
for im in bpy.data.images:
    if im.source in {'VIEWER','GENERATED'} or im.name in {'Render Result','Viewer Node'}: continue
    p=os.path.join(td,im.name+'.png'); im2=im.copy(); im2.filepath_raw=p; im2.file_format='PNG'
    im2.save(); bpy.data.images.remove(im2); print('TEX',p)
    if im.packed_file is None:
        try: im.pack()
        except Exception: pass
common=dict(use_selection=True, object_types={'ARMATURE','MESH'},
    axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False,
    path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
    mesh_smooth_type='FACE', use_mesh_modifiers=False)
# rest
if arm.animation_data: arm.animation_data.action=None
rb.clear_pose(arm)
bpy.ops.object.select_all(action='DESELECT'); arm.select_set(True); me.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,f'{NAME}_blenderig.fbx'), bake_anim=False, **common)
# walk
act=rb.make_action(arm,f'{NAME}_walk'); rb.author_humanoid_walk(arm)
tr,mean=rb.measure_limb_travel(arm,'humanoid_walk'); print('TRAVEL',{k:round(v,3) for k,v in tr.items()},round(mean,3))
arm.animation_data.action=act
sc=bpy.context.scene; sc.frame_start=0; sc.frame_end=30; sc.render.fps=30
bpy.ops.export_scene.fbx(filepath=os.path.join(OUT,f'{NAME}_blenderig_walk.fbx'), bake_anim=True,
    bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **common)
# leave blend at rest pose with walk action stored (fake user)
arm.animation_data.action=None; rb.clear_pose(arm); sc.frame_set(0)
bpy.ops.wm.save_mainfile(compress=True)
print('DONE')
