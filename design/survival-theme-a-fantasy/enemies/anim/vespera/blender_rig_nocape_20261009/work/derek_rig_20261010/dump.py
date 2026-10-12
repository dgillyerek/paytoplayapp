import bpy, sys, json, numpy as np, hashlib
OUT=sys.argv[sys.argv.index('--')+1]
d={'version':list(bpy.data.version),'objects':{},'bones':{},'actions':{}}
for o in bpy.data.objects:
    e={'type':o.type,'parent':o.parent.name if o.parent else None,'parent_type':o.parent_type,'parent_bone':o.parent_bone,
       'loc':[round(x,5) for x in o.location],'rot':[round(x,5) for x in o.rotation_euler],'scale':[round(x,5) for x in o.scale],
       'mods':[(m.type,m.name,getattr(m,'object',None) and m.object.name) for m in o.modifiers],'mode':o.mode,
       'action':(o.animation_data.action.name if o.animation_data and o.animation_data.action else None)}
    if o.type=='MESH':
        me=o.data; e['verts']=len(me.vertices); e['faces']=len(me.polygons); e['vgroups']=[g.name for g in o.vertex_groups]
        co=np.array([v.co[:] for v in me.vertices]); np.save(OUT+'_'+o.name+'_co.npy',co)
        G=len(o.vertex_groups); W=np.zeros((len(me.vertices),G),np.float32)
        for v in me.vertices:
            for g in v.groups: W[v.index,g.group]=g.weight
        np.save(OUT+'_'+o.name+'_W.npy',W)
        e['unweighted']=int((W.sum(1)<1e-6).sum()); e['max_infl']=int((W>0).sum(1).max()) if G else 0
        e['materials']=[m.name if m else None for m in me.materials]
    if o.type=='ARMATURE':
        e['pose_position']=o.data.pose_position
        for b in o.data.bones:
            d['bones'][b.name]={'head':[round(x,5) for x in b.head_local],'tail':[round(x,5) for x in b.tail_local],'parent':b.parent.name if b.parent else None,
              'connect':b.use_connect,'deform':b.use_deform,'roll_mat':[round(x,4) for r in b.matrix_local.to_3x3() for x in r]}
        e['pose_nonrest']={pb.name:[round(x,4) for x in list(pb.location)+list(pb.rotation_quaternion)+list(pb.rotation_euler)+list(pb.scale)] for pb in o.pose.bones}
        e['constraints']={pb.name:[c.type for c in pb.constraints] for pb in o.pose.bones if pb.constraints}
    d['objects'][o.name]=e
for a in bpy.data.actions:
    fcs=[]
    try:
        it=a.fcurves
    except Exception:
        it=[]
        for l in a.layers:
            for s in l.strips:
                for cb in s.channelbags: it+=list(cb.fcurves)
    for fc in it:
        kp=[(round(k.co[0],3),round(k.co[1],5)) for k in fc.keyframe_points]
        fcs.append((fc.data_path,fc.array_index,len(kp),hashlib.md5(str(kp).encode()).hexdigest()[:8]))
    d['actions'][a.name]={'range':list(a.frame_range),'fcurves':len(fcs),'fake':a.use_fake_user,'users':a.users,'curves':sorted(fcs)}
json.dump(d,open(OUT+'.json','w'),indent=1)
print('DUMPED',OUT)
