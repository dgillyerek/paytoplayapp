import bpy,numpy as np,json,math
from mathutils import Vector
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
cape,crig=d.objects
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P_+bn
arm.animation_data.action=bpy.data.actions['BONEQUILL_attack']
body=bpy.data.objects['BONEQUILL_body']
def segd(Q,a,b):
    ab=b-a; t=np.clip(((Q-a)@ab)/(ab@ab),0,1); return np.linalg.norm(Q-(a+t[:,None]*ab),axis=1)
# ribcage ellipsoid from pen.py idea: torso verts in Spine1/Spine2 frame -> use body verts weighted Spine1/2 as obstacle
gis=[body.vertex_groups[P_+n].index for n in ('Spine','Spine1','Spine2','Neck','Head')]
NQ=len(body.data.vertices)-json.load(open(W+'/quiver.json'))['quiver_verts'] if 'quiver' in str(json.load(open(W+'/quiver.json'))) else len(body.data.vertices)
tor=[v.index for v in body.data.vertices if v.index<NQ and any(g.group in gis and g.weight>0.6 for g in v.groups)]
FI=json.load(open(W+'/fist.json'))
hi=body.vertex_groups[P_+'RightHand'].index; HM=arm.data.bones[P_+'RightHand'].matrix_local.inverted()
fl=np.array([(HM@v.co)[:] for v in body.data.vertices if any(g.group==hi and g.weight>0.9 for g in v.groups)]); yl=fl[:,1].max()
NL=Vector(fl[(fl[:,1]>0.45*yl)&(fl[:,1]<0.8*yl)].mean(0))
TW={}; res=[]; prevx=None; maxroll=0
for k in range(0,61):
    f=k/2; sc.frame_set(int(f),subframe=f-int(f)); dg=bpy.context.evaluated_depsgraph_get()
    e=cape.evaluated_get(dg); m=e.to_mesh(); Q=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',Q); Q=(np.c_[Q.reshape(-1,3),np.ones(len(m.vertices))]@np.array(cape.matrix_world).T)[:,:3]; e.to_mesh_clear()
    eb=body.evaluated_get(dg); mb=eb.to_mesh(); B=np.empty(len(mb.vertices)*3); mb.vertices.foreach_get('co',B); B=B.reshape(-1,3)[tor]; eb.to_mesh_clear()
    pts=[np.array(arm.matrix_world@arm.pose.bones[P_+'Right'+n].head) for n in ('Arm','ForeArm','Hand')]+[np.array(arm.matrix_world@arm.pose.bones[P_+'RightHand'].tail)]
    nc=0; nb=0
    for (a,b,r),i0 in zip(((pts[0],pts[1],0.05),(pts[1],pts[2],0.045),(pts[2],pts[3],0.045)),range(3)):
        hh=segd(Q,a,b)<r; nc+=int(hh.sum())
        if hh.any() and k%2==0: print('SEG',f,i0,Q[hh].mean(0).round(2))
        if i0>0: nb+=int((segd(B,a,b)<r*0.8).sum())
    for nn in ('RightArm','RightForeArm'):
        q=arm.pose.bones[P_+nn].rotation_quaternion; TW.setdefault(nn,[]).append(math.degrees(2*math.atan2(q.y,q.w)))
    x=arm.pose.bones[P_+'RightForeArm'].matrix.to_3x3().col[0].normalized(); y=arm.pose.bones[P_+'RightForeArm'].matrix.to_3x3().col[1].normalized()
    if prevx is not None:
        px=(prevx-prevx.dot(y)*y).normalized(); ang=math.degrees(px.angle(x)); maxroll=max(maxroll,ang); print('ROLL',f,round(ang,1))
    prevx=x.copy()
    fing=arm.matrix_world@arm.pose.bones[P_+'RightHand'].matrix@NL
    res.append((f,nc,nb,tuple(round(c,3) for c in fing)))
for r in res: print('QC',r)
print('QCMAX cape',max(r[1] for r in res),'torso/head',max(r[2] for r in res),'max roll step deg',round(maxroll,1))

for nn,v in TW.items(): d=np.abs(np.diff(v)); d=np.minimum(d,360-d); print('TWIST',nn,'range',round(min(v),1),round(max(v),1),'max half-frame step',round(float(d.max()),1))
mx={}
for k in range(0,61):
    f=k/2; sc.frame_set(int(f),subframe=f-int(f))
    for nn in ('RightArm','RightForeArm'):
        q=arm.pose.bones[P_+nn].rotation_quaternion; mx[nn]=max(mx.get(nn,0),math.degrees(2*math.acos(min(1,abs(q.w)))))
print('SWINGMAX',{k:round(v,1) for k,v in mx.items()})
