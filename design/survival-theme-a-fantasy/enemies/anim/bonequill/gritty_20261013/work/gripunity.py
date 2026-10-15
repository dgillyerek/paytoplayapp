"""Bone-space bow grip for Unity: grip pose relative to mixamorig:LeftHand (Blender bone space), the two hand-child bone heads
Unity uses to confirm axes/scale at runtime, and the centre of the closed hand around the grip axis per clip (rest, walk, attack)."""
import bpy,json,math,numpy as np
from mathutils import Vector,Matrix
W='/workspace/bonequill_gritty/work'; sc=bpy.context.scene; arm=bpy.data.objects['BONEQUILL_rig']; P='mixamorig:'; body=bpy.data.objects['BONEQUILL_body']
FI=json.load(open(W+'/fist.json')); GL=Vector(FI['grip_local']); RFIX=Matrix(((-1,0,0),(0,-1,0),(0,0,1)))
hb=arm.data.bones[P+'LeftHand']; Hinv=hb.matrix_local.inverted()
kids={n:list(Hinv@arm.data.bones[P+n].head_local) for n in ('LeftHandFingers1','LeftHandThumb1')}
ids=set(body.vertex_groups[P+n].index for n in ('LeftHand','LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2'))
hidx=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in ids)>0.3]
ax=RFIX@Vector((0,0,1)); u=Vector((1,0,0)); w=ax.cross(u)
out={}
for an in ('rest','walk','attack'):
    arm.animation_data.action=bpy.data.actions['BONEQUILL_'+an]; worst=0; cs=[]
    for k in range(61):
        f=k/2; sc.frame_set(int(f),subframe=f-int(f)); dg=bpy.context.evaluated_depsgraph_get()
        M=arm.matrix_world@arm.pose.bones[P+'LeftHand'].matrix; Mi=M.inverted()
        e=body.evaluated_get(dg); m=e.to_mesh(); V=[Mi@(body.matrix_world@m.vertices[i].co) for i in hidx]; e.to_mesh_clear()
        # closed-hand centre: per 10-degree bin around the grip axis, the innermost hand point; centre = mean of those ring points in the axis plane
        best={}
        for p in V:
            d=p-GL; a=d.dot(ax)
            if abs(a)>0.03: continue
            r=d-ax*a
            if r.length>0.06: continue
            b=int((math.degrees(math.atan2(r.dot(w),r.dot(u)))+360)%360//10)
            if b not in best or r.length<best[b].length: best[b]=r
        ring=np.array([list(v) for v in best.values()]); c=GL+Vector(ring.mean(0)); off=(c-GL).length
        worst=max(worst,off); cs.append([f]+[round(x,5) for x in c]+[round(off,5)])
    out[an]={'max_grip_to_hand_centre_m':round(worst,5),'per_sample':cs}
    print('GU',an,round(worst,4))
json.dump({'bone':P+'LeftHand','grip_pos_bone_m':list(GL),'grip_rot_bone_rows':[list(r) for r in RFIX],'grip_axis_bone':list(ax),
  'child_heads_bone_m':kids,'hand_centre':out},open(W+'/grip_unity.json','w'),indent=0)
