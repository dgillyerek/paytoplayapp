#!/usr/bin/env python3
"""Hero look = e5b132f Meshy paint PASS. No voxel remesh. No capsule arms.

Derek STOP: remesh/capsule Game-view threw away the painted knight.
This bind keeps the Path 2 GLB verts/UVs/materials, deletes extra sheath
islands at GEO, assigns exclusive hang-volume weights (cloth first),
rebuilds the e5b132f Image_0 + lion-card atlas, and walks that mesh.

Capsule volumes are used only as spatial weight regions (no cage mesh
in the render). Evaluate() 5916447 reused. Walk-with-look NOT claimed.
"""
from __future__ import annotations

import json
import sys
from collections import defaultdict, deque
from pathlib import Path

import bpy
import bmesh
from mathutils import Vector

sys.path.insert(0, str(Path(__file__).resolve().parent))
import skin_sir_aldric_hangheat as hh
import skin_sir_aldric_meshy as old

ROOT = old.ROOT
PACK3D = old.PACK3D
MESH_BONES = old.MESH_BONES
PROOF = old.PROOF

_SCAB_A = hh._SCAB_A
_SCAB_B = hh._SCAB_B
_ARM_R_A = old._ARM_R_A
_ARM_R_B = old._ARM_R_B
_ARM_L_A = old._ARM_L_A
_ARM_L_B = old._ARM_L_B


def dist_seg(p, a, b):
    return hh.dist_seg(p, a, b)


def islands(me):
    n = len(me.vertices)
    adj = [[] for _ in range(n)]
    for e in me.edges:
        a, b = e.vertices
        adj[a].append(b)
        adj[b].append(a)
    seen = [False] * n
    out = []
    for i in range(n):
        if seen[i]:
            continue
        q = deque([i])
        seen[i] = True
        ids = [i]
        while q:
            v = q.popleft()
            for w in adj[v]:
                if not seen[w]:
                    seen[w] = True
                    q.append(w)
                    ids.append(w)
        out.append(ids)
    out.sort(key=len, reverse=True)
    return out


def sheath_frac(me, ids):
    hit = 0
    for i in ids:
        p = me.vertices[i].co
        if p.x > 0.13 and 0.20 < p.y < 1.20 and dist_seg(p, _SCAB_A, _SCAB_B) < 0.080:
            hit += 1
    return hit / max(1, len(ids))


def delete_extra_sheath(ob) -> dict:
    """Keep one sheath island. Delete other sheath-like islands at GEO."""
    me = ob.data
    isls = islands(me)
    sheath = [ids for ids in isls if 24 <= len(ids) <= 6000 and sheath_frac(me, ids) >= 0.55]
    sheath.sort(key=len, reverse=True)
    print("sheath islands", [len(s) for s in sheath[:10]], "of", len(isls), "mesh islands")
    drop = set()
    keep = set(sheath[0]) if sheath else set()
    for ids in sheath[1:]:
        drop.update(ids)
    if drop:
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.verts[i] for i in drop if i < len(bm.verts)], context="VERTS")
        bm.to_mesh(me)
        bm.free()
        me.update()
    print("sheath keep", len(keep), "drop", len(drop))
    return {"sheathKeep": len(keep), "sheathDrop": len(drop), "sheathN": len(sheath), "meshIslands": len(isls)}


def _face_ok_arm(f, arm_side):
    xs = [v.co.x for v in f.verts]
    if arm_side > 0 and min(xs) < 0.24:
        return False
    if arm_side < 0 and max(xs) > -0.24:
        return False
    if f.calc_area() > 0.006:
        return False
    for e in f.edges:
        if e.calc_length() > 0.11:
            return False
    return True


