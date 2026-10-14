import bpy,numpy as np,sys
o=[o for o in bpy.data.objects if o.type=='MESH'][0]; me=o.data
P=np.empty(len(me.vertices)*3); me.vertices.foreach_get('co',P); np.save(sys.argv[-1],P.reshape(-1,3))
