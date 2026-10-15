import bpy,math,json,sys
from mathutils import Vector,Quaternion,Matrix
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'; PB=arm.pose.bones
FJ=json.load(open(W+'/fingers.json')); tag=sys.argv[-1]
Mh=arm.data.bones[P_+'LeftHand'].matrix_local.to_3x3()
def rot(n,axis_hand,deg):
    pb=PB[P_+n]; pb.rotation_mode='QUATERNION'; R=pb.bone.matrix_local.to_3x3(); ax=(Mh@Vector(axis_hand)).normalized(); q=Quaternion(ax,math.radians(deg))
    pb.rotation_quaternion=(R.inverted()@q.to_matrix()@R).to_quaternion()@pb.rotation_quaternion
if tag!='orig':
    for i,a in enumerate(FJ['finger_curl_deg']): rot('LeftHandFingers%d'%(i+1),(0,0,1),-a)
    for i,a in enumerate(FJ['thumb_deg']): rot('LeftHandThumb%d'%(i+1),FJ['thumb_axis'],a)
bpy.context.view_layer.update()
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'; sc.render.resolution_x=300; sc.render.resolution_y=300
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'; cam.data.ortho_scale=0.25
B=arm.data.bones[P_+'LeftHand']; c=B.matrix_local@Vector((0.02,0.1,-0.07))
if tag!='orig':
    import bmesh
    g=B.matrix_local@Vector(FJ['grip_local']); bpy.ops.mesh.primitive_cylinder_add(radius=0.024,depth=0.25,location=g); cy=bpy.context.active_object
    cy.rotation_euler=(Mh@Vector((0,0,1))).to_track_quat('Z','Y').to_euler()
for k,d in {'palm':Mh@Vector((1,0,0)),'back':Mh@Vector((-1,0,0)),'edge':Mh@Vector((0,0,1))}.items():
    cam.location=c+d*2; cam.rotation_euler=(-d).to_track_quat('-Z','Y').to_euler(); sc.render.filepath=f'{W}/hp_{tag}_{k}.png'; bpy.ops.render.render(write_still=True)
