"""Rowan bone-space bow grip for Unity (same as Bonequill #63 gripunity.py): grip pose relative to mixamorig:LeftHand (Blender bone space),
the two hand-child bone heads Unity uses to confirm axes/scale at runtime, and the centre of the closed hand around the grip axis on
every frame and half-frame of rest, walk and attack."""
import sys; sys.path.append("/home/box/.local/lib/python3.13/site-packages")
import bpy,json,math,numpy as np
from mathutils import Vector,Matrix
W='/workspace/rowan_fresh/w2'; P='mixamorig:'
bpy.ops.wm.open_mainfile(filepath=W+'/anim.blend')
sc=bpy.context.scene; arm=bpy.data.objects['ROWAN_rig']; body=bpy.data.objects['ROWAN_body']
FI=json.load(open(W+'/grip.json')); GL=Vector(FI['grip_local']); RFIX=Matrix(FI['grip_rot_local'])
hb=arm.data.bones[P+'LeftHand']; Hinv=hb.matrix_local.inverted()
kids={n:list(Hinv@arm.data.bones[P+n].head_local) for n in ('LeftHandFingers1','LeftHandThumb1')}
ids=set(body.vertex_groups[P+n].index for n in ('LeftHand','LeftHandFingers1','LeftHandFingers2','LeftHandFingers3','LeftHandThumb1','LeftHandThumb2'))
hidx=[v.index for v in body.data.vertices if sum(g.weight for g in v.groups if g.group in ids)>0.3]
ax=RFIX@Vector((0,0,1)); u=ax.orthogonal().normalized(); w=ax.cross(u)
out={}
for an in ('rest','walk','attack'):
    arm.animation_data.action=bpy.data.actions['ROWAN_'+an]; worst=0; cs=[]
    for k in range(61):
        f=k/2; sc.frame_set(int(f),subframe=f-int(f)); dg=bpy.context.evaluated_depsgraph_get()
        M=arm.matrix_world@arm.pose.bones[P+'LeftHand'].matrix; Mi=M.inverted()
        e=body.evaluated_get(dg); m=e.to_mesh(); V=[Mi@(body.matrix_world@m.vertices[i].co) for i in hidx]; e.to_mesh_clear()
        best={}
        for p in V:
            d=p-GL; a=d.dot(ax)
            if abs(a)>0.03: continue
            r=d-ax*a
            if r.length>0.06: continue
            b=int((math.degrees(math.atan2(r.dot(w),r.dot(u)))+360)%360//10)
            if b not in best or r.length<best[b].length: best[b]=r
        ring=np.array([list(v) for v in best.values()])
        # centre of the circle the closed hand wraps (least-squares circle fit in the plane normal to the grip axis); a plain mean of a
        # 270-deg wrap is pulled toward the closed side, so it is recorded too but not used
        X=np.c_[ring@np.array(u),ring@np.array(w)]; A_=np.c_[2*X,np.ones(len(X))]; sol=np.linalg.lstsq(A_,(X**2).sum(1),rcond=None)[0]
        c=GL+u*float(sol[0])+w*float(sol[1]); off=(c-GL).length; rad=math.sqrt(max(sol[2]+sol[0]**2+sol[1]**2,0)); cm=GL+Vector(ring.mean(0))
        worst=max(worst,off); cs.append([f]+[round(x,5) for x in c]+[round(off,5),round(rad,5),round((cm-GL).length,5)])
    out[an]={'max_grip_to_hand_centre_m':round(worst,5),'bins':len(best),'per_sample':cs}
    print('GU',an,round(worst,4),cs[0],flush=True)
mean=np.array([s[1:4] for v in out.values() for s in v['per_sample']]).mean(0)
json.dump({'bone':P+'LeftHand','grip_pos_bone_m':[round(x,5) for x in GL],'grip_rot_bone_rows':[list(r) for r in RFIX],'grip_axis_bone':list(ax),
  'child_heads_bone_m':{k:[round(x,5) for x in v] for k,v in kids.items()},'hand_centre_mean':[round(float(x),5) for x in mean],'hand_centre':out},open(W+'/bowgrip/grip_unity.json','w'),indent=0)
