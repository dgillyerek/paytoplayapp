import os, sys, json, subprocess, shutil
from PIL import Image, ImageDraw, ImageFont
TAG=sys.argv[1]
B='/workspace/design/survival-theme-a-fantasy/heroes/anim'
C={'rowan':dict(NAME='ROWAN_nocape',RIG=f'{B}/rowan/blender_rig_nocape_20261008',STAGE=f'{B}/rowan/attack_20261008',SOT=f'{B}/rowan/LOOK_SOT/nocape_20261007/03_human_ranger_rowan_FULLBODY_nocape.png',LABEL='ROWAN no-cape'),
   'lyra':dict(NAME='LYRA_tighten',RIG=f'{B}/lyra/blender_rig_tighten_20261008',STAGE=f'{B}/lyra/attack_20261008',SOT=f'{B}/lyra/LOOK_SOT/tighten_20261007/02_human_mage_lyra_FULLBODY_tighten.png',LABEL='LYRA fitted')}[TAG]
N=C['NAME']; QF=f"{C['RIG']}/work/qc_frames"; AF=f"{C['STAGE']}/work/frames"
def font(sz):
    for p in ['/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf','/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf']:
        if os.path.exists(p): return ImageFont.truetype(p,sz)
    return ImageFont.load_default()
F=font(22); FS=font(16); FT=font(28)
BG=(24,25,29)
# 1 compare vs look SoT
H=960; sot=Image.open(C['SOT']).convert('RGB'); sot=sot.resize((int(sot.width*H/sot.height),H),Image.LANCZOS)
views=['front','side_left','back','side_right']; ims=[Image.open(f'{QF}/rest_{v}.png').convert('RGB') for v in views]
W=sot.width+sum(i.width for i in ims); S=Image.new('RGB',(W,H+90),BG); d=ImageDraw.Draw(S)
d.text((12,10),f"{C['LABEL']}  |  look SoT (left, unmodified) vs Blender rig rest pose + held prop (EEVEE, ortho)  |  {N}_blenderig.blend",font=F,fill=(235,235,235))
x=0; S.paste(sot,(0,60)); d.text((10,H+62),'LOOK SoT (PASSed 10-07)',font=FS,fill=(255,200,90)); x=sot.width
for v,i in zip(views,ims): S.paste(i,(x,60)); d.text((x+10,H+62),f'rig rest: {v}',font=FS,fill=(150,200,255)); x+=i.width
p1=f"{C['RIG']}/{N}_rest_compare_lookSoT.png"; S.save(p1)
# 2 walk sheet
T=230; fr=list(range(0,30,3)); rows=['rear','q34front','side']
S=Image.new('RGB',(90+T*len(fr),50+T*len(rows)),BG); d=ImageDraw.Draw(S)
d.text((10,10),f"{N} walk ({N}_blenderig_walk.fbx, 10-05 identity-rest method, f0-f30 loop @30fps) every 3rd frame, held prop on RightHand",font=FS,fill=(235,235,235))
for r,v in enumerate(rows):
    d.text((6,50+r*T+T//2),v,font=FS,fill=(255,200,90))
    for c,f in enumerate(fr):
        S.paste(Image.open(f'{QF}/walk_{v}_{f:03d}.png').convert('RGB').resize((T,T),Image.LANCZOS),(90+c*T,50+r*T)); d.text((94+c*T,52+r*T),f'f{f}',font=FS,fill=(220,220,220))
p2=f"{C['RIG']}/{N}_walk_sheet.jpg"; S.save(p2,quality=90)
# 3 attack sheet
meta=json.load(open(f"{C['STAGE']}/work/attack_meta.json")); SF=meta['sheet_frames']; rows=['rear','side','q34front']
keys={}
for k,v in meta['keys'].items():
    if isinstance(v,int): keys[v]=k
S=Image.new('RGB',(90+T*len(SF),50+T*len(rows)),BG); d=ImageDraw.Draw(S)
d.text((10,10),f"{N} attack ({N}_blenderig_attack.fbx f0-f30 @30fps, release f{meta['release']}); rear = battle cam, projectile -> top of screen",font=FS,fill=(235,235,235))
for r,v in enumerate(rows):
    d.text((6,50+r*T+T//2),v,font=FS,fill=(255,200,90))
    for c,f in enumerate(SF):
        S.paste(Image.open(f'{AF}/s_{v}_f{f:02d}.png').convert('RGB').resize((T,T),Image.LANCZOS),(90+c*T,50+r*T))
        d.text((94+c*T,52+r*T),f'f{f} {keys.get(f,"")}',font=FS,fill=(220,220,220))
p3=f"{C['STAGE']}/{N}_attack_sheet.jpg"; S.save(p3,quality=90)
# 4 mp4s
tmp=f"/tmp/mp4_{TAG}"; shutil.rmtree(tmp,ignore_errors=True); os.makedirs(tmp)
seq=[('WALK',f) for _ in range(2) for f in range(30)]+[('ATTACK',f) for _ in range(2) for f in range(31)]
def panel(lab,f):
    if lab=='WALK': a=f'{QF}/walk_rear_{f:03d}.png'; b=f'{QF}/walk_q34front_{f:03d}.png'
    else: a=f'{AF}/m_rear_{f:03d}.png'; b=f'{AF}/m_q34front_{f:03d}.png'
    S=Image.new('RGB',(1080,540)); S.paste(Image.open(a).convert('RGB').resize((540,540)),(0,0)); S.paste(Image.open(b).convert('RGB').resize((540,540)),(540,0))
    d=ImageDraw.Draw(S); d.text((10,8),f'{N}  {lab}  f{f:02d}',font=F,fill=(255,255,255)); d.text((10,512),'rear battle cam (forward = up)',font=FS,fill=(200,200,200)); d.text((550,512),'3/4 front',font=FS,fill=(200,200,200))
    return S
for i,(lab,f) in enumerate(seq): panel(lab,f).save(f'{tmp}/{i:04d}.png')
p4=f"{C['STAGE']}/{N}_walk_attack_preview.mp4"
subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate','30','-i',f'{tmp}/%04d.png','-c:v','libx264','-pix_fmt','yuv420p','-crf','20',p4],check=True)
tmp2=tmp+'_w'; shutil.rmtree(tmp2,ignore_errors=True); os.makedirs(tmp2)
for i in range(90): panel('WALK',i%30).save(f'{tmp2}/{i:04d}.png')
p5=f"{C['RIG']}/{N}_walk_preview.mp4"
subprocess.run(['ffmpeg','-y','-loglevel','error','-framerate','30','-i',f'{tmp2}/%04d.png','-c:v','libx264','-pix_fmt','yuv420p','-crf','20',p5],check=True)
print('POST',p1,p2,p3,p4,p5)
