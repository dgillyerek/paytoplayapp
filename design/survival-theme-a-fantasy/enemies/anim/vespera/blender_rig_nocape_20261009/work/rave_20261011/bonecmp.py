import bpy, json, sys
a=bpy.data.objects['VESPERA_rig']; out={}
for b in a.data.bones: out[b.name]=[list(b.head_local),list(b.tail_local),[list(r) for r in b.matrix_local],b.parent.name if b.parent else None,b.use_connect]
json.dump(out,open(sys.argv[-1],'w'))
