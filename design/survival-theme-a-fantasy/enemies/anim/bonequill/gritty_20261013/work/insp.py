import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,numpy as np
for tag,p in (('body','/workspace/bonequill_gritty/meshy_body/BONEQUILL_gritty_body_meshy_texture.glb'),('cape','/workspace/bonequill_gritty/meshy_cape/BONEQUILL_gritty_cloak_meshy_texture.glb')):
    bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.gltf(filepath=p)
    for o in bpy.data.objects:
        if o.type!='MESH': continue
        me=o.data; P=np.array([o.matrix_world@v.co for v in me.vertices])
        print(tag,o.name,len(me.vertices),len(me.polygons),P.min(0).round(3),P.max(0).round(3),[m.name for m in me.materials])
    print([ (i.name,i.size[:]) for i in bpy.data.images])
    bpy.ops.wm.save_as_mainfile(filepath=f'/workspace/bonequill_gritty/work/raw_{tag}.blend')
