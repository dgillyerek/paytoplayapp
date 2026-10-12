import bpy,sys,math,json,numpy as np
from mathutils import kdtree, Vector
A_=sys.argv[sys.argv.index('--')+1:]; OUT=A_[0]; BLENDTAG=A_[1] if len(A_)>1 else ''
exec(open('meas.py').read().split("# bind pose selection")[0].replace("OUT=sys.argv[sys.argv.index('--')+1]; ACT=","ACT=").replace("ACT=sys.argv[sys.argv.index('--')+2] if len(sys.argv)>sys.argv.index('--')+2 else 'LYRA_tighten_attack'","ACT='x'"))
def symyaw(Q):
    if len(Q)>6000: Q=Q[np.random.RandomState(0).choice(len(Q),6000,replace=False)]
    kd=kdtree.KDTree(len(Q))
    for i,p in enumerate(Q): kd.insert(Vector(p),i)
    kd.balance(); c=Q.mean(0); best=None
    for th in np.arange(-40,40.1,1.0):
        t=math.radians(th); n=np.array([math.cos(t),math.sin(t),0])
        M=Q-2*np.outer((Q-c)@n,n)
        d=np.mean([kd.find(Vector(p))[2] for p in M[::3]])
        if best is None or d<best[0]: best=(d,th)
    # refine
    return best[1], round(best[0]*1000,2)
def meas(tag):
    V,_=evalV(); pb=arm.pose.bones; r={'tag':tag}
    for reg,bones in (('pelvis',['Hips']),('chest',['Spine1','Spine2']),('head',['Head','Neck']),('torso',['Hips','Spine','Spine1','Spine2'])):
        m=np.isin(dom,bones); r[reg]=symyaw(V[m])
    L=pb[P+'LeftArm'].head; R=pb[P+'RightArm'].head; d=L-R; r['shoulder_line']=round(-math.degrees(math.atan2(-d.y,d.x)),1)
    L=pb[P+'LeftUpLeg'].head; R=pb[P+'RightUpLeg'].head; d=L-R; r['hip_line']=round(-math.degrees(math.atan2(-d.y,d.x)),1)
    for s in ('Left','Right'):
        ft=pb[P+s+'Foot']; dd_=ft.tail-ft.head; r[s+'foot']=round(math.degrees(math.atan2(dd_.x,-dd_.y)),1)
    return r
res=[]
arm.data.pose_position='REST'; sc.frame_set(0); res.append(meas('bind'))
arm.data.pose_position='POSE'
for t in ad.nla_tracks: t.mute=True
for a,f in (('LYRA_tighten_rest',0),('LYRA_tighten_walk',0),('LYRA_tighten_walk',15),('LYRA_tighten_attack',0)):
    ad.action=bpy.data.actions[a]; sc.frame_set(f); res.append(meas(a.split('_')[-1]+f'_f{f}'))
json.dump(res,open(OUT,'w'))
for r in res: print('SYM',json.dumps(r))
