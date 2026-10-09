import sys
from PIL import Image, ImageDraw, ImageFont
F=lambda s: ImageFont.truetype('/usr/share/fonts/truetype/dejavu/DejaVuSans-Bold.ttf',s)
R=sys.argv[1]; OUT=sys.argv[2]
cols=[('rest f1','rest','front',1),('walk f7','walk','front',7),('walk f13 3/4','walk','q34',13),('attack f26','attack','front',26),('attack f69 full draw','attack','front',69),('attack f69 3/4','attack','q34',69),('attack f82','attack','front',82),('attack f94 3/4','attack','q34',94)]
rearcols=[('rest f1 rear','rest','rear',1),('walk f7 rear','walk','rear',7),('walk f13 rear','walk','rear',13),('attack f26 rear','attack','rear',26),('attack f40 rear','attack','rear',40),('attack f69 rear','attack','rear',69),('attack f82 rear','attack','rear',82),('attack f94 rear','attack','rear',94)]
box=(20,40,520,960); w,h=250,460
rows=[("BEFORE  Design no-bow pack as committed (88b4aa9): Meshy skin","before",cols),("AFTER  skin cleanup on the same mesh / paint / motion","after",cols),("BEFORE  rear battle camera","before",rearcols),("AFTER  rear battle camera","after",rearcols)]
W=w*len(cols); H=90+len(rows)*(h+70)
S=Image.new('RGB',(W,H),(18,18,20)); d=ImageDraw.Draw(S)
d.text((16,18),'Rowan Meshy compare (no-bow pack): skin fix, Blender 4.3 renders of the FBXs Unity imports',fill=(255,255,255),font=F(30))
y=90
for title,kind,cs in rows:
    d.text((16,y+8),title,fill=(255,214,120) if kind=='after' else (200,200,200),font=F(26)); y+=50
    for i,(lab,clip,view,f) in enumerate(cs):
        im=Image.open(f'{R}/{kind}/{kind}_{clip}_{view}_f{f:03d}.png').crop(box).resize((w,h)); S.paste(im,(i*w,y))
        d.text((i*w+8,y+6),lab,fill=(255,255,255),font=F(17))
    y+=h+20
S.save(OUT); print(S.size)
