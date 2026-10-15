"""Bowstring clearance from the bow hand: the string segment (bow-local y=0.19, z=+-0.62) vs every left hand, finger, thumb and
forearm vertex and the bone segments, every frame and half-frame of rest, walk and attack. Attack f4-f17 is the draw (the right hand
holds the string) and is reported but exempt."""
import bpy,json,math,numpy as np
from mathutils import Vector,Matrix
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P='mixamorig:'; body=bpy.data.objects['BONEQUILL_body']
GL=Vector(json.load(open(W+'/fist.json'))['grip_local']); ROLL=45; oR=Matrix(((-1,0,0),(0,-1,0),(0,0,1)))@Matrix.Rotation(math.radians(ROLL),3,'Z')
BN=('LeftForeArm','LeftHand','LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2')
ids=set(body.vertex_groups[P+n].index for n in BN)
hidx=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in ids)>0.3]
def seg(P_,a,b):
    d=b-a; t=np.clip(((P_-a)@d)/(d@d),0,1); return np.linalg.norm(P_-(a+t[:,None]*d),axis=1).min()
def segseg(a,b,c,d,n=40):
    Q=np.array([c+(d-c)*i/n for i in range(n+1)]); return seg(Q,a,b)
out={}; worst={}
for an in ('rest','walk','attack'):
    arm.animation_data.action=bpy.data.actions['BONEQUILL_'+an]; per=[]
    for k in range(61):
        f=k/2; sc.frame_set(int(f),subframe=f-int(f)); dg=bpy.context.evaluated_depsgraph_get()
        M=arm.matrix_world@arm.pose.bones[P+'LeftHand'].matrix; R=M.to_3x3().normalized(); B=R@oR; g=M.to_translation()+R@GL
        a=np.array(g+B@Vector((0,0.19,-0.62))); b=np.array(g+B@Vector((0,0.19,0.62)))
        e=body.evaluated_get(dg); m=e.to_mesh(); V=np.array([(body.matrix_world@m.vertices[i].co)[:] for i in hidx]); e.to_mesh_clear()
        dm=seg(V,a,b)
        db=min(segseg(a,b,np.array(arm.matrix_world@arm.pose.bones[P+n].head),np.array(arm.matrix_world@arm.pose.bones[P+n].tail)) for n in BN)
        draw=an=='attack' and 4<=f<=17
        per.append([f,round(float(dm),4),round(float(db),4),draw])
    out[an]=per; worst[an]={'min_mesh_m':min(r[1] for r in per if not r[3]),'min_bone_m':min(r[2] for r in per if not r[3])}
json.dump({'bow_roll_deg':ROLL,'string_bow_local':[[0,0.19,-0.62],[0,0.19,0.62]],'draw_exempt':'attack f4-f17','worst':worst,'per_sample':out},open(W+'/string_clearance.json','w'),indent=0)
print('STRINGCLR',json.dumps(worst))
