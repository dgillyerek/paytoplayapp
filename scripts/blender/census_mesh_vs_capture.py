#!/usr/bin/env python3
"""Mesh-vs-capture census at the FAIL MP4 mid-swing pose (t=0.625).

Capture A: Unity Actor LBS (same mesh.txt + bindposes + Evaluate() as
SirAldric3DActor). Editor Game-view is not on this VM — this is the
SkinnedMeshRenderer deformation Unity would compute, rendered as a still PNG
(no video encoder).

Capture B: offline Blender EEVEE of the same Evaluate() pose (no Unity recorder).

Also extract the cc77d8a FAIL MP4 frame (video encoder) for the Design-eyed clip.

LOOK path (UVs/albedo) is not edited. Bind NOT claimed.
"""
from __future__ import annotations

import json
import subprocess
import sys
from collections import defaultdict
import os
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFont

Image.MAX_IMAGE_PIXELS = None

ROOT = Path(__file__).resolve().parents[2]
PACK3D = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d"
PROOF = ROOT / "Docs/Survival/previews/gate3"
WALK = PROOF / "walk"
ART = Path("/opt/cursor/artifacts")
MESH = PACK3D / "sir_aldric_meshy.mesh.txt"
ATLAS = PACK3D / "sir_aldric_meshy_atlas.png"
MP4 = PROOF / "sir_aldric_path2_walk_toward_top.mp4"
MID_T = 0.625
FPS = 16
MID_FRAME = int(round(MID_T * FPS))  # f_010 in the 16fps walk

BONE_REST = {
    "Root": ((0.0, 0.0, 0.0), (0.0, 0.0, 0.0)),
    "Hips": ((0.0, 0.96, 0.0), (0.0, 0.0, 0.0)),
    "Spine": ((0.0, 0.12, 0.0), (0.0, 0.0, 0.0)),
    "Chest": ((0.0, 0.18, 0.0), (0.0, 0.0, 0.0)),
    "Neck": ((0.0, 0.20, 0.0), (0.0, 0.0, 0.0)),
    "Head": ((0.0, 0.10, 0.0), (0.0, 0.0, 0.0)),
    "Arm_L": ((-0.22, 0.10, 0.0), (0.0, 0.0, 0.0)),
    "Fore_L": ((0.0, -0.28, 0.0), (0.0, 0.0, 0.0)),
    "Hand_L": ((0.0, -0.24, 0.0), (0.0, 0.0, 0.0)),
    "Arm_R": ((0.22, 0.10, 0.0), (0.0, 0.0, 0.0)),
    "Fore_R": ((0.0, -0.28, 0.0), (0.0, 0.0, 0.0)),
    "Hand_R": ((0.0, -0.24, 0.0), (0.0, 0.0, 0.0)),
    "Sword": ((0.02, -0.08, 0.06), (0.0, 0.0, 0.0)),
    "UpLeg_L": ((-0.11, -0.04, 0.0), (0.0, 0.0, 0.0)),
    "Leg_L": ((0.0, -0.42, 0.0), (0.0, 0.0, 0.0)),
    "Foot_L": ((0.0, -0.40, 0.05), (0.0, 0.0, 0.0)),
    "UpLeg_R": ((0.11, -0.04, 0.0), (0.0, 0.0, 0.0)),
    "Leg_R": ((0.0, -0.42, 0.0), (0.0, 0.0, 0.0)),
    "Foot_R": ((0.0, -0.40, 0.05), (0.0, 0.0, 0.0)),
    "Scabbard": ((0.20, -0.04, -0.02), (18.0, 0.0, 22.0)),
    "Cape": ((0.0, 0.08, -0.12), (0.0, 0.0, 0.0)),
}
PARENT = {
    "Root": None, "Hips": "Root", "Spine": "Hips", "Chest": "Spine",
    "Neck": "Chest", "Head": "Neck",
    "Arm_L": "Chest", "Fore_L": "Arm_L", "Hand_L": "Fore_L",
    "Arm_R": "Chest", "Fore_R": "Arm_R", "Hand_R": "Fore_R", "Sword": "Hand_R",
    "UpLeg_L": "Hips", "Leg_L": "UpLeg_L", "Foot_L": "Leg_L",
    "UpLeg_R": "Hips", "Leg_R": "UpLeg_R", "Foot_R": "Leg_R",
    "Scabbard": "Hips", "Cape": "Chest",
}
ORDER = list(BONE_REST)
POSE_BONE = {
    "Hips": "hips", "Spine": "spine", "Chest": "chest", "Head": "head",
    "Arm_L": "arm_l", "Fore_L": "fore_l", "Arm_R": "arm_r", "Fore_R": "fore_r",
    "Hand_R": "hand_r", "Sword": "sword",
    "UpLeg_L": "up_l", "Leg_L": "leg_l", "Foot_L": "foot_l",
    "UpLeg_R": "up_r", "Leg_R": "leg_r", "Foot_R": "foot_r",
}


