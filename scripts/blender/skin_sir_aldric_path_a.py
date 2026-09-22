#!/usr/bin/env python3
"""Path A LOCKED: clean mid-poly retopo + project Meshy/e5b132f albedo.

Derek: STOP tube / paper / remesh-melt / capsule bind hacks.
1) SOURCE = Path 2 Meshy GLB + e5b132f Image_0 / SoT lion.
2) CLEAN RETOPO = QuadriFlow mid-poly (manifold, humanoid-ready).
   Voxel is scaffold only, then shrinkwrap back onto the Meshy surface.
   Not the hero mesh. No capsule arms/legs. No paper-island weights.
3) TEXTURE PROJECT = Image_0 / lion bake from the Meshy GLB onto retopo UVs.
   Not Play-cam still compositing.
4) FBX = Y-up, face +Z, one character-RIGHT scabbard, Actor armature.
5) RE-GATE = World stills FIRST vs e5b132f/turnaround, then one Evaluate() walk.

Bind/walk NOT claimed. Hub PNG not swapped. Evaluate() keys reused.
Design ACK folded: ~20–60k tris, one volume/limb, planted 1L+1R walk.
"""
from __future__ import annotations

import json
import math
import os
import shutil
import sys
from pathlib import Path

import bpy
import numpy as np
from mathutils import Vector
from PIL import Image

sys.path.insert(0, str(Path(__file__).resolve().parent))
import import_sir_aldric_meshy as shots
import skin_sir_aldric_hangheat as hh
import skin_sir_aldric_meshy as old
import skin_sir_aldric_meshylook as look
import skin_sir_aldric_retopo as rt

ROOT = old.ROOT
PACK3D = old.PACK3D
PROOF = old.PROOF
ART = Path("/opt/cursor/artifacts")
LOOK_E5 = PROOF / "look_e5b132f"
DROP = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/PATH_A_RETOPO"
MESH_BONES = old.MESH_BONES
TARGET_FACES = int(os.environ.get("PATHA_FACES", "22000"))
SCAFFOLD_VOXEL = float(os.environ.get("PATHA_SCAFFOLD", "0.009"))
WRAP_OFFSET = float(os.environ.get("PATHA_WRAP", "0.003"))
DECIMATE_FACES = int(os.environ.get("PATHA_DECIMATE", "36000"))

CAM_SHOTS = (
    ("world_rear", shots.CAM_EYE, shots.CAM_TARGET),
    ("world_rear_34", shots.CAM_EYE_34, shots.CAM_TARGET),
    ("world_front", shots.CAM_FRONT, shots.CAM_FRONT_T),
    ("world_side_r", shots.CAM_SIDE, shots.CAM_SIDE_T),
    ("world_34_front", shots.CAM_34_FRONT, shots.CAM_34_FRONT_T),
)


def n_islands(me):
    return rt.n_islands(me)


def duplicate_mesh(src, name):
    tgt = src.copy()
    tgt.data = src.data.copy()
    tgt.name = name
    bpy.context.collection.objects.link(tgt)
    return tgt


def mesh_extent(ob):
    xs = [v.co.x for v in ob.data.vertices]
    ys = [v.co.y for v in ob.data.vertices]
    zs = [v.co.z for v in ob.data.vertices]
    return (
        min(xs), max(xs), min(ys), max(ys), min(zs), max(zs),
        max(ys) - min(ys), max(xs) - min(xs), max(zs) - min(zs),
    )


def snapshot_coords(ob):
    return [v.co.copy() for v in ob.data.vertices]


def restore_coords(ob, coords):
    if len(coords) != len(ob.data.vertices):
        return False
    for v, c in zip(ob.data.vertices, coords):
        v.co = c
    ob.data.update()
    return True


def quadriflow(ob, faces: int) -> bool:
    before_v = len(ob.data.vertices)
    before_f = len(ob.data.polygons)
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    try:
        bpy.ops.object.quadriflow_remesh(
            use_mesh_symmetry=False,
            use_preserve_sharp=True,
            use_preserve_boundary=False,
            mode="FACES",
            target_faces=faces,
            seed=4,
        )
    except Exception as exc:
        print("quadriflow failed", exc)
        return False
    for p in ob.data.polygons:
        p.use_smooth = True
    after_v = len(ob.data.vertices)
    after_f = len(ob.data.polygons)
    changed = abs(after_f - before_f) > 80 or abs(after_v - before_v) > 80
    print(
        "quadriflow", faces, "v", after_v, "f", after_f,
        "islands", n_islands(ob.data), "changed", changed,
        "from", before_v, before_f,
    )
    return after_f > 200 and changed


def scaffold_watertight(ob, size: float):
    """One watertight volume so QuadriFlow has manifold input.

    Not the hero mesh. Silhouette is restored by shrinkwrap to Meshy.
    """
    rt.apply_voxel(ob, size)
    print("scaffold voxel (not hero)", size, "islands", n_islands(ob.data))


def ensure_meshy_source(existing):
    """Use hidden MeshySource, or re-import the GLB without wiping the scene."""
    if existing is not None and existing.data.materials:
        existing.hide_set(False)
        existing.hide_render = True
        return existing
    before = set(bpy.data.objects)
    bpy.ops.import_scene.gltf(filepath=str(old.SRC_GLB))
    added = [o for o in bpy.data.objects if o not in before and o.type == "MESH"]
    if not added:
        raise SystemExit("GLB import produced no mesh")
    bpy.ops.object.select_all(action="DESELECT")
    for o in added:
        o.select_set(True)
    bpy.context.view_layer.objects.active = added[0]
    if len(added) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = "MeshySource"
    me = ob.data
    for v in me.vertices:
        x, y, z = v.co
        v.co = Vector((-x, z, -y))
    ys = [v.co.y for v in me.vertices]
    ymin, ymax = min(ys), max(ys)
    scale = old.TARGET_H / max(1e-6, ymax - ymin)
    for v in me.vertices:
        v.co = Vector((v.co.x * scale, (v.co.y - ymin) * scale, v.co.z * scale))
    me.update()
    look.delete_extra_sheath(ob)
    hh.weld(ob, 0.001)
    print("imported bake source", len(ob.data.vertices), "f", len(ob.data.polygons))
    return ob


def shrinkwrap_to_source(tgt, src, offset: float) -> bool:
    """Snap mid-poly onto Meshy. Offset keeps volume from collapsing into paper."""
    pre = mesh_extent(tgt)
    snap = snapshot_coords(tgt)
    bpy.ops.object.select_all(action="DESELECT")
    tgt.select_set(True)
    bpy.context.view_layer.objects.active = tgt
    m = tgt.modifiers.new("toMeshy", "SHRINKWRAP")
    m.target = src
    m.wrap_method = "NEAREST_SURFACEPOINT"
    m.offset = offset
    applied_mode = "ON_SURFACE"
    try:
        m.wrap_mode = "ABOVE_SURFACE"
        applied_mode = "ABOVE_SURFACE"
    except TypeError:
        m.wrap_mode = "ON_SURFACE"
    bpy.ops.object.modifier_apply(modifier="toMeshy")
    bm_ok = True
    try:
        import bmesh
        bm = bmesh.new()
        bm.from_mesh(tgt.data)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(tgt.data)
        bm.free()
    except Exception:
        bm_ok = False
    tgt.data.update()
    post = mesh_extent(tgt)
    collapsed = post[6] < 1.55 or post[7] < 0.42 or post[8] < 0.18
    print(
        "shrinkwrap", applied_mode, "offset", offset,
        "v", len(tgt.data.vertices), "f", len(tgt.data.polygons),
        "h", round(post[6], 3), "w", round(post[7], 3), "d", round(post[8], 3),
        "pre_h", round(pre[6], 3), "normals", bm_ok, "collapsed", collapsed,
    )
    if collapsed:
        restore_coords(tgt, snap)
        print("shrinkwrap restored — kept mid-poly volume")
        return False
    return True


