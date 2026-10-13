import glob,sys
from PIL import Image,ImageDraw
W='/workspace/oakenshield_fresh/work'; O='/workspace/oakenshield_fresh'
for k in ('rest','walk','attack'):
    fs=sorted(glob.glob(f'{W}/sh_{k}_*_full.png'))
    n=len(fs); cw=360; side=k=='attack'; S=Image.new('RGB',(cw*n,640+360+30+(640 if side else 0)),'white'); d=ImageDraw.Draw(S)
    for i,f in enumerate(fs):
        S.paste(Image.open(f).convert('RGB'),(i*cw,30)); S.paste(Image.open(f.replace('_full','_face')).convert('RGB'),(i*cw,670))
        if side: S.paste(Image.open(f.replace('_full','_side')).convert('RGB'),(i*cw,1030))
        d.text((i*cw+8,8),f"{k} f{f.split('_')[-2]}",fill='black')
    S.save(f'{O}/OAKENSHIELD_fresh_{k}_sheet.jpg',quality=88)
