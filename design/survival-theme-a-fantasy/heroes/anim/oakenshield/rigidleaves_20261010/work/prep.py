"""Step 1: from the clothsplit blend, drop the hanging cloth object + its 14 cloth bones and the old actions,
record joints (bone heads/tails), sample per-vertex basecolor. Output prep.blend, joints.json, vcol.npy"""
import bpy, numpy as np, json, sys
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]
arm=bpy.data.objects['OAKENSHIELD_rig']; body=bpy.data.objects['OAKENSHIELD_body']; cloth=bpy.data.objects['OAKENSHIELD_cloth']
rep={'cloth_faces_removed':len(cloth.data.polygons),'cloth_verts_removed':len(cloth.data.vertices)}
bpy.data.objects.remove(cloth,do_unlink=True)
bpy.context.view_layer.objects.active=arm; bpy.ops.object.mode_set(mode='EDIT')
gone=[b.name for b in arm.data.edit_bones if not b.name.startswith('mixamorig:')]
for n in gone: arm.data.edit_bones.remove(arm.data.edit_bones[n])
bpy.ops.object.mode_set(mode='OBJECT')
rep['cloth_bones_removed']=gone
for g in list(body.vertex_groups):
    if not g.name.startswith('mixamorig:'): body.vertex_groups.remove(g)
J={}
for b in arm.data.bones:
    n=b.name.split(':')[1]; J[n]=list(b.head_local)
    J[n+'_tail']=list(b.tail_local)
J['HeadTop']=J['Head_tail']
for s in ('Left','Right'): J[s+'HandEnd']=J[s+'Hand_tail']; J[s+'ToeEnd']=J[s+'ToeBase_tail']
json.dump(J,open(OUT+'/joints.json','w'),indent=0)
# sample basecolor at each vertex (mean of its loop UVs)
me=body.data; img=None
for m in me.materials:
    for n in m.node_tree.nodes:
        if n.type=='TEX_IMAGE' and 'basecolor' in n.image.name.lower(): img=n.image
print('IMG',img.name,img.size[:])
W,H=img.size; px=np.array(img.pixels[:],dtype=np.float32).reshape(H,W,4)
uv=np.zeros(len(me.loops)*2); me.uv_layers.active.data.foreach_get('uv',uv); uv=uv.reshape(-1,2)
lv=np.zeros(len(me.loops),int); me.loops.foreach_get('vertex_index',lv)
su=np.zeros((len(me.vertices),2)); cnt=np.zeros(len(me.vertices)); np.add.at(su,lv,uv); np.add.at(cnt,lv,1)
vu=su/np.maximum(cnt,1)[:,None]
xi=np.clip((vu[:,0]%1)*W,0,W-1).astype(int); yi=np.clip((vu[:,1]%1)*H,0,H-1).astype(int)
col=px[yi,xi,:3]; np.save(OUT+'/vcol.npy',col)
for a in list(bpy.data.actions): 
    rep.setdefault('old_actions',[]).append(a.name)
arm.animation_data_clear()
bpy.ops.wm.save_as_mainfile(filepath=OUT+'/prep.blend',compress=True)
json.dump(rep,open(OUT+'/prep.json','w'),indent=1); print('PREP',rep)
