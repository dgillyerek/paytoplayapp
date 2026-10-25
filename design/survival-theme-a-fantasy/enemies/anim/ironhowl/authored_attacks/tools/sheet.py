import os,re,sys
from PIL import Image, ImageDraw
d,clip,outp=sys.argv[1:4]; contact=int(sys.argv[4]) if len(sys.argv)>4 else -1; title=sys.argv[5] if len(sys.argv)>5 else clip
fs=sorted(f for f in os.listdir(d) if f.startswith(clip+'_f'))
frames=sorted(set(int(re.search(r'_f(\d+)_',f).group(1)) for f in fs))
H=300; W=int(H*330/380); top=28
im=Image.new('RGB',(W*len(frames),H*2+top),(18,18,22)); dr=ImageDraw.Draw(im)
dr.text((6,8),title,fill=(255,255,255))
for c,f in enumerate(frames):
    for r,v in enumerate(['side','front']):
        im.paste(Image.open(f'{d}/{clip}_f{f:03d}_{v}.png').convert('RGB').resize((W,H)),(c*W,top+r*H))
        lab=f'f{f}'+('  IMPACT' if f==contact else '')
        dr.text((c*W+5,top+r*H+5),lab,fill=(255,80,80) if f==contact else (255,230,0))
im.save(outp); print(outp)
