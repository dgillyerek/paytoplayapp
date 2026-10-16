import bpy,math,sys
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
crig=bpy.data.objects['BONEQUILL_cape_rig']
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P_+bn
act,fr,tag=sys.argv[-3],int(sys.argv[-2]),sys.argv[-1]
arm.animation_data.action=bpy.data.actions[act]; sc.frame_set(fr)
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'; sc.render.resolution_x=400; sc.render.resolution_y=400
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=0.75
for k,(loc,rot) in {'front':((0,-5,1.5),(math.pi/2,0,0)),'side':((5,0,1.5),(math.pi/2,0,math.pi/2)),'back':((0,5,1.5),(math.pi/2,0,math.pi))}.items():
    cam.location=loc; cam.rotation_euler=rot; sc.render.filepath=f'{W}/hood_{tag}_{k}.png'; bpy.ops.render.render(write_still=True)
