"""Minimal FBX binary (7.x) reader/writer that keeps every byte it does not touch.
Nodes keep their raw property bytes; only properties replaced via set_prop are re-encoded."""
import struct, zlib
HEAD = b'Kaydara FBX Binary  \x00\x1a\x00'
class Node:
    __slots__=('name','props','children','raw_props','nprops','dirty','_had_sentinel')
    def __init__(s,name): s.name=name; s.props=None; s.children=[]; s.raw_props=b''; s.nprops=0; s.dirty=False; s._had_sentinel=False
    def find(s,name): return [c for c in s.children if c.name==name]
def _read_props(buf, n):
    out=[]; o=0
    for _ in range(n):
        t=buf[o:o+1]; o+=1
        if t in b'YCILFD':
            sz={b'Y':2,b'C':1,b'I':4,b'L':8,b'F':4,b'D':8}[t]; fmt={b'Y':'<h',b'C':'<?',b'I':'<i',b'L':'<q',b'F':'<f',b'D':'<d'}[t]
            out.append((t,struct.unpack_from(fmt,buf,o)[0])); o+=sz
        elif t in b'SR':
            l=struct.unpack_from('<I',buf,o)[0]; o+=4; out.append((t,bytes(buf[o:o+l]))); o+=l
        elif t in b'fdlibc':
            alen,enc,clen=struct.unpack_from('<III',buf,o); o+=12
            data=bytes(buf[o:o+clen]); o+=clen
            if enc==1: data=zlib.decompress(data)
            fmt={b'f':'f',b'd':'d',b'l':'q',b'i':'i',b'b':'?',b'c':'B'}[t]
            out.append((t,list(struct.unpack('<%d%s'%(alen,fmt),data)),enc))
        else: raise ValueError('prop type %r'%t)
    return out
def _enc_props(props):
    b=bytearray()
    for p in props:
        t=p[0]; b+=t
        if t in b'YCILFD':
            fmt={b'Y':'<h',b'C':'<?',b'I':'<i',b'L':'<q',b'F':'<f',b'D':'<d'}[t]; b+=struct.pack(fmt,p[1])
        elif t in b'SR': b+=struct.pack('<I',len(p[1]))+p[1]
        else:
            fmt={b'f':'f',b'd':'d',b'l':'q',b'i':'i',b'b':'?',b'c':'B'}[t]
            data=struct.pack('<%d%s'%(len(p[1]),fmt),*p[1]); enc=p[2]
            if enc==1: data=zlib.compress(data,9)
            b+=struct.pack('<III',len(p[1]),enc,len(data))+data
    return bytes(b)
def read(path):
    d=open(path,'rb').read(); assert d[:23]==HEAD
    ver=struct.unpack_from('<I',d,23)[0]; big=ver>=7500
    hdr='<QQQB' if big else '<IIIB'; hs=struct.calcsize(hdr)
    def node(o):
        end,np_,plen,nlen=struct.unpack_from(hdr,d,o)
        if end==0: return None,o+hs
        n=Node(bytes(d[o+hs:o+hs+nlen])); p=o+hs+nlen
        n.raw_props=d[p:p+plen]; n.nprops=np_; p+=plen
        n._had_sentinel = p<end
        while p<end:
            c,p=node(p)
            if c is None: break
            n.children.append(c)
        return n,end
    root=Node(b''); o=27
    while True:
        n,o2=node(o)
        if n is None: o=o2; break
        root.children.append(n); o=o2
    return dict(root=root,ver=ver,tail=d[o:],path=path)
def props(n):
    if n.props is None: n.props=_read_props(n.raw_props,n.nprops)
    return n.props
def set_prop(n,i,value):
    p=props(n); old=p[i]; p[i]=(old[0],value)+tuple(old[2:]); n.dirty=True
def write(f,path):
    ver=f['ver']; big=ver>=7500; hdr='<QQQB' if big else '<IIIB'; hs=struct.calcsize(hdr)
    out=bytearray(HEAD+struct.pack('<I',ver))
    sentinel=b'\0'*hs
    def w(n):
        start=len(out); pr=_enc_props(n.props) if n.dirty else bytes(n.raw_props)
        out.extend(b'\0'*hs); out.extend(n.name); out.extend(pr)
        for c in n.children: w(c)
        if n._had_sentinel: out.extend(sentinel)
        struct.pack_into(hdr,out,start,len(out),len(props(n)) if n.dirty else n.nprops,len(pr),len(n.name))
    for c in f['root'].children: w(c)
    out.extend(sentinel)
    # footer: keep original footer id, recompute alignment padding, keep the rest
    tail=f['tail']; fid=tail[:16]; rest=tail[-(4+120+16):]
    out.extend(fid); out.extend(b'\0'*4)
    pad=((len(out)+15)&~15)-len(out)
    if pad==0: pad=16
    out.extend(b'\0'*pad); out.extend(rest)
    open(path,'wb').write(bytes(out))
