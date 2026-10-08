import json, os, subprocess, hashlib, shutil, glob
from PIL import Image, ImageDraw, ImageFont
OUT='/workspace/design/survival-theme-a-fantasy/heroes/anim/rowan/walkfix_male_20261007'
FR='/workspace/attack_20261007/walkfix/frames'
def font(s):
    for p in ['/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf','/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf']:
        if os.path.exists(p): return ImageFont.truetype(p,s)
    return ImageFont.load_default()
cw,ch=300,400; cols=list(range(0,30,3)); top=46; lab=22
S=Image.new('RGB',(cw*len(cols), top+2*(ch+lab)),(14,15,19)); d=ImageDraw.Draw(S)
d.text((10,10),'ROWAN_male_blenderig_walk.fbx (walkfix 2026-10-07): rebaked from identity rest on ROWAN_male_blenderig.blend 9255d17c; 31 keys @30fps, frame 30 = frame 0 (loop)',fill=(235,225,190),font=font(17))
for r,v in enumerate(('rear','q34')):
    y=top+r*(ch+lab)
    d.text((8,y+2),{'rear':'REAR gameplay cam (forward = top of screen)','q34':'3/4 FRONT'}[v],fill=(150,200,255),font=font(15))
    for i,f in enumerate(cols):
        im=Image.open(f'{FR}/c_{v}_{f:02d}.png').convert('RGB').resize((cw,ch),Image.LANCZOS)
        S.paste(im,(i*cw,y+lab)); d.text((i*cw+6,y+lab+4),f'f{f:02d}',fill=(255,230,120),font=font(15))
S.save(f'{OUT}/ROWAN_male_walk_contact.jpg',quality=90)
subprocess.run(['ffmpeg','-y','-loglevel','error','-stream_loop','2','-framerate','30','-i',f'{FR}/m_%02d.png','-pix_fmt','yuv420p','-c:v','libx264','-crf','20',f'{OUT}/ROWAN_male_walk_preview_rear.mp4'],check=True)
