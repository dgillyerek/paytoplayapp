"""Bowstring clearance, every frame and half-frame of rest, walk and attack, with the per-frame bow roll (bowroll.py): the string
segment (bow-local y=0.19, z=+-0.62) vs the whole body mesh (incl. bow hand, fingers, forearm), the cloak, and the bow-arm bones.
Only the draw hand (RightHand and its fingers) is exempt, and only while it holds the string (attack f10-f17). Also records whether
the string is on the side away from the body (dot of string direction with grip-from-torso direction)."""
import sys; sys.path.insert(0,'/workspace/bonequill_gritty/work')
import bpy,json,math,numpy as np
from mathutils import Vector,Matrix
from mathutils.kdtree import KDTree
from bowroll import bowrot,roll,ROLL_REST,ROLL_DRAW
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P='mixamorig:'; body=bpy.data.objects['BONEQUILL_body']
with bpy.data.libraries.load(W+'/cape.blend') as (s,d): d.objects=['BONEQUILL_cape','BONEQUILL_cape_rig']
for o in d.objects: sc.collection.objects.link(o)
cape,crig=d.objects
for cn,bn in (('cape_Head','Head'),('cape_LeftArm','LeftArm'),('cape_RightArm','RightArm'),('cape_Spine2','Spine2'),('cape_LeftShoulder','LeftShoulder'),('cape_RightShoulder','RightShoulder')):
    c=crig.pose.bones[cn].constraints.new('COPY_TRANSFORMS'); c.target=arm; c.subtarget=P+bn
GL=Vector(json.load(open(W+'/fist.json'))['grip_local'])
rh=set(g.index for g in body.vertex_groups if g.name.startswith(P+'RightHand'))
isrh=np.array([sum(g.weight for g in v.groups if g.group in rh)>0.3 for v in body.data.vertices])
BN=('LeftForeArm','LeftHand','LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2')
def seg(Q,a,b):
    d=b-a; t=np.clip(((Q-a)@d)/(d@d),0,1); return np.linalg.norm(Q-(a+t[:,None]*d),axis=1).min()
out={}; worst={}
for an in ('rest','walk','attack'):
    arm.animation_data.action=bpy.data.actions['BONEQUILL_'+an]; per=[]
    for k in range(61):
        f=k/2; sc.frame_set(int(f),subframe=f-int(f)); dg=bpy.context.evaluated_depsgraph_get()
        M=arm.matrix_world@arm.pose.bones[P+'LeftHand'].matrix; R=M.to_3x3().normalized(); B=R@bowrot(an,f); g=M.to_translation()+R@GL
        a=np.array(g+B@Vector((0,0.19,-0.62))); b=np.array(g+B@Vector((0,0.19,0.62)))
        draw=an=='attack' and 10<=f<=17
        e=body.evaluated_get(dg); m=e.to_mesh(); V=np.array([(body.matrix_world@v.co)[:] for v in m.vertices]); e.to_mesh_clear()
        ec=cape.evaluated_get(dg); mc=ec.to_mesh(); C=np.array([(cape.matrix_world@v.co)[:] for v in mc.vertices]); ec.to_mesh_clear()
        keep=~isrh if draw else np.ones(len(V),bool)
        db=seg(V[keep],a,b); dc=seg(C,a,b)
        dbone=min(seg(np.array([np.array(arm.matrix_world@arm.pose.bones[P+n].head)+(np.array(arm.matrix_world@arm.pose.bones[P+n].tail)-np.array(arm.matrix_world@arm.pose.bones[P+n].head))*i/40 for i in range(41)]),a,b) for n in BN)
        tor=Vector(V[::50].mean(0)); tor.z=g.z
        away=(B@Vector((0,1,0))).dot((g-tor).normalized())
        per.append([f,round(float(db),4),round(float(dc),4),round(float(dbone),4),draw,round(roll(an,f),2),round(away,3)])
    out[an]=per; worst[an]={'min_body_m':min(r[1] for r in per),'min_cloak_m':min(r[2] for r in per),'min_arm_bones_m':min(r[3] for r in per),
        'min_away_rest_carry':min((r[6] for r in per if r[5]==ROLL_REST),default=None)}
json.dump({'roll_rest_deg':ROLL_REST,'roll_draw_deg':ROLL_DRAW,'roll_keys':[4,10,17,24],'string_bow_local':[[0,0.19,-0.62],[0,0.19,0.62]],
  'draw_hand_exempt':'RightHand* vertices, attack f10-f17','columns':'frame, body incl. bow hand+forearm, cloak, bow-arm bones, draw, roll_deg, away',
  'worst':worst,'per_sample':out},open(W+'/string_clearance.json','w'),indent=0)
print('STRINGCLR',json.dumps(worst))
