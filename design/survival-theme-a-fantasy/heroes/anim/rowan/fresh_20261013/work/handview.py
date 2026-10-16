import bpy,math,numpy as np,sys
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/work/anim.blend')
tag=sys.argv[-1]
body=bpy.data.objects['ROWAN_body']; me=body.data; N=len(me.vertices)
gi={body.vertex_groups[n].index:n for n in body.vertex_groups.keys()}
w=np.zeros(N); h=np.zeros(N)
for v in me.vertices:
    for g in v.groups:
        n=gi[g.group]
        if any(k in n for k in ('Arm','Hand')): w[v.index]+=g.weight
        if n.endswith('Hips'): h[v.index]+=g.weight
ca=me.color_attributes.new('wc','FLOAT_COLOR','POINT'); ca.data.foreach_set('color',np.c_[w,h*0.2,h,np.ones(N)].ravel())
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.color_type='VERTEX'; sc.render.resolution_x=500; sc.render.resolution_y=500
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=0.42
for side,xx in (('L',0.29),('R',-0.26)):
    for k,(loc,rot) in {'front':((xx,-5,0.92),(math.pi/2,0,0)),'out':((xx+5*(1 if side=='L' else -1),-0.1,0.92),(math.pi/2,0,math.pi/2 if side=='L' else -math.pi/2))}.items():
        cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'/workspace/rowan_fresh/work/hv_{tag}_{side}_{k}.png'; bpy.ops.render.render(write_still=True)
