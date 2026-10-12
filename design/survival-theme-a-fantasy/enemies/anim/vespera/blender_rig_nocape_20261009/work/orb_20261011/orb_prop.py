"""VESPERA_orb prop (Derek picked option C of the 2026-10-11 weapon sheet): dark-violet orb in a silver claw cradle with a
floating rune ring. Built from the sheet geometry, sized to a 10 cm orb (radius 5 cm), origin at the orb centre.
Ring lies across the hand-to-hand axis (Blender X) so it never cuts the palms. Materials are named so the Unity attack
driver turns on emission for the glowing parts (names containing 'crystal' / 'rune')."""
import bpy, bmesh, math, sys, json, os
from mathutils import Vector, Matrix
OUT=sys.argv[sys.argv.index('--')+1]; os.makedirs(OUT,exist_ok=True)
bpy.ops.wm.read_factory_settings(use_empty=True)
sc=bpy.context.scene
K=0.05/0.044
def mat(name,base,metal=0.0,rough=0.5,emit=None,es=0.0):
    m=bpy.data.materials.new(name); m.use_nodes=True; p=m.node_tree.nodes['Principled BSDF']
    p.inputs['Base Color'].default_value=(*base,1); p.inputs['Metallic'].default_value=metal; p.inputs['Roughness'].default_value=rough
    if emit: p.inputs['Emission Color'].default_value=(*emit,1); p.inputs['Emission Strength'].default_value=es
    m.diffuse_color=(*base,1); return m
M={'crystal':mat('VESPERA_orb_crystal',(0.30,0.07,0.62),0.0,0.08,(0.42,0.10,0.95),1.6),
   'rune':mat('VESPERA_orb_rune_glow',(0.62,0.28,1.0),0.0,0.3,(0.55,0.18,1.0),4.0),
   'silver':mat('VESPERA_orb_silver',(0.30,0.28,0.34),1.0,0.42)}
T=Matrix.Translation; R=lambda a,ax: Matrix.Rotation(math.radians(a),4,ax)
parts={k:bmesh.new() for k in M}
def add(m,fn,Tm=Matrix()):
    b=bmesh.new(); fn(b); bmesh.ops.transform(b,matrix=Matrix.Diagonal((K,K,K,1))@Tm,verts=b.verts)
    me=bpy.data.meshes.new('t'); b.to_mesh(me); parts[m].from_mesh(me); bpy.data.meshes.remove(me); b.free()
def sphere(r,u,v): return lambda b: bmesh.ops.create_uvsphere(b,u_segments=u,v_segments=v,radius=r)
def torus(Rr,r,seg=40,rs=8):
    def f(b):
        vs=[[b.verts.new(((Rr+r*math.cos(2*math.pi*j/rs))*math.cos(2*math.pi*i/seg),(Rr+r*math.cos(2*math.pi*j/rs))*math.sin(2*math.pi*i/seg),r*math.sin(2*math.pi*j/rs))) for j in range(rs)] for i in range(seg)]
        for i in range(seg):
            for j in range(rs): b.faces.new((vs[i][j],vs[(i+1)%seg][j],vs[(i+1)%seg][(j+1)%rs],vs[i][(j+1)%rs]))
    return f
def box(sx,sy,sz): return lambda b: bmesh.ops.create_cube(b,size=1.0,matrix=Matrix.Diagonal((sx,sy,sz,1)))
def tube(path,r,rs=6):
    def f(b):
        rings=[]
        for i,p in enumerate(path):
            p=Vector(p); t=(Vector(path[min(i+1,len(path)-1)])-Vector(path[max(i-1,0)])).normalized(); a=t.orthogonal().normalized(); c=t.cross(a)
            rr=r[i] if isinstance(r,(list,tuple)) else r
            rings.append([b.verts.new(p+rr*(math.cos(2*math.pi*j/rs)*a+math.sin(2*math.pi*j/rs)*c)) for j in range(rs)])
        for i in range(len(rings)-1):
            for j in range(rs): b.faces.new((rings[i][j],rings[i][(j+1)%rs],rings[i+1][(j+1)%rs],rings[i+1][j]))
    return f
add('crystal',sphere(0.044,28,18))
add('silver',torus(0.026,0.004,28,8),T((0,0,-0.040)))
for k in range(4):
    a=2*math.pi*k/4+math.pi/4; pts=[(0.026*math.cos(a),0.026*math.sin(a),-0.040)]
    for i in range(10):
        t=i/9; ang=math.radians(-65+105*t); r=0.047; pts.append((r*math.cos(ang)*math.cos(a),r*math.cos(ang)*math.sin(a),r*math.sin(ang)))
    add('silver',tube(pts,[0.004]*(len(pts)-3)+[0.003,0.0022,0.0012],6))
rr=R(90,'Y')@R(20,'X')        # ring plane across the hand axis (normal ~ X), tilted 20 deg
add('silver',torus(0.066,0.0028,48,8),rr)
for k in range(8):
    a=2*math.pi*k/8
    add('rune',box(0.009,0.004,0.012),rr@T((0.066*math.cos(a),0.066*math.sin(a),0))@R(math.degrees(a),'Z'))
objs=[]
for k,b in parts.items():
    me=bpy.data.meshes.new('VESPERA_orb_'+k); b.to_mesh(me); b.free()
    for p in me.polygons: p.use_smooth=True
    me.materials.append(M[k]); o=bpy.data.objects.new('VESPERA_orb_'+k,me); sc.collection.objects.link(o); objs.append(o)
# one object for the prop (Unity measures the whole instance anyway)
bpy.ops.object.select_all(action='DESELECT')
for o in objs: o.select_set(True)
bpy.context.view_layer.objects.active=objs[0]; bpy.ops.object.join(); orb=bpy.context.object; orb.name='VESPERA_orb'; orb.data.name='VESPERA_orb'
mn=Vector((min(v.co[i] for v in orb.data.vertices) for i in range(3))); mx=Vector((max(v.co[i] for v in orb.data.vertices) for i in range(3)))
ext=mx-mn
info={'blender_extent_m':[round(x,5) for x in ext],'unity_extent_m':[round(ext.x,5),round(ext.z,5),round(ext.y,5)],'faces':len(orb.data.polygons),
      'materials':[m.name for m in orb.data.materials],'orb_radius_m':0.05}
bpy.ops.export_scene.fbx(filepath=f'{OUT}/VESPERA_orb.fbx',use_selection=True,object_types={'MESH'},axis_forward='-Z',axis_up='Y',
    apply_unit_scale=True,apply_scale_options='FBX_SCALE_ALL',bake_anim=False,add_leaf_bones=False,mesh_smooth_type='FACE',path_mode='AUTO')
bpy.ops.wm.save_as_mainfile(filepath=f'{OUT}/VESPERA_orb_prop.blend',compress=True)
json.dump(info,open(f'{OUT}/orb_prop.json','w'),indent=1); print('ORB',json.dumps(info))
