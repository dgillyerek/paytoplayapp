import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy, math, json, numpy as np
from mathutils import Vector, Matrix
OUT='/workspace/oakenshield_fresh'; W='/workspace/oakenshield_fresh/work'
bpy.ops.wm.open_mainfile(filepath=W+'/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['OAKENSHIELD_rig']; body=bpy.data.objects['OAKENSHIELD_body']; P_='mixamorig:'
bpy.ops.import_scene.fbx(filepath=W+'/OAKENSHIELD_thornspear.fbx'); spear=bpy.data.objects['OAKENSHIELD_thornspear']; spear.rotation_mode='QUATERNION'
A={k:bpy.data.actions['OAKENSHIELD_'+k] for k in ('rest','walk','attack')}
rep={}
# ---- face skew metric: head-region verts vs the rigid Head transform ----
me=body.data; N=len(me.vertices)
P0=np.array([v.co[:] for v in me.vertices])
hi=body.vertex_groups[P_+'Head'].index
hw=np.zeros(N)
for v in me.vertices:
    for g in v.groups:
        if g.group==hi: hw[v.index]=g.weight
face=np.nonzero(hw>0.999)[0]; band=np.nonzero((hw>0.01)&(hw<0.999))[0]
Hrest=arm.data.bones[P_+'Head'].matrix_local
def deformed():
    dg=bpy.context.evaluated_depsgraph_get(); e=body.evaluated_get(dg); m=e.to_mesh(); Q=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',Q); e.to_mesh_clear(); return Q.reshape(-1,3)
def skew():
    Q=deformed(); M=np.array(arm.pose.bones[P_+'Head'].matrix@Hrest.inverted())
    R=(np.c_[P0,np.ones(N)]@M.T)[:,:3]; d=np.linalg.norm(Q-R,axis=1); return float(d[face].max()),float(d[band].max()) if len(band) else 0
# ---- spear: held frames per #48 finalize2 (grip 7 cm along RightHand, bound at f10, tip forward + a little up) ----
REF,REL=10,14
arm.animation_data.action=A['attack']; sc.frame_set(REF); pb=arm.pose.bones[P_+'RightHand']
Mref=arm.matrix_world@pb.matrix; gref=Mref.to_translation()+(Mref.to_3x3()@Vector((0,1,0)))*0.07
tip=Vector((0,-1,0.12)).normalized(); yax=-tip; xax=Vector((1,0,0)); xax=(xax-xax.dot(yax)*yax).normalized(); zax=xax.cross(yax)
Rsp=Matrix((xax,yax,zax)).transposed(); Rh=Mref.to_3x3().normalized(); Roff=Rh.inverted()@Rsp; poff=Rh.inverted()@(gref-Mref.to_translation())
C=Matrix(((-1,0,0),(0,0,1),(0,-1,0)))
def U(v): return [round(-v.x,5),round(v.z,5),round(-v.y,5)]
def UQ(Rb):
    q=(C@Rb@C.inverted()).to_quaternion().normalized()
    if q.w<0: q=-q
    return [round(q.x,5),round(q.y,5),round(q.z,5),round(q.w,5)]
held=[]; spearpose={}
for f in range(31):
    sc.frame_set(f); M=arm.matrix_world@pb.matrix; R=M.to_3x3().normalized(); p=M.to_translation()+R@poff; Rw=R@Roff
    s_=0.05 if f<3 else (min(1.0,0.05+0.95*(f-3)/5.0) if f<=8 else 1.0); vis=3<=f<REL
    held.append(U(p)+UQ(Rw)+[round(s_,5),vis]); spearpose[f]=(p.copy(),Rw.copy(),s_,vis)
spawn=held[REL][:3]; dirw=-(spearpose[REL][1].col[1])
proj=[[spawn[0],spawn[1],round(spawn[2]+max(0,f-REL)*11.0/30.0,5),0,0,0,1,1,f>=REL] for f in range(31)]
cal={n:U(arm.matrix_world@arm.data.bones[P_+n].head_local) for n in ('Hips','Head','LeftHand','RightHand')}
rep.update(calibration_unity=cal,spear_held_unity=held,spear_proj_unity=proj)
# ---- render ----
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
sc.render.film_transparent=False; sc.world=bpy.data.worlds.new('w') if sc.world is None else sc.world
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam; cam.data.type='ORTHO'
def shot(path,full=True,side=False):
    if side: sc.render.resolution_x=360; sc.render.resolution_y=640; cam.data.ortho_scale=2.0; cam.location=(5,0,0.95); cam.rotation_euler=(math.pi/2,0,math.pi/2); sc.render.filepath=path; bpy.ops.render.render(write_still=True); return
    if full: sc.render.resolution_x=360; sc.render.resolution_y=640; cam.data.ortho_scale=2.0; cam.location=(0,-5,0.95)
    else:
        sc.render.resolution_x=360; sc.render.resolution_y=360; cam.data.ortho_scale=0.42
        h=arm.matrix_world@arm.pose.bones[P_+'Head'].head; cam.location=(h.x,-5,h.z+0.12)
    cam.rotation_euler=(math.pi/2,0,0); sc.render.filepath=path; bpy.ops.render.render(write_still=True)
FR={'rest':[0],'walk':[0,4,8,12,15,19,23,27],'attack':[0,5,8,10,12,14,17,20,25,30]}
sk={}; feet={}
for k,frs in FR.items():
    arm.animation_data.action=A[k]
    for f in range(31):
        sc.frame_set(f); sk.setdefault(k,[]).append(skew())
        if k=='walk':
            a=arm.matrix_world@arm.pose.bones[P_+'LeftFoot'].head; b=arm.matrix_world@arm.pose.bones[P_+'RightFoot'].head; feet[f]=round(abs(a.x-b.x)*100,1)
    for f in frs:
        sc.frame_set(f)
        if k=='attack':
            p,R,s_,vis=spearpose[f]
            if f>=REL: p=spearpose[REL][0]+dirw*(f-REL)*11/30; R=spearpose[REL][1]
            spear.matrix_world=Matrix.Translation(p)@R.to_4x4()@Matrix.Scale(s_,4); spear.hide_render=not (vis or f>=REL)
        else: spear.hide_render=True
        shot(f'{W}/sh_{k}_{f:02d}_full.png'); shot(f'{W}/sh_{k}_{f:02d}_face.png',False)
        if k=='attack': shot(f'{W}/sh_{k}_{f:02d}_side.png',side=True)
rep['face_skew_max_cm']={k:round(max(a for a,b in v)*100,3) for k,v in sk.items()}
rep['neck_head_band_max_cm']={k:round(max(b for a,b in v)*100,3) for k,v in sk.items()}
rep['walk_ankle_spacing_cm']=feet
json.dump(rep,open(W+'/sheet.json','w'),indent=1); print('SHEET',json.dumps({k:v for k,v in rep.items() if not k.startswith('spear')}))