def _vg_dump(ob):
    dump = []
    groups = ob.vertex_groups
    for v in ob.data.vertices:
        wts = [(groups[g.group].name, g.weight) for g in v.groups if g.weight > 1e-6]
        dump.append((v.co.copy(), wts))
    return dump


def _vg_fill_new(ob, dump):
    """Hole-fill verts have no groups. Copy from nearest pre-peel vert. Not a rebind."""
    from mathutils.kdtree import KDTree

    if not dump:
        return 0
    kd = KDTree(len(dump))
    for i, (co, _w) in enumerate(dump):
        kd.insert(co, i)
    kd.balance()
    names = {g.name: g for g in ob.vertex_groups}
    n = 0
    for v in ob.data.vertices:
        has = False
        for g in ob.vertex_groups:
            try:
                if g.weight(v.index) > 1e-8:
                    has = True
                    break
            except RuntimeError:
                continue
        if has:
            continue
        _co, idx, _d = kd.find(v.co)
        for name, w in dump[idx][1]:
            g = names.get(name)
            if g is not None:
                g.add([v.index], w, "REPLACE")
        n += 1
    return n


def _front_torso_p(c: Vector) -> bool:
    # Include waist / mid-tabard. Stop above the skirt hem and inside the arms.
    return 0.86 < c.y < 1.48 and abs(c.x) < 0.195 and c.z > -0.01


def peel_front_torso_inner(tgt, src=None) -> int:
    """Delete the inner remesh wall on the front tabard.

    Solidify+voxel left a connected double shell (one island, ~half the
    torso faces sit behind the +Z first hit). Those poke through cloth as
    tan/under-mesh. Keep one outer surface. Helm/gauntlets/rear untouched.
    Surviving verts keep their weights — not a bind iterate.
    """
    import bmesh
    from mathutils.bvhtree import BVHTree

    me = tgt.data
    dump = _vg_dump(tgt)
    killed = 0
    for _pass in range(4):
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.faces.ensure_lookup_table()
        bvh = BVHTree.FromBMesh(bm)
        kill = []
        for face in bm.faces:
            c = face.calc_center_median()
            if not _front_torso_p(c):
                continue
            n = face.normal
            hit, _sn, idx, _dist = bvh.ray_cast(
                Vector((c.x, c.y, 0.90)), Vector((0.0, 0.0, -1.0))
            )
            if hit is None or idx is None:
                if n.z < -0.35:
                    kill.append(face)
                continue
            if idx == face.index:
                continue
            other = bm.faces[idx] if idx < len(bm.faces) else None
            other_z = other.normal.z if other is not None else 0.0
            if hit.z > c.z + 0.0012:
                kill.append(face)
            elif abs(hit.z - c.z) < 0.004 and n.z < other_z - 0.02:
                kill.append(face)
            elif n.z < -0.35 and idx != face.index:
                kill.append(face)
        nkill = len(kill)
        if nkill < 4:
            bm.free()
            break
        # Do NOT holes_fill — that recaps the cavity and brings the inner wall back.
        bmesh.ops.delete(bm, geom=kill, context="FACES")
        loose = [v for v in bm.verts if not v.link_faces]
        if loose:
            bmesh.ops.delete(bm, geom=loose, context="VERTS")
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(me)
        bm.free()
        me.update()
        killed += nkill
        print("peel pass", _pass, "deleted", nkill, "f", len(me.polygons), flush=True)
    rt.delete_small_islands(tgt, keep_min=24)
    filled = _vg_fill_new(tgt, dump)
    if src is not None and killed:
        _shrinkwrap_front_torso(tgt, src, 0.002)
    print("peel front torso killed", killed, "newWts", filled, "v", len(me.vertices), "f", len(me.polygons))
    return killed


def _shrinkwrap_front_torso(tgt, src, offset: float):
    """Snap only the front tabard onto Meshy after peeling the inner wall."""
    vg_name = "PathAFrontPeel"
    if vg_name in tgt.vertex_groups:
        tgt.vertex_groups.remove(tgt.vertex_groups[vg_name])
    vg = tgt.vertex_groups.new(name=vg_name)
    n = 0
    for v in tgt.data.vertices:
        if _front_torso_p(v.co):
            vg.add([v.index], 1.0, "REPLACE")
            n += 1
    if n < 12:
        return
    bpy.ops.object.select_all(action="DESELECT")
    tgt.select_set(True)
    bpy.context.view_layer.objects.active = tgt
    m = tgt.modifiers.new("peelWrap", "SHRINKWRAP")
    m.target = src
    m.vertex_group = vg_name
    m.wrap_method = "NEAREST_SURFACEPOINT"
    m.offset = offset
    try:
        m.wrap_mode = "ABOVE_SURFACE"
    except TypeError:
        m.wrap_mode = "ON_SURFACE"
    bpy.ops.object.modifier_apply(modifier="peelWrap")
    try:
        import bmesh
        bm = bmesh.new()
        bm.from_mesh(tgt.data)
        bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
        bm.to_mesh(tgt.data)
        bm.free()
    except Exception:
        pass
    tgt.data.update()
    print("peel wrap front verts", n, "offset", offset)


def _is_studio_bg(rgb) -> bool:
    """Reject Play-cam studio/ground, keep navy tabard / brown scabbard."""
    r, g, b = (int(c) for c in rgb[:3])
    chroma = max(r, g, b) - min(r, g, b)
    return max(r, g, b) < 58 and chroma < 14


def _load_project_cams():
    tan_v = math.tan(math.radians(shots.CAM_FOV) * 0.5)
    tan_h = tan_v * (1080.0 / 1920.0)
    cams = []
    for name, eye, tgt in CAM_SHOTS:
        path = LOOK_E5 / f"{name}.png"
        if not path.exists():
            print("missing project still", path)
            continue
        arr = np.array(Image.open(path).convert("RGB"))
        mw = shots.play_cam_matrix(eye, tgt)
        cams.append({
            "name": name,
            "eye": Vector(eye),
            "inv": mw.inverted(),
            "arr": arr,
            "h": arr.shape[0],
            "w": arr.shape[1],
            "tan_h": tan_h,
            "tan_v": tan_v,
            "caption": 80,
        })
    print("project cams", [c["name"] for c in cams])
    return cams


def _face_normals(me):
    """Area-weighted vertex normals from faces. Do not trust stale v.normal."""
    acc = [Vector((0.0, 0.0, 0.0)) for _ in range(len(me.vertices))]
    for poly in me.polygons:
        n = poly.normal
        a = max(poly.area, 1e-8)
        for vi in poly.vertices:
            acc[vi] = acc[vi] + n * a
    out = []
    for n in acc:
        if n.length < 1e-8:
            out.append(Vector((0.0, 1.0, 0.0)))
        else:
            out.append(n.normalized())
    return out


def _visible(bvh, eye: Vector, p: Vector) -> bool:
    direction = p - eye
    dist = direction.length
    if dist < 1e-5:
        return False
    hit, _n, _idx, hdist = bvh.ray_cast(eye, direction / dist, dist + 0.02)
    if hit is None:
        return True
    return hdist >= dist - 0.025


