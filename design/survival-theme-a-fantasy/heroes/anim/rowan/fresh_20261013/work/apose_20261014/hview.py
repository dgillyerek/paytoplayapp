"""quick ortho close-ups: blender -b --python hview.py -- file.blend out_prefix cx,cy,cz scale [action frame] [extra.fbx-placement-json]"""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, math, json
from mathutils import Vector
a=sys.argv[sys.argv.index('--')+1:]
bpy.ops.wm.open_mainfile(filepath=a[0]); OUT=a[1]; C=Vector([float(x) for x in a[2].split(',')]); S=float(a[3])
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
sc.render.resolution_x=sc.render.resolution_y=int(a[6]) if len(a)>6 else 500
if len(a)>5 and a[4]!='-':
    arm=bpy.data.objects['ROWAN_rig']; arm.animation_data.action=bpy.data.actions[a[4]]; sc.frame_set(int(a[5]))
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=S
for nm,d in (('front',(0,-1,0)),('out',(1,0,0)),('in',(-1,0,0)),('back',(0,1,0)),('below',(0,0,-1))):
    D=Vector(d); cam.location=C+D*2; cam.rotation_euler=(-D).to_track_quat('-Z','Y' if nm!='below' else 'Z').to_euler()
    cam.data.clip_end=4; cam.data.clip_start=1.0 if nm!='in' else 1.88
    sc.render.filepath=f'{OUT}_{nm}.png'; bpy.ops.render.render(write_still=True)
