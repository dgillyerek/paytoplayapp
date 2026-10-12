import bpy,sys,json,math,numpy as np
from mathutils import Vector
OUT=sys.argv[sys.argv.index('--')+1]; ACT=sys.argv[sys.argv.index('--')+2] if len(sys.argv)>sys.argv.index('--')+2 else 'LYRA_tighten_attack'
P='mixamorig:'; sc=bpy.context.scene
arm=bpy.data.objects['LYRA_tighten_rig']; body=bpy.data.objects['LYRA_tighten_body']; ad=arm.animation_data
for t in ad.nla_tracks: t.mute=True
me=body.data
# texture
img=None
for m in me.materials:
    for n in m.node_tree.nodes:
        if n.type=='TEX_IMAGE' and n.image and 'base' in n.image.name.lower(): img=n.image
if img is None:
    for m in me.materials:
        for n in m.node_tree.nodes:
            if n.type=='TEX_IMAGE' and n.image: img=n.image; break
W,H=img.size; px=np.empty(W*H*4,np.float32); img.pixels.foreach_get(px); px=px.reshape(H,W,4)
uv=me.uv_layers.active.data; nl=len(me.loops)
UV=np.empty(nl*2,np.float32); uv.foreach_get('uv',UV); UV=UV.reshape(-1,2)
LV=np.empty(nl,np.int64); me.loops.foreach_get('vertex_index',LV)
xi=np.clip((UV[:,0]%1)*W,0,W-1).astype(int); yi=np.clip((UV[:,1]%1)*H,0,H-1).astype(int)
col=px[yi,xi,:3]
nv=len(me.vertices); csum=np.zeros((nv,3)); cnt=np.zeros(nv)
np.add.at(csum,LV,col); np.add.at(cnt,LV,1); C=csum/np.maximum(cnt,1)[:,None]
gold=(C[:,0]>0.45)&(C[:,1]>0.3)&(C[:,2]<0.35)&(C[:,0]-C[:,2]>0.25)
gi={g.index:g.name.replace(P,'') for g in body.vertex_groups}
dom=[]; wts=[]
for v in me.vertices:
    ws={gi[g.group]:g.weight for g in v.groups}; d=max(ws,key=ws.get) if ws else '?'; dom.append(d); wts.append(ws)
dom=np.array(dom)
def evalV():
    dg=bpy.context.evaluated_depsgraph_get(); be=body.evaluated_get(dg); m=be.to_mesh()
    V=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',V); N=np.empty(len(m.vertices)*3); m.vertices.foreach_get('normal',N)
    be.to_mesh_clear(); Mw=np.array(body.matrix_world); V=V.reshape(-1,3)@Mw[:3,:3].T+Mw[:3,3]; N=N.reshape(-1,3)@Mw[:3,:3].T
    return V,N
def yaw(v): return math.degrees(math.atan2(v[0],-v[1]))
def dd(a): return (a+180)%360-180
# bind pose selection
arm.data.pose_position='REST'; sc.frame_set(0)
Vb,Nb=evalV()
sel={}
for reg,bones in (('thigh',('LeftUpLeg',)),('shin',('LeftLeg',)),('boot',('LeftFoot',))):
    m=np.isin(dom,bones)&gold
    # axis of the region: bone head->tail in bind
    b=arm.data.bones[P+bones[0]]; h=np.array(b.head_local); t=np.array(b.tail_local); ax=(t-h)/np.linalg.norm(t-h)
    rel=Vb-h; along=rel@ax; radial=rel-np.outer(along,ax)
    fw=np.array([0,-1,0.]); fw=fw-fw.dot(ax)*ax; fw/=np.linalg.norm(fw)
    cosf=(radial@fw)/np.maximum(np.linalg.norm(radial,axis=1),1e-9)
    L=np.linalg.norm(t-h)
    m2=m&(cosf>0.5)&(along>0.1*L)&(along<0.9*L)
    if reg=='boot': m2=m&(cosf>0.5)
    sel[reg]=np.where(m2)[0]
    print('SEL',reg,int(m.sum()),len(sel[reg]))
# also outer-side panel candidates for reference
def facing(V,N,idx,bn):
    pb=arm.pose.bones[P+bn]; h=np.array(pb.head); t=np.array(pb.tail); ax=(t-h)/np.linalg.norm(t-h)
    rel=V[idx]-h; along=rel@ax; radial=rel-np.outer(along,ax); r=radial.mean(0)
    n=N[idx].mean(0)
    return yaw(r), yaw(n)
def frame_meas(tag):
    V,N=evalV(); pb=arm.pose.bones
    hips=np.array(pb[P+'Hips'].matrix.to_3x3())@np.array([0,0,0.])  # placeholder
    # pelvis forward: rest-forward axis of hips bone carried by pose
    pf=np.array(pb[P+'Hips'].matrix.to_3x3()@FL['Hips']); py=yaw(pf)
    ft=pb[P+'LeftFoot']; fd=np.array(ft.tail-ft.head); fy=yaw(fd)
    H=pb[P+'LeftUpLeg'].head; K=pb[P+'LeftLeg'].head; A=pb[P+'LeftFoot'].head; e=(A-H).normalized(); kp=(K-H)-(K-H).dot(e)*e
    r={'tag':tag,'pelvis':round(py,1),'foot':round(fy,1),'knee_cm':round(kp.length*100,1),'knee':round(yaw(kp),1) if kp.length>0.01 else None}
    for reg,bn in (('thigh','LeftUpLeg'),('shin','LeftLeg'),('boot','LeftFoot')):
        cy,ny=facing(V,N,sel[reg],bn if reg!='boot' else 'LeftLeg')
        r[reg]=round(ny,1); r[reg+'_vsPelvis']=round(dd(ny-py),1); r[reg+'_vsFoot']=round(dd(ny-fy),1)
    # bone roll: thigh/shin rest-forward axis yaw
    for bn in ('LeftUpLeg','LeftLeg'):
        r[bn+'_bone_vsPelvis']=round(dd(yaw(np.array(pb[P+bn].matrix.to_3x3()@FL[bn]))-py),1)
    return r
# rest-forward local axes from BIND (edit) pose
FL={b:arm.data.bones[P+b].matrix_local.to_3x3().inverted()@Vector((0,-1,0)) for b in ('Hips','LeftUpLeg','LeftLeg')}
res=[frame_meas('bind')]
arm.data.pose_position='POSE'
for a,f in (('LYRA_tighten_rest',0),('LYRA_tighten_walk',0)):
    ad.action=bpy.data.actions[a]; sc.frame_set(f); res.append(frame_meas(a.split('_')[-1]+f'_f{f}'))
ad.action=bpy.data.actions[ACT]
for f in range(31): sc.frame_set(f); res.append(frame_meas(f'atk_f{f}'))
json.dump(dict(res=res,sel={k:v.tolist() for k,v in sel.items()},img=img.name),open(OUT,'w'))
for r in res: print('M',json.dumps(r))
