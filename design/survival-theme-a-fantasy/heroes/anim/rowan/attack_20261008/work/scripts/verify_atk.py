import bpy, sys, math, json
from mathutils import Vector
REST,ATK,OUTJ=sys.argv[sys.argv.index('--')+1:][:3]; EXTRA=sys.argv[sys.argv.index('--')+4:]
def imp(p):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=p)
    return [o for o in bpy.data.objects if o.type=='ARMATURE'], [o for o in bpy.data.objects if o.type=='MESH']
P='mixamorig:'
A,M=imp(REST); a=A[0]; rn=[b.name for b in a.data.bones]; rh=(a.matrix_world@a.pose.bones[P+'Hips'].matrix).copy()
rpose={b.name:(a.matrix_world@b.matrix).copy() for b in a.pose.bones}; rfaces=sum(len(m.data.polygons) for m in M)
A,M=imp(ATK); a=A[0]; act=a.animation_data.action; f0,f1=map(int,act.frame_range); sc=bpy.context.scene
out={'bones':len(a.data.bones),'same_names_order':[b.name for b in a.data.bones]==rn,'faces':sum(len(m.data.polygons) for m in M),'rest_faces':rfaces,'frame_range':[f0,f1],'action':act.name}
dev={}; travel=0
for f in (f0,f1):
    sc.frame_set(f); dev[f]=max((lambda d:min(d,360-d))(math.degrees((a.matrix_world@b.matrix).to_quaternion().rotation_difference(rpose[b.name].to_quaternion()).angle)) for b in a.pose.bones)
    if f==f0:
        H=a.matrix_world@a.pose.bones[P+'Hips'].matrix
        out['hips_first_vs_rest_deg']=math.degrees(H.to_quaternion().rotation_difference(rh.to_quaternion()).angle); out['hips_first_vs_rest_m']=(H.translation-rh.translation).length
mxh=0
for f in range(f0,f1+1):
    sc.frame_set(f); H=a.matrix_world@a.pose.bones[P+'Hips'].matrix
    mxh=max(mxh,(lambda d:min(d,360-d))(math.degrees(H.to_quaternion().rotation_difference(rh.to_quaternion()).angle)))
    for b in a.pose.bones: travel=max(travel,((a.matrix_world@b.matrix).translation-rpose[b.name].translation).length)
out['end_pose_dev_deg']=dev; out['hips_max_dev_deg_all_frames']=mxh; out['max_bone_travel_m']=travel
for p in EXTRA:
    A,M=imp(p); e={'armatures':[(x.name,[b.name for b in x.data.bones]) for x in A],'meshes':[(m.name,len(m.data.polygons)) for m in M]}
    if A and A[0].animation_data and A[0].animation_data.action: e['frames']=list(A[0].animation_data.action.frame_range)
    out[p.split('/')[-1]]=e
json.dump(out,open(OUTJ,'w'),indent=1,default=float); print('VATK',json.dumps(out,default=float))
