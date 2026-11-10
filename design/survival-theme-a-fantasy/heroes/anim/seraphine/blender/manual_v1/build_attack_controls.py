"""Create a non-destructive IK control rig for Seraphine's two new attacks."""
import bpy
import os
from mathutils import Vector

OUT = r"C:\Development\paytoplayapp\design\survival-theme-a-fantasy\heroes\anim\seraphine\blender\manual_v1\SERAPHINE_attack_controls.blend"
ARM = bpy.data.objects["SERAPHINE_rig"]
P = "mixamorig:"

def control(name, location):
    old = bpy.data.objects.get(name)
    if old:
        bpy.data.objects.remove(old, do_unlink=True)
    bpy.ops.object.empty_add(type="SPHERE", location=location)
    obj = bpy.context.object
    obj.name = name
    obj.empty_display_size = 0.08
    obj.empty_display_type = "SPHERE"
    return obj

def bone_head(name):
    return ARM.matrix_world @ ARM.pose.bones[P + name].head

right_hand = control("CTRL_attack_right_hand", bone_head("RightHand"))
left_hand = control("CTRL_attack_left_hand", bone_head("LeftHand"))
right_aim = control("CTRL_attack_right_aim", bone_head("RightHand") + Vector((0, -1, 0.2)))

def clear_constraints():
    for name in ("RightForeArm", "LeftForeArm", "RightHand"):
        pb = ARM.pose.bones[P + name]
        for constraint in list(pb.constraints):
            pb.constraints.remove(constraint)

clear_constraints()
for side, target in (("Right", right_hand), ("Left", left_hand)):
    ik = ARM.pose.bones[P + side + "ForeArm"].constraints.new("IK")
    ik.name = "Attack hand IK"
    ik.target = target
    ik.chain_count = 2
    ik.use_stretch = False

track = ARM.pose.bones[P + "RightHand"].constraints.new("TRACK_TO")
track.name = "Weapon aim"
track.target = right_aim
track.track_axis = "TRACK_X"
track.up_axis = "UP_Z"

def animate_target(obj, points):
    obj.animation_data_clear()
    for frame, point in points:
        obj.location = point
        obj.keyframe_insert("location", frame=frame)

# The targets define physically reachable hand arcs.  The actual animation is baked
# into the 24-bone game skeleton after posing and review; these controls never ship.
animate_target(right_hand, [(1, (-.40,-.20,1.24)), (12, (-.38,-.10,1.34)),
                            (22, (-.30,-.48,1.30)), (32, (-.36,-.30,1.27)), (52, (-.40,-.20,1.24))])
animate_target(left_hand, [(1, (.23,-.14,.94)), (12, (.10,-.24,1.16)),
                           (22, (.04,-.46,1.20)), (32, (.12,-.28,1.10)), (52, (.23,-.14,.94))])
animate_target(right_aim, [(1, (-.40,-1.2,1.44)), (12, (-.28,-.90,1.44)),
                           (22, (-.26,-1.48,1.50)), (32, (-.30,-1.10,1.46)), (52, (-.40,-1.2,1.44))])

bpy.context.scene.frame_start = 1
bpy.context.scene.frame_end = 52
bpy.ops.wm.save_as_mainfile(filepath=OUT, copy=False)
print("Saved", OUT)