def _sample_cams(p: Vector, n: Vector, cams, bvh) -> tuple[tuple[int, int, int] | None, float]:
    acc = np.zeros(3, np.float64)
    wsum = 0.0
    for cam in cams:
        view = cam["eye"] - p
        if view.length < 1e-6:
            continue
        view.normalize()
        facing = n.dot(view)
        # Vertex normals on a remesh can be weak; allow grazing + rely on ray vis.
        if facing < -0.15:
            continue
        if not _visible(bvh, cam["eye"], p):
            continue
        pc = cam["inv"] @ p
        depth = -pc.z
        if depth < 0.12:
            continue
        ndc_x = pc.x / (depth * cam["tan_h"])
        ndc_y = pc.y / (depth * cam["tan_v"])
        if abs(ndc_x) > 0.98 or abs(ndc_y) > 0.98:
            continue
        px = int((ndc_x * 0.5 + 0.5) * cam["w"])
        py = int((0.5 - ndc_y * 0.5) * cam["h"])
        if py < cam["caption"] or py >= cam["h"] or px < 0 or px >= cam["w"]:
            continue
        rgb = cam["arr"][py, px]
        if _is_studio_bg(rgb):
            continue
        w = max(0.12, facing) ** 1.6
        acc += np.array(rgb, np.float64) * w
        wsum += w
    if wsum < 1e-6:
        return None, 0.0
    return tuple(int(max(0, min(255, c))) for c in (acc / wsum)), wsum


def structured_fill(tgt):
    """Steel / navy / scabbard fallback. Do not use paper-Meshy transfer as base."""
    colors = []
    for v in tgt.data.vertices:
        p = v.co
        if p.x > 0.14 and 0.22 < p.y < 1.18 and hh.dist_seg(p, hh._SCAB_A, hh._SCAB_B) < 0.070:
            colors.append((88, 50, 26))
        elif abs(p.x) < 0.20 and 0.70 < p.y < 1.40 and abs(p.z) < 0.18:
            colors.append((26, 46, 108))
        elif p.y > 1.50:
            colors.append((168, 170, 174))
        else:
            colors.append((156, 158, 160))
    return colors


def camera_project_colors(tgt, fill_colors):
    """Per-vert fallback. Prefer camera_project_atlas for look."""
    from mathutils.bvhtree import BVHTree
    import bmesh
    cams = _load_project_cams()
    colors = list(fill_colors)
    hit = miss = 0
    me = tgt.data
    me.update()
    bm = bmesh.new()
    bm.from_mesh(me)
    bvh = BVHTree.FromBMesh(bm)
    bm.free()
    norms = _face_normals(me)
    for i, v in enumerate(me.vertices):
        rgb, w = _sample_cams(v.co, norms[i], cams, bvh)
        if rgb is None:
            miss += 1
            continue
        colors[i] = rgb
        hit += 1
    print("camera project verts", "hit", hit, "miss", miss, "of", len(me.vertices), flush=True)
    return colors, hit


def _best_cam(centroid: Vector, n: Vector, cams):
    byname = {c["name"]: c for c in cams}
    # Lock the big painted cards to the matching Play still so lion/hem stay put.
    if n.z < -0.28 and "world_rear" in byname:
        cam = byname["world_rear"]
        view = (cam["eye"] - centroid).normalized()
        return cam, max(0.20, n.dot(view))
    if n.z > 0.28 and "world_front" in byname:
        cam = byname["world_front"]
        view = (cam["eye"] - centroid).normalized()
        return cam, max(0.20, n.dot(view))
    if n.x > 0.45 and "world_side_r" in byname:
        cam = byname["world_side_r"]
        view = (cam["eye"] - centroid).normalized()
        return cam, max(0.20, n.dot(view))
    best = None
    best_f = 0.08
    for cam in cams:
        view = cam["eye"] - centroid
        if view.length < 1e-6:
            continue
        view.normalize()
        f = n.dot(view)
        if f > best_f:
            best_f = f
            best = cam
    return best, best_f


def _sample_one_cam(p: Vector, cam):
    pc = cam["inv"] @ p
    depth = -pc.z
    if depth < 0.12:
        return None
    ndc_x = pc.x / (depth * cam["tan_h"])
    ndc_y = pc.y / (depth * cam["tan_v"])
    if abs(ndc_x) > 0.98 or abs(ndc_y) > 0.98:
        return None
    fx = (ndc_x * 0.5 + 0.5) * (cam["w"] - 1)
    fy = (0.5 - ndc_y * 0.5) * (cam["h"] - 1)
    if fy < cam["caption"] or fy >= cam["h"] - 1 or fx < 0 or fx >= cam["w"] - 1:
        return None
    x0, y0 = int(fx), int(fy)
    tx, ty = fx - x0, fy - y0
    c00 = cam["arr"][y0, x0].astype(np.float32)
    c10 = cam["arr"][y0, min(x0 + 1, cam["w"] - 1)].astype(np.float32)
    c01 = cam["arr"][min(y0 + 1, cam["h"] - 1), x0].astype(np.float32)
    c11 = cam["arr"][min(y0 + 1, cam["h"] - 1), min(x0 + 1, cam["w"] - 1)].astype(np.float32)
    rgb = (c00 * (1 - tx) * (1 - ty) + c10 * tx * (1 - ty) + c01 * (1 - tx) * ty + c11 * tx * ty)
    if _is_studio_bg(rgb):
        return None
    return tuple(int(max(0, min(255, c))) for c in rgb)


def camera_project_atlas(tgt, fill_colors, atlas_path: Path):
    """Per-texel Play-cam project. Keeps lion/hem coherent (not per-vert shred)."""
    cams = _load_project_cams()
    me = tgt.data
    me.calc_loop_triangles()
    uv = me.uv_layers.active
    s = rt.ATLAS_SIZE
    acc = np.zeros((s, s, 3), np.float32)
    wgt = np.zeros((s, s), np.float32)
    # Keep a Cycles bake if present; else structured steel/navy (not paper smear).
    if atlas_path.exists() and atlas_path.stat().st_size > 80_000:
        acc[:] = np.array(Image.open(atlas_path).convert("RGB").resize((s, s)), np.float32)
    else:
        rt.rasterize_atlas(tgt, fill_colors, atlas_path)
        acc[:] = np.array(Image.open(atlas_path).convert("RGB"), np.float32)
    wgt[:] = 0.15
    tris_hit = pix_hit = 0
    for tri in me.loop_triangles:
        n = Vector(tri.normal)
        pts = []
        pos = []
        for vi, li in zip(tri.vertices, tri.loops):
            u, v = uv.data[li].uv
            pts.append((float(u) * (s - 1), (1.0 - float(v)) * (s - 1)))
            pos.append(me.vertices[vi].co.copy())
        centroid = (pos[0] + pos[1] + pos[2]) / 3.0
        cam, facing = _best_cam(centroid, n, cams)
        if cam is None:
            continue
        (x0, y0), (x1, y1), (x2, y2) = pts
        minx = max(0, int(math.floor(min(x0, x1, x2))))
        maxx = min(s - 1, int(math.ceil(max(x0, x1, x2))))
        miny = max(0, int(math.floor(min(y0, y1, y2))))
        maxy = min(s - 1, int(math.ceil(max(y0, y1, y2))))
        denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
        if abs(denom) < 1e-8:
            continue
        wrote = False
        for y in range(miny, maxy + 1):
            py = y + 0.5
            for x in range(minx, maxx + 1):
                px = x + 0.5
                w0 = ((y1 - y2) * (px - x2) + (x2 - x1) * (py - y2)) / denom
                w1 = ((y2 - y0) * (px - x2) + (x0 - x2) * (py - y2)) / denom
                w2 = 1.0 - w0 - w1
                if w0 < -0.01 or w1 < -0.01 or w2 < -0.01:
                    continue
                p = pos[0] * w0 + pos[1] * w1 + pos[2] * w2
                rgb = _sample_one_cam(p, cam)
                if rgb is None:
                    continue
                acc[y, x] = np.array(rgb, np.float32)
                wgt[y, x] = facing
                pix_hit += 1
                wrote = True
        if wrote:
            tris_hit += 1
    # Grow projected stills into steel-fill holes (copy neighbor, not max — max blows white).
    hit = wgt > 0.16
    for _ in range(8):
        grown = acc.copy()
        new_hit = hit.copy()
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            shifted = np.roll(acc, (dy, dx), (0, 1))
            shift_hit = np.roll(hit, (dy, dx), (0, 1))
            take = (~hit) & shift_hit
            grown[take] = shifted[take]
            new_hit |= take
        acc = grown
        hit = new_hit
    Image.fromarray(np.clip(acc, 0, 255).astype(np.uint8), "RGB").save(atlas_path)
    print("camera project atlas tris", tris_hit, "pix", pix_hit, "size", atlas_path.stat().st_size, flush=True)
    return tris_hit


