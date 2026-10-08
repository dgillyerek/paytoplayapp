import bpy, sys, numpy as np
sys.path.insert(0,'/workspace/rig_20261008'); import quiver
ob=[o for o in bpy.data.objects if o.type=='MESH'][0]; arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
print('mesh',ob.name,len(ob.data.vertices),len(ob.data.polygons),'arm',arm.name,len(arm.data.bones))
M=np.array(ob.matrix_world); co=np.array([(M@np.array([*v.co,1]))[:3] for v in ob.data.vertices])
wq=quiver.quiver_weight(co); idx=np.where(wq>0)[0]; print('quiver verts',len(idx),'rigid',(wq>=0.999).sum())
gi={g.name:g.index for g in ob.vertex_groups}; sp=gi['mixamorig:Spine2']; names={g.index:g.name for g in ob.vertex_groups}
for i in map(int,idx):
    v=ob.data.vertices[i]; w={g.group:g.weight*(1-float(wq[i])) for g in v.groups}
    w[sp]=w.get(sp,0)+float(wq[i])
    top=dict(sorted(w.items(),key=lambda kv:-kv[1])[:4]); s=sum(top.values())
    for g in list(v.groups):
        ob.vertex_groups[g.group].remove([i])
    for g,x in top.items():
        if x>0: ob.vertex_groups[g].add([i],x/s,'REPLACE')
# check
bad=0; mx=0
for i in map(int,idx):
    v=ob.data.vertices[i]; s=sum(g.weight for g in v.groups); mx=max(mx,len(v.groups)); bad+=abs(s-1)>1e-4
print('post: max infl',mx,'bad sums',bad)
bpy.ops.wm.save_mainfile()
