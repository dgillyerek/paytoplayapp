"""Author a more legible, full-body Kaelen attack pair and export review FBXs."""
import os, sys, json, math
import bpy
from mathutils import Vector, Quaternion, Matrix
ROOT="C:/Development/paytoplayapp"
FIXDIR=ROOT+"/design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix10"
FIX9=ROOT+"/design/survival-theme-a-fantasy/new_2026-11-03/kaelen/fix9"
os.makedirs(FIXDIR+"/fbx",exist_ok=True); sys.path.insert(0,FIX9)
import k9_fix as ik
import k9_present as present
FWD=Vector((0,-1,0)); UP=Vector((0,0,1))
ARM=bpy.data.objects["KAELEN_rig"]; BODY=bpy.data.objects["KAELEN_body"]
SWORDS={n:bpy.data.objects[n] for n in ("KAELEN_sword_L","KAELEN_sword_R")}

def smooth(t):
    t=max(0.0,min(1.0,t)); return t*t*(3-2*t)
def curve(f, pts):
    if f<=pts[0][0]: return pts[0][1]
    if f>=pts[-1][0]: return pts[-1][1]
    for (a,x),(b,y) in zip(pts,pts[1:]):
        if a<=f<=b: return x+(y-x)*smooth((f-a)/(b-a))
    return pts[-1][1]
def key(pb,f):
    pb.keyframe_insert(data_path="location",frame=f); pb.keyframe_insert(data_path="rotation_quaternion",frame=f)
def rotate_world(pb,axis,degrees):
    if abs(degrees)<1e-5:return
    M=pb.matrix.copy(); head=M.to_translation()
    ax=(ARM.matrix_world.inverted().to_3x3()@axis).normalized()
    T=Matrix.Translation(head); q=Quaternion(ax,math.radians(degrees))
    pb.matrix=T@q.to_matrix().to_4x4()@T.inverted()@M
def assign(a): ik.assign_slot(ARM,a)

def body_motion(kind,f):
    hips=ARM.pose.bones["mixamorig:Hips"]; sp=ARM.pose.bones["mixamorig:Spine"]
    sp1=ARM.pose.bones["mixamorig:Spine1"]; sp2=ARM.pose.bones["mixamorig:Spine2"]
    if kind=="cross":
        hy=curve(f,[(1,0),(6,-8),(12,5),(18,7),(23,2),(30,0),(48,0)])
        sy=curve(f,[(1,0),(6,8),(12,-4),(18,-7),(23,-3),(30,0),(48,0)])
        pitch=curve(f,[(1,0),(6,-2),(12,5),(17,9),(21,5),(30,0),(48,0)])
        forward=curve(f,[(1,0),(6,0),(14,.055),(18,.06),(24,.025),(32,0),(48,0)])
        dip=curve(f,[(1,0),(8,0),(15,-.035),(18,-.035),(25,0),(48,0)])
    else:
        hy=curve(f,[(1,0),(6,-8),(13,8),(19,3),(24,-7),(30,8),(36,3),(43,0),(58,0)])
        sy=curve(f,[(1,0),(6,8),(13,-7),(19,-3),(24,7),(30,-8),(36,-3),(43,0),(58,0)])
        pitch=curve(f,[(1,0),(6,-2),(13,7),(19,4),(24,-2),(30,7),(36,4),(43,0),(58,0)])
        forward=curve(f,[(1,0),(8,0),(14,.025),(20,.01),(27,.025),(33,.01),(40,0),(58,0)])
        dip=curve(f,[(1,0),(8,0),(14,-.025),(20,-.01),(27,-.025),(34,-.01),(42,0),(58,0)])
    rotate_world(hips,UP,hy)
    rotate_world(sp,UP,-sy*.30); rotate_world(sp1,UP,-sy*.36); rotate_world(sp2,UP,-sy*.22)
    rotate_world(sp,Vector((1,0,0)),pitch*.30); rotate_world(sp1,Vector((1,0,0)),pitch*.40); rotate_world(sp2,Vector((1,0,0)),pitch*.30)
    hips.matrix.translation += ARM.matrix_world.inverted().to_3x3()@(FWD*forward+UP*dip)
    for pb in (hips,sp,sp1,sp2): key(pb,f)

