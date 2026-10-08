"""Vertex classification -> face mask with island rules; debug render of final mask."""
import bpy, bmesh, sys, os, numpy as np
from mathutils import Vector
src,label=sys.argv[sys.argv.index('--')+1:][:2]
bpy.ops.wm.open_mainfile(filepath=src)
body=next(o for o in bpy.data.objects if o.type=='MESH')
d=np.load(f'cls_{label}.npz'); cl=d['cloth']; zcut=float(d['zcut']); z0=float(d['z0']); H=float(d['H'])
bm=bmesh.new(); bm.from_mesh(body.data); bm.transform(body.matrix_world); bm.faces.ensure_lookup_table()
fc=np.array([sum(cl[v.index] for v in f.verts)>=2 for f in bm.faces])
fz=np.array([max(v.co.z for v in f.verts) for f in bm.faces])
def islands(mask):
    lab=-np.ones(len(mask),int); n=0
    for f in bm.faces:
        if not mask[f.index] or lab[f.index]>=0: continue
        st=[f]; lab[f.index]=n
        while st:
            x=st.pop()
            for e in x.edges:
                for g in e.link_faces:
                    if mask[g.index] and lab[g.index]<0: lab[g.index]=n; st.append(g)
        n+=1
    return lab,n
for it in range(2):
    lab,n=islands(fc)
    keep=set(np.unique(lab[(lab>=0)&(fz>zcut-0.025)]))       # cloth must hang from the waist seam
    drop=[i for i in range(n) if i not in keep]
    fc[np.isin(lab,drop)]=False
    lab,n=islands(~fc); sz=np.bincount(lab[lab>=0],minlength=n); big=np.argmax(sz)
    if it==0: print('BODY ISLANDS',n,sorted(sz.tolist(),reverse=True)[:6])
    fc[(lab>=0)&(lab!=big)]=True
lab,n=islands(fc); sz=sorted(np.bincount(lab[lab>=0],minlength=n).tolist(),reverse=True)
print('MASK',label,'cloth faces',int(fc.sum()),'body faces',int((~fc).sum()),'cloth islands',n,sz[:8])
np.save(f'fc_{label}.npy',fc)
me=body.data
fcol=me.color_attributes.new('m','BYTE_COLOR','CORNER')
cols=np.zeros((len(me.loops),4)); 
for poly in me.polygons:
    c=[1.0,0.35,0.1,1] if fc[poly.index] else [0.75,0.75,0.75,1]
    for li in poly.loop_indices: cols[li]=c
fcol.data.foreach_set('color',cols.ravel()); me.color_attributes.active_color=fcol
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='VERTEX'
sc.render.resolution_x,sc.render.resolution_y=500,800
for o in list(bpy.data.objects):
    if o.type in {'CAMERA','LIGHT'}: bpy.data.objects.remove(o)
cd=bpy.data.cameras.new('c'); cam=bpy.data.objects.new('c',cd); sc.collection.objects.link(cam); sc.camera=cam
c=Vector((0,0,z0+0.40*H))
for name,dd in (('front',(0,-1,0.15)),('rear',(0,1,0.15)),('side',(1,0,0.1)),('below',(0.25,-0.35,-1))):
    cam.location=c+Vector(dd).normalized()*H*2.2; cam.rotation_euler=(c-cam.location).to_track_quat('-Z','Y').to_euler()
    sc.render.filepath=f'dbg/{label}_{name}.png'; bpy.ops.render.render(write_still=True)
