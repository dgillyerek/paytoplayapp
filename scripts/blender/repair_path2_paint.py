#!/usr/bin/env python3
"""Path 2 Meshy paint FAIL iterate: UV island bleed + crisp SoT lion.

LOOK stills pipeline only. Does not skin to walk / Animator / Play hub.
Does NOT claim look PASS. Exports patched GLB in original Meshy axes
(so import_sir_aldric_meshy.py remap stays valid).
"""
from __future__ import annotations

import json
from collections import defaultdict, deque
from pathlib import Path

import bpy
import bmesh
import numpy as np
from mathutils import Vector
from PIL import Image, ImageDraw, ImageFilter, ImageEnhance

ROOT = Path(__file__).resolve().parents[2]
SRC_GLB = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/AI_MESH_PATH2/out/aldric_meshy_retopo.glb"
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
OUT_GLB = PACK3D / "sir_aldric_meshy_retopo.glb"
OUT_GLB_DESIGN = SRC_GLB
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
GATE = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/TURNAROUND_GATE1/LOCKED"
WORKDIR = Path("/tmp/path2-paint")
WORKDIR.mkdir(parents=True, exist_ok=True)
TARGET_H = 1.86

# Tight SoT lion boxes (gold rampant only; pauldrons/belt cropped).
LION_REAR_BOX = (448, 258, 588, 400)  # 01_rear_LOCKED 1024²
LION_FRONT_BOX = (332, 308, 436, 416)  # 01_FRONT 768×1024


def import_and_orient():
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
    me.update()
    bm = bmesh.new()
    bm.from_mesh(me)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(me)
    bm.free()
    ys = [v.co.y for v in me.vertices]
    ymin, ymax = min(ys), max(ys)
    scale = TARGET_H / (ymax - ymin)
    for v in me.vertices:
        v.co = Vector((v.co.x * scale, (v.co.y - ymin) * scale, v.co.z * scale))
    me.update()
    for p in me.polygons:
        p.use_smooth = True
    # Split-normal seams read as dark cracks at island borders.
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    try:
        bpy.ops.mesh.customdata_custom_splitnormals_clear()
    except Exception as exc:
        print("split-normal clear skipped", exc)
    bpy.ops.object.mode_set(mode="OBJECT")
    return ob


def blender_images():
    out = {}
    for img in bpy.data.images:
        if img.size[0] >= 1024:
            path = WORKDIR / f"{img.name.replace('/', '_')}.png"
            img.filepath_raw = str(path)
            img.file_format = "PNG"
            img.save()
            out[img.name] = (img, path)
            print("saved", img.name, img.size[:], path)
    return out


def png_rgb(path: Path) -> np.ndarray:
    return np.array(Image.open(path).convert("RGB"))


def extract_lion_sprite(src: Path, box) -> Image.Image:
    im = Image.open(src).convert("RGBA").crop(box)
    arr = np.array(im)
    r, g, b, a = [arr[:, :, i] for i in range(4)]
    gold = (r > 118) & (g > 88) & (r > b + 10) & (a > 16)
    gold = gold | np.pad(gold, ((1, 0), (0, 0)), mode="constant")[:-1, :]
    gold = gold | np.pad(gold, ((0, 1), (0, 0)), mode="constant")[1:, :]
    gold = gold | np.pad(gold, ((0, 0), (1, 0)), mode="constant")[:, :-1]
    gold = gold | np.pad(gold, ((0, 0), (0, 1)), mode="constant")[:, 1:]
    # keep a 1px navy fringe so the stitch isn't a cutout
    blue = (b > r + 8) & (b > 50) & (r < 100)
    fringe = gold.copy()
    for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
        fringe = fringe | np.roll(np.roll(gold, dy, 0), dx, 1)
    keep = gold | (fringe & blue)
    out = arr.copy()
    out[..., 3] = np.where(keep, 255, 0).astype(np.uint8)
    out[~gold & keep, 3] = 160
    sprite = Image.fromarray(out, "RGBA")
    sprite = ImageEnhance.Contrast(sprite).enhance(1.18)
    sprite = sprite.filter(ImageFilter.UnsharpMask(radius=1.4, percent=130, threshold=2))
    sprite.save(WORKDIR / f"lion_sprite_{src.stem}.png")
    return sprite


