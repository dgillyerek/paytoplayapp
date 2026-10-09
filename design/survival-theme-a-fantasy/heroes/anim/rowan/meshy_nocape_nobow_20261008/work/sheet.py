import sys,glob,os
from PIL import Image, ImageDraw
a=sys.argv; d,clip,views,frames,out=a[1],a[2],a[3].split(','),[int(x) for x in a[4].split(',')],a[5]
labels=dict(a[6].split('=') for _ in [0] for _ in [0]) if False else {}
im0=Image.open(f'{d}/{clip}_{views[0]}_{frames[0]:03d}.png'); w,h=im0.size
S=Image.new('RGB',(w*len(frames),h*len(views)+0),'white'); dr=ImageDraw.Draw(S)
for r,v in enumerate(views):
    for c,f in enumerate(frames):
        S.paste(Image.open(f'{d}/{clip}_{v}_{f:03d}.png').convert('RGB'),(c*w,r*h))
        lab=f'{v} f{f} ({f/24:.2f}s)'; dr.rectangle([c*w,r*h,c*w+6*len(lab)+6,r*h+14],fill=(0,0,0)); dr.text((c*w+3,r*h+2),lab,fill=(255,255,255))
S.save(out,quality=88)
