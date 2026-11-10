import bpy, os, sys
kind = sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else 'thrust'
root = r'C:\Development\paytoplayapp'
arm = bpy.data.objects['SERAPHINE_rig']
P='mixamorig:'
ad=arm.animation_data_create(); idle=bpy.data.actions['idle']; ad.action=idle; ad.action_slot=idle.slots[0]

def key(obj, pts):
    obj.animation_data_clear()
    for f,p in pts:
        obj.location=p; obj.keyframe_insert('location',frame=f)

rh=bpy.data.objects['CTRL_attack_right_hand']; lh=bpy.data.objects['CTRL_attack_left_hand']; aim=bpy.data.objects['CTRL_attack_right_aim']
if kind == 'cast':
    key(rh, [(1,(-.40,-.20,1.24)),(12,(-.48,.02,1.42)),(20,(-.34,-.34,1.40)),(28,(-.30,-.50,1.30)),(40,(-.40,-.20,1.24)),(52,(-.40,-.20,1.24))])
    key(lh, [(1,(.23,-.14,.94)),(12,(.22,-.02,1.24)),(20,(.12,-.30,1.24)),(28,(.16,-.38,1.12)),(40,(.23,-.14,.94)),(52,(.23,-.14,.94))])
    key(aim,[(1,(-.40,-1.2,1.44)),(12,(-.48,.55,1.95)),(20,(-.22,-1.20,1.62)),(28,(-.12,-1.45,1.42)),(40,(-.40,-1.2,1.44)),(52,(-.40,-1.2,1.44))])
bpy.context.view_layer.objects.active=arm; arm.select_set(True); bpy.ops.object.mode_set(mode='POSE')
for b in arm.data.bones: b.select=True
bpy.context.scene.frame_set(1)
bpy.ops.nla.bake(frame_start=1, frame_end=52, step=1, only_selected=False, visual_keying=True, clear_constraints=False, clear_parents=False, use_current_action=False, bake_types={'POSE'})
bpy.ops.object.mode_set(mode='OBJECT')
action=ad.action; action.name='attack_'+('throw' if kind=='cast' else 'thrust')
for o in bpy.data.objects:o.select_set(False)
for n in ('SERAPHINE_rig','SERAPHINE_body','SERAPHINE_spear','SERAPHINE_wing_A','SERAPHINE_wing_B'): bpy.data.objects[n].select_set(True)
bpy.context.view_layer.objects.active=arm; s=bpy.context.scene;s.frame_start=1;s.frame_end=52
path=os.path.join(root,'design/survival-theme-a-fantasy/heroes/anim/seraphine/blender/manual_v1',kind+'_candidate.fbx')
bpy.ops.export_scene.fbx(filepath=path,use_selection=True,object_types={'ARMATURE','MESH'},global_scale=1.0,apply_unit_scale=True,apply_scale_options='FBX_SCALE_NONE',axis_forward='-Z',axis_up='Y',primary_bone_axis='Y',secondary_bone_axis='X',add_leaf_bones=False,bake_anim=True,bake_anim_use_all_actions=False,bake_anim_use_nla_strips=False,bake_anim_force_startend_keying=True,bake_anim_simplify_factor=0.0,bake_anim_step=1.0,mesh_smooth_type='FACE',use_armature_deform_only=False,path_mode='AUTO')
print(path)
