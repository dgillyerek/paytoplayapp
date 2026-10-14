import bpy, bmesh, math, json
from mathutils import Vector, Matrix
W='/workspace/rowan_fresh/work'
def mat(name,rgb,rough=0.7):
    m=bpy.data.materials.new(name); m.use_nodes=True; b=m.node_tree.nodes['Principled BSDF']
    b.inputs['Base Color'].default_value=(*rgb,1); b.inputs['Roughness'].default_value=rough; m.diffuse_color=(*rgb,1); return m
def tube(bm,pts,r,seg=10,cap=True):
    rings=[]
    for i,p in enumerate(pts):
        p=Vector(p); t=(Vector(pts[min(i+1,len(pts)-1)])-Vector(pts[max(i-1,0)])).normalized()
        a=Vector((1,0,0)) if abs(t.x)<0.9 else Vector((0,1,0)); u=t.cross(a).normalized(); v=t.cross(u)
        rr=r(i/(len(pts)-1)) if callable(r) else r
        rings.append([bm.verts.new(p+(u*math.cos(2*math.pi*k/seg)+v*math.sin(2*math.pi*k/seg))*rr) for k in range(seg)])
    for A,B in zip(rings[:-1],rings[1:]):
        for k in range(seg): bm.faces.new((A[k],A[(k+1)%seg],B[(k+1)%seg],B[k]))
    if cap: bm.faces.new(rings[0][::-1]); bm.faces.new(rings[-1])
def obj(name,build,mats):
    me=bpy.data.meshes.new(name); bm=bmesh.new(); build(bm); bm.to_mesh(me); bm.free()
    o=bpy.data.objects.new(name,me); bpy.context.scene.collection.objects.link(o)
    for m in mats: me.materials.append(m)
    return o
def export(o,fn):
    for x in bpy.context.view_layer.objects: x.select_set(False)
    o.select_set(True); bpy.context.view_layer.objects.active=o
    bpy.ops.export_scene.fbx(filepath=f'{W}/{fn}',use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',apply_scale_options='FBX_SCALE_ALL',
        apply_unit_scale=True,add_leaf_bones=False,bake_anim=False,mesh_smooth_type='FACE')
bpy.ops.wm.read_factory_settings(use_empty=True)
wood=mat('ROWAN_bow_wood',(0.22,0.12,0.06),0.6); grip=mat('ROWAN_bow_grip_leather',(0.08,0.05,0.035)); string=mat('ROWAN_bowstring',(0.75,0.70,0.60))
leather=mat('ROWAN_quiver_leather',(0.26,0.15,0.08),0.8); dark=mat('ROWAN_quiver_band',(0.10,0.06,0.04)); shaft=mat('ROWAN_arrow_shaft',(0.45,0.32,0.18)); fl=mat('ROWAN_arrow_fletch',(0.16,0.20,0.12)); tip=mat('ROWAN_arrow_tip',(0.35,0.35,0.37),0.4)
# ---- bow: grip at origin, upright along Z, limbs bow toward -Y (target), tips recurve, string on +Y side
H=0.66
def bowpts(n=41):
    P=[]
    for i in range(n):
        s=-1+2*i/(n-1); z=s*H; y=-0.10*(1-s*s)+0.03*max(0,abs(s)-0.85)/0.15
        P.append((0,y+0.10*(1-0) - 0.10, z))
    return P
def bbow(bm):
    pts=bowpts(); tube(bm,pts,lambda t:0.012+0.008*(1-abs(2*t-1)) ,8)
    tube(bm,[(0,-0.10,-0.07),(0,-0.10,0.07)],0.024,8)   # leather grip wrap
    tube(bm,[pts[0],pts[-1]],0.0018,4)                  # string
def setmat(o,rules):
    for f in o.data.polygons:
        c=sum((o.data.vertices[v].co for v in f.vertices),Vector())/len(f.vertices); f.material_index=rules(c)
