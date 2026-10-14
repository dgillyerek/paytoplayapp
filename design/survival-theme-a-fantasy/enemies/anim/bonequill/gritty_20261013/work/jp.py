import bpy,sys
sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; arm.animation_data.action=bpy.data.actions['BONEQUILL_attack']
for f in (6,10,14,17):
    sc.frame_set(f); print(f,{n:tuple(round(v,3) for v in arm.pose.bones['mixamorig:'+n].head) for n in ('RightShoulder','RightArm','RightForeArm','RightHand','LeftArm','LeftHand','Spine2')})
