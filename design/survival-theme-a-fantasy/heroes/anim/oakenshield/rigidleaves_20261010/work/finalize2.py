import bpy, bmesh, sys, json, os, time, math
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]
os.makedirs(OUT,exist_ok=True)
sc=bpy.context.scene; sc.render.fps=30
body=bpy.data.objects['OAKENSHIELD_body']; arm=bpy.data.objects['OAKENSHIELD_rig']; me=body.data
rep={}
# 2026-10-11: backface twins were added in rigidleaves_20261010 already; not repeated on Derek's blend
# Design paint-pass group: grow with the new twin verts too
fg=body.vertex_groups.get('DESIGN_paint_fill')
rep['paint_fill_group_verts']=sum(1 for v in me.vertices for g in v.groups if fg and g.group==fg.index)
for im in bpy.data.images:
    if im.source in {'VIEWER','GENERATED'} or im.name in {'Render Result','Viewer Node'}: continue
    if im.packed_file is None:
        try: im.pack()
        except Exception as ex: print('PACKFAIL',im.name,ex)
rep['images']=[im.name for im in bpy.data.images]
rep['faces']=len(me.polygons); rep['verts']=len(me.vertices)
dg=[g.index for g in body.vertex_groups if g.name.startswith('mixamorig:')]
wmax=max(sum(1 for g in v.groups if g.group in dg and g.weight>0) for v in me.vertices)
unw=sum(1 for v in me.vertices if sum(g.weight for g in v.groups if g.group in dg)<0.999)
rep['max_influences']=wmax; rep['unweighted']=unw
def sel():
    for o in bpy.context.view_layer.objects: o.select_set(False)
    for o in (arm,body): o.select_set(True)
    bpy.context.view_layer.objects.active=arm
hum=dict(use_selection=True, object_types={'ARMATURE','MESH'}, axis_forward='-Z', axis_up='Y', apply_scale_options='FBX_SCALE_ALL',
    apply_unit_scale=True, global_scale=1.0, add_leaf_bones=False, path_mode='COPY', embed_textures=True, use_armature_deform_only=False,
    mesh_smooth_type='FACE', use_mesh_modifiers=False)
reb=dict(use_selection=True, object_types={'ARMATURE','MESH'}, use_mesh_modifiers=True, mesh_smooth_type='FACE', add_leaf_bones=False,
    bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=False,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0, path_mode='COPY', embed_textures=True,
    axis_forward='-Z', axis_up='Y', apply_unit_scale=True, apply_scale_options='FBX_SCALE_ALL')
A={k:bpy.data.actions['OAKENSHIELD_'+k] for k in ('rest','walk','attack')}
def reset():
    arm.animation_data.action=None
    for pb in arm.pose.bones:
        pb.location=(0,0,0); pb.rotation_euler=(0,0,0); pb.rotation_quaternion=(1,0,0,0); pb.scale=(1,1,1)
    bpy.context.view_layer.update()
if arm.animation_data is None: arm.animation_data_create()
reset(); sc.frame_set(0)
# ---------- 2. attack calibration + thorn spear held/projectile frames (Unity axes: x=-bx, y=bz, z=-by)
C=Matrix(((-1,0,0),(0,0,1),(0,-1,0)))
def U(v): return [round(-v.x,5),round(v.z,5),round(-v.y,5)]
def UQ(Rb):
    Ru=(C@Rb@C.inverted()).to_quaternion().normalized()
    if Ru.w<0: Ru=-Ru
    return [round(Ru.x,5),round(Ru.y,5),round(Ru.z,5),round(Ru.w,5)]
cal={}
for n in ('Hips','Head','LeftHand','RightHand'):
    cal[n]=U(arm.matrix_world@arm.data.bones['mixamorig:'+n].head_local)   # bind pose
arm.animation_data.action=A['attack']
def grip(pb):
    M=arm.matrix_world@pb.matrix; return M, M.to_translation()+(M.to_3x3()@Vector((0,1,0)))*0.07   # 7 cm along the hand = palm
REF=10; REL=14
sc.frame_set(REF); pb=arm.pose.bones['mixamorig:RightHand']; Mref,gref=grip(pb)
tip=Vector((0,-1,0.12)).normalized()       # spear tip (prop local -Y) points forward, a little up, at the cocked frame
yax=-tip; xax=Vector((1,0,0)); xax=(xax-xax.dot(yax)*yax).normalized(); zax=xax.cross(yax)
Rsp=Matrix((xax,yax,zax)).transposed()     # prop world rotation at REF
Rhand=Mref.to_3x3().normalized()
Roff=Rhand.inverted()@Rsp; poff=Rhand.inverted()@(gref-Mref.to_translation())
held=[]; track={}
for f in range(31):
    sc.frame_set(f); M=arm.matrix_world@pb.matrix; R=M.to_3x3().normalized()
    p=M.to_translation()+R@poff; Rw=R@Roff
    sc_=0.05 if f<3 else (min(1.0,0.05+0.95*(f-3)/5.0) if f<=8 else 1.0)
    vis=3<=f<REL
    held.append(U(p)+UQ(Rw)+[round(sc_,5),vis]); track[f]=U(p)
spawn=held[REL][:3]
proj=[]
for f in range(31):
    z=spawn[2]+max(0,f-REL)*11.0/30.0
    proj.append([spawn[0],spawn[1],round(z,5),0,0,0,1,1,f>=REL])
rep['calibration_unity']=cal; rep['spear_held_unity']=held; rep['spear_proj_unity']=proj; rep['spear_spawn_unity_f14']=spawn
# rest FBX: bind pose with the rest action's head/neck turn applied (Unity's rest = the FBX default node pose)
reset(); arm.animation_data.action=A['rest']; sc.frame_set(0)
sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/OAKENSHIELD_rigidleaves.fbx', bake_anim=False, **hum)
for tag in ('walk','attack'):
    reset(); arm.animation_data.action=A[tag]; sc.frame_start=0; sc.frame_end=30
    sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/OAKENSHIELD_rigidleaves_{tag}.fbx', **reb)
reset(); arm.animation_data.action=A['rest']; sc.frame_start=0; sc.frame_end=30; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=f'{OUT}/OAKENSHIELD_rigidleaves.blend', compress=True)
rep['blender']=bpy.app.version_string
json.dump(rep,open(f'{OUT}/finalize.json','w'),indent=1)
print('FINAL',json.dumps({k:v for k,v in rep.items() if not k.startswith('spear_')}))
