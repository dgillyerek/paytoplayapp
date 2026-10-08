import sys,os,json,shutil,subprocess,hashlib,glob
from PIL import Image,ImageDraw
NAME=sys.argv[1]; cfg=json.load(open(f'cfg_{NAME}.json')); OUT=os.path.join(cfg['anim_dir'],'clothsplit_20261007')
if not OUT.startswith('/'): OUT=os.path.join('/workspace/design/survival-theme-a-fantasy',OUT)
FR=f'frames_{NAME}'; meta=json.load(open(f'{OUT}/work/build_meta.json')); qc=json.load(open(f'{OUT}/work/qc.json'))
for b in glob.glob(f'{OUT}/*.blend1'): os.remove(b)
os.makedirs(f'{OUT}/stills',exist_ok=True)
for f in sorted(glob.glob(f'{FR}/rest_*.png')): shutil.copy(f,f'{OUT}/stills/{NAME}_{os.path.basename(f)}')
# rest compare sheet (body only vs with cloth, 4 views)
views=['front','q34','side','rear']; ims=[[Image.open(f'{FR}/rest_{k}_{v}.png').convert('RGB') for v in views] for k in ['bodyonly','withcloth']]
w,h=ims[0][0].size; sh=Image.new('RGB',(w*4,h*2+40),(40,40,44)); d=ImageDraw.Draw(sh)
for r,row in enumerate(ims):
    for c,im in enumerate(row): sh.paste(im,(c*w,40+r*h))
d.text((10,10),f'{NAME} clothsplit rest: top = body only (cloth hidden), bottom = cloth shown | views {views}',fill='white')
sh.save(f'{OUT}/stills/{NAME}_clothsplit_rest_compare.jpg',quality=88)
# walk contact sheet rear + q34
cs=sorted(glob.glob(f'{FR}/c_rear_*.png')); n=len(cs); im0=Image.open(cs[0]); s=0.5; cw,ch=int(im0.width*s),int(im0.height*s)
sheet=Image.new('RGB',(cw*n,ch*2+40),(40,40,44)); d=ImageDraw.Draw(sheet)
for i,f in enumerate(cs):
    fr=f.split('_')[-1][:2]
    for r,v in enumerate(['rear','q34']):
        sheet.paste(Image.open(f'{FR}/c_{v}_{fr}.png').convert('RGB').resize((cw,ch)),(i*cw,40+r*ch))
    d.text((i*cw+6,44),f'f{int(fr)+1}',fill='white')
d.text((10,10),f'{NAME}_clothsplit_walk (cloth bones at rest; body-only bake) | top rear, bottom 3/4',fill='white')
sheet.save(f'{OUT}/{NAME}_clothsplit_walk_contact.jpg',quality=88)
mp4=f'{OUT}/{NAME}_clothsplit_walk_preview_rear.mp4'
subprocess.run(['ffmpeg','-y','-loglevel','error','-stream_loop','2','-framerate','30','-i',f'{FR}/m_%02d.png','-vf','scale=trunc(iw/2)*2:trunc(ih/2)*2','-c:v','libx264','-pix_fmt','yuv420p','-crf','20',mp4],check=True)
# scripts
for f in ['cs_lib.py','classify.py','mask.py','split_test.py','build.py','qc.py','post.py',f'cfg_{NAME}.json',f'legs_{NAME}.json']:
    if os.path.exists(f): shutil.copy(f,f'{OUT}/work/{f}')
# NOTE
def spring(nb,L):
    if L>0.75: return dict(stiffness=0.25,damping=0.35,drag=0.4,gravity=0.6,radius=0.06)
    if L>0.40: return dict(stiffness=0.35,damping=0.40,drag=0.45,gravity=0.4,radius=0.05)
    return dict(stiffness=0.5,damping=0.45,drag=0.5,gravity=0.25,radius=0.04)
import math
rows=[]
for c in meta['chains']:
    J=c['joints']; L=sum(math.dist(J[i],J[i+1]) for i in range(len(J)-1)); sp=spring(c['nbones'],L)
    rows.append(f"| {c['prefix']} | {c['sector']} | {', '.join(c['bones'])} | {c['parent']} | {L:.2f} | {c['nverts']} | {sp['stiffness']} | {sp['damping']} | {sp['drag']} | {sp['gravity']} | {sp['radius']} |")
