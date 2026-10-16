import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, os, subprocess
W='/workspace/bonequill_fresh/work'; OUT=W+'/export'; fbm=OUT+'/BONEQUILL_cape.fbm'; os.makedirs(fbm,exist_ok=True)
bpy.ops.wm.open_mainfile(filepath=W+'/cape.blend')
cape=bpy.data.objects['BONEQUILL_cape']; crig=bpy.data.objects['BONEQUILL_cape_rig']
for o in list(bpy.data.objects):
    if o not in (cape,crig): bpy.data.objects.remove(o)
def src(sock):
    l=sock.links
    while l:
        n=l[0].from_node
        if n.type=='TEX_IMAGE': return n.image
        ins=[i for i in n.inputs if i.is_linked]; l=ins[0].links if ins else None
mat=cape.data.materials[0]; b=[n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'][0]
for r,sock,fmt,ext in (('basecolor_0','Base Color','JPEG','.jpg'),('metal_rough_1','Roughness','PNG','.png'),('normal_2','Normal','JPEG','.jpg')):
    im=src(b.inputs[sock]) or (src(b.inputs['Metallic']) if r.startswith('metal') else None); fn=f'BONEQUILL_cape_{r}{ext}'; p=f'{fbm}/{fn}'
    im.name=fn; raw=p.rsplit('.',1)[0]+'_raw.png'; im.filepath_raw=raw; im.file_format='PNG'; im.save(filepath=raw)
    subprocess.run(['python3','-c',f"from PIL import Image; im=Image.open('{raw}').convert('RGB'); im.save('{p}',quality=90) if '{fmt}'=='JPEG' else im.save('{p}',optimize=True)"],check=True); os.remove(raw)
    if im.packed_file: im.unpack(method='REMOVE')
    im.filepath=p; im.reload()
bpy.context.view_layer.objects.active=crig
for o in bpy.context.scene.objects: o.select_set(o in (cape,crig))
bpy.context.view_layer.objects.active=crig
bpy.ops.export_scene.fbx(filepath=f'{OUT}/BONEQUILL_cape.fbx', bake_anim=False, use_mesh_modifiers=False, use_selection=True, object_types={'ARMATURE','MESH'},
    axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL', apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY',
    embed_textures=True, mesh_smooth_type='FACE', use_armature_deform_only=False)
print('CAPEX ok')
