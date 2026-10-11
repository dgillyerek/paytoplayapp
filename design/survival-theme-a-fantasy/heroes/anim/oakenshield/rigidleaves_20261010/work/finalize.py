import bpy, bmesh, sys, json, os, time, math
import numpy as np
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]
os.makedirs(OUT,exist_ok=True)
sc=bpy.context.scene; sc.render.fps=30
body=bpy.data.objects['OAKENSHIELD_body']; arm=bpy.data.objects['OAKENSHIELD_rig']; me=body.data
rep={}
# ---------- 1. backface shell: faces whose back side is visible from outside get a flipped twin (same UV/paint/weights)
arm.data.pose_position='REST'
bm=bmesh.new(); bm.from_mesh(me); bm.faces.ensure_lookup_table(); bm.verts.ensure_lookup_table()
bvh=BVHTree.FromBMesh(bm)
dirs=[]
for el in (-25,0,30,60):
    n=8 if el<60 else 4
    for k in range(n):
        az=2*math.pi*k/n; c=math.cos(math.radians(el))
        dirs.append(Vector((c*math.cos(az),c*math.sin(az),-math.sin(math.radians(el)))))
t0=time.time()
seen=np.zeros(len(bm.faces),bool)
cents=[f.calc_center_median() for f in bm.faces]; nrm=[f.normal.copy() for f in bm.faces]
for d in dirs:
    for i,f in enumerate(bm.faces):
        if seen[i] or nrm[i].dot(d)<=0.05: continue
        hit=bvh.ray_cast(cents[i]-d*3.0,d,3.2)
        if hit[2]==i: seen[i]=True
rep['backface_visible_faces']=int(seen.sum()); rep['backface_scan_s']=round(time.time()-t0,1)
mark=seen.copy()
for i in np.nonzero(seen)[0]:
    for e in bm.faces[i].edges:
        for g in e.link_faces: mark[g.index]=True
faces=[bm.faces[i] for i in np.nonzero(mark)[0]]
res=bmesh.ops.duplicate(bm,geom=faces)
newf=[g for g in res['geom'] if isinstance(g,bmesh.types.BMFace)]
bmesh.ops.reverse_faces(bm,faces=newf,flip_multires=False)
rep['backface_twin_faces']=len(newf)
bm.to_mesh(me); me.update(); bm.free()
arm.data.pose_position='POSE'
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
    bpy.ops.object.select_all(action='DESELECT')
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
    for pb in arm.pose.bones: pb.matrix_basis.identity()
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
    cal[n]=U(arm.matrix_world@arm.pose.bones['mixamorig:'+n].head)
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
reset(); sc.frame_set(0)
sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/OAKENSHIELD_rigidleaves.fbx', bake_anim=False, **hum)
for tag in ('walk','attack'):
    reset(); arm.animation_data.action=A[tag]; sc.frame_start=0; sc.frame_end=30
    sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/OAKENSHIELD_rigidleaves_{tag}.fbx', **reb)
reset(); arm.animation_data.action=A['rest']; sc.frame_start=0; sc.frame_end=30; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=f'{OUT}/OAKENSHIELD_rigidleaves.blend', compress=True)
rep['blender']=bpy.app.version_string
json.dump(rep,open(f'{OUT}/finalize.json','w'),indent=1)
print('FINAL',json.dumps({k:v for k,v in rep.items() if not k.startswith('spear_')}))
