import bpy, json, math, os, numpy as np
from mathutils import Matrix, Vector, Quaternion
OUT='/workspace/design/survival-theme-a-fantasy/heroes/anim/rowan/meshy_nocape_nobow_20261008'
SRC='/workspace/design/survival-theme-a-fantasy/heroes/anim/rowan/meshy_nocape_20261008'
E=os.environ
RELEASE=int(E.get('RELEASE','77')); NOCK=int(E.get('NOCK','42')); AIMF=int(E.get('AIMF','72')); GRAB=int(E.get('GRAB','27'))
DO_EXPORT=E.get('EXPORT','0')=='1'
xf=np.load('xf.npz'); s=float(xf['s']); Rx=Matrix(xf['R'].tolist()); tx=Vector(xf['t'].tolist())
SHIFT=Vector((-0.0741,0,0.9517))
bpy.ops.wm.open_mainfile(filepath='/workspace/rig_meshy08/out/stage1.blend')
sc=bpy.context.scene; sc.render.fps=24
arm=bpy.data.objects['target_character']; me=bpy.data.objects['output_unwrapped']
print('arm mw',[round(x,6) for r in arm.matrix_world for x in r])
A_rest=arm.animation_data.action; A_rest.name='ROWAN_meshy_rest'; A_rest.use_fake_user=True
def take(path,name):
    before=set(bpy.data.objects)
    bpy.ops.import_scene.fbx(filepath=path)
    new=[o for o in bpy.data.objects if o not in before]
    a2=[o for o in new if o.type=='ARMATURE'][0]; act=a2.animation_data.action; act.name=name; act.use_fake_user=True
    for o in new: bpy.data.objects.remove(o, do_unlink=True)
    return act
A_walk=take('src/ROWAN_meshy_walk.fbx','ROWAN_meshy_walk')
A_atk=take('src/ROWAN_meshy_attack.fbx','ROWAN_meshy_attack')
bpy.data.orphans_purge(do_local_ids=True, do_linked_ids=True, do_recursive=True)
print('actions',[(a.name,a.frame_range[:],len(a.fcurves)) for a in bpy.data.actions])
P=arm.pose.bones
def setact(a):
    arm.animation_data.action=a; sc.frame_start,sc.frame_end=int(a.frame_range[0]),int(a.frame_range[1])
def fist(side):
    h=P[f'mixamorig:{side}Hand']; m=P.get(f'mixamorig:{side}HandMiddle1')
    return arm.matrix_world@((h.head+m.head)/2 if m else h.head)
def hm(side): return arm.matrix_world@P[f'mixamorig:{side}Hand'].matrix
def yaw(v): return math.degrees(math.atan2(v.x,-v.y))
# ---------- aim fix ----------
setact(A_atk); F=list(range(1,122))
def shotdirs():
    out={}
    for f in F:
        sc.frame_set(f); d=fist('Left')-fist('Right'); out[f]=d
    return out
d0=shotdirs()
hold=[f for f in range(69,RELEASE)]
dh=sum((d0[f].normalized() for f in hold),Vector()); 
YAW0=yaw(Vector((dh.x,dh.y,0))); PITCH0=math.degrees(math.atan2(dh.z,Vector((dh.x,dh.y)).length))
print('shot yaw before fix %.2f pitch %.2f'%(YAW0,PITCH0))
hb=P['mixamorig:Hips']; bone=hb.bone
mats=[]
for f in F: sc.frame_set(f); mats.append(hb.matrix.copy())
piv=Vector((mats[0].translation.x,mats[0].translation.y,0))
ROT=-YAW0
AF=Matrix.Translation(piv)@Matrix.Rotation(math.radians(ROT),4,'Z')@Matrix.Translation(-piv)
dp='pose.bones["mixamorig:Hips"]'
fcs={(fc.data_path,fc.array_index):fc for fc in A_atk.fcurves}
interp=fcs[(dp+'.location',0)].keyframe_points[0].interpolation
print('hips loc keys',len(fcs[(dp+'.location',0)].keyframe_points),'interp',interp, 'rot keys',len(fcs[(dp+'.rotation_quaternion',0)].keyframe_points))
prev=None; vals=[]
for f,M in zip(F,mats):
    B=bone.matrix_local.inverted()@AF@M
    l,q,_=B.decompose()
    if prev is not None and prev.dot(q)<0: q=-q
    prev=q; vals.append((f,l,q))
for path,n,get in ((dp+'.location',3,lambda l,q:l),(dp+'.rotation_quaternion',4,lambda l,q:q)):
    for i in range(n):
        fc=fcs.get((path,i)) or A_atk.fcurves.new(path,index=i,action_group='mixamorig:Hips')
        fc.keyframe_points.clear(); fc.keyframe_points.add(len(vals))
        for k,(f,l,q) in enumerate(vals):
            kp=fc.keyframe_points[k]; kp.co=(f,get(l,q)[i]); kp.interpolation=interp
        fc.update()
