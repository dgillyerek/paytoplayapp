import sys; sys.path.insert(0,'/workspace/ironhowl/work')
from common import *
a=sys.argv[sys.argv.index('--')+1:]; out=a[0]; items=a[1:]  # path:f1,f2,...
arm,meshes=load_body()
UPL=UnityPlayer(arm,F(D+'/IRONHOWL_rig.fbx')) if os.environ.get('UP') else None
sfx=os.environ.get('SFX','')
cam=L.setup_scene('WORKBENCH',(330,380)); cam.data.clip_start=1; cam.data.clip_end=5000
sc=bpy.context.scene; sc.display.shading.show_shadows=True; sc.display.shading.color_type=os.environ.get('CT','SINGLE'); sc.display.shading.single_color=(0.72,0.66,0.6); sc.display.shading.show_cavity=True
bpy.ops.mesh.primitive_plane_add(size=600,location=(0,-60,0)); g=bpy.context.object
gm=bpy.data.materials.new('G'); gm.diffuse_color=(0.25,0.24,0.22,1); g.data.materials.append(gm)
for it in items:
    path,frames=it.split(':'); name=os.path.basename(path)[:-4]
    if UPL:
        fx=F(path); arm.animation_data.action=None
    else:
        act=load_clip_action(path); arm.animation_data_create(); arm.animation_data.action=act
    for fr in [int(x) for x in frames.split(',')]:
        if UPL: UPL.apply(fx,fr)
        else: sc.frame_set(fr+1)  # Blender imports the take from frame 1
        for view,eye,look in (('side',(-560,-60,110),(0,-60,75)),('front',(250,-540,130),(0,-50,75))):
            L.aim(cam,eye,look,62); L.render(f'{out}/{name}_f{fr:03d}_{view}{sfx}.png')
