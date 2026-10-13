import bpy,json
out={}
for t in ('blenderig','blenderig_walk','blenderig_attack','cape'):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f'/workspace/bonequill_fresh/work/export/BONEQUILL_{t}.fbx')
    a=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; m=[o for o in bpy.data.objects if o.type=='MESH'][0]
    act=a.animation_data.action if a.animation_data else None
    out[t]=dict(bones=len(a.data.bones),faces=len(m.data.polygons),groups=len(m.vertex_groups),frames=list(act.frame_range) if act else None,images=[i.name for i in bpy.data.images],dims=[round(x,3) for x in m.dimensions])
print('QC',json.dumps(out))
