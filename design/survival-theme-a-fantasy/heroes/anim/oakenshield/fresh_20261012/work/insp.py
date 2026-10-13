import bpy,bmesh,numpy as np
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath='/workspace/oakenshield_fresh/meshy/OAKENSHIELD_meshy_texture.glb')
for o in bpy.data.objects:
    print(o.name,o.type,tuple(round(x,3) for x in o.dimensions),o.location[:],o.rotation_euler[:],o.scale[:])
    if o.type=='MESH':
        me=o.data; print('v',len(me.vertices),'f',len(me.polygons),'uv',len(me.uv_layers),[m.name for m in me.materials])
        co=np.array([o.matrix_world@v.co for v in me.vertices]); print('min',co.min(0),'max',co.max(0))
for i in bpy.data.images: print('img',i.name,i.size[:])
bpy.ops.wm.save_as_mainfile(filepath='/workspace/oakenshield_fresh/work/raw.blend')
