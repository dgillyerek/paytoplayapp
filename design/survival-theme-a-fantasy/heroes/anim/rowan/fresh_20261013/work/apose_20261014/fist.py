"""Left-hand fist (2026-10-14, Derek: bow went through the hand). The 20-bone rig has no finger bones, so the curl is sculpted into the
LeftHand mesh at bind: every vertex past the knuckle line wraps around a grip axis (across the palm, parallel to the hand width) at
radius = its distance from that axis, angle = arc length / centreline radius. The vertices are 100% LeftHand, so the curled pose holds
in every frame of rest, walk and attack. Writes rig.blend in place (run after rig.py, before coat.py) and fist.json (grip frame in
LeftHand bone space for the bow offset)."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, json, numpy as np
from mathutils import Vector, Matrix
W='/workspace/rowan_fresh/w2'
bpy.ops.wm.open_mainfile(filepath=W+'/rig.blend')
o=bpy.data.objects['ROWAN_body']; me=o.data; arm=bpy.data.objects['ROWAN_rig']
if 'fist_done' in me: raise SystemExit('fist already applied; rerun rig.py first')
P=np.array([v.co[:] for v in me.vertices]); g=o.vertex_groups['mixamorig:LeftHand'].index
H=np.array([i for i,v in enumerate(me.vertices) if any(x.group==g and x.weight>0.999 for x in v.groups)])
J={k:np.array(v) for k,v in json.load(open(W+'/joints.json')).items()}
import os
RG=0.020; CLR=0.004; TK=float(os.environ.get('TK','0.08')); RCX=float(os.environ.get('RCX','0.006'))
w0=J['LeftHand']; X=P[H]-w0
u0=J['LeftHandEnd']-w0; u0/=np.linalg.norm(u0); t0=X@u0
c1=X[(t0>0.04)&(t0<0.08)].mean(0); c2=X[(t0>0.13)&(t0<0.17)].mean(0); u=c2-c1; u/=np.linalg.norm(u)
n=np.array([-1.0,0,0]); n-=n.dot(u)*u; n/=np.linalg.norm(n)      # palm normal: palm faces the thigh (-X) in the bind
w=np.cross(u,n)
t=X@u; h=X@n; q=X@w
cen=X[(t>0.03)&(t<TK)]; hc=cen@n; hpalm=np.percentile(hc,97)     # palm surface (n side)
qc=np.median(cen@w)
Ch=hpalm+RG+CLR; Rc=RG+CLR+RCX
fing=(t>TK)
s=t-TK; rho=Ch-h; th=s/Rc
Xn=X.copy()
base=w0+u*TK+n*Ch     # axis point (world) at q=0
for k in np.nonzero(fing)[0]:
    p=base+w*q[k]+rho[k]*(-n*np.cos(th[k])+u*np.sin(th[k]))
    Xn[k]=p-w0
P2=P.copy(); P2[H]=Xn+w0
# clear the grip: any hand vertex closer than RG+CLR to the grip axis (along the grip, |a|<0.075 m) is pushed out radially
gp0=base+w*qc; Vd=P2[H]-gp0; a_=Vd@w; Rv=Vd-np.outer(a_,w); rr=np.linalg.norm(Rv,axis=1); m=(np.abs(a_)<0.075)&(rr<RG+CLR)
P2[H[m]]=gp0+np.outer(a_[m],w)+Rv[m]/np.maximum(rr[m],1e-6)[:,None]*(RG+CLR); pushed=int(m.sum())
me.vertices.foreach_set('co',P2.ravel()); me['fist_done']=1; me.update()
# grip frame in LeftHand bone space: origin on the axis at the hand's width centre, Z along w
B=arm.data.bones['mixamorig:LeftHand'].matrix_local
gp=Vector(base+w*qc); zb=Vector(w); yb=-(Vector(u)-Vector(u).dot(zb)*zb).normalized(); xb=yb.cross(zb); Rw=Matrix((xb,yb,zb)).transposed()   # bow: Z along the fist axis, string (+Y) toward the wrist
# ---- check: bow vs hand at bind ----
from mathutils.bvhtree import BVHTree
before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=W+'/ROWAN_bow.fbx'); bo=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]
BM=np.array(bo.matrix_world); BV=np.array([v.co[:] for v in bo.data.vertices])@BM[:3,:3].T+BM[:3,3]
BVw=BV@np.array(Rw).T+np.array(gp); bpolys=[tuple(p.vertices) for p in bo.data.polygons]
for x in set(bpy.data.objects)-before: bpy.data.objects.remove(x)
Hset=set(H.tolist()); hpolys=[tuple(p.vertices) for p in me.polygons if all(i in Hset for i in p.vertices)]
th_=BVHTree.FromPolygons([tuple(v) for v in P2],hpolys); tb_=BVHTree.FromPolygons([tuple(v) for v in BVw],bpolys)
def inside(tree,pts,tol=0.001):
    c=0
    for p in pts:
        r=tree.find_nearest(Vector(p),0.05)
        if r[0] is not None and (Vector(p)-r[0]).dot(r[1])<-tol: c+=1
    return c
# denser bow sampling: points on bow faces
def dense(V,polys,step=0.003):
    out=[]
    for p in polys:
        for k in range(1,len(p)-1):
            A,B_,C_=V[p[0]],V[p[k]],V[p[k+1]]; L=max(np.linalg.norm(B_-A),np.linalg.norm(C_-A),np.linalg.norm(C_-B_)); n_=max(1,int(L/step))
            for i in range(n_+1):
                for j in range(n_+1-i): out.append(A+(B_-A)*i/n_+(C_-A)*j/n_)
    return np.array(out)
near=np.linalg.norm(BVw-np.array(gp),axis=1)<0.25; BD=dense(BVw,[p for p in bpolys if near[list(p)].all()])
bow_in_hand=inside(th_,BD)
hand_in_bow=inside(tb_,P2[H])
ax=Vector(w); d_axis=[ (Vector(p)-gp - (Vector(p)-gp).dot(ax)*ax).length for p in P2[H] if abs((Vector(p)-gp).dot(ax))<0.06]
print('CHECK pushed',pushed,'bow_samples',len(BD),'bow_in_hand',bow_in_hand,'hand_in_bow',hand_in_bow,'min_dist_to_grip_axis_cm',round(min(d_axis)*100,2))
np.save(W+'/fist_bow_world.npy',BVw)
inv=B.inverted()
json.dump({'TK':TK,'RG':RG,'CLR':CLR,'axis_point_world':list(gp),'u':list(u),'n':list(n),'w':list(w),'q_centre':float(qc),
           'grip_local':list(inv@gp),'grip_rot_local':[list(r) for r in (B.to_3x3().inverted()@Rw)],'curled_verts':int(fing.sum()),'pushed_off_grip':pushed,'bow_surface_samples':len(BD),'bow_in_hand_bind':bow_in_hand,'hand_in_bow_bind':hand_in_bow,'min_hand_to_grip_axis_cm':round(min(d_axis)*100,2)},open(W+'/fist.json','w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=W+'/rig.blend',compress=True); print('FIST',int(fing.sum()),'hpalm',round(hpalm,4),'qc',round(qc,4))
