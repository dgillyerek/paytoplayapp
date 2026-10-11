"""Regenerate LyraAttack.cs staff/bolt frames + MD5 from apply2 samples json. usage: gen_cs.py samples.json cs_path new_md5"""
import sys,json,re,numpy as np
from scipy.spatial.transform import Rotation as Ro
S=json.load(open(sys.argv[1])); CS=sys.argv[2]; MD5=sys.argv[3]
C=np.array([[-1,0,0],[0,0,1],[0,-1,0.]])
def g(x):
    s=f"{round(float(x),5):.5f}".rstrip('0').rstrip('.')
    if s in ('-0',''): s='0'
    return s+'f'
src=open(CS).read()
lines=src.split('\n')
idx=[i for i,l in enumerate(lines) if 'new AttackFrame(' in l]
assert len(idx)==62, len(idx)
staff_i=idx[:31]; bolt_i=idx[31:]
for f,i in enumerate(staff_i):
    M=np.array(S['staff'][f]); p=C@M[:3,3]; R=C@M[:3,:3]@C.T; R=R/np.linalg.norm(R,axis=0)
    q=Ro.from_matrix(R).as_quat()
    if q[3]<0: q=-q
    old=lines[i]; tail=old.split('(',1)[1].split(')')[0].split(', ')[7:]
    lines[i]=old.split('new AttackFrame(')[0]+'new AttackFrame('+', '.join([g(v) for v in (*p,*q)]+tail)+')'+(',' if old.rstrip().endswith(',') else '')
for f,i in enumerate(bolt_i):
    p=C@np.array(S['spawn'])+np.array([0,0,9.0/30*max(0,f-12)]); old=lines[i]; parts=old.split('(',1)[1].split(')')[0].split(', ')
    lines[i]=old.split('new AttackFrame(')[0]+'new AttackFrame('+', '.join([g(v) for v in p]+parts[3:])+')'+(',' if old.rstrip().endswith(',') else '')
out='\n'.join(lines)
out=re.sub(r'FileMd5 = "[0-9a-f]{32}"',f'FileMd5 = "{MD5}"',out)
open(CS,'w').write(out); print('cs ok')
