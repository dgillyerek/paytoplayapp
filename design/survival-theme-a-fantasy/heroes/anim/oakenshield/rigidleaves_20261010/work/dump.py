import bpy, sys, json, numpy as np
OUT=sys.argv[sys.argv.index('--')+1]
r={'version':bpy.app.version_string,'file_version':list(bpy.data.version)}
r['objects']={o.name:{'type':o.type,'parent':o.parent.name if o.parent else None,'loc':[round(v,5) for v in o.location],'rot':[round(v,5) for v in o.rotation_euler],'scale':[round(v,5) for v in o.scale],
  'mods':[(m.type,m.name,getattr(getattr(m,'object',None),'name',None)) for m in o.modifiers]} for o in bpy.data.objects}
r['actions']={}
for a in bpy.data.actions:
    fcs=[]
    try:
        fcs=list(a.fcurves)
    except Exception:
        for l in a.layers:
            for s in l.strips:
                for cb in s.channelbags: fcs+=list(cb.fcurves)
    d={}
    for fc in fcs:
        d[f'{fc.data_path}[{fc.array_index}]']=[round(k.co[1],5) for k in fc.keyframe_points]
    r['actions'][a.name]={'range':list(a.frame_range),'n_fc':len(fcs),'curves':d}
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
r['bones']={b.name:{'head':[round(v,5) for v in b.head_local],'tail':[round(v,5) for v in b.tail_local],'parent':b.parent.name if b.parent else None,
   'roll_mat':[round(v,4) for row in b.matrix_local.to_3x3() for v in row],'connect':b.use_connect,'deform':b.use_deform} for b in arm.data.bones}
r['pose_basis']={pb.name:[round(v,5) for row in pb.matrix_basis for v in row] for pb in arm.pose.bones}
r['arm_action']=arm.animation_data.action.name if arm.animation_data and arm.animation_data.action else None
body=[o for o in bpy.data.objects if o.type=='MESH'][0]; me=body.data
N=len(me.vertices); co=np.empty(N*3); me.vertices.foreach_get('co',co)
G=[g.name for g in body.vertex_groups]; W=np.zeros((N,len(G)),np.float32)
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
r['mesh']={'verts':N,'faces':len(me.polygons),'groups':G}
np.savez(OUT+'.npz',co=co.reshape(-1,3),W=W,G=np.array(G))
json.dump(r,open(OUT+'.json','w'))
print('DUMP OK',N,len(G))
