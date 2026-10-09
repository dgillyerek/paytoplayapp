import bpy
for a in bpy.data.actions:
    for fc in a.fcurves:
        for kp in fc.keyframe_points:
            kp.co.x-=1; kp.handle_left.x-=1; kp.handle_right.x-=1
        fc.update()
    print(a.name,a.frame_range[:])
bpy.context.scene.render.fps=24
bpy.ops.wm.save_as_mainfile(filepath='/workspace/rig_meshy08/out/build_s.blend')
