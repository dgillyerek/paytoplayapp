# crease/stretch around a joint: max change of the dihedral angle across each edge (deg) and edge growth (cm), over an action
import bpy, sys, json, numpy as np
a=sys.argv[sys.argv.index('--')+1:]; OUT=a[0]; ACTS=a[1].split(','); BONE=a[2]; RAD=float(a[3])
o=bpy.data.objects['OAKENSHIELD_body']; me=o.data; arm=bpy.data.objects['OAKENSHIELD_rig']; sc=bpy.context.scene
N=len(me.vertices); F=[list(p.vertices) for p in me.polygons]
tri=np.array([f[:3] for f in F])
E={}
for fi,f in enumerate(F):
    for k in range(len(f)):
        e=tuple(sorted((f[k],f[(k+1)%len(f)]))); E.setdefault(e,[]).append(fi)
ed=np.array([e for e,fs in E.items() if len(fs)==2]); ef=np.array([fs for e,fs in E.items() if len(fs)==2])
def coords():
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); m=ob.to_mesh(); c=np.empty(N*3); m.vertices.foreach_get('co',c); ob.to_mesh_clear(); return c.reshape(-1,3)
def fn(c):
    n=np.cross(c[tri[:,1]]-c[tri[:,0]],c[tri[:,2]]-c[tri[:,0]]); return n/np.maximum(np.linalg.norm(n,axis=1,keepdims=True),1e-12)
arm.data.pose_position='REST'; bpy.context.view_layer.update(); c0=coords(); arm.data.pose_position='POSE'
j=np.array(arm.data.bones['mixamorig:'+BONE].head_local)
mid=(c0[ed[:,0]]+c0[ed[:,1]])/2; sel=np.linalg.norm(mid-j,axis=1)<RAD
ar=0.5*np.linalg.norm(np.cross(c0[tri[:,1]]-c0[tri[:,0]],c0[tri[:,2]]-c0[tri[:,0]]),axis=1)
sel&=(ar[ef[:,0]]>4e-6)&(ar[ef[:,1]]>4e-6)   # skip sliver faces (< 4 mm2) whose normals flip on noise
ed=ed[sel]; ef=ef[sel]
n0=fn(c0); d0=np.degrees(np.arccos(np.clip((n0[ef[:,0]]*n0[ef[:,1]]).sum(1),-1,1))); L0=np.linalg.norm(c0[ed[:,0]]-c0[ed[:,1]],axis=1)
rep={}
for act in ACTS:
    arm.animation_data.action=bpy.data.actions[act]; f0,f1=map(int,bpy.data.actions[act].frame_range)
    dd=np.zeros(len(ed)); g=np.zeros(len(ed))
    for f in range(f0,f1+1):
        sc.frame_set(f); c=coords(); n=fn(c)
        d=np.degrees(np.arccos(np.clip((n[ef[:,0]]*n[ef[:,1]]).sum(1),-1,1)))
        dd=np.maximum(dd,np.abs(d-d0)); g=np.maximum(g,np.linalg.norm(c[ed[:,0]]-c[ed[:,1]],axis=1)-L0)
    rep[act]={'edges_in_region':int(len(ed)),'crease_gt_30deg':int((dd>30).sum()),'crease_gt_60deg':int((dd>60).sum()),'max_crease_deg':round(float(dd.max()),1),
              'p99_crease_deg':round(float(np.percentile(dd,99)),1),'grow_gt_2cm':int((g>0.02).sum()),'max_grow_cm':round(float(g.max()*100),2)}
    print('CREASE',act,json.dumps(rep[act]),flush=True)
json.dump(rep,open(OUT,'w'),indent=1)
