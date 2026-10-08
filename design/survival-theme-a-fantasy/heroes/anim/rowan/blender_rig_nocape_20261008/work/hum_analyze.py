"""Per-character wrapper (ROWAN_nocape 2026-10-08). Quiver verts are excluded and arms / legs / face are
overridden (see /workspace/rig_20261008/joint_override.py); joint positions measured from arm cross-section
centroids of the split body (ROWAN_body_verts.npy) and checked on the front/side bone overlay."""
import importlib.util, sys, numpy as np
_s=importlib.util.spec_from_file_location('hum_analyze_shared','/workspace/design/_shared_rig_pipeline/hum_analyze.py')
_m=importlib.util.module_from_spec(_s); _s.loader.exec_module(_m)
sys.path.insert(0,'/workspace/rig_20261008'); import quiver, joint_override
ARMS=dict(
 RA=dict(clav_in=[-0.03,0.03,1.505],shoulder=[-0.215,0.04,1.49],arm=[-0.235,0.04,1.485],elbow=[-0.355,0.045,1.21],wrist=[-0.45,-0.025,1.00],hand=[-0.51,-0.075,0.87]),
 LA=dict(clav_in=[0.03,0.0,1.505],shoulder=[0.215,-0.02,1.49],arm=[0.235,-0.025,1.485],elbow=[0.33,-0.095,1.21],wrist=[0.335,-0.17,0.99],hand=[0.325,-0.21,0.82]))
NECK=dict(neck_end=[0.0,-0.035,1.635],head=[0.0,-0.035,1.635],head_end=[0.0,-0.035,1.90])
def analyze_humanoid(co):
    keep=quiver.quiver_weight(co)<=0.0
    print('[wrapper] quiver verts excluded:', int((~keep).sum()))
    J=_m.analyze_humanoid(co[keep])
    return joint_override.apply(J, co[keep], ARMS, NECK)
