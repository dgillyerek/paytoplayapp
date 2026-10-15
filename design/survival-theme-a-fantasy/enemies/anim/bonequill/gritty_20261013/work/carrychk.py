import sys; sys.path.insert(0,'/workspace/bonequill_gritty/work')
import bpy,json,math,numpy as np
from mathutils import Vector,Matrix
from mathutils.bvhtree import BVHTree
from bowroll import bowrot
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P='mixamorig:'; body=bpy.data.objects['BONEQUILL_body']
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
cape,crig=d.objects
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P+bn
bpy.ops.import_scene.fbx(filepath=W+'/BONEQUILL_bow.fbx'); bow=[o for o in bpy.context.selected_objects if o.type=='MESH'][0]
LIMB=np.array([bow.data.vertices[v].co[:] for p in bow.data.polygons if p.material_index!=1 for v in p.vertices])[::2]
bowbvh=BVHTree.FromPolygons([v.co.copy() for v in bow.data.vertices],[p.vertices[:] for p in bow.data.polygons])
GL=Vector(json.load(open(W+'/fist.json'))['grip_local'])
ex=set(body.vertex_groups[P+n].index for n in ('LeftHand','LeftForeArm','LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2'))
excl=np.array([any(g.group in ex and g.weight>0.3 for g in v.groups) for v in body.data.vertices])
fa=body.vertex_groups[P+'LeftForeArm'].index
isfa=[v.index for v in body.data.vertices if any(g.group==fa and g.weight>0.5 for g in v.groups)]
polys=[p.vertices[:] for p in body.data.polygons if not excl[list(p.vertices)].any()]
clips=sys.argv[sys.argv.index('--')+1].split(',') if '--' in sys.argv else ['rest','walk']
rep={}
for an in clips:
    arm.animation_data.action=bpy.data.actions['BONEQUILL_'+an]; w=dict(limb=0,cloak=0,fa_in_bow=0,str_body=9,str_cape=9,tilt=0,out=1,wrist=0,bow_fa=0)
    for k in range(0,61,1 if an=='attack' else 2):
        f=k/2; sc.frame_set(int(f),subframe=f-int(f)); dg=bpy.context.evaluated_depsgraph_get()
        M=arm.matrix_world@arm.pose.bones[P+'LeftHand'].matrix; R=M.to_3x3().normalized(); B=R@bowrot(an,f); g=M.to_translation()+R@GL
        BW=Matrix.Translation(g)@B.to_4x4(); Bi=BW.inverted()
        e=body.evaluated_get(dg); m=e.to_mesh(); V=[body.matrix_world@v.co for v in m.vertices]; e.to_mesh_clear()
        ec=cape.evaluated_get(dg); mc=ec.to_mesh(); CV=[cape.matrix_world@v.co for v in mc.vertices]; CP=[p.vertices[:] for p in mc.polygons]; ec.to_mesh_clear()
        bvh=BVHTree.FromPolygons(V,polys); cb=BVHTree.FromPolygons(CV,CP)
        Pb=(np.c_[LIMB,np.ones(len(LIMB))]@np.array(BW).T)[:,:3]; nl=0; nc=0
        for p in Pb:
            p=Vector(p); loc,nrm,_,dd=bvh.find_nearest(p)
            if loc is not None and ((p-loc).dot(nrm)<0 and dd<0.15 or dd<0.01): nl+=1
            loc,nrm,_,dd=cb.find_nearest(p)
            if loc is not None and dd<0.01: nc+=1
        nf=0
        for i in isfa:
            q=Bi@V[i]; loc,nrm,_,dd=bowbvh.find_nearest(q)
            if loc is not None and (q-loc).dot(nrm)<0 and dd<0.05: nf+=1
        Va=np.array([v[:] for v in V]); Ca=np.array([v[:] for v in CV])
        draw=an=='attack' and 10<=f<=17
        a=np.array(g+B@Vector((0,0.19,-0.62))); b=np.array(g+B@Vector((0,0.19,0.62)))
        def seg(Q):
            d=b-a; t=np.clip(((Q-a)@d)/(d@d),0,1); return float(np.linalg.norm(Q-(a+t[:,None]*d),axis=1).min())
        sb=seg(Va) if not draw else 9; scp=seg(Ca)
        ax=B@Vector((0,0,1)); tilt=math.degrees(math.acos(abs(ax.z))); out=(B@Vector((0,1,0))).x
        fy=(arm.matrix_world.to_3x3()@arm.pose.bones[P+'LeftForeArm'].matrix.to_3x3()).col[1].normalized(); hy=R.col[1].normalized()
        wr=math.degrees(fy.angle(hy))
        mid=(arm.matrix_world@arm.pose.bones[P+'LeftForeArm'].head+arm.matrix_world@arm.pose.bones[P+'LeftForeArm'].tail)/2
        dv=mid-g; bfa=(dv-ax*dv.dot(ax)).length
        w['limb']=max(w['limb'],nl); w['cloak']=max(w['cloak'],nc); w['fa_in_bow']=max(w['fa_in_bow'],nf); w['str_body']=min(w['str_body'],round(sb,4)); w['str_cape']=min(w['str_cape'],round(scp,4))
        if an!='attack': w['tilt']=max(w['tilt'],round(tilt,1)); w['out']=min(w['out'],round(out,2)); w['wrist']=max(w['wrist'],round(wr,1)); w['bow_fa']=max(w['bow_fa'],round(bfa,3))
        if an=='attack' and (nl or nc or nf or sb<0.02 or scp<0.02): w.setdefault('bad',[]).append((f,nl,nc,nf,round(sb,3)))
    rep[an]=w
print('CARRY',json.dumps(rep))
