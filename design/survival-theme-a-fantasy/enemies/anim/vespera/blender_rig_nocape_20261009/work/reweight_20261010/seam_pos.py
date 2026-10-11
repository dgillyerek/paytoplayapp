import bpy, numpy as np
from mathutils import kdtree
me=bpy.data.objects['VESPERA_body'].data; V0=65195
co=np.array([v.co[:] for v in me.vertices])[:V0]
kd=kdtree.KDTree(V0)
for i in range(V0): kd.insert(co[i],i)
kd.balance(); S=set()
for i in range(V0):
    r=kd.find_range(co[i],1e-6)
    if len(r)>1: S.add(i)
np.save('/workspace/vespera-reweight/work/seam_pos.npy',co[sorted(S)]); print('SEAMV',len(S))
