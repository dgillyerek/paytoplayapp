import bpy,math,json
from mathutils import Vector,Matrix
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
bpy.ops.import_scene.fbx(filepath=W+'/BONEQUILL_bow.fbx'); bow=[o for o in bpy.context.selected_objects if o.type=='MESH'][0]; bow.parent=None
FI=json.load(open(W+'/fist.json')); GL=Vector(FI['grip_local'])
A=bpy.data.actions; arm.animation_data.action=A['BONEQUILL_attack']; sc.frame_set(10)
bm=lambda: arm.matrix_world@arm.pose.bones[P_+'LeftHand'].matrix
M=bm(); R=M.to_3x3().normalized(); op=R.inverted()@((M@GL)-M.to_translation()); oR=R.inverted()
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'; sc.render.resolution_x=360; sc.render.resolution_y=360
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=0.32
for an,f in (('BONEQUILL_rest',0),('BONEQUILL_walk',8),('BONEQUILL_attack',10)):
    arm.animation_data.action=A[an]; sc.frame_set(f); M=bm(); R=M.to_3x3().normalized()
    g=M.to_translation()+R@op; bow.matrix_world=Matrix.Translation(g)@(R@oR).to_4x4()
    for k,d in {'outside':Vector((1,-0.3,0.1)),'inside':Vector((-1,-0.6,0.15)),'front':Vector((0.15,-1,0.25))}.items():
        d.normalize(); cam.location=g+d*2.5; cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=f'{W}/bh_{an[10:]}_{k}.png'; bpy.ops.render.render(write_still=True)