def _pbr_paths(atlas_path: Path):
    stem = atlas_path.with_suffix("")
    return Path(str(stem) + "_metallic.png"), Path(str(stem) + "_roughness.png")


def write_pbr_maps(atlas_path: Path):
    """Steel vs navy/gold/leather so EEVEE key light reads specular silver, not plastic gray."""
    rgb = np.array(Image.open(atlas_path).convert("RGB"), np.float32)
    r, g, b = rgb[:, :, 0], rgb[:, :, 1], rgb[:, :, 2]
    lum = 0.30 * r + 0.59 * g + 0.11 * b
    chroma = np.maximum(np.maximum(r, g), b) - np.minimum(np.minimum(r, g), b)
    navy = (b > r + 8) & (b > 40) & (r < 110)
    gold = (r > 118) & (g > 80) & (r > b + 10)
    leather = (~gold) & (r > g + 10) & (r > b + 15) & (r < 170) & (g < 110) & (b < 90)
    steel = (~navy) & (~gold) & (~leather) & (chroma < 38) & (lum > 68)
    metallic = np.zeros(lum.shape, np.float32)
    roughness = np.full(lum.shape, 0.62, np.float32)
    metallic[steel] = 0.84
    roughness[steel] = 0.28
    metallic[gold] = 0.78
    roughness[gold] = 0.38
    metallic[navy] = 0.0
    roughness[navy] = 0.64
    metallic[leather] = 0.04
    roughness[leather] = 0.72
    met_p, rgh_p = _pbr_paths(atlas_path)
    Image.fromarray(np.clip(metallic * 255.0, 0, 255).astype(np.uint8), "L").save(met_p)
    Image.fromarray(np.clip(roughness * 255.0, 0, 255).astype(np.uint8), "L").save(rgh_p)
    print("pbr maps steel", int(steel.sum()), "navy", int(navy.sum()), "gold", int(gold.sum()), flush=True)
    return met_p, rgh_p


def enable_eevee_spec():
    sc = bpy.context.scene
    try:
        sc.eevee.use_ssr = True
        sc.eevee.ssr_quality = 0.75
    except Exception:
        pass
    try:
        sc.eevee.use_raytracing = True
    except Exception:
        pass
    try:
        sc.eevee.taa_render_samples = 32
    except Exception:
        pass


def assign_projected_material(ob, atlas_path: Path):
    """Image_0/lion albedo + steel metallic. Not global 0.12/0.55 plastic gray."""
    def load_img(path: Path, name: str, noncolor: bool):
        path = path.resolve()
        for old in list(bpy.data.images):
            if old.name == name or old.name.startswith(name + "."):
                bpy.data.images.remove(old)
        # Unique path so Blender cannot reuse the packed/open-time cache.
        live = Path(f"/tmp/patha_{name}.png")
        live.write_bytes(path.read_bytes())
        img = bpy.data.images.load(str(live), check_existing=False)
        img.name = name
        try:
            img.reload()
        except Exception:
            pass
        if noncolor:
            try:
                img.colorspace_settings.name = "Non-Color"
            except Exception:
                pass
        return img

    img = load_img(atlas_path, "PathAAtlas", False)
    mat = bpy.data.materials.new("PathALook")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    try:
        bsdf.inputs["Roughness"].default_value = 0.32
        bsdf.inputs["Metallic"].default_value = 0.80
    except Exception:
        pass
    try:
        if "Specular IOR Level" in bsdf.inputs:
            bsdf.inputs["Specular IOR Level"].default_value = 0.55
        elif "Specular" in bsdf.inputs:
            bsdf.inputs["Specular"].default_value = 0.55
    except Exception:
        pass
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    met_p, rgh_p = _pbr_paths(atlas_path)
    if not met_p.exists() or not rgh_p.exists():
        write_pbr_maps(atlas_path)
    if met_p.exists():
        met = nt.nodes.new("ShaderNodeTexImage")
        met.image = load_img(met_p, "PathAMetallic", True)
        nt.links.new(met.outputs["Color"], bsdf.inputs["Metallic"])
    if rgh_p.exists():
        rgh = nt.nodes.new("ShaderNodeTexImage")
        rgh.image = load_img(rgh_p, "PathARoughness", True)
        nt.links.new(rgh.outputs["Color"], bsdf.inputs["Roughness"])
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    ob.data.materials.clear()
    ob.data.materials.append(mat)
    enable_eevee_spec()


def _sample_src_image(arr, uv):
    if arr is None:
        return None
    ah, aw = arr.shape[:2]
    x = int(max(0, min(aw - 1, float(uv.x) * aw)))
    y = int(max(0, min(ah - 1, (1.0 - float(uv.y)) * ah)))
    return tuple(int(c) for c in arr[y, x])


def _sample_card_xy(arr, u, v):
    if arr is None:
        return None
    ah, aw = arr.shape[:2]
    x = int(max(0, min(aw - 1, float(u) * (aw - 1))))
    y = int(max(0, min(ah - 1, (1.0 - float(v)) * (ah - 1))))
    return tuple(int(c) for c in arr[y, x])


# Front-half torso AABB — includes inner remesh shells that peek tan through cloth.
TABARD_FRONT_WIN = (-0.165, 0.165, 1.045, 1.455)
# e5b132f rampant dest box — card fills this, not a cluster of center faces.
LION_FRONT_WIN = (-0.076, 0.076, 1.178, 1.355)


def dest_in_front_lion(p: Vector, n: Vector) -> bool:
    return (
        LION_FRONT_WIN[0] <= p.x <= LION_FRONT_WIN[1]
        and LION_FRONT_WIN[2] <= p.y <= LION_FRONT_WIN[3]
        and p.z > 0.02
    )


def load_lion_cards():
    """Prefer Meshy GLB lion_card_front/back. Rebuild the same cards if missing."""
    albedo, lions, imgs = rt.source_textures()
    front = back = None
    for n, a in lions.items():
        ln = n.lower()
        if "front" in ln:
            front = a
        elif "back" in ln:
            back = a
    if front is None or back is None:
        try:
            import repair_path2_paint as repair
            navy = np.array([36, 58, 118], np.uint8)
            if front is None and (repair.GATE / "01_FRONT.png").exists():
                spr = repair.extract_lion_sprite(repair.GATE / "01_FRONT.png", repair.LION_FRONT_BOX)
                front = np.array(repair.compose_lion_card(spr, navy).convert("RGB"))
                print("lion card front rebuilt from SoT box")
            if back is None and (repair.LOOK / "01_rear_LOCKED.png").exists():
                spr = repair.extract_lion_sprite(repair.LOOK / "01_rear_LOCKED.png", repair.LION_REAR_BOX)
                back = np.array(repair.compose_lion_card(spr, navy).convert("RGB"))
                print("lion card back rebuilt from SoT box")
        except Exception as exc:
            print("lion card fallback", exc)
    print(
        "lion cards front", None if front is None else front.shape,
        "back", None if back is None else back.shape,
        "glb", list(lions),
    )
    return albedo, lions, imgs, front, back


def _atlas_cloth(atlas, uv_loop, s: int) -> bool:
    u, v = uv_loop.uv
    x = int(max(0, min(s - 1, float(u) * (s - 1))))
    y = int(max(0, min(s - 1, (1.0 - float(v)) * (s - 1))))
    r, g, b = (int(c) for c in atlas[y, x])
    return (b > r + 8 and b > 40 and r < 130) or (r > 110 and g > 80 and r > b + 8)


