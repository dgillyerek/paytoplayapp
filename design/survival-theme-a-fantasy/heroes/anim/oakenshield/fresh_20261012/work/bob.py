import bpy, math
from mathutils import Quaternion, Vector
W='/workspace/oakenshield_fresh/work/export'
bpy.ops.wm.open_mainfile(filepath=W+'/OAKENSHIELD_fresh.blend')
sc=bpy.context.scene; arm=bpy.data.objects['OAKENSHIELD_rig']; body=bpy.data.objects['OAKENSHIELD_body']
A=bpy.data.actions['OAKENSHIELD_walk']; arm.animation_data.action=A; pb=arm.pose.bones['mixamorig:Head']
R=pb.bone.matrix_local.to_3x3()
for f in range(31):
    sc.frame_set(f); ph=2*math.pi*f/30
    nod=1.25*(1-math.cos(2*ph))     # 0..2.5 deg forward, peaks at the two footfalls (f7.5, f22.5)
    q=Quaternion(Vector((1,0,0)),math.radians(nod))
    pb.rotation_quaternion=(R.inverted()@q.to_matrix()@R).to_quaternion()@pb.rotation_quaternion
    pb.keyframe_insert('rotation_quaternion',frame=f)
for o in bpy.context.view_layer.objects: o.select_set(o in (arm,body))
bpy.context.view_layer.objects.active=arm; sc.frame_start=0; sc.frame_end=30
bpy.ops.export_scene.fbx(filepath=W+'/OAKENSHIELD_blenderig_walk.fbx', use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y',
  apply_scale_options='FBX_SCALE_ALL', apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, mesh_smooth_type='FACE',
  use_armature_deform_only=False, use_mesh_modifiers=True, bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
  bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
arm.animation_data.action=bpy.data.actions['OAKENSHIELD_rest']; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=W+'/OAKENSHIELD_fresh.blend',compress=True)
print('BOB ok')
