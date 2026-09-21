#!/usr/bin/env python3
"""Escalation: remesh Path 2 look into a CLEAN mid-poly, then hang-skin FBX.

Old premise (abandoned): Bone1 / hang-heat weights on Meshy shatter (11283
islands). Failed the same three hard checks on 89481e5 / 71f0c4a / e56aeb1.

New premise: voxel-remesh Path 2 into connected manifold limbs + ONE solid
scabbard capsule (shattered hem/sheath shells deleted). Bake look albedos
from the look-PASS source. Proper hang armature (upper+fore+hand, legs).
Export FMT v4 mesh.txt + FBX. Evaluate() keys from 5916447 reused.

Walk-with-look is NOT claimed. Play hub PNG not swapped.
"""
from __future__ import annotations

import json
import math
import os
import sys
from collections import defaultdict, deque
from pathlib import Path

import bpy
import bmesh
import numpy as np
from mathutils import Vector
from mathutils.bvhtree import BVHTree
from PIL import Image, ImageFilter

sys.path.insert(0, str(Path(__file__).resolve().parent))
import skin_sir_aldric_hangheat as hh
import skin_sir_aldric_meshy as old

ROOT = old.ROOT
PACK3D = old.PACK3D
PROOF = old.PROOF
MESH_BONES = old.MESH_BONES
PARENT = old.PARENT

VOXEL = float(os.environ.get("RETOPO_VOXEL", "0.014"))
ATLAS_SIZE = 2048
ARM_PUSH = 0.022
WELD = 0.002
SOLIDIFY = 0.010
TARGET_FACES = 16000

_SCAB_A = hh._SCAB_A
_SCAB_B = hh._SCAB_B


def arm_axis(side: float, push: float):
    if side > 0:
        return (
            old._ARM_R_A + Vector((push, 0.0, 0.0)),
            old._ARM_R_B + Vector((push, 0.0, 0.0)),
        )
    return (
        old._ARM_L_A + Vector((-push, 0.0, 0.0)),
        old._ARM_L_B + Vector((-push, 0.0, 0.0)),
    )


def dist_seg(p, a, b):
    return hh.dist_seg(p, a, b)


def n_islands(me):
    n = len(me.vertices)
    adj = [[] for _ in range(n)]
    for e in me.edges:
        a, b = e.vertices
        adj[a].append(b)
        adj[b].append(a)
    seen = [False] * n
    sizes = []
    for i in range(n):
        if seen[i]:
            continue
        q = deque([i])
        seen[i] = True
        s = 0
        while q:
            v = q.popleft()
            s += 1
            for w in adj[v]:
                if not seen[w]:
                    seen[w] = True
                    q.append(w)
        sizes.append(s)
    sizes.sort(reverse=True)
    return len(sizes), sizes[:8]


def is_sheath(p: Vector) -> bool:
    return p.x > 0.13 and 0.20 < p.y < 1.20 and dist_seg(p, _SCAB_A, _SCAB_B) < 0.090


def in_arm(p: Vector, side: float, push: float = ARM_PUSH) -> bool:
    a, b = arm_axis(side, push)
    if p.x * side < 0.17:
        return False
    if not (0.58 < p.y < 1.52):
        return False
    return dist_seg(p, a, b) < 0.090


def delete_verts(ob, pred) -> int:
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.verts.ensure_lookup_table()
    drop = [v for v in bm.verts if pred(v.co)]
    n = len(drop)
    if drop:
        bmesh.ops.delete(bm, geom=drop, context="VERTS")
        bm.to_mesh(me)
    bm.free()
    me.update()
    return n


def delete_small_islands(ob, keep_min=40):
    me = ob.data
    n = len(me.vertices)
    adj = [[] for _ in range(n)]
    for e in me.edges:
        a, b = e.vertices
        adj[a].append(b)
        adj[b].append(a)
    seen = [False] * n
    drop = set()
    kept = []
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
        if len(ids) < keep_min:
            drop.update(ids)
        else:
            kept.append(len(ids))
    if drop:
        bm = bmesh.new()
        bm.from_mesh(me)
        bm.verts.ensure_lookup_table()
        bmesh.ops.delete(bm, geom=[bm.verts[i] for i in drop if i < len(bm.verts)], context="VERTS")
        bm.to_mesh(me)
        bm.free()
        me.update()
    print("islands keep", sorted(kept, reverse=True)[:8], "drop", len(drop))
    return kept


