"""Per-character wrapper (LYRA_tighten 2026-10-08). Arms / neck-head / legs / face overridden
(see /workspace/rig_20261008/joint_override.py); positions measured on the front/side ortho renders + arm
cross-section centroids, checked on the bone overlay."""
import importlib.util, sys, numpy as np
_s=importlib.util.spec_from_file_location('hum_analyze_shared','/workspace/design/_shared_rig_pipeline/hum_analyze.py')
_m=importlib.util.module_from_spec(_s); _s.loader.exec_module(_m)
sys.path.insert(0,'/workspace/rig_20261008'); import joint_override
ARMS=dict(
 RA=dict(clav_in=[-0.025,0.0,1.585],shoulder=[-0.12,0.0,1.565],arm=[-0.14,0.0,1.555],elbow=[-0.315,-0.01,1.235],wrist=[-0.45,-0.07,1.265],hand=[-0.55,-0.11,1.255]),
 LA=dict(clav_in=[0.025,0.0,1.585],shoulder=[0.12,0.0,1.565],arm=[0.14,0.0,1.555],elbow=[0.215,-0.04,1.21],wrist=[0.265,-0.09,1.045],hand=[0.275,-0.11,0.92]))
NECK=dict(spine2_tail=[0.0,0.0,1.585],neck=[0.0,0.0,1.585],neck_end=[0.0,0.0,1.665],head=[0.0,0.0,1.665],head_end=[0.0,0.0,1.87])
def analyze_humanoid(co):
    J=_m.analyze_humanoid(co)
    return joint_override.apply(J, co, ARMS, NECK)
