"""bow-hand close-ups: left fist + bow grip in rest, walk and attack, three angles each."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, json, math
from mathutils import Vector, Matrix
W='/workspace/rowan_fresh/w2'; OUT=sys.argv[sys.argv.index('--')+1]
bpy.ops.wm.open_mainfile(filepath=W+'/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; F=json.load(open(W+'/fist.json')); gl=Vector(F['grip_local']); Rl=Matrix(F['grip_rot_local'])
before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=W+'/ROWAN_bow.fbx'); bo=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]
M=bo.matrix_world.copy(); bo.parent=None; bo.data.transform(M); bo.matrix_world=Matrix()
for x in set(bpy.data.objects)-before:
    if x is not bo: bpy.data.objects.remove(x)
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'; sc.render.resolution_x=sc.render.resolution_y=320
w_=bpy.data.worlds.new('bg'); sc.world=w_; w_.color=(0.45,0.45,0.45)
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=0.26
SH=[('rest',0),('walk',0),('walk',8),('walk',15),('walk',23),('attack',0),('attack',4),('attack',8),('attack',10),('attack',14),('attack',22)]
paths=[]
for act,f in SH:
    arm.animation_data.action=bpy.data.actions['ROWAN_'+act]; sc.frame_set(f)
    HM=arm.matrix_world@arm.pose.bones['mixamorig:LeftHand'].matrix; c=HM@gl; R=HM.to_3x3().normalized()@Rl
    bo.matrix_world=Matrix.Translation(c)@R.to_4x4()
    zb=R.col[2]; yb=R.col[1]; xb=R.col[0]
    for nm,D,up in (('knuckles',-yb if False else xb,zb),('palm',-xb,zb),('grip_end',zb,yb)):
        D=D.normalized(); cam.location=c+D*1.2; cam.rotation_euler=(-D).to_track_quat('-Z','Y').to_euler()
        # roll so the bow axis is screen-up
        q=(-D).to_track_quat('-Z','Y'); cam.rotation_euler=q.to_euler()
        cam.data.clip_start=1.2-0.14; cam.data.clip_end=1.2+0.6
        p=f'{OUT}_{act}_{f:02d}_{nm}.png'; sc.render.filepath=p; bpy.ops.render.render(write_still=True); paths.append((nm,f'{act} f{f}',p))
json.dump(paths,open(OUT+'_paths.json','w'))
