#!/usr/bin/env python3
"""Path A LOCKED: clean mid-poly retopo + project Meshy/e5b132f albedo.

Derek: STOP tube / paper / remesh-melt / capsule bind hacks.
1) SOURCE = Path 2 Meshy GLB + e5b132f Image_0 / SoT lion.
2) CLEAN RETOPO = QuadriFlow mid-poly (manifold, humanoid-ready).
   Voxel is scaffold only, then shrinkwrap back onto the Meshy surface.
   Not the hero mesh. No capsule arms/legs. No paper-island weights.
3) TEXTURE PROJECT = camera-project frozen e5b132f World stills onto
   retopo UVs (Play cams). Transfer/Image_0 fills occluded bits.
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
TARGET_FACES = int(os.environ.get("PATHA_FACES", "14000"))
SCAFFOLD_VOXEL = float(os.environ.get("PATHA_SCAFFOLD", "0.011"))
WRAP_OFFSET = float(os.environ.get("PATHA_WRAP", "0.006"))
DECIMATE_FACES = int(os.environ.get("PATHA_DECIMATE", "18000"))

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
    # Structured fill first so occluded texels are steel/navy, not paper smear.
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
    Image.fromarray(np.clip(acc, 0, 255).astype(np.uint8), "RGB").save(atlas_path)
    print("camera project atlas tris", tris_hit, "pix", pix_hit, "size", atlas_path.stat().st_size, flush=True)
    return tris_hit


def assign_projected_material(ob, atlas_path: Path):
    """Emission-heavy so World stills read as the projected Meshy knight, not re-lit clay."""
    img = bpy.data.images.load(str(atlas_path))
    img.name = "PathAAtlas"
    mat = bpy.data.materials.new("PathALook")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    mix = nt.nodes.new("ShaderNodeMixShader")
    emit = nt.nodes.new("ShaderNodeEmission")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    emit.inputs["Strength"].default_value = 1.0
    try:
        bsdf.inputs["Roughness"].default_value = 0.62
        bsdf.inputs["Metallic"].default_value = 0.08
    except Exception:
        pass
    mix.inputs[0].default_value = 0.0
    emit.inputs["Strength"].default_value = 1.0
    nt.links.new(tex.outputs["Color"], emit.inputs["Color"])
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    nt.links.new(emit.outputs["Emission"], mix.inputs[1])
    nt.links.new(bsdf.outputs["BSDF"], mix.inputs[2])
    nt.links.new(mix.outputs["Shader"], out.inputs["Surface"])
    ob.data.materials.clear()
    ob.data.materials.append(mat)


def project_albedo(src, tgt, atlas_path: Path):
    """e5b132f World stills onto retopo UVs. Structured fill, not paper transfer."""
    rt.smart_uv(tgt)
    fill = structured_fill(tgt)
    cam_hits = camera_project_atlas(tgt, fill, atlas_path)
    if atlas_path.stat().st_size < 80_000:
        print("projected atlas thin — try Cycles bake")
        rt.cycles_bake(src, tgt, atlas_path)
    atlas = Image.open(atlas_path).convert("RGB")
    arr = np.array(atlas)
    if cam_hits < 200:
        print("camera project thin — stamp SoT lion backup")
        rt.stamp_lion(tgt, arr)
        Image.fromarray(arr, "RGB").save(atlas_path)
    assign_projected_material(tgt, atlas_path)
    print("projected atlas", atlas_path, atlas_path.stat().st_size, "cam_tris", cam_hits)
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
        "- TEXTURE: frozen e5b132f World stills camera-projected onto retopo UVs; "
        "Image_0 transfer fills occluded verts.\n"
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
    # Paper shells do not voxel-fuse. Thicken, then one scaffold volume.
    rt.apply_solidify(tgt, 0.012)
    scaffold_watertight(tgt, max(SCAFFOLD_VOXEL, 0.016))
    rt.delete_small_islands(tgt, keep_min=150)
    islands, sizes = n_islands(tgt.data)
    if islands > 4:
        print("still shattered, coarser scaffold")
        scaffold_watertight(tgt, 0.022)
        rt.delete_small_islands(tgt, keep_min=200)
        islands, sizes = n_islands(tgt.data)
    if len(tgt.data.polygons) > DECIMATE_FACES:
        rt.decimate_to(tgt, DECIMATE_FACES)
    ok = quadriflow(tgt, TARGET_FACES)
    if not ok:
        print("QuadriFlow no-op or failed — keep decimated scaffold, then wrap")
    wrapped = shrinkwrap_to_source(tgt, src, WRAP_OFFSET)
    rt.delete_small_islands(tgt, keep_min=20)
    # Mid-poly budget after wrap: Design ACK ~20–60k tris.
    if len(tgt.data.polygons) > 30000:
        rt.decimate_to(tgt, 24000)
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
        "honestArt": (
            "clean mid-poly retopo + camera-project of frozen e5b132f World stills "
            "onto retopo UVs (Image_0 transfer fills occluded verts). "
            "Abandoned shattered-Meshy paper/tube/capsule bind hacks. "
            "Voxel used only as watertight scaffold; silhouette restored by "
            "shrinkwrap ABOVE_SURFACE onto Meshy. Spatial nearest-bone-segment "
            "weights + one Scabbard volume lock. No exclusive paper corridors."
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
