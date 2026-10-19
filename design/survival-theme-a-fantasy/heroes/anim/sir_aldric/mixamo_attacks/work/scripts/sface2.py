import bpy,sys
from mathutils import Vector
bpy.ops.wm.read_factory_settings(use_empty=True); bpy.ops.import_scene.fbx(filepath=sys.argv[sys.argv.index('--')+1])
arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; sc=bpy.context.scene; W=arm.matrix_world; pre='mixamorig:'
for f in range(1,int(arm.animation_data.action.frame_range[1])+1,6):
    sc.frame_set(f)
    l=W@arm.pose.bones[pre+'LeftArm'].head; r=W@arm.pose.bones[pre+'RightArm'].head; h=W@arm.pose.bones[pre+'Hips'].head
    fw=Vector((0,0,1)).cross(r-l).normalized(); rh=W@arm.pose.bones[pre+'RightHand'].head; lf=W@arm.pose.bones[pre+'LeftFoot'].head; rf=W@arm.pose.bones[pre+'RightFoot'].head
    s=1/(r-l).length*0.3
    print('SF',f,'chest',[round(x,2) for x in fw],'RHrel',[round(x,2) for x in (rh-h)*s],'LFrel',[round(x,2) for x in (lf-h)*s],'RFrel',[round(x,2) for x in (rf-h)*s])
