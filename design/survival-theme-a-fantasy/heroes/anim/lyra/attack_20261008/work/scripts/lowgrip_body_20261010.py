"""Stage A: author LYRA_tighten_attack body (hips/spine/neck/head/legs) for the two-handed side-on cast; arms at rest. Dump per-frame data."""
import bpy, sys, json, math, numpy as np
from mathutils import Matrix, Vector
args=sys.argv[sys.argv.index('--')+1:]; PAR=json.load(open(args[0])); OUT=args[1]; SAVE=len(args)>2 and args[2]=='save'
sc=bpy.context.scene; arm=bpy.data.objects['LYRA_tighten_rig']; body=bpy.data.objects['LYRA_tighten_body']; staff=bpy.data.objects['LYRA_staff']
P='mixamorig:'; B={b.name.replace(P,''):b for b in arm.data.bones}; REST={k:b.matrix_local.copy() for k,b in B.items()}
PAR_={k:(b.parent.name.replace(P,'') if b.parent else None) for k,b in B.items()}
order=[b.name.replace(P,'') for b in arm.data.bones]
def ss(t): t=min(1,max(0,t)); return t*t*(3-2*t)
def env(f,a,b,c,d): return ss((f-a)/(b-a))*(1-ss((f-c)/(d-c)))
def Rz(a): return Matrix.Rotation(math.radians(a),4,'Z')
def about(p,M): return Matrix.Translation(p)@M@Matrix.Translation(-p)
def align(y0,p0,y1,p1):
    def fr(y,p):
        y=y.normalized(); p=(p-p.dot(y)*y).normalized(); return Matrix((y,p,y.cross(p))).transposed()
    return fr(y1,p1)@fr(y0,p0).inverted()
def heads(M,n): # posed head of bone n from parent pose
    p=PAR_[n]; return (M[p]@REST[p].inverted()@REST[n]).translation
def pose_frame(f):
    s=env(f,*PAR['turn_env']); M={}
    yh=PAR['yaw_hips']*s; drop=PAR['drop']*s
    h0=REST['Hips'].translation
    fr=PAR['thrust_f']; hy=float(np.interp(f,fr,PAR['thrust_hy'])); lt=float(np.interp(f,fr,PAR['thrust_lean']))
    M['Hips']=Matrix.Translation((0,hy,-drop))@about(h0,Rz(yh))@REST['Hips']
    cum=yh
    for n,(dy,lean) in zip(['Spine','Spine1','Spine2'],PAR['spine']):
        cum+=dy*s; hp=heads(M,n)
        L=Rz(cum)@Matrix.Rotation(math.radians(lean*s),4,'X')@Rz(-cum)
        L=Matrix.Rotation(math.radians(lt/3.0),4,Vector((math.cos(math.radians(0)),0,0)))@L
        R=(Rz(cum)@L.to_4x4() if False else L@Rz(cum)).to_3x3()@REST[n].to_3x3()
        M[n]=Matrix.Translation(hp)@R.to_4x4()
    for n,dy in (('Neck',PAR['neck']),('Head',PAR['head'])):
        cum+=dy*s; hp=heads(M,n); M[n]=Matrix.Translation(hp)@(Matrix.Rotation(math.radians(lt*0.3),3,'X')@Rz(cum).to_3x3()@REST[n].to_3x3()).to_4x4()
    for side in ('Left','Right'):
        for n in ('Shoulder','Arm','ForeArm','Hand'):
            k=side+n; p=PAR_[k]; M[k]=M[p]@REST[p].inverted()@REST[k]
        phi=PAR['pivot_'+side]*env(f,*PAR['pivot_env'])
        RF=REST[side+'Foot']; T0=B[side+'Foot'].tail_local.copy(); piv=Vector((T0.x,T0.y,0))
        MF=about(piv,Rz(phi))@RF; A=MF.translation
        H=heads(M,side+'UpLeg'); H0=REST[side+'UpLeg'].translation; K0=REST[side+'Leg'].translation; A0=RF.translation
        L1=(K0-H0).length; L2=(A0-K0).length; dv=A-H; d=dv.length
        if d>L1+L2-1e-4: d=L1+L2-1e-4
        dh=dv.normalized()
        ax0=(A0-H0).normalized(); pole0=(K0-H0)-((K0-H0).dot(ax0))*ax0; pole0.normalize()
        pole=(Rz(phi*PAR['knee_follow']+yh*(1-PAR['knee_follow'])).to_3x3()@pole0); pole=(pole-pole.dot(dh)*dh).normalized()
        a=(L1*L1-L2*L2+d*d)/(2*d); hh=math.sqrt(max(0,L1*L1-a*a)); K=H+dh*a+pole*hh
        Q1=align(K0-H0,pole0,K-H,pole); M[side+'UpLeg']=Matrix.Translation(H)@(Q1@REST[side+'UpLeg'].to_3x3()).to_4x4()
        Q2=align(A0-K0,pole0,A-K,pole); M[side+'Leg']=Matrix.Translation(K)@(Q2@REST[side+'Leg'].to_3x3()).to_4x4()
        M[side+'Foot']=Matrix.Translation(H+dh*d if False else A)@MF.to_3x3().to_4x4()
    return M
