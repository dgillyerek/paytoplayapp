"""Quiver with bone arrows (Meshy didn't generate one): modelled here, sat on the back OUTSIDE the fitted cloak, over the right
shoulder like the approved art, joined into the body mesh and skinned 100% to Spine2 (rigid). UVs point at body-texture patches
(dark armour leather, bone, purple rune) so it keeps the single body material."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, bmesh, numpy as np, json, math
from mathutils import Vector, Matrix
W_='/workspace/bonequill_gritty/work'
bpy.ops.wm.open_mainfile(filepath=W_+'/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; body=bpy.data.objects['BONEQUILL_body']
arm.animation_data.action=bpy.data.actions['BONEQUILL_rest']; sc.frame_set(0)
with bpy.data.libraries.load(W_+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape']
cape=d.objects[0]; C=np.array([v.co[:] for v in cape.data.vertices])
me=body.data; N=len(me.vertices); P=np.array([v.co[:] for v in me.vertices])
uvl=me.uv_layers.active.data; loopv=np.array([l.vertex_index for l in me.loops]); UV=np.array([d.uv[:] for d in uvl])
img=[n.image for n in me.materials[0].node_tree.nodes if n.type=='TEX_IMAGE' and n.image and 'Base' in (n.label+n.name+n.image.name) ] or [n.image for n in me.materials[0].node_tree.nodes if n.type=='TEX_IMAGE']
img=img[0]; w,h=img.size; px=np.array(img.pixels[:]).reshape(h,w,4)[:,:,:3]
def col(uv): i=np.clip((uv*[w,h]).astype(int),0,[w-1,h-1]); return px[i[:,1],i[:,0]]
cols=col(UV)
lum=cols.mean(1); purple=(cols[:,2]-cols[:,1])+(cols[:,0]-cols[:,1])*0.5
z_l=P[loopv,2]
uv_dark=UV[np.argsort(np.abs(lum-np.percentile(lum[(z_l>0.2)&(z_l<0.5)],30))+(~((z_l>0.2)&(z_l<0.5)))*9)[:1]][0]
uv_bone=UV[np.argmax(lum*(z_l>1.65))]
uv_purp=UV[np.argmax(purple)]
print('UV',uv_dark,uv_bone,uv_purp,'cols',col(np.array([uv_dark,uv_bone,uv_purp])))
# axis: top over the right shoulder, bottom toward the left hip, behind the cloak
top=Vector((-0.15,0,1.52)); bot=Vector((0.07,0,1.02)); ax=(top-bot).normalized(); R=0.055   # mouth over the RIGHT shoulder (draw-hand side) so he can pull an arrow; ax=(top-bot).normalized(); R=0.055
ts=np.linspace(0,1,12); yb=[]
for t in ts:
    p=bot.lerp(top,t); m=(np.abs(C[:,2]-p.z)<0.05)&(np.abs(C[:,0]-p.x)<0.09)&(C[:,1]>0); yb.append(C[m,1].max() if m.any() else 0.2)
print('yb',np.round(yb,3)); bot.y=float(np.percentile(yb[:9],60))+R; top.y=bot.y; ax=(top-bot).normalized()
bm=bmesh.new(); groups=[]
def tube(a,b,r,seg,uv):
    a=Vector(a);b=Vector(b); d=(b-a); L=d.length; q=Vector((0,0,1)).rotation_difference(d.normalized())
    res=bmesh.ops.create_cone(bm,cap_ends=True,segments=seg,radius1=r,radius2=r,depth=L,matrix=Matrix.Translation((a+b)/2)@q.to_matrix().to_4x4())
    groups.append((res['verts'],uv))
tube(bot,top,R,14,uv_dark)                                 # quiver body
tube(bot-ax*0.01,bot+ax*0.03,R*1.12,14,uv_purp)             # rune-trimmed bottom band
tube(top-ax*0.03,top+ax*0.005,R*1.12,14,uv_purp)            # mouth band
rng=np.random.default_rng(7)
for k in range(6):                                           # bone arrows: shafts + purple fletching
    o=Vector(((k%3-1)*0.022,(k//3-0.5)*0.022,0)); o=Vector((0,0,1)).rotation_difference(ax).to_matrix()@o
    a=top+o-ax*0.05; b=top+o+ax*(0.13+0.03*rng.random())
    tube(a,b,0.006,6,uv_bone)
    f0=b-ax*0.075; side=ax.cross(Vector((0,1,0))).normalized()
    for sgn in (1,-1):
        v=[bm.verts.new(f0),bm.verts.new(b),bm.verts.new(b+side*0.022*sgn),bm.verts.new(f0+side*0.018*sgn)]; bm.faces.new(v); groups.append((v,uv_purp))
qm=bpy.data.meshes.new('quiver'); bm.verts.ensure_lookup_table()
vuv={}
for vs,uv in groups:
    for v in vs: vuv[v.index]=uv
bm.to_mesh(qm); quv=qm.uv_layers.new(name=me.uv_layers.active.name)
for l in qm.loops: quv.data[l.index].uv=vuv.get(l.vertex_index,uv_dark)
q=bpy.data.objects.new('quiver',qm); sc.collection.objects.link(q); q.data.materials.append(me.materials[0])
vg=q.vertex_groups.new(name='mixamorig:Spine2'); vg.add(list(range(len(qm.vertices))),1.0,'REPLACE')
for o in bpy.context.view_layer.objects: o.select_set(False)
q.select_set(True); body.select_set(True); bpy.context.view_layer.objects.active=body; bpy.ops.object.join()
rep={'quiver_verts':len(qm.vertices),'quiver_y_top':round(top.y,3),'quiver_y_bot':round(bot.y,3),'body_faces':len(body.data.polygons)}
bpy.ops.wm.save_as_mainfile(filepath=W_+'/anim.blend',compress=True); json.dump(rep,open(W_+'/quiver.json','w')); print('QUIVER',rep)