def uv_islands(me):
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.faces.ensure_lookup_table()
    uv_layer = bm.loops.layers.uv.active
    adj = defaultdict(set)
    for e in bm.edges:
        if len(e.link_faces) != 2:
            continue
        f0, f1 = e.link_faces
        u0 = {l.vert.index: l[uv_layer].uv.copy() for l in f0.loops}
        u1 = {l.vert.index: l[uv_layer].uv.copy() for l in f1.loops}
        shared = set(u0) & set(u1)
        if len(shared) < 2:
            continue
        if all((u0[vi] - u1[vi]).length < 1e-5 for vi in shared):
            adj[f0.index].add(f1.index)
            adj[f1.index].add(f0.index)
    seen = {}
    iid = 0
    for f in bm.faces:
        if f.index in seen:
            continue
        q = deque([f.index])
        seen[f.index] = iid
        while q:
            cur = q.popleft()
            for n in adj[cur]:
                if n not in seen:
                    seen[n] = iid
                    q.append(n)
        iid += 1
    bm.free()
    return seen, iid


def raster_occupancy(me, w, h) -> np.ndarray:
    im = Image.new("L", (w, h), 0)
    d = ImageDraw.Draw(im)
    uv = me.uv_layers.active.data
    for poly in me.polygons:
        pts = []
        for li in poly.loop_indices:
            u, v = uv[li].uv
            pts.append((u * (w - 1), (1.0 - v) * (h - 1)))
        if len(pts) >= 3:
            d.polygon(pts, fill=255)
    return np.array(im) > 0


def dilate_nn(color: np.ndarray, occ: np.ndarray, steps: int = 14) -> np.ndarray:
    """Nearest-neighbour bleed of occupied texels into empty atlas gaps."""
    out = color.copy()
    m = occ.copy()
    h, w = m.shape
    for i in range(steps):
        if m.all():
            break
        shifted = [
            (np.roll(out, 1, 0), np.roll(m, 1, 0)),
            (np.roll(out, -1, 0), np.roll(m, -1, 0)),
            (np.roll(out, 1, 1), np.roll(m, 1, 1)),
            (np.roll(out, -1, 1), np.roll(m, -1, 1)),
        ]
        new = out.copy()
        newm = m.copy()
        for c, om in shifted:
            take = (~newm) & om
            new[take] = c[take]
            newm |= take
        # do not wrap across atlas edges
        newm[0, :] = m[0, :]
        newm[-1, :] = m[-1, :]
        newm[:, 0] = m[:, 0]
        newm[:, -1] = m[:, -1]
        out, m = new, newm
        if i in (0, 3, 7, steps - 1):
            print("bleed step", i + 1, "filled", int(m.sum()), "/", m.size)
    return out


def inpaint_dark_borders(color: np.ndarray, occ: np.ndarray) -> np.ndarray:
    """Replace near-black island-border texels (baked crack lines)."""
    lum = color.astype(np.float32)
    lum = 0.30 * lum[:, :, 0] + 0.59 * lum[:, :, 1] + 0.11 * lum[:, :, 2]
    occ_u8 = occ.astype(np.uint8) * 255
    er = np.array(Image.fromarray(occ_u8, "L").filter(ImageFilter.MinFilter(5))) > 0
    border = occ & (~er)
    dark = border & (lum < 28.0)
    if not dark.any():
        print("dark-border texels 0")
        return color
    # copy from eroded interior via rolls
    out = color.copy()
    src_m = er
    src_c = color.copy()
    m = src_m.copy()
    filled = dark.copy()
    for _ in range(8):
        for dy, dx in ((-1, 0), (1, 0), (0, -1), (0, 1)):
            c = np.roll(np.roll(src_c, dy, 0), dx, 1)
            om = np.roll(np.roll(m, dy, 0), dx, 1)
            take = filled & om
            out[take] = c[take]
            filled[take] = False
        if not filled.any():
            break
    print("dark-border inpainted", int(dark.sum()) - int(filled.sum()), "of", int(dark.sum()))
    return out


def sample_rgb(albedo, u, v):
    h, w = albedo.shape[:2]
    x = int(np.clip(u, 0.0, 0.999) * w)
    y = int(np.clip(1.0 - v, 0.0, 0.999) * h)
    return albedo[y, x]


