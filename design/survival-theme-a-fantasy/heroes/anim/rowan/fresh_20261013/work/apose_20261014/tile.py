import sys; from PIL import Image
out=sys.argv[1]; ims=[Image.open(p) for p in sys.argv[2:]]; W=Image.new('RGB',(sum(i.width for i in ims),max(i.height for i in ims)),'white'); x=0
for im in ims: W.paste(im,(x,0)); x+=im.width
W.save(out)
