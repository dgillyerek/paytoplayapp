import bpy,math,numpy as np
from mathutils import Vector,Quaternion
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/work/anim.blend')
arm=bpy.data.objects['ROWAN_rig']; body=bpy.data.objects['ROWAN_body']; P_='mixamorig:'; PB=arm.pose.bones
arm.animation_data.action=None
for pb in PB: pb.rotation_mode='QUATERNION'; pb.location=(0,0,0); pb.rotation_quaternion=(1,0,0,0); pb.scale=(1,1,1)
def rot(n,axis,deg):
    pb=PB[P_+n]; R=pb.bone.matrix_local.to_3x3(); q=Quaternion(Vector(axis),math.radians(deg))
    pb.rotation_quaternion=(R.inverted()@q.to_matrix()@R).to_quaternion()@pb.rotation_quaternion
rot('LeftArm',(0,1,0),-75); rot('RightArm',(0,1,0),75); bpy.context.view_layer.update()
print('LHand',tuple(round(x,3) for x in (arm.matrix_world@PB[P_+'LeftHand'].head)[:]))
print('RHand',tuple(round(x,3) for x in (arm.matrix_world@PB[P_+'RightHand'].head)[:]))
sc=bpy.context.scene; sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.color_type='TEXTURE'; sc.display.shading.light='STUDIO'
sc.render.resolution_x=600; sc.render.resolution_y=900
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=1.95
for k,(loc,rot_) in {'front':((0,-5,0.9),(math.pi/2,0,0)),'side':((5,0,0.9),(math.pi/2,0,math.pi/2))}.items():
    cam.location=loc; cam.rotation_euler=rot_; sc.render.filepath=f'/workspace/rowan_fresh/work/pose2_{k}.png'; bpy.ops.render.render(write_still=True)
