import sys; sys.path.insert(0,'/workspace/bonequill_gritty/work'); from bowroll import bow_matrix
import bpy,json,math
from mathutils import Vector,Matrix,Quaternion
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
def imp(fp):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=fp); o=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]
    for x in set(bpy.data.objects)-before:
        if x is not o: bpy.data.objects.remove(x)
    o.parent=None; o.rotation_mode='QUATERNION'; return o
bow=imp(W+'/BONEQUILL_bow.fbx'); arrow=imp(W+'/BONEQUILL_bonearrow_purple_fletch.fbx')
# hide the braced string faces of the bow; draw the string as a curve tip -> nock -> tip
si=[i for i,m in enumerate(bow.data.materials) if 'string' in m.name][0]
import bmesh; bm_=bmesh.new(); bm_.from_mesh(bow.data); bmesh.ops.delete(bm_,geom=[f for f in bm_.faces if f.material_index==si],context='FACES'); bm_.to_mesh(bow.data)
cu=bpy.data.curves.new('str','CURVE'); cu.dimensions='3D'; cu.bevel_depth=0.003; sp=cu.splines.new('POLY'); sp.points.add(2)
so=bpy.data.objects.new('str',cu); sc.collection.objects.link(so); so.data.materials.append(bow.data.materials[si])
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
crig=bpy.data.objects['BONEQUILL_cape_rig']
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P_+bn
S=json.load(open(W+'/sheet.json')); H=S['arrow_held_unity']; FI=json.load(open(W+'/fist.json')); GL=Vector(FI['grip_local'])
C=Matrix(((-1,0,0),(0,0,1),(0,-1,0)))
arm.animation_data.action=bpy.data.actions['BONEQUILL_attack']
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='MATERIAL'; sc.render.resolution_x=300; sc.render.resolution_y=420
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'
# target marker line straight ahead (-y)
me=bpy.data.meshes.new('aim'); me.from_pydata([(0,-0.3,0),(0,-3,0)],[(0,1)],[]); 
def place(f):
    sc.frame_set(f); hand=arm.matrix_world@arm.pose.bones[P_+'LeftHand'].matrix; B=bow_matrix(hand,GL,'attack',f); bow.matrix_world=B
    h=H[f]; p=Vector((-h[0],-h[2],h[1])); q=Quaternion((h[6],h[3],h[4],h[5])); R=C.inverted()@q.to_matrix()@C
    arrow.matrix_world=Matrix.Translation(p)@R.to_4x4(); arrow.hide_render=not h[8]
    top=B@Vector((0,0.19,0.62)); bot=B@Vector((0,0.19,-0.62)); mid=p if 10<=f<17 else (top+bot)/2
    for i,v in enumerate((top,mid,bot)): sp.points[i].co=(v.x,v.y,v.z,1)
    return B
for f in (0,4,8,10,12,14,16,17,20,30):
    place(f); cam.data.ortho_scale=2.3; cam.location=(0,-4,1.05); cam.rotation_euler=(math.radians(90),0,0)
    sc.render.filepath=f'{W}/as_front_{f:02d}.png'; bpy.ops.render.render(write_still=True)
sc.render.resolution_x=420; sc.render.resolution_y=420
for f in (16,):
    B=place(f); cam.data.ortho_scale=1.6; cam.location=(0,-0.3,4); cam.rotation_euler=(0,0,0)
    sc.render.filepath=f'{W}/as_top_{f:02d}.png'; bpy.ops.render.render(write_still=True)
    cam.data.ortho_scale=0.7; cam.location=(0,-0.45,4); sc.render.filepath=f'{W}/as_topclose_{f:02d}.png'; bpy.ops.render.render(write_still=True)