def fill_holes(ob):
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bmesh.ops.holes_fill(bm, edges=[e for e in bm.edges if e.is_boundary], sides=8)
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.update()
    print("holes", len(ob.data.vertices), "faces", len(ob.data.polygons), "islands", n_islands(ob.data))


def apply_solidify(ob, thick):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    m = ob.modifiers.new("sol", "SOLIDIFY")
    m.thickness = thick
    m.offset = 0.0
    bpy.ops.object.modifier_apply(modifier="sol")
    print("solidify", thick, "v", len(ob.data.vertices), "f", len(ob.data.polygons))


def decimate_to(ob, target_faces):
    n = max(1, len(ob.data.polygons))
    if n <= target_faces:
        return
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    m = ob.modifiers.new("dec", "DECIMATE")
    m.ratio = target_faces / n
    bpy.ops.object.modifier_apply(modifier="dec")
    for p in ob.data.polygons:
        p.use_smooth = True
    print("decimate", n, "->", len(ob.data.polygons), "v", len(ob.data.vertices), "islands", n_islands(ob.data))


def apply_voxel(ob, size):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    m = ob.modifiers.new("vox", "REMESH")
    m.mode = "VOXEL"
    m.voxel_size = size
    bpy.ops.object.modifier_apply(modifier="vox")
    for p in ob.data.polygons:
        p.use_smooth = True
    print("voxel", size, "v", len(ob.data.vertices), "f", len(ob.data.polygons), "islands", n_islands(ob.data))


def push_arms(ob, delta):
    me = ob.data
    n = 0
    for v in me.vertices:
        p = v.co
        if in_arm(p, 1.0, push=0.0):
            v.co.x += delta
            n += 1
        elif in_arm(p, -1.0, push=0.0):
            v.co.x -= delta
            n += 1
    me.update()
    print("arm push", delta, "verts", n)


def make_scabbard():
    """One solid brown/gold capsule on character-RIGHT. Not Meshy sheath shards."""
    a = Vector((0.215, 1.02, -0.025))
    b = Vector((0.355, 0.34, 0.045))
    axis = b - a
    mid = (a + b) * 0.5
    length = axis.length
    bpy.ops.mesh.primitive_cylinder_add(
        radius=0.020, depth=length, vertices=14, location=mid
    )
    body = bpy.context.active_object
    body.name = "ScabbardSolid"
    body.rotation_euler = axis.to_track_quat("Z", "X").to_euler()
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    # throat / chape gold volumes (same island after join+weld)
    throat_c = a.lerp(b, 0.06)
    bpy.ops.mesh.primitive_cylinder_add(
        radius=0.036, depth=0.055, vertices=14, location=throat_c
    )
    throat = bpy.context.active_object
    throat.rotation_euler = axis.to_track_quat("Z", "X").to_euler()
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    chape_c = a.lerp(b, 0.94)
    bpy.ops.mesh.primitive_cylinder_add(
        radius=0.028, depth=0.045, vertices=12, location=chape_c
    )
    chape = bpy.context.active_object
    chape.rotation_euler = axis.to_track_quat("Z", "X").to_euler()
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)

    bpy.ops.object.select_all(action="DESELECT")
    for o in (body, throat, chape):
        o.select_set(True)
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()
    ob = bpy.context.active_object
    apply_voxel(ob, 0.012)
    delete_small_islands(ob, keep_min=8)
    print("scabbard solid", len(ob.data.vertices), "faces", len(ob.data.polygons), "islands", n_islands(ob.data))
    return ob


def join_meshes(objs, name):
    bpy.ops.object.select_all(action="DESELECT")
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    ob = bpy.context.view_layer.objects.active
    ob.name = name
    return ob