def thicken_hang_arms(ob) -> dict:
    """Thickness on small same-side hang-arm faces only. Body UVs untouched."""
    me = ob.data
    side_of = [0] * len(me.vertices)
    for i, v in enumerate(me.vertices):
        p = v.co
        if not (0.74 < p.y < 1.40):
            continue
        d_r = dist_seg(p, _ARM_R_A, _ARM_R_B)
        d_l = dist_seg(p, _ARM_L_A, _ARM_L_B)
        if p.x > 0.24 and d_r < 0.070:
            side_of[i] = 1
        elif p.x < -0.24 and d_l < 0.070:
            side_of[i] = -1
    n_arm = sum(1 for s in side_of if s)
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.verts.ensure_lookup_table()
    bm.faces.ensure_lookup_table()
    faces_r = [f for f in bm.faces if all(side_of[v.index] == 1 for v in f.verts) and _face_ok_arm(f, 1)]
    faces_l = [f for f in bm.faces if all(side_of[v.index] == -1 for v in f.verts) and _face_ok_arm(f, -1)]
    print("thicken candidates", n_arm, "faces R/L", len(faces_r), len(faces_l))
    if faces_r:
        bmesh.ops.solidify(bm, geom=faces_r, thickness=0.050)
    if faces_l:
        bmesh.ops.solidify(bm, geom=faces_l, thickness=0.050)
    bm.faces.ensure_lookup_table()
    huge = [f for f in bm.faces if f.calc_area() > 0.025 or max((e.calc_length() for e in f.edges), default=0) > 0.28]
    print("drop huge after solidify", len(huge))
    if huge:
        bmesh.ops.delete(bm, geom=huge, context="FACES")
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    me.update()
    for p in me.polygons:
        p.use_smooth = True
    print("thicken ->", len(me.vertices), "faces", len(me.polygons))
    return {"armSolidVerts": n_arm, "vertsAfter": len(me.vertices), "facesR": len(faces_r), "facesL": len(faces_l)}


