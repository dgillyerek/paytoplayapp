import bpy, json, sys
a=bpy.data.actions['VESPERA_nocape_walk']; out=[]
pb=bpy.data.objects['VESPERA_rig']
pb.animation_data.action=a
for f in range(31):
    bpy.context.scene.frame_set(f)
    out.append([[round(x,6) for x in p.matrix_basis.col[i]] for p in pb.pose.bones for i in range(4)])
json.dump(out,open(sys.argv[-1],'w'))
