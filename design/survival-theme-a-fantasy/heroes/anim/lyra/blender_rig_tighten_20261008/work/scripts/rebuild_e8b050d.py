"""Rebuild Lyra clips on Derek's e8b050d rig (bones untouched)."""
import bpy, json, math, sys, numpy as np
from mathutils import Matrix, Vector
REPO='/workspace/lyra-onefile'
L=f'{REPO}/design/survival-theme-a-fantasy/heroes/anim/lyra'
WD='/workspace/lyra-rebuild-e8b050d'
OUTD=sys.argv[sys.argv.index('--')+1]
sc=bpy.context.scene
arm=bpy.data.objects['LYRA_tighten_rig']; body=bpy.data.objects['LYRA_tighten_body']
staff=bpy.data.objects['LYRA_staff']; bolt=bpy.data.objects['LYRA_arcane_bolt_vfx']
REP={'saved_mode':arm.mode}
if arm.mode!='OBJECT':
    bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='OBJECT')
assert arm.matrix_world==Matrix.Identity(4)
bone_snapshot={b.name:(tuple(b.head_local),tuple(b.tail_local),tuple(map(tuple,b.matrix_local))) for b in arm.data.bones}
P='mixamorig:'
# ---------- 1. toe weights -> foot (ToeBase bones were deleted by Derek) ----------
vg={g.name:g for g in body.vertex_groups}
for s in ('Left','Right'):
    t=vg.get(P+s+'ToeBase'); f=vg[P+s+'Foot']
    if t is None or (P+s+'ToeBase') in arm.data.bones: continue
    n=0
    for v in body.data.vertices:
        tw=0.0; fw=0.0
        for g in v.groups:
            if g.group==t.index: tw=g.weight
            elif g.group==f.index: fw=g.weight
        if tw>0: f.add([v.index],min(1.0,fw+tw),'REPLACE'); n+=1
    body.vertex_groups.remove(t); REP[f'toe_merged_{s}']=n
# orphan curves of deleted bones (rest/attack actions)
def FC(a):
    out=[]
    for Ly in a.layers:
        for st in Ly.strips:
            for cb in st.channelbags: out+=[(cb,fc) for fc in cb.fcurves]
    return out
bn=set(arm.data.bones.keys()); rm=0
for a in bpy.data.actions:
    for cb,fc in FC(a):
        if fc.data_path.startswith('pose.bones["'):
            b=fc.data_path.split('"')[1]
            if b not in bn: cb.fcurves.remove(fc); rm+=1
REP['orphan_curves_removed']=rm
# ---------- 2. staff re-seat (Design rest_world unchanged) ----------
smp=f'{L}/blender_rig_tighten_20261008/props/LYRA_staff_meta.json'; amp=f'{L}/attack_20261008/work/attack_meta.json'
smeta=json.load(open(smp)); ameta=json.load(open(amp))
RW=Matrix(smeta['rest_world_blender'])
hb=arm.data.bones[P+'RightHand']; HR=hb.matrix_local
OFF=HR.inverted()@RW
staff.parent=arm; staff.parent_type='BONE'; staff.parent_bone=hb.name
staff.matrix_parent_inverse=Matrix.Identity(4)
staff.matrix_basis=Matrix.Translation((0,-hb.length,0))@OFF
REP['old_offset']=smeta['offset_in_hand_blender']; REP['new_offset']=[list(r) for r in OFF]
smeta['offset_in_hand_blender']=[list(r) for r in OFF]
smeta['offset_note']="2026-10-08 22:45 rig edit (e8b050d): re-derived for Derek-edited RightHand (inv(new hand rest) @ rest_world_blender); rest_world unchanged."
ameta['props']['staff']['offset_RightHand']=[list(r) for r in OFF]
json.dump(smeta,open(smp,'w'),indent=1)
# ---------- 3. attack retarget (world-space rotation deltas from a995489's attack) ----------
R=np.load(f'{WD}/old_attack_ref.npz')
onames=[str(x) for x in R['names']]; OP=R['pose']; ORr=R['rest']
oi={n:i for i,n in enumerate(onames)}
B={b.name:b for b in arm.data.bones}; REST={n:b.matrix_local.copy() for n,b in B.items()}
order=[b.name for b in arm.data.bones]; PAR={n:(B[n].parent.name if B[n].parent else None) for n in B}
def R3(m): return m.to_3x3().normalized()
act=bpy.data.actions['LYRA_tighten_attack']; ad=arm.animation_data; walk_saved=ad.action
basisF=[]
for f in range(31):
    D={}
    for n in order:
        Ro=Matrix(OP[f,oi[n]].tolist()); Rr=Matrix(ORr[oi[n]].tolist())
        dW=R3(Ro)@R3(Rr).inverted()
        m=(dW@R3(REST[n])).to_4x4()
        p=PAR[n]
        if p: m.translation=(D[p]@REST[p].inverted()@REST[n]).translation
        else: m.translation=REST[n].translation+(Ro.translation-Rr.translation)
        D[n]=m
    bf={}
    for n in order:
        p=PAR[n]
        bf[n]=(REST[n].inverted()@REST[p]@D[p].inverted()@D[n]) if p else REST[n].inverted()@D[n]
    basisF.append(bf)