def lerp(a, b, t):
    return a + (b - a) * t


def sample_keys(keys, u):
    n = len(keys)
    x = (u % 1.0) * n
    i0 = int(np.floor(x)) % n
    i1 = (i0 + 1) % n
    return lerp(keys[i0], keys[i1], x - np.floor(x))


def walk_pose(t):
    """Same keys as SirAldric3DMotion.WalkPose / Blender apply_pose."""
    u = (t / 1.00) % 1.0
    hips_y, hips_z = sample_keys([-3.0, 5.0, 3.0, -5.0], u), sample_keys([5.5, 1.5, -5.5, 1.5], u)
    spine_y = sample_keys([8.0, -8.0, -8.0, 8.0], u)
    bob = 0.012 + 0.018 * abs(np.cos(u * np.pi * 2.0))
    return {
        "root_z": t * 0.80, "root_y": bob,
        "hips": (0.0, hips_y, hips_z),
        "spine": (5.0, spine_y, -hips_z),
        "chest": (2.0, spine_y * 0.5, 0.0),
        "head": (6.0, spine_y * 0.25, 0.0),
        "up_l": (sample_keys([-18.0, -12.0, 8.0, 12.0], u), 0.0, sample_keys([0.0, 0.0, 8.0, 0.0], u)),
        "leg_l": (sample_keys([80.0, 12.0, 14.0, 18.0], u), 0.0, 0.0),
        "foot_l": (sample_keys([16.0, -12.0, -6.0, 14.0], u), 0.0, 0.0),
        "up_r": (sample_keys([8.0, 12.0, -18.0, -12.0], u), 0.0, sample_keys([-8.0, 0.0, 0.0, 0.0], u)),
        "leg_r": (sample_keys([14.0, 16.0, 80.0, 12.0], u), 0.0, 0.0),
        "foot_r": (sample_keys([-6.0, 14.0, 16.0, -12.0], u), 0.0, 0.0),
        "arm_l": (sample_keys([36.0, 32.0, -32.0, -34.0], u), 0.0, -22.0),
        "fore_l": (-18.0, 0.0, 0.0),
        "arm_r": (sample_keys([-32.0, -34.0, 24.0, 22.0], u), 4.0, 22.0),
        "fore_r": (-22.0, 0.0, 0.0),
        "hand_r": (0.0, 0.0, 0.0),
        "sword": (-6.0, 0.0, 8.0),
    }


def unity_euler(x, y, z):
    x, y, z = np.radians([x, y, z])
    cx, sx = np.cos(x), np.sin(x)
    cy, sy = np.cos(y), np.sin(y)
    cz, sz = np.cos(z), np.sin(z)
    rx = np.array([[1, 0, 0, 0], [0, cx, -sx, 0], [0, sx, cx, 0], [0, 0, 0, 1]], np.float64)
    ry = np.array([[cy, 0, sy, 0], [0, 1, 0, 0], [-sy, 0, cy, 0], [0, 0, 0, 1]], np.float64)
    rz = np.array([[cz, -sz, 0, 0], [sz, cz, 0, 0], [0, 0, 1, 0], [0, 0, 0, 1]], np.float64)
    return ry @ rx @ rz


def translation(x, y, z):
    m = np.eye(4, dtype=np.float64)
    m[0, 3], m[1, 3], m[2, 3] = x, y, z
    return m


def bone_worlds(pose):
    rest = {}
    posed = {}
    for name in ORDER:
        loc, rest_eul = BONE_REST[name]
        parent = PARENT[name]
        rest_local = translation(*loc) @ unity_euler(*rest_eul)
        key = POSE_BONE.get(name)
        eul = pose[key] if key else rest_eul
        pose_local = translation(*loc) @ unity_euler(*eul)
        if parent is None:
            rest[name] = rest_local
            posed[name] = translation(0.0, pose["root_y"], pose["root_z"]) @ pose_local
        else:
            rest[name] = rest[parent] @ rest_local
            posed[name] = posed[parent] @ pose_local
    bind = {n: np.linalg.inv(rest[n]) for n in ORDER}
    skin = {n: posed[n] @ bind[n] for n in ORDER}
    return skin


