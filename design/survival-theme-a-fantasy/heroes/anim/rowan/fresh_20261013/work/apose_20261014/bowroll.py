"""Rowan bow roll about its long axis inside the LeftHand grip (degrees, applied after the grip-axis flip RFIX = grip.json grip_rot_local).
v5c 2026-10-15 (Derek): carried with the STRING UP, pressed along the inside of the forearm, limb outside: carry roll ROLL_REST ~0 (anim.py
CARRY solves the hand from the bow frame; the string side points back along the hand to the forearm). Attack: smoothstep to ROLL_DRAW
(50, string toward him) over f2-f10, held through the f14 release to f17, back by f26. Mirrored in Unity by BlenderBoneGrip.RollDeg."""
import json,math, os, json
from mathutils import Matrix
_c=json.load(open(os.path.join(os.path.dirname(__file__),'roll_pick.json'))) if os.path.exists(os.path.join(os.path.dirname(__file__),'roll_pick.json')) else {}
ROLL_REST=float(os.environ.get('C_ROLL',_c.get('rest',0.0))); ROLL_DRAW=float(os.environ.get('C_RD',_c.get('draw',50.0))); F_A,F_B,F_C,F_E=[float(x) for x in os.environ.get('C_KEYS','1,11,17,27').split(',')]
def ss(x): x=max(0.0,min(1.0,x)); return x*x*(3-2*x)
def roll(clip,f,rr=None,rd=None):
    rr=ROLL_REST if rr is None else rr; rd=ROLL_DRAW if rd is None else rd
    if clip!='attack' or f<=F_A or f>=F_E: return rr
    if f<F_B: return rr+(rd-rr)*ss((f-F_A)/(F_B-F_A))
    if f<=F_C: return rd
    return rd+(rr-rd)*ss((f-F_C)/(F_E-F_C))
RFIX=Matrix(json.load(open('/workspace/rowan_fresh/w2/grip.json'))['grip_rot_local'])   # v5g: includes the 7 deg palm seat tilt
def bowrot(clip,f,rr=None,rd=None): return RFIX@Matrix.Rotation(math.radians(roll(clip,f,rr,rd)),3,'Z')
