"""Re-encode a parse_fbx tree (binary FBX) with Blender's encode_bin."""
import sys, array
sys.path.insert(0,"/workspace/ironhowl/work")
from fbxlib import encode_bin as E
import numpy as np
def to_elem(el):
    e=E.FBXElem(el.id)
    for t,p in zip(el.props_type, el.props):
        c=chr(t)
        if c=='C': e.add_char(p if isinstance(p,bytes) else bytes([int(p)]))
        elif c=='B': e.add_bool(bool(p))
        elif c=='Y': e.add_int16(p)
        elif c=='I': e.add_int32(p)
        elif c=='L': e.add_int64(p)
        elif c=='F': e.add_float32(p)
        elif c=='D': e.add_float64(p)
        elif c=='S': e.add_string(p)
        elif c=='R': e.add_bytes(p)
        elif c=='f': e.add_float32_array(np.asarray(p,dtype=np.float32))
        elif c=='d': e.add_float64_array(np.asarray(p,dtype=np.float64))
        elif c=='i': e.add_int32_array(np.asarray(p,dtype=np.int32))
        elif c=='l': e.add_int64_array(np.asarray(p,dtype=np.int64))
        elif c=='b': e.add_bool_array(np.asarray(p,dtype=bool))
        else: raise ValueError(c)
    for ch in el.elems: e.elems.append(to_elem(ch))
    return e
def write(root, version, path):
    r=E.FBXElem(b'')
    for ch in root.elems: r.elems.append(to_elem(ch))
    E.write(path, r, version)