bow=obj('ROWAN_bow',bbow,[wood,grip,string])
setmat(bow,lambda c: 2 if c.y>-0.005 and abs(c.z)<0.6 else (1 if abs(c.z)<0.075 and c.y<-0.06 else 0))
# shift so the grip (y=-0.10) sits at the origin
bow.data.transform(Matrix.Translation((0,0.10,0)))
export(bow,'ROWAN_bow.fbx')
# ---- arrow: nock at origin, tip toward -Y, 0.75 m
def barrow(bm,L=0.75):
    tube(bm,[(0,-0.0,0),(0,-L,0)],0.0045,6)
    tube(bm,[(0,-L,0),(0,-L-0.045,0)],lambda t:0.011*(1-t)+0.0005,6)
    for k in range(3):
        a=2*math.pi*k/3; d=Vector((math.cos(a),0,math.sin(a)))
        v=[bm.verts.new(Vector((0,-0.02,0))+d*0.004),bm.verts.new(Vector((0,-0.13,0))+d*0.004),bm.verts.new(Vector((0,-0.11,0))+d*0.022),bm.verts.new(Vector((0,-0.03,0))+d*0.022)]
        bm.faces.new(v); bm.faces.new([bm.verts.new(x.co) for x in v[::-1]])
arrow=obj('ROWAN_arrow',barrow,[shaft,fl,tip])
def arrule(c):
    if c.y<-0.75: return 2
    if c.y>-0.14 and (c.x*c.x+c.z*c.z)**0.5>0.006: return 1
    return 0
setmat(arrow,arrule); export(arrow,'ROWAN_arrow.fbx')
# ---- quiver: centred at origin, long axis +Z (opening up), 0.62 m, r 0.05, 7 arrows out of the top
def bq(bm):
    L=0.62; tube(bm,[(0,0,-L/2+0.0),(0,0,L/2)],lambda t:0.044+0.008*t,14,cap=True)
    for zz in (-0.27,0.0,0.27): tube(bm,[(0,0,zz-0.012),(0,0,zz+0.012)],lambda t:0.0485+0.008*(zz/0.62+0.5),14)
    import random; random.seed(4)
    for k in range(7):
        a=2*math.pi*k/7; rr=0.028 if k else 0; o=Vector((math.cos(a)*rr,math.sin(a)*rr,0))
        top=L/2+0.10+random.uniform(-0.015,0.02); tube(bm,[o+Vector((0,0,L/2-0.2)),o+Vector((0,0,top))],0.0045,6)
        for j in range(3):
            b=a+2*math.pi*j/3+0.3; d=Vector((math.cos(b),math.sin(b),0))
            v=[bm.verts.new(o+Vector((0,0,top-0.01))+d*0.004),bm.verts.new(o+Vector((0,0,top-0.12))+d*0.004),bm.verts.new(o+Vector((0,0,top-0.10))+d*0.02),bm.verts.new(o+Vector((0,0,top-0.02))+d*0.02)]
            bm.faces.new(v); bm.faces.new([bm.verts.new(x.co) for x in v[::-1]])
q=obj('ROWAN_quiver',bq,[leather,dark,shaft,fl])
def qrule(c):
    r=(c.x*c.x+c.y*c.y)**0.5
    if c.z>0.31+0.005 or (r<0.04 and c.z>0.1):
        return 3 if c.z>0.31+0.0 and r>0.0055 and c.z>0.31 and any(True for _ in [0]) and r>0.006 else 2
    if r>0.0475 and min(abs(c.z-zz) for zz in (-0.27,0,0.27))<0.013: return 1
    return 0
setmat(q,qrule); export(q,'ROWAN_quiver.fbx')
rep={}
for o in (bow,arrow,q):
    import numpy as np
    P=np.array([v.co[:] for v in o.data.vertices]); rep[o.name]={'faces':len(o.data.polygons),'min':P.min(0).round(4).tolist(),'max':P.max(0).round(4).tolist()}
bpy.ops.wm.save_as_mainfile(filepath=f'{W}/props.blend'); json.dump(rep,open(f'{W}/props.json','w'),indent=1); print('PROPS',rep)
