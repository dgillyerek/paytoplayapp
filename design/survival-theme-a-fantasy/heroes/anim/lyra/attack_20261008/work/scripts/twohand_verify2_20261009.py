import bpy, sys, json, numpy as np
from mathutils import Matrix, Vector, Quaternion
from mathutils.bvhtree import BVHTree
OUT=sys.argv[sys.argv.index('--')+1]
sc=bpy.context.scene; arm=bpy.data.objects['LYRA_tighten_rig']; body=bpy.data.objects['LYRA_tighten_body']; staff=bpy.data.objects['LYRA_staff']
arm.animation_data.action=bpy.data.actions['LYRA_tighten_attack']
P='mixamorig:'
gi={g.index:g.name.replace(P,'') for g in body.vertex_groups}
W={}
dom=[]
for v in body.data.vertices:
    ws={gi[g.group]:g.weight for g in v.groups}; W[v.index]=ws
    dom.append(max(ws,key=ws.get) if ws else '?')
dom=np.array(dom)
# shoulder/armpit blend-zone verts
zone={}
for s in ('Left','Right'):
    zone[s]=set(i for i,ws in W.items() if 0.15<ws.get(s+'Shoulder',0)+ws.get(s+'Arm',0)<0.85)
edges=[tuple(e.vertices) for e in body.data.edges]
zedges={s:np.array([e for e in edges if e[0] in zone[s] and e[1] in zone[s]]) for s in zone}
rest=np.array([v.co[:] for v in body.data.vertices])
rl={s:np.linalg.norm(rest[zedges[s][:,0]]-rest[zedges[s][:,1]],axis=1) for s in zone}
sv=[v.co.copy() for v in staff.data.vertices]
LC=Vector((0.01,0.05,-0.03)); LU=Vector((0.926,-0.327,0.189)).normalized()
def region(r):
    if r in ('Head','Neck'): return 'head/hair'
    if r in ('Spine','Spine1','Spine2','Hips'): return 'torso'
    return r
def st(q,ax):
    p=Vector((q.x,q.y,q.z)).dot(ax)*ax; tw=Quaternion((q.w,p.x,p.y,p.z))
    if tw.magnitude<1e-9: tw=Quaternion()
    tw.normalize(); sw=q@tw.inverted(); a,an=sw.to_axis_angle(); tax,tan=tw.to_axis_angle()
    tdeg=np.degrees(tan)*np.sign(tax.dot(ax)); tdeg=(tdeg+180)%360-180
    return a*an, tdeg