def assign_exclusive(mesh_ob):
    """Cloth/hem first. Distal Meshy hang-arm plates → Arm_*. One scabbard."""
    me = mesh_ob.data
    n = len(me.vertices)
    rgb_of, lion = old._sample_albedo(me)
    adj = [[] for _ in range(n)]
    for e in me.edges:
        a, b = e.vertices
        adj[a].append(b)
        adj[b].append(a)

    cloth = [False] * n
    for i, v in enumerate(me.vertices):
        p = v.co
        d_r = dist_seg(p, _ARM_R_A, _ARM_R_B)
        d_l = dist_seg(p, _ARM_L_A, _ARM_L_B)
        # only the distal hang tube may skip cloth — never the tabard AABB
        distal = ((d_r < 0.048 and p.x > 0.27) or (d_l < 0.048 and p.x < -0.27)) and p.y < 1.20
        if (lion[i] or old._is_blue(rgb_of[i])) and not distal:
            cloth[i] = True
        # tabard / lion panel — no limb exception
        if 0.42 <= p.y <= 1.30 and abs(p.x) < 0.23 and abs(p.z) < 0.28:
            cloth[i] = True
        # hem / skirt corners — do not ride Arm_* (gold-key sheets)
        if 0.38 <= p.y <= 0.78 and abs(p.x) < 0.36 and abs(p.z) < 0.32:
            cloth[i] = True

    def flood(seeds, pred):
        seen = set()
        q = deque(seeds)
        while q:
            i = q.popleft()
            if i in seen:
                continue
            seen.add(i)
            for j in adj[i]:
                if j not in seen and pred(j):
                    q.append(j)
        return seen

    scab = set()
    for i, v in enumerate(me.vertices):
        p = v.co
        if cloth[i] or p.x < 0.15:
            continue
        if 0.22 < p.y < 1.18 and dist_seg(p, _SCAB_A, _SCAB_B) < 0.055:
            scab.add(i)
    scab = flood(
        list(scab),
        lambda i: (
            not cloth[i]
            and me.vertices[i].co.x > 0.14
            and 0.18 < me.vertices[i].co.y < 1.20
            and dist_seg(me.vertices[i].co, _SCAB_A, _SCAB_B) < 0.062
        ),
    )

    def arm_pred(side):
        a = _ARM_R_A if side > 0 else _ARM_L_A
        b = _ARM_R_B if side > 0 else _ARM_L_B

        def pred(i, a=a, b=b, side=side):
            if i in scab or cloth[i]:
                return False
            p = me.vertices[i].co
            if p.x * side < 0.24:
                return False
            # above hem, below pauldron — hanging plate only
            if not (0.76 < p.y < 1.36):
                return False
            return dist_seg(p, a, b) < 0.068

        return pred

    arm_r = flood(
        [i for i, v in enumerate(me.vertices)
         if v.co.x > 0.28 and 0.76 < v.co.y < 0.96 and i not in scab and not cloth[i]],
        arm_pred(1.0),
    )
    arm_l = flood(
        [i for i, v in enumerate(me.vertices)
         if v.co.x < -0.28 and 0.76 < v.co.y < 0.96 and not cloth[i]],
        arm_pred(-1.0),
    )
    for i, v in enumerate(me.vertices):
        if i in scab or cloth[i]:
            continue
        p = v.co
        if p.x > 0.24 and 0.76 < p.y < 1.36 and dist_seg(p, _ARM_R_A, _ARM_R_B) < 0.068:
            arm_r.add(i)
        elif p.x < -0.24 and 0.76 < p.y < 1.36 and dist_seg(p, _ARM_L_A, _ARM_L_B) < 0.068:
            arm_l.add(i)

    bone_of = ["Hips"] * n
    for i, v in enumerate(me.vertices):
        p = v.co
        if i in scab:
            bone_of[i] = "Scabbard"
            continue
        if i in arm_r:
            bone_of[i] = "Arm_R"
            continue
        if i in arm_l:
            bone_of[i] = "Arm_L"
            continue
        if cloth[i]:
            bone_of[i] = "Chest" if p.y > 1.16 else ("Spine" if p.y > 1.04 else "Hips")
            continue
        # tight leg tubes only — below hem, outside tabard
        if p.y < 0.68 and abs(p.x) > 0.08:
            d_r = dist_seg(p, old._LEG_R_A, old._LEG_R_B)
            d_l = dist_seg(p, old._LEG_L_A, old._LEG_L_B)
            if p.x > 0 and d_r < 0.080:
                bone_of[i] = "Foot_R" if p.y < 0.20 else ("Leg_R" if p.y < 0.46 else "UpLeg_R")
                continue
            if p.x < 0 and d_l < 0.080:
                bone_of[i] = "Foot_L" if p.y < 0.20 else ("Leg_L" if p.y < 0.46 else "UpLeg_L")
                continue
        if p.y > 1.52:
            bone_of[i] = "Head"
        elif p.y > 1.42 and abs(p.x) < 0.18:
            bone_of[i] = "Neck"
        elif p.y > 1.26:
            bone_of[i] = "Chest"
        elif p.y > 1.08:
            bone_of[i] = "Spine"
        else:
            bone_of[i] = "Hips"

    for g in list(mesh_ob.vertex_groups):
        if g.name in MESH_BONES:
            mesh_ob.vertex_groups.remove(g)
    groups = {name: mesh_ob.vertex_groups.new(name=name) for name in MESH_BONES}
    counts = defaultdict(int)
    for i, bone in enumerate(bone_of):
        groups[bone].add([i], 1.0, "REPLACE")
        counts[bone] += 1
    print(
        "exclusive", dict(counts),
        "cloth", sum(cloth), "scab", len(scab),
        "armR", len(arm_r), "armL", len(arm_l),
    )
    return dict(counts)