def guard_data(orig):
    off={}; direc={}
    for s in ("R","L"):
        shoulder=ARM.matrix_world @ ARM.pose.bones[ik.SIDES[s]["upper"]].head
        off[s]=orig[0][f"{s}_hand"]-shoulder
        direc[s]=orig[0][f"{s}_dir"].normalized()
    return off,direc

def dual_targets(f, shoulders, off, gd):
    out={}
    for s in ("R","L"):
        lat=ik.SIDES[s]["lateral"]; S=shoulders[s]; guard=S+off[s]
        ra,peak,hit,rec=(3,9,16,24) if s=="R" else (17,24,31,40)
        high=S+lat*.12+FWD*.03+UP*.34
        low=S+lat*.08+FWD*.36+UP*(-.31)
        hdir=(lat*.12+FWD*.35+UP*.93).normalized()
        cdir=(-lat*.20+FWD*.48-UP*.75).normalized()
        if f<ra: hand,d=guard,gd[s]
        elif f<=peak:
            t=smooth((f-ra)/(peak-ra)); hand=guard.lerp(high,t); d=gd[s].lerp(hdir,t).normalized()
        elif f<=hit:
            t=smooth((f-peak)/(hit-peak)); hand=high.lerp(low,t); d=hdir.lerp(cdir,t).normalized()
        elif f<=rec:
            t=smooth((f-hit)/(rec-hit)); hand=low.lerp(guard,t); d=cdir.lerp(gd[s],t).normalized()
        else: hand,d=guard,gd[s]
        out[s]=(hand,d)
    return out

def cross_targets(f,shoulders,off,gd):
    out={}
    if f<=7: phase="draw"; t=smooth((f-1)/6)
    elif f<=17: phase="drive"; t=smooth((f-7)/10)
    elif f<=20: phase="impact"; t=1
    elif f<=34: phase="recover"; t=smooth((f-20)/14)
    else: phase="settle"; t=smooth((f-34)/14)
    for s in ("R","L"):
        lat=ik.SIDES[s]["lateral"]; S=shoulders[s]; guard=S+off[s]
        hit=S+lat*.07+FWD*.40+UP*(-.18); draw=guard-FWD*.07+UP*.025
        fwd=(FWD*.97+lat*(-.10 if s=="L" else .10)+UP*(.07 if s=="L" else -.07)).normalized()
        if phase=="draw": hand=guard.lerp(draw,t); d=gd[s].lerp((FWD*.5+UP*.45+lat*.1).normalized(),t).normalized()
        elif phase=="drive": hand=draw.lerp(hit,t); d=gd[s].lerp(fwd,t).normalized()
        elif phase=="impact": hand,d=hit,fwd
        elif phase=="recover": hand=hit.lerp(guard,t); d=fwd.lerp(gd[s],t).normalized()
        else: hand,d=guard,gd[s]
        out[s]=(hand,d)
    return out

def solve_clearance(targets,lengths):
    """Solve each arm and iteratively move only the offending blade lane away."""
    saved_prev=dict(ik.PREV_TWIST)
    current=targets
    for attempt in range(12):
        ik.PREV_TWIST.clear(); ik.PREV_TWIST.update(saved_prev)
        for s in ("R","L"):
            ik.solve_side(ARM,s,current[s][0],current[s][1],*lengths[s])
        bpy.context.view_layer.update()
        col=ik.collision_frame(ARM,BODY,SWORDS,None)
        body_ok=(col["swordL_body_tris"]==0 and col["swordR_body_tris"]==0 and col["swordL_body_cm"]>=3.0 and col["swordR_body_cm"]>=3.0)
        pair_ok=(col["sword_sword_tris"]==0 and col["sword_sword_cm"]>=3.0)
        if body_ok and pair_ok: return current,col
        moved={}
        for s in ("R","L"):
            hand,direction=current[s]; lat=ik.SIDES[s]["lateral"]
            push=Vector((0,0,0)); turn=Vector((0,0,0))
            if col[f"sword{s}_body_tris"] or col[f"sword{s}_body_cm"]<3.0:
                push += lat*.035 + FWD*.06 + UP*.025
                turn += lat*.15 + FWD*.30 + UP*.10
            if not pair_ok:
                push += lat*.025 + FWD*.02; turn += lat*.20
            moved[s]=(hand+push,(direction+turn).normalized())
        current=moved
    return current,col