def ang(q): a=np.degrees(q.angle); return a if a<=180 else 360-a
res=[]
for f in range(31):
    sc.frame_set(f); dg=bpy.context.evaluated_depsgraph_get()
    be=body.evaluated_get(dg); me=be.to_mesh()
    V=np.empty(len(me.vertices)*3,np.float32); me.vertices.foreach_get('co',V); V=V.reshape(-1,3).astype(float)
    polys=[tuple(p.vertices) for p in me.polygons]; be.to_mesh_clear()
    bvh=BVHTree.FromPolygons([Vector(v) for v in V],polys)
    Ms=staff.matrix_world
    se=staff.evaluated_get(dg).to_mesh(); sbvh=BVHTree.FromPolygons([Ms@v.co for v in se.vertices],[tuple(p.vertices) for p in se.polygons]); 
    slz=[se.vertices[p.vertices[0]].co.z for p in se.polygons]; staff.evaluated_get(dg).to_mesh_clear()
    # left grip zL from hand
    Mh=arm.pose.bones[P+'LeftHand'].matrix; gp=Mh@LC; zL=(Ms.inverted()@gp).z
    lgrip=f in range(6,23)
    ov=sbvh.overlap(bvh); inter={}; gripinter={}
    for a,b in ov:
        reg=region(dom[polys[b][0]]); z=slz[a]
        isgrip=(abs(z)<0.10 and reg in('RightHand','RightForeArm')) or (abs(z-zL)<0.10 and reg in ('LeftHand',))
        (gripinter if isgrip else inter)[reg]=(gripinter if isgrip else inter).get(reg,0)+1
    mn={}
    for c in sv[::3]:
        p=Ms@c; loc,nrm,fi,dist=bvh.find_nearest(p)
        reg=region(dom[polys[fi][0]])
        if (abs(c.z)<0.10 and reg in ('RightHand','RightForeArm')) or (abs(c.z-zL)<0.10 and reg in ('LeftHand','LeftForeArm')): continue
        inside=False
        if dist<0.12:
            cnt=0
            for dv in (Vector((0,0,1)),Vector((1,0.3,0.1)).normalized(),Vector((-0.2,-1,0.3)).normalized()):
                n=0;o=p.copy()
                for _ in range(40):
                    h=bvh.ray_cast(o,dv)
                    if h[0] is None: break
                    n+=1;o=h[0]+dv*1e-4
                cnt+=n%2
            inside=cnt>=2
        sd=-dist if inside else dist
        if reg not in mn or sd<mn[reg]: mn[reg]=sd
    # left-hand contact: hand verts distance to shaft axis
    hidx=np.where(dom=='LeftHand')[0]; Mi=np.array(Ms.inverted()); HL=(Mi[:3,:3]@V[hidx].T).T+Mi[:3,3]
    near=np.abs(HL[:,2]-zL)<0.05; r=np.hypot(HL[near,0],HL[near,1]); R0=0.027
    contact=dict(min_gap=round(float(r.min()-R0),4),max_pen=round(float(max(0,R0-r.min())),4),n_touch=int(((r-R0)<0.006).sum()),grip_axis_dist=round(float(np.hypot((Ms.inverted()@gp).x,(Ms.inverted()@gp).y)),4))
    # stretch
    strch={}
    for s in zone:
        pl=np.linalg.norm(V[zedges[s][:,0]]-V[zedges[s][:,1]],axis=1)/rl[s]
        strch[s]=[round(float(pl.max()),3),round(float(np.percentile(pl,99)),3),round(float(pl.min()),3),round(float(np.mean(np.abs(pl-1))),4)]
    jt={}
    for s in ('Left','Right'):
        qh=arm.pose.bones[P+s+'Hand'].matrix_basis.to_quaternion(); qf=arm.pose.bones[P+s+'ForeArm'].matrix_basis.to_quaternion(); qa=arm.pose.bones[P+s+'Arm'].matrix_basis.to_quaternion(); qs=arm.pose.bones[P+s+'Shoulder'].matrix_basis.to_quaternion()
        sw,tw=st(qh,Vector((0,1,0))); jt[s+'Wrist_total']=round(ang(qh),1); jt[s+'Wrist_twist']=round(tw,1)
        swa,twa=st(qa,Vector((0,1,0))); jt[s+'Arm_swing']=round(np.degrees(swa.length),1); jt[s+'Arm_twist']=round(twa,1)
        jt[s+'Clavicle']=round(ang(qs),1)
        ya=arm.pose.bones[P+s+'Arm'].vector.normalized(); yf=arm.pose.bones[P+s+'ForeArm'].vector.normalized(); jt[s+'Elbow_bend']=round(np.degrees(ya.angle(yf)),1)
        swf,twf=st(qf,Vector((0,1,0))); jt[s+'Forearm_twist']=round(twf,1)
    feet={s:[round(x,4) for x in list(arm.pose.bones[P+s+'Foot'].tail)+list(arm.pose.bones[P+s+'Foot'].head)] for s in ('Left','Right')}
    r=dict(f=f,intersect=inter,grip_intersect=gripinter,minclear=round(min(mn.values()),3),minclear_by={k:round(v,3) for k,v in sorted(mn.items(),key=lambda kv:kv[1])[:4]},
           stretch=strch,joints=jt,left_contact=contact,zL=round(zL,3),feet=feet,crystal=[round(x,4) for x in (Ms@Vector((0.00277,0.00764,0.525)))],hips=[round(x,4) for x in arm.pose.bones[P+'Hips'].head])
    res.append(r); print(json.dumps(r),flush=True)
json.dump(res,open(OUT,'w'),indent=0)