# write keys into existing curves (per-frame linear euler, as before)
cur={}
for cb,fc in FC(act):
    if fc.data_path.startswith('pose.bones["'): cur[(fc.data_path.split('"')[1],fc.data_path.rsplit('.',1)[1],fc.array_index)]=fc
prevE={}
for f in range(31):
    for n in order:
        loc,rot,scl=basisF[f][n].decompose()
        e=rot.to_euler('XYZ',prevE[n]) if n in prevE else rot.to_euler('XYZ'); prevE[n]=e
        if n!='mixamorig:Hips' and B[n].use_connect: loc=Vector((0,0,0))
        vals={'location':loc,'rotation_euler':e,'scale':Vector((1,1,1))}
        for prop,v in vals.items():
            for i in range(3):
                fc=cur[(n,prop,i)]
                kp=[k for k in fc.keyframe_points if abs(k.co[0]-f)<1e-4][0]
                d=v[i]-kp.co[1]; kp.co[1]=v[i]; kp.handle_left[1]+=d; kp.handle_right[1]+=d
for fc in cur.values(): fc.update()
# ---------- 4. walk v2 re-authored on this rig ----------
sys.argv=[sys.argv[0],'--',f'{WD}/walk_report.json']
exec(open(f'{L}/blender_rig_tighten_20261008/work/scripts/walk_v2_author.py').read(),{'__name__':'walk'})
ad.action=bpy.data.actions['LYRA_tighten_walk']
# ---------- 5. bolt at the new f12 crystal ----------
CRY=Vector(ameta['props']['staff']['crystal_local']); REL=ameta['props']['bolt']['release']; SPD=ameta['props']['bolt']['speed_mps']
ad.action=act; sc.frame_set(REL); spawn=staff.matrix_world@CRY
REP['spawn_old']=ameta['props']['bolt']['spawn_world']; REP['spawn_new']=list(spawn)
for cb,fc in FC(bolt.animation_data.action):
    if fc.data_path=='location':
        for k in fc.keyframe_points:
            t=max(0,k.co[0]-REL); v=spawn[fc.array_index]+(-SPD*t/30.0 if fc.array_index==1 else 0.0)
            d=v-k.co[1]; k.co[1]=v; k.handle_left[1]+=d; k.handle_right[1]+=d
        fc.update()
ameta['props']['bolt']['spawn_world']=list(spawn)
ameta['rig_edit_note_e8b050d']=("2026-10-08 22:45: Derek moved hips, knees, ankles, left elbow/wrist, right wrist, head length and removed both ToeBase bones. "
  "Attack retargeted by world-space rotation deltas from a995489; staff offset re-derived from the unchanged rest_world; bolt spawn re-measured at the f12 crystal. Old spawn "+str([round(x,5) for x in REP['spawn_old']])+".")
json.dump(ameta,open(amp,'w'),indent=1)
# ---------- 6. sample for Unity / QC ----------
S={}
for an in ('LYRA_tighten_rest','LYRA_tighten_walk','LYRA_tighten_attack'):
    ad.action=bpy.data.actions[an]; st=[];hd=[];ps=[]
    for f in range(31):
        sc.frame_set(f); st.append([list(r) for r in staff.matrix_world]); hd.append([list(r) for r in arm.pose.bones[P+'RightHand'].matrix])
        ps.append({pb.name:list(pb.head) for pb in arm.pose.bones})
    S[an]=dict(staff=st,hand=hd,heads=ps)
REP['rest_heads']={b.name:list(b.head_local) for b in arm.data.bones}
REP['bones_unchanged']=all(bone_snapshot[b.name]==(tuple(b.head_local),tuple(b.tail_local),tuple(map(tuple,b.matrix_local))) for b in arm.data.bones)
json.dump(S,open(f'{WD}/samples.json','w')); 
# ---------- 7. save (object mode, walk active) + export ----------
ad.action=bpy.data.actions['LYRA_tighten_walk']; sc.frame_set(0)
bpy.ops.wm.save_mainfile(compress=True)
common=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
    mesh_smooth_type='FACE', use_mesh_modifiers=False)
anim=dict(bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0)
def sel(objs):
    bpy.ops.object.select_all(action='DESELECT')
    for o in objs: o.hide_set(False); o.select_set(True)
    bpy.context.view_layer.objects.active=objs[0]
ad.action=None
for pb in arm.pose.bones: pb.matrix_basis=Matrix.Identity(4)
sc.frame_set(0); sel([arm,body]); bpy.ops.export_scene.fbx(filepath=f'{OUTD}/LYRA_tighten_blenderig.fbx',bake_anim=False,**common)
for nm in ('walk','attack'):
    ad.action=bpy.data.actions[f'LYRA_tighten_{nm}']; sc.frame_set(0); sel([arm,body])
    bpy.ops.export_scene.fbx(filepath=f'{OUTD}/LYRA_tighten_blenderig_{nm}.fbx',**anim,**common)
json.dump(REP,open(f'{WD}/build_report.json','w'),indent=1)
print('BUILD OK',json.dumps({k:REP[k] for k in ('saved_mode','orphan_curves_removed','bones_unchanged','spawn_new','spawn_old')}),REP.get('toe_merged_Left'),REP.get('toe_merged_Right'))