def load_mesh(path: Path):
    verts, uvs, widx, ww, tris = [], [], [], [], []
    bone = "Hips"
    pending = []
    index = {n: i for i, n in enumerate(ORDER)}
    for raw in path.read_text().splitlines():
        if not raw or raw[0] == "#":
            continue
        if raw.startswith("BONE "):
            bone = raw[5:].strip()
            continue
        if raw.startswith("V "):
            p = raw[2:].split()
            verts.append((float(p[0]), float(p[1]), float(p[2])))
            uvs.append((float(p[6]), float(p[7])))
            names, ws = [], []
            for tok in p[8:4 + 8]:
                if ":" not in tok:
                    continue
                n, w = tok.split(":", 1)
                if n in index:
                    names.append(index[n])
                    ws.append(float(w))
            if not names:
                names, ws = [index.get(bone, index["Hips"])], [1.0]
            while len(names) < 4:
                names.append(0)
                ws.append(0.0)
            s = sum(ws[:4]) or 1.0
            widx.append(names[:4])
            ww.append([w / s for w in ws[:4]])
            pending.append(len(verts) - 1)
            continue
        if raw == "T" and len(pending) >= 3:
            tris.append((pending[-3], pending[-2], pending[-1]))
    return (
        np.array(verts, np.float64),
        np.array(uvs, np.float64),
        np.array(widx, np.int32),
        np.array(ww, np.float64),
        np.array(tris, np.int32),
    )


def skin_verts(verts, widx, ww, skin):
    mats = np.stack([skin[n] for n in ORDER], axis=0)
    vh = np.ones((len(verts), 4), np.float64)
    vh[:, :3] = verts
    out = np.zeros((len(verts), 4), np.float64)
    for k in range(4):
        m = mats[widx[:, k]]
        out += ww[:, k:k + 1] * np.einsum("nij,nj->ni", m, vh)
    return out[:, :3]


def extract_mp4_frame(dest: Path):
    dest.parent.mkdir(parents=True, exist_ok=True)
    subprocess.check_call(
        [
            "ffmpeg", "-y", "-i", str(MP4),
            "-vf", f"select=eq(n\\,{MID_FRAME})",
            "-vframes", "1", str(dest),
        ],
        stdout=subprocess.DEVNULL,
        stderr=subprocess.DEVNULL,
    )
    print("mp4 frame", dest, dest.stat().st_size, "n", MID_FRAME)


def render_actor_lbs_blender(skinned, uvs, tris, dest: Path):
    """Render Actor-skinned verts through the same Play cam (still PNG, no encoder)."""
    import bpy
    from mathutils import Vector

    sys.path.insert(0, str(Path(__file__).resolve().parent))
    import skin_sir_aldric_meshy as old

    bpy.ops.wm.read_factory_settings(use_empty=True)
    me = bpy.data.meshes.new("ActorLBS")
    me.from_pydata([tuple(p) for p in skinned], [], [tuple(t) for t in tris])
    me.update()
    uv_layer = me.uv_layers.new(name="UVMap")
    me.calc_loop_triangles()
    # from_pydata makes one loop per corner in tri order
    for li, loop in enumerate(me.loops):
        uv_layer.data[li].uv = Vector((float(uvs[loop.vertex_index][0]), float(uvs[loop.vertex_index][1])))
    ob = bpy.data.objects.new("ActorLBS", me)
    bpy.context.collection.objects.link(ob)
    img = bpy.data.images.load(str(ATLAS))
    mat = bpy.data.materials.new("ActorUnlit")
    mat.use_nodes = True
    nt = mat.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    em = nt.nodes.new("ShaderNodeEmission")
    tex = nt.nodes.new("ShaderNodeTexImage")
    tex.image = img
    em.inputs["Strength"].default_value = 1.0
    nt.links.new(tex.outputs["Color"], em.inputs["Color"])
    nt.links.new(em.outputs["Emission"], out.inputs["Surface"])
    me.materials.append(mat)
    old.setup_render()
    dest.parent.mkdir(parents=True, exist_ok=True)
    bpy.context.scene.render.filepath = str(dest)
    bpy.ops.render.render(write_still=True)
    print("actor lbs still", dest, dest.stat().st_size)


def font(size):
    path = Path("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf")
    if path.exists():
        return ImageFont.truetype(str(path), size)
    return ImageFont.load_default()


def cap(im, text):
    d = ImageDraw.Draw(im)
    d.rectangle((0, 0, im.width, 78), fill=(16, 16, 14))
    d.text((18, 22), text, fill=(236, 230, 210), font=font(20))
    return im


