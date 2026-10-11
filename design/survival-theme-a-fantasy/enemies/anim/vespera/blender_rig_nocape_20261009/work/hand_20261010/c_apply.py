import bpy, numpy as np, sys
OUT=sys.argv[sys.argv.index('--')+1]
body=bpy.data.objects['VESPERA_body']; me=body.data
d=np.load('/workspace/vespera-hand/work/weights.npz'); X=d['X']; names=['mixamorig:'+n for n in d['names']]
assert X.shape[0]==len(me.vertices)
for g in list(body.vertex_groups): body.vertex_groups.remove(g)
G=[body.vertex_groups.new(name=n) for n in names]
for j,g in enumerate(G):
    idx=np.nonzero(X[:,j]>0)[0]
    for w in np.unique(np.round(X[idx,j],5)):
        sel=idx[np.round(X[idx,j],5)==w]; g.add(sel.tolist(),float(w),'REPLACE')
print('GROUPS',[g.name for g in body.vertex_groups])
bpy.ops.wm.save_as_mainfile(filepath=OUT,compress=True)
