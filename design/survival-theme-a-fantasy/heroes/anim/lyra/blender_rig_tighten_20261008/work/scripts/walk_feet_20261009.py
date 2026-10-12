"""2026-10-09: re-author ONLY Lyra's walk (v3) on Derek's e8b050d rig and re-export the walk FBX.
Usage: blender -b LYRA_tighten_blenderig.blend --python walk_feet_20261009.py -- <out_dir> <report.json>
Bones, rest/attack actions, staff and bolt are not touched."""
import bpy, sys, json
from mathutils import Matrix
L='/workspace/lyra-onefile/design/survival-theme-a-fantasy/heroes/anim/lyra'
OUTD,REPJ=sys.argv[sys.argv.index('--')+1:][:2]
sc=bpy.context.scene
arm=bpy.data.objects['LYRA_tighten_rig']; body=bpy.data.objects['LYRA_tighten_body']
if arm.mode!='OBJECT':
    bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='OBJECT')
snap=lambda: {b.name:(tuple(b.head_local),tuple(b.tail_local),tuple(map(tuple,b.matrix_local))) for b in arm.data.bones}
bones0=snap()
def keys(a):
    out=[]
    for Ly in a.layers:
        for st in Ly.strips:
            for cb in st.channelbags:
                for fc in cb.fcurves: out.append((fc.data_path,fc.array_index,tuple(tuple(k.co) for k in fc.keyframe_points)))
    return sorted(out)
other0={n:keys(bpy.data.actions[n]) for n in ('LYRA_tighten_rest','LYRA_tighten_attack','LYRA_arcane_bolt_attack')}
staff=bpy.data.objects['LYRA_staff']; st0=(staff.parent_bone,tuple(map(tuple,staff.matrix_basis)))
sys.argv=[sys.argv[0],'--',REPJ]
G={'__name__':'walk'}
exec(open(f'{L}/blender_rig_tighten_20261008/work/scripts/walk_v3_author.py').read(),G)
ad=arm.animation_data; ad.action=bpy.data.actions['LYRA_tighten_walk']; sc.frame_set(0)
rep=json.load(open(REPJ))
rep['bones_unchanged']=snap()==bones0
rep['rest_attack_bolt_actions_unchanged']=all(keys(bpy.data.actions[n])==other0[n] for n in other0)
rep['staff_unchanged']=(staff.parent_bone,tuple(map(tuple,staff.matrix_basis)))==st0
rep['feet']={s:dict(yaw_rest=round(G['SOLE'][s]['yaw_rest'],2),yaw_fix=round(G['SOLE'][s]['yaw_fix'],2),target_x=round(G['SOLE'][s]['target_x'],4)) for s in ('Left','Right')}
json.dump(rep,open(REPJ,'w'),indent=0)
assert rep['bones_unchanged'] and rep['rest_attack_bolt_actions_unchanged'] and rep['staff_unchanged']
bpy.ops.wm.save_mainfile(compress=True)
common=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
    mesh_smooth_type='FACE', use_mesh_modifiers=False)
anim=dict(bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
bpy.ops.object.select_all(action='DESELECT')
for o in (arm,body): o.hide_set(False); o.select_set(True)
bpy.context.view_layer.objects.active=arm
bpy.ops.export_scene.fbx(filepath=f'{OUTD}/LYRA_tighten_blenderig_walk.fbx',**anim,**common)
print('WALK3 OK',json.dumps({k:rep[k] for k in ('bones_unchanged','rest_attack_bolt_actions_unchanged','staff_unchanged','feet')}))