def source_textures():
    """Save Path 2 Image_0 + lion cards for BVH sampling."""
    out_dir = Path("/tmp/path2-retopo")
    out_dir.mkdir(parents=True, exist_ok=True)
    imgs = {}
    for img in bpy.data.images:
        if img.size[0] < 64:
            continue
        p = out_dir / f"{img.name.replace('/', '_')}.png"
        img.filepath_raw = str(p)
        img.file_format = "PNG"
        try:
            img.save()
        except Exception:
            continue
        if p.exists():
            imgs[img.name] = np.array(Image.open(p).convert("RGB"))
    albedo = None
    for name, arr in imgs.items():
        if "Image_0" in name:
            albedo = arr
            break
    if albedo is None and imgs:
        albedo = next(iter(imgs.values()))
    lions = {n: a for n, a in imgs.items() if "lion" in n.lower()}
    print("src tex", list(imgs), "lions", list(lions))
    return albedo, lions, imgs


def mat_image_name(mat) -> str | None:
    if not mat or not mat.use_nodes:
        return None
    for n in mat.node_tree.nodes:
        if n.type == "TEX_IMAGE" and n.image:
            return n.image.name
    return None


def transfer_albedo(src, tgt) -> list[tuple[int, int, int]]:
    albedo, lions, imgs = source_textures()
    src_me = src.data
    bm = bmesh.new()
    bm.from_mesh(src_me)
    bm.faces.ensure_lookup_table()
    bm.verts.ensure_lookup_table()
    uv_lay = bm.loops.layers.uv.active
    bvh = BVHTree.FromBMesh(bm)
    mats = list(src_me.materials)
    colors = [(160, 160, 155)] * len(tgt.data.vertices)
    hits = miss = lion_n = 0
    for i, v in enumerate(tgt.data.vertices):
        loc, _n, idx, dist = bvh.find_nearest(v.co)
        if loc is None or idx is None or dist > 0.08:
            miss += 1
            continue
        face = bm.faces[idx]
        hits += 1
        # barycentric UV
        verts = [loop.vert.co for loop in face.loops]
        uvs = [loop[uv_lay].uv.copy() if uv_lay else Vector((0.5, 0.5)) for loop in face.loops]
        w = mathutils_bary(loc, verts)
        uv = Vector((0.0, 0.0))
        for wt, u in zip(w, uvs):
            uv += u * wt
        mat = mats[face.material_index] if face.material_index < len(mats) else None
        mname = mat.name if mat else ""
        iname = mat_image_name(mat)
        arr = None
        if mat and "lion" in mname.lower():
            if iname and iname in lions:
                arr = lions[iname]
            elif lions:
                arr = next(iter(lions.values()))
            lion_n += 1
        elif iname and iname in imgs:
            arr = imgs[iname]
        else:
            arr = albedo
        if arr is None:
            continue
        ah, aw = arr.shape[:2]
        x = int(max(0, min(aw - 1, float(uv.x) * aw)))
        y = int(max(0, min(ah - 1, (1.0 - float(uv.y)) * ah)))
        colors[i] = tuple(int(c) for c in arr[y, x])
    bm.free()
    print("transfer hits", hits, "miss", miss, "lion", lion_n)
    return colors


def mathutils_bary(p, verts):
    if len(verts) < 3:
        return [1.0] * len(verts)
    a, b, c = verts[0], verts[1], verts[2]
    v0, v1, v2 = b - a, c - a, p - a
    d00, d01, d11 = v0.dot(v0), v0.dot(v1), v1.dot(v1)
    d20, d21 = v2.dot(v0), v2.dot(v1)
    denom = d00 * d11 - d01 * d01
    if abs(denom) < 1e-12:
        return [1.0, 0.0, 0.0]
    v = (d11 * d20 - d01 * d21) / denom
    w = (d00 * d21 - d01 * d20) / denom
    u = 1.0 - v - w
    return [u, v, w]


def paint_scabbard_colors(ob, colors, scab_group):
    """Brown shaft + gold throat/chape. Do not sample shattered sheath."""
    g = ob.vertex_groups.get(scab_group)
    if g is None:
        return 0
    n = 0
    for i, v in enumerate(ob.data.vertices):
        assigned = False
        for vg in v.groups:
            if vg.group == g.index and vg.weight > 0.5:
                assigned = True
                break
        if not assigned:
            continue
        t = (v.co.y - 0.34) / max(1e-6, 1.02 - 0.34)
        if t > 0.88 or t < 0.10:
            colors[i] = (198, 156, 62)
        elif t > 0.78 or (0.18 < t < 0.26):
            colors[i] = (176, 132, 48)
        else:
            colors[i] = (92, 54, 28)
        n += 1
    print("scabbard paint", n)
    return n


