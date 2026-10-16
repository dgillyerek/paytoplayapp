"""Rowan bow-hand checks on every half-frame of rest, walk, attack (2026-10-14 v3): finger chain length change, hand wrap angle
round the bow grip axis, hand-in-bow, bow-in-hand, bow-in-body, bow-in-coat (coat tails at their skinned rest on Hips). Writes grip_halfframe.json."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, json, math, numpy as np
sys.path.insert(0,'/workspace/rowan_fresh/w2/bowgrip'); from bowroll import bowrot   # v5: bow rolled per frame (carry 270)
from mathutils import Vector, Matrix
from mathutils.bvhtree import BVHTree
W='/workspace/rowan_fresh/w2'; P_='mixamorig:'
bpy.ops.wm.open_mainfile(filepath=W+'/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; body=bpy.data.objects['ROWAN_body']; coat=bpy.data.objects['ROWAN_coat']; crig=bpy.data.objects['ROWAN_coat_rig']
if crig.animation_data: crig.animation_data.action=None
for pb in crig.pose.bones: pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=(1,0,0,0); pb.location=(0,0,0)
before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=W+'/ROWAN_bow.fbx'); bow=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]
M0=bow.matrix_world.copy(); bow.parent=None; bow.data.transform(M0); bow.matrix_world=Matrix()
for x in set(bpy.data.objects)-before:
    if x is not bow: bpy.data.objects.remove(x)
BOWV=np.array([v.co[:] for v in bow.data.vertices]); BOWP=[tuple(p.vertices) for p in bow.data.polygons]
def dense(V,polys,step=0.004):
    out=[]
    for p in polys:
        for k in range(1,len(p)-1):
            A_,B_,C_=V[p[0]],V[p[k]],V[p[k+1]]; L=max(np.linalg.norm(B_-A_),np.linalg.norm(C_-A_),np.linalg.norm(C_-B_)); n_=max(1,int(L/step))
            for i in range(n_+1):
                for j in range(n_+1-i): out.append(A_+(B_-A_)*i/n_+(C_-A_)*j/n_)
    return np.array(out)
BOWD=dense(BOWV,BOWP)
F=json.load(open(W+'/grip.json')); GL=Vector(F['grip_local']); GR=Matrix(F['grip_rot_local'])
me=body.data; N=len(me.vertices)
HN=['LeftHand','LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2']
gi={body.vertex_groups[P_+n].index for n in HN}; fi={body.vertex_groups[P_+n].index for n in HN[1:]}
hw=np.array([sum(g.weight for g in v.groups if g.group in gi) for v in me.vertices]); fw=np.array([sum(g.weight for g in v.groups if g.group in fi) for v in me.vertices])
handL=np.nonzero(hw>0.999)[0]; HS=set(handL.tolist())
hpolys=[tuple(p.vertices) for p in me.polygons if all(i in HS for i in p.vertices)]
nonhand=[tuple(p.vertices) for p in me.polygons if not any(i in HS for i in p.vertices)]
cpolys=[tuple(p.vertices) for p in coat.data.polygons]
def evalQ(o):
    dg=bpy.context.evaluated_depsgraph_get(); e=o.evaluated_get(dg); m=e.to_mesh(); Q=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',Q); e.to_mesh_clear(); Q=Q.reshape(-1,3)
    return (np.c_[Q,np.ones(len(Q))]@np.array(o.matrix_world).T)[:,:3]
def inside_count(tree,pts,tol=0.001,maxd=0.05):
    c=0
    for p in pts:
        r=tree.find_nearest(Vector(p),maxd)
        if r[0] is not None and (Vector(p)-r[0]).dot(r[1])<-tol: c+=1
    return c
def chainlen(names):
    pts=[arm.matrix_world@arm.pose.bones[P_+names[0]].head]+[arm.matrix_world@arm.pose.bones[P_+n].tail for n in names]
    return sum((pts[i+1]-pts[i]).length for i in range(len(pts)-1))
FCH=['LeftHandFingers1','LeftHandFingers2','LeftHandFingers3']; TCH=['LeftHandThumb1','LeftHandThumb2']
restF=sum(arm.data.bones[P_+n].length for n in FCH); restT=sum(arm.data.bones[P_+n].length for n in TCH)
FE=np.array([e.vertices[:] for e in me.edges]); FE=FE[(fw[FE[:,0]]>0.05)|(fw[FE[:,1]]>0.05)]
P0=np.array([v.co[:] for v in me.vertices]); L0=np.linalg.norm(P0[FE[:,0]]-P0[FE[:,1]],axis=1)
out={}; worst={}
for an in ('rest','walk','attack'):
    arm.animation_data.action=bpy.data.actions['ROWAN_'+an]; per=[]
    for k in range(61):
        f=k*0.5; sc.frame_set(int(f),subframe=f-int(f)); bpy.context.view_layer.update()
        crig.pose.bones['coat_Hips'].matrix=arm.pose.bones[P_+'Hips'].matrix.copy(); bpy.context.view_layer.update()
        HM=arm.matrix_world@arm.pose.bones[P_+'LeftHand'].matrix; R=HM.to_3x3().normalized(); c=HM@GL; Rb=R@bowrot(an,f)
        bow.matrix_world=Matrix.Translation(c)@Rb.to_4x4(); bpy.context.view_layer.update()
        Q=evalQ(body); C=evalQ(coat); Mb=np.array(bow.matrix_world); D=BOWD@Mb[:3,:3].T+Mb[:3,3]; Bw=BOWV@Mb[:3,:3].T+Mb[:3,3]
        th=BVHTree.FromPolygons([tuple(v) for v in Q],hpolys); near=np.linalg.norm(D-np.array(c),axis=1)<0.09
        bih=inside_count(th,D[near])
        tbw=BVHTree.FromPolygons([tuple(v) for v in Bw],BOWP); hib=inside_count(tbw,Q[handL])
        tbody=BVHTree.FromPolygons([tuple(v) for v in Q],nonhand); tc=BVHTree.FromPolygons([tuple(v) for v in C],cpolys)
        bib=inside_count(tbody,D[::3],tol=0.004); bic=inside_count(tc,D[::3],tol=0.004)
        ax=np.array(Rb@Vector((0,0,1))); ax/=np.linalg.norm(ax); cc=np.array(c)
        V=Q[handL]-cc; a=V@ax; Rv=V-np.outer(a,ax); rr=np.linalg.norm(Rv,axis=1); sel=(np.abs(a)<0.05)
        rmin=float(rr[sel].min()); ring=sel&(rr<rmin+0.018)
        e1=np.cross(ax,[0,0,1.0]); e1=e1 if np.linalg.norm(e1)>0.1 else np.cross(ax,[1.0,0,0]); e1/=np.linalg.norm(e1); e2=np.cross(ax,e1)
        ang=np.degrees(np.arctan2(Rv[ring]@e2,Rv[ring]@e1))%360; wrap=len(np.unique((ang//10).astype(int)))*10
        fl=chainlen(FCH)/restF-1; tl=chainlen(TCH)/restT-1
        el=np.linalg.norm(Q[FE[:,0]]-Q[FE[:,1]],axis=1)/np.maximum(L0,1e-6)
        per.append(dict(f=f,bow_in_hand=bih,hand_in_bow=hib,bow_in_body=bib,bow_in_coat=bic,wrap_deg=wrap,min_hand_to_grip_axis_cm=round(rmin*100,2),
                        finger_chain_change=round(abs(fl),5),thumb_chain_change=round(abs(tl),5),finger_edge_stretch_max=round(float(el.max()),3)))
    out[an]=per
    worst[an]={'bow_in_hand':max(p['bow_in_hand'] for p in per),'hand_in_bow':max(p['hand_in_bow'] for p in per),'bow_in_body':max(p['bow_in_body'] for p in per),
               'bow_in_coat':max(p['bow_in_coat'] for p in per),'wrap_deg_min':min(p['wrap_deg'] for p in per),'min_hand_to_grip_axis_cm':min(p['min_hand_to_grip_axis_cm'] for p in per),
               'finger_chain_change_max':max(max(p['finger_chain_change'],p['thumb_chain_change']) for p in per),'finger_edge_stretch_max':max(p['finger_edge_stretch_max'] for p in per)}
    print('GRIPCHK',an,json.dumps(worst[an]),flush=True)
rep={'samples_per_clip':61,'step_frames':0.5,'finger_mesh_length_change_max':F['max_finger_length_change'],'finger_curl_deg':F['finger_curl_deg'],'thumb_deg':F['thumb_deg'],'worst':worst,'per_sample':out}
json.dump(rep,open(W+'/grip_halfframe.json','w'),indent=0); print('GRIPDONE',json.dumps(worst))
