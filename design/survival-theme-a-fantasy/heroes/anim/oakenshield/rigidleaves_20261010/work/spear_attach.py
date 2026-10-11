# import the thorn spear and keep it on the RightHand with the same offset Unity binds at f10 (for review renders only)
import bpy
from mathutils import Vector, Matrix
SP='/workspace/oakenshield-fix/repo/Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/oakenshield/OAKENSHIELD_thornspear.fbx'
arm=bpy.data.objects['OAKENSHIELD_rig']; sc=bpy.context.scene
before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=SP)
new=[o for o in bpy.data.objects if o not in before]
root=bpy.data.objects.new('SPEAR_ROOT',None); sc.collection.objects.link(root)
for o in new:
    if o.parent is None: o.parent=root
arm.animation_data.action=bpy.data.actions['OAKENSHIELD_attack']; sc.frame_set(10)
pb=arm.pose.bones['mixamorig:RightHand']; M=arm.matrix_world@pb.matrix; R=M.to_3x3().normalized()
g=M.to_translation()+R@Vector((0,0.07,0))
tip=Vector((0,-1,0.12)).normalized(); y=-tip; x=Vector((1,0,0)); x=(x-x.dot(y)*y).normalized(); z=x.cross(y)
Rsp=Matrix((x,y,z)).transposed()
W=Matrix.Translation(g)@Rsp.to_4x4()
c=root.constraints.new('CHILD_OF'); c.target=arm; c.subtarget='mixamorig:RightHand'
c.inverse_matrix=(arm.matrix_world@pb.matrix).inverted(); root.matrix_world=W
# scale keys (conjured f3-8) and hide after release
for f in range(31):
    s=0.05 if f<3 else (min(1.0,0.05+0.95*(f-3)/5.0) if f<=8 else 1.0)
    root.scale=(s,s,s); root.keyframe_insert('scale',frame=f)
    for o in new:
        o.hide_render=not(3<=f<14); o.keyframe_insert('hide_render',frame=f)