q=qc; bw=meta['body_weights']
qtab="\n".join(f"| {k} | {v['bones']} | {v['body22_present']} / {v['body22_relative_order_same']} | {v['cloth_verts_with_leg_weight']} | {v['body_torso_verts_with_leg_weight']} | {v['body_unweighted']}/{v['cloth_unweighted']} | {v.get('frame_range','-')} | {v.get('cloth_bone_local_max_dev_m','-')} / {v.get('cloth_bone_local_max_dev_deg','-')} | {v.get('body_motion_vs_original_max_m','-')} / {v.get('body_motion_vs_original_max_deg','-')} |" for k,v in q.items())
extra=open(f'note_extra_{NAME}.md').read() if os.path.exists(f'note_extra_{NAME}.md') else ''
note=f"""# {NAME}: cloth split (2026-10-07)

## Overview

New sibling pack. The current packs (`blender_rig/`, `mixamo/`, `attack_20261007/`) were **not** modified; their MD5s were verified before and after.

| | |
|---|---|
| Rig source | `{meta['rig_blend']}` (22 mixamorig body bones, unchanged) |
| Attack source | `{meta['attack_blend']}` |

**Meshes**
- `{NAME}_body`: {q['rest']['body_faces']} faces. Cloth faces were removed and the openings closed with real triangles; boundary edges after fill: {meta['body_cut']['boundary_edges_after_fill']} (pre-existing non-manifold edges from the source remesh).
- `{NAME}_cloth`: {q['rest']['cloth_faces']} faces, the loose cloth below z = {meta['zcut']:.3f} m.
- Selection: geometric. The legs are found by horizontal section loops, and everything below the seam that is not inside a leg/foot/hand tube becomes cloth.
- Same material and UVs as the source.

**Body weights**
- Leg-bone weight was moved to Hips on {bw['verts_leg_weight_moved_to_Hips']} torso/non-leg verts (above the crotch, z {bw['crotch_z']:.3f}, plus the non-leg band at the seam).
- {bw['seam_verts_blended_to_Spine']} seam verts were blended toward Spine.
- Torso verts with leg weight after the fix: {bw['torso_verts_with_leg_weight_after']}.
- Leg tubes keep their original weights.

## Cloth chains

All chains are parented to `mixamorig:Spine` (the cloth starts at hip height). Only bone `_01` of each chain shares weight with Spine (a ring of the top 35%); the rest is chain-only, with at most 4 influences.

Sector letters: F = front (−Y), L = character-left (+X), B = back.

Suggested spring settings are a starting point for a Unity SpringBone / DynamicBone / MagicaCloth BoneSpring-style setup, all 0–1.

| chain | sector | bones | parent | length m | verts | stiffness | damping | drag | gravity | radius m |
|---|---|---|---|---|---|---|---|---|---|---|
{chr(10).join(rows)}

- **Colliders:** capsules on mixamorig:LeftUpLeg/LeftLeg/RightUpLeg/RightLeg (r ≈ 0.07 thigh, 0.055 shin) and a sphere on Hips (r ≈ 0.15).
- **Limits:** cap the angle at about 60° per joint for the front strips and about 75° for the back/cape strips.
- **Inertia:** lower the gravity on front strips if they clip the thighs during the walk; raise the root stiffness if the attack swing flares too much.

## Animation

- Walk and attack were baked with the **body bones only**; fcurves for non-body bones were removed.
- Cloth bones sit exactly at rest on every frame (keyed constant by the full-bone bake).
- Export settings match the existing packs:
  - rest: hum profile, no animation.
  - walk/attack: rebake profile (bake all bones, force start/end keys, step 1, simplify 0, axis −Z/Y, embedded textures).

## QC (reimport of each FBX)

| fbx | bones | body22 present / rel. order | cloth verts w/ leg weight | torso verts w/ leg weight | unweighted body/cloth | frames | cloth bone max dev m/deg | body motion vs original FBX m/deg |
|---|---|---|---|---|---|---|---|---|
{qtab}

- Bone order: the 22 body bones keep their relative order, but they are **not** the first 22 indices. Blender writes bones by hierarchy, so the cloth chains (children of Spine) are interleaved after Spine's subtree. Use name-based mapping (Unity Humanoid / Avatar does this).
- Cloth mesh vertex groups: Spine plus cloth bones only; no leg groups.

## Caveats
{extra}

## Files

- `{NAME}_clothsplit.blend`, `{NAME}_clothsplit.fbx` (rest), `{NAME}_clothsplit_walk.fbx`, `{NAME}_clothsplit_attack.fbx`
- `stills/`: rest stills with the cloth hidden and shown (front/q34/side/rear) plus `{NAME}_clothsplit_rest_compare.jpg`
- `{NAME}_clothsplit_walk_contact.jpg` (rear + 3/4, every 3rd frame)
- `{NAME}_clothsplit_walk_preview_rear.mp4`
- `work/`: build_meta.json, qc.json, scripts
"""
open(f'{OUT}/NOTE.md','w').write(note)
# checksums
lines=[]
for root,_,fs in os.walk(OUT):
    for f in sorted(fs):
        p=os.path.join(root,f); rel=os.path.relpath(p,OUT)
        if rel=='CHECKSUMS.md5' or '__pycache__' in rel: continue
        lines.append(f"{hashlib.md5(open(p,'rb').read()).hexdigest()}  {rel}")
open(f'{OUT}/CHECKSUMS.md5','w').write("\n".join(sorted(lines,key=lambda l:l[34:]))+"\n")
print(OUT); print("\n".join(l for l in lines if l.endswith(('.blend','.fbx','.mp4','.jpg'))))
