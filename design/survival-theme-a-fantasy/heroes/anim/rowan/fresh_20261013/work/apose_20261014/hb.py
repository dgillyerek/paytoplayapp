import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, json, math
from mathutils import Vector, Matrix
a=sys.argv[sys.argv.index('--')+1:]; W='/workspace/rowan_fresh/w2'
bpy.ops.wm.open_mainfile(filepath=a[0]); OUT=a[1]; act=a[2] if len(a)>2 else '-'; fr=int(a[3]) if len(a)>3 else 0
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']
if act!='-': arm.animation_data.action=bpy.data.actions[act]
sc.frame_set(fr)
F=json.load(open(W+'/fist.json'))
before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=W+'/ROWAN_bow.fbx'); bo=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]
M=bo.matrix_world.copy(); bo.parent=None; bo.data.transform(M); bo.matrix_world=Matrix()
for x in set(bpy.data.objects)-before:
    if x is not bo: bpy.data.objects.remove(x)
HM=arm.matrix_world@arm.pose.bones['mixamorig:LeftHand'].matrix
gl=Vector(F['grip_local']); Rl=Matrix(F['grip_rot_local'])
bo.matrix_world=Matrix.Translation(HM@gl)@(HM.to_3x3().normalized()@Rl).to_4x4()
c=HM@gl
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'; sc.render.resolution_x=sc.render.resolution_y=500
w_=bpy.data.worlds.new('bg'); sc.world=w_; w_.color=(0.45,0.45,0.45)
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=0.24
dirs=[('a',(0,-1,0)),('b',(1,0,0)),('c',(0,1,0)),('d',(-1,0,0)),('e',(0,0,1)),('f',(0,0,-1))]
for nm,d in dirs:
    D=Vector(d); cam.location=c+D*1.5; up='Y' if abs(D.z)<0.9 else 'Y'
    cam.rotation_euler=(-D).to_track_quat('-Z','Y' if abs(D.z)<0.9 else 'X').to_euler(); cam.data.clip_start=1.5-0.12 if nm in('d',) else 0.5; cam.data.clip_end=3
    sc.render.filepath=f'{OUT}_{nm}.png'; bpy.ops.render.render(write_still=True)