def FC(a):
    out=[]
    for Ly in a.layers:
        for st in Ly.strips:
            for cb in st.channelbags: out+=[(cb,fc) for fc in cb.fcurves]
    return out
act=bpy.data.actions['LYRA_tighten_attack']; ad=arm.animation_data
cur={(fc.data_path.split('"')[1].replace(P,''),fc.data_path.rsplit('.',1)[1],fc.array_index):fc for cb,fc in FC(act) if fc.data_path.startswith('pose.bones["')}
prevE={}; REP={'leg_reach':[]}
for f in range(31):
    M=pose_frame(f)
    for n in order:
        p=PAR_[n]; Bm=(REST[n].inverted()@M[n]) if p is None else REST[n].inverted()@REST[p]@M[p].inverted()@M[n]
        loc,rot,scl=Bm.decompose(); e=rot.to_euler('XYZ',prevE[n]) if n in prevE else rot.to_euler('XYZ'); prevE[n]=e
        if n!='Hips': loc=Vector((0,0,0)) if loc.length<1e-5 else loc
        if n!='Hips' and loc.length>1e-4: print('WARN loc',n,f,loc)
        for prop,v in (('location',loc),('rotation_euler',e),('scale',Vector((1,1,1)))):
            for i in range(3):
                fc=cur[(n,prop,i)]; kp=[k for k in fc.keyframe_points if abs(k.co[0]-f)<1e-4][0]
                dv=v[i]-kp.co[1]; kp.co[1]=v[i]; kp.handle_left[1]+=dv; kp.handle_right[1]+=dv
for fc in cur.values(): fc.update()
ad.action=act
names=[b.name for b in arm.data.bones]
pose=[];bv=[];bn=[];feet=[]
for f in range(31):
    sc.frame_set(f); dg=bpy.context.evaluated_depsgraph_get()
    pose.append([np.array(arm.pose.bones[n].matrix) for n in names])
    be=body.evaluated_get(dg); me=be.to_mesh()
    co=np.empty(len(me.vertices)*3,np.float32); me.vertices.foreach_get('co',co)
    no=np.empty(len(me.vertices)*3,np.float32); me.vertices.foreach_get('normal',no)
    bv.append(co.reshape(-1,3)); bn.append(no.reshape(-1,3)); be.to_mesh_clear()
    feet.append([list(arm.pose.bones[P+s+'Foot'].tail)+list(arm.pose.bones[P+s+'Foot'].head) for s in ('Left','Right')])
gi={g.index:g.name for g in body.vertex_groups}
dom=[]
for v in body.data.vertices:
    best=max(v.groups,key=lambda g:g.weight,default=None); dom.append(gi[best.group] if best else '?')
sv=np.array([v.co[:] for v in staff.data.vertices],np.float32)
np.savez(OUT,names=np.array(names),rest=np.array([np.array(b.matrix_local) for b in arm.data.bones]),pose=np.array(pose),bv=np.array(bv),bn=np.array(bn),dom=np.array(dom),sv=sv,
   stm0=np.array(staff.matrix_world) if False else np.array(arm.data.bones[P+'RightHand'].matrix_local.inverted()@Matrix(json.load(open(PAR['staff_meta']))['rest_world_blender'])),feet=np.array(feet))
ad.action=bpy.data.actions['LYRA_tighten_walk']; sc.frame_set(0)
if SAVE: bpy.ops.wm.save_as_mainfile(filepath=args[3],compress=True)
print('BODY OK',np.array(feet)[[0,8,12,30]].round(3).tolist())