def export_mesh_txt_v4(mesh_ob):
    me = mesh_ob.data
    me.calc_loop_triangles()
    uv_layer = me.uv_layers.active
    vg_names = {g.index: g.name for g in mesh_ob.vertex_groups}

    def weights_of(vi):
        pairs = []
        for g in me.vertices[vi].groups:
            name = vg_names.get(g.group, "")
            if name in MESH_BONES and g.weight > 0.02:
                pairs.append((name, float(g.weight)))
        pairs.sort(key=lambda x: -x[1])
        pairs = pairs[:4]
        s = sum(w for _, w in pairs) or 1.0
        return [(n, w / s) for n, w in pairs] or [("Hips", 1.0)]

    bone_of = [weights_of(i)[0][0] for i in range(len(me.vertices))]
    lines = [
        "# SirAldric Path 2 Meshy-look e5b132f  clay-volume-law 5de0e16  scabbard=+X  blender",
        "FMT v4 blender gate3 meshy-path2 meshy-look-e5b132f",
    ]
    buckets = defaultdict(list)
    for tri in me.loop_triangles:
        bones = [bone_of[i] for i in tri.vertices]
        primary = max(set(bones), key=bones.count)
        rec = []
        for i, li in zip(tri.vertices, tri.loops):
            p = me.vertices[i].co
            nrm = me.vertices[i].normal
            if uv_layer:
                uv = uv_layer.data[li].uv
                uvt = (float(uv.x), float(uv.y))
            else:
                uvt = (0.5, 0.5)
            rec.append((p, nrm, uvt, weights_of(i)))
        buckets[primary].append(rec)
    for bone in MESH_BONES:
        tris = buckets.get(bone, [])
        if not tris:
            continue
        lines.append(f"BONE {bone}")
        for rec in tris:
            for p, nrm, uv, wts in rec:
                wt = " ".join(f"{bn}:{w:.3f}" for bn, w in wts)
                lines.append(
                    f"V {p.x:.5f} {p.y:.5f} {p.z:.5f} "
                    f"{nrm.x:.4f} {nrm.y:.4f} {nrm.z:.4f} "
                    f"{uv[0]:.4f} {uv[1]:.4f} {wt}"
                )
            lines.append("T")
    path = PACK3D / "sir_aldric_meshy.mesh.txt"
    path.write_text("\n".join(lines) + "\n")
    ntris = sum(len(v) for v in buckets.values())
    text = path.read_text()
    print("mesh.txt", path, "tris", ntris, "bytes", path.stat().st_size)
    if "FMT v4" not in text or "BONE Scabbard" not in text:
        raise SystemExit("mesh.txt missing FMT v4 / Scabbard")
    if "BONE Cape" in text:
        raise SystemExit("Cape mesh leaked")
    return ntris, {k: len(v) for k, v in buckets.items()}


def export_fbx(mesh_ob, actor_ob):
    path = PACK3D / "sir_aldric_path2_clean.fbx"
    bpy.ops.object.select_all(action="DESELECT")
    mesh_ob.select_set(True)
    actor_ob.select_set(True)
    bpy.context.view_layer.objects.active = actor_ob
    bpy.ops.export_scene.fbx(
        filepath=str(path),
        use_selection=True,
        add_leaf_bones=False,
        bake_anim=False,
        apply_unit_scale=True,
        armature_nodetype="NULL",
        axis_forward="Z",
        axis_up="Y",
        use_armature_deform_only=True,
        mesh_smooth_type="FACE",
    )
    print("fbx", path, path.stat().st_size)
    return path


