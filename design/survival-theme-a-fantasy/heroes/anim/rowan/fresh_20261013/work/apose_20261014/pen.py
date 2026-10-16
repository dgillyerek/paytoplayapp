"""Arm vs chest/coat penetration on every attack frame and half-frame (2026-10-14). Arm verts (>12 cm from the shoulder joint at bind)
are tested against the posed torso (part 0) and the coat tails (rigid on Hips): a vertex counts as inside when its nearest torso/coat
face normal points away from it by more than TOL. Output pen.json."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, json, numpy as np, os
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
from scipy.spatial import ConvexHull
ZLO,ZHI,SL=0.98,1.46,0.03
W='/workspace/rowan_fresh/w2'; BL=sys.argv[sys.argv.index('--')+1] if '--' in sys.argv else W+'/anim.blend'
CLIPS=sys.argv[sys.argv.index('--')+2].split(',') if '--' in sys.argv and len(sys.argv)>sys.argv.index('--')+2 else ['attack']
bpy.ops.wm.open_mainfile(filepath=BL)
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; body=bpy.data.objects['ROWAN_body']; coat=bpy.data.objects['ROWAN_coat']; crig=bpy.data.objects['ROWAN_coat_rig']
P_='mixamorig:'; part=np.load(W+'/part.npy'); J=json.load(open(W+'/joints.json')); TOL=0.004; DBG=os.environ.get('DBG')=='1'
P0=np.array([v.co[:] for v in body.data.vertices])
armv={1:None,2:None}
for pid,s in ((1,'Left'),(2,'Right')):
    S=np.array(J[s+'Arm']); idx=np.nonzero(part==pid)[0]; armv[pid]=idx[np.linalg.norm(P0[idx]-S,axis=1)>0.12]
if len(armv[1])==0 or len(armv[2])==0: raise SystemExit('arm part ids wrong')
polys=[tuple(p.vertices) for p in body.data.polygons]; tor=[p for p in polys if all(part[i]==0 for i in p)]
# skin seam: arm verts within 2 face-rings of a torso vertex are the armpit joint itself (they share faces with the torso), not a pass-through
nb=[set() for _ in range(len(P0))]
for p in polys:
    for i in p: nb[i].update(p)
ring=set(i for p in polys if any(part[j]==0 for j in p) for i in p if part[i] in (1,2))
ring2=set(j for i in ring for j in nb[i] if part[j] in (1,2))
SEAMV={pid:int(sum(1 for i in armv[pid] if i in ring2)) for pid in (1,2)}
for pid in (1,2): armv[pid]=np.array([i for i in armv[pid] if i not in ring2])
cpolys=[tuple(p.vertices) for p in coat.data.polygons]
HipsRest=arm.data.bones[P_+'Hips'].matrix_local.copy()
for pb in crig.pose.bones: pb.rotation_mode='QUATERNION'
def evalco(o):
    dg=bpy.context.evaluated_depsgraph_get(); e=o.evaluated_get(dg); m=e.to_mesh(); M=np.array(o.matrix_world)
    c=np.array([v.co[:] for v in m.vertices]); e.to_mesh_clear(); return c@M[:3,:3].T+M[:3,3]
def hull_inside(B,V):
    T=B[part==0]; res=np.zeros(len(V),bool); dep=np.zeros(len(V))
    for z0 in np.arange(ZLO,ZHI,SL):
        m=(T[:,2]>=z0)&(T[:,2]<z0+SL); mv=(V[:,2]>=z0)&(V[:,2]<z0+SL)
        if m.sum()<10 or mv.sum()==0: continue
        eq=ConvexHull(T[m][:,:2]).equations; d=(V[mv][:,:2]@eq[:,:2].T+eq[:,2]).max(1)
        idx=np.nonzero(mv)[0]; res[idx]=d<-TOL; dep[idx]=-d
    return res,dep
arm.animation_data.action=None
for pb in arm.pose.bones: pb.location=(0,0,0); pb.rotation_quaternion=(1,0,0,0)
bpy.context.view_layer.update(); Bb=evalco(body); seam={}
D0={}
def depth_vec(B,V,tb):
    hin,_=hull_inside(B,V); out=np.zeros(len(V))
    for k in np.nonzero(hin)[0]:
        hit=tb.find_nearest(Vector(V[k]),0.10)
        if hit[0] is not None: out[k]=max(0.0,-(Vector(V[k])-hit[0]).dot(hit[1]))
    return out
tbb=BVHTree.FromPolygons([tuple(v) for v in Bb],tor)
for pid in (1,2):
    D0[pid]=depth_vec(Bb,Bb[armv[pid]],tbb); seam[pid]=int((D0[pid]>TOL).sum())
print('SEAM in contact at bind',seam,'seam ring verts excluded',SEAMV)
out={'_arm_verts_in_contact_at_bind':{'L':seam[1],'R':seam[2]},'_skin_seam_ring_verts_excluded':{'L':SEAMV[1],'R':SEAMV[2]},'_tested_arm_verts':{'L':len(armv[1]),'R':len(armv[2])},'_note':'arm verts >12 cm from the shoulder joint and more than 2 face-rings from the arm/torso skin seam; inside = inside the posed torso cross-section hull (3 cm slabs, z 0.98-1.46) AND behind the nearest torso face by more than 4 mm DEEPER than the same vertex sits in the scanned bind pose, or behind a coat-tail face by more than 4 mm; frames sampled every 0.5'}
for clip in CLIPS:
    arm.animation_data.action=bpy.data.actions['ROWAN_'+clip]; rows=[]
    for k in range(61):
        f=k/2; sc.frame_set(int(f),subframe=f-int(f))
        crig.pose.bones['coat_Hips'].matrix=arm.pose.bones[P_+'Hips'].matrix.copy(); bpy.context.view_layer.update()
        B=evalco(body); C=evalco(coat)
        r={'f':f}; tb=BVHTree.FromPolygons([tuple(v) for v in B],tor)
        for pid,s in ((1,'L'),(2,'R')):
            V=B[armv[pid]]; dv=depth_vec(B,V,tb)-D0[pid]; ins=dv>TOL; n=int(ins.sum()); dmax=float(dv.max()) if n else 0.0; where=V[ins]
            if n and DBG:
                bd=np.linalg.norm(P0[armv[pid][ins]]-np.array(J[('Left' if pid==1 else 'Right')+'Arm']),axis=1)
                print('DBG',f,s,n,np.array(where).mean(0).round(3),'bind_dist_to_shoulder pct',np.percentile(bd,[0,50,100]).round(3))
            # coat tails: nearest-face normal test
            nc=0; tcb=BVHTree.FromPolygons([tuple(v) for v in C],cpolys)
            for vv in V[V[:,2]<0.98]:
                hit=tcb.find_nearest(Vector(vv),0.06)
                if hit[0] is not None and (Vector(vv)-hit[0]).dot(hit[1])<-TOL: nc+=1
            r[s+'_inside']=n; r[s+'_depth_cm']=round(dmax*100,2); r[s+'_in_coat']=nc
        rows.append(r); print('PEN',clip,r,flush=True)
    out[clip+'_max']={k:max(x[k] for x in rows) for k in rows[0] if k!='f'}
    out[clip]=rows
json.dump(out,open(W+'/pen.json' if len(CLIPS)==1 and CLIPS[0]=='attack' else W+'/pen_'+'_'.join(CLIPS)+'.json','w'),indent=1)
