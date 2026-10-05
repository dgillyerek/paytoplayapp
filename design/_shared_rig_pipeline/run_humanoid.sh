#!/bin/bash
set -e
NAME="$1"; GLB="$2"; OUT="$3"
mkdir -p "$OUT"/{work,stills}
LOG="$OUT/work/batch.log"
echo "=== START $NAME $(date) ===" | tee -a "$LOG"
run_stage() {
  local stage=$1
  echo "--- stage $stage ---" | tee -a "$LOG"
  if ! blender -b --python /workspace/design/_shared_rig_pipeline/hum_pipeline.py -- \
      --glb "$GLB" --out "$OUT" --name "$NAME" --target-faces 200000 --stage "$stage" \
      >>"$LOG" 2>&1; then
    echo "STAGE $stage FAILED" | tee -a "$LOG"
    return 1
  fi
}
if ! run_stage prep; then
  echo -e "# FAIL $NAME\n\nprep/import failed. See work/batch.log\n" > "$OUT/FAIL.md"
  exit 2
fi
if ! run_stage build; then
  # FAIL.md may already exist from pipeline
  [ -f "$OUT/FAIL.md" ] || echo -e "# FAIL $NAME\n\nbuild/skin failed. See work/batch.log\n" > "$OUT/FAIL.md"
  exit 2
fi
run_stage pose || true
run_stage export || true
run_stage verify || true
# success criteria: blend + fbx exist and verify has 0 unweighted
if [ -f "$OUT/${NAME}_blenderig.blend" ] && [ -f "$OUT/${NAME}_blenderig.fbx" ]; then
  python3 - <<PY
import json,os
v=os.path.join("$OUT","work","verify.json")
ok=False
if os.path.exists(v):
  d=json.load(open(v))
  m=d.get("rig",{}).get("meshes",[{}])[0]
  ok = m.get("unweighted",99)==0 and m.get("faces",0)>1000 and d.get("rig",{}).get("bones",0)>=15
open(os.path.join("$OUT","PASS.md" if ok else "FAIL.md"),"w").write(
  ("# PASS $NAME\n\nAuto humanoid Mixamo rig. See stills/ and sheet.\n" if ok else "# FAIL $NAME\n\nVerify did not confirm skinned mesh.\n")
)
print("RESULT","PASS" if ok else "FAIL")
PY
else
  echo -e "# FAIL $NAME\n\nMissing blend/fbx outputs.\n" > "$OUT/FAIL.md"
  echo RESULT FAIL
fi
echo "=== END $NAME $(date) ===" | tee -a "$LOG"
