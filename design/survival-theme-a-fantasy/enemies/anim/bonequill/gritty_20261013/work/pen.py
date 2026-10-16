import bpy,numpy as np
from mathutils import Vector
sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; arm.animation_data.action=bpy.data.actions['BONEQUILL_attack']; PB=arm.pose.bones
bad=[]
for f in range(31):
    sc.frame_set(f); M=(PB['mixamorig:Spine2'].matrix).inverted()
    worst=9
    for a,b in (('RightArm','RightForeArm'),('RightForeArm','RightHand'),('LeftArm','LeftForeArm'),('LeftForeArm','LeftHand')):
        A=PB['mixamorig:'+a].head; B=PB['mixamorig:'+b].head
        for t in np.linspace(0.25 if a.endswith('Arm') and 'Fore' not in a else 0,1,12):
            p=M@(A.lerp(B,t)); # spine2 local: y up the bone, x/z across
            if -0.35<p.y<0.15:  # Spine2 head .. neck, down to the belly
                r=(p.x/0.15)**2+(p.z/0.12)**2; worst=min(worst,r)
    bad.append(round(worst,2))
print('PEN (ellipse r^2, <1 = inside ribcage)',bad)
