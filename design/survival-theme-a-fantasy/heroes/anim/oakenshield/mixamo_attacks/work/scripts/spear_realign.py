"""Attack 2 spear realign (prop only): the thorn spear rides RightHand at her locked attack-1 grip; between the keys it turns IN the grip
(about the grip point) and slides forward through the fist by slide_m from the locked orientation to the two-hand line: point leading, shaft from the grip through the left palm
(LeftHand + 7 cm along ForeArm->Hand). Minimal rotation, slerped by smoothstep weight. Body untouched. QC: body overlap (minus the
hands), tip direction, left palm distance from the shaft."""
import bpy,json,math,sys
from mathutils import Matrix,Vector,Quaternion
from mathutils.bvhtree import BVHTree
PRM=json.load(open(sys.argv[sys.argv.index('--')+1]))
P='mixamorig:'; a=bpy.data.objects['OAKENSHIELD_rig']; body=bpy.data.objects['OAKENSHIELD_body']; sc=bpy.context.scene; AW=a.matrix_world
hp=bpy.data.objects['QC_held']; HL=Matrix(PRM['local']); K=PRM['keys']; PAL=PRM.get('palm_m',0.07); TIP=Vector((0,-1,0))
def sm(t): t=min(1,max(0,t)); return t*t*(3-2*t)
def wt(f):
    s,fu,ho,e=K
    if f<=s or f>=e: return 0.0
    if f<fu: return sm((f-s)/(fu-s))
    if f<=ho: return 1.0
    return 1-sm((f-ho)/(e-ho))
def bvh(o):
    dg=bpy.context.evaluated_depsgraph_get(); oe=o.evaluated_get(dg); me=oe.to_mesh()
    vs=[oe.matrix_world@v.co for v in me.vertices]; ps=[tuple(p.vertices) for p in me.polygons]; oe.to_mesh_clear(); return BVHTree.FromPolygons(vs,ps)
HG={g.index for g in body.vertex_groups if any(k in g.name for k in PRM.get('exclude_groups',['RightHand','LeftHand']))}
hv={v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in HG)>0.3}
hpoly={p.index for p in body.data.polygons if any(i in hv for i in p.vertices)}
N=sc.frame_end+1; out=[]
hp.animation_data_clear()
for f in range(N):
    sc.frame_set(f); g=lambda n:AW@a.pose.bones[P+n].head
    M0=AW@a.pose.bones[P+'RightHand'].matrix@HL; grip=M0.translation
    LH=g('LeftHand'); palm=LH+PAL*(LH-g('LeftForeArm')).normalized()
    d0=(M0.to_3x3()@TIP).normalized(); want=(palm-grip).normalized()
    w=wt(f); R=d0.rotation_difference(want); Rw=Quaternion().slerp(R,w)
    M=Matrix.Translation(grip)@Rw.to_matrix().to_4x4()@Matrix.Translation(-grip)@M0
    dl=(M.to_3x3()@TIP).normalized(); M=Matrix.Translation(dl*PRM.get('slide_m',0.0)*w)@M
    hp.matrix_world=M; hp.rotation_mode='QUATERNION'; hp.keyframe_insert('location',frame=f); hp.keyframe_insert('rotation_quaternion',frame=f)
    hp.hide_render=False; hp.keyframe_insert('hide_render',frame=f)
    d=(M.to_3x3()@TIP).normalized(); hl=palm-grip; perp=(hl-d*hl.dot(d)).length
    bpy.context.view_layer.update()
    pr=bvh(body).overlap(bvh(hp)); bad=len([1 for x,y in pr if x not in hpoly])
    out.append({'f':f,'w':round(w,3),'tip_dir':[round(x,3) for x in d],'yaw_off_target_deg':round(math.degrees(math.atan2(d.x,-d.y)),1),'pitch_deg':round(math.degrees(math.asin(max(-1,min(1,d.z)))),1),
        'turn_in_grip_deg':round(math.degrees(R.angle)*w,1),'palm_off_shaft_m':round(perp,3),'tip':[round(x,3) for x in M@Vector((0,-0.775,0))],'overlap_not_hands':bad,'slide_m':round(PRM.get('slide_m',0.0)*w,3),'butt_behind_grip_m':round(0.626-PRM.get('slide_m',0.0)*w,3)})
json.dump(out,open(PRM['qc_out'],'w'),indent=1)
bpy.ops.wm.save_as_mainfile(filepath=PRM['blend_out'],compress=True)
for o in out[::2]: print('S',o['f'],o['w'],o['yaw_off_target_deg'],o['pitch_deg'],o['turn_in_grip_deg'],o['palm_off_shaft_m'],o['overlap_not_hands'])
