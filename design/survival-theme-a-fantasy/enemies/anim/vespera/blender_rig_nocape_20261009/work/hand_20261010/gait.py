import bpy, json, math, sys
arm=bpy.data.objects['VESPERA_rig']; sc=bpy.context.scene; P='mixamorig:'
arm.animation_data.action=bpy.data.actions['VESPERA_nocape_walk']
R={'knee':{},'ank_y':{},'ank_x':{},'hipz':[],'foot_yaw':{}}
for s in ('Left','Right'):
    for k in R:
        if k!='hipz': R[k][s]=[]
for f in range(31):
    sc.frame_set(f)
    for s in ('Left','Right'):
        u=arm.pose.bones[P+s+'UpLeg']; l=arm.pose.bones[P+s+'Leg']; ft=arm.pose.bones[P+s+'Foot']
        a=(u.tail-u.head).normalized(); b=(l.tail-l.head).normalized()
        R['knee'][s].append(round(math.degrees(a.angle(b)),1)); R['ank_y'][s].append(l.tail.y); R['ank_x'][s].append(l.tail.x)
        d=ft.tail-ft.head; R['foot_yaw'][s].append(round(math.degrees(math.atan2(d.x,-d.y)),1))
    R['hipz'].append(round(arm.pose.bones[P+'Spine1'].head.z,3))
out={'knee_bend_deg':{s:[min(v),max(v)] for s,v in R['knee'].items()},'stride_m':{s:round(max(v)-min(v),3) for s,v in R['ank_y'].items()},
 'stance_width_m':round(sum(abs(a-b) for a,b in zip(R['ank_x']['Left'],R['ank_x']['Right']))/31,3),'pelvis_root_z':[min(R['hipz']),max(R['hipz'])],
 'foot_yaw_deg':{s:[min(v),max(v)] for s,v in R['foot_yaw'].items()},'knee_by_frame':R['knee']}
print('GAIT',json.dumps(out)); json.dump(out,open(sys.argv[-1],'w'),indent=1)
