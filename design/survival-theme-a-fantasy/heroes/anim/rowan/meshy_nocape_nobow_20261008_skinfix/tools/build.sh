#!/usr/bin/env bash
# Rowan Meshy no-bow skin fix (2026-10-08). Rebuilds the three Assets body FBXs from Design's committed
# no-bow pack + the untouched Meshy source it was cut from. Needs Blender 4.3 on PATH, python3 + numpy.
#   bash build.sh <repo root> <output dir>
set -euo pipefail
here="$(cd "$(dirname "$0")" && pwd)"
repo="$(cd "$1" && pwd)"; out="$2"; mkdir -p "$out"; out="$(cd "$out" && pwd)"
base="$repo/design/survival-theme-a-fantasy/heroes/anim/rowan"
src="$base/meshy_nocape_20261008"; nb="$base/meshy_nocape_nobow_20261008"
export SRC_WORK="${SRC_WORK:-$(mktemp -d)}" NB_WORK="${NB_WORK:-$(mktemp -d)}"
mkdir -p "$SRC_WORK/stretch_orig" "$NB_WORK/stretch_orig" "$NB_WORK/stretch"
# bind pose + Meshy weights of the untouched source and of Design's no-bow body
for w in "$SRC_WORK:$src/ROWAN_meshy_walk.fbx" "$NB_WORK:$nb/ROWAN_meshy_nobow_walk.fbx"; do
  ROWAN_FIX_WORK="${w%%:*}" blender -b --python "$here/dump_bind_bones.py" -- "${w#*:}" >/dev/null 2>&1
  ROWAN_FIX_WORK="${w%%:*}" blender -b --python "$here/dump_bind_weights.py" -- "${w#*:}" >/dev/null 2>&1
done
# 1) how far each edge stretches with Meshy's own skin, every frame (source decides keep-vs-rebuild;
#    the no-bow numbers are the "before" column)
for c in rest walk attack; do
  blender -b --python "$here/stretch.py" -- "$src/ROWAN_meshy_$c.fbx" "$SRC_WORK/stretch_orig/st_$c.npz" 1 2>&1 | grep STRETCH
  blender -b --python "$here/stretch.py" -- "$nb/ROWAN_meshy_nobow_$c.fbx" "$NB_WORK/stretch_orig/st_$c.npz" 1 2>&1 | grep STRETCH
done
# 2) weights only, measure again, 3) weights + fist/pouch weld split
for c in rest walk attack; do SPLIT=0 python3 "$here/port_nobow.py" "$src/ROWAN_meshy_walk.fbx" "$nb/ROWAN_meshy_nobow_$c.fbx" "$NB_WORK/pass1_$c.fbx"; done
for c in rest walk attack; do
  blender -b --python "$here/stretch.py" -- "$NB_WORK/pass1_$c.fbx" "$NB_WORK/stretch/st_$c.npz" 1 2>&1 | grep STRETCH
done
for c in rest walk attack; do python3 "$here/port_nobow.py" "$src/ROWAN_meshy_walk.fbx" "$nb/ROWAN_meshy_nobow_$c.fbx" "$out/ROWAN_meshy_nobow_$c.fbx"; done
for c in rest walk attack; do
  blender -b --python "$here/stretch.py" -- "$out/ROWAN_meshy_nobow_$c.fbx" "$NB_WORK/st_final_$c.npz" 1 2>&1 | grep STRETCH
done
md5sum "$out"/ROWAN_meshy_nobow_*.fbx
