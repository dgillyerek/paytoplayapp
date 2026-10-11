from PIL import Image, ImageDraw, ImageFont
import os
cols=[('rest',0),('walk',0),('walk',8),('walk',15),('walk',23),('atk',6),('atk',10),('atk',14),('atk',20)]
views=[('front','front'),('q34','three-quarter'),('rearbattle','rear battle cam'),('legs','legs close-up')]
CW,CH=360,560; LW=230; TH=60
F=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',22); Fs=ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf',18)
W=Image.new('RGB',(LW+CW*len(cols),TH+CH*2*len(views)+70),(24,24,24)); d=ImageDraw.Draw(W)
d.text((10,8),'OAKENSHIELD   BEFORE = PR #48 4b7f231 (cloth split + spring bones)   AFTER = rigidleaves_20261010 (cloth removed, rigid leaves, clean weights, new walk + attack)',fill=(255,255,255),font=F)
for j,(a,f) in enumerate(cols):
    d.text((LW+CW*j+110,TH-26),{'rest':'rest','walk':'walk','atk':'attack'}[a]+f' f{f:02d}',fill=(255,220,120),font=F)
y=TH
for v,vn in views:
    for tag,dr in (('BEFORE 4b7f231','c_old'),('AFTER new','c_new')):
        d.text((10,y+CH//2-30),vn,fill=(200,200,255),font=F); d.text((10,y+CH//2+5),tag,fill=(255,140,140) if 'BEFORE' in tag else (140,255,140),font=F)
        for j,(a,f) in enumerate(cols):
            p=f'{dr}/{v}_{a}_f{f:02d}.png'
            if os.path.exists(p): W.paste(Image.open(p).convert('RGB'),(LW+CW*j,y))
        y+=CH
d.text((10,y+15),'Blender workbench renders of the source blends. AFTER attack frames show the thorn spear on RightHand as Unity binds it at f10 (hidden after release f14). BEFORE shows cloth at rest (it only moved via runtime springs).',fill=(220,220,220),font=Fs)
W.save('/workspace/oakenshield-fix/OAKENSHIELD_rigidleaves_contact.png')
sm='/workspace/oakenshield-fix/OAKENSHIELD_rigidleaves_contact_small.jpg'
for q in (85,75,65,55):
    W.resize((1600,W.height*1600//W.width),Image.LANCZOS).save(sm,quality=q)
    if os.path.getsize(sm)<1_000_000: break
W.resize((2400,W.height*2400//W.width),Image.LANCZOS).save('/workspace/oakenshield-fix/out/OAKENSHIELD_rigidleaves_contact.jpg',quality=82)
print(W.size,os.path.getsize(sm),os.path.getsize('/workspace/oakenshield-fix/out/OAKENSHIELD_rigidleaves_contact.jpg'))