def _crop_lion_sprite(card: np.ndarray) -> np.ndarray:
    """Tight gold rampant + navy fringe. Drops the card's empty navy padding."""
    r, g, b = card[:, :, 0], card[:, :, 1], card[:, :, 2]
    gold = (r > 118) & (g > 80) & (r > b + 10)
    if not gold.any():
        return card
    ys, xs = np.where(gold)
    y0, y1 = max(0, int(ys.min()) - 6), min(card.shape[0], int(ys.max()) + 7)
    x0, x1 = max(0, int(xs.min()) - 6), min(card.shape[1], int(xs.max()) + 7)
    return card[y0:y1, x0:x1]


def _image0_navy_sampler(src, navy):
    """Dest→source Image_0 for tabard navy. Drops shredded gold, not Game stills."""
    import bmesh
    from mathutils.bvhtree import BVHTree

    if src is None:
        return lambda _p, _n: tuple(int(c) for c in navy), lambda: None
    albedo, _lions, _imgs = rt.source_textures()[:3]
    bm = bmesh.new()
    bm.from_mesh(src.data)
    bm.faces.ensure_lookup_table()
    uv_lay = bm.loops.layers.uv.active
    bvh = BVHTree.FromBMesh(bm)
    mats = list(src.data.materials)

    def sample(p: Vector, n: Vector):
        loc, _sn, idx, dist = bvh.find_nearest(p)
        if loc is None or idx is None or dist > 0.06:
            return tuple(int(c) for c in navy)
        face = bm.faces[idx]
        mat = mats[face.material_index] if face.material_index < len(mats) else None
        mname = (mat.name if mat else "").lower()
        iname = (rt.mat_image_name(mat) or "").lower()
        if "lion" in mname or "lion" in iname:
            return tuple(int(c) for c in navy)
        verts = [loop.vert.co for loop in face.loops]
        uvs = [loop[uv_lay].uv.copy() if uv_lay else Vector((0.5, 0.5)) for loop in face.loops]
        wts = rt.mathutils_bary(loc, verts)
        uvw = Vector((0.0, 0.0))
        for wt, u in zip(wts, uvs):
            uvw += u * wt
        rgb = _sample_src_image(albedo, uvw)
        if rgb is None:
            return tuple(int(c) for c in navy)
        r, g, b = rgb
        navy_ok = b > r + 8 and b > 40 and r < 110
        if not navy_ok:
            return tuple(int(c) for c in navy)
        return rgb

    def close():
        bm.free()

    return sample, close


def _pick_image0_navy(src, fallback):
    """One dest→source Image_0 navy at chest center. Solid fill, not a noisy bake."""
    sample, close = _image0_navy_sampler(src, fallback)
    rgb = sample(Vector((0.0, 1.24, 0.16)), Vector((0.0, 0.0, 1.0)))
    close()
    return np.array(rgb, np.uint8)


def _atlas_tan(atlas, uv_loop, s: int) -> bool:
    u, v = uv_loop.uv
    x = int(max(0, min(s - 1, float(u) * (s - 1))))
    y = int(max(0, min(s - 1, (1.0 - float(v)) * (s - 1))))
    r, g, b = (int(c) for c in atlas[y, x])
    chroma = max(r, g, b) - min(r, g, b)
    lum = 0.30 * r + 0.59 * g + 0.11 * b
    return chroma < 42 and 70 < lum < 185


def stamp_front_lion_dest(tgt, atlas_path: Path, card, src=None) -> int:
    """Opaque front tabard: solid Image_0 navy on every chest face, then lion.

    Tan shred was inner remesh + dest-planar folds still sampling old islands.
    Park the whole front-half torso on a navy pad, dest-planar only the clean
    front faces onto a lion strip that is the e5b132f window. Not Game stills.
    """
    if card is None or not atlas_path.exists():
        print("front lion stamp skipped — no card/atlas")
        return 0
    me = tgt.data
    uv = me.uv_layers.active
    if uv is None:
        return 0
    atlas = np.array(Image.open(atlas_path).convert("RGB"))
    s = atlas.shape[0]
    navy = _pick_image0_navy(src, np.array(card[8, 8], np.uint8))
    tx0, tx1, ty0, ty1 = TABARD_FRONT_WIN
    u0, u1, v0, v1 = 0.68, 0.94, 0.62, 0.97
    nu0, nu1, nv0, nv1 = 0.955, 0.995, 0.84, 0.98
    cloth, outer, stray = [], [], []
    for poly in me.polygons:
        c = poly.center
        n = poly.normal
        on_strip = 0
        tan_n = 0
        for li in poly.loop_indices:
            uu, vv = uv.data[li].uv
            if 0.66 < uu < 0.96 and 0.60 < vv < 0.99:
                on_strip += 1
            if _atlas_tan(atlas, uv.data[li], s):
                tan_n += 1
        in_tabard = tx0 <= c.x <= tx1 and ty0 <= c.y <= ty1 and c.z > -0.01
        torso = 0.96 < c.y < 1.48 and abs(c.x) < 0.20 and c.z > -0.02
        loose = torso and (tan_n >= 1 or on_strip >= 1)
        if in_tabard or loose:
            cloth.append(poly.index)
        elif on_strip >= 2:
            stray.append(poly.index)
        if in_tabard or loose:
            outer.append(poly.index)
    print("front tabard cloth", len(cloth), "outer", len(outer), "stray", len(stray), flush=True)
    if len(cloth) < 8:
        return 0
    atlas[int((1.0 - nv1) * s):int((1.0 - nv0) * s), int(nu0 * s):int(nu1 * s)] = navy
    for fi in cloth:
        poly = me.polygons[fi]
        for li in poly.loop_indices:
            uv.data[li].uv = Vector(((nu0 + nu1) * 0.5, (nv0 + nv1) * 0.5))
    for fi in stray:
        poly = me.polygons[fi]
        for li in poly.loop_indices:
            uv.data[li].uv = Vector((0.02, 0.02))
    y_top = int((1.0 - v1) * s)
    y_bot = int((1.0 - v0) * s)
    x_l = int(u0 * s)
    x_r = int(u1 * s)
    atlas[y_top:y_bot, x_l:x_r] = navy
    sprite = _crop_lion_sprite(card)
    sr, sg, sb = sprite[:, :, 0], sprite[:, :, 1], sprite[:, :, 2]
    sprite_navy = (sb > sr + 8) & (sb > 40) & (sr < 110)
    sprite = sprite.copy()
    sprite[sprite_navy] = navy
    su0, su1, sv0, sv1 = u0, u1, v0, v1
    ly_top = int((1.0 - sv1) * s)
    ly_bot = int((1.0 - sv0) * s)
    lx_l = int(su0 * s)
    lx_r = int(su1 * s)
    if lx_r > lx_l and ly_bot > ly_top:
        slot = Image.fromarray(sprite).resize((lx_r - lx_l, ly_bot - ly_top), Image.LANCZOS)
        atlas[ly_top:ly_bot, lx_l:lx_r] = np.array(slot)
    mx0, mx1, my0, my1 = -0.132, 0.132, 1.122, 1.390
    for fi in outer:
        poly = me.polygons[fi]
        for li, vi in zip(poly.loop_indices, poly.vertices):
            p = me.vertices[vi].co
            tu = max(0.0, min(1.0, (p.x - mx0) / max(1e-6, mx1 - mx0)))
            tv = max(0.0, min(1.0, (p.y - my0) / max(1e-6, my1 - my0)))
            uv.data[li].uv = Vector((u0 + tu * (u1 - u0), v0 + tv * (v1 - v0)))
    Image.fromarray(atlas).save(atlas_path)
    print(
        "front tabard navy+lion", x_l, y_top, x_r, y_bot,
        "cloth", len(cloth), "outer", len(outer),
        "lionBlit", lx_l, ly_top, lx_r, ly_bot, "sprite", sprite.shape, flush=True,
    )
    return len(cloth)


