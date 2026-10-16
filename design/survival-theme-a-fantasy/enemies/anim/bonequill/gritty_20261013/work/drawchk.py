"""Derek 10-15 23:29: drawn string and nocked arrow at the draw. Per half frame f10-16.5 (nocked, string at the nock):
string = top tip -> nock -> bottom tip; arrow = nock -> 0.79 m along nock->rest. Reports min distance of string and arrow to the
body (draw-hand bones exempt: they hold the nock) and to the cloak, the arrow's clearance from the riser axis, and the arrow yaw/pitch
off the attack direction (-y) at the release frame. Writes draw_check.json."""
import sys; sys.path.insert(0,'/workspace/bonequill_gritty/work'); from bowroll import bow_matrix, ARROW_REST_BOW
import bpy,json,math,numpy as np
from mathutils import Vector
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P_='mixamorig:'
body=bpy.data.objects['BONEQUILL_body']
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
cape,crig=d.objects
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P_+bn
arm.animation_data.action=bpy.data.actions['BONEQUILL_attack']
FI=json.load(open(W+'/fist.json')); GL=Vector(FI['grip_local'])
gi={g.index:g.name for g in body.vertex_groups}
ex=np.array([any(gi[g.group].startswith(P_+'RightHand') and g.weight>0.3 for g in v.groups) for v in body.data.vertices])
bowex=np.array([any(gi[g.group].startswith(P_+'LeftHand') and g.weight>0.3 for g in v.groups) for v in body.data.vertices])
hw=body.vertex_groups[P_+'RightHand'].index; HM=arm.data.bones[P_+'RightHand'].matrix_local.inverted()
fl=np.array([(HM@v.co)[:] for v in body.data.vertices if any(g.group==hw and g.weight>0.9 for g in v.groups)]); yl=fl[:,1].max()
NL=Vector(fl[(fl[:,1]>0.45*yl)&(fl[:,1]<0.8*yl)].mean(0))
def mesh(o):
    dg=bpy.context.evaluated_depsgraph_get(); e=o.evaluated_get(dg); m=e.to_mesh(); Q=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',Q); e.to_mesh_clear()
    Q=Q.reshape(-1,3); return (np.c_[Q,np.ones(len(Q))]@np.array(o.matrix_world).T)[:,:3]
def segd(Q,a,b):
    a,b=np.array(a),np.array(b); ab=b-a; t=np.clip(((Q-a)@ab)/(ab@ab),0,1); return np.linalg.norm(Q-(a+t[:,None]*ab),axis=1)
rows=[]
for k in range(20,34):
    f=k/2; sc.frame_set(int(f),subframe=f-int(f))
    hand=arm.matrix_world@arm.pose.bones[P_+'LeftHand'].matrix; B=bow_matrix(hand,GL,'attack',f)
    nk=arm.matrix_world@arm.pose.bones[P_+'RightHand'].matrix@NL; top=B@Vector((0,0.19,0.62)); bot=B@Vector((0,0.19,-0.62)); rest=B@Vector(ARROW_REST_BOW)
    d=(rest-nk).normalized(); tip=nk+d*0.79
    Qb=mesh(body); Qc=mesh(cape); Qbo=Qb[~ex]
    s_body=min(segd(Qbo,top,nk).min(),segd(Qbo,nk,bot).min()); s_cape=min(segd(Qc,top,nk).min(),segd(Qc,nk,bot).min())
    Qa=Qb[~ex&~bowex]; a_body=segd(Qa,nk,tip).min(); a_cape=segd(Qc,nk,tip).min()
    Bi=B.inverted(); pn=Bi@nk; pt=Bi@tip
    # arrow clearance from the riser axis (bow-local z) near the grip
    import numpy as _n; ts=_n.linspace(0,1,200); P=[pn.lerp(pt,t) for t in ts]; riser=min(math.hypot(p.x,p.y) for p in P if abs(p.z)<0.07) if any(abs(p.z)<0.07 for p in P) else 9
    side=(Bi@rest).x
    yaw=math.degrees(math.atan2(d.x,-d.y)); pitch=math.degrees(math.asin(d.z))
    rows.append([f,round(s_body,4),round(s_cape,4),round(a_body,4),round(a_cape,4),round(riser,4),round(side,4),round(yaw,2),round(pitch,2),[round(v,4) for v in nk]])
rel=[r for r in rows if r[0]==16.5][0]
out={'columns':['frame','string_body_m','string_cloak_m','arrow_body_m','arrow_cloak_m','arrow_riser_axis_m','rest_side_x','yaw_deg','pitch_deg','nock_world'],
     'rows':rows,'min':{'string_body_m':min(r[1] for r in rows),'string_cloak_m':min(r[2] for r in rows),'arrow_body_m':min(r[3] for r in rows),'arrow_cloak_m':min(r[4] for r in rows)},
     'release':{'frame':16.5,'yaw_deg':rel[7],'pitch_deg':rel[8]},'arrow_rest_bow':list(ARROW_REST_BOW)}
json.dump(out,open(W+'/draw_check.json','w'),indent=1); print('DRAWCHK',json.dumps({k:out[k] for k in ('min','release')}))
for r in rows: print('ROW',r[:9])
