"""Acceptance checks for the Kaelen motion repair. Run in Blender on KAELEN.blend."""
import bpy
import hashlib
from mathutils import Vector

ROOT = "C:/Development/paytoplayapp"
PREVIEW = ROOT + "/Assets/ThemePack/fantasy_kingdom_a/art/preview_new_chars/kaelen"
FINAL = ROOT + "/design/survival-theme-a-fantasy/heroes/anim/kaelen/final"
FWD = Vector((0.0, -1.0, 0.0))
rig = bpy.data.objects["KAELEN_rig"]

def assign(action):
    rig.animation_data.action = action
    rig.animation_data.action_slot = action.slots[0]
    for track in rig.animation_data.nla_tracks:
        track.mute = True

def world_hand(side):
    bone = rig.pose.bones[f'mixamorig:{"Left" if side == "L" else "Right"}Arm.002']
    return rig.matrix_world @ bone.head

def sword_data(side):
    sword = bpy.data.objects[f"KAELEN_sword_{side}"]
    ev = sword.evaluated_get(bpy.context.evaluated_depsgraph_get())
    mesh = ev.to_mesh()
    pts = [ev.matrix_world @ v.co for v in mesh.vertices]
    ev.to_mesh_clear()
    hand = world_hand(side)
    tip = max(pts, key=lambda p: (p - hand).length)
    return hand, tip, (tip - hand).normalized()

def check_cross():
    action = bpy.data.actions["attack2_crossthrust"]
    assign(action)
    scene = bpy.context.scene
    guard = {side: None for side in ("L", "R")}
    candidates = []
    for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
        scene.frame_set(frame); bpy.context.view_layer.update()
        axes = {}; hands = {}
        for side in ("L", "R"):
            hand, tip, axis = sword_data(side)
            axes[side], hands[side] = axis, hand
            if guard[side] is None:
                guard[side] = hand.copy()
        driven = min(axes[s].dot(FWD) for s in ("L", "R"))
        advance = min(guard[s].y - hands[s].y for s in ("L", "R"))
        if driven >= 0.75 and advance >= 0.10:
            candidates.append((frame, driven, advance))
    assert candidates, "cross-thrust has no simultaneous forward drive with both hands advancing at least 10 cm"
    return candidates

def check_dual():
    action = bpy.data.actions["attack_dualslash"]
    assign(action)
    peaks = {}
    for side in ("L", "R"):
        samples = []
        for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
            bpy.context.scene.frame_set(frame); bpy.context.view_layer.update()
            hand, tip, axis = sword_data(side)
            samples.append((tip.z, frame))
        peaks[side] = max(samples)[1]
    assert abs(peaks["L"] - peaks["R"]) >= 5, f"dual-slash cuts are not distinct; sword-tip peaks L/R={peaks}"
    return peaks

def check_dual_anatomy():
    """Elbow stays outside the body. Hands stay in front. Inside-out arms fail this."""
    action = bpy.data.actions["attack_dualslash"]
    assign(action)
    inverted = []
    behind = []
    for frame in range(int(action.frame_range[0]), int(action.frame_range[1]) + 1):
        bpy.context.scene.frame_set(frame)
        bpy.context.view_layer.update()
        for side, lat, bone in (
            ("L", Vector((1.0, 0.0, 0.0)), "mixamorig:LeftArm"),
            ("R", Vector((-1.0, 0.0, 0.0)), "mixamorig:RightArm"),
        ):
            upper = rig.pose.bones[bone]
            shoulder = rig.matrix_world @ upper.head
            elbow = rig.matrix_world @ upper.tail
            hand = world_hand(side)
            lat_dot = (elbow - shoulder).dot(lat)
            if lat_dot < 0.02:
                inverted.append((frame, side, round(lat_dot, 3)))
            # Y+ is behind the character. A raised hand must stay in front of its shoulder.
            if shoulder.y - hand.y < -0.04:
                behind.append((frame, side, round(hand.y - shoulder.y, 3)))
    assert not inverted, f"inside-out elbow {inverted[:8]}"
    assert not behind, f"hand behind body {behind[:8]}"
    return {"inverted": inverted, "behind": behind}

def check_walk_assets():
    def sha(path):
        h=hashlib.sha256()
        with open(path,"rb") as f:
            for block in iter(lambda:f.read(1<<20),b""): h.update(block)
        return h.hexdigest()
    a,b=sha(FINAL+"/KAELEN_walk.fbx"),sha(PREVIEW+"/KAELEN_walk.fbx")
    assert a==b, f"Unity preview uses stale walk FBX: final={a}, preview={b}"
    return a

if __name__ == "__main__":
    results={}
    results["cross_thrust"] = check_cross()
    results["dual_slash"] = check_dual()
    results["dual_anatomy"] = check_dual_anatomy()
    results["walk_sha256"] = check_walk_assets()
    print("ACCEPTANCE_PASS", results)
