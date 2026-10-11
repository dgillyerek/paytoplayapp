import bpy, sys, os, math, json
from mathutils import Matrix, Vector
REST,WALK,OUTJ=sys.argv[sys.argv.index('--')+1:]
def imp(p):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=p)
    arm=[o for o in bpy.data.objects if o.type=='ARMATURE'][0]; mesh=[o for o in bpy.data.objects if o.type=='MESH']
    return arm,mesh
def bw(arm,n): return arm.matrix_world@arm.pose.bones[n].matrix
P='mixamorig:'
arm,mesh=imp(REST)
R={'bones':len(arm.data.bones),'names':[b.name for b in arm.data.bones],'faces':sum(len(m.data.polygons) for m in mesh),'verts':sum(len(m.data.vertices) for m in mesh),
   'hips':bw(arm,P+'Hips').copy(),'has_anim':bool(arm.animation_data and arm.animation_data.action)}
dg=bpy.context.evaluated_depsgraph_get(); ev=mesh[0].evaluated_get(dg); m=ev.to_mesh(); mw=ev.matrix_world
pts=[mw@v.co for v in m.vertices]; R['bbox']=[[min(p[i] for p in pts) for i in range(3)],[max(p[i] for p in pts) for i in range(3)]]
ev.to_mesh_clear()
# facing in rest: nose/toe direction: toe tip y relative to ankle
R['toe_minus_foot_y']=(arm.matrix_world@arm.data.bones[P+'LeftToeBase'].tail_local).y-(arm.matrix_world@arm.data.bones[P+'LeftFoot'].head_local).y
arm,mesh=imp(WALK); act=arm.animation_data.action; sc=bpy.context.scene
f0,f1=int(act.frame_range[0]),int(act.frame_range[1])
rows=[]; poses={}
for f in range(f0,f1+1):
    sc.frame_set(f)
    lf=bw(arm,P+'LeftFoot').translation.copy(); rf=bw(arm,P+'RightFoot').translation.copy()
    lk=bw(arm,P+'LeftLeg').translation.copy(); lu=bw(arm,P+'LeftUpLeg').translation.copy()
    rows.append(dict(f=f,Ly=lf.y,Ry=rf.y,Lz=lf.z,Rz=rf.z,knee_y=lk.y,hip_y=lu.y,hz=bw(arm,P+'Hips').translation.z))
    poses[f]={b.name:b.matrix.copy() for b in arm.pose.bones}
    if f==f0:
        H0=bw(arm,P+'Hips'); hd=(H0.translation-R['hips'].translation).length
        ha=math.degrees(H0.to_quaternion().rotation_difference(R['hips'].to_quaternion()).angle)
        fwd=H0.to_3x3()@Vector((0,0,1))  # hips bone local z
        fwd_rest=R['hips'].to_3x3()@Vector((0,0,1))
ly=[r['Ly'] for r in rows]; ry=[r['Ry'] for r in rows]; n=len(ly); ml=sum(ly)/n; mr=sum(ry)/n
corr=sum((a-ml)*(b-mr) for a,b in zip(ly,ry))/math.sqrt(sum((a-ml)**2 for a in ly)*sum((b-mr)**2 for b in ry)+1e-12)
lp=max(math.degrees(poses[f0][k].to_quaternion().rotation_difference(poses[f1][k].to_quaternion()).angle) for k in poses[f0])
ld=max((poses[f0][k].translation-poses[f1][k].translation).length for k in poses[f0])
# knee direction: at the frame of max knee bend, knee should be in front (-Y) of hip->ankle line
imax=max(range(n),key=lambda i:-ly[i])  # left foot most forward
kneefront=[(r['knee_y']-0.5*(r['hip_y']+r['Ly'])) for r in rows]
out=dict(rest_bones=R['bones'],rest_faces=R['faces'],rest_verts=R['verts'],rest_bbox=R['bbox'],rest_has_anim=R['has_anim'],
  rest_toe_minus_foot_y=R['toe_minus_foot_y'],walk_bones=len(arm.data.bones),walk_names_same=[b.name for b in arm.data.bones]==R['names'],
  walk_faces=sum(len(m.data.polygons) for m in mesh),action=act.name,frame_range=[f0,f1],
  hips_f0_vs_rest_m=hd,hips_f0_vs_rest_deg=ha,hips_localZ_rest=list(fwd_rest),hips_localZ_f0=list(fwd),
  foot_y_corr_LR=corr,Lfoot_y_range=[min(ly),max(ly)],Rfoot_y_range=[min(ry),max(ry)],
  loop_first_vs_last_deg=lp,loop_first_vs_last_m=ld,hips_z_range=[min(r['hz'] for r in rows),max(r['hz'] for r in rows)],
  min_foot_z=min(min(r['Lz'],r['Rz']) for r in rows),knee_offset_y_min_max=[min(kneefront),max(kneefront)])
json.dump(out,open(OUTJ,'w'),indent=1,default=float)
print('VERIFY',json.dumps({k:(round(v,4) if isinstance(v,float) else v) for k,v in out.items()},default=float))
