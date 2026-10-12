import bpy, sys, os, json, math
from mathutils import Vector, Matrix, Quaternion
args=sys.argv[sys.argv.index('--')+1:]; OUT=args[0]; RES=int(args[1]); SHOTS=args[2].split(';'); VIEWS=args[3].split(',')
sc=bpy.context.scene; arm=bpy.data.objects['VESPERA_rig']
AJ=json.load(open('/workspace/vespera-orb/work/anim.json'))['attack']
with bpy.data.libraries.load('/workspace/vespera-orb/work/out/VESPERA_orb_prop.blend') as (src,dst): dst.objects=['VESPERA_orb']
orb=dst.objects[0]; sc.collection.objects.link(orb)
bolt=orb.copy(); sc.collection.objects.link(bolt)
sc.render.engine='BLENDER_EEVEE'; sc.render.film_transparent=True; sc.view_settings.view_transform='Standard'
sc.render.resolution_x=RES; sc.render.resolution_y=int(RES*1.5)
try: sc.eevee.taa_render_samples=24
except: pass
w=sc.world or bpy.data.worlds.new('w'); sc.world=w; w.use_nodes=True; bg=w.node_tree.nodes['Background']; bg.inputs[0].default_value=(0.2,0.21,0.25,1); bg.inputs[1].default_value=0.55
def area(name,loc,tgt,energy,size,color=(1,1,1)):
    L=bpy.data.lights.new(name,'AREA'); L.energy=energy; L.size=size; L.color=color
    o=bpy.data.objects.new(name,L); sc.collection.objects.link(o); o.location=loc; o.rotation_euler=(Vector(tgt)-Vector(loc)).to_track_quat('-Z','Y').to_euler()
area('key',(-2.2,-2.6,2.8),(0,-0.2,1.1),260,2.0,(1,0.96,0.92)); area('fill',(2.4,-2.2,1.6),(0,-0.2,1.1),90,2.5,(0.85,0.88,1.0)); area('rim',(0.6,2.6,2.6),(0,-0.2,1.2),300,1.5,(0.8,0.7,1.0))
area('fillside',(-2.6,0.8,1.6),(0,-0.2,1.1),120,2.0)
cam=bpy.data.objects.new('cam',bpy.data.cameras.new('cam')); sc.collection.objects.link(cam); sc.camera=cam
V={'front':((-0.05,-4.2,1.2),(-0.05,-0.45,0.92)),'side':((-4.2,-0.75,1.2),(-0.05,-0.75,0.92))}
os.makedirs(OUT,exist_ok=True)
for shot in SHOTS:
    tag,f=shot.split(':'); f=int(f)
    arm.animation_data.action=bpy.data.actions['VESPERA_nocape_'+tag]; sc.frame_set(f)
    orb.hide_render=True; bolt.hide_render=True
    if tag=='attack':
        a=AJ[f]
        if 0<a['orb_scale'] and f<20:
            orb.hide_render=False; orb.location=a['orb']; s=a['orb_scale']; orb.scale=(s,s,s); orb.rotation_euler=(math.radians(a['orb_spin']),0,0)
        if 20<=f<30:
            bolt.hide_render=False; p=Vector(AJ[20]['orb'])+Vector((0,-0.3*(f-20),0)); bolt.location=p; bolt.scale=(1,1,1); bolt.rotation_euler=(0,0,0)
    for v in VIEWS:
        l,t=V[v]; l,t=Vector(l),Vector(t); cam.location=l; cam.rotation_euler=(t-l).to_track_quat('-Z','Y').to_euler(); cam.data.lens=50
        sc.render.filepath=f'{OUT}/{v}_{tag}_f{f:02d}.png'; bpy.ops.render.render(write_still=True)
print('SHEET DONE')
