#!/bin/bash
NAME=$1; GLB=$2; OUT=$3; shift 3
for st in "$@"; do
  echo "--- $NAME stage $st $(date +%T)"
  blender -b --python /workspace/design/_shared_rig_pipeline/hum_pipeline.py -- --glb "$GLB" --out "$OUT" --name "$NAME" --target-faces 200000 --stage $st >> "$OUT/work/batch.log" 2>&1 || { echo "STAGE $st FAILED"; exit 2; }
done
echo "--- $NAME done $(date +%T)"
