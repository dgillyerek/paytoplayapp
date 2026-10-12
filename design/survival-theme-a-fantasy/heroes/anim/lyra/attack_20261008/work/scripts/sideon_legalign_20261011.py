import bpy,sys,json,math
from mathutils import Vector
OUT=sys.argv[sys.argv.index('--')+1]
arm=bpy.data.objects['LYRA_tighten_rig']; sc=bpy.context.scene; ad=arm.animation_data
for t in ad.nla_tracks: t.mute=True
ad.action=bpy.data.actions['LYRA_tighten_attack']; P='mixamorig:'
sc.frame_set(0)
fl={pb.name:pb.matrix.to_3x3().inverted()@Vector((0,-1,0)) for pb in arm.pose.bones}
def yaw(v): return math.degrees(math.atan2(v.x,-v.y))
def d(a): return (a+180)%360-180
res=[]; toe0={}
for f in range(31):
    sc.frame_set(f); pb=arm.pose.bones
    hy=yaw(pb[P+'Hips'].matrix.to_3x3()@fl[P+'Hips']); ch=yaw(pb[P+'Spine2'].matrix.to_3x3()@fl[P+'Spine2']); hd=yaw(pb[P+'Head'].matrix.to_3x3()@fl[P+'Head'])
    r=dict(f=f,pelvis=round(hy,1),chest=round(ch,1),head=round(hd,1))
    for s in ('Left','Right'):
        for b in ('UpLeg','Leg','Foot'):
            n=P+s+b; r[s+b]=round(d(yaw(pb[n].matrix.to_3x3()@fl[n])-hy),1)
        H=pb[P+s+'UpLeg'].head; K=pb[P+s+'Leg'].head; A=pb[P+s+'Foot'].head; e=(A-H).normalized(); kp=(K-H)-(K-H).dot(e)*e
        kpv=Vector((kp.x,kp.y,0))
        r[s+'Knee_cm']=round(kp.length*100,1); r[s+'KneeDir']=round(d(yaw(kpv)-hy),1) if kp.length>0.02 else None
        T=pb[P+s+'Foot'].tail; r[s+'toe']=[round(x,3) for x in T]; r[s+'heel_z']=round(A.z,3)
    res.append(r)
json.dump(res,open(OUT,'w'))
for r in res: print('LA',r['f'],r['pelvis'],r['chest'],r['head'],[r[k] for k in ('LeftUpLeg','LeftLeg','LeftFoot','LeftKneeDir','RightUpLeg','RightLeg','RightFoot','RightKneeDir')],r['Lefttoe'][2],r['Righttoe'][2])
print('MAXDEV',max(abs(r[s+b]) for r in res for s in ('Left','Right') for b in ('UpLeg','Leg','Foot')), 'kneedir',max(abs(r[s+'KneeDir']) for r in res for s in ('Left','Right') if r[s+'KneeDir'] is not None))