def smart_uv(ob):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=66.0, island_margin=0.004)
    bpy.ops.object.mode_set(mode="OBJECT")


def rasterize_atlas(ob, colors, path: Path):
    me = ob.data
    me.calc_loop_triangles()
    uv = me.uv_layers.active
    s = ATLAS_SIZE
    arr = np.zeros((s, s, 3), np.uint8)
    weight = np.zeros((s, s), np.float32)
    for tri in me.loop_triangles:
        pts = []
        cols = []
        for vi, li in zip(tri.vertices, tri.loops):
            u, v = uv.data[li].uv
            pts.append((float(u) * (s - 1), (1.0 - float(v)) * (s - 1)))
            cols.append(np.array(colors[vi], np.float32))
        raster_tri(arr, weight, pts, cols)
    # dilate bleed
    filled = weight > 0
    img = Image.fromarray(arr, "RGB")
    for _ in range(8):
        dil = img.filter(ImageFilter.MaxFilter(3))
        darr = np.array(dil)
        arr = np.where(filled[..., None], arr, darr)
        img = Image.fromarray(arr, "RGB")
        # grow filled via max on mask
        mask = Image.fromarray((filled.astype(np.uint8) * 255), "L")
        mask = mask.filter(ImageFilter.MaxFilter(3))
        filled = np.array(mask) > 0
    # leftover empty → mid grey
    arr = np.where(filled[..., None], arr, np.array([150, 148, 140], np.uint8))
    Image.fromarray(arr, "RGB").save(path)
    print("atlas", path, path.stat().st_size)
    return path


def raster_tri(arr, weight, pts, cols):
    (x0, y0), (x1, y1), (x2, y2) = pts
    minx = max(0, int(math.floor(min(x0, x1, x2))))
    maxx = min(arr.shape[1] - 1, int(math.ceil(max(x0, x1, x2))))
    miny = max(0, int(math.floor(min(y0, y1, y2))))
    maxy = min(arr.shape[0] - 1, int(math.ceil(max(y0, y1, y2))))
    denom = (y1 - y2) * (x0 - x2) + (x2 - x1) * (y0 - y2)
    if abs(denom) < 1e-8:
        return
    for y in range(miny, maxy + 1):
        py = y + 0.5
        for x in range(minx, maxx + 1):
            px = x + 0.5
            w0 = ((y1 - y2) * (px - x2) + (x2 - x1) * (py - y2)) / denom
            w1 = ((y2 - y0) * (px - x2) + (x0 - x2) * (py - y2)) / denom
            w2 = 1.0 - w0 - w1
            if w0 < -0.01 or w1 < -0.01 or w2 < -0.01:
                continue
            col = w0 * cols[0] + w1 * cols[1] + w2 * cols[2]
            arr[y, x] = np.clip(col, 0, 255).astype(np.uint8)
            weight[y, x] = 1.0


LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
LION_REAR_BOX = (448, 258, 588, 400)


def scale_uvs(ob, v_max=0.74):
    uv = ob.data.uv_layers.active.data
    for loop in uv:
        loop.uv = Vector((float(loop.uv.x), float(loop.uv.y) * v_max))


def cycles_bake(src, tgt, path: Path):
    img = bpy.data.images.new("RetopoBake", ATLAS_SIZE, ATLAS_SIZE, alpha=False)
    mat = bpy.data.materials.new("RetopoBakeMat")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    if bsdf:
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    tgt.data.materials.clear()
    tgt.data.materials.append(mat)
    nt.nodes.active = tex
    tex.select = True

    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = 4
    sc.cycles.bake_type = "DIFFUSE"
    bake = sc.render.bake
    bake.use_pass_direct = False
    bake.use_pass_indirect = False
    bake.use_pass_color = True
    bake.use_selected_to_active = True
    bake.cage_extrusion = 0.07
    bake.margin = 8
    bake.use_clear = True

    src.hide_set(False)
    src.hide_render = False
    bpy.ops.object.select_all(action="DESELECT")
    src.select_set(True)
    tgt.select_set(True)
    bpy.context.view_layer.objects.active = tgt
    try:
        bpy.ops.object.bake(type="DIFFUSE", pass_filter={"COLOR"})
    except Exception as exc:
        print("cycles bake failed", exc)
        return None
    img.filepath_raw = str(path)
    img.file_format = "PNG"
    img.save()
    print("cycles bake", path, path.stat().st_size)
    return path


