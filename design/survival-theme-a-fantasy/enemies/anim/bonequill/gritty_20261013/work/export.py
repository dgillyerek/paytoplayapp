import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, os, json
W='/workspace/bonequill_gritty/work'; OUT=W+'/export'; os.makedirs(OUT+'/BONEQUILL_blenderig.fbm',exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=W+'/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; body=bpy.data.objects['BONEQUILL_body']
def src(sock):
    l=sock.links
    while l:
        n=l[0].from_node
        if n.type=='TEX_IMAGE': return n.image
        ins=[i for i in n.inputs if i.is_linked]; l=ins[0].links if ins else None
def bake(obj,matname,prefix,fbm):
    global bsdf
    os.makedirs(fbm,exist_ok=True); mat=obj.data.materials[0]; mat.name=matname; bsdf=[n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'][0]
    roles={'basecolor_0':(src(bsdf.inputs['Base Color']),'JPEG','.jpg'),'metal_rough_1':(src(bsdf.inputs['Roughness']) or src(bsdf.inputs['Metallic']),'PNG','.png'),
           'normal_2':(src(bsdf.inputs['Normal']),'JPEG','.jpg')}
    rep={}
    for r,(im,fmt,ext) in roles.items():
        fn=f'{prefix}_{r}{ext}'; p=f'{fbm}/{fn}'
        im.name=fn; raw=p.rsplit('.',1)[0]+'_raw.png'; im.filepath_raw=raw; im.file_format='PNG'; im.save(filepath=raw)
        import subprocess; subprocess.run(['python3','-c',f"from PIL import Image; im=Image.open('{raw}').convert('RGB'); im.save('{p}',quality=90) if '{fmt}'=='JPEG' else im.save('{p}',optimize=True); print(im.size)"],check=True); os.remove(raw)
        if im.packed_file: im.unpack(method='REMOVE')
        im.filepath=p; im.reload(); rep[r]=fn
rep={}
bake(body,'BONEQUILL_fresh','BONEQUILL_fresh',OUT+'/BONEQUILL_blenderig.fbm')
def sel():
    for o in bpy.context.view_layer.objects: o.select_set(False)
    for o in (arm,body): o.select_set(True)
    bpy.context.view_layer.objects.active=arm
base=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, mesh_smooth_type='FACE', use_armature_deform_only=False)
A={k:bpy.data.actions['BONEQUILL_'+k] for k in ('rest','walk','attack')}
def reset():
    arm.animation_data.action=None
    for pb in arm.pose.bones: pb.location=(0,0,0); pb.rotation_quaternion=(1,0,0,0); pb.scale=(1,1,1)
    bpy.context.view_layer.update()
reset(); arm.animation_data.action=A['rest']; sc.frame_set(0)
sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/BONEQUILL_blenderig.fbx', bake_anim=False, use_mesh_modifiers=False, **base)
for tag in ('walk','attack'):
    reset(); arm.animation_data.action=A[tag]; sc.frame_start=0; sc.frame_end=30
    sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/BONEQUILL_blenderig_{tag}.fbx', use_mesh_modifiers=True, bake_anim=True, bake_anim_use_all_bones=True,
        bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False, bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, **base)
reset(); arm.animation_data.action=A['rest']; sc.frame_set(0)
for im in bpy.data.images:
    if im.source=='FILE' and not im.packed_file: im.pack()
bpy.ops.wm.save_as_mainfile(filepath=f'{OUT}/BONEQUILL_gritty.blend',compress=True)
with bpy.data.libraries.load(W+'/cape.blend') as (s_,d_): d_.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d_.objects: sc.collection.objects.link(o)
cape,crig=d_.objects; bake(cape,'BONEQUILL_cape','BONEQUILL_cape',OUT+'/BONEQUILL_cape.fbm')
for o in bpy.context.view_layer.objects: o.select_set(False)
cape.select_set(True); crig.select_set(True); bpy.context.view_layer.objects.active=crig
bpy.ops.export_scene.fbx(filepath=f'{OUT}/BONEQUILL_cape.fbx', bake_anim=False, use_mesh_modifiers=False, **base)
rep['blender']=bpy.app.version_string; json.dump(rep,open(W+'/export.json','w'),indent=1); print('EXPORT',rep)
