import bpy,json
out={}
for t in ('','_walk','_attack'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f'/workspace/oakenshield_fresh/work/export/OAKENSHIELD_blenderig{t}.fbx')
    a=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; m=[o for o in bpy.data.objects if o.type=='MESH'][0]
    act=a.animation_data.action if a.animation_data else None
    out[t or 'rest']=dict(bones=len(a.data.bones),faces=len(m.data.polygons),groups=len(m.vertex_groups),frames=list(act.frame_range) if act else None,
       images=[i.name for i in bpy.data.images],height=round(m.dimensions.z if m.dimensions.z>m.dimensions.y else m.dimensions.y,3))
print('QC',json.dumps(out))
