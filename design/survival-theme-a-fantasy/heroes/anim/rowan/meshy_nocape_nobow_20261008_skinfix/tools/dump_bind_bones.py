import bpy, sys, collections, json
import os as _os; WORK=_os.environ['ROWAN_FIX_WORK']
path = sys.argv[sys.argv.index('--')+1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path)
o=bpy.data.objects['output_unwrapped']; me=o.data; mw=o.matrix_world
arm=bpy.data.objects['target_character']
pts=[(i,tuple(mw@v.co)) for i,v in enumerate(me.vertices)]
json.dump({'pts':[p[1] for p in pts],'parents':{b.name:(b.parent.name if b.parent else None) for b in arm.data.bones},'bones':{b.name:[tuple(arm.matrix_world@b.head_local),tuple(arm.matrix_world@b.tail_local)] for b in arm.data.bones}}, open(WORK+'/bind_pts.json','w'))
