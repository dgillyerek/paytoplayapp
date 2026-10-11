"""Apply optimised right-arm attack keys (RightArm/RightForeArm/RightHand) to LYRA_tighten_attack, re-key bolt, sample, save, export attack FBX."""
import bpy, sys, json, numpy as np
from mathutils import Matrix, Vector, Euler
args=sys.argv[sys.argv.index('--')+1:]
XN=args[0]; META=args[1]; OUTFBX=args[2]; SAMPLES=args[3]; SAVE=args[4]=='save'
X=np.load(XN)
sc=bpy.context.scene; arm=bpy.data.objects['LYRA_tighten_rig']; staff=bpy.data.objects['LYRA_staff']; bolt=bpy.data.objects['LYRA_arcane_bolt_vfx']
if arm.mode!='OBJECT':
    bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='OBJECT')
P='mixamorig:'
bone_snapshot={b.name:(tuple(b.head_local),tuple(b.tail_local),tuple(map(tuple,b.matrix_local))) for b in arm.data.bones}
def FC(a):
    out=[]
    for Ly in a.layers:
        for st in Ly.strips:
            for cb in st.channelbags: out+=[(cb,fc) for fc in cb.fcurves]
    return out
def snap(a): return {(fc.data_path,fc.array_index):[tuple(k.co)+tuple(k.handle_left)+tuple(k.handle_right) for k in fc.keyframe_points] for cb,fc in FC(a)}
before={a.name:snap(a) for a in bpy.data.actions}
act=bpy.data.actions['LYRA_tighten_attack']; ad=arm.animation_data
cur={}
for cb,fc in FC(act):
    if fc.data_path.startswith('pose.bones["'): cur[(fc.data_path.split('"')[1],fc.data_path.rsplit('.',1)[1],fc.array_index)]=fc
def rv2q(v):
    v=Vector(v); a=v.length
    from mathutils import Quaternion
    return Quaternion((1,0,0,0)) if a<1e-12 else Quaternion(v/a,a)
for bn,k in ((P+'RightArm',0),(P+'RightForeArm',1),(P+'RightHand',2)):
    assert arm.pose.bones[bn].rotation_mode=='XYZ', arm.pose.bones[bn].rotation_mode
    prev=None
    for f in range(31):
        q=rv2q(X[f,3*k:3*k+3]); e=q.to_euler('XYZ',prev) if prev else q.to_euler('XYZ'); prev=e
        for i in range(3):
            fc=cur[(bn,'rotation_euler',i)]
            kp=[kk for kk in fc.keyframe_points if abs(kk.co[0]-f)<1e-4][0]
            dv=e[i]-kp.co[1]; kp.co[1]=e[i]; kp.handle_left[1]+=dv; kp.handle_right[1]+=dv
for fc in cur.values(): fc.update()
ameta=json.load(open(META))
CRY=Vector(ameta['props']['staff']['crystal_local']); REL=ameta['props']['bolt']['release']; SPD=ameta['props']['bolt']['speed_mps']
walk_saved=ad.action
ad.action=act; sc.frame_set(REL); spawn=staff.matrix_world@CRY
old_spawn=list(ameta['props']['bolt']['spawn_world'])
for cb,fc in FC(bolt.animation_data.action):
    if fc.data_path=='location':
        for kk in fc.keyframe_points:
            t=max(0,kk.co[0]-REL); v=spawn[fc.array_index]+(-SPD*t/30.0 if fc.array_index==1 else 0.0)
            dv=v-kk.co[1]; kk.co[1]=v; kk.handle_left[1]+=dv; kk.handle_right[1]+=dv
        fc.update()
S={}
ad.action=act; st=[];hd=[];cr=[];bl=[]
for f in range(31):
    sc.frame_set(f); st.append([list(r) for r in staff.matrix_world]); cr.append(list(staff.matrix_world@CRY)); bl.append(list(bolt.matrix_world.translation))
S['staff']=st; S['crystal']=cr; S['bolt']=bl; S['spawn']=list(spawn); S['old_spawn']=old_spawn
S['rest_heads']={b.name:list(b.head_local) for b in arm.data.bones}
after={a.name:snap(a) for a in bpy.data.actions}
S['changed_actions']=[n for n in after if after[n]!=before.get(n)]
S['bones_unchanged']=all(bone_snapshot[b.name]==(tuple(b.head_local),tuple(b.tail_local),tuple(map(tuple,b.matrix_local))) for b in arm.data.bones)
# which attack curves changed
S['changed_attack_curves']=sorted(set(k[0] for k in after['LYRA_tighten_attack'] if after['LYRA_tighten_attack'][k]!=before['LYRA_tighten_attack'][k]))
json.dump(S,open(SAMPLES,'w'))
ad.action=bpy.data.actions['LYRA_tighten_walk']; sc.frame_set(0)
if SAVE:
    ameta['props']['bolt']['spawn_world']=list(spawn)
    json.dump(ameta,open(META,'w'),indent=1)
    bpy.ops.wm.save_mainfile(compress=True)
    common=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
        apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
        mesh_smooth_type='FACE', use_mesh_modifiers=False)
    anim=dict(bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
    body=bpy.data.objects['LYRA_tighten_body']
    bpy.ops.object.select_all(action='DESELECT')
    for o in (arm,body): o.hide_set(False); o.select_set(True)
    bpy.context.view_layer.objects.active=arm
    ad.action=act; sc.frame_set(0)
    bpy.ops.export_scene.fbx(filepath=OUTFBX,**anim,**common)
print('APPLY OK',json.dumps({k:S[k] for k in ('spawn','old_spawn','changed_actions','bones_unchanged','changed_attack_curves')}))