def is_cloth(rgb) -> bool:
    r, g, b = (int(c) for c in rgb)
    if b > r + 8 and b > 40 and r < 110:
        return True
    if r > 110 and g > 80 and r > b + 8 and r > g - 10:
        return True
    return False


def is_gold(rgb) -> bool:
    r, g, b = (int(c) for c in rgb)
    return r > 118 and g > 80 and r > b + 10


def collect_panel_faces(me, albedo, islands, side: str):
    """Upper-torso cloth only. No UV-island flood (that spanned the atlas)."""
    uv = me.uv_layers.active.data
    faces = []
    for poly in me.polygons:
        c = Vector((0, 0, 0))
        cloth = 0
        nloop = 0
        for li, vi in zip(poly.loop_indices, poly.vertices):
            c += me.vertices[vi].co
            rgb = sample_rgb(albedo, uv[li].uv.x, uv[li].uv.y)
            nloop += 1
            if is_cloth(rgb):
                cloth += 1
        c /= nloop
        n = poly.normal
        if not (1.12 < c.y < 1.36):
            continue
        if abs(c.x) > 0.16:
            continue
        if abs(c.z) > 0.17:
            continue
        if cloth < max(1, nloop // 2):
            continue
        if side == "back" and n.z > -0.32:
            continue
        if side == "front" and n.z < 0.55:
            continue
        faces.append(poly.index)
    print(side, "panel faces", len(faces))
    return faces


def uv_bbox(me, face_ids):
    uv = me.uv_layers.active.data
    us, vs = [], []
    for fi in face_ids:
        for li in me.polygons[fi].loop_indices:
            u, v = uv[li].uv
            us.append(u)
            vs.append(v)
    return min(us), min(vs), max(us), max(vs)


def xyz_bbox(me, face_ids):
    xs, ys, zs = [], [], []
    for fi in face_ids:
        for vi in me.polygons[fi].vertices:
            p = me.vertices[vi].co
            xs.append(p.x)
            ys.append(p.y)
            zs.append(p.z)
    return min(xs), min(ys), min(zs), max(xs), max(ys), max(zs)


def assign_planar_uv(me, face_ids, uv_rect, side: str):
    """Planar X/Y unwrap into uv_rect.

    Front Play-cam right = −X, so u = x already puts SoT-image-right
    (lion head) on the viewer's right. Do not flip front U.
    """
    u0, v0, u1, v1 = uv_rect
    xmin, ymin, _zmin, xmax, ymax, _zmax = xyz_bbox(me, face_ids)
    xpad = (xmax - xmin) * 0.08
    ypad = (ymax - ymin) * 0.08
    xmin -= xpad
    xmax += xpad
    ymin -= ypad
    ymax += ypad
    uv = me.uv_layers.active.data
    for fi in face_ids:
        for li, vi in zip(me.polygons[fi].loop_indices, me.polygons[fi].vertices):
            p = me.vertices[vi].co
            tx = (p.x - xmin) / max(1e-6, xmax - xmin)
            ty = (p.y - ymin) / max(1e-6, ymax - ymin)
            uv[li].uv = Vector((u0 + tx * (u1 - u0), v0 + ty * (v1 - v0)))
    return (xmin, ymin, xmax, ymax)


def raster_lion_panel(albedo, me, face_ids, sprite: Image.Image, navy, xyz_range, side: str, uv_rect):
    """Paint navy + SoT lion into the panel using 3D planar mapping."""
    h, w = albedo.shape[:2]
    xmin, ymin, xmax, ymax = xyz_range
    spr = sprite.convert("RGBA")
    sa = np.array(spr)
    sh, sw = sa.shape[:2]
    uv = me.uv_layers.active.data
    # PIL polygon fill per triangle → then map pixels via UV inverse to 3D.
    # Faster: fill UV triangles with barycentric 3D, then sample sprite from X/Y.
    painted = 0
    gold_px = 0
    for fi in face_ids:
        poly = me.polygons[fi]
        loops = list(zip(poly.loop_indices, poly.vertices))
        if len(loops) < 3:
            continue
        def vert(k):
            li, vi = loops[k]
            u, v = uv[li].uv
            p = me.vertices[vi].co
            return np.array([u * (w - 1), (1.0 - v) * (h - 1), p.x, p.y], np.float64)

        v0 = vert(0)
        for i in range(1, len(loops) - 1):
            v1 = vert(i)
            v2 = vert(i + 1)
            pts = np.stack([v0, v1, v2])
            minx = max(0, int(np.floor(pts[:, 0].min())))
            maxx = min(w - 1, int(np.ceil(pts[:, 0].max())))
            miny = max(0, int(np.floor(pts[:, 1].min())))
            maxy = min(h - 1, int(np.ceil(pts[:, 1].max())))
            if maxx <= minx or maxy <= miny:
                continue
            xs = np.arange(minx, maxx + 1)
            ys = np.arange(miny, maxy + 1)
            XX, YY = np.meshgrid(xs, ys)
            # barycentric in pixel space
            ax, ay = pts[0, 0], pts[0, 1]
            bx, by = pts[1, 0], pts[1, 1]
            cx, cy = pts[2, 0], pts[2, 1]
            denom = (by - cy) * (ax - cx) + (cx - bx) * (ay - cy)
            if abs(denom) < 1e-8:
                continue
            w0 = ((by - cy) * (XX - cx) + (cx - bx) * (YY - cy)) / denom
            w1 = ((cy - ay) * (XX - cx) + (ax - cx) * (YY - cy)) / denom
            w2 = 1.0 - w0 - w1
            inside = (w0 >= -0.01) & (w1 >= -0.01) & (w2 >= -0.01)
            if not inside.any():
                continue
            px = w0 * pts[0, 2] + w1 * pts[1, 2] + w2 * pts[2, 2]
            py = w0 * pts[0, 3] + w1 * pts[1, 3] + w2 * pts[2, 3]
            tx = (px - xmin) / max(1e-6, xmax - xmin)
            ty = (py - ymin) / max(1e-6, ymax - ymin)
            if side == "front":
                tx = 1.0 - tx
            # fit sprite in central 82% of panel
            sx = (tx - 0.09) / 0.82
            sy = 1.0 - (ty - 0.08) / 0.84  # sprite y=0 top, 3D y up
            iy = np.clip((sy * (sh - 1)).astype(np.int32), 0, sh - 1)
            ix = np.clip((sx * (sw - 1)).astype(np.int32), 0, sw - 1)
            in_spr = (sx >= 0.0) & (sx <= 1.0) & (sy >= 0.0) & (sy <= 1.0)
            yy = YY[inside]
            xx = XX[inside]
            albedo[yy, xx] = navy
            painted += int(inside.sum())
            mask = inside & in_spr
            if mask.any():
                pix = sa[iy[mask], ix[mask]]
                alpha = pix[:, 3] > 40
                if alpha.any():
                    dest_y = YY[mask][alpha]
                    dest_x = XX[mask][alpha]
                    gp = pix[alpha]
                    # blend gold over navy
                    a = gp[:, 3:4].astype(np.float32) / 255.0
                    base = albedo[dest_y, dest_x].astype(np.float32)
                    albedo[dest_y, dest_x] = np.clip(gp[:, :3].astype(np.float32) * a + base * (1.0 - a), 0, 255).astype(np.uint8)
                    gold_px += int(alpha.sum())
    print(side, "painted px", painted, "gold overlay", gold_px)
    return albedo


def paint_uv_seam_joints(albedo, me, islands):
    """Average dark UV-seam texels at neck / shoulders / joints."""
    h, w = albedo.shape[:2]
    uv = me.uv_layers.active.data
    # build loop UV per vertex per polygon via bmesh for seam edges
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.edges.ensure_lookup_table()
    uv_layer = bm.loops.layers.uv.active
    n_paint = 0
    for e in bm.edges:
        if len(e.link_faces) != 2:
            continue
        f0, f1 = e.link_faces
        if islands[f0.index] == islands[f1.index]:
            continue
        mid = (e.verts[0].co + e.verts[1].co) * 0.5
        joint = False
        if 1.38 < mid.y < 1.62 and abs(mid.x) < 0.20:
            joint = True  # neck
        if 1.20 < mid.y < 1.52 and 0.10 < abs(mid.x) < 0.40:
            joint = True  # shoulders
        if 0.82 < mid.y < 1.12 and abs(mid.x) > 0.20:
            joint = True  # elbows
        if 0.40 < mid.y < 0.66 and 0.04 < abs(mid.x) < 0.24:
            joint = True  # knees
        if not joint:
            continue
        uvs = []
        for f in (f0, f1):
            for l in f.loops:
                if l.vert in e.verts:
                    uvs.append(l[uv_layer].uv.copy())
        cols = []
        coords = []
        for uvv in uvs:
            x = int(np.clip(uvv.x, 0, 0.999) * w)
            y = int(np.clip(1.0 - uvv.y, 0, 0.999) * h)
            cols.append(albedo[y, x].astype(np.int32))
            coords.append((x, y))
        if not cols:
            continue
        lums = [0.3 * c[0] + 0.59 * c[1] + 0.11 * c[2] for c in cols]
        if min(lums) > 40:
            continue
        # take brightest non-black
        best = cols[int(np.argmax(lums))]
        if max(lums) < 18:
            continue
        for x, y in coords:
            for dy in range(-2, 3):
                for dx in range(-2, 3):
                    yy = min(h - 1, max(0, y + dy))
                    xx = min(w - 1, max(0, x + dx))
                    lum = 0.3 * int(albedo[yy, xx, 0]) + 0.59 * int(albedo[yy, xx, 1]) + 0.11 * int(albedo[yy, xx, 2])
                    if lum < 48:
                        albedo[yy, xx] = best.astype(np.uint8)
                        n_paint += 1
    bm.free()
    print("joint-seam texels painted", n_paint)
    return albedo


def compose_lion_card(sprite: Image.Image, navy, size=(512, 640)) -> Image.Image:
    card = Image.new("RGB", size, tuple(int(c) for c in navy))
    spr = sprite.convert("RGBA")
    sw, sh = spr.size
    scale = min(size[0] * 0.88 / sw, size[1] * 0.88 / sh)
    nw, nh = max(1, int(sw * scale)), max(1, int(sh * scale))
    spr = spr.resize((nw, nh), Image.LANCZOS)
    x = (size[0] - nw) // 2
    y = (size[1] - nh) // 2
    card.paste(spr.convert("RGB"), (x, y), spr.split()[-1])
    return card


def add_lion_material(ob, name: str, image_path: Path, face_ids, side: str):
    me = ob.data
    img = bpy.data.images.load(str(image_path))
    try:
        img.pack()
    except Exception:
        pass
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes.get("Principled BSDF")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    tex.location = (-400, 280)
    nt.links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
    if "Metallic" in bsdf.inputs:
        bsdf.inputs["Metallic"].default_value = 0.12
    if "Roughness" in bsdf.inputs:
        bsdf.inputs["Roughness"].default_value = 0.58
    me.materials.append(mat)
    idx = len(me.materials) - 1
    for fi in face_ids:
        me.polygons[fi].material_index = idx
    assign_planar_uv(me, face_ids, (0.05, 0.04, 0.95, 0.96), side)
    print("material", name, "slot", idx, "faces", len(face_ids))


def write_blender_image(bl_img, rgb: np.ndarray, path: Path):
    Image.fromarray(rgb, "RGB").save(path)
    bl_img.filepath = str(path)
    bl_img.source = "FILE"
    bl_img.reload()
    try:
        bl_img.pack()
    except Exception as exc:
        print("pack skipped", exc)


def inverse_orient(ob):
    """Unity Y-up (X,Y,Z) → original Blender-imported Meshy Z-up."""
    me = ob.data
    for v in me.vertices:
        X, Y, Z = v.co
        v.co = Vector((-X, -Z, Y))
    me.update()


def export_glb(ob, path: Path):
    bpy.ops.object.select_all(action="DESELECT")
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    path.parent.mkdir(parents=True, exist_ok=True)
    bpy.ops.export_scene.gltf(
        filepath=str(path),
        export_format="GLB",
        use_selection=True,
        export_apply=True,
        export_texcoords=True,
        export_normals=True,
        export_materials="EXPORT",
        export_cameras=False,
        export_extras=False,
        export_yup=True,
    )
    print("glb", path, path.stat().st_size)


def main():
    if not SRC_GLB.exists():
        raise SystemExit(f"missing {SRC_GLB}")
    ob = import_and_orient()
    me = ob.data
    xs = [v.co.x for v in me.vertices]
    ys = [v.co.y for v in me.vertices]
    zs = [v.co.z for v in me.vertices]
    print(
        "oriented bbox",
        round(min(xs), 3), round(max(xs), 3),
        round(min(ys), 3), round(max(ys), 3),
        round(min(zs), 3), round(max(zs), 3),
        "scabbard+X" if max(xs) > abs(min(xs)) else "WARN -X",
    )
    imgs = blender_images()
    albedo_entry = imgs.get("Image_0") or next(iter(imgs.values()))
    bl_albedo, albedo_path = albedo_entry
    albedo = png_rgb(albedo_path)
    h, w = albedo.shape[:2]
    print("albedo", w, h)

    islands, nisl = uv_islands(me)
    print("uv islands", nisl)

    occ0 = raster_occupancy(me, w, h)
    print("occupancy", int(occ0.sum()), "/", occ0.size)

    # 1) Bleed + seam inpaint on the Meshy atlas (does not move UVs of plate).
    albedo = paint_uv_seam_joints(albedo, me, islands)
    occ = raster_occupancy(me, w, h)
    albedo = inpaint_dark_borders(albedo, occ)
    albedo_bled = dilate_nn(albedo, occ, steps=16)
    Image.fromarray(albedo_bled, "RGB").save(WORKDIR / "albedo_repaired.png")
    for name in ("Image_1", "Image_2"):
        if name not in imgs:
            continue
        bl_img, path = imgs[name]
        arr = png_rgb(path)
        arr = dilate_nn(arr, occ, steps=12)
        write_blender_image(bl_img, arr, WORKDIR / f"{name}_repaired.png")
        print("bled", name)
    write_blender_image(bl_albedo, albedo_bled, WORKDIR / "Image_0_repaired.png")

    # 2) Dedicated lion cards + planar UV on upper-torso cloth only.
    #    Does not overwrite plate islands on Image_0.
    back_faces = collect_panel_faces(me, albedo, islands, "back")
    front_faces = collect_panel_faces(me, albedo, islands, "front")
    if len(back_faces) < 8 or len(front_faces) < 8:
        raise SystemExit(f"lion panel too small back={len(back_faces)} front={len(front_faces)}")
    navy_samples = []
    uv = me.uv_layers.active.data
    for fi in back_faces + front_faces:
        for li in me.polygons[fi].loop_indices:
            rgb = sample_rgb(albedo, uv[li].uv.x, uv[li].uv.y)
            r, g, b = (int(c) for c in rgb)
            lum = 0.3 * r + 0.59 * g + 0.11 * b
            if b > r + 10 and b > 45 and r < 100 and lum > 28:
                navy_samples.append(rgb)
    sampled = np.median(np.array(navy_samples), axis=0) if navy_samples else np.array([28, 44, 96], np.float32)
    sot_navy = np.array([40, 66, 128], np.float32)
    navy = np.clip(sampled * 0.45 + sot_navy * 0.55, 0, 255).astype(np.uint8)
    print("navy", navy.tolist(), "faces back/front", len(back_faces), len(front_faces))

    lion_back = extract_lion_sprite(LOOK / "01_rear_LOCKED.png", LION_REAR_BOX)
    lion_front = extract_lion_sprite(GATE / "01_FRONT.png", LION_FRONT_BOX)
    card_b = compose_lion_card(lion_back, navy)
    card_f = compose_lion_card(lion_front, navy)
    p_back = WORKDIR / "lion_card_back.png"
    p_front = WORKDIR / "lion_card_front.png"
    card_b.save(p_back)
    card_f.save(p_front)

    add_lion_material(ob, "LionBack", p_back, back_faces, "back")
    add_lion_material(ob, "LionFront", p_front, front_faces, "front")

    inverse_orient(ob)
    export_glb(ob, OUT_GLB)
    if OUT_GLB_DESIGN.resolve() != OUT_GLB.resolve():
        export_glb(ob, OUT_GLB_DESIGN)

    note = {
        "lookPassClaimed": False,
        "repair": "uv-island-bleed + dedicated SoT lion cards (planar UV)",
        "navy": navy.tolist(),
        "backFaces": len(back_faces),
        "frontFaces": len(front_faces),
        "walkPaused": True,
        "playHubLocked": True,
    }
    (WORKDIR / "repair.json").write_text(json.dumps(note, indent=2) + "\n")
    print("done repair", note)


if __name__ == "__main__":
    main()
