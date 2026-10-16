import bpy,json,math,itertools,numpy as np
from mathutils import Vector,Matrix,Quaternion
from mathutils.bvhtree import BVHTree
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'; PB=arm.pose.bones
body=bpy.data.objects['BONEQUILL_body']
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
cape,crig=d.objects
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P_+bn
bpy.ops.import_scene.fbx(filepath=W+'/BONEQUILL_bow.fbx'); bow=[o for o in bpy.context.selected_objects if o.type=='MESH'][0]
BV=np.array([v.co[:] for v in bow.data.vertices])[::3]; 
FI=json.load(open(W+'/fist.json')); GL=Vector(FI['grip_local'])
ex=set(body.vertex_groups[P_+n].index for n in ('LeftHand','LeftForeArm','LeftArm'))
excl=np.array([any(g.group in ex and g.weight>0.3 for g in v.groups) for v in body.data.vertices])
polys=[p.vertices[:] for p in body.data.polygons if not excl[list(p.vertices)].any()]
A=bpy.data.actions; arm.animation_data.action=A['BONEQUILL_attack']; sc.frame_set(14)
M=arm.matrix_world@arm.pose.bones[P_+'LeftHand'].matrix; R=M.to_3x3().normalized(); op=R.inverted()@((M@GL)-M.to_translation()); oR=R.inverted()
RL=PB[P_+'LeftHand'].matrix.to_3x3().to_quaternion()
def upd(): bpy.context.view_layer.update()
def rot(n,axis,deg):
    pb=PB[P_+n]; Rr=pb.bone.matrix_local.to_3x3(); q=Quaternion(Vector(axis),math.radians(deg))
    pb.rotation_quaternion=(Rr.inverted()@q.to_matrix()@Rr).to_quaternion()@pb.rotation_quaternion
def setrot(n,q):
    upd(); pb=PB[P_+n]; Mm=pb.matrix.copy(); pb.matrix=Matrix.Translation(Mm.to_translation())@q.to_matrix().to_4x4(); upd()
envs=[]
for an,frs in (('rest',[0]),('walk',list(range(0,30,3)))):
    arm.animation_data.action=A['BONEQUILL_'+an]
    for f in frs:
        sc.frame_set(f); dg=bpy.context.evaluated_depsgraph_get()
        e=body.evaluated_get(dg); m=e.to_mesh(); V=[body.matrix_world@v.co for v in m.vertices]; e.to_mesh_clear()
        ec=cape.evaluated_get(dg); mc=ec.to_mesh(); CV=[cape.matrix_world@v.co for v in mc.vertices]; CP=[p.vertices[:] for p in mc.polygons]; ec.to_mesh_clear()
        envs.append((an,f,BVHTree.FromPolygons(V,polys),BVHTree.FromPolygons(CV,CP)))
res=[]
for abd,elb,a,b in itertools.product((14,18,24),(30,45,60),(45,55,65),(-8,0,8)):
    tot=0; mind=9
    for an,f,bb,cb in envs:
        arm.animation_data.action=A['BONEQUILL_'+an]; sc.frame_set(f)
        rot('LeftArm',(0,1,0),abd-6); rot('LeftForeArm',(1,0,0),-elb); upd()
        q=(Quaternion((0,1,0),math.radians(a))@Quaternion((1,0,0),math.radians(b))@RL); setrot('LeftHand',q)
        M=arm.matrix_world@PB[P_+'LeftHand'].matrix; Rm=M.to_3x3().normalized(); BW=Matrix.Translation(M.to_translation()+Rm@op)@(Rm@oR).to_4x4()
        Pb=[BW@Vector(p) for p in BV]
        for i,p in enumerate(Pb):
            loc,nrm,_,dd=bb.find_nearest(p)
            if loc is not None:
                ins=(p-loc).dot(nrm)<0
                if (ins and dd<0.15) or dd<0.01: tot+=1
                if not ins: mind=min(mind,dd)
            if i: 
                dv=p-Pb[i-1]; L=dv.length
                if L>1e-6 and cb.ray_cast(Pb[i-1],dv/L,L)[0] is not None: tot+=1
    top=BW@Vector((0,0,0.7)); res.append((tot,-mind,abd,elb,a,b,tuple(round(c,2) for c in top)))
res=[r for r in res if r[0]==0]; res.sort(key=lambda r:(r[6][2]+r[1]*3))
for r in res[:12]: print('RES',r)
