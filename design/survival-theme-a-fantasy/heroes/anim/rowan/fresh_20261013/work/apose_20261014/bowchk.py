import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,json,math
from mathutils import Vector, Matrix
bpy.ops.wm.open_mainfile(filepath='/workspace/rowan_fresh/w2/anim.blend'); W='/workspace/rowan_fresh/w2'
arm=bpy.data.objects['ROWAN_rig']; sc=bpy.context.scene; F=json.load(open(W+'/fist.json')); Rl=Matrix(F['grip_rot_local'])
for act in ('rest','walk','attack'):
    arm.animation_data.action=bpy.data.actions['ROWAN_'+act]
    for f in ((0,) if act=='rest' else (0,8,10,14,17) if act=='attack' else (0,8)):
        sc.frame_set(f); HM=arm.matrix_world@arm.pose.bones['mixamorig:LeftHand'].matrix; R=HM.to_3x3().normalized()@Rl
        z=R.col[2]; y=R.col[1]; print('BOW',act,f,'axis',tuple(round(v,2) for v in z),'tilt_from_vertical',round(math.degrees(math.acos(min(1,abs(z.z)))),1),'string_dir',tuple(round(v,2) for v in y))
