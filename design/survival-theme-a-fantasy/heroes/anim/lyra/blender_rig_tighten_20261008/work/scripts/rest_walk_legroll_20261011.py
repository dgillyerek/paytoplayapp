"""Rest + walk: roll LeftUpLeg and LeftLeg 35 deg inward about their own long axis (same as the attack's leg_roll),
keeping every joint position and every other bone's armature-space pose. Rewrites only LeftUpLeg/LeftLeg/LeftFoot keys
of LYRA_tighten_rest and LYRA_tighten_walk. Exports rest (posed, no anim) and walk FBX."""
import bpy, sys, json, math
from mathutils import Matrix
A=sys.argv[sys.argv.index('--')+1:]; ROLL=float(A[0]); OUTB=A[1]; OUTD=A[2]
P='mixamorig:'; sc=bpy.context.scene; arm=bpy.data.objects['LYRA_tighten_rig']; body=bpy.data.objects['LYRA_tighten_body']; ad=arm.animation_data
MUTE0=[t.mute for t in ad.nla_tracks]
for t in ad.nla_tracks: t.mute=True
names=[b.name for b in arm.data.bones]; pbs=arm.pose.bones
bones0={b.name:(tuple(b.head_local),tuple(b.tail_local),tuple(map(tuple,b.matrix_local))) for b in arm.data.bones}
def FC(a): return [fc for L in a.layers for st in L.strips for cb in st.channelbags for fc in cb.fcurves]
other={}
for an in ('LYRA_tighten_attack','LYRA_arcane_bolt_attack'):
    other[an]=[(fc.data_path,fc.array_index,[tuple(k.co) for k in fc.keyframe_points]) for fc in FC(bpy.data.actions[an])]
def roll(M):
    for bn in ('LeftUpLeg','LeftLeg'):
        m=M[P+bn]; R=m.to_3x3()@Matrix.Rotation(math.radians(ROLL),3,'Y'); t=m.translation.copy(); M[P+bn]=R.to_4x4(); M[P+bn].translation=t
    return M
rep={}
for an in ('LYRA_tighten_rest','LYRA_tighten_walk'):
    act=bpy.data.actions[an]; ad.action=act
    cur={(fc.data_path.split('"')[1],fc.data_path.rsplit('.',1)[1],fc.array_index):fc for fc in FC(act) if fc.data_path.startswith('pose.bones')}
    frames=sorted({int(round(k.co[0])) for fc in cur.values() for k in fc.keyframe_points})
    before={}; target={}
    for f in frames:
        sc.frame_set(f); before[f]={n:pbs[n].matrix.copy() for n in names}; target[f]=roll({n:before[f][n].copy() for n in names})
    prev={}
    for f in frames:
        M=target[f]
        for n in (P+'LeftUpLeg',P+'LeftLeg',P+'LeftFoot'):
            b=arm.data.bones[n]
            Bas=(b.parent.matrix_local.inverted()@b.matrix_local).inverted()@M[b.parent.name].inverted()@M[n]
            loc,q,s=Bas.decompose(); old=pbs[n].rotation_euler
            e=q.to_euler('XYZ',prev[n]) if n in prev else q.to_euler('XYZ')
            prev[n]=e
            for i in range(3):
                for ch,v in (('rotation_euler',e[i]),('location',loc[i])):
                    fc=cur[(n,ch,i)]; kp=[k for k in fc.keyframe_points if abs(k.co[0]-f)<1e-4][0]; dv=v-kp.co[1]; kp.co[1]=v; kp.handle_left[1]+=dv; kp.handle_right[1]+=dv
    for fc in cur.values(): fc.update()
    err=0; oth=0; jt=0
    for f in frames:
        sc.frame_set(f)
        for n in names:
            d=max(abs(x) for r in (pbs[n].matrix-target[f][n]) for x in r); err=max(err,d)
            if n not in (P+'LeftUpLeg',P+'LeftLeg'): oth=max(oth,max(abs(x) for r in (pbs[n].matrix-before[f][n]) for x in r))
            jt=max(jt,(pbs[n].head-before[f][n].translation).length,(pbs[n].tail-(before[f][n]@__import__('mathutils').Vector((0,arm.data.bones[n].length,0)))).length)
    rep[an]=dict(frames=len(frames),pose_err=err,other_bones_maxdiff=oth,joint_pos_maxdiff_m=jt)
rep['bones_unchanged']=all(bones0[b.name]==(tuple(b.head_local),tuple(b.tail_local),tuple(map(tuple,b.matrix_local))) for b in arm.data.bones)
rep['attack_bolt_unchanged']=all(other[an]==[(fc.data_path,fc.array_index,[tuple(k.co) for k in fc.keyframe_points]) for fc in FC(bpy.data.actions[an])] for an in other)
print('RW',json.dumps(rep))
assert rep['bones_unchanged'] and rep['attack_bolt_unchanged']
common=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
    mesh_smooth_type='FACE', use_mesh_modifiers=False)
anim=dict(bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
bpy.ops.object.select_all(action='DESELECT')
for o in (arm,body): o.hide_set(False); o.select_set(True)
bpy.context.view_layer.objects.active=arm
ad.action=bpy.data.actions['LYRA_tighten_rest']; sc.frame_set(0)
bpy.ops.export_scene.fbx(filepath=f'{OUTD}/LYRA_tighten_blenderig.fbx',bake_anim=False,**common)
ad.action=bpy.data.actions['LYRA_tighten_walk']; sc.frame_set(0)
bpy.ops.export_scene.fbx(filepath=f'{OUTD}/LYRA_tighten_blenderig_walk.fbx',**anim,**common)
for t,m in zip(ad.nla_tracks,MUTE0): t.mute=m
ad.action=bpy.data.actions['LYRA_tighten_walk']; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=OUTB,compress=True)
print('RW OK')
