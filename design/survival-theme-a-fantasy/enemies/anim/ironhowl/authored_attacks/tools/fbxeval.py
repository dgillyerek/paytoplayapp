"""Evaluate per-bone local matrices (as Unity's FBX SDK would: T * Pre * R(XYZ) * Post^-1, S ignored) from an FBX at given frames."""
import sys, math
sys.path.insert(0,"/workspace/ironhowl/work")
from fbxlib.parse_fbx import parse
import numpy as np
KTIME=46186158000
def props(el):
    out={}
    for c in el.elems:
        if c.id==b'Properties70':
            for p in c.elems:
                out[p.props[0].decode()]=[x.decode() if isinstance(x,bytes) else x for x in p.props[1:]]
    return out
def child(el,id):
    for c in el.elems:
        if c.id==id: return c
def rx(a):c,s=math.cos(a),math.sin(a);return np.array([[1,0,0],[0,c,-s],[0,s,c]])
def ry(a):c,s=math.cos(a),math.sin(a);return np.array([[c,0,s],[0,1,0],[-s,0,c]])
def rz(a):c,s=math.cos(a),math.sin(a);return np.array([[c,-s,0],[s,c,0],[0,0,1]])
def eul(v):
    x,y,z=[math.radians(t) for t in v]; return rz(z)@ry(y)@rx(x)
class F:
    def __init__(s,path):
        root,_=parse(path)
        objs=child(root,b'Objects'); conns=child(root,b'Connections')
        s.gs=props(child(root,b'GlobalSettings'))
        s.models={};s.cn={};s.cv={}
        for o in objs.elems:
            uid=o.props[0]
            if o.id==b'Model': s.models[uid]=(o.props[1].split(b'\x00')[0].decode(),props(o))
            elif o.id==b'AnimationCurveNode': s.cn[uid]={'name':o.props[1].split(b'\x00')[0].decode(),'p':props(o),'c':{}}
            elif o.id==b'AnimationCurve':
                t=np.array(child(o,b'KeyTime').props[0],dtype=np.int64); v=np.array(child(o,b'KeyValueFloat').props[0],dtype=float)
                s.cv[uid]=(t,v)
        s.anim={} # model name -> {'T':node,'R':node}
        s.parent={}
        for c in conns.elems:
            typ=c.props[0].decode(); a,b=c.props[1],c.props[2]
            if typ=='OO' and a in s.models and b in s.models: s.parent[s.models[a][0]]=s.models[b][0]
            if typ=='OP' and a in s.cn and b in s.models:
                prop=c.props[3].decode(); s.anim.setdefault(s.models[b][0],{})[prop]=a
            if typ=='OP' and a in s.cv and b in s.cn:
                s.cn[b]['c'][c.props[3].decode()]=a
    def chan(s,name,prop,default,frame):
        node=s.anim.get(name,{}).get(prop)
        if node is None: return list(default)
        cn=s.cn[node]; out=[]
        for i,ax in enumerate('XYZ'):
            k='d|'+ax
            if k in cn['c']:
                t,v=s.cv[cn['c'][k]]; tt=frame*KTIME/30.0
                out.append(float(np.interp(tt,t,v)))
            else:
                d=cn['p'].get(k); out.append(d[-1] if d else default[i])
        return out
    def local(s,name,frame):
        pr=[p for p in s.models.values() if p[0]==name][0][1]
        T=self_get(pr,'Lcl Translation',[0,0,0]); R=self_get(pr,'Lcl Rotation',[0,0,0])
        T=s.chan(name,'Lcl Translation',T,frame); R=s.chan(name,'Lcl Rotation',R,frame)
        pre=eul(self_get(pr,'PreRotation',[0,0,0])); post=eul(self_get(pr,'PostRotation',[0,0,0]))
        M=np.eye(4); M[:3,:3]=pre@eul(R)@post.T; M[:3,3]=T
        return M
def self_get(pr,k,d):
    v=pr.get(k); return list(v[-3:]) if v else list(d)
if __name__=='__main__':
    a=F(sys.argv[1]); b=F(sys.argv[2]); frames=[int(x) for x in sys.argv[3].split(',')]
    names=[m[0] for m in a.models.values() if m[0].startswith('mixamorig:')]
    worst=0
    for f in frames:
        for n in names:
            A=a.local(n,f); B=b.local(n,f)
            dr=np.abs(A[:3,:3]-B[:3,:3]).max(); dt=np.abs(A[:3,3]-B[:3,3]).max()
            worst=max(worst,dr)
            if dr>1e-3 or dt>1e-2: print("DIFF f",f,n,"rot",round(dr,4),"pos",round(dt,4), A[:3,3].round(3), B[:3,3].round(3))
    print("parents b:",{k:v for k,v in b.parent.items() if 'Hips' in k}, "gsA",a.gs.get('UnitScaleFactor'),"gsB",b.gs.get('UnitScaleFactor'),"worst rot",worst)
