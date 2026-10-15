import bpy,json,numpy as np
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
body=bpy.data.objects['BONEQUILL_body']
bpy.ops.import_scene.fbx(filepath=W+'/BONEQUILL_bow.fbx'); bow=[o for o in bpy.context.selected_objects if o.type=='MESH'][0]
bm_=bow.data; Vb=[bow.matrix_world@v.co for v in bm_.vertices]; Vb=[(bow.matrix_world.inverted()@v) for v in Vb]
bvh=BVHTree.FromPolygons([v.co.copy() for v in bm_.vertices],[p.vertices[:] for p in bm_.polygons])
FI=json.load(open(W+'/fist.json')); GL=Vector(FI['grip_local'])
gis_={body.vertex_groups[P_+n].index for n in ('LeftHand','LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2')}; hidx=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in gis_)>0.3]
A=bpy.data.actions; arm.animation_data.action=A['BONEQUILL_attack']; sc.frame_set(10)
M=arm.matrix_world@arm.pose.bones[P_+'LeftHand'].matrix; RLr=M.to_3x3().normalized()
off_p=GL.copy(); off_R=Matrix(((-1,0,0),(0,-1,0),(0,0,1)))
out={}
for an,n in (('BONEQUILL_rest',31),('BONEQUILL_walk',31),('BONEQUILL_attack',31)):
    arm.animation_data.action=A[an]; res=[]
    for f in range(n):
        sc.frame_set(f); dg=bpy.context.evaluated_depsgraph_get(); e=body.evaluated_get(dg); m=e.to_mesh()
        M=arm.matrix_world@arm.pose.bones[P_+'LeftHand'].matrix; R=M.to_3x3().normalized()
        BW=Matrix.Translation(M.to_translation()+R@off_p)@(R@off_R).to_4x4(); Bi=BW.inverted()
        cnt=0; mind=9
        for i in hidx:
            p=Bi@(body.matrix_world@m.vertices[i].co); loc,nrm,_,d=bvh.find_nearest(p)
            if loc is None: continue
            if (p-loc).dot(nrm)<0 and d<0.05:
                cnt+=1
                if an=='BONEQUILL_attack' and f==10: print('IN',tuple(round(c,3) for c in p),round(d,3))
            elif abs(p.z)<0.12: mind=min(mind,d)
        e.to_mesh_clear(); res.append(cnt)
    out[an]=res
print('BOWHAND',json.dumps(out),'hand_verts',len(hidx))
