import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages"); sys.path.insert(0,'/workspace/rowan_fresh/w2/bowgrip')
import bpy,json,math,os
from mathutils import Vector,Matrix
from bowroll import bowrot
W='/workspace/rowan_fresh/w2'; P='mixamorig:'; BL=os.environ.get('BL',W+'/anim.blend'); TAG=os.environ.get('TAG','new'); O='/tmp/rr'
bpy.ops.wm.open_mainfile(filepath=BL)
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; crig=bpy.data.objects['ROWAN_coat_rig']
if crig.animation_data: crig.animation_data.action=None
for pb in crig.pose.bones: pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=(1,0,0,0); pb.location=(0,0,0)
before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=W+'/ROWAN_bow.fbx'); bow=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]; bow.parent=None
GL=Vector(json.load(open(W+'/grip.json'))['grip_local'])
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
w_=bpy.data.worlds.new('bg'); sc.world=w_; w_.color=(0.45,0.45,0.45)
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'
def setf(an,f):
    arm.animation_data.action=bpy.data.actions['ROWAN_'+an]; sc.frame_set(f)
    crig.pose.bones['coat_Hips'].matrix=arm.pose.bones[P+'Hips'].matrix.copy(); bpy.context.view_layer.update()
    M=arm.matrix_world@arm.pose.bones[P+'LeftHand'].matrix; R=M.to_3x3().normalized(); g=M.to_translation()+R@GL
    bow.matrix_world=Matrix.Translation(g)@(R@bowrot(an,f)).to_4x4(); bpy.context.view_layer.update(); return g
def shoot(path,loc,d,scale,rx,ry):
    sc.render.resolution_x=rx; sc.render.resolution_y=ry; cam.data.ortho_scale=scale
    d=Vector(d).normalized(); cam.location=loc+d*4; cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler(); sc.render.filepath=path; bpy.ops.render.render(write_still=True)
MODE=os.environ.get('MODE','hand')
if MODE=='hand':
    for an,f in (('rest',0),('walk',8),('attack',4),('attack',10),('attack',20)):
        g=setf(an,f)
        for k,d in {'front':(0.1,-1,0.15),'outside':(1,-0.35,0.1),'top':(0.2,-0.3,1)}.items(): shoot(f'{O}/hand_{an}{f:02d}_{k}.png',g,d,0.5,360,360)
        shoot(f'{O}/carry_{an}{f:02d}_front.png',Vector((0.15,0,1.05)),(0.1,-1,0.1),1.6,500,600)
elif MODE=='walk':
    for f in (0,4,8,12,15,19,23,27):
        setf('walk',f); shoot(f'{O}/walk_{TAG}_{f:02d}.png',Vector((0,0.1,0.55)),(0,-1,0),1.3,320,520)
        shoot(f'{O}/walkfeet_{TAG}_{f:02d}.png',Vector((0,0.1,0.3)),(0,-0.25,1),1.0,320,320)
else:
    for f in (0,3,5,8,10,14,18,22,26,30):
        setf('attack',f); shoot(f'{O}/atk_side_{f:02d}.png',Vector((0,-0.1,0.95)),(1,0,0.05),2.2,300,560); shoot(f'{O}/atk_front_{f:02d}.png',Vector((0,-0.1,0.95)),(0.15,-1,0.05),2.2,300,560)
