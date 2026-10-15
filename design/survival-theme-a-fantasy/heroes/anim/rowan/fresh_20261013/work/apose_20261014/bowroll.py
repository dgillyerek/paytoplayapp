"""Rowan bow roll about its long axis inside the LeftHand grip (degrees, applied after the grip-axis flip RFIX = grip.json grip_rot_local).
Rest/walk carry: string away from the body (ROLL_REST). Attack: eases to ROLL_DRAW (string toward him) from F_A to the nock at F_B,
holds through the release (f14) to F_C, eases back by F_E. Mirrored exactly in Unity by BlenderBoneGrip.RollDeg (RowanAttack.BowGrip)."""
import math, os, json
from mathutils import Matrix
_c=json.load(open(os.path.join(os.path.dirname(__file__),'roll_pick.json'))) if os.path.exists(os.path.join(os.path.dirname(__file__),'roll_pick.json')) else {}
ROLL_REST=float(_c.get('rest',180.0)); ROLL_DRAW=float(_c.get('draw',0.0)); F_A,F_B,F_C,F_E=2.0,8.0,17.0,24.0
def ss(x): x=max(0.0,min(1.0,x)); return x*x*(3-2*x)
def roll(clip,f,rr=None,rd=None):
    rr=ROLL_REST if rr is None else rr; rd=ROLL_DRAW if rd is None else rd
    if clip!='attack' or f<=F_A or f>=F_E: return rr
    if f<F_B: return rr+(rd-rr)*ss((f-F_A)/(F_B-F_A))
    if f<=F_C: return rd
    return rd+(rr-rd)*ss((f-F_C)/(F_E-F_C))
RFIX=Matrix(((-1,0,0),(0,-1,0),(0,0,1)))
def bowrot(clip,f,rr=None,rd=None): return RFIX@Matrix.Rotation(math.radians(roll(clip,f,rr,rd)),3,'Z')
