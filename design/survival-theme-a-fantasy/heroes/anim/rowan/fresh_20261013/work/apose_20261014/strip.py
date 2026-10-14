"""frame-by-frame strip of the raise: blender -b --python strip.py -- anim.blend out.png f0,f1,... [scale] [cz]"""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, math, json
from mathutils import Vector, Matrix
a=sys.argv[sys.argv.index('--')+1:]; W='/workspace/rowan_fresh/w2'
bpy.ops.wm.open_mainfile(filepath=a[0]); OUT=a[1]; FR=[int(x) for x in a[2].split(',')]; S=float(a[3]) if len(a)>3 else 1.2; CZ=float(a[4]) if len(a)>4 else 1.2
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; arm.animation_data.action=bpy.data.actions['ROWAN_attack']
crig=bpy.data.objects['ROWAN_coat_rig']
F=json.load(open(W+'/fist.json'))
before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=W+'/ROWAN_bow.fbx'); bo=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]
M=bo.matrix_world.copy(); bo.parent=None; bo.data.transform(M); bo.matrix_world=Matrix()
for x in set(bpy.data.objects)-before:
    if x is not bo: bpy.data.objects.remove(x)
gl=Vector(F['grip_local']); Rl=Matrix(F['grip_rot_local'])
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
sc.render.resolution_x=300; sc.render.resolution_y=400
w_=bpy.data.worlds.new('bg'); sc.world=w_; w_.color=(0.45,0.45,0.45)
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=S
paths=[]
for view,loc,rot in (('front',(0,-5,CZ),(math.pi/2,0,0)),('side',(5,0,CZ),(math.pi/2,0,math.pi/2))):
    for f in FR:
        sc.frame_set(f)
        for pb in crig.pose.bones: pb.rotation_mode='QUATERNION'
        crig.pose.bones['coat_Hips'].matrix=arm.pose.bones['mixamorig:Hips'].matrix.copy(); bpy.context.view_layer.update()
        HM=arm.matrix_world@arm.pose.bones['mixamorig:LeftHand'].matrix
        bo.matrix_world=Matrix.Translation(HM@gl)@(HM.to_3x3().normalized()@Rl).to_4x4()
        cam.location=loc; cam.rotation_euler=rot
        p=f'{OUT}_{view}_{f:02d}.png'; sc.render.filepath=p; bpy.ops.render.render(write_still=True); paths.append((view,f,p))
json.dump(paths,open(OUT+'_paths.json','w'))
