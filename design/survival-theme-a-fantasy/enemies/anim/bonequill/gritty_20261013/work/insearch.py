import sys; sys.path.insert(0,'/workspace/bonequill_gritty/work')
import bpy,json,math,itertools,numpy as np
from mathutils import Vector,Matrix,Quaternion
from mathutils.bvhtree import BVHTree
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P='mixamorig:'; body=bpy.data.objects['BONEQUILL_body']
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
cape,crig=d.objects
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P+bn
bpy.ops.import_scene.fbx(filepath=W+'/BONEQUILL_bow.fbx'); bow=[o for o in bpy.context.selected_objects if o.type=='MESH'][0]
LIMB=np.array([bow.data.vertices[v].co[:] for p in bow.data.polygons if p.material_index!=1 for v in p.vertices])[::3]
GL=Vector(json.load(open(W+'/fist.json'))['grip_local']); RF=Matrix(((-1,0,0),(0,-1,0),(0,0,1)))
def grp(names,thr=0.3):
    ids=set(body.vertex_groups[P+n].index for n in names); return np.array([sum(g.weight for g in v.groups if g.group in ids)>thr for v in body.data.vertices])
handfa=grp(('LeftHand','LeftForeArm','LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2'))
fa=grp(('LeftForeArm',),0.5)
polys=[p.vertices[:] for p in body.data.polygons if not handfa[list(p.vertices)].any()]
fapolys=[p.vertices[:] for p in body.data.polygons if fa[list(p.vertices)].all()]
samples=[]
for an,fs in (('rest',[0]),('walk',[0,4,8,12,15,19,23,27])):
    arm.animation_data.action=bpy.data.actions['BONEQUILL_'+an]
    for f in fs:
        sc.frame_set(f); dg=bpy.context.evaluated_depsgraph_get()
        M=arm.matrix_world@arm.pose.bones[P+'LeftHand'].matrix; Mloc=arm.pose.bones[P+'LeftHand'].matrix.copy()
        e=body.evaluated_get(dg); m=e.to_mesh(); V=[body.matrix_world@v.co for v in m.vertices]; e.to_mesh_clear()
        ec=cape.evaluated_get(dg); mc=ec.to_mesh(); CV=[cape.matrix_world@v.co for v in mc.vertices]; CP=[p.vertices[:] for p in mc.polygons]; ec.to_mesh_clear()
        Va=np.array([v[:] for v in V])
        samples.append(dict(an=an,f=f,pos=M.to_translation(),R=M.to_3x3().normalized(),bvh=BVHTree.FromPolygons(V,polys),fbvh=BVHTree.FromPolygons(V,fapolys),
            cb=BVHTree.FromPolygons(CV,CP),body=Va[~handfa],fa=Va[fa],cape=np.array([v[:] for v in CV])))
# NOTE: hand world rotation is replaced by the candidate; the grip point moves with the hand rotation about the wrist
def seg(Q,a,b):
    d=b-a; t=np.clip(((Q-a)@d)/(d@d),0,1); return float(np.linalg.norm(Q-(a+t[:,None]*d),axis=1).min())
out=[]
for TILT,LEAN,ROLL,PHI in itertools.product((25,30,35),(40,47,54,60),(330,345,360),range(-30,35,5)):
    T,L=math.radians(TILT),math.radians(LEAN)
    z=Vector((math.sin(L),math.sin(T),math.cos(T))).normalized(); y=Vector((-math.cos(math.radians(PHI)),math.sin(math.radians(PHI)),0)); y=(y-y.dot(z)*z).normalized(); x=y.cross(z)
    Bw=Matrix((x,y,z)).transposed(); off=RF@Matrix.Rotation(math.radians(ROLL),3,'Z'); Rh=Bw@off.inverted()
    w=dict(limb=0,cloak=0,sfa_min=9,sfa_max=0,spen=0,sbody=9,scape=9)
    for s in samples:
        g=s['pos']+Rh@GL; B=Rh@off
        a=np.array(g+B@Vector((0,0.19,-0.62))); b=np.array(g+B@Vector((0,0.19,0.62)))
        Pb=[g+B@Vector(q) for q in LIMB]
        for p in Pb:
            loc,nrm,_,dd=s['bvh'].find_nearest(p)
            if loc is not None and ((p-loc).dot(nrm)<0 and dd<0.15 or dd<0.01): w['limb']+=1
            loc,nrm,_,dd=s['cb'].find_nearest(p)
            if loc is not None and dd<0.01: w['cloak']+=1
        dv=Vector(b-a); hit=s['fbvh'].ray_cast(Vector(a),dv.normalized(),dv.length)
        if hit[0] is not None: w['spen']+=1
        d=seg(s['fa'],a,b); w['sfa_min']=min(w['sfa_min'],d); w['sfa_max']=max(w['sfa_max'],d)
        w['sbody']=min(w['sbody'],seg(s['body'],a,b)); w['scape']=min(w['scape'],seg(s['cape'],a,b))
    out.append(((TILT,LEAN,ROLL,PHI),{k:(round(v,4) if isinstance(v,float) else v) for k,v in w.items()}))
for c,w in out:
    if w['limb']==0 and w['cloak']==0 and w['sbody']>=0.01 and w['scape']>=0.01: print('OK',c,w)
print('N',len(out)); json.dump([[list(c),w] for c,w in out],open('/tmp/insearch.json','w'))
