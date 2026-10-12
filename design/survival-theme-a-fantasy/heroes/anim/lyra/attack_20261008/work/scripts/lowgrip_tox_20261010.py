# convert total-arm X (31x19) into apply-ready X (31x25): arm remainder + clavicle columns
import sys,numpy as np
exec(open('opt2.py').read())
X=np.load(sys.argv[1]); Y2=np.zeros((31,25)); Y2[:,:19]=X
for f in range(31):
    for s,o,c in (('Right',0,19),('Left',9,22)):
        cs,ar=clav_split(f,s,X[f,o:o+3]); Y2[f,c:c+3]=cs; Y2[f,o:o+3]=ar
np.save(sys.argv[2],Y2); print('tox ok')
