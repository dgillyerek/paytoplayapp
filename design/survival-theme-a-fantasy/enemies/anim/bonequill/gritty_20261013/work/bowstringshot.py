import sys; sys.path.insert(0,'/workspace/bonequill_gritty/work'); from bowroll import bowrot, roll
import bpy,math,json
from mathutils import Vector,Matrix
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
bpy.ops.import_scene.fbx(filepath=W+'/BONEQUILL_bow.fbx'); bow=[o for o in bpy.context.selected_objects if o.type=='MESH'][0]; bow.parent=None
FI=json.load(open(W+'/fist.json')); GL=Vector(FI['grip_local'])
A=bpy.data.actions; arm.animation_data.action=A['BONEQUILL_attack']; sc.frame_set(10)
bm=lambda: arm.matrix_world@arm.pose.bones[P_+'LeftHand'].matrix
M=bm(); R=M.to_3x3().normalized(); op=GL.copy(); oR=(Matrix(((-1,0,0),(0,-1,0),(0,0,1)))@Matrix.Rotation(math.radians(45),3,'Z'))
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'; sc.render.resolution_x=360; sc.render.resolution_y=360
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=1.5
for an,f in (('BONEQUILL_rest',0),('BONEQUILL_walk',8),('BONEQUILL_walk',23),('BONEQUILL_attack',6),('BONEQUILL_attack',14)):
    arm.animation_data.action=A[an]; sc.frame_set(f); M=bm(); R=M.to_3x3().normalized()
    oR=bowrot(an[10:],f); g=M.to_translation()+R@op; bow.matrix_world=Matrix.Translation(g)@(R@oR).to_4x4()
    ax=(R@oR)@Vector((0,0,1))
    for k,d in {'axis':ax.normalized()*-1,'outside':Vector((1,-0.3,0.1)),'back':Vector((0.25,1,0.15)),'front':Vector((0.15,-1,0.25))}.items():
        d.normalize(); cam.location=g+d*2.5; cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler()
        sc.render.filepath=f'{W}/bs_{an[10:]}{f}_{k}.png'; bpy.ops.render.render(write_still=True)
