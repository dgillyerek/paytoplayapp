import bpy, sys, json, numpy as np
from mathutils import Matrix, Vector, Quaternion
from mathutils.bvhtree import BVHTree
OUT=sys.argv[sys.argv.index('--')+1]
sc=bpy.context.scene; arm=bpy.data.objects['LYRA_tighten_rig']; body=bpy.data.objects['LYRA_tighten_body']; staff=bpy.data.objects['LYRA_staff']
arm.animation_data.action=bpy.data.actions['LYRA_tighten_attack']
P='mixamorig:'
sv=[v.co.copy() for v in staff.data.vertices][::2]
gi={g.index:g.name for g in body.vertex_groups}
dom=[]
for v in body.data.vertices:
    best=max(v.groups,key=lambda g:g.weight,default=None); dom.append(gi[best.group].replace(P,'') if best else '?')
OFF=arm.data.bones[P+'RightHand'].matrix_local.inverted()@staff.matrix_world  # at whatever frame; recomputed below
def region(r):
    if r in ('Head','Neck'): return 'head/hair'
    if r in ('Spine','Spine1','Spine2','Hips'): return 'torso'
    return r
def wrist(bn,FX):
    q=arm.pose.bones[bn].matrix_basis.to_quaternion(); y=Vector((0,1,0))
    p=Vector((q.x,q.y,q.z)).dot(y)*y; tw=Quaternion((q.w,p.x,p.y,p.z)); tw.normalize() if tw.magnitude>1e-9 else None
    sw=q@tw.inverted(); ax,an=sw.to_axis_angle(); rv=ax*an
    if an>np.pi: rv=ax*(an-2*np.pi)
    SX=y.cross(FX)
    tax,tan=tw.to_axis_angle(); t=np.degrees(tan*np.sign(tax.dot(y)))
    t=(t+180)%360-180
    return [round(np.degrees(rv.dot(FX)),1),round(np.degrees(rv.dot(SX)),1),round(t,1),round(np.degrees(q.angle) if q.angle<=np.pi else 360-np.degrees(q.angle),1)]
sc.frame_set(0); OFF=arm.pose.bones[P+'RightHand'].matrix.inverted()@staff.matrix_world
fx=OFF.to_3x3().col[2].copy(); fx.y=0; FXR=fx.normalized()
FXL=Vector((0,0,1))
res=[]
for f in range(31):
    sc.frame_set(f); dg=bpy.context.evaluated_depsgraph_get()
    be=body.evaluated_get(dg); me=be.to_mesh()
    verts=[v.co.copy() for v in me.vertices]; polys=[tuple(p.vertices) for p in me.polygons]
    be.to_mesh_clear()
    bvh=BVHTree.FromPolygons(verts,polys)
    Ms=staff.matrix_world
    mn={}; pen={}
    for c in sv:
        p=Ms@c; loc,nrm,fi,dist=bvh.find_nearest(p)
        if loc is None: continue
        reg=region(dom[polys[fi][0]])
        if abs(c.z)<0.10 and reg in ('RightHand','RightForeArm'): continue
        inside=False
        if dist<0.12:
            cnt=0
            for dvec in (Vector((0,0,1)),Vector((1,0.3,0.1)).normalized(),Vector((-0.2,-1,0.3)).normalized()):
                n=0; o=p.copy()
                for _ in range(40):
                    h=bvh.ray_cast(o,dvec)
                    if h[0] is None: break
                    n+=1; o=h[0]+dvec*1e-4
                cnt+=n%2
            inside=cnt>=2
        part='crystal/head' if c.z>0.15 else ('grip' if abs(c.z)<0.10 else ('upper shaft' if c.z>0 else 'lower shaft'))
        sd=-dist if inside else dist
        k=part+'|'+reg
        if k not in mn or sd<mn[k]: mn[k]=sd
        if inside: pen[k]=max(pen.get(k,0),dist)
    r=dict(f=f,wristR=wrist(P+'RightHand',FXR),wristL=wrist(P+'LeftHand',FXL),
           pen={k:round(v,3) for k,v in pen.items()},minclear=round(min(mn.values()),3),
           minclear_by={k:round(v,3) for k,v in sorted(mn.items(),key=lambda kv:kv[1])[:4]},
           crystal=[round(x,4) for x in (Ms@Vector((0.00277,0.00764,0.525)))])
    res.append(r); print(r,flush=True)
json.dump(res,open(OUT,'w'),indent=0)
