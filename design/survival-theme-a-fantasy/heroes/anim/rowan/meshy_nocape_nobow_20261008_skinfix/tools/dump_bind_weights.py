import bpy, sys, json
import os as _os; WORK=_os.environ['ROWAN_FIX_WORK']
path = sys.argv[sys.argv.index('--')+1]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=path)
o=bpy.data.objects['output_unwrapped']; me=o.data; mw=o.matrix_world
gname={g.index:g.name for g in o.vertex_groups}
out=[]
for v in me.vertices:
    out.append([list(mw@v.co), {gname[g.group]:g.weight for g in v.groups if g.weight>0}])
edges=[list(e.vertices) for e in me.edges]
json.dump({'v':out,'e':edges}, open(WORK+'/bind_w.json','w'))
