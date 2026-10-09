import bpy, json, math, numpy as np
from mathutils import Vector
OUT='/workspace/design/survival-theme-a-fantasy/heroes/anim/rowan/meshy_nocape_nobow_20261008'
R={}
def load(p):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=p); return bpy.context.scene
def basis_table(arm,frames,skip=()):
    out=[]
    for f in frames:
        bpy.context.scene.frame_set(f)
        out.append(np.array([[x for r in pb.matrix_basis for x in r] for pb in arm.pose.bones if pb.name not in skip]))
    return np.array(out)
for clip,src in (('rest','rest'),('walk','walk'),('attack','attack')):
    sc=load(f'src/ROWAN_meshy_{src}.fbx'); a0=bpy.data.objects['target_character']
    names0=[b.name for b in a0.data.bones]; fr=a0.animation_data.action.frame_range
    frames=list(range(int(fr[0]),int(fr[1])+1))
    T0=basis_table(a0,frames,skip=('mixamorig:Hips',))
    m0=bpy.data.objects['output_unwrapped']; B0=np.array([v.co[:] for v in m0.data.vertices])
    sc=load(f'{OUT}/ROWAN_meshy_nobow_{clip}.fbx')
    objs=[(o.name,o.type) for o in bpy.data.objects]
    a=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; m=[o for o in bpy.data.objects if o.type=='MESH'][0]
    act=a.animation_data.action; fr1=act.frame_range
    T1=basis_table(a,frames,skip=('mixamorig:Hips',))
    co=np.array([(m.matrix_world@v.co)[:] for v in m.data.vertices])
    tris=sum(len(p.vertices)-2 for p in m.data.polygons)
    # kept verts unchanged? nearest check
    from mathutils.kdtree import KDTree
    kd=KDTree(len(B0)); [kd.insert(Vector(p),i) for i,p in enumerate(B0)]; kd.balance()
    dmax=0; nnew=0
    for v in m.data.vertices:
        _,_,d=kd.find(v.co)
        if d>1e-4: nnew+=1
        else: dmax=max(dmax,d)
    r=dict(objects=objs,bones=len(a.data.bones),bone_names_same=[b.name for b in a.data.bones]==names0,frames=list(fr1),fps=sc.render.fps,
           tris=tris,verts=len(m.data.vertices),height=float(np.ptp(co[:,2])),zmin=float(co[:,2].min()),sphere_absent=not any('Icosphere' in o.name for o in bpy.data.objects),
           images=sorted(i.name for i in bpy.data.images),non_hips_basis_maxdiff_vs_source=float(np.abs(T1-T0).max()),
           kept_verts_maxdist_vs_source=dmax,new_verts=nnew, arm_world_identity=float(np.abs(np.array(a.matrix_world)-np.eye(4)).max()))
    if clip=='attack':
        P=a.pose.bones
        def fist(s): return a.matrix_world@((P[f'mixamorig:{s}Hand'].head+P[f'mixamorig:{s}HandMiddle1'].head)/2)
        ds=Vector()
        for f in range(69,77): sc.frame_set(f); ds+=(fist('Left')-fist('Right')).normalized()
        r['shot_yaw_after_fix_deg']=math.degrees(math.atan2(ds.x,-ds.y)); r['shot_pitch_deg']=math.degrees(math.atan2(ds.z,math.hypot(ds.x,ds.y)))
        sc.frame_set(72); r['LH_fist_f72']=list(fist('Left'))
    R[clip]=r; print(clip,r)
# bow
sc=load(f'{OUT}/ROWAN_meshy_bow_attack.fbx')
br=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; bw=[o for o in bpy.data.objects if o.type=='MESH'][0]
dg=bpy.context.evaluated_depsgraph_get()
gi=bw.vertex_groups['bow_grip'].index
rigid=[v.index for v in bw.data.vertices if all(g.group==gi and g.weight>0.999 for g in v.groups) and len(v.groups)==1]
def ev(f):
    sc.frame_set(f); dg=bpy.context.evaluated_depsgraph_get(); e=bw.evaluated_get(dg).to_mesh(); c=np.array([(bw.matrix_world@v.co)[:] for v in e.vertices]); return c
c1=ev(1); idx=np.array(rigid[::40]); 
def pd(c): x=c[idx]; return np.linalg.norm(x[:,None]-x[None],axis=2)
d1=pd(c1); worst=0
for f in (30,58,72,77,100,121): worst=max(worst,float(np.abs(pd(ev(f))-d1).max()))
sc.frame_set(72); g=br.matrix_world@br.pose.bones['bow_grip'].head
R['bow']=dict(objects=[(o.name,o.type) for o in bpy.data.objects],bones=[b.name for b in br.data.bones],frames=list(br.animation_data.action.frame_range),fps=sc.render.fps,
   faces=len(bw.data.polygons),bbox_len=float(np.ptp(c1,axis=0).max()),rigid_verts=len(rigid),rigid_pairwise_maxdev_m=worst,grip_f72=list(g),
   grip_to_LHfist_f72_m=(g-Vector(R['attack']['LH_fist_f72'])).length)
print('bow',R['bow'])
sc=load(f'{OUT}/ROWAN_meshy_arrow_attack.fbx'); o=bpy.data.objects[0]
pos={}
for f in (20,30,50,76,77,78,90):
    sc.frame_set(f); pos[f]=[list(o.matrix_world.translation), list(o.matrix_world.to_scale())]
R['arrow']=dict(objects=[(x.name,x.type) for x in bpy.data.objects],frames=list(o.animation_data.action.frame_range) if o.animation_data and o.animation_data.action else None,pos=pos,
  mats=sorted(m.name for m in bpy.data.materials))
print('arrow',R['arrow'])
for p in ('props/ROWAN_bow_meshy.fbx','props/ROWAN_arrow_blue_fletch_meshy.fbx'):
    sc=load(f'{OUT}/{p}'); ms=[o for o in bpy.data.objects if o.type=='MESH'][0]
    c=np.array([(ms.matrix_world@v.co)[:] for v in ms.data.vertices]); R[p]=dict(objects=[(x.name,x.type) for x in bpy.data.objects],bbox_min=list(c.min(0)),bbox_max=list(c.max(0)),faces=len(ms.data.polygons))
    print(p,R[p])
json.dump(R,open(f'{OUT}/work/verify_reimport.json','w'),indent=1,default=float)