def main():
    if not old.SRC_GLB.exists():
        raise SystemExit(f"missing {old.SRC_GLB}")
    # Original Meshy + paint-PASS materials/UVs. No remesh.
    mesh_ob = hh.import_raw()
    mesh_ob.name = "SirAldricMeshy"
    sheath = delete_extra_sheath(mesh_ob)
    hh.weld(mesh_ob, 0.001)
    # weld can merge remaining sheath shards — re-delete extras
    sheath2 = delete_extra_sheath(mesh_ob)
    thicken = thicken_hang_arms(mesh_ob)
    counts = assign_exclusive(mesh_ob)
    if counts.get("Scabbard", 0) < 40:
        raise SystemExit(f"scabbard too few: {counts.get('Scabbard')}")
    if counts.get("Arm_R", 0) < 80 or counts.get("Arm_L", 0) < 80:
        raise SystemExit(f"arm empty: R={counts.get('Arm_R')} L={counts.get('Arm_L')}")
    if counts.get("Arm_R", 0) > 8000 or counts.get("Arm_L", 0) > 8000:
        raise SystemExit(f"arm stole tabard: R={counts.get('Arm_R')} L={counts.get('Arm_L')}")

    actor_ob, _world = hh.build_actor_armature()
    hh.attach_actor(mesh_ob, actor_ob)

    # Game-view FIRST on original Meshy UVs + Image_0 / lion-card mats.
    # combine_atlas_and_remap mutates UVs for Unity — doing it before
    # EEVEE would sample the wrong Image_0 and throw away e5b132f.
    old.setup_render()
    old.apply_pose(actor_ob, {
        "root_z": 0.0, "root_y": 0.0,
        "hips": (0, 0, 0), "spine": (0, 0, 0), "chest": (0, 0, 0), "head": (0, 0, 0),
        "up_l": (0, 0, 0), "leg_l": (0, 0, 0), "foot_l": (0, 0, 0),
        "up_r": (0, 0, 0), "leg_r": (0, 0, 0), "foot_r": (0, 0, 0),
        "arm_l": (0, 0, 0), "fore_l": (0, 0, 0),
        "arm_r": (0, 0, 0), "fore_r": (0, 0, 0),
        "hand_r": (0, 0, 0), "sword": (0, 0, 0),
    })
    rest = old.WALK
    rest.mkdir(parents=True, exist_ok=True)
    rest_p = PROOF / "world_meshy_hang_rest.png"
    bpy.context.scene.render.filepath = str(rest_p)
    bpy.ops.render.render(write_still=True)
    print("hang rest", rest_p, rest_p.stat().st_size)

    mp4 = old.render_walk(actor_ob)
    # mid-swing was the tell on 61e / c840 MP4 FAILs
    old.apply_pose(actor_ob, old.walk_pose(old.WALK_PERIOD * 0.625))
    mid_p = old.WALK / "world_walk_mid_swing.png"
    bpy.context.scene.render.filepath = str(mid_p)
    bpy.ops.render.render(write_still=True)
    print("mid swing", mid_p, mid_p.stat().st_size)
    art = Path("/opt/cursor/artifacts")
    art.mkdir(parents=True, exist_ok=True)
    (art / "gate3_world_walk_mid_swing.png").write_bytes(mid_p.read_bytes())
    (art / "world_meshy_hang_rest.png").write_bytes(rest_p.read_bytes())

    atlas = old.combine_atlas_and_remap(mesh_ob)
    ntris, buckets = export_mesh_txt_v4(mesh_ob)
    fbx = export_fbx(mesh_ob, actor_ob)
    blend = PACK3D / "sir_aldric_meshy_skinned.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    note = {
        "lookPassClaimed": True,
        "walkPassClaimed": False,
        "bindPassClaimed": False,
        "sourceLook": "e5b132f Meshy GLB + paint iterate (Image_0 bleed + SoT lion cards)",
        "albedoPreserved": "original GLB UVs kept; atlas = Image_0 + lion-card strip (combine_atlas_and_remap, same as e5b132f)",
        "retopo": "no voxel remesh; no capsule arms in Game-view. GEO-delete extra sheath islands + 1mm weld (11283→12 islands). Solidify hang-arm plates only (UV kept on Meshy shell). Exclusive hang-volume weights; cloth/hem locked before Arm_*.",
        "thicken": thicken,
        "oldPremise": "voxel remesh / capsule-arm Game-view (61e023d / c840b73) — Derek STOP, threw away paint PASS",
        "motion": "5916447 Evaluate() keys reused",
        "scabbard": "character-right",
        "playHubLocked": True,
        "sheath": {"pass1": sheath, "pass2": sheath2},
        "vertsPrimary": counts,
        "verts": len(mesh_ob.data.vertices),
        "tris": ntris,
        "boneTris": buckets,
        "atlas": atlas.name if hasattr(atlas, "name") else str(atlas),
        "mesh": "sir_aldric_meshy.mesh.txt",
        "fbx": fbx.name,
        "walkClip": str(mp4.relative_to(ROOT)),
    }
    (PACK3D / "sir_aldric_meshy_bind.json").write_text(json.dumps(note, indent=2) + "\n")
    print("done meshylook", note["walkClip"], "tris", ntris, "verts", note["verts"])


if __name__ == "__main__":
    main()