def author(name,kind):
    action=bpy.data.actions[name]; f0=int(action.frame_range[0]); f1=int(action.frame_range[1])
    orig=ik.snapshot_original(ARM,action,f0,f1)
    assign(action); bpy.context.scene.frame_set(f0); bpy.context.view_layer.update()
    off,gd=guard_data(orig); lengths=ik.bone_lengths(ARM)
    assign(action); bpy.context.scene.frame_set(f0); bpy.context.view_layer.update()
    ik.GUARD_TWIST.clear(); ik.PREV_TWIST.clear()
    for spec in ik.SIDES.values():
        for role in ("upper","fore"):
            pb=ARM.pose.bones[spec[role]]; _,_,ang=ik.swing_twist(pb.rotation_quaternion)
            ik.GUARD_TWIST[pb.name]=ang; ik.PREV_TWIST[pb.name]=ang
    if kind=="dual": ik.ensure_lift_sign(ARM)
    edited=["mixamorig:Hips","mixamorig:Spine","mixamorig:Spine1","mixamorig:Spine2"]
    for spec in ik.SIDES.values(): edited += [spec["upper"],spec["fore"],spec["shoulder"]]
    rows=[]
    for f in range(f0,f1+1):
        bpy.context.scene.frame_set(f); bpy.context.view_layer.update(); body_motion(kind,f)
        if kind=="dual":
            for s,spec in ik.SIDES.items():
                t=smooth((f-3)/6)*(1-smooth((f-9)/7)) if s=="R" else smooth((f-17)/6)*(1-smooth((f-24)/7))
                pb=ARM.pose.bones[spec["shoulder"]]
                if t>.001: ik.lift_clavicle(ARM,pb,.28*t*ik.LIFT_SIGN.get(s,1))
                pb.keyframe_insert(data_path="rotation_quaternion",frame=f)
        shoulders=ik.live_shoulders(ARM)
        targets=dual_targets(f,shoulders,off,gd) if kind=="dual" else cross_targets(f,shoulders,off,gd)
        targets,col=solve_clearance(targets,lengths)
        for s in ("R","L"):
            for role in ("upper","fore"):
                ARM.pose.bones[ik.SIDES[s][role]].keyframe_insert(data_path="rotation_quaternion",frame=f)
        col["frame"]=f; rows.append(col)
    ik.continuous_quats(ARM,action,edited,f0,f1)
    overlaps=[r for r in rows if r["swordL_body_tris"] or r["swordR_body_tris"] or r["sword_sword_tris"]]
    result={"action":name,"kind":kind,"frames":f1-f0+1,"overlap_frames":[r["frame"] for r in overlaps],"min_clearance_cm":{k:min(r[k] for r in rows) for k in ("swordL_body_cm","swordR_body_cm","sword_sword_cm")}}
    print("MOTION_RESULT",json.dumps(result))
    if overlaps: raise RuntimeError(f"Geometry overlap in {name}: {overlaps[:4]}")
    return result

def main():
    results=[author("attack_dualslash","dual"),author("attack2_crossthrust","cross")]
    outblend=FIXDIR+"/KAELEN_fix10.blend"; bpy.ops.wm.save_as_mainfile(filepath=outblend,copy=True)
    refs={}
    for clip in ("attack_dualslash","attack2_crossthrust"):
        refs[clip]=present.sample_poses(clip)
        present.export_clip(clip,FIXDIR+"/fbx/KAELEN_"+clip+".fbx")
    json.dump(results,open(FIXDIR+"/motion_report.json","w"),indent=2)
    roundtrip={clip:present.compare_reimport(FIXDIR+"/fbx/KAELEN_"+clip+".fbx",refs[clip],clip) for clip in ("attack_dualslash","attack2_crossthrust")}
    json.dump(roundtrip,open(FIXDIR+"/roundtrip.json","w"),indent=2,default=str); print("ROUNDTRIP",json.dumps(roundtrip))
    if any(v.get("max_length_err_m",1)>.001 for v in roundtrip.values()): raise RuntimeError("FBX round-trip >1 mm arm-length error")
    print("AUTHOR_DONE",outblend)
if __name__=="__main__": main()