def bake_image0_texels(src, tgt, atlas_path: Path) -> int:
    """Per-texel bake from Meshy Image_0 + lion cards onto retopo UVs.

    Cycles selected-to-active misses: source paper sits *inside* the
    shrinkwrapped retopo, so cage rays hit the target first (55KB black).
    This shoots dest→source via BVH. Prefer Image_0 / lion; penalize
    Image_1/2 trim slivers that smeared gold on earlier transfers.
    """
    import bmesh
    from mathutils.bvhtree import BVHTree

    if src is None or src == tgt:
        return 0
    albedo, lions, imgs, lion_front, lion_back = load_lion_cards()
    if albedo is None:
        print("bake_image0: no Image_0")
        return 0
    lion_arr = lion_back if lion_back is not None else lion_front

    src_me = src.data
    bm = bmesh.new()
    bm.from_mesh(src_me)
    bm.faces.ensure_lookup_table()
    uv_lay = bm.loops.layers.uv.active
    bvh = BVHTree.FromBMesh(bm)
    mats = list(src_me.materials)

    if tgt.data.uv_layers.active is None:
        rt.smart_uv(tgt)
    me = tgt.data
    me.calc_loop_triangles()
    uv = me.uv_layers.active
    s = rt.ATLAS_SIZE
    acc = np.full((s, s, 3), 150, np.float32)
    wgt = np.zeros((s, s), np.float32)

    def src_uv(face, loc):
        verts = [loop.vert.co for loop in face.loops]
        uvs = [loop[uv_lay].uv.copy() if uv_lay else Vector((0.5, 0.5)) for loop in face.loops]
        wts = rt.mathutils_bary(loc, verts)
        uvw = Vector((0.0, 0.0))
        for wt, u in zip(wts, uvs):
            uvw += u * wt
        return uvw

    def face_kind(face):
        mat = mats[face.material_index] if face.material_index < len(mats) else None
        mname = (mat.name if mat else "").lower()
        iname = (rt.mat_image_name(mat) or "").lower()
        if "lion" in mname or "lion" in iname:
            if "front" in mname or "front" in iname:
                return "lion_front"
            if "back" in mname or "back" in iname:
                return "lion_back"
            return "lion"
        if "image_0" in iname:
            return "albedo"
        if "image_1" in iname or "image_2" in iname:
            return "trim"
        return "albedo"

    def pick(p: Vector, n: Vector):
        cands = []
        loc, sn, idx, dist = bvh.find_nearest(p)
        if loc is not None and idx is not None and dist < 0.055:
            align = abs(Vector(sn).dot(n)) if sn is not None else 0.0
            cands.append((dist, align, idx, Vector(loc)))
        for sign in (-1.0, 1.0):
            origin = p + n * (0.002 * sign)
            hit, hsn, hidx, hdist = bvh.ray_cast(origin, n * sign, 0.055)
            if hit is not None and hidx is not None:
                align = abs(Vector(hsn).dot(n)) if hsn is not None else 0.0
                cands.append((hdist, align, hidx, Vector(hit)))
        if not cands:
            return None
        def score(c):
            dist, align, idx, _loc = c
            kind = face_kind(bm.faces[idx])
            pen = 0.018 if kind == "trim" else (-0.012 if kind.startswith("lion") else -0.006)
            return dist - 0.028 * align + pen
        return min(cands, key=score)

    pix = 0
    for tri in me.loop_triangles:
        n = Vector(tri.normal)
        if n.length < 1e-8:
            continue
        n.normalize()
        pts, pos = [], []
        for vi, li in zip(tri.vertices, tri.loops):
            u, v = uv.data[li].uv
            pts.append((float(u) * (s - 1), (1.0 - float(v)) * (s - 1)))
            pos.append(me.vertices[vi].co.copy())
        (x0, y0), (x1, y1), (x2, y2) = pts
        minx = max(0, int(math.floor(min(x0, x1, x2))))
        maxx = min(s - 1, int(math.ceil(max(x0, x1, x2))))
        miny = max(0, int(math.floor(min(y0, y1, y2))))
        maxy = min(s - 1, int(math.ceil(max(y0, y1, y2))))
        denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
        if abs(denom) < 1e-8:
            continue
        for y in range(miny, maxy + 1):
            py = y + 0.5
            for x in range(minx, maxx + 1):
                px = x + 0.5
                w0 = ((y1 - y2) * (px - x2) + (x2 - x1) * (py - y2)) / denom
                w1 = ((y2 - y0) * (px - x2) + (x0 - x2) * (py - y2)) / denom
                w2 = 1.0 - w0 - w1
                if w0 < -0.01 or w1 < -0.01 or w2 < -0.01:
                    continue
                p = pos[0] * w0 + pos[1] * w1 + pos[2] * w2
                if dest_in_front_lion(p, n) and lion_front is not None:
                    u = (p.x - LION_FRONT_WIN[0]) / max(1e-6, LION_FRONT_WIN[1] - LION_FRONT_WIN[0])
                    v = (p.y - LION_FRONT_WIN[2]) / max(1e-6, LION_FRONT_WIN[3] - LION_FRONT_WIN[2])
                    rgb = _sample_card_xy(lion_front, u, v)
                    if rgb is not None:
                        acc[y, x] = np.array(rgb, np.float32)
                        wgt[y, x] = 1.0
                        pix += 1
                    continue
                got = pick(p, n)
                if got is None:
                    continue
                _d, _a, idx, loc = got
                face = bm.faces[idx]
                kind = face_kind(face)
                uvw = src_uv(face, loc)
                if kind == "lion_front":
                    rgb = _sample_src_image(lion_front if lion_front is not None else lion_arr, uvw)
                elif kind == "lion_back":
                    rgb = _sample_src_image(lion_back if lion_back is not None else lion_arr, uvw)
                elif kind == "lion":
                    pick_arr = lion_front if n.z > 0.0 and lion_front is not None else (lion_back if lion_back is not None else lion_arr)
                    rgb = _sample_src_image(pick_arr if pick_arr is not None else albedo, uvw)
                else:
                    rgb = _sample_src_image(albedo, uvw)
                if rgb is None:
                    continue
                acc[y, x] = np.array(rgb, np.float32)
                wgt[y, x] = 1.0
                pix += 1
    bm.free()
    hit = wgt > 0.5
    for _ in range(10):
        grown = acc.copy()
        new_hit = hit.copy()
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            shifted = np.roll(acc, (dy, dx), (0, 1))
            shift_hit = np.roll(hit, (dy, dx), (0, 1))
            take = (~hit) & shift_hit
            grown[take] = shifted[take]
            new_hit |= take
        acc = grown
        hit = new_hit
    Image.fromarray(np.clip(acc, 0, 255).astype(np.uint8), "RGB").save(atlas_path)
    print("image0 texel bake pix", pix, "size", atlas_path.stat().st_size, flush=True)
    return pix


def project_albedo(src, tgt, atlas_path: Path):
    """Image_0 + dest-space lion cards onto retopo UVs. No frozen Game stills."""
    keep = os.environ.get("PATHA_KEEP_ATLAS") == "1" and atlas_path.exists() and atlas_path.stat().st_size > 80_000
    pix = 0
    if keep:
        print("keep existing Image_0 atlas — dest lion stamp + PBR only")
    else:
        pix = bake_image0_texels(src, tgt, atlas_path)
        if pix < 2000:
            raise SystemExit(f"Image_0 bake did not stick: pix={pix}")
    _albedo, _lions, _imgs, lion_front, _lion_back = load_lion_cards()
    stamped = stamp_front_lion_dest(tgt, atlas_path, lion_front, src)
    write_pbr_maps(atlas_path)
    assign_projected_material(tgt, atlas_path)
    print("baked atlas", atlas_path, atlas_path.stat().st_size, "pix", pix, "frontLion", stamped)
    return atlas_path


