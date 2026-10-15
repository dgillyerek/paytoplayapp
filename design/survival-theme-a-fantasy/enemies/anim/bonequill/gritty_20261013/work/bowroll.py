"""Bow roll about its long axis inside the LeftHand grip (degrees, applied after the grip-axis flip RFIX).
Rest/walk carry: string outward along the inner forearm (270). Attack: eases to 30 (string toward him; the string sweeps round the outside, away from the body) from f2 to the nock at f10, holds through
the release at f17, eases back by f26. Mirrored exactly in Unity by BlenderBoneGrip.RollDeg."""
import math
from mathutils import Matrix
import os
ROLL_REST=float(os.environ.get('C_ROLL',270)); ROLL_DRAW=float(os.environ.get('C_RD',30)); F_A,F_B,F_C,F_E=2.0,10.0,17.0,26.0
def ss(x): x=max(0.0,min(1.0,x)); return x*x*(3-2*x)
def roll(clip,f):
    if clip!='attack' or f<=F_A or f>=F_E: return ROLL_REST
    if f<F_B: return ROLL_REST+(ROLL_DRAW-ROLL_REST)*ss((f-F_A)/(F_B-F_A))
    if f<=F_C: return ROLL_DRAW
    return ROLL_DRAW+(ROLL_REST-ROLL_DRAW)*ss((f-F_C)/(F_E-F_C))
RFIX=Matrix(((-1,0,0),(0,-1,0),(0,0,1)))
def bowrot(clip,f): return RFIX@Matrix.Rotation(math.radians(roll(clip,f)),3,'Z')
