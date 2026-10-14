import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,numpy as np
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath='/workspace/rowan_fresh/meshy_apose/ROWAN_apose_meshy_texture.glb')
for o in bpy.data.objects:
    print(o.name,o.type,o.parent)
    if o.type!='MESH': continue
    me=o.data; P=np.array([o.matrix_world@v.co for v in me.vertices])
    print(len(me.vertices),len(me.polygons),P.min(0).round(3),P.max(0).round(3),[m.name for m in me.materials])
print([(i.name,i.size[:]) for i in bpy.data.images])
for o in list(bpy.data.objects):
    if o.type=='MESH':
        o.data.transform(o.matrix_world); o.parent=None; o.matrix_world.identity()
    else: bpy.data.objects.remove(o)
bpy.ops.wm.save_as_mainfile(filepath='/workspace/rowan_fresh/w2/raw_body.blend')