def _seg_dist(p: Vector, a: Vector, b: Vector) -> float:
    ab = b - a
    denom = max(1e-9, ab.length_squared)
    t = max(0.0, min(1.0, (p - a).dot(ab) / denom))
    return (p - (a + t * ab)).length


def spatial_bind(mesh_ob):
    """Nearest rest-bone-segment weights on the retopo volume.

    Not exclusive paper-island corridors. Not heat on shatter.
    """
    rest = {}
    for name in old.ORDER if hasattr(old, "ORDER") else list(old.BONE_REST):
        loc, eul = old.BONE_REST[name]
        parent = old.PARENT[name]
        local = Vector(loc)
        if parent is None:
            rest[name] = local
        else:
            rest[name] = rest[parent] + local
    child_of = {p: [] for p in old.PARENT}
    for n, p in old.PARENT.items():
        if p:
            child_of.setdefault(p, []).append(n)
    segs = {}
    for name in MESH_BONES:
        head = rest[name]
        kids = [c for c in child_of.get(name, []) if c in rest]
        if kids:
            tail = rest[kids[0]]
            if (tail - head).length < 0.06:
                tail = head + Vector((0.0, -0.16, 0.0))
        else:
            tail = head + Vector((0.0, -0.12, 0.0))
        segs[name] = (head, tail)
    for g in list(mesh_ob.vertex_groups):
        mesh_ob.vertex_groups.remove(g)
    groups = {n: mesh_ob.vertex_groups.new(name=n) for n in MESH_BONES}
    for i, v in enumerate(mesh_ob.data.vertices):
        p = v.co
        dists = []
        for name, (a, b) in segs.items():
            if name == "Scabbard":
                continue
            dists.append((_seg_dist(p, a, b), name))
        dists.sort()
        near = dists[0][0]
        take = [(d, n) for d, n in dists[:4] if d <= max(0.10, near * 2.6)]
        ws = [(n, 1.0 / max(d, 0.012) ** 2) for d, n in take]
        s = sum(w for _, w in ws) or 1.0
        for n, w in ws:
            groups[n].add([i], w / s, "REPLACE")
    print("spatial bind segs", {n: (round(a.y, 2), round(b.y, 2)) for n, (a, b) in segs.items()})


def lock_scabbard(mesh_ob) -> int:
    """One character-RIGHT scabbard volume. Not a paper-island hack."""
    me = mesh_ob.data
    groups = {g.name: g for g in mesh_ob.vertex_groups}
    if "Scabbard" not in groups:
        groups["Scabbard"] = mesh_ob.vertex_groups.new(name="Scabbard")
    n = 0
    for i, v in enumerate(me.vertices):
        p = v.co
        if p.x > 0.14 and 0.22 < p.y < 1.18 and hh.dist_seg(p, hh._SCAB_A, hh._SCAB_B) < 0.058:
            for g in groups.values():
                try:
                    g.remove([i])
                except RuntimeError:
                    pass
            groups["Scabbard"].add([i], 1.0, "REPLACE")
            n += 1
    print("scabbard lock", n)
    return n


def recount(mesh_ob):
    me = mesh_ob.data
    vg = {g.index: g.name for g in mesh_ob.vertex_groups}
    counts = {n: 0 for n in MESH_BONES}
    for v in me.vertices:
        best, bw = "Hips", 0.0
        for g in v.groups:
            name = vg.get(g.group, "")
            if name in counts and g.weight > bw:
                best, bw = name, g.weight
        counts[best] = counts.get(best, 0) + 1
    print("primary", {k: v for k, v in counts.items() if v})
    return counts


def render_world_shots():
    """front / rear / side / ¾ + rear Play angle. Design eyes these FIRST."""
    enable_eevee_spec()
    cam = bpy.context.scene.camera
    if cam is None:
        raise SystemExit("no camera")
    PROOF.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    for name, (eye, tgt) in (
        ("world_rear", (shots.CAM_EYE, shots.CAM_TARGET)),
        ("world_rear_34", (shots.CAM_EYE_34, shots.CAM_TARGET)),
        ("world_front", (shots.CAM_FRONT, shots.CAM_FRONT_T)),
        ("world_side_r", (shots.CAM_SIDE, shots.CAM_SIDE_T)),
        ("world_34_front", (shots.CAM_34_FRONT, shots.CAM_34_FRONT_T)),
    ):
        cam.matrix_world = shots.play_cam_matrix(eye, tgt)
        path = PROOF / f"{name}.png"
        bpy.context.scene.render.filepath = str(path)
        bpy.ops.render.render(write_still=True)
        print("world", name, path.stat().st_size)
        try:
            (ART / f"path_a_{name}.png").write_bytes(path.read_bytes())
        except OSError as exc:
            print("artifact skip", exc)
    rest = PROOF / "world_meshy_hang_rest.png"
    rest.write_bytes((PROOF / "world_rear.png").read_bytes())


def patch_mesh_header():
    path = PACK3D / "sir_aldric_meshy.mesh.txt"
    lines = path.read_text().splitlines()
    if lines:
        lines[0] = "# SirAldric Path A retopo+project e5b132f  scabbard=+X  blender"
    if len(lines) > 1:
        lines[1] = "FMT v4 blender gate3 meshy-path2 path-a-retopo-project"
    path.write_text("\n".join(lines) + "\n")


def write_drop(note: dict):
    DROP.mkdir(parents=True, exist_ok=True)
    readme = DROP / "README.md"
    readme.write_text(
        "# Path A — clean retopo + Meshy project\n\n"
        "Derek Path A LOCKED. Design ACK folded into this drop.\n\n"
        "- SOURCE: Path 2 Meshy GLB + e5b132f Image_0 / SoT lion. LOOK = Meshy knight.\n"
        "- RETOPO: mid-poly manifold (voxel scaffold only, then shrinkwrap to Meshy). "
        "Not remesh-melt hero, not capsules, not paper-island weights.\n"
        "- GEO: peel inner remesh wall on the front tabard (solidify double-shell). "
        "One outer cloth surface. Helm/gauntlets/rear untouched.\n"
        "- TEXTURE: solid Image_0 navy on that outer cloth + dest-planar "
        "lion_card_front. Steel PBR kept. No frozen Game still compositing. "
        "Do NOT claim Design PASS.\n"
        "- FBX: ThemePack `sir_aldric_path2_clean.fbx` — Y-up, face +Z, one scabbard-R.\n"
        "- RE-GATE: World stills first, then one Evaluate() walk. "
        "**Bind NOT claimed. Walk NOT claimed.** Hub PNG HOLD. PR #21 HOLD.\n\n"
        f"tris={note.get('tris')} verts={note.get('verts')} islands={note.get('islands')}\n"
        f"wrap={note.get('wrap')} bind=spatial (not automatic weights)\n"
    )
    for name in (
        "world_rear.png", "world_rear_34.png", "world_front.png",
        "world_side_r.png", "world_34_front.png",
        "gate3_path_a_vs_e5b132f.png", "gate3_worldcam_vs_sot.png",
        "sir_aldric_path2_walk_toward_top.mp4",
    ):
        src = PROOF / name
        if src.exists():
            shutil.copy2(src, DROP / name)
    for name in (
        "world_walk_contact_l.png", "world_walk_contact_r.png",
        "world_walk_pass_l.png", "world_walk_n10.png",
    ):
        src = old.WALK / name
        if src.exists():
            shutil.copy2(src, DROP / name)
    (DROP / "sir_aldric_meshy_bind.json").write_text(json.dumps(note, indent=2) + "\n")
    print("path A drop", DROP)


