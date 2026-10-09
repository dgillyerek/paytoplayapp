import bpy, os, shutil, json, sys
OUT='/workspace/design/survival-theme-a-fantasy/heroes/anim/rowan/meshy_nocape_nobow_20261008'
SRC='/workspace/design/survival-theme-a-fantasy/heroes/anim/rowan/meshy_nocape_20261008/attack_zip/Meshy_AI_Azure_Ranger_biped'
ONLY=os.environ.get('ONLY',''); OPT=os.environ.get('SCALEOPT','FBX_SCALE_NONE'); DIR=os.environ.get('DIR',OUT)
sc=bpy.context.scene; sc.render.fps=24; sc.render.fps_base=1.0; arm=bpy.data.objects['target_character']; me=bpy.data.objects['output_unwrapped']
brig=bpy.data.objects['ROWAN_bow_rig']; bow=bpy.data.objects['ROWAN_bow']; ar=bpy.data.objects['ROWAN_arrow_blue_fletch']
# textures
mp={'texture_0':'texture_0','normal':'texture_0_normal','texture_0_roughness.png':'texture_0_roughness','texture_0_metallic.png':'texture_0_metallic'}
os.makedirs(OUT+'/textures',exist_ok=True)
for im in bpy.data.images:
    if im.name in mp:
        fn=f'Meshy_AI_Azure_Ranger_biped_{mp[im.name]}.png'; dst=OUT+'/textures/'+fn
        if not os.path.exists(dst): shutil.copy2(SRC+'/'+fn,dst)
        if im.packed_file: im.packed_files[0] and None
        while im.packed_file: im.packed_file is not None and im.unpack(method='REMOVE')
        im.filepath=dst; im.reload(); print('img',im.name,'->',fn,im.size[:])
def sel(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
def exp(path,objs,types,anim,embed):
    sel(objs)
    bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types=types,apply_unit_scale=True,apply_scale_options=OPT,
      axis_forward='-Z',axis_up='Y',add_leaf_bones=False,use_armature_deform_only=False,primary_bone_axis='Y',secondary_bone_axis='X',
      bake_anim=anim,bake_anim_use_all_bones=True,bake_anim_use_nla_strips=False,bake_anim_use_all_actions=False,bake_anim_force_startend_keying=True,
      bake_anim_step=1.0,bake_anim_simplify_factor=0.0,path_mode='COPY' if embed else 'AUTO',embed_textures=embed,mesh_smooth_type='FACE',use_mesh_modifiers=False)
    print('exported',path,os.path.getsize(path))
def setrange(a): sc.frame_start,sc.frame_end=int(a.frame_range[0]),int(a.frame_range[1])
for clip in ('rest','walk','attack'):
    if ONLY and clip not in ONLY: continue
    a=bpy.data.actions['ROWAN_meshy_'+clip]; arm.animation_data.action=a; setrange(a)
    exp(f'{DIR}/ROWAN_meshy_nobow_{clip}.fbx',[arm,me],{'ARMATURE','MESH'},True,True)
if not ONLY:
    a=bpy.data.actions['ROWAN_meshy_bow_attack']; brig.animation_data.action=a; setrange(a)
    exp(f'{DIR}/ROWAN_meshy_bow_attack.fbx',[brig,bow],{'ARMATURE','MESH'},True,True)
    a=bpy.data.actions['ROWAN_meshy_arrow_attack']; ar.animation_data.action=a; setrange(a)
    exp(f'{DIR}/ROWAN_meshy_arrow_attack.fbx',[ar],{'MESH'},True,True)
    # static props
    brig.animation_data.action=None
    for pb in brig.pose.bones: pb.location=(0,0,0); pb.rotation_quaternion=(1,0,0,0); pb.rotation_euler=(0,0,0); pb.scale=(1,1,1)
    ar.animation_data.action=None; ar.location=(0,0,0); ar.rotation_quaternion=(1,0,0,0); ar.scale=(1,1,1)
    sc.frame_set(1)
    exp(f'{DIR}/props/ROWAN_bow_meshy.fbx',[brig,bow],{'ARMATURE','MESH'},False,True)
    exp(f'{DIR}/props/ROWAN_arrow_blue_fletch_meshy.fbx',[ar],{'MESH'},False,True)
    # restore for blend
    arm.animation_data.action=bpy.data.actions['ROWAN_meshy_attack']; brig.animation_data.action=bpy.data.actions['ROWAN_meshy_bow_attack']; ar.animation_data.action=bpy.data.actions['ROWAN_meshy_arrow_attack']
    setrange(bpy.data.actions['ROWAN_meshy_attack']); sc.frame_set(1)
    bpy.ops.wm.save_as_mainfile(filepath=f'{OUT}/ROWAN_meshy_nocape_nobow.blend',relative_remap=True,compress=True)
    bpy.ops.file.make_paths_relative(); bpy.ops.wm.save_mainfile(compress=True)
    print('blend saved')
