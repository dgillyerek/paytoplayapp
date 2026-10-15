"""Exact bowstring clearance with the picked roll curve (bowroll.py, roll_pick.json from rollsearch.py) for Rowan's bow: per frame/half-frame and candidate roll (10 deg steps), distance from the
string segment (bow-local y=0.13, z=+-0.66) to the body mesh (v5c: the bow forearm + bow hand are exempt and reported separately as fa_skin -- the carried string lies against the inner forearm; RightHand* exempt only while it holds the
string, attack f8-f14), the coat tails (rigid on Hips, as pen.py) and the bow-arm bones, plus the away-from-body sign."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages"); sys.path.insert(0,'/workspace/rowan_fresh/w2/bowgrip')
import bpy,json,math,numpy as np
from mathutils import Vector,Matrix
W='/workspace/rowan_fresh/w2'; P='mixamorig:'
bpy.ops.wm.open_mainfile(filepath=W+'/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; body=bpy.data.objects['ROWAN_body']; coat=bpy.data.objects['ROWAN_coat']; crig=bpy.data.objects['ROWAN_coat_rig']
if crig.animation_data: crig.animation_data.action=None
for pb in crig.pose.bones: pb.rotation_mode='QUATERNION'; pb.rotation_quaternion=(1,0,0,0); pb.location=(0,0,0)
GL=Vector(json.load(open(W+'/grip.json'))['grip_local']); RFIX=Matrix(((-1,0,0),(0,-1,0),(0,0,1)))
rh=set(g.index for g in body.vertex_groups if g.name.startswith(P+'RightHand'))
isrh=np.array([sum(g.weight for g in v.groups if g.group in rh)>0.3 for v in body.data.vertices])
lf=set(g.index for g in body.vertex_groups if g.name.startswith(P+'LeftHand') or g.name==P+'LeftForeArm')
islf=np.array([sum(g.weight for g in v.groups if g.group in lf)>0.3 for v in body.data.vertices])
BN=('LeftArm','LeftForeArm','LeftHand','LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2')
def segd(Q,a,b):
    d=b-a; t=np.clip(((Q-a)@d)/(d@d),0,1); return np.linalg.norm(Q-(a+t[:,None]*d),axis=1).min()
def evalco(o):
    dg=bpy.context.evaluated_depsgraph_get(); e=o.evaluated_get(dg); m=e.to_mesh(); Q=np.empty(len(m.vertices)*3); m.vertices.foreach_get('co',Q); e.to_mesh_clear()
    M=np.array(o.matrix_world); Q=Q.reshape(-1,3); return Q@M[:3,:3].T+M[:3,3]
ROLLS=list(range(0,360,10)); out={}
for an in ('rest','walk','attack'):
    arm.animation_data.action=bpy.data.actions['ROWAN_'+an]; per=[]
    for k in range(61):
        f=k/2; sc.frame_set(int(f),subframe=f-int(f))
        crig.pose.bones['coat_Hips'].matrix=arm.pose.bones[P+'Hips'].matrix.copy(); bpy.context.view_layer.update()
        M=arm.matrix_world@arm.pose.bones[P+'LeftHand'].matrix; R=M.to_3x3().normalized(); g=M.to_translation()+R@GL
        V=evalco(body); C=evalco(coat); draw=an=='attack' and 8<=f<=14
        Vf=V[islf]; Vk=V[~isrh&~islf] if draw else V[~islf]; Vk=Vk[np.linalg.norm(Vk-np.array(g),axis=1)<0.9]
        bones=np.vstack([np.array(arm.matrix_world@arm.pose.bones[P+n].head)+(np.array(arm.matrix_world@arm.pose.bones[P+n].tail)-np.array(arm.matrix_world@arm.pose.bones[P+n].head))*i/40 for n in BN for i in range(41)])
        tor=Vector(V[::50].mean(0)); tor.z=g.z
        from bowroll import bowrot,roll
        if True:
            r=roll(an,f); B=R@bowrot(an,f)
            a=np.array(g+B@Vector((0,0.13,-0.66))); b=np.array(g+B@Vector((0,0.13,0.66)))
            away=(B@Vector((0,1,0))).dot((g-tor).normalized())
            per.append([f,round(float(segd(Vk,a,b)),4),round(float(segd(C,a,b)),4),round(float(segd(bones,a,b)),4),draw,round(r,2),round(away,3),round(float(segd(Vf,a,b)),4)])
    out[an]=per; print('done',an,flush=True)
from bowroll import ROLL_REST,ROLL_DRAW,F_A,F_B,F_C,F_E
worst={an:{'min_body_m':min(r[1] for r in p),'min_coat_m':min(r[2] for r in p),'min_arm_bones_m':min(r[3] for r in p)} for an,p in out.items()}
json.dump({'roll_rest_deg':ROLL_REST,'roll_draw_deg':ROLL_DRAW,'roll_keys':[F_A,F_B,F_C,F_E],'string_bow_local':[[0,0.13,-0.66],[0,0.13,0.66]],
  'draw_hand_exempt':'RightHand* vertices, attack f8-f14 (nock to release)','columns':'frame, body excl. bow hand+forearm, coat tails, bow-arm bones, draw, roll_deg, away, fa_skin (string to bow forearm/hand skin vertices)',
  'worst':worst,'per_sample':out},open(W+'/bowgrip/string_clearance.json','w'),indent=0)
print('STRINGCLR',json.dumps(worst))