def compose(actor_p, blender_p, mp4_p, dest: Path):
    ART.mkdir(parents=True, exist_ok=True)
    cells = []
    for p, label in (
        (actor_p, "A  Actor LBS  |  Unity skin path  |  still PNG (no encoder)"),
        (blender_p, "B  Blender EEVEE  |  same Evaluate() pose  |  offline still"),
        (mp4_p, "C  MP4 frame  |  video encoder  |  n=10 t=0.625"),
    ):
        im = Image.open(p).convert("RGB")
        tw = 420
        th = int(im.height * tw / im.width)
        cells.append(cap(im.resize((tw, th), Image.LANCZOS), label))
    w, h = cells[0].size
    sheet = Image.new("RGB", (w * 3, 130 + h), (16, 16, 14))
    d = ImageDraw.Draw(sheet)
    tag = os.environ.get("CENSUS_TAG", "").strip()
    if tag == "tubes":
        d.text((18, 16), "GATE 3  |  mesh-vs-capture  |  mid-swing t=0.625  |  closed hang-arm tubes  |  bind NOT claimed", fill=(236, 230, 210), font=font(24))
        d.text((18, 52), "A = SirAldric3DActor LBS (mesh.txt + bindposes + Evaluate).  Editor Game-view not on this VM.", fill=(180, 176, 160), font=font(16))
        d.text((18, 80), "B = offline Blender EEVEE.  C = walk MP4 n=10 after ffmpeg/yuv420p.", fill=(180, 176, 160), font=font(16))
        d.text((18, 104), "Premise: delete paper Arm_* ; closed thick-walled tubes ; e5b132f UV reproject. Eye A+B+C for closed volumes.", fill=(180, 176, 160), font=font(16))
    else:
        d.text((18, 16), "GATE 3  |  mesh-vs-capture  |  mid-swing t=0.625  |  VERDICT: MESH  |  bind NOT claimed", fill=(236, 230, 210), font=font(24))
        d.text((18, 52), "A = SirAldric3DActor LBS (mesh.txt + bindposes + Evaluate).  Editor Game-view not on this VM.", fill=(180, 176, 160), font=font(16))
        d.text((18, 80), "B = offline Blender (Design-eyed clip source).  C = same clip after ffmpeg/yuv420p.", fill=(180, 176, 160), font=font(16))
        d.text((18, 104), "EYED: A+B+C all tear (left navy sheet, right gold slats). Offline ALSO tears → MESH, not capture.", fill=(220, 160, 120), font=font(16))
    for i, im in enumerate(cells):
        sheet.paste(im, (i * w, 130))
    dest.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(dest, optimize=True)
    (ART / dest.name).write_bytes(dest.read_bytes())
    print("sheet", dest, dest.stat().st_size)


def main():
    WALK.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    tag = os.environ.get("CENSUS_TAG", "").strip()
    if tag:
        actor_p = WALK / f"census_{tag}_actor_lbs_mid_swing.png"
        blender_p = WALK / "world_walk_mid_swing.png"
        mp4_p = WALK / f"census_{tag}_mp4_mid_swing.png"
        sheet_p = PROOF / f"gate3_mesh_vs_capture_{tag}_mid_swing.png"
    else:
        actor_p = WALK / "census_actor_lbs_mid_swing.png"
        blender_p = WALK / "census_blender_mid_swing.png"
        if not blender_p.exists():
            blender_p = WALK / "world_walk_mid_swing.png"
        mp4_p = WALK / "census_mp4_mid_swing.png"
        sheet_p = PROOF / "gate3_mesh_vs_capture_mid_swing.png"

    if "--compose-only" in sys.argv:
        compose(actor_p, blender_p, mp4_p, sheet_p)
        return

    pose = walk_pose(MID_T)
    print("pose t", MID_T, "arm_l", pose["arm_l"], "arm_r", pose["arm_r"], "up_l", pose["up_l"])
    verts, uvs, widx, ww, tris = load_mesh(MESH)
    skin = bone_worlds(pose)
    skinned = skin_verts(verts, widx, ww, skin)
    print("lbs verts", len(skinned), "tris", len(tris))
    np.savez("/tmp/actor_lbs_mid.npz", v=skinned, uv=uvs, t=tris)

    try:
        import bpy  # noqa: F401
        in_blender = True
    except ImportError:
        in_blender = False
    if not in_blender:
        # extract MP4 + compose later after blender render
        extract_mp4_frame(mp4_p)
        (ART / mp4_p.name).write_bytes(mp4_p.read_bytes())
        # invoke blender to render Actor LBS still
        subprocess.check_call(
            ["blender", "--background", "--python", str(Path(__file__).resolve())],
        )
        if actor_p.exists() and blender_p.exists() and mp4_p.exists():
            compose(actor_p, blender_p, mp4_p, sheet_p)
        note = {
            "lookPassClaimed": True,
            "bindPassClaimed": False,
            "walkPassClaimed": False,
            "midSwingT": MID_T,
            "mp4Frame": MID_FRAME,
            "unityEditor": False,
            "captureA": "Actor LBS still PNG — same mesh.txt/bindposes/Evaluate() as SirAldric3DActor (no Editor Game-view on this VM, no video encoder)",
            "captureB": "offline Blender EEVEE world_walk_mid_swing.png (same pose, no Unity recorder)",
            "captureC": "cc77d8a FAIL MP4 decoded frame n=10",
            "lookPath": "unchanged e5b132f UVs/albedo",
            "actorPng": str(actor_p.relative_to(ROOT)),
            "blenderPng": str(blender_p.relative_to(ROOT)),
            "mp4Png": str(mp4_p.relative_to(ROOT)),
            "sheet": str(sheet_p.relative_to(ROOT)),
        }
        tag = os.environ.get("CENSUS_TAG", "").strip()
        json_name = f"sir_aldric_mesh_vs_capture_{tag}.json" if tag else "sir_aldric_mesh_vs_capture.json"
        (PACK3D / json_name).write_text(json.dumps(note, indent=2) + "\n")
        print("census json written", json_name, "— eye the sheet before a bind verdict")
        return

    data = np.load("/tmp/actor_lbs_mid.npz")
    render_actor_lbs_blender(data["v"], data["uv"], data["t"], actor_p)
    (ART / actor_p.name).write_bytes(actor_p.read_bytes())


