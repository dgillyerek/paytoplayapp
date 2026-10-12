import bpy, sys, json, math
D=sys.argv[sys.argv.index('--')+1]; out={}
for tag,fn in (('rest','OAKENSHIELD_rigidleaves.fbx'),('walk','OAKENSHIELD_rigidleaves_walk.fbx'),('attack','OAKENSHIELD_rigidleaves_attack.fbx')):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f'{D}/{fn}')
    a=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; m=[o for o in bpy.data.objects if o.type=='MESH'][0]; me=m.data
    dg={g.index for g in m.vertex_groups if g.name.startswith('mixamorig:')}
    pb=a.pose.bones['mixamorig:Head']
    r={'objects':sorted(o.name for o in bpy.data.objects),'bones':[b.name for b in a.data.bones],'faces':len(me.polygons),
       'max_infl':max(sum(1 for g in v.groups if g.group in dg) for v in me.vertices),
       'unweighted':sum(1 for v in me.vertices if sum(g.weight for g in v.groups if g.group in dg)<0.99),
       'actions':[(ac.name,list(ac.frame_range)) for ac in bpy.data.actions],
       'head_pose_deg':round(math.degrees(pb.matrix_basis.to_quaternion().angle),1)}
    out[tag]=r
json.dump(out,open(f'{D}/qc_fbx.json','w'),indent=1)
for k,v in out.items(): print('QC',k,len(v['bones']),v['faces'],v['max_infl'],v['unweighted'],v['actions'],'headpose',v['head_pose_deg'])
