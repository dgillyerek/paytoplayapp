#!/usr/bin/env python3
"""3D Animator Sir Aldric preview — high-angle rear, march toward TOP of 1080×1920.

Blender mid-poly + look_targets atlas (path B). Same Evaluate() walk as the
5916447 motion hold. Not a box atlas / capsule / PNG warp.
"""
from __future__ import annotations

import math
import shutil
import subprocess
import sys
from collections import defaultdict
from pathlib import Path

import numpy as np
from PIL import Image, ImageChops, ImageDraw, ImageFont, ImageStat

ROOT = Path(__file__).resolve().parents[3]
OUT = Path(__file__).resolve().parent
ART = Path("/opt/cursor/artifacts")
LOOK = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/look_targets"
REFS = ROOT / "design/survival-theme-a-fantasy/heroes/anim/sir_aldric/UNITY_3D_HANDOFF/refs"
PHASES = REFS / "gait_bar_phases"
W, H = 1080, 1920
WALK_PERIOD = 1.00
WALK_CYCLES = 2
ATTACK = 1.40
WALK_BLOCK = WALK_PERIOD * WALK_CYCLES
LOOP = WALK_BLOCK + ATTACK
MARCH = 0.80

SILVER = (186, 194, 204)
GOLD = (211, 176, 82)
BLUE = (41, 76, 148)
BROWN = (92, 56, 33)
DARK = (46, 51, 56)
GROUND = (28, 32, 24)

CAM_EYE = np.array([0.0, 2.80, -5.40])
CAM_TARGET = np.array([0.0, 0.90, 0.50])
CAM_FOV = 30.0

REAR_IMG = Image.open(LOOK / "01_rear_LOCKED.png").convert("RGBA")
REAR = np.array(REAR_IMG)
REAR_RGB = REAR[:, :, :3].astype(np.float32)
REAR_A = REAR[:, :, 3].astype(np.float32) / 255.0
ATLAS_PATH = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_atlas.png"
MESH_PATH = ROOT / "Assets/ThemePack/fantasy_kingdom_a/art/heroes/3d/sir_aldric_midpoly.mesh.txt"
ATLAS = np.array(Image.open(ATLAS_PATH).convert("RGB")).astype(np.float32)
BONE_PY = {
    "Head": "head",
    "Neck": "neck",
    "Chest": "chest",
    "Spine": "spine",
    "Hips": "hips",
    "Arm_L": "arm_l",
    "Fore_L": "fore_l",
    "Hand_L": "hand_l",
    "Arm_R": "arm_r",
    "Fore_R": "fore_r",
    "Hand_R": "hand_r",
    "Sword": "sword",
    "UpLeg_L": "up_l",
    "Leg_L": "leg_l",
    "Foot_L": "foot_l",
    "UpLeg_R": "up_r",
    "Leg_R": "leg_r",
    "Foot_R": "foot_r",
    "Scabbard": "scabbard",
}


def load_blender_parts(path: Path):
    """Bone-local tris from the Blender mesh.txt (same file the Actor binds)."""
    groups = defaultdict(list)
    bone = "Hips"
    pending = []
    for raw in path.read_text().splitlines():
        if not raw or raw[0] == "#":
            continue
        if raw.startswith("FMT") or raw.startswith("BONE "):
            if raw.startswith("BONE "):
                bone = raw[5:].strip()
                pending = []
            continue
        if raw.startswith("V "):
            p = raw[2:].split()
            v = np.array([float(p[0]), float(p[1]), float(p[2])], np.float64)
            n = np.array([float(p[3]), float(p[4]), float(p[5])], np.float64)
            uv = np.array([float(p[6]), float(p[7])], np.float64)
            pending.append((v, n, uv))
            continue
        if raw == "T" and len(pending) >= 3:
            (p0, n0, uv0), (p1, n1, uv1), (p2, n2, uv2) = pending[-3:]
            groups[bone].append((p0, p1, p2, n0, n1, n2, uv0, uv1, uv2))
    out = []
    for name, tris in groups.items():
        py = BONE_PY.get(name)
        if py:
            out.append((py, tris))
    return out
# Content bbox of 01_rear (precomputed).
BX0, BY0, BX1, BY1 = 274, 40, 749, 983