CYCLE_NS = (0, 5, 10, 15)


def cycle_main():
    """A/B/C at n=0,5,10,15 so Design can eye the whole walk, not t=0.625 only."""
    WALK.mkdir(parents=True, exist_ok=True)
    ART.mkdir(parents=True, exist_ok=True)
    try:
        import bpy  # noqa: F401
        in_blender = True
    except ImportError:
        in_blender = False

    if in_blender:
        for n in CYCLE_NS:
            data = np.load(f"/tmp/actor_lbs_n{n:02d}.npz")
            dest = WALK / f"census_cycle_actor_n{n:02d}.png"
            render_actor_lbs_blender(data["v"], data["uv"], data["t"], dest)
            (ART / dest.name).write_bytes(dest.read_bytes())
        return

    verts, uvs, widx, ww, tris = load_mesh(MESH)
    for n in CYCLE_NS:
        t = n / float(FPS)
        pose = walk_pose(t)
        skinned = skin_verts(verts, widx, ww, bone_worlds(pose))
        np.savez(f"/tmp/actor_lbs_n{n:02d}.npz", v=skinned, uv=uvs, t=tris)
        dest = WALK / f"census_cycle_mp4_n{n:02d}.png"
        subprocess.check_call(
            [
                "ffmpeg", "-y", "-i", str(MP4),
                "-vf", f"select=eq(n\\,{n})",
                "-vframes", "1", str(dest),
            ],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL,
        )
        print("cycle mp4", n, dest.stat().st_size)
    env = dict(**os.environ)
    env["CENSUS_TAG"] = "cycle"
    subprocess.check_call(
        ["blender", "--background", "--python", str(Path(__file__).resolve())],
        env=env,
    )
    subprocess.check_call(["python3", str(ROOT / "scripts/blender/compose_cycle_abc.py")])
    note = {
        "lookPassClaimed": True,
        "bindPassClaimed": False,
        "walkPassClaimed": False,
        "frames": list(CYCLE_NS),
        "unityEditor": False,
        "sheet": "Docs/Survival/previews/gate3/gate3_mesh_vs_capture_cycle.png",
        "failTip": "76eab9d",
        "premise": "ONE connected loft per side; leftover paper deleted — cycle A/B/C",
        "vsFail": "Docs/Survival/previews/gate3/gate3_path2_vs_76eab9d.png",
        "walkClip": "Docs/Survival/previews/gate3/sir_aldric_path2_walk_toward_top.mp4",
    }
    (PACK3D / "sir_aldric_mesh_vs_capture_cycle.json").write_text(json.dumps(note, indent=2) + "\n")
    print("cycle census done")


if __name__ == "__main__":
    if os.environ.get("CENSUS_TAG") == "cycle":
        cycle_main()
    else:
        main()
