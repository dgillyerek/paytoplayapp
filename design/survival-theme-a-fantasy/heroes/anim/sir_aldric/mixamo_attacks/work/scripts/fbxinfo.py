import sys
sys.path.append('/workspace/tools/blender-5.2.2-linux-x64/5.2/scripts/addons_core')
import bpy
from io_scene_fbx import parse_fbx
def props(el):
    out={}
    for c in el.elems:
        if c.id==b'P':
            out[c.props[0].decode(errors='ignore')]=c.props[4:]
    return out
for fp in sys.argv[sys.argv.index('--')+1:]:
    root,ver=parse_fbx.parse(fp)
    print('FILE',fp.split('/')[-1],'ver',ver)
    for e in root.elems:
        if e.id==b'GlobalSettings':
            for c in e.elems:
                if c.id==b'Properties70':
                    p=props(c); print('  GS',{k:p[k] for k in ('UpAxis','UpAxisSign','FrontAxis','FrontAxisSign','CoordAxis','UnitScaleFactor','OriginalUnitScaleFactor') if k in p})
        if e.id==b'Objects':
            for c in e.elems:
                if c.id==b'Model':
                    nm=c.props[1].split(b'\x00')[0].decode(errors='ignore')
                    if nm in ('Armature','mixamorig:Hips','Mesh_0','mixamorig:LeftFoot'):
                        pp={}
                        for d in c.elems:
                            if d.id==b'Properties70': pp=props(d)
                        print('  M',nm,c.props[2],{k:[round(float(x),4) for x in pp[k]] for k in ('Lcl Translation','Lcl Rotation','Lcl Scaling','PreRotation') if k in pp})
                if c.id==b'AnimationStack': print('  STACK',c.props[1].split(b'\x00')[0])