def make_lion_card() -> Image.Image:
    rear = Image.open(LOOK / "01_rear_LOCKED.png").convert("RGB")
    x0, y0, x1, y1 = LION_REAR_BOX
    spr = rear.crop((x0, y0, x1, y1))
    card = Image.new("RGB", (512, 480), (36, 58, 118))
    sw, sh = spr.size
    scale = min(card.size[0] * 0.90 / sw, card.size[1] * 0.90 / sh)
    nw, nh = max(1, int(sw * scale)), max(1, int(sh * scale))
    spr = spr.resize((nw, nh), Image.LANCZOS)
    card.paste(spr, ((card.size[0] - nw) // 2, (card.size[1] - nh) // 2))
    return card


def stamp_lion(ob, atlas: np.ndarray):
    """Planar-remap upper-back faces onto a reserved top strip; paste SoT lion."""
    me = ob.data
    uv = me.uv_layers.active.data
    back = []
    for poly in me.polygons:
        c = poly.center
        n = poly.normal
        if n.z < -0.25 and 1.06 < c.y < 1.42 and abs(c.x) < 0.16:
            back.append(poly)
    print("lion back faces", len(back))
    if len(back) < 6:
        return 0
    # reserved strip: blender v 0.76..0.99, u 0.04..0.30
    u0, u1, v0, v1 = 0.04, 0.30, 0.76, 0.99
    xs, ys = [], []
    for poly in back:
        for vi in poly.vertices:
            p = me.vertices[vi].co
            xs.append(p.x)
            ys.append(p.y)
    xmin, xmax = min(xs), max(xs)
    ymin, ymax = min(ys), max(ys)
    for poly in back:
        for li, vi in zip(poly.loop_indices, poly.vertices):
            p = me.vertices[vi].co
            u = u0 + (p.x - xmin) / max(1e-6, xmax - xmin) * (u1 - u0)
            v = v0 + (p.y - ymin) / max(1e-6, ymax - ymin) * (v1 - v0)
            uv[li].uv = Vector((u, v))
    card = make_lion_card()
    s = atlas.shape[0]
    # blender v=0 bottom → PNG y = (1-v)*s
    y_top = int((1.0 - v1) * s)
    y_bot = int((1.0 - v0) * s)
    x_l = int(u0 * s)
    x_r = int(u1 * s)
    slot = card.resize((max(1, x_r - x_l), max(1, y_bot - y_top)), Image.LANCZOS)
    atlas[y_top:y_bot, x_l:x_r] = np.array(slot)
    print("lion stamp", x_l, y_top, x_r, y_bot)
    return len(back)


def paint_scabbard_atlas(ob, atlas: np.ndarray, scab_group="_ScabGeo"):
    me = ob.data
    g = ob.vertex_groups.get(scab_group) or ob.vertex_groups.get("Scabbard")
    if g is None or me.uv_layers.active is None:
        return 0
    me.calc_loop_triangles()
    uv = me.uv_layers.active
    s = atlas.shape[0]
    n = 0
    for tri in me.loop_triangles:
        ids = list(tri.vertices)
        ok = 0
        for vi in ids:
            for vg in me.vertices[vi].groups:
                if vg.group == g.index and vg.weight > 0.4:
                    ok += 1
                    break
        if ok < 2:
            continue
        pts, cols = [], []
        for vi, li in zip(ids, tri.loops):
            u, v = uv.data[li].uv
            pts.append((float(u) * (s - 1), (1.0 - float(v)) * (s - 1)))
            t = (me.vertices[vi].co.y - 0.34) / 0.70
            if t > 0.88 or t < 0.10:
                cols.append(np.array([198, 156, 62], np.float32))
            else:
                cols.append(np.array([92, 54, 28], np.float32))
        w = np.zeros((s, s), np.float32)
        raster_tri(atlas, w, pts, cols)
        n += 1
    print("scabbard atlas tris", n)
    return n


def assign_baked_material(ob, atlas_path: Path):
    img = bpy.data.images.load(str(atlas_path))
    img.name = "RetopoAtlas"
    mat = bpy.data.materials.new("RetopoLook")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    if bsdf:
        nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
        try:
            bsdf.inputs["Roughness"].default_value = 0.55
            bsdf.inputs["Metallic"].default_value = 0.12
        except Exception:
            pass
    ob.data.materials.clear()
    ob.data.materials.append(mat)


def tag_scabbard_group(body, scab):
    """Vertex group on scabbard object before join; survives join."""
    g = scab.vertex_groups.new(name="_ScabGeo")
    g.add(list(range(len(scab.data.vertices))), 1.0, "REPLACE")
    # body gets empty group so join keeps the name
    if "_ScabGeo" not in {x.name for x in body.vertex_groups}:
        body.vertex_groups.new(name="_ScabGeo")


def split_limb_chains(mesh_ob):
    """Proper upper / fore / hand and up-leg / shin / foot on solid volumes."""
    me = mesh_ob.data
    groups = {g.name: g for g in mesh_ob.vertex_groups}
    for n in MESH_BONES:
        if n not in groups:
            groups[n] = mesh_ob.vertex_groups.new(name=n)
    scab = groups.get("_ScabGeo")
    ar_a, ar_b = arm_axis(1.0, ARM_PUSH)
    al_a, al_b = arm_axis(-1.0, ARM_PUSH)
    n_arm = n_leg = 0
    for i, v in enumerate(me.vertices):
        if scab:
            skip = False
            for vg in v.groups:
                if vg.group == scab.index and vg.weight > 0.5:
                    skip = True
                    break
            if skip:
                continue
        p = v.co
        # cloth / tabard never on limbs
        if abs(p.x) < 0.195 and 0.48 < p.y < 1.22 and abs(p.z) < 0.24:
            continue
        if in_arm(p, 1.0) or (p.x > 0.20 and 0.58 < p.y < 1.50 and dist_seg(p, ar_a, ar_b) < 0.11):
            for name in ("Arm_R", "Fore_R", "Hand_R", "Arm_L", "Fore_L", "Hand_L"):
                groups[name].remove([i])
            if p.y >= 1.16:
                groups["Arm_R"].add([i], 1.0, "REPLACE")
                if p.y < 1.22:
                    groups["Fore_R"].add([i], 0.35, "ADD")
            elif p.y >= 0.88:
                groups["Fore_R"].add([i], 1.0, "REPLACE")
                if p.y > 1.10:
                    groups["Arm_R"].add([i], 0.35, "ADD")
                if p.y < 0.94:
                    groups["Hand_R"].add([i], 0.35, "ADD")
            else:
                groups["Hand_R"].add([i], 1.0, "REPLACE")
                if p.y > 0.82:
                    groups["Fore_R"].add([i], 0.30, "ADD")
            n_arm += 1
            continue
        if in_arm(p, -1.0) or (p.x < -0.20 and 0.58 < p.y < 1.50 and dist_seg(p, al_a, al_b) < 0.11):
            for name in ("Arm_R", "Fore_R", "Hand_R", "Arm_L", "Fore_L", "Hand_L"):
                groups[name].remove([i])
            if p.y >= 1.16:
                groups["Arm_L"].add([i], 1.0, "REPLACE")
                if p.y < 1.22:
                    groups["Fore_L"].add([i], 0.35, "ADD")
            elif p.y >= 0.88:
                groups["Fore_L"].add([i], 1.0, "REPLACE")
                if p.y > 1.10:
                    groups["Arm_L"].add([i], 0.35, "ADD")
                if p.y < 0.94:
                    groups["Hand_L"].add([i], 0.35, "ADD")
            else:
                groups["Hand_L"].add([i], 1.0, "REPLACE")
                if p.y > 0.82:
                    groups["Fore_L"].add([i], 0.30, "ADD")
            n_arm += 1
            continue
        # legs: outside the tabard, below hips
        if p.y < 0.96 and abs(p.x) > 0.045 and not is_sheath(p):
            side = "R" if p.x > 0 else "L"
            for name in (f"UpLeg_{side}", f"Leg_{side}", f"Foot_{side}"):
                groups[name].remove([i])
            if p.y >= 0.54:
                groups[f"UpLeg_{side}"].add([i], 1.0, "REPLACE")
                if p.y < 0.62:
                    groups[f"Leg_{side}"].add([i], 0.30, "ADD")
            elif p.y >= 0.22:
                groups[f"Leg_{side}"].add([i], 1.0, "REPLACE")
                if p.y > 0.48:
                    groups[f"UpLeg_{side}"].add([i], 0.30, "ADD")
                if p.y < 0.28:
                    groups[f"Foot_{side}"].add([i], 0.30, "ADD")
            else:
                groups[f"Foot_{side}"].add([i], 1.0, "REPLACE")
            n_leg += 1
    print("split limbs arm", n_arm, "leg", n_leg)


def lock_cloth_and_scabbard(mesh_ob):
    me = mesh_ob.data
    groups = {g.name: g for g in mesh_ob.vertex_groups}
    for n in MESH_BONES:
        if n not in groups:
            groups[n] = mesh_ob.vertex_groups.new(name=n)
    scab_g = groups.get("_ScabGeo")
    limb = {"Arm_L", "Fore_L", "Hand_L", "Arm_R", "Fore_R", "Hand_R",
            "UpLeg_L", "Leg_L", "Foot_L", "UpLeg_R", "Leg_R", "Foot_R", "Scabbard"}
    n_cloth = n_scab = 0
    for i, v in enumerate(me.vertices):
        p = v.co
        on_scab = False
        if scab_g:
            for vg in v.groups:
                if vg.group == scab_g.index and vg.weight > 0.5:
                    on_scab = True
                    break
        if not on_scab:
            on_scab = is_sheath(p) and dist_seg(p, _SCAB_A, _SCAB_B) < 0.055
        if on_scab:
            for g in mesh_ob.vertex_groups:
                if g.name != "_ScabGeo":
                    g.remove([i])
            groups["Scabbard"].add([i], 1.0, "REPLACE")
            n_scab += 1
            continue
        # tabard / hem / lion cloth — torso only (kills mid tear bands)
        cloth = (
            abs(p.x) < 0.200
            and 0.46 < p.y < 1.24
            and abs(p.z) < 0.26
            and not in_arm(p, 1.0)
            and not in_arm(p, -1.0)
        )
        if cloth:
            for name in limb:
                if name in groups:
                    groups[name].remove([i])
            if p.y > 1.16:
                groups["Chest"].add([i], 1.0, "ADD")
            elif p.y > 1.04:
                groups["Spine"].add([i], 1.0, "ADD")
            else:
                groups["Hips"].add([i], 1.0, "ADD")
            n_cloth += 1
    print("lock cloth", n_cloth, "scabbard", n_scab)
    if scab_g:
        mesh_ob.vertex_groups.remove(scab_g)
    return n_cloth, n_scab


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
    meta = PACK3D / "sir_aldric_path2_clean.fbx.meta"
    if not meta.exists():
        meta.write_text(
            "fileFormatVersion: 2\n"
            "guid: d3f61e0000000000000000000000cc04\n"
            "DefaultImporter:\n"
            "  externalObjects: {}\n"
            "  userData: \n"
            "  assetBundleName: \n"
            "  assetBundleVariant: \n"
        )
    return path


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
        "# SirAldric Path 2 retopo+skin  clay-volume-law 5de0e16  scabbard=+X  blender",
        "FMT v4 blender gate3 meshy-path2 retopo-skin",
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
    if not old.SRC_GLB.exists():
        raise SystemExit(f"missing {old.SRC_GLB}")
    src = hh.import_raw()
    src.name = "Path2Source"
    # working copy
    tgt = src.copy()
    tgt.data = src.data.copy()
    bpy.context.collection.objects.link(tgt)
    tgt.name = "SirAldricRetopo"
    dropped = delete_verts(tgt, is_sheath)
    print("deleted sheath shards", dropped)
    hh.weld(tgt, WELD)
    fill_holes(tgt)
    apply_solidify(tgt, SOLIDIFY)
    push_arms(tgt, ARM_PUSH)
    apply_voxel(tgt, VOXEL)
    delete_small_islands(tgt, keep_min=80)
    decimate_to(tgt, TARGET_FACES)
    # leave arms pushed — readable solid hang volumes, not fused to tabard

    scab = make_scabbard()
    tag_scabbard_group(tgt, scab)
    mesh_ob = join_meshes([tgt, scab], "SirAldricRetopo")
    print("joined", len(mesh_ob.data.vertices), "faces", len(mesh_ob.data.polygons), "islands", n_islands(mesh_ob.data))

    smart_uv(mesh_ob)
    scale_uvs(mesh_ob, 0.74)
    atlas_path = PACK3D / "sir_aldric_meshy_atlas.png"
    baked = cycles_bake(src, mesh_ob, atlas_path)
    bake_ok = baked is not None and atlas_path.exists()
    if bake_ok:
        preview = np.array(Image.open(atlas_path).convert("RGB"))
        bake_ok = float(preview.mean()) > 18
        print("bake mean", round(float(preview.mean()), 2))
    if not bake_ok:
        colors = transfer_albedo(src, mesh_ob)
        paint_scabbard_colors(mesh_ob, colors, "_ScabGeo")
        rasterize_atlas(mesh_ob, colors, atlas_path)
    atlas = np.array(Image.open(atlas_path).convert("RGB"))
    stamp_lion(mesh_ob, atlas)
    paint_scabbard_atlas(mesh_ob, atlas)
    Image.fromarray(atlas, "RGB").save(atlas_path)
    assign_baked_material(mesh_ob, atlas_path)

    # hide / remove source so the walk render is the clean mesh only
    src.hide_render = True
    src.hide_set(True)

    heat_ob = hh.build_heat_armature()
    hh.heat_bind(mesh_ob, heat_ob)
    split_limb_chains(mesh_ob)
    n_cloth, n_scab = lock_cloth_and_scabbard(mesh_ob)
    counts = hh.primary_counts(mesh_ob)
    if counts.get("Scabbard", 0) < 40:
        raise SystemExit(f"scabbard too few: {counts.get('Scabbard')} primary={counts}")
    if counts.get("Fore_R", 0) < 8 or counts.get("Fore_L", 0) < 8:
        raise SystemExit(f"elbow empty: Fore_R={counts.get('Fore_R')} Fore_L={counts.get('Fore_L')}")
    if counts.get("Arm_R", 0) < 20 or counts.get("Arm_L", 0) < 20:
        raise SystemExit(f"arm empty: R={counts.get('Arm_R')} L={counts.get('Arm_L')}")

    actor_ob, world = hh.build_actor_armature()
    hh.attach_actor(mesh_ob, actor_ob)
    # drop heat armature from view
    heat_ob.hide_render = True
    heat_ob.hide_set(True)

    fbx = export_fbx(mesh_ob, actor_ob)
    ntris, buckets = export_mesh_txt_v4(mesh_ob)

    old.setup_render()
    mp4 = old.render_walk(actor_ob)

    blend = PACK3D / "sir_aldric_meshy_skinned.blend"
    bpy.ops.wm.save_as_mainfile(filepath=str(blend))
    islands, sizes = n_islands(mesh_ob.data)
    note = {
        "lookPassClaimed": True,
        "walkPassClaimed": False,
        "oldPremise": "weights on Meshy shatter (11283 islands) — Bone1 89481e5/71f0c4a then hang-heat e56aeb1. Abandoned.",
        "newPremise": "retopo/remesh Path 2 look into clean mid-poly (voxel) + one solid scabbard capsule + hang armature skin FBX. Look bake from e5b132f source.",
        "voxel": VOXEL,
        "armPush": ARM_PUSH,
        "islands": islands,
        "islandSizes": sizes,
        "sheathShardsDeleted": dropped,
        "clothLocked": n_cloth,
        "scabbardVerts": n_scab,
        "motion": "5916447 Evaluate() keys reused — gait/weave/forward-swing not edited",
        "scabbard": "character-right",
        "playHubLocked": True,
        "vertsPrimary": counts,
        "verts": len(mesh_ob.data.vertices),
        "tris": ntris,
        "boneTris": buckets,
        "atlas": atlas_path.name,
        "mesh": "sir_aldric_meshy.mesh.txt",
        "fbx": fbx.name,
        "format": "FMT v4 object-space + 4 named weights",
        "walkClip": str(mp4.relative_to(ROOT)),
    }
    (PACK3D / "sir_aldric_meshy_bind.json").write_text(json.dumps(note, indent=2) + "\n")
    print("done retopo", note["walkClip"], "tris", ntris, "islands", islands)


if __name__ == "__main__":
    main()
