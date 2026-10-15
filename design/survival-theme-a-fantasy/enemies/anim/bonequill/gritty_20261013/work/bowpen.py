"""Bow-vs-body clearance on every frame and half-frame of rest, walk and attack: bow vertices inside (or within 1 cm of) the body
surface, and bow edges crossing the cloak. The left hand/forearm (the fist around the grip) is excluded from the body test."""
import bpy,json,math,hashlib,numpy as np
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
body=bpy.data.objects['BONEQUILL_body']
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
cape,crig=d.objects
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P_+bn
bpy.ops.import_scene.fbx(filepath=W+'/BONEQUILL_bow.fbx'); bow=[o for o in bpy.context.selected_objects if o.type=='MESH'][0]
BV=np.array([v.co[:] for v in bow.data.vertices]); BE=np.array([e.vertices[:] for e in bow.data.edges])
FI=json.load(open(W+'/fist.json')); GL=Vector(FI['grip_local'])
ex=set(body.vertex_groups[P_+n].index for n in ('LeftHand','LeftForeArm'))
excl=np.array([any(g.group in ex and g.weight>0.3 for g in v.groups) for v in body.data.vertices])
polys=[p.vertices[:] for p in body.data.polygons if not excl[list(p.vertices)].any()]
A=bpy.data.actions; arm.animation_data.action=A['BONEQUILL_attack']; sc.frame_set(14)
M=arm.matrix_world@arm.pose.bones[P_+'LeftHand'].matrix; R=M.to_3x3().normalized(); op=R.inverted()@((M@GL)-M.to_translation()); oR=R.inverted()
out={}; worst={}
for an in ('rest','walk','attack'):
    arm.animation_data.action=A['BONEQUILL_'+an]; per=[]
    for k in range(61):
        f=k/2; sc.frame_set(int(f),subframe=f-int(f)); dg=bpy.context.evaluated_depsgraph_get()
        M=arm.matrix_world@arm.pose.bones[P_+'LeftHand'].matrix; R=M.to_3x3().normalized()
        BW=Matrix.Translation(M.to_translation()+R@op)@(R@oR).to_4x4()
        Pb=(np.c_[BV,np.ones(len(BV))]@np.array(BW).T)[:,:3]
        e=body.evaluated_get(dg); m=e.to_mesh(); V=[body.matrix_world@v.co for v in m.vertices]; e.to_mesh_clear()
        bvh=BVHTree.FromPolygons(V,polys)
        nb=0; md=9
        for p in Pb:
            loc,nrm,_,dd=bvh.find_nearest(Vector(p))
            if loc is None: continue
            inside=(Vector(p)-loc).dot(nrm)<0
            if inside and dd<0.15 or dd<0.01: nb+=1
            md=min(md,-dd if (inside and dd<0.15) else dd)
        ec=cape.evaluated_get(dg); mc=ec.to_mesh(); CV=[cape.matrix_world@v.co for v in mc.vertices]; CP=[p.vertices[:] for p in mc.polygons]; ec.to_mesh_clear()
        cb=BVHTree.FromPolygons(CV,CP); nc=0
        for a,b in BE[::1]:
            pa=Vector(Pb[a]); pb_=Vector(Pb[b]); d=pb_-pa; L=d.length
            if L<1e-6: continue
            hit=cb.ray_cast(pa,d/L,L)
            if hit[0] is not None: nc+=1
        per.append([f,nb,nc,round(md,4)])
    out[an]=per; worst[an]={'body_verts_max':max(r[1] for r in per),'cloak_edges_max':max(r[2] for r in per),'min_clearance_m':min(r[3] for r in per)}
md5=lambda p: hashlib.md5(open(p,'rb').read()).hexdigest()
rep={'samples_per_clip':61,'step_frames':0.5,'worst':worst,'per_sample':out,
     'fbx_md5':{k:md5(W+'/export/'+f) for k,f in (('rest','BONEQUILL_blenderig.fbx'),('walk','BONEQUILL_blenderig_walk.fbx'),('attack','BONEQUILL_blenderig_attack.fbx'))}}
json.dump(rep,open(W+'/bow_clearance.json','w'),indent=0); print('BOWPEN',json.dumps(worst))
