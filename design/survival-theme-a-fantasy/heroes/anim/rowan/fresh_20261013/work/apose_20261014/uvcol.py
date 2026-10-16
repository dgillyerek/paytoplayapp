import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,numpy as np
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/w2/dec_body.blend')
o=bpy.data.objects['ROWAN_body']; me=o.data
nl=len(me.loops); uv=np.empty(nl*2); me.uv_layers.active.data.foreach_get('uv',uv); uv=uv.reshape(-1,2)
lv=np.empty(nl,int); me.loops.foreach_get('vertex_index',lv)
mat=me.materials[0]; b=[n for n in mat.node_tree.nodes if n.type=='BSDF_PRINCIPLED'][0]
im=b.inputs['Base Color'].links[0].from_node.image; w,h=im.size; px=np.empty(w*h*4,np.float32); im.pixels.foreach_get(px); px=px.reshape(h,w,4)
X=np.clip((uv[:,0]%1)*w,0,w-1).astype(int); Y=np.clip((uv[:,1]%1)*h,0,h-1).astype(int); c=px[Y,X,:3]
N=len(me.vertices); col=np.zeros((N,3)); cnt=np.zeros(N); np.add.at(col,lv,c); np.add.at(cnt,lv,1); col/=np.maximum(cnt,1)[:,None]
np.save('vcol.npy',col); print('ok',im.name,im.size, col.mean(0))
