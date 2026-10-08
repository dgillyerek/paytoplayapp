import bpy, sys, numpy as np
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view
a=sys.argv[sys.argv.index('--')+1:]; BLEND,OUTP=a[0],a[1]; xray=len(a)>2
bpy.ops.wm.open_mainfile(filepath=BLEND)
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]
for o in bpy.data.objects:
    if o.type=='MESH' and xray: pass
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
sc.render.resolution_x=700; sc.render.resolution_y=900
cd=bpy.data.cameras.new('c'); cam=bpy.data.objects.new('c',cd); sc.collection.objects.link(cam); sc.camera=cam; cd.type='ORTHO'; cd.ortho_scale=2.1
from PIL import Image, ImageDraw
tgt=Vector((0,0,0.95))
outs=[]
for v,d in (('front',(0,-1,0)),('side',(1,0,0))):
    d=Vector(d); cam.location=tgt+d*4; cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
    p=f'{OUTP}_{v}.png'; sc.render.filepath=p; bpy.ops.render.render(write_still=True)
    im=Image.open(p).convert('RGB'); dr=ImageDraw.Draw(im); W,H=im.size
    for b in arm.pose.bones:
        h=world_to_camera_view(sc,cam,arm.matrix_world@b.head); t=world_to_camera_view(sc,cam,arm.matrix_world@b.tail)
        col=(255,60,60) if 'Left' in b.name else ((60,160,255) if 'Right' in b.name else (255,200,40))
        dr.line([(h.x*W,(1-h.y)*H),(t.x*W,(1-t.y)*H)],fill=col,width=3); dr.ellipse([h.x*W-4,(1-h.y)*H-4,h.x*W+4,(1-h.y)*H+4],fill=col)
    outs.append(im)
S=Image.new('RGB',(1400,900)); S.paste(outs[0],(0,0)); S.paste(outs[1],(700,0)); S.save(f'{OUTP}_bones.png')