def _strip_painted_scabbard(rgb, alpha):
    """Drop the look-target's painted right-hip sheath so only the 3D Scabbard remains."""
    out = rgb.copy()
    h, w = alpha.shape
    xs = np.arange(w)
    ys = np.arange(h)[:, None]
    mid = 0.5 * (BX0 + BX1)
    right = xs[None, :] > mid + 8
    visible = alpha > 0.12
    shift = max(8, w // 16)
    src = np.roll(out, shift, axis=1)
    for _ in range(3):
        r, g, b = out[..., 0], out[..., 1], out[..., 2]
        brown = (
            visible
            & right
            & (r > 38)
            & (r > b + 18)
            & (g < r * 0.92)
            & (b < 95)
            & ((r + g + b) < 440)
        )
        below_lion = ys > BY0 + 0.40 * (BY1 - BY0)
        gold = (
            visible
            & right
            & below_lion
            & (r > 130)
            & (g > 90)
            & (b < 130)
            & (r > b + 30)
        )
        mask = brown | gold
        out[mask] = src[mask]
        src = np.roll(out, shift, axis=1)
    return out


REAR_RGB = _strip_painted_scabbard(REAR_RGB, REAR_A)


def clamp01(x):
    return 0.0 if x < 0 else 1.0 if x > 1 else x


def lerp(a, b, t):
    return a + (b - a) * t


def smooth01(x):
    x = clamp01(x)
    return x * x * (3 - 2 * x)


def repeat(t, length):
    r = t % length
    return r + length if r < 0 else r


def evaluate(t):
    loop_t = repeat(t, LOOP)
    attacking = loop_t >= WALK_BLOCK
    root_z = loop_t * MARCH
    if attacking:
        return attack_pose(loop_t - WALK_BLOCK, root_z)
    return walk_pose(loop_t, root_z)


# Four Game-view keys — same tables as SirAldric3DMotion.WalkPose. Do not drift.
# u=0 pass L, 0.25 contact L, 0.50 pass R, 0.75 contact R.
# 14d9c17 pass thigh +22 stacked on knee +X and the swing foot's world Z went
# negative (calves read as kicking toward the camera). Pass thigh is −X so the
# tucked foot travels world +Z / TOP. Small +X trail = toe-off, not a back-kick.
HIPSY = [-3.0, 5.0, 3.0, -5.0]
HIPSZ = [5.5, 1.5, -5.5, 1.5]
SPINEY = [8.0, -8.0, -8.0, 8.0]
UPLX = [-18.0, -12.0, 8.0, 12.0]
UPLZ = [0.0, 0.0, 8.0, 0.0]
LEGL = [80.0, 12.0, 14.0, 18.0]
FOOTL = [16.0, -12.0, -6.0, 14.0]
UPRX = [8.0, 12.0, -18.0, -12.0]
UPRZ = [-8.0, 0.0, 0.0, 0.0]
LEGR = [14.0, 16.0, 80.0, 12.0]
FOOTR = [-6.0, 14.0, 16.0, -12.0]
ARMLX = [36.0, 32.0, -32.0, -34.0]
ARMRX = [-32.0, -34.0, 24.0, 22.0]


def sample_keys(keys, u):
    n = len(keys)
    x = repeat(u, 1.0) * n
    i0 = int(math.floor(x)) % n
    i1 = (i0 + 1) % n
    t = x - math.floor(x)
    return lerp(keys[i0], keys[i1], t)


def walk_pose(loop_t, root_z):
    u = repeat(loop_t / WALK_PERIOD, 1.0)
    hips_y, hips_z = sample_keys(HIPSY, u), sample_keys(HIPSZ, u)
    spine_y = sample_keys(SPINEY, u)
    arm_lx, arm_rx = sample_keys(ARMLX, u), sample_keys(ARMRX, u)
    bob = 0.012 + 0.018 * abs(math.cos(u * math.pi * 2.0))
    return dict(
        attacking=False,
        drawn=False,
        root_z=root_z,
        root_y=bob,
        hips=(0, hips_y, hips_z),
        spine=(5, spine_y, -hips_z),
        chest=(2, spine_y * 0.5, 0),
        head=(6, spine_y * 0.25, 0),
        up_l=(sample_keys(UPLX, u), 0, sample_keys(UPLZ, u)),
        leg_l=(sample_keys(LEGL, u), 0, 0),
        foot_l=(sample_keys(FOOTL, u), 0, 0),
        up_r=(sample_keys(UPRX, u), 0, sample_keys(UPRZ, u)),
        leg_r=(sample_keys(LEGR, u), 0, 0),
        foot_r=(sample_keys(FOOTR, u), 0, 0),
        arm_l=(arm_lx, 0, -22),
        fore_l=(-18, 0, 0),
        arm_r=(arm_rx, 4, 22),
        fore_r=(-22, 0, 0),
        hand_r=(0, 0, 0),
        sword=(-6, 0, 8),
        label="WALK  ·  toward TOP",
    )


def attack_pose(attack_t, root_z):
    u = clamp01(attack_t / ATTACK)
    if u < 0.18:
        k = smooth01(u / 0.18)
        arm_x, arm_y, fore_x = lerp(-16, -85, k), lerp(6, 2, k), lerp(-22, -8, k)
        lunge, spine_x, drawn_k = 0.0, lerp(4, 10, k), k
    elif u < 0.40:
        k = smooth01((u - 0.18) / 0.22)
        arm_x, arm_y, fore_x = lerp(-85, -155, k), lerp(2, 0, k), lerp(-8, -28, k)
        lunge, spine_x, drawn_k = lerp(0, 0.04, k), lerp(10, 14, k), 1.0
    elif u < 0.56:
        k = smooth01((u - 0.40) / 0.16)
        arm_x, arm_y, fore_x = lerp(-155, -118, k), lerp(0, 0, k), lerp(-28, -6, k)
        lunge, spine_x, drawn_k = lerp(0.04, 0.10, k), lerp(14, 6, k), 1.0
    elif u < 0.78:
        k = smooth01((u - 0.56) / 0.22)
        arm_x, arm_y, fore_x = lerp(-118, -48, k), lerp(0, 4, k), lerp(-6, -16, k)
        lunge, spine_x, drawn_k = lerp(0.10, 0.02, k), lerp(6, 2, k), 1.0 - k * 0.35
    else:
        k = smooth01((u - 0.78) / 0.22)
        arm_x, arm_y, fore_x = lerp(-48, -16, k), lerp(4, 6, k), lerp(-16, -22, k)
        lunge, spine_x, drawn_k = lerp(0.02, 0, k), lerp(2, 4, k), 1.0 - k
    drawn = drawn_k > 0.22
    strike = drawn and -175 <= arm_x <= -80
    return dict(
        attacking=True,
        drawn=drawn,
        root_z=root_z,
        root_y=lunge,
        hips=(lunge * 20, 0, 0),
        spine=(spine_x, 0, 0),
        chest=(spine_x * 0.4, 0, 0),
        head=(8, 0, 0),
        up_l=(8, 0, 0),
        leg_l=(12, 0, 0),
        foot_l=(-6, 0, 0),
        up_r=(-6, 0, 0),
        leg_r=(16, 0, 0),
        foot_r=(-4, 0, 0),
        arm_l=(-22, 0, 10),
        fore_l=(-18, 0, 0),
        arm_r=(arm_x, arm_y, -8),
        fore_r=(fore_x, 0, 0),
        hand_r=(-12 if drawn else 0, 0, 0),
        sword=(-10 if drawn else -6, 0, 0 if drawn else 8),
        label="ATTACK  ·  strike TOP" if strike else "ATTACK  ·  draw / recover",
    )


def rot_euler(x, y, z):
    x, y, z = np.radians([x, y, z])
    cx, sx = math.cos(x), math.sin(x)
    cy, sy = math.cos(y), math.sin(y)
    cz, sz = math.cos(z), math.sin(z)
    rx = np.array([[1, 0, 0], [0, cx, -sx], [0, sx, cx]], np.float64)
    ry = np.array([[cy, 0, sy], [0, 1, 0], [-sy, 0, cy]], np.float64)
    rz = np.array([[cz, -sz, 0], [sz, cz, 0], [0, 0, 1]], np.float64)
    r = ry @ rx @ rz
    m = np.eye(4)
    m[:3, :3] = r
    return m


def trans(p):
    m = np.eye(4)
    m[:3, 3] = p
    return m


def local_of(pos, eul):
    return trans(pos) @ rot_euler(*eul)


def fk(pose):
    root = local_of((0, pose["root_y"], pose["root_z"]), (0, 0, 0))
    hips = root @ local_of((0, 0.96, 0), pose["hips"])
    spine = hips @ local_of((0, 0.12, 0), pose["spine"])
    chest = spine @ local_of((0, 0.18, 0), pose["chest"])
    neck = chest @ local_of((0, 0.20, 0), (0, 0, 0))
    head = neck @ local_of((0, 0.10, 0), pose["head"])
    arm_l = chest @ local_of((-0.22, 0.10, 0), pose["arm_l"])
    fore_l = arm_l @ local_of((0, -0.28, 0), pose["fore_l"])
    hand_l = fore_l @ local_of((0, -0.24, 0), (0, 0, 0))
    arm_r = chest @ local_of((0.22, 0.10, 0), pose["arm_r"])
    fore_r = arm_r @ local_of((0, -0.28, 0), pose["fore_r"])
    hand_r = fore_r @ local_of((0, -0.24, 0), pose["hand_r"])
    sword = hand_r @ local_of((0.02, -0.08, 0.06), pose["sword"])
    up_l = hips @ local_of((-0.11, -0.04, 0), pose["up_l"])
    leg_l = up_l @ local_of((0, -0.42, 0), pose["leg_l"])
    foot_l = leg_l @ local_of((0, -0.40, 0.05), pose["foot_l"])
    up_r = hips @ local_of((0.11, -0.04, 0), pose["up_r"])
    leg_r = up_r @ local_of((0, -0.42, 0), pose["leg_r"])
    foot_r = leg_r @ local_of((0, -0.40, 0.05), pose["foot_r"])
    scabbard = hips @ trans((0.20, -0.04, -0.02)) @ rot_euler(18, 0, 22)
    cape = chest @ trans((0, 0.08, -0.12))
    return {
        "head": head, "neck": neck, "chest": chest, "spine": spine, "hips": hips,
        "arm_l": arm_l, "fore_l": fore_l, "hand_l": hand_l,
        "arm_r": arm_r, "fore_r": fore_r, "hand_r": hand_r, "sword": sword,
        "up_l": up_l, "leg_l": leg_l, "foot_l": foot_l,
        "up_r": up_r, "leg_r": leg_r, "foot_r": foot_r,
        "scabbard": scabbard, "cape": cape, "root": root,
    }


def xform_p(m, p):
    h = m @ np.array([p[0], p[1], p[2], 1.0])
    return h[:3]


def world_knee_flex(bones, side):
    hip = xform_p(bones[f"up_{side}"], (0, 0, 0))
    knee = xform_p(bones[f"leg_{side}"], (0, 0, 0))
    ankle = xform_p(bones[f"foot_{side}"], (0, 0, 0))
    thigh = knee - hip
    shin = ankle - knee
    t = thigh / (np.linalg.norm(thigh) + 1e-8)
    s = shin / (np.linalg.norm(shin) + 1e-8)
    # Angle between thigh and shin *directions* (0 = straight, ~70 = pass flex).
    return math.degrees(math.acos(float(np.clip(np.dot(t, s), -1.0, 1.0))))


def phase_snapshot(name, t):
    pose = evaluate(t)
    bones = fk(pose)
    foot_l = xform_p(bones["foot_l"], (0, 0, 0))
    foot_r = xform_p(bones["foot_r"], (0, 0, 0))
    hand_l = xform_p(bones["hand_l"], (0, -0.04, 0))
    hand_r = xform_p(bones["hand_r"], (0, -0.04, 0))
    hip = xform_p(bones["hips"], (0, 0, 0))
    chest = xform_p(bones["chest"], (0, 0, 0))
    head = xform_p(bones["head"], (0, 0.12, 0))
    return {
        "name": name,
        "t": t,
        "hips": pose["hips"],
        "spine": pose["spine"],
        "up_l": pose["up_l"],
        "leg_l": pose["leg_l"],
        "up_r": pose["up_r"],
        "leg_r": pose["leg_r"],
        "arm_l": pose["arm_l"],
        "arm_r": pose["arm_r"],
        "foot_l": foot_l,
        "foot_r": foot_r,
        "hand_l": hand_l,
        "hand_r": hand_r,
        "hip": hip,
        "chest": chest,
        "head": head,
        "knee_flex_l": world_knee_flex(bones, "l"),
        "knee_flex_r": world_knee_flex(bones, "r"),
    }


def bone_drive_rows():
    return [
        phase_snapshot("PASS L", 0.0),
        phase_snapshot("CONTACT L", WALK_PERIOD * 0.25),
        phase_snapshot("PASS R", WALK_PERIOD * 0.5),
        phase_snapshot("CONTACT R", WALK_PERIOD * 0.75),
    ]


def format_bone_drive_table(rows):
    header = (
        "phase      | Hips.Y | Hips.Z | Spine.Y | hip.X | chest.X | head.X | "
        "UpL.X | LegL.X | wKneeL | UpR.X | LegR.X | ArmL.X | ArmR.X | "
        "footL.X | dFootY | stepZ"
    )
    lines = [
        "BONE DRIVE DUMP — SirAldric3DMotion.Evaluate == Actor BuildLoopClip source",
        "(PlayableGraph samples Evaluate() → localRotation xyzw; not a missing-clip miss.)",
        header,
        "-" * len(header),
    ]
    for r in rows:
        d_y = abs(r["foot_l"][1] - r["foot_r"][1])
        step_z = abs(r["foot_l"][2] - r["foot_r"][2])
        lines.append(
            f"{r['name']:<10} | {r['hips'][1]:6.1f} | {r['hips'][2]:6.1f} | {r['spine'][1]:7.1f} | "
            f"{r['hip'][0]:5.3f} | {r['chest'][0]:7.3f} | {r['head'][0]:6.3f} | "
            f"{r['up_l'][0]:5.1f} | {r['leg_l'][0]:6.1f} | {r['knee_flex_l']:6.1f} | "
            f"{r['up_r'][0]:5.1f} | {r['leg_r'][0]:6.1f} | {r['arm_l'][0]:6.1f} | "
            f"{r['arm_r'][0]:6.1f} | {r['foot_l'][0]:7.3f} | {d_y:6.3f} | {step_z:5.3f}"
        )
    pass_l, contact_l, pass_r, contact_r = rows
    arm_amp_l = max(abs(pass_l["arm_l"][0]), abs(contact_l["arm_l"][0]), abs(pass_r["arm_l"][0]), abs(contact_r["arm_l"][0]))
    arm_span_l = max(r["arm_l"][0] for r in rows) - min(r["arm_l"][0] for r in rows)
    arm_span_r = max(r["arm_r"][0] for r in rows) - min(r["arm_r"][0] for r in rows)
    lines.append(
        f"GATE knee pass L/R euler {pass_l['leg_l'][0]:.1f}/{pass_r['leg_r'][0]:.1f} "
        f"(need ≥50). world flex {pass_l['knee_flex_l']:.1f}/{pass_r['knee_flex_r']:.1f}."
    )
    lines.append(
        f"GATE arm span L={arm_span_l:.1f}° R={arm_span_r:.1f}° peak|X|={arm_amp_l:.1f}° "
        f"(near-zero span = remap miss)."
    )
    lines.append(
        f"GATE pass foot lift dY L={abs(pass_l['foot_l'][1] - pass_l['foot_r'][1]):.3f} "
        f"R={abs(pass_r['foot_r'][1] - pass_r['foot_l'][1]):.3f} "
        f"(need ≳0.12 tucked under pelvis; 14d9c17 0.38m was the back-kick)."
    )
    lines.append(
        f"GATE pass foot under pelvis |X| L={abs(pass_l['foot_l'][0]):.3f} "
        f"R={abs(pass_r['foot_r'][0]):.3f} (need <0.12; a6d4703 stance was 0.39 side-kick)."
    )
    c_step = abs(contact_l["foot_l"][2] - contact_l["foot_r"][2])
    lines.append(
        f"GATE contact stepZ={c_step:.3f} vs march-step {MARCH * WALK_PERIOD * 0.5:.3f} "
        f"(need |Δ|<0.10)."
    )
    peak_hip = peak_chest = peak_head = 0.0
    for i in range(40):
        snap = phase_snapshot("t", i * WALK_PERIOD / 40.0)
        peak_hip = max(peak_hip, abs(snap["hip"][0]))
        peak_chest = max(peak_chest, abs(snap["chest"][0]))
        peak_head = max(peak_head, abs(snap["head"][0]))
    lines.append(
        f"GATE lateral COM |X| peak (40-sample walk) hip={peak_hip:.3f} "
        f"chest={peak_chest:.3f} head={peak_head:.3f} "
        f"(need chest/head <0.04; ff81201 weaved ~0.11). Root X = 0."
    )
    dz_l = swing_foot_world_dz("l", 0.75)
    dz_r = swing_foot_world_dz("r", 0.25)
    lines.append(
        f"GATE pass-foot world ΔZ L-swing={dz_l['net']:+.3f} "
        f"(min step {dz_l['min_step']:+.3f}) R-swing={dz_r['net']:+.3f} "
        f"(min step {dz_r['min_step']:+.3f}) — must be + toward TOP; "
        f"14d9c17 min step was −0.039 (back-kick / −Z)."
    )
    lines.append(
        "EYE: pass foot tucks under the pelvis then steps toward TOP; "
        "calves/feet do not kick toward the camera."
    )
    return "\n".join(lines)


def foot_world_z(t, side):
    pose = evaluate(t)
    bones = fk(pose)
    return float(xform_p(bones[f"foot_{side}"], (0, 0, 0))[2])


def swing_foot_world_dz(side, start_u, samples=16):
    """World Z of the swing foot from opposite-contact through pass to lead-contact."""
    zs = [foot_world_z((start_u + i * 0.50 / samples) * WALK_PERIOD, side) for i in range(samples + 1)]
    steps = [zs[i + 1] - zs[i] for i in range(len(zs) - 1)]
    return {
        "net": zs[-1] - zs[0],
        "min_step": min(steps),
        "trail": zs[0],
        "pass": zs[samples // 2],
        "lead": zs[-1],
    }


def xform_n(m, n):
    nn = m[:3, :3] @ n
    return nn / (np.linalg.norm(nn) + 1e-8)


def uv_of(bind_world):
    u = clamp01((bind_world[0] + 0.32) / 0.64)
    v = clamp01(bind_world[1] / 1.86)
    px = BX0 + u * (BX1 - BX0)
    py = BY1 - v * (BY1 - BY0)
    return px, py


def sample_rear(px, py):
    x = int(np.clip(round(px), 0, REAR.shape[1] - 1))
    y = int(np.clip(round(py), 0, REAR.shape[0] - 1))
    rgb = REAR_RGB[y, x]
    a = REAR_A[y, x]
    if a < 0.12:
        return np.array(SILVER, np.float32)
    return rgb


def sample_atlas(u, v):
    h, w = ATLAS.shape[:2]
    x = int(np.clip(round(float(u) * (w - 1)), 0, w - 1))
    y = int(np.clip(round((1.0 - float(v)) * (h - 1)), 0, h - 1))
    return ATLAS[y, x]


def capsule_tris(a, b, radius, rings=5, segs=8):
    a = np.array(a, np.float64)
    b = np.array(b, np.float64)
    axis = b - a
    height = np.linalg.norm(axis)
    if height < 1e-5:
        return sphere_tris(a, radius)
    ny = axis / height
    ref = np.array([0.0, 1.0, 0.0]) if abs(ny[1]) < 0.9 else np.array([1.0, 0.0, 0.0])
    nx = np.cross(ref, ny)
    nx /= np.linalg.norm(nx)
    nz = np.cross(ny, nx)
    rings_pts = []
    for i in range(rings + 1):
        t = i / rings
        p = a * (1 - t) + b * t
        ring = []
        for s in range(segs):
            ang = s / segs * math.tau
            radial = math.cos(ang) * nx + math.sin(ang) * nz
            ring.append((p + radial * radius, radial))
        rings_pts.append(ring)
    tris = []
    for i in range(rings):
        for s in range(segs):
            s1 = (s + 1) % segs
            p00, n00 = rings_pts[i][s]
            p01, n01 = rings_pts[i][s1]
            p10, n10 = rings_pts[i + 1][s]
            p11, n11 = rings_pts[i + 1][s1]
            tris.append((p00, p10, p01, n00, n10, n01))
            tris.append((p01, p10, p11, n01, n10, n11))
    tris.extend(sphere_tris(a, radius))
    tris.extend(sphere_tris(b, radius))
    return tris


def sphere_tris(center, radius, slices=6, stacks=4):
    center = np.array(center, np.float64)
    pts = []
    for y in range(stacks + 1):
        v = y / stacks
        phi = v * math.pi
        row = []
        for x in range(slices + 1):
            u = x / slices
            th = u * math.tau
            n = np.array([math.sin(phi) * math.cos(th), math.cos(phi), math.sin(phi) * math.sin(th)])
            row.append((center + n * radius, n))
        pts.append(row)
    tris = []
    for y in range(stacks):
        for x in range(slices):
            p0, n0 = pts[y][x]
            p1, n1 = pts[y][x + 1]
            p2, n2 = pts[y + 1][x]
            p3, n3 = pts[y + 1][x + 1]
            tris.append((p0, p2, p1, n0, n2, n1))
            tris.append((p1, p2, p3, n1, n2, n3))
    return tris


# (bone, local_tris_with_uv)
PARTS = []


def add_part(bone, tris, textured=False, color=SILVER):
    PARTS.append((bone, tris))


def build_parts():
    PARTS.clear()
    PARTS.extend(load_blender_parts(MESH_PATH))


REST_FK = None


def rest_world(bone, local):
    return xform_p(REST_FK[bone], local)


LIGHT = np.array([0.28, 0.78, -0.55], np.float64)
LIGHT = LIGHT / np.linalg.norm(LIGHT)


def project(p, eye, r, u, f):
    d = p - eye
    cam = np.array([np.dot(d, r), np.dot(d, u), np.dot(d, f)])
    if cam[2] < 0.08:
        return None
    fov = math.radians(CAM_FOV)
    fy = 1.0 / math.tan(fov * 0.5)
    aspect = W / float(H)
    ndc_x = fy * cam[0] / (cam[2] * aspect)
    ndc_y = fy * cam[1] / cam[2]
    sx = (ndc_x + 1.0) * 0.5 * W
    sy = (1.0 - ndc_y) * 0.5 * H
    return sx, sy, cam[2]


def _tri_coverage(zbuf, p0, p1, p2):
    xs = (p0[0], p1[0], p2[0])
    ys = (p0[1], p1[1], p2[1])
    minx = max(0, int(math.floor(min(xs))))
    maxx = min(W - 1, int(math.ceil(max(xs))))
    miny = max(0, int(math.floor(min(ys))))
    maxy = min(H - 1, int(math.ceil(max(ys))))
    if maxx < minx or maxy < miny:
        return None
    denom = (p1[1] - p2[1]) * (p0[0] - p2[0]) + (p2[0] - p1[0]) * (p0[1] - p2[1])
    if abs(denom) < 1e-8:
        return None
    yy, xx = np.mgrid[miny : maxy + 1, minx : maxx + 1]
    a = ((p1[1] - p2[1]) * (xx - p2[0]) + (p2[0] - p1[0]) * (yy - p2[1])) / denom
    b = ((p2[1] - p0[1]) * (xx - p2[0]) + (p0[0] - p2[0]) * (yy - p2[1])) / denom
    c = 1.0 - a - b
    mask = (a >= 0) & (b >= 0) & (c >= 0)
    if not np.any(mask):
        return None
    z = a * p0[2] + b * p1[2] + c * p2[2]
    subz = zbuf[miny : maxy + 1, minx : maxx + 1]
    nearer = mask & (z < subz)
    if not np.any(nearer):
        return None
    return minx, maxx, miny, maxy, a, b, c, z, nearer, subz


def raster_tri(zbuf, cbuf, p0, p1, p2, c0, c1, c2):
    cov = _tri_coverage(zbuf, p0, p1, p2)
    if cov is None:
        return
    minx, maxx, miny, maxy, a, b, c, z, nearer, subz = cov
    subz[nearer] = z[nearer]
    col = a[..., None] * c0 + b[..., None] * c1 + c[..., None] * c2
    col = np.clip(col, 0, 255).astype(np.uint8)
    dest = cbuf[miny : maxy + 1, minx : maxx + 1]
    dest[nearer] = col[nearer]


def raster_tri_uv(zbuf, cbuf, p0, p1, p2, uv0, uv1, uv2, shade):
    """Per-pixel atlas sample so the lion / Greek-key stay readable."""
    cov = _tri_coverage(zbuf, p0, p1, p2)
    if cov is None:
        return
    minx, maxx, miny, maxy, a, b, c, z, nearer, subz = cov
    subz[nearer] = z[nearer]
    u = a * float(uv0[0]) + b * float(uv1[0]) + c * float(uv2[0])
    v = a * float(uv0[1]) + b * float(uv1[1]) + c * float(uv2[1])
    ah, aw = ATLAS.shape[0], ATLAS.shape[1]
    xs = np.clip(np.round(u * (aw - 1)).astype(np.int32), 0, aw - 1)
    ys = np.clip(np.round((1.0 - v) * (ah - 1)).astype(np.int32), 0, ah - 1)
    col = ATLAS[ys, xs] * shade
    col = np.clip(col, 0, 255).astype(np.uint8)
    dest = cbuf[miny : maxy + 1, minx : maxx + 1]
    dest[nearer] = col[nearer]


def draw_world_tri(zbuf, cbuf, eye, r, u, f, w0, w1, w2, n, color):
    shade = 0.32 + 0.68 * max(0.0, float(np.dot(n, LIGHT)))
    pts = []
    for w in (w0, w1, w2):
        pr = project(w, eye, r, u, f)
        if pr is None:
            return
        pts.append(pr)
    c = np.array(color, np.float32) * shade
    raster_tri(zbuf, cbuf, pts[0], pts[1], pts[2], c, c, c)


def box_world(center, size):
    e = np.array(size) * 0.5
    c = np.array(center)
    corners = np.array(
        [
            c + [-e[0], -e[1], -e[2]],
            c + [e[0], -e[1], -e[2]],
            c + [e[0], e[1], -e[2]],
            c + [-e[0], e[1], -e[2]],
            c + [-e[0], -e[1], e[2]],
            c + [e[0], -e[1], e[2]],
            c + [e[0], e[1], e[2]],
            c + [-e[0], e[1], e[2]],
        ],
        np.float64,
    )
    faces = (
        (0, 1, 2, 3, (0, 0, -1)),
        (5, 4, 7, 6, (0, 0, 1)),
        (4, 0, 3, 7, (-1, 0, 0)),
        (1, 5, 6, 2, (1, 0, 0)),
        (3, 2, 6, 7, (0, 1, 0)),
        (4, 5, 1, 0, (0, -1, 0)),
    )
    return corners, faces


def render_pose(pose):
    bones = fk(pose)
    eye = CAM_EYE
    target = CAM_TARGET
    forward = target - eye
    forward = forward / np.linalg.norm(forward)
    right = np.cross(np.array([0.0, 1.0, 0.0]), forward)
    right = right / np.linalg.norm(right)
    up = np.cross(forward, right)
    zbuf = np.full((H, W), 1e9, np.float32)
    cbuf = np.zeros((H, W, 3), np.uint8)
    cbuf[:] = (18, 20, 16)
    # Ground plane (z toward TOP of screen).
    for gz in np.linspace(-0.4, 3.2, 10):
        gx0, gx1 = -2.2, 2.2
        w0 = np.array([gx0, 0.0, gz])
        w1 = np.array([gx1, 0.0, gz])
        w2 = np.array([gx1, 0.0, gz + 0.36])
        w3 = np.array([gx0, 0.0, gz + 0.36])
        col = GROUND if int(round(gz * 4)) % 2 == 0 else (36, 42, 30)
        n = np.array([0.0, 1.0, 0.0])
        draw_world_tri(zbuf, cbuf, eye, right, up, forward, w0, w1, w2, n, col)
        draw_world_tri(zbuf, cbuf, eye, right, up, forward, w0, w2, w3, n, col)
    for i in range(6):
        z = 0.15 + i * 0.45
        corners, faces = box_world((0.0, 0.02, z), (0.55 - i * 0.04, 0.02, 0.10))
        for a, b, c, d, nloc in faces:
            n = np.array(nloc, np.float64)
            draw_world_tri(zbuf, cbuf, eye, right, up, forward, corners[a], corners[b], corners[c], n, GOLD)
            draw_world_tri(zbuf, cbuf, eye, right, up, forward, corners[a], corners[c], corners[d], n, GOLD)
        corners, faces = box_world((0.0, 0.28, 2.15), (0.38, 0.55, 0.38))
    keep = (56, 18, 30)
    for a, b, c, d, nloc in faces:
        n = np.array(nloc, np.float64)
        draw_world_tri(zbuf, cbuf, eye, right, up, forward, corners[a], corners[b], corners[c], n, keep)
        draw_world_tri(zbuf, cbuf, eye, right, up, forward, corners[a], corners[c], corners[d], n, keep)

    for bone, tris in PARTS:
        m = bones[bone]
        for p0, p1, p2, n0, n1, n2, uv0, uv1, uv2 in tris:
            w0, w1, w2 = xform_p(m, p0), xform_p(m, p1), xform_p(m, p2)
            nn = xform_n(m, (n0 + n1 + n2) / 3.0)
            shade = 0.48 + 0.52 * max(0.0, float(np.dot(nn, LIGHT)))
            prs = []
            uvs = []
            ok = True
            for wp, uv in ((w0, uv0), (w1, uv1), (w2, uv2)):
                pr = project(wp, eye, right, up, forward)
                if pr is None:
                    ok = False
                    break
                prs.append(pr)
                uvs.append(uv)
            if ok:
                raster_tri_uv(zbuf, cbuf, prs[0], prs[1], prs[2], uvs[0], uvs[1], uvs[2], shade)

    im = Image.fromarray(cbuf, "RGB")
    d = ImageDraw.Draw(im)
    try:
        title = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 28)
        phase = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
        note = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf", 18)
    except OSError:
        title = phase = note = ImageFont.load_default()
    d.text((W / 2, 56), "SIR ALDRIC  ·  walk → attack TOP", fill=(237, 230, 209), font=title, anchor="mm")
    d.text((W / 2, 96), "▲  TOP  ·  ENEMY", fill=(211, 176, 82), font=phase, anchor="mm")
    d.text((W / 2, 134), pose["label"], fill=(211, 176, 82), font=phase, anchor="mm")
    d.text(
        (W / 2, H - 48),
        "3D Animator  ·  high-angle rear  ·  march TOP  ·  no PNG warp",
        fill=(168, 172, 164),
        font=note,
        anchor="mm",
    )
    return im


def body_crop(im):
    return im.crop((80, 160, W - 80, H - 80))


def max_delta(a, b):
    diff = ImageChops.difference(a, b)
    return max(ImageStat.Stat(diff).extrema[c][1] for c in range(3))


def mean_delta(a, b):
    diff = ImageChops.difference(a, b)
    return sum(ImageStat.Stat(diff).mean) / 3.0


def hip_screen_y(pose):
    bones = fk(pose)
    hip = xform_p(bones["hips"], (0, 0, 0))
    forward = CAM_TARGET - CAM_EYE
    forward = forward / np.linalg.norm(forward)
    right = np.cross(np.array([0.0, 1.0, 0.0]), forward)
    right = right / np.linalg.norm(right)
    up = np.cross(forward, right)
    pr = project(hip, CAM_EYE, right, up, forward)
    return pr[1] if pr else None


def main():
    global REST_FK
    REST_FK = fk(evaluate(0.0) | {"root_y": 0.0, "root_z": 0.0})
    build_parts()

    y0 = hip_screen_y(evaluate(0.05))
    y1 = hip_screen_y(evaluate(WALK_BLOCK - 0.05))
    if y0 is None or y1 is None or y1 >= y0 - 8:
        raise SystemExit(f"FAIL orientation: hips must move toward TOP (smaller sy). sy0={y0} sy1={y1}")

    def sword_vs_hip_z(t):
        pose = evaluate(t)
        bones = fk(pose)
        hip = xform_p(bones["hips"], (0, 0, 0))
        tip = xform_p(bones["sword"], (0.01, -0.58, 0.04))
        hand = xform_p(bones["hand_r"], (0, -0.04, 0))
        return hip[2], hand[2], tip[2]

    for t, name in [(WALK_PERIOD * 0.25, "walk"), (WALK_BLOCK + ATTACK * 0.48, "strike")]:
        hz, handz, tipz = sword_vs_hip_z(t)
        if handz < hz - 0.02 or tipz < hz - 0.05:
            raise SystemExit(
                f"FAIL arms/sword toward camera: {name} hipZ={hz:.3f} handZ={handz:.3f} tipZ={tipz:.3f} "
                "(need hand/tip further +Z / TOP than hips)"
            )
        scab = xform_p(fk(evaluate(t))["scabbard"], (0.02, -0.24, 0))
        if scab[0] < 0.12:
            raise SystemExit(f"FAIL scabbard not character-right: {name} scabX={scab[0]:.3f}")

    brown_bones = {bone for bone, _ in PARTS if bone == "scabbard"}
    if "scabbard" not in brown_bones:
        raise SystemExit("FAIL missing character-right scabbard mesh")
    if any(bone == "cape" for bone, _ in PARTS):
        raise SystemExit("FAIL cape/back-sheath mesh still present")

    rows = bone_drive_rows()
    dump = format_bone_drive_table(rows)
    print(dump, flush=True)
    if "--dump" in sys.argv:
        return

    fps = 16
    n = int(LOOP * fps)
    raw = Path("/tmp/aldric-3d")
    if raw.exists():
        shutil.rmtree(raw)
    raw.mkdir()
    frames = []
    for i in range(n):
        im = render_pose(evaluate(i / fps))
        im.save(raw / f"f_{i:03d}.png")
        frames.append(im)
        print(f"{i + 1}/{n}", flush=True)

    walk_pass_l = render_pose(evaluate(0.0))
    walk_contact_l = render_pose(evaluate(WALK_PERIOD * 0.25))
    walk_pass_r = render_pose(evaluate(WALK_PERIOD * 0.5))
    walk_contact_r = render_pose(evaluate(WALK_PERIOD * 0.75))
    strike = render_pose(evaluate(WALK_BLOCK + ATTACK * 0.48))

    pass_l = evaluate(0.0)
    pass_r = evaluate(WALK_PERIOD * 0.5)
    contact_l = evaluate(WALK_PERIOD * 0.25)
    if not (52 <= pass_l["leg_l"][0] <= 82 and 52 <= pass_r["leg_r"][0] <= 82):
        raise SystemExit(
            f"FAIL gait-bar pass knee: L={pass_l['leg_l'][0]:.1f} R={pass_r['leg_r'][0]:.1f} (need 52–82)"
        )
    if pass_l["hips"][2] < 5 or pass_r["hips"][2] > -5:
        raise SystemExit(f"FAIL hip drop: passL Z={pass_l['hips'][2]:.1f} passR Z={pass_r['hips'][2]:.1f}")
    peak_chest = 0.0
    peak_head = 0.0
    peak_hip = 0.0
    for i in range(40):
        snap = phase_snapshot("t", i * WALK_PERIOD / 40.0)
        peak_chest = max(peak_chest, abs(snap["chest"][0]))
        peak_head = max(peak_head, abs(snap["head"][0]))
        peak_hip = max(peak_hip, abs(snap["hip"][0]))
    if peak_chest > 0.04 or peak_head > 0.04:
        raise SystemExit(
            f"FAIL lateral weave: peak |chest.X|={peak_chest:.3f} |head.X|={peak_head:.3f} "
            "(need <0.04 m — body must stay on the +Z line)"
        )
    if contact_l["hips"][1] <= 4 or contact_l["spine"][1] >= -4:
        raise SystemExit("FAIL shoulder–hip counter-rotation at contact L")
    pass_l_row, _, pass_r_row, _ = rows
    if pass_l["up_l"][0] >= 0:
        raise SystemExit(
            f"FAIL pass-L thigh still +X (back-kick toward camera): {pass_l['up_l'][0]:.1f} "
            "(14d9c17; need −X so the swing foot travels +Z / TOP)"
        )
    if pass_r["up_r"][0] >= 0:
        raise SystemExit(
            f"FAIL pass-R thigh still +X (back-kick toward camera): {pass_r['up_r'][0]:.1f}"
        )
    dz_l = swing_foot_world_dz("l", 0.75)
    dz_r = swing_foot_world_dz("r", 0.25)
    if dz_l["min_step"] < 0 or dz_r["min_step"] < 0:
        raise SystemExit(
            f"FAIL swing foot world ΔZ not toward TOP: "
            f"L min={dz_l['min_step']:+.3f} net={dz_l['net']:+.3f} "
            f"R min={dz_r['min_step']:+.3f} net={dz_r['net']:+.3f} "
            "(must stay + during swing; 14d9c17 kicked −Z toward camera)"
        )
    if abs(pass_l_row["foot_l"][1] - pass_l_row["foot_r"][1]) < 0.12:
        raise SystemExit(
            f"FAIL passing-L foot tuck/lift missing: "
            f"L={pass_l_row['foot_l'][1]:.3f} R={pass_l_row['foot_r'][1]:.3f} "
            "(need dY≳0.12 under the pelvis; 14d9c17 0.38m lift was the back-kick)"
        )
    if abs(pass_r_row["foot_r"][1] - pass_r_row["foot_l"][1]) < 0.12:
        raise SystemExit(
            f"FAIL passing-R foot tuck/lift missing: "
            f"L={pass_r_row['foot_l'][1]:.3f} R={pass_r_row['foot_r'][1]:.3f}"
        )
    if abs(pass_l_row["foot_l"][0]) > 0.12 or abs(pass_l_row["foot_r"][0]) > 0.22:
        raise SystemExit(
            f"FAIL pass-L side kick: footL.x={pass_l_row['foot_l'][0]:.3f} "
            f"footR.x={pass_l_row['foot_r'][0]:.3f} (pass under pelvis, stance <0.22)"
        )
    if abs(pass_r_row["foot_r"][0]) > 0.12 or abs(pass_r_row["foot_l"][0]) > 0.22:
        raise SystemExit(
            f"FAIL pass-R side kick: footL.x={pass_r_row['foot_l'][0]:.3f} "
            f"footR.x={pass_r_row['foot_r'][0]:.3f}"
        )
    bones_c = fk(contact_l)
    step_l = xform_p(bones_c["foot_l"], (0, 0, 0))
    step_r = xform_p(bones_c["foot_r"], (0, 0, 0))
    step_len = abs(step_l[2] - step_r[2])
    if pass_l["arm_l"][0] <= 8 or pass_l["arm_r"][0] >= -4:
        raise SystemExit(
            f"FAIL pinned/missing pendulum at pass L: armL={pass_l['arm_l'][0]:.1f} armR={pass_l['arm_r'][0]:.1f}"
        )
    if contact_l["arm_l"][0] <= 8 or contact_l["arm_r"][0] >= -8:
        raise SystemExit(
            f"FAIL contralateral at contact L: armL={contact_l['arm_l'][0]:.1f} armR={contact_l['arm_r'][0]:.1f}"
        )
    expected = MARCH * WALK_PERIOD * 0.5
    if abs(step_len - expected) > 0.10:
        raise SystemExit(f"FAIL stride/speed slide: step={step_len:.3f} vs march-step={expected:.3f}")

    def scabbard_brown(im):
        arr = np.array(im)
        r, g, b = (arr[:, :, 0].astype(np.int16), arr[:, :, 1].astype(np.int16), arr[:, :, 2].astype(np.int16))
        # Leather only — exclude shaded gold lion/trim (g stays high vs r).
        return (r > 55) & (r < 140) & (g > 28) & (g < 78) & (b < 58) & (r > g + 16) & (g > b)

    def look_counts(im):
        arr = np.array(im)
        body = arr[int(H * 0.22) : int(H * 0.78), int(W * 0.28) : int(W * 0.72)]
        r, g, b = body[:, :, 0].astype(np.int16), body[:, :, 1].astype(np.int16), body[:, :, 2].astype(np.int16)
        blue = (b > 40) & (b > r + 12) & (b > g)
        gold = (r > 110) & (g > 80) & (r > b + 18)
        grey = (r > 130) & (g > 130) & (b > 130) & (np.abs(r - g) < 22) & (np.abs(g - b) < 22)
        return int(blue.sum()), int(gold.sum()), int(grey.sum())

    for im, name in ((walk_contact_l, "walk"), (strike, "strike")):
        mask = scabbard_brown(im)
        # Window is tall: RootZ march moves the hip toward TOP (smaller sy) on strike.
        left_upper = int(mask[int(H * 0.18) : int(H * 0.34), int(W * 0.26) : int(W * 0.42)].sum())
        right_hip = int(mask[int(H * 0.28) : int(H * 0.70), int(W * 0.52) : int(W * 0.80)].sum())
        if right_hip < 250:
            raise SystemExit(f"FAIL missing character-right scabbard on {name}: right-hip brown px={right_hip}")
        if left_upper > 200:
            raise SystemExit(f"FAIL back/left sheath on {name}: left-upper brown px={left_upper}")
    blue_px, gold_px, grey_px = look_counts(walk_contact_l)
    if blue_px < 2500:
        raise SystemExit(f"FAIL look: royal-blue surcoat not readable ({blue_px} px) — still a grey capsule?")
    if gold_px < 800:
        raise SystemExit(f"FAIL look: gold lion/trim not readable ({gold_px} px)")
    if blue_px < grey_px * 0.12:
        raise SystemExit(f"FAIL look: body still reads grey capsule (blue={blue_px} grey={grey_px})")
    d_walk = max_delta(body_crop(walk_contact_l), body_crop(walk_contact_r))
    d_pass = max_delta(body_crop(walk_pass_l), body_crop(walk_contact_l))
    d_strike = max_delta(body_crop(walk_contact_l), body_crop(strike))
    m_walk = mean_delta(body_crop(walk_contact_l), body_crop(walk_contact_r))
    m_strike = mean_delta(body_crop(walk_contact_l), body_crop(strike))
    proof = (
        "PATH B Blender/FBX (5916447 motion HOLD): box-atlas stopped. Sir Aldric is a "
        "Blender mid-poly (sir_aldric.fbx + mesh.txt) with look_targets atlas (lion + "
        "Greek-key from 01_rear_LOCKED). Scabbard character-right. Boots silver/gold. "
        "Play hub stays locked rear PNG. "
        f"Hips screen-Y {y0:.0f}→{y1:.0f} (toward TOP). "
        f"Pass knee {pass_l['leg_l'][0]:.0f}°/{pass_r['leg_r'][0]:.0f}°. "
        f"step {step_len:.2f}m vs march-step {expected:.2f}m. "
        f"pass-foot world ΔZ L {dz_l['net']:+.3f} (min {dz_l['min_step']:+.3f}) "
        f"R {dz_r['net']:+.3f} (min {dz_r['min_step']:+.3f}). "
        f"LOOK px blue={blue_px} gold={gold_px} grey={grey_px} "
        "(need blue≥2500 gold≥800 — not a grey capsule). "
        f"max |Δ| walk {d_walk:.0f}/255; pass vs contact {d_pass:.0f}/255; "
        f"walk vs strike {d_strike:.0f}/255. "
        "Single brown scabbard character-right. High-angle rear +Z = TOP.\n"
        "EYE motion: pass foot tucks under the pelvis then steps toward TOP.\n"
        "LOOK gate is NOT claimed. Derek must recognize the locked painted knight.\n"
        "PIXEL Δ does not override the eye test.\n"
        + dump
    )
    print(proof)
    if d_walk < 18 or d_strike < 18 or d_pass < 18:
        raise SystemExit("FAIL: 3D motion too weak: " + proof)

    def fit_sample(path):
        src = Image.open(path).convert("RGB")
        arr = np.array(src)
        ink = arr.min(axis=2) < 240
        ys, xs = np.where(ink)
        if len(xs) == 0:
            return src
        pad = 18
        crop = src.crop(
            (
                max(0, int(xs.min()) - pad),
                max(0, int(ys.min()) - pad),
                min(src.width, int(xs.max()) + pad),
                min(src.height, int(ys.max()) + pad),
            )
        )
        cell = Image.new("RGB", (540, 480), (248, 248, 246))
        scale = min((540 - 32) / crop.width, (480 - 72) / crop.height)
        nw, nh = int(crop.width * scale), int(crop.height * scale)
        placed = crop.resize((nw, nh), Image.BILINEAR)
        cell.paste(placed, ((540 - nw) // 2, 56 + (480 - 72 - nh) // 2))
        return cell

    def fit_aldric(im):
        arr = np.array(im)
        rgb = arr.astype(np.int16)
        # Titles/footer and yellow track ticks must not set the bbox or the knight shrinks.
        ignore = np.zeros(arr.shape[:2], dtype=bool)
        ignore[:150, :] = True
        ignore[-90:, :] = True
        dark = arr.max(axis=2) < 48
        yellow = (rgb[:, :, 0] > 150) & (rgb[:, :, 1] > 130) & (rgb[:, :, 2] < 130)
        vis = ~dark & ~yellow & ~ignore
        ys, xs = np.where(vis)
        if len(xs) == 0:
            crop = im.crop((220, 280, W - 220, H - 200))
        else:
            pad = 36
            crop = im.crop(
                (
                    max(0, int(xs.min()) - pad),
                    max(0, int(ys.min()) - pad),
                    min(im.width, int(xs.max()) + pad),
                    min(im.height, int(ys.max()) + pad),
                )
            )
        cell = Image.new("RGB", (540, 480), (18, 20, 16))
        scale = min((540 - 28) / crop.width, (480 - 64) / crop.height)
        nw, nh = max(1, int(crop.width * scale)), max(1, int(crop.height * scale))
        placed = crop.resize((nw, nh), Image.BILINEAR)
        cell.paste(placed, ((540 - nw) // 2, 48 + (480 - 64 - nh) // 2))
        return cell

    sample_cells = [
        (fit_sample(PHASES / "pass_l_rear.png"), "SAMPLE  ·  PASS L"),
        (fit_sample(PHASES / "contact_l_rear.png"), "SAMPLE  ·  CONTACT L"),
        (fit_sample(PHASES / "pass_r_rear.png"), "SAMPLE  ·  PASS R"),
        (fit_sample(PHASES / "contact_r_rear.png"), "SAMPLE  ·  CONTACT R"),
    ]
    aldric_cells = [
        (fit_aldric(walk_pass_l), "ALDRIC  ·  PASS L"),
        (fit_aldric(walk_contact_l), "ALDRIC  ·  CONTACT L"),
        (fit_aldric(walk_pass_r), "ALDRIC  ·  PASS R"),
        (fit_aldric(walk_contact_r), "ALDRIC  ·  CONTACT R"),
    ]
    sheet = Image.new("RGB", (W, H), (18, 20, 16))
    font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 20)
    # 4 rows × 2 cols: each phase is sample | aldric side-by-side, stacked passL / contactL / passR / contactR.
    order = [
        (sample_cells[0], aldric_cells[0]),
        (sample_cells[1], aldric_cells[1]),
        (sample_cells[2], aldric_cells[2]),
        (sample_cells[3], aldric_cells[3]),
    ]
    for row, (samp, ald) in enumerate(order):
        for col, (im, lab) in enumerate((samp, ald)):
            x, y = col * 540, row * 480
            sheet.paste(im, (x, y))
            ImageDraw.Draw(sheet).text((x + 14, y + 12), lab, fill=(237, 230, 209) if col else (40, 40, 38), font=font)

    still = OUT / "sir_aldric_locked_rear_gameview_1080x1920.png"
    walk_contact_l.save(still, optimize=True)
    sheet_path = OUT / "sir_aldric_walk_draw_strike_recover_sheet.png"
    sheet.save(sheet_path, optimize=True)

    look_sheet = Image.new("RGB", (W, H), (18, 20, 16))
    look_font = ImageFont.truetype("/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf", 22)
    look_left = fit_sample(LOOK / "01_rear_LOCKED.png")
    look_left = look_left.resize((540, 960), Image.BILINEAR)
    look_right = fit_aldric(walk_contact_l)
    look_right = look_right.resize((540, 960), Image.BILINEAR)
    look_sheet.paste(look_left, (0, 200))
    look_sheet.paste(look_right, (540, 200))
    ImageDraw.Draw(look_sheet).text((40, 48), "LOOK  ·  01_rear_LOCKED  vs  Game-view still", fill=(237, 230, 209), font=look_font)
    ImageDraw.Draw(look_sheet).text((40, 88), "Look gate NOT claimed", fill=(211, 176, 82), font=look_font)
    ImageDraw.Draw(look_sheet).text((24, 220), "01_rear SoT", fill=(40, 40, 38), font=font)
    ImageDraw.Draw(look_sheet).text((564, 220), "ALDRIC Blender / FBX", fill=(237, 230, 209), font=font)
    look_path = OUT / "sir_aldric_look_vs_01_rear.png"
    look_sheet.save(look_path, optimize=True)
    gif_full = OUT / "sir_aldric_walk_attack_toward_top.gif"
    gif_walk = OUT / "sir_aldric_locked_master_walks_toward_top.gif"
    pal = [f.convert("P", palette=Image.ADAPTIVE, colors=64) for f in frames]
    pal[0].save(gif_full, save_all=True, append_images=pal[1:], duration=int(1000 / fps), loop=0, optimize=True)
    nw = int(WALK_BLOCK * fps)
    pal[0].save(gif_walk, save_all=True, append_images=pal[1:nw], duration=int(1000 / fps), loop=0, optimize=True)
    mp4 = OUT / "sir_aldric_walk_attack_toward_top.mp4"
    subprocess.check_call(
        [
            "ffmpeg",
            "-y",
            "-framerate",
            str(fps),
            "-i",
            str(raw / "f_%03d.png"),
            "-pix_fmt",
            "yuv420p",
            "-vf",
            "scale=1080:1920",
            "-crf",
            "18",
            "-movflags",
            "+faststart",
            str(mp4),
        ]
    )
    (OUT / "PIXEL_PROOF.txt").write_text(proof + "\n")
    ART.mkdir(parents=True, exist_ok=True)
    for p in (still, sheet_path, look_path, gif_full, gif_walk, mp4, OUT / "PIXEL_PROOF.txt"):
        dest = ART / f"aldric_gait_{p.name}"
        shutil.copy2(p, dest)
        print("wrote", p, p.stat().st_size)


if __name__ == "__main__":
    main()
