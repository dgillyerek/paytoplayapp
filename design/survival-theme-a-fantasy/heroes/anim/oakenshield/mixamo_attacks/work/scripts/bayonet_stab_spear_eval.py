import bpy,json,math
from mathutils import Matrix,Vector
P='mixamorig:'; a=bpy.data.objects['OAKENSHIELD_rig']; sc=bpy.context.scene; AW=a.matrix_world
H=json.load(open('p_oak_thrust.json'))['held']; HL=Matrix(H['local']); FL=HL@Matrix.Rotation(math.pi,4,'X')
def ang(u,v): return round(math.degrees(u.angle(v)),1)
for f in range(0,121,4):
    sc.frame_set(f); RH=AW@a.pose.bones[P+'RightHand'].matrix; LH=(AW@a.pose.bones[P+'LeftHand'].matrix).translation
    g=(RH@HL).translation; d0=((RH@HL).to_3x3()@Vector((0,-1,0))).normalized(); d1=((RH@FL).to_3x3()@Vector((0,-1,0))).normalized()
    hl=(LH-g); line=hl.normalized()
    perp=(hl-d1*hl.dot(d1)).length
    hips=AW@a.pose.bones[P+'LeftUpLeg'].head-AW@a.pose.bones[P+'RightUpLeg'].head; fac=Vector((0,0,1)).cross(-hips); fac.z=0
    print('E',f,'grip',[round(x,2) for x in g],'LH',[round(x,2) for x in LH],'locked->-Y',ang(d0,Vector((0,-1,0))),'flip->-Y',ang(d1,Vector((0,-1,0))),'flip dir',[round(x,2) for x in d1],'RH->LH line vs flip',ang(line,d1),'LH off flipped shaft m',round(perp,2),'hips yaw',round(math.degrees(math.atan2(-fac.x,-fac.y)),0))
