import bpy, sys, os, json
from mathutils import Vector
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]; SHOTS=args[1].split(';'); VIEWS=args[2].split(','); RES=int(args[3]) if len(args)>3 else 300
sc=bpy.context.scene; arm=bpy.data.objects['VESPERA_rig']
sc.render.engine='BLENDER_WORKBENCH'; sc.display.shading.light='STUDIO'; sc.display.shading.color_type='TEXTURE'
sc.render.resolution_x=RES; sc.render.resolution_y=int(RES*1.5)
w=sc.world or bpy.data.worlds.new('w'); sc.world=w
sc.display.shading.background_type='VIEWPORT' if hasattr(sc.display.shading,'background_type') else None
bpy.ops.mesh.primitive_plane_add(size=8,location=(0,0,0))
rep=json.load(open(os.path.join(os.path.dirname(OUT.rstrip('/')),'anim.json'))) if os.path.exists(os.path.join(os.path.dirname(OUT.rstrip('/')),'anim.json')) else None
orb=None
if 'VESPERA_orb_preview' not in bpy.data.objects:
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.05,location=(0,0,-5)); orb=bpy.context.object; orb.name='VESPERA_orb_preview'
    m=bpy.data.materials.new('orbp'); m.diffuse_color=(0.6,0.2,1,1); orb.data.materials.append(m); orb.color=(0.7,0.2,1,1)
sc.display.shading.color_type='TEXTURE'
cam=bpy.data.objects.new('c',bpy.data.cameras.new('c')); sc.collection.objects.link(cam); sc.camera=cam
V={'front':((0,-3.6,1.25),(-0.05,-0.25,0.95)),'side':((-3.6,-0.3,1.25),(-0.05,-0.25,0.95)),'q34':((-2.5,-2.7,1.4),(-0.05,-0.3,0.95)),
   'top':((-0.05,-0.6,3.6),(-0.05,-0.45,1.1)),'cu':((-1.0,-1.6,1.45),(-0.06,-0.40,1.18)),'cuf':((-0.05,-2.0,1.3),(-0.05,-0.40,1.18)),'cus':((-1.9,-0.45,1.3),(-0.05,-0.40,1.18))}
os.makedirs(OUT,exist_ok=True)
for sh in SHOTS:
    tag,f=sh.split(':'); f=int(f)
    arm.animation_data.action=bpy.data.actions['VESPERA_nocape_'+tag]; sc.frame_set(f)
    if orb:
        if tag=='attack' and rep and rep['attack'][f]['orb_scale']>0 and f<20:
            orb.location=rep['attack'][f]['orb']; s=rep['attack'][f]['orb_scale']; orb.scale=(s,s,s)
        else: orb.location=(0,0,-5)
    for v in VIEWS:
        l,t=V[v]; l,t=Vector(l),Vector(t); cam.location=l; cam.rotation_euler=(t-l).to_track_quat('-Z','Y').to_euler(); cam.data.lens=50 if v!='cu' else 50
        sc.render.filepath=f'{OUT}/{v}_{tag}_f{f:02d}.png'; bpy.ops.render.render(write_still=True)