d1=shotdirs()
dh1=sum((d1[f].normalized() for f in hold),Vector())
YAW1=yaw(Vector((dh1.x,dh1.y,0))); print('shot yaw after fix %.3f (rot applied %.2f deg about Z, pivot %s)'%(YAW1,ROT,tuple(round(x,4) for x in piv)))
# rigidity check: hand-to-hand distance per frame unchanged
dd=max(abs(d0[f].length-d1[f].length) for f in F); print('max |hand-dist change| %.2e'%dd)
# ---------- bow ----------
with bpy.data.libraries.load('/workspace/rig_20261008/props/rowan_props.blend',link=False) as (src,dst):
    dst.objects=['ROWAN_bow','ROWAN_bow_rig']
for o in dst.objects: sc.collection.objects.link(o)
bow=bpy.data.objects['ROWAN_bow']; brig=bpy.data.objects['ROWAN_bow_rig']
bow.data.transform(Matrix.Scale(s,4))
bpy.context.view_layer.objects.active=brig; brig.select_set(True); bpy.ops.object.mode_set(mode='EDIT')
for eb in brig.data.edit_bones: eb.head*=s; eb.tail*=s
bpy.ops.object.mode_set(mode='OBJECT')
pj=json.load(open('/workspace/rig_20261008/props/rowan_props.json'))
RW=Matrix(pj['rest_world']); BRACE=pj['nock_local'][1]*s
Wb=(Rx@RW.to_3x3()).to_4x4(); Wb.translation=s*(Rx@(RW.translation-SHIFT))+tx
rh_rest=arm.matrix_world@P['mixamorig:RightHand'].bone.matrix_local
offR=rh_rest.inverted()@Wb
print('bind bow grip',tuple(round(x,4) for x in Wb.translation),'RH head rest',tuple(round(x,4) for x in rh_rest.translation))
# LH offset at aim frame
sc.frame_set(AIMF); Lc,Rc=fist('Left'),fist('Right')
dv=(Lc-Rc).normalized(); Yb=-dv; up=Vector((0,0,1)); Zb=(up-up.dot(Yb)*Yb).normalized(); Xb=Yb.cross(Zb)
WL=Matrix((Xb,Yb,Zb)).transposed().to_4x4(); WL.translation=Lc
offL=hm('Left').inverted()@WL
gm=brig.data.bones['bow_grip'].matrix_local
bw={}; Dser={}; lat={}
for f in F:
    sc.frame_set(f); W=hm('Left')@offL; bw[f]=W
    rc=fist('Right'); rel=W.inverted()@rc
    Dser[f]=rel.y-BRACE; lat[f]=math.hypot(rel.x,rel.z)
print('D/lat per frame:', ' '.join('%d:%.2f/%.2f'%(f,Dser[f],lat[f]) for f in range(36,84)))
RAMP=int(E.get('RAMP','3'))
def Darr(f): return max(0.0,Dser[f])
def Dat(f):
    if NOCK<=f<RELEASE:
        k=min(1.0,(f-NOCK+1)/float(RAMP)); k=k*k*(3-2*k); return k*max(0.0,Dser[f])
    if f==RELEASE: return -0.02
    if f==RELEASE+1: return 0.008
    return 0.0
DMAX=max(max(Dat(f),Darr(f) if NOCK<=f<RELEASE else 0) for f in F); print('DMAX %.3f'%DMAX)
brig.animation_data_create()
def bake_bow(name,frames,Wfun,Dfun):
    a=bpy.data.actions.new(name); a.use_fake_user=True; brig.animation_data.action=a
    g=brig.pose.bones['bow_grip']; n=brig.pose.bones['bow_nock']; g.rotation_mode='QUATERNION'; prev=None
    for f in frames:
        g.matrix=Wfun(f)@gm; bpy.context.view_layer.update()
        q=g.rotation_quaternion.copy()
        if prev is not None and prev.dot(q)<0: g.rotation_quaternion=-q
        prev=g.rotation_quaternion.copy()
        n.location=(0,Dfun(f),0)
        g.keyframe_insert('location',frame=f); g.keyframe_insert('rotation_quaternion',frame=f); n.keyframe_insert('location',frame=f)
    for fc in a.fcurves:
        for kp in fc.keyframe_points: kp.interpolation='LINEAR'
    return a
BA=bake_bow('ROWAN_meshy_bow_attack',F,lambda f:bw[f],Dat)
def rh_bake(act,name):
    setact(act); fr=list(range(int(act.frame_range[0]),int(act.frame_range[1])+1)); Ws={}
    for f in fr: sc.frame_set(f); Ws[f]=hm('Right')@offR
    return bake_bow(name,fr,lambda f:Ws[f],lambda f:0.0)
