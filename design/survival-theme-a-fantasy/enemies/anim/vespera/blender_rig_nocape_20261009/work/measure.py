import bpy, numpy as np, math, json
o=bpy.data.objects['VESPERA_body']; a=bpy.data.objects['VESPERA_rig']; sc=bpy.context.scene
names=[g.name for g in o.vertex_groups]; N=len(o.data.vertices)
dom=np.array([names[max(v.groups,key=lambda g:g.weight).group] for v in o.data.vertices])
fs={s:np.isin(dom,['mixamorig:%sFoot'%s,'mixamorig:%sToeBase'%s]) for s in ('Left','Right')}
def coords():
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); m=ob.to_mesh(); c=np.empty(N*3); m.vertices.foreach_get('co',c); ob.to_mesh_clear(); return c.reshape(-1,3)
out={}
a.animation_data.action=bpy.data.actions['VESPERA_nocape_walk']
pb=a.pose.bones; rec={s:[] for s in fs}; head=[]; ank=[]; yaw={s:[] for s in fs}; hand=[]
for f in range(31):
    sc.frame_set(f); c=coords()
    for s,m in fs.items():
        P=c[m]; zmin=P[:,2].min(); low=P[P[:,2]<zmin+0.004]
        ball=a.matrix_world@pb['mixamorig:%sToeBase'%s].head; ft=a.matrix_world@pb['mixamorig:%sFoot'%s].head
        rec[s].append((f,zmin,ball.x,ball.y,ball.z)); d=ball-ft; yaw[s].append(math.degrees(math.atan2(d.x,-d.y)))
    H=pb['mixamorig:Head'].matrix; head.append(H.to_euler()[:])
    ank.append(((a.matrix_world@pb['mixamorig:LeftFoot'].head).x-(a.matrix_world@pb['mixamorig:RightFoot'].head).x))
    hip=a.matrix_world@pb['mixamorig:Hips'].head
    hand.append([abs((a.matrix_world@pb['mixamorig:%sHand'%s].head).x-hip.x) for s in ('Left','Right')])
for s in fs:
    R=np.array(rec[s]); bz=R[:,4]; planted=bz<bz.min()+0.01
    xy=R[planted][:,2:4]; slide=float(np.linalg.norm(xy-xy.mean(0),axis=1).max()*100) if len(xy) else 0
    out[s]={'sole_min_z_cm':round(float(R[:,1].min())*100,2),'sole_max_ground_gap_when_planted_cm':round(float(R[planted][:,1].max())*100,2),
            'ball_planted_frames':int(planted.sum()),'ball_slide_cm':round(slide,2),'foot_yaw_deg_range':[round(min(yaw[s]),1),round(max(yaw[s]),1)]}
E=np.degrees(np.array(head)); out['head_rot_range_deg']=[round(float(x),1) for x in (E.max(0)-E.min(0))]
out['ankle_width_cm']=[round(min(ank)*100,1),round(max(ank)*100,1)]
H=np.array(hand); out['hand_lateral_from_hips_cm']=[round(float(H.min())*100,1),round(float(H.max())*100,1)]
for s in fs: print('REC',s,[(int(r[0]),round(r[1]*100,1),round(r[3],2),round(r[4]*100,1)) for r in rec[s]])
print('MEAS',json.dumps(out))
