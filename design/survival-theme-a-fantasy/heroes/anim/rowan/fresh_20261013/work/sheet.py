import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, math, json, numpy as np
from mathutils import Vector, Matrix
W='/workspace/rowan_fresh/work'
bpy.ops.wm.open_mainfile(filepath=W+'/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; body=bpy.data.objects['ROWAN_body']; P_='mixamorig:'
def imp(fp):
    before=set(bpy.data.objects); bpy.ops.import_scene.fbx(filepath=fp); o=[x for x in bpy.data.objects if x not in before and x.type=='MESH'][0]
    for x in set(bpy.data.objects)-before:
        if x is not o: bpy.data.objects.remove(x)
    M=o.matrix_world.copy(); o.parent=None; o.data.transform(M); o.rotation_mode='QUATERNION'; o.matrix_world=Matrix(); import numpy as _n; _P=_n.array([v.co[:] for v in o.data.vertices]); print('PROP',fp,_P.min(0).round(3),_P.max(0).round(3)); return o
bow=imp(W+'/ROWAN_bow.fbx'); arrow=imp(W+'/ROWAN_arrow.fbx'); proj=imp(W+'/ROWAN_arrow.fbx'); quiv=imp(W+'/ROWAN_quiver.fbx')
A={k:bpy.data.actions['ROWAN_'+k] for k in ('rest','walk','attack')}
rep={}
# face skew
me=body.data; N=len(me.vertices); P0=np.array([v.co[:] for v in me.vertices]); hi=body.vertex_groups[P_+'Head'].index
hw=np.zeros(N)
for v in me.vertices:
    for g in v.groups:
        if g.group==hi: hw[v.index]=g.weight
face=np.nonzero(hw>0.999)[0]; band=np.nonzero((hw>0.01)&(hw<0.999))[0]; Hrest=arm.data.bones[P_+'Head'].matrix_local
def skew():
    dg=bpy.context.evaluated_depsgraph_get(); e=body.evaluated_get(dg); m=e.to_mesh(); Q=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',Q); e.to_mesh_clear(); Q=Q.reshape(-1,3)
    M=np.array(arm.pose.bones[P_+'Head'].matrix@Hrest.inverted()); R=(np.c_[P0,np.ones(N)]@M.T)[:,:3]; d=np.linalg.norm(Q-R,axis=1)
    return float(d[face].max()),float(d[band].max()) if len(band) else 0
C=Matrix(((-1,0,0),(0,0,1),(0,-1,0)))
def U(v): return [round(-v.x,5),round(v.z,5),round(-v.y,5)]
def UQ(Rb):
    q=(C@Rb@C.inverted()).to_quaternion().normalized()
    if q.w<0: q=-q
    return [round(q.x,5),round(q.y,5),round(q.z,5),round(q.w,5)]
REF,REL,V0=10,14,5; SPD=14.0; BOWV0,BOWV1=4,24
arm.animation_data.action=A['attack']; sc.frame_set(REF)
def bm(n): return arm.matrix_world@arm.pose.bones[P_+n].matrix
# bow: grip 6 cm along LeftHand (palm), world-upright at full draw
ML=bm('LeftHand'); RL=ML.to_3x3().normalized(); grip=ML.to_translation()+RL@Vector((0,0.06,0))
bow_off=(RL.inverted()@(grip-ML.to_translation()), RL.inverted())   # pos, rot (bow rot = identity at REF)
# arrow: nock 4 cm along RightHand, tip toward the bow grip
MR=bm('RightHand'); RR=MR.to_3x3().normalized(); nock=MR.to_translation()+RR@Vector((0,0.04,0))
fw=(grip-nock).normalized(); yax=-fw; xax=Vector((1,0,0)); xax=(xax-xax.dot(yax)*yax).normalized(); zax=xax.cross(yax); Rar=Matrix((xax,yax,zax)).transposed()
ar_off=(RR.inverted()@(nock-MR.to_translation()), RR.inverted()@Rar)
import json as _j
QC=Vector(_j.load(open(W+'/quiver_place.json'))['center']); QR=Matrix.Rotation(math.radians(_j.load(open(W+'/quiver_place.json'))['tilt_deg']),3,'Y')
arm.animation_data.action=None
for pb in arm.pose.bones: pb.rotation_quaternion=(1,0,0,0); pb.location=(0,0,0)
bpy.context.view_layer.update()
MS=bm('Spine2'); RS=MS.to_3x3().normalized(); q_off=(RS.inverted()@(QC-MS.to_translation()), RS.inverted()@QR)
arm.animation_data.action=A['attack']; sc.frame_set(REF)
def place(o,M,off): R=M.to_3x3().normalized(); return M.to_translation()+R@off[0], R@off[1]
held=[]; bowtr=[]; projtr=[]; arpose={}; quivtr=[]
for f in range(31):
    sc.frame_set(f); p,R=place(arrow,bm('RightHand'),ar_off); vis=V0<=f<REL; held.append(U(p)+UQ(R)+[1.0,vis]); arpose[f]=(p.copy(),R.copy())
    bp,bR=place(bow,bm('LeftHand'),bow_off); bowtr.append(U(bp)+UQ(bR)+[1.0,BOWV0<=f<=BOWV1]); qp,qR=place(quiv,bm('Spine2'),q_off); quivtr.append(U(qp)+UQ(qR)+[1.0,True])
sp,sR=arpose[REL-1]
for f in range(31):
    p=sp+fw*max(0,f-REL+1)*SPD/30.0; projtr.append(U(p)+UQ(sR)+[1.0,f>=REL])
arm.animation_data.action=A['rest']; sc.frame_set(0); qp,qR=place(quiv,bm('Spine2'),q_off)
rep['quiver_idle_rest_world_unity']=U(qp)+UQ(qR)
rep['calibration_unity']={n:U(arm.matrix_world@arm.data.bones[P_+n].head_local) for n in ('Hips','Head','LeftHand','RightHand')}
rep.update(arrow_held_unity=held,arrow_proj_unity=projtr,bow_attack_unity=bowtr,quiver_attack_unity=quivtr,arrow_dir_unity=U(fw))
# render
w_=bpy.data.worlds.new('bg') if not sc.world else sc.world; sc.world=w_; w_.color=(0.45,0.45,0.45)
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'
VIEWS={'full':((0,-5,0.95),(math.pi/2,0,0)),'side':((5,0,0.95),(math.pi/2,0,math.pi/2)),'back':((0,5,0.95),(math.pi/2,0,math.pi))}
def shot(path,view):
    if view=='face':
        sc.render.resolution_x=360; sc.render.resolution_y=360; cam.data.ortho_scale=0.45; h=arm.matrix_world@arm.pose.bones[P_+'Head'].head
        cam.location=(h.x,-5,h.z+0.12); cam.rotation_euler=(math.pi/2,0,0)
    else:
        sc.render.resolution_x=360; sc.render.resolution_y=640; cam.data.ortho_scale=2.1; cam.location,cam.rotation_euler=VIEWS[view]
    sc.render.filepath=path; bpy.ops.render.render(write_still=True)
FR={'rest':[0],'walk':[0,4,8,12,15,19,23,27],'attack':[0,3,6,8,10,12,14,16,18,22,26,30]}
sk={}; feet={}
for k,frs in FR.items():
    arm.animation_data.action=A[k]
    for f in range(31):
        sc.frame_set(f); sk.setdefault(k,[]).append(skew())
        if k=='walk':
            a=bm('LeftFoot').to_translation(); b=bm('RightFoot').to_translation(); feet[f]=round(abs(a.x-b.x)*100,1)
    for f in frs:
        sc.frame_set(f); M=bm('LeftHand'); bp,bR=place(bow,M,bow_off); bow.matrix_world=Matrix.Translation(bp)@bR.to_4x4(); bow.hide_render=not(k=='attack' and BOWV0<=f<=BOWV1)
        qp,qR=place(quiv,bm('Spine2'),q_off); quiv.matrix_world=Matrix.Translation(qp)@qR.to_4x4()
        p,R=place(arrow,bm('RightHand'),ar_off); arrow.matrix_world=Matrix.Translation(p)@R.to_4x4(); arrow.hide_render=not(k=='attack' and V0<=f<REL)
        proj.matrix_world=Matrix.Translation(sp+fw*max(0,f-REL+1)*SPD/30.0)@sR.to_4x4(); proj.hide_render=not(k=='attack' and f>=REL)
        for v in ('full','face','side','back'):
            shot(f'{W}/sh_{k}_{f:02d}_{v}.png',v)
rep['face_skew_max_cm']={k:round(max(a for a,b in v)*100,3) for k,v in sk.items()}
rep['neck_head_band_max_cm']={k:round(max(b for a,b in v)*100,3) for k,v in sk.items()}
rep['walk_ankle_spacing_cm']=feet
json.dump(rep,open(W+'/sheet.json','w'),indent=1); print('SHEET',json.dumps({k:v for k,v in rep.items() if not k.startswith(('arrow_held','arrow_proj','bow_att','quiver_att'))}))
