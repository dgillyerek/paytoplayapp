#!/usr/bin/env python3
"""Premise B: delete dup sheath at GEO, heat/multi-bone on hang skeleton.

Census (census_path2_bind.py): mesh arms are already hang (~8° from −Y),
NOT A-pose. 11283 islands; 2 overlapping sheath groups. The twice-failed
premise was Bone1 corridor weights, not an A-pose rest.

Evaluate() keys are COPIED not edited. Walk-with-look NOT claimed.
Play hub PNG not swapped.
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
import skin_sir_aldric_meshy as old

ROOT = old.ROOT
PACK3D = old.PACK3D
SRC_GLB = old.SRC_GLB
PROOF = old.PROOF
TARGET_H = old.TARGET_H
MESH_BONES = old.MESH_BONES
PARENT = old.PARENT
BONE_REST = old.BONE_REST

_SCAB_A = Vector((0.20, 1.04, -0.04))
_SCAB_B = Vector((0.38, 0.30, 0.05))

# Hang joint spans for HEAT (head → tail along the limb). Not the Actor +Y stubs.
HEAT_SPAN = {
    "Root": ((0.0, 0.0, 0.0), (0.0, 0.08, 0.0)),
    "Hips": ((0.0, 0.96, 0.0), (0.0, 1.08, 0.0)),
    "Spine": ((0.0, 1.08, 0.0), (0.0, 1.26, 0.0)),
    "Chest": ((0.0, 1.26, 0.0), (0.0, 1.46, 0.0)),
    "Neck": ((0.0, 1.46, 0.0), (0.0, 1.56, 0.0)),
    "Head": ((0.0, 1.56, 0.0), (0.0, 1.82, 0.0)),
    "Arm_L": ((-0.22, 1.36, 0.0), (-0.24, 1.08, 0.0)),
    "Fore_L": ((-0.24, 1.08, 0.0), (-0.28, 0.84, 0.02)),
    "Hand_L": ((-0.28, 0.84, 0.02), (-0.32, 0.68, 0.03)),
    "Arm_R": ((0.22, 1.36, 0.0), (0.24, 1.08, 0.0)),
    "Fore_R": ((0.24, 1.08, 0.0), (0.28, 0.84, 0.02)),
    "Hand_R": ((0.28, 0.84, 0.02), (0.32, 0.68, 0.03)),
    "Sword": ((0.30, 0.76, 0.04), (0.32, 0.64, 0.06)),
    "UpLeg_L": ((-0.11, 0.92, 0.0), (-0.11, 0.50, 0.0)),
    "Leg_L": ((-0.11, 0.50, 0.0), (-0.11, 0.20, 0.03)),
    "Foot_L": ((-0.11, 0.20, 0.03), (-0.11, 0.02, 0.10)),
    "UpLeg_R": ((0.11, 0.92, 0.0), (0.11, 0.50, 0.0)),
    "Leg_R": ((0.11, 0.50, 0.0), (0.11, 0.20, 0.03)),
    "Foot_R": ((0.11, 0.20, 0.03), (0.11, 0.02, 0.10)),
    "Scabbard": ((0.20, 1.04, -0.03), (0.36, 0.32, 0.05)),
    "Cape": ((0.0, 1.34, -0.12), (0.0, 1.10, -0.18)),
}


def dist_seg(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / max(1e-9, ab.length_squared)))
    return (p - (a + t * ab)).length


def import_raw():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=str(SRC_GLB))
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = "SirAldricMeshy"
    me = ob.data
    for v in me.vertices:
        x, y, z = v.co
        v.co = Vector((-x, z, -y))
    ys = [v.co.y for v in me.vertices]
    ymin, ymax = min(ys), max(ys)
    scale = TARGET_H / max(1e-6, ymax - ymin)
    for v in me.vertices:
        v.co = Vector((v.co.x * scale, (v.co.y - ymin) * scale, v.co.z * scale))
    me.update()
    return ob


def delete_dup_sheath(ob) -> dict:
    """Delete overlapping sheath islands at GEO. Keep the largest in-box component."""
    me = ob.data
    marked = []
    for i, v in enumerate(me.vertices):
        p = v.co
        if p.x > 0.14 and 0.22 < p.y < 1.18 and dist_seg(p, _SCAB_A, _SCAB_B) < 0.075:
            marked.append(i)
    mark = set(marked)
    adj = [[] for _ in me.vertices]
    for e in me.edges:
        a, b = e.vertices
        if a in mark and b in mark:
            adj[a].append(b)
            adj[b].append(a)
    seen = set()
    comps = []
    for i in marked:
        if i in seen:
            continue
        q = deque([i])
        seen.add(i)
        ids = []
        while q:
            v = q.popleft()
            ids.append(v)
            for w in adj[v]:
                if w not in seen:
                    seen.add(w)
                    q.append(w)
        comps.append(ids)
    comps.sort(key=len, reverse=True)
    keep = set(comps[0]) if comps else set()
    drop = set()
    for c in comps[1:]:
        drop.update(c)
    print("sheath comps", [len(c) for c in comps[:8]], "keep", len(keep), "drop", len(drop))
    if drop:
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.verts[i] for i in drop if i < len(bm.verts)], context="VERTS")
        bm.to_mesh(me)
        bm.free()
        me.update()
    return {"sheathKeep": len(keep), "sheathDrop": len(drop), "sheathComps": [len(c) for c in comps]}


def weld(ob, dist=0.001):
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    before = len(bm.verts)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=dist)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    print("weld", dist, before, "->", len(bm.verts), "faces", len(bm.faces))
    bm.free()
    me.update()
    for p in me.polygons:
        p.use_smooth = True
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    try:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception:
        pass
    bpy.ops.object.mode_set(mode="OBJECT")


def build_heat_armature():
    arm = bpy.data.armatures.new("HeatArm")
    ob = bpy.data.objects.new("HeatArm", arm)
    bpy.context.collection.objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    eb = {}
    for name, (head, tail) in HEAT_SPAN.items():
        b = arm.edit_bones.new(name)
        b.head = Vector(head)
        b.tail = Vector(tail)
        b.roll = 0.0
        b.use_deform = name in MESH_BONES
        eb[name] = b
    for name, parent in PARENT.items():
        if parent:
            eb[name].parent = eb[parent]
            eb[name].use_connect = False
    bpy.ops.object.mode_set(mode="OBJECT")
    return ob


def build_actor_armature():
    return old.build_armature()


def heat_bind(mesh_ob, heat_ob):
    bpy.ops.object.select_all(action="DESELECT")
    mesh_ob.select_set(True)
    heat_ob.select_set(True)
    bpy.context.view_layer.objects.active = heat_ob
    bpy.ops.object.parent_set(type="ARMATURE_AUTO")
    print("heat parent", [g.name for g in mesh_ob.vertex_groups])


def attach_actor(mesh_ob, actor_ob):
    mesh_ob.parent = None
    for m in list(mesh_ob.modifiers):
        if m.type == "ARMATURE":
            mesh_ob.modifiers.remove(m)
    mesh_ob.parent = actor_ob
    mod = mesh_ob.modifiers.new("Armature", "ARMATURE")
    mod.object = actor_ob
    mod.use_vertex_groups = True
    return mod


def _is_blue(rgb):
    r, g, b = rgb
    return b > r + 12 and b > 45 and r < 110


def lock_cloth_and_sheath(mesh_ob, sheath_meta):
    """After heat: cloth cannot ride limbs; remaining sheath = Scabbard 1.0."""
    me = mesh_ob.data
    groups = {g.name: g for g in mesh_ob.vertex_groups}
    for n in MESH_BONES:
        if n not in groups:
            groups[n] = mesh_ob.vertex_groups.new(name=n)

    # albedo for cloth
    rgb_of = [(128, 128, 128)] * len(me.vertices)
    lion = [False] * len(me.vertices)
    imgs = {img.name: img for img in bpy.data.images if img.size[0] >= 64}
    albedo = None
    for name, img in imgs.items():
        if "Image_0" in name:
            albedo = img
            break
    if albedo is None and imgs:
        albedo = next(iter(imgs.values()))
    if albedo is not None:
        from PIL import Image
        import numpy as np

        path = Path("/tmp/path2-skin/Image_0.png")
        path.parent.mkdir(parents=True, exist_ok=True)
        albedo.filepath_raw = str(path)
        albedo.file_format = "PNG"
        albedo.save()
        arr = np.array(Image.open(path).convert("RGB"))
        ah, aw = arr.shape[:2]
        uv_layer = me.uv_layers.active
        acc = defaultdict(list)
        for poly in me.polygons:
            mat = me.materials[poly.material_index] if me.materials else None
            is_lion = bool(mat and "lion" in mat.name.lower())
            for li, vi in zip(poly.loop_indices, poly.vertices):
                if is_lion:
                    lion[vi] = True
                if uv_layer:
                    u, v = uv_layer.data[li].uv
                    x = int(max(0, min(aw - 1, float(u) * aw)))
                    y = int(max(0, min(ah - 1, (1.0 - float(v)) * ah)))
                    acc[vi].append(tuple(int(c) for c in arr[y, x]))
        for i, samples in acc.items():
            n = len(samples)
            rgb_of[i] = (
                sum(s[0] for s in samples) // n,
                sum(s[1] for s in samples) // n,
                sum(s[2] for s in samples) // n,
            )

    limb = {"Arm_L", "Fore_L", "Hand_L", "Arm_R", "Fore_R", "Hand_R",
            "UpLeg_L", "Leg_L", "Foot_L", "UpLeg_R", "Leg_R", "Foot_R", "Scabbard"}
    torso = ["Hips", "Spine", "Chest"]
    n_cloth = n_scab = 0
    for i, v in enumerate(me.vertices):
        p = v.co
        on_sheath = p.x > 0.16 and 0.24 < p.y < 1.16 and dist_seg(p, _SCAB_A, _SCAB_B) < 0.055
        if on_sheath:
            for g in mesh_ob.vertex_groups:
                g.remove([i])
            groups["Scabbard"].add([i], 1.0, "REPLACE")
            n_scab += 1
            continue
        cloth = lion[i] or _is_blue(rgb_of[i])
        if cloth and 0.50 < p.y < 1.20 and abs(p.x) < 0.22:
            for name in limb:
                if name in groups:
                    groups[name].remove([i])
            # keep / boost torso
            groups["Hips"].add([i], 1.0, "ADD")
            n_cloth += 1
    print("lock cloth", n_cloth, "sheath", n_scab)
    return n_cloth, n_scab


def primary_counts(mesh_ob):
    vg_names = {g.index: g.name for g in mesh_ob.vertex_groups}
    counts = defaultdict(int)
    for v in mesh_ob.data.vertices:
        if v.groups:
            g = max(v.groups, key=lambda x: x.weight)
            counts[vg_names.get(g.group, "Hips")] += 1
        else:
            counts["Hips"] += 1
    print("primary", dict(counts))
    return dict(counts)


def export_mesh_txt_v4(mesh_ob):
    """Object-space verts + up to 4 named weights. Unity bindposes handle rest."""
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

    bone_of = []
    for i in range(len(me.vertices)):
        bone_of.append(weights_of(i)[0][0])

    lines = [
        "# SirAldric Path 2 hang-heat  clay-volume-law 5de0e16  scabbard=+X  blender",
        "FMT v4 blender gate3 meshy-path2 hang-heat",
    ]
    buckets = defaultdict(list)
    for tri in me.loop_triangles:
        bones = [bone_of[i] for i in tri.vertices]
        primary = max(set(bones), key=bones.count)
        rec = []
        for i, li in zip(tri.vertices, tri.loops):
            p = me.vertices[i].co
            n = me.vertices[i].normal
            if uv_layer:
                uv = uv_layer.data[li].uv
                uvt = (float(uv.x), float(uv.y))
            else:
                uvt = (0.5, 0.5)
            rec.append((p, n, uvt, weights_of(i)))
        buckets[primary].append(rec)

    for bone in MESH_BONES:
        tris = buckets.get(bone, [])
        if not tris:
            continue
        lines.append(f"BONE {bone}")
        for rec in tris:
            for p, n, uv, wts in rec:
                wt = " ".join(f"{bn}:{w:.3f}" for bn, w in wts)
                lines.append(
                    f"V {p.x:.5f} {p.y:.5f} {p.z:.5f} "
                    f"{n.x:.4f} {n.y:.4f} {n.z:.4f} "
                    f"{uv[0]:.4f} {uv[1]:.4f} {wt}"
                )
            lines.append("T")
    path = PACK3D / "sir_aldric_meshy.mesh.txt"
    path.write_text("\n".join(lines) + "\n")
    ntris = sum(len(v) for v in buckets.values())
    text = path.read_text()
    print("mesh.txt", path, "tris", ntris, "bytes", path.stat().st_size)
    if "FMT v4" not in text:
        raise SystemExit("missing FMT v4")
    if "BONE Scabbard" not in text:
        raise SystemExit("missing BONE Scabbard")
    if "BONE Cape" in text:
        raise SystemExit("Cape mesh leaked")
    if ":" not in text:
        raise SystemExit("v4 weights missing")
    return ntris, {k: len(v) for k, v in buckets.items()}


def main():
    if not SRC_GLB.exists():
        raise SystemExit(f"missing {SRC_GLB}")
    mesh_ob = import_raw()
    sheath = delete_dup_sheath(mesh_ob)
    weld(mesh_ob, 0.001)
    heat_ob = build_heat_armature()
    heat_bind(mesh_ob, heat_ob)
    lock_cloth_and_sheath(mesh_ob, sheath)
    counts = primary_counts(mesh_ob)
    if counts.get("Scabbard", 0) < 80:
        raise SystemExit(f"scabbard too few after heat: {counts.get('Scabbard')}")
    if counts.get("Fore_R", 0) < 40 or counts.get("Fore_L", 0) < 40:
        raise SystemExit(f"elbow empty: Fore_R={counts.get('Fore_R')} Fore_L={counts.get('Fore_L')}")
    actor_ob, world = build_actor_armature()
    attach_actor(mesh_ob, actor_ob)
    old.setup_render()
    mp4 = old.render_walk(actor_ob)
    atlas = old.combine_atlas_and_remap(mesh_ob)
    ntris, buckets = export_mesh_txt_v4(mesh_ob)
    blend = PACK3D / "sir_aldric_meshy_skinned.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    note = {
        "lookPassClaimed": True,
        "walkPassClaimed": False,
        "oldPremise": "rigid Bone1 / corridor weights on Meshy shell + hang Evaluate() — failed 89481e5 and 71f0c4a",
        "newPremise": "B: delete duplicate sheath islands at GEO, then Blender heat/multi-bone on hang spans (elbow+hand influence). Census: mesh already hang (~8deg), not A-pose.",
        "sheath": sheath,
        "motion": "5916447 Evaluate() keys reused — gait/weave/forward-swing not edited",
        "scabbard": "character-right",
        "playHubLocked": True,
        "vertsPrimary": counts,
        "tris": ntris,
        "boneTris": buckets,
        "atlas": atlas.name,
        "mesh": "sir_aldric_meshy.mesh.txt",
        "format": "FMT v4 object-space + 4 named weights",
        "walkClip": str(mp4.relative_to(ROOT)),
    }
    (PACK3D / "sir_aldric_meshy_bind.json").write_text(json.dumps(note, indent=2) + "\n")
    print("done hangheat", note["walkClip"], "tris", ntris, "premise B")


if __name__ == "__main__":
    main()
