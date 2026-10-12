import bpy,sys,math,json,numpy as np
A_=sys.argv[sys.argv.index('--')+1:]; OUT=A_[0]
exec(open('meas.py').read().split("# bind pose selection")[0].replace("OUT=sys.argv[sys.argv.index('--')+1]; ACT=","ACT=").replace("ACT=sys.argv[sys.argv.index('--')+2] if len(sys.argv)>sys.argv.index('--')+2 else 'LYRA_tighten_attack'","ACT='x'"))
G2=(C[:,0]>C[:,2]+0.08)
arm.data.pose_position='REST'; sc.frame_set(0); Vb,_=evalV()
SEG={}
for s in ('Left','Right'):
    for seg,lo,hi in (('UpLeg',0.15,0.85),('Leg',0.1,0.7)):
        b=arm.data.bones[P+s+seg]; h=np.array(b.head_local); t=np.array(b.tail_local); ax=(t-h)/np.linalg.norm(t-h); L=np.linalg.norm(t-h)
        rel=Vb-h; al=rel@ax; rr=np.linalg.norm(rel-np.outer(al,ax),axis=1)
        SEG[(s,seg)]=np.where(np.isin(dom,[s+seg])&(al>lo*L)&(al<hi*L)&(rr<0.13))[0]
FLh=arm.data.bones[P+'Hips'].matrix_local.to_3x3().inverted()@Vector((0,-1,0))
def yawv(v): return math.degrees(math.atan2(v[0],-v[1]))
def dd(a): return (a+180)%360-180
def profile(V,s,seg,ref_yaw):
    idx=SEG[(s,seg)]; pb=arm.pose.bones[P+s+seg]; h=np.array(pb.head); t=np.array(pb.tail); ax=(t-h)/np.linalg.norm(t-h)
    rel=V[idx]-h; rad=rel-np.outer(rel@ax,ax)
    f=np.array([math.sin(math.radians(ref_yaw)),-math.cos(math.radians(ref_yaw)),0]); f=f-f.dot(ax)*ax; f/=np.linalg.norm(f); sv=np.cross(f,ax)
    # angle measured from ref forward, + toward the leg's OUTER side
    ang=np.degrees(np.arctan2(rad@sv,rad@f)); sg=1 if s=='Left' else -1
    # sv = f x ax ; for downward ax and f=-Y: (-Y)x(-Z)=+X -> +X is +: outer for Left
    ang=sg*ang
    A,_=np.histogram(ang,bins=24,range=(-180,180)); Gh,_=np.histogram(ang[G2[idx]],bins=24,range=(-180,180))
    return Gh/np.maximum(A,1)
def center(fr):
    c=np.radians(np.arange(-172.5,180,15)); w=fr-fr.mean()
    return math.degrees(math.atan2((w*np.sin(c)).sum(),(w*np.cos(c)).sum()))
def shift(a,b):
    best=max(((np.corrcoef(np.roll(b,k),a)[0,1],k*15) for k in range(-12,12)))
    return dd(best[1])
def meas(tag):
    V,_=evalV(); pb=arm.pose.bones
    py=yawv(np.array(pb[P+'Hips'].matrix.to_3x3()@FLh)); r={'tag':tag,'pelvis':round(py,1)}
    for s in ('Left','Right'):
        ft=pb[P+s+'Foot']; fy=yawv(np.array(ft.tail-ft.head)); sg=1 if s=='Left' else -1
        r[s+'_foot_vsPelvis_out']=round(sg*dd(fy-py),1)
        for seg in ('UpLeg','Leg'):
            pr=profile(V,s,seg,py); c=center(pr); r[f'{s}{seg}_patt_vsPelvis_out']=round(c,1); r[f'{s}{seg}_patt_vsFoot_out']=round(dd(c-sg*dd(fy-py)),1)
            r[f'{s}{seg}_prof']=pr.round(2).tolist()
    return r
res=[meas('bind')]
arm.data.pose_position='POSE'
for t in ad.nla_tracks: t.mute=True
for a in ('LYRA_tighten_rest','LYRA_tighten_walk'):
    ad.action=bpy.data.actions[a]; sc.frame_set(0); res.append(meas(a.split('_')[-1]+'_f0'))
ad.action=bpy.data.actions['LYRA_tighten_attack']
for f in range(31): sc.frame_set(f); res.append(meas(f'atk_f{f}'))
# residual twist: left profile vs mirrored-right bind profile (rel to own foot -> use pelvis-relative minus foot)
refT=np.array(res[0]['RightUpLeg_prof']); refS=np.array(res[0]['RightLeg_prof'])
for r in res:
    r['LeftUpLeg_shift_vs_rightbind']=shift(np.array(r['LeftUpLeg_prof']),refT)
    r['LeftLeg_shift_vs_rightbind']=shift(np.array(r['LeftLeg_prof']),refS)
json.dump(res,open(OUT,'w'))
for r in res: print('P',r['tag'],'pelvis',r['pelvis'],'| L thigh patt vsPelvis',r['LeftUpLeg_patt_vsPelvis_out'],'vsFoot',r['LeftUpLeg_patt_vsFoot_out'],'shift',r['LeftUpLeg_shift_vs_rightbind'],'| L shin',r['LeftLeg_patt_vsPelvis_out'],r['LeftLeg_patt_vsFoot_out'],r['LeftLeg_shift_vs_rightbind'],'| Lfoot',r['Left_foot_vsPelvis_out'],'| R thigh',r['RightUpLeg_patt_vsPelvis_out'],r['RightUpLeg_patt_vsFoot_out'],'R shin',r['RightLeg_patt_vsPelvis_out'],'Rfoot',r['Right_foot_vsPelvis_out'])
