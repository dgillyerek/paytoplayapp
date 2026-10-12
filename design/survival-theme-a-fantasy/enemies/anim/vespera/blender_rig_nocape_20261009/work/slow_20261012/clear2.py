"""Per-frame clearance of BOTH forearm/hand pieces against the rest of the body (2026-10-11 orb pass).
Right piece = the freed 'armside' piece (as clear.py, hand + forearm beyond 5% of the forearm); left piece = vertices whose
dominant bone is LeftForeArm/LeftHand beyond 5% of the forearm. Body = faces with no arm-dominant vertex, no hair, no lining twin.
Also reports hand-vs-hand overlaps and the min hand-to-hair distance."""
import bpy, numpy as np, json, sys
from mathutils.bvhtree import BVHTree
args=sys.argv[sys.argv.index('--')+1:]; ACT=args[0]; OUT=args[1]
o=bpy.data.objects['VESPERA_body']; me=o.data; a=bpy.data.objects['VESPERA_rig']; sc=bpy.context.scene
N=len(me.vertices)
AS=np.zeros(N,np.int32); me.attributes['armside'].data.foreach_get('value',AS); AS=AS.astype(bool)
TW=np.zeros(N,np.int32); me.attributes['is_twin'].data.foreach_get('value',TW); TW=TW.astype(bool)
m=np.load('/workspace/vespera-hand/work/mesh.npz'); hair=m['hair']; assert len(hair)==N,(len(hair),N)
names=[g.name.split(':')[1] for g in o.vertex_groups]; W=np.zeros((N,len(names)))
for v in me.vertices:
    for g in v.groups: W[v.index,g.group]=g.weight
dom=np.array(names)[W.argmax(1)]
armdom=np.isin(dom,['RightArm','RightForeArm','RightHand','LeftArm','LeftForeArm','LeftHand'])
B={k:[np.array(x) for x in v] for k,v in json.load(open('/workspace/vespera-hand/work/bones.json')).items()}
co=np.array([v.co[:] for v in me.vertices])
def tpar(b):
    p,q=B[b]; return ((co-p)@(q-p))/((q-p)@(q-p))
tR=tpar('RightForeArm'); tL=tpar('LeftForeArm')
F=[list(p.vertices) for p in me.polygons]
body_f=[f for f in F if not (AS[f].any() or armdom[f].any() or TW[f].all() or hair[f].any())]
hair_f=[f for f in F if hair[f].all() and not (AS[f].any() or armdom[f].any())]
pieces={}
pr=AS&(tR>0.05); pl=np.isin(dom,['LeftForeArm','LeftHand'])&(tL>0.05)&~AS
for nm,pm,t in (('R',pr,tR),('L',pl,tL)):
    hf=[f for f in F if pm[f].all() and not TW[f].all()]
    hv=np.nonzero(pm&~TW)[0]; lf=[f for f in F if (pm&(t>0.45))[f].all() and not TW[f].all()]; pieces[nm]=(hf,hv,t[hv]>0.45,lf)
def coords():
    dg=bpy.context.evaluated_depsgraph_get(); ob=o.evaluated_get(dg); mm=ob.to_mesh(); c=np.empty(N*3); mm.vertices.foreach_get('co',c); ob.to_mesh_clear(); return c.reshape(-1,3)
a.animation_data.action=bpy.data.actions[ACT]; rep=[]
for f in range(int(bpy.data.actions[ACT].frame_range[1])+1):
    sc.frame_set(f); c=coords(); cl=c.tolist()
    Bb=BVHTree.FromPolygons(cl,body_f); Bhair=BVHTree.FromPolygons(cl,hair_f)
    r=dict(f=f); BH={}
    for nm,(hf,hv,low,lf) in pieces.items():
        Bh=BVHTree.FromPolygons(cl,hf); BH[nm]=Bh
        ov=Bh.overlap(Bb); ovh=Bh.overlap(Bhair); ovl=BVHTree.FromPolygons(cl,lf).overlap(Bb)
        d=np.array([Bb.find_nearest(c[i])[3] for i in hv]); dh=np.array([(Bhair.find_nearest(c[i])[3] or 9) for i in hv])
        r[nm]=dict(body_tri_overlaps=len(ov),hand_lowforearm_body_overlaps=len(ovl),hair_tri_overlaps=len(ovh),min_body_cm=round(float(d.min())*100,1),
                   min_body_hand_lowforearm_cm=round(float(d[low].min())*100,1),min_hair_hand_lowforearm_cm=round(float(dh[low].min())*100,1))
    r['hand_hand_overlaps']=len(BH['R'].overlap(BH['L']))
    rep.append(r)
json.dump(rep,open(OUT,'w'),indent=0)
for r in rep: print('CLR',json.dumps(r))
