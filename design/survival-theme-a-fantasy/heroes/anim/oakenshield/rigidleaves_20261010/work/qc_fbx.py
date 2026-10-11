import bpy, sys, json
args=sys.argv[sys.argv.index('--')+1:]; D=args[0]; out={}
for tag,fn in (('rest','OAKENSHIELD_rigidleaves.fbx'),('walk','OAKENSHIELD_rigidleaves_walk.fbx'),('attack','OAKENSHIELD_rigidleaves_attack.fbx')):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=f'{D}/{fn}')
    a=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; m=[o for o in bpy.data.objects if o.type=='MESH']
    me=m[0].data
    r={'objects':sorted(o.name for o in bpy.data.objects),'bones':[b.name for b in a.data.bones],'faces':len(me.polygons),
       'max_infl':max(len([g for g in v.groups if m[0].vertex_groups[g.group].name.startswith('mixamorig:')]) for v in me.vertices),'unweighted':sum(1 for v in me.vertices if sum(g.weight for g in v.groups if m[0].vertex_groups[g.group].name.startswith('mixamorig:'))<0.99),
       'actions':[(ac.name,list(ac.frame_range)) for ac in bpy.data.actions]}
    out[tag]=r
json.dump(out,open(f'{D}/qc_fbx.json','w'),indent=1)
for k,v in out.items(): print('QC',k,len(v['bones']),v['objects'],v['faces'],v['max_infl'],v['unweighted'],v['actions'])