BR=rh_bake(A_rest,'ROWAN_meshy_bow_rest'); BWk=rh_bake(A_walk,'ROWAN_meshy_bow_walk')
setact(A_atk); brig.animation_data.action=BA
# ---------- arrow ----------
bpy.ops.import_scene.fbx(filepath='/workspace/design/survival-theme-a-fantasy/heroes/anim/rowan/attack_20261008/ROWAN_arrow_blue_fletch.fbx')
ar=bpy.data.objects['ROWAN_arrow_blue_fletch']; ar.data.transform(ar.matrix_world); ar.matrix_world=Matrix.Identity(4)
L0=-min(v.co.y for v in ar.data.vertices)
LEN=max(s*L0, DMAX+BRACE+0.12); ky=LEN/L0
ar.data.transform(Matrix.Diagonal((s,ky,s,1)))
print('arrow length %.3f (raw %.3f, ky %.3f, thickness scale s=%.4f)'%(LEN,L0,ky,s))
ar.rotation_mode='QUATERNION'
def nockM(f,D):
    W=bw[f]; M=W.copy(); M.translation=W@Vector((0,BRACE+D,0)); return M
offA=hm_n=None
sc.frame_set(NOCK); offA=hm('Right').inverted()@nockM(NOCK,Darr(NOCK))
V=14.0*s; DIR=Vector((0,-1,0))
sp=nockM(RELEASE-1,Darr(RELEASE-1)); spawn=sp.translation.copy()
Q=DIR.to_track_quat('-Y','Z')
AM={}
for f in F:
    sc.frame_set(f); scale=1.0
    if f<GRAB-1: M=hm('Right')@offA; scale=0.0
    elif f<NOCK:
        M=hm('Right')@offA; scale=min(1.0,(f-(GRAB-1))/2.0)
    elif f<RELEASE: M=nockM(f,Darr(f))
    else:
        M=Q.to_matrix().to_4x4(); M.translation=spawn+DIR*V*(f-RELEASE+1)/24.0
    AM[f]=(M,scale)
ar.animation_data_create(); aa=bpy.data.actions.new('ROWAN_meshy_arrow_attack'); aa.use_fake_user=True; ar.animation_data.action=aa
prev=None
for f in F:
    M,scl=AM[f]; l,q,_=M.decompose()
    if prev is not None and prev.dot(q)<0: q=-q
    prev=q; ar.location=l; ar.rotation_quaternion=q; ar.scale=(max(scl,1e-4),)*3
    for p in ('location','rotation_quaternion','scale'): ar.keyframe_insert(p,frame=f)
for fc in aa.fcurves:
    for kp in fc.keyframe_points: kp.interpolation='CONSTANT' if fc.data_path=='scale' else 'LINEAR'
# post-fix aim of arrow at nock (3D)
na=bw[RELEASE-1].to_3x3()@Vector((0,-1,0))
print('nocked arrow dir at f%d: yaw %.2f pitch %.2f'%(RELEASE-1,yaw(na),math.degrees(math.asin(na.z))))
meta=dict(units='metres (Blender/Unity), Z-up in Blender, FBX axis_forward -Z up Y', fps=24, scale_s=s,
  bow=dict(fbx='ROWAN_meshy_bow_attack.fbx', static_fbx='props/ROWAN_bow_meshy.fbx', frame='origin at grip centre, +Z long axis, local -Y = shoot dir, bow_nock local +Y = string draw (m)',
    brace_m=BRACE, offset_in_RightHand_rest_walk=[list(r) for r in offR], offset_in_LeftHand_attack=[list(r) for r in offL],
    parent_windows=dict(rest='RightHand all frames',walk='RightHand all frames',attack='LeftHand f1-f121 (baked per-frame in ROWAN_meshy_bow_attack.fbx, character space)'),
    draw_per_frame={f:round(Dat(f),4) for f in F}),
  arrow=dict(fbx='props/ROWAN_arrow_blue_fletch_meshy.fbx', anim_fbx='ROWAN_meshy_arrow_attack.fbx', length_m=LEN, grab_frame=GRAB, scale_in=[GRAB-1,GRAB+1], in_right_hand=[GRAB-1,NOCK-1], offset_in_RightHand=[list(r) for r in offA],
    nocked=[NOCK,RELEASE-1], release_frame=RELEASE, spawn_pos=list(spawn), dir=list(DIR), speed_mps=V),
  aim_fix=dict(rot_deg_about_Z=ROT, pivot=list(piv), yaw_before=YAW0, yaw_after=YAW1, pitch=PITCH0))
json.dump(meta,open('/workspace/rig_meshy08/out/meta_draft.json','w'),indent=1,default=float)
bpy.ops.wm.save_as_mainfile(filepath='/workspace/rig_meshy08/out/build.blend')
print('SAVED')