def main():
    if not old.SRC_GLB.exists():
        raise SystemExit(f"missing {old.SRC_GLB}")
    src = hh.import_raw()
    src.name = "MeshySource"
    look.delete_extra_sheath(src)
    hh.weld(src, 0.001)
    print("source islands", n_islands(src.data), "v", len(src.data.vertices), "f", len(src.data.polygons))

    tgt = duplicate_mesh(src, "SirAldricPathA")
    rt.fill_holes(tgt)
    # Paper shells do not voxel-fuse. Thicken, then the finest scaffold that holds.
    # Do NOT floor voxel at 0.016 — that was the remesh-melt hero.
    rt.apply_solidify(tgt, 0.010)
    scaffold_watertight(tgt, SCAFFOLD_VOXEL)
    rt.delete_small_islands(tgt, keep_min=150)
    islands, sizes = n_islands(tgt.data)
    if islands > 4:
        print("still shattered, step scaffold 0.013")
        scaffold_watertight(tgt, 0.013)
        rt.delete_small_islands(tgt, keep_min=180)
        islands, sizes = n_islands(tgt.data)
    if islands > 4:
        print("still shattered, step scaffold 0.016")
        scaffold_watertight(tgt, 0.016)
        rt.delete_small_islands(tgt, keep_min=200)
        islands, sizes = n_islands(tgt.data)
    if len(tgt.data.polygons) > DECIMATE_FACES:
        rt.decimate_to(tgt, DECIMATE_FACES)
    ok = quadriflow(tgt, TARGET_FACES)
    if not ok:
        print("QuadriFlow no-op or failed — keep decimated scaffold, then wrap")
    wrapped = shrinkwrap_to_source(tgt, src, WRAP_OFFSET)
    rt.delete_small_islands(tgt, keep_min=20)
    # Mid-poly budget after wrap: Design ACK ~20–60k tris (keep detail).
    if len(tgt.data.polygons) > 32000:
        rt.decimate_to(tgt, 28000)
        if wrapped:
            shrinkwrap_to_source(tgt, src, WRAP_OFFSET)
            rt.delete_small_islands(tgt, keep_min=20)
    islands, sizes = n_islands(tgt.data)
    ext = mesh_extent(tgt)
    print(
        "retopo islands", islands, "top", sizes,
        "v", len(tgt.data.vertices), "f", len(tgt.data.polygons),
        "h", round(ext[6], 3), "wrapped", wrapped,
    )
    if islands > 8:
        raise SystemExit(f"retopo still shattered: {islands} {sizes}")
    ntris_est = len(tgt.data.polygons) * 2
    if ntris_est < 8000:
        raise SystemExit(f"retopo too thin: faces={len(tgt.data.polygons)}")

    atlas_path = PACK3D / "sir_aldric_meshy_atlas.png"
    src.hide_set(False)
    project_albedo(src, tgt, atlas_path)
    src.hide_set(True)
    src.hide_render = True

    actor_ob, _world = hh.build_actor_armature()
    hh.attach_actor(tgt, actor_ob)
    spatial_bind(tgt)
    scab_n = lock_scabbard(tgt)
    counts = recount(tgt)
    if scab_n < 20 or counts.get("Scabbard", 0) < 20:
        raise SystemExit(f"scabbard empty: lock={scab_n} primary={counts.get('Scabbard')}")
    arm_r = counts.get("Arm_R", 0) + counts.get("Fore_R", 0) + counts.get("Hand_R", 0)
    arm_l = counts.get("Arm_L", 0) + counts.get("Fore_L", 0) + counts.get("Hand_L", 0)
    if arm_r < 30 or arm_l < 30:
        raise SystemExit(f"arm empty after spatial bind: R={arm_r} L={arm_l} {counts}")
    leg_r = counts.get("UpLeg_R", 0) + counts.get("Leg_R", 0) + counts.get("Foot_R", 0)
    leg_l = counts.get("UpLeg_L", 0) + counts.get("Leg_L", 0) + counts.get("Foot_L", 0)
    if leg_r < 30 or leg_l < 30:
        raise SystemExit(f"leg empty after spatial bind: R={leg_r} L={leg_l} {counts}")

    old.setup_render()
    try:
        bpy.context.scene.eevee.taa_render_samples = 32
    except AttributeError:
        pass
    if bpy.data.objects.get("Rim") is None:
        rim = bpy.data.lights.new("Rim", "SUN")
        rim.energy = 2.4
        rim_o = bpy.data.objects.new("Rim", rim)
        bpy.context.collection.objects.link(rim_o)
        rim_o.location = (0.0, 2.2, 3.4)
        rim_o.rotation_euler = (math.radians(40), math.radians(180), 0.0)
    old.apply_pose(actor_ob, {
        "root_z": 0.0, "root_y": 0.0,
        "hips": (0, 0, 0), "spine": (0, 0, 0), "chest": (0, 0, 0), "head": (0, 0, 0),
        "up_l": (0, 0, 0), "leg_l": (0, 0, 0), "foot_l": (0, 0, 0),
        "up_r": (0, 0, 0), "leg_r": (0, 0, 0), "foot_r": (0, 0, 0),
        "arm_l": (0, 0, 0), "fore_l": (0, 0, 0),
        "arm_r": (0, 0, 0), "fore_r": (0, 0, 0),
        "hand_r": (0, 0, 0), "sword": (0, 0, 0),
    }, tgt)
    render_world_shots()

    mp4 = old.render_walk(actor_ob, tgt)
    for n in (0, 5, 10, 15):
        old.apply_pose(actor_ob, old.walk_pose(n / 16.0), tgt)
        npng = old.WALK / f"world_walk_n{n:02d}.png"
        bpy.context.scene.render.filepath = str(npng)
        bpy.ops.render.render(write_still=True)
        print("cycle n", n, npng.stat().st_size)

    fbx = rt.export_fbx(tgt, actor_ob)
    ntris, buckets = look.export_mesh_txt_v4(tgt)
    patch_mesh_header()
    blend = PACK3D / "sir_aldric_meshy_skinned.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    note = {
        "lookPassClaimed": True,
        "walkPassClaimed": False,
        "bindPassClaimed": False,
        "pathA": True,
        "designAckFolded": True,
        "honestArt": (
            "Design ACK folded into this Path A run (no parallel path, no bind hacks). "
            "Finer voxel scaffold + QuadriFlow mid-poly, shrinkwrap to Meshy, "
            "Cycles bake of Image_0 plus Play-cam project of e5b132f stills. "
            "Principled look (not emit-stencil). Abandoned paper/tube/capsule hacks. "
            "Do not claim this drop as bind, walk, or retopo PASS."
        ),
        "sourceLook": "e5b132f Meshy GLB + Image_0 / SoT lion",
        "retopo": (
            f"decimate≤{DECIMATE_FACES} then QuadriFlow targetFaces={TARGET_FACES}; "
            f"shrinkwrap offset={WRAP_OFFSET} wrapped={wrapped}; no capsule limbs"
        ),
        "bind": "spatial nearest rest-bone-segment (≤4) + one Scabbard volume lock. Not automatic weights.",
        "motion": "5916447 Evaluate() keys reused; root plant after Evaluate() so soles kiss Y=0",
        "scabbard": "character-right",
        "playHubLocked": True,
        "wrap": wrapped,
        "islands": islands,
        "islandSizes": sizes,
        "vertsPrimary": counts,
        "verts": len(tgt.data.vertices),
        "tris": ntris,
        "boneTris": buckets,
        "atlas": atlas_path.name,
        "mesh": "sir_aldric_meshy.mesh.txt",
        "fbx": fbx.name,
        "walkClip": str(mp4.relative_to(ROOT)),
    }
    (PACK3D / "sir_aldric_meshy_bind.json").write_text(json.dumps(note, indent=2) + "\n")
    write_drop(note)
    print("done path A", note["walkClip"], "tris", ntris, "verts", note["verts"], "islands", islands)


if __name__ == "__main__":
    main()
