import json,numpy as np
c=0.015
J={'Hips':(c,0.0,0.96),'Spine':(c,0.0,1.08),'Spine1':(c,0.0,1.18),'Spine2':(c,0.0,1.29),'Neck':(c,0.0,1.47),'Head':(c,-0.01,1.535),'HeadTop':(c,-0.02,1.80)}
for s,x in (('Left',1),('Right',-1)):
    J[s+'Shoulder']=(c+0.04*x,0.0,1.44); J[s+'Arm']=(c+0.185*x,0.0,1.42); J[s+'ForeArm']=(c+0.255*x,0.02,1.14)
    J[s+'Hand']=(c+0.27*x,-0.03,0.93); J[s+'HandEnd']=(c+0.275*x,-0.06,0.78)
    J[s+'UpLeg']=(c+0.10*x,0.0,0.89); J[s+'Leg']=(c+0.14*x,0.0,0.50); J[s+'Foot']=(c+0.18*x,0.05,0.10); J[s+'FootEnd']=(c+0.19*x,-0.17,0.02)
json.dump(J,open('joints_in.json','w'))
from PIL import Image,ImageDraw
for v,fn,ax,sg in (('front','raw_front.png',0,1),('side','raw_side.png',1,-1)):
    im=Image.open(fn).convert('RGB'); d=ImageDraw.Draw(im)
    for k,p in J.items():
        X=300+sg*p[ax]*461.5 if v=='front' else 300-p[1]*461.5*(-1); Z=450-(p[2]-0.9)*461.5
        if v=='side': X=300+p[1]*461.5*-1*-1
        d.ellipse((X-4,Z-4,X+4,Z+4),fill=(255,0,0) if 'Left' in k else (0,255,0) if 'Right' in k else (255,255,0))
    im.save(f'jt_{v}.png')
