import bpy, bmesh, sys, json, os, time, math
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]
os.makedirs(OUT,exist_ok=True)
sc=bpy.context.scene; sc.render.fps=30
body=bpy.data.objects['VESPERA_nocape_body']; arm=bpy.data.objects['VESPERA_nocape_rig']; me=body.data
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
        dirs.append(Vector((c*math.cos(az),c*math.sin(az),-math.sin(math.radians(el)))))   # ray direction (toward the body)
t0=time.time()
seen=np.zeros(len(bm.faces),bool)
cents=[f.calc_center_median() for f in bm.faces]; nrm=[f.normal.copy() for f in bm.faces]
for d in dirs:
    for i,f in enumerate(bm.faces):
        if seen[i] or nrm[i].dot(d)<=0.05: continue      # only faces facing away from this viewer
        o=cents[i]-d*3.0
        hit=bvh.ray_cast(o,d,3.2)
        if hit[2]==i: seen[i]=True
rep['backface_visible_faces']=int(seen.sum()); rep['backface_scan_s']=round(time.time()-t0,1)
# dilate one ring so twin edges tuck under neighbours
mark=seen.copy()
for i in np.nonzero(seen)[0]:
    for e in bm.faces[i].edges:
        for g in e.link_faces: mark[g.index]=True
faces=[bm.faces[i] for i in np.nonzero(mark)[0]]
res=bmesh.ops.duplicate(bm,geom=faces)
newf=[g for g in res['geom'] if isinstance(g,bmesh.types.BMFace)]
bmesh.ops.reverse_faces(bm,faces=newf,flip_multires=False)
rep['backface_twin_faces']=len(newf)
cz=np.array([f.calc_center_median()[:] for f in newf]) if newf else np.zeros((0,3))
rep['twin_regions_z']={'back_or_hair_y>0':int((cz[:,1]>0).sum()),'front_y<0':int((cz[:,1]<=0).sum()),
  'skirt_hip_z0.75_1.05':int(((cz[:,2]>0.75)&(cz[:,2]<1.05)).sum()),'torso_shoulders_z1.05_1.5':int(((cz[:,2]>=1.05)&(cz[:,2]<1.5)).sum()),'head_hair_z>=1.5':int((cz[:,2]>=1.5).sum())}
bm.to_mesh(me); me.update(); bm.free()
# twin verts are new: put them in a vertex group so Design can find them
nv=len(me.vertices)
arm.data.pose_position='POSE'
# ---------- 2. names to match the ThemePack contract
arm.name='VESPERA_rig'; arm.data.name='VESPERA_rig'; body.name='VESPERA_body'; me.name='VESPERA_body'
for im in bpy.data.images:
    if im.source in {'VIEWER','GENERATED'} or im.name in {'Render Result','Viewer Node'}: continue
    if im.packed_file is None:
        try: im.pack()
        except Exception as ex: print('PACKFAIL',im.name,ex)
rep['images']=[im.name for im in bpy.data.images]
rep['faces']=len(me.polygons); rep['verts']=len(me.vertices)
wmax=max(len(v.groups) for v in me.vertices); unw=sum(1 for v in me.vertices if sum(g.weight for g in v.groups)<0.999)
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
A={k:bpy.data.actions['VESPERA_nocape_'+k] for k in ('rest','walk','attack')}
def reset():
    arm.animation_data.action=None
    for pb in arm.pose.bones: pb.matrix_basis.identity()
if arm.animation_data is None: arm.animation_data_create()
reset(); sc.frame_set(0)
# ---------- 3. calibration + bolt spawn (Unity axes: x=-bx, y=bz, z=-by)
def U(v): return [round(-v.x,5),round(v.z,5),round(-v.y,5)]
cal={}
for n in ('Hips','Head','LeftHand','RightHand'):
    cal[n]=U(arm.matrix_world@arm.pose.bones['mixamorig:'+n].head)
arm.animation_data.action=A['attack']
track={}
for f in range(31):
    sc.frame_set(f); pb=arm.pose.bones['mixamorig:RightHand']
    track[f]={'head':U(arm.matrix_world@pb.head),'tail':U(arm.matrix_world@pb.tail)}
sc.frame_set(13); pb=arm.pose.bones['mixamorig:RightHand']
spawn=arm.matrix_world@pb.tail+Vector((0,-0.12,0))
rep['calibration_unity']=cal; rep['bolt_spawn_unity_f13']=U(spawn); rep['right_hand_track_unity']=track
reset(); sc.frame_set(0)
sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/VESPERA_nocape.fbx', bake_anim=False, **hum)
for tag in ('walk','attack'):
    reset(); arm.animation_data.action=A[tag]; sc.frame_start=0; sc.frame_end=30
    sel(); bpy.ops.export_scene.fbx(filepath=f'{OUT}/VESPERA_nocape_{tag}.fbx', **reb)
reset(); arm.animation_data.action=A['rest']; sc.frame_start=0; sc.frame_end=30; sc.frame_set(0)
bpy.ops.wm.save_as_mainfile(filepath=f'{OUT}/VESPERA_nocape.blend', compress=True)
rep['blender']=bpy.app.version_string
json.dump(rep,open(f'{OUT}/finalize.json','w'),indent=1)
print('FINAL',json.dumps({k:v for k,v in rep.items() if k!='right_hand_track_unity'}))
