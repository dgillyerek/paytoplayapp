"""Mesh analysis for Mixamo-style humanoid joint placement."""
import numpy as np

def body_centroid_at(co, body, z, hz):
    m=body & (np.abs(co[:,2]-z)<hz)
    if m.sum()<10:
        m=np.abs(co[:,2]-z)<hz
    return co[m].mean(0) if m.sum() else np.array([0.,0.,z])

def analyze_humanoid(co):
    z0,z1=float(co[:,2].min()), float(co[:,2].max())
    H=z1-z0
    # body column: central |x| band using percentile of upper-torso width later refined
    halfx=np.percentile(np.abs(co[:,0]),92)
    body0=np.abs(co[:,0]) < max(0.07, halfx*0.45)

    # Face forward from head tip Y
    head_h = co[:,2] > z0+0.82*H
    face_m = head_h & (np.abs(co[:,0]) < 0.08)
    if face_m.sum()>40:
        face=co[face_m]
        face_forward = 1.0 if abs(face[:,1].max()) >= abs(face[:,1].min()) else -1.0
    else:
        face_forward = 1.0

    # --- Hips: max body-column width in 0.42H..0.56H ---
    def width_at(mask_base, z, hz):
        m=mask_base & (np.abs(co[:,2]-z)<hz)
        if m.sum()<15: return 0.0, None
        return float(co[m,0].max()-co[m,0].min()), co[m].mean(0)

    hip_cands=[]
    for z in np.linspace(z0+0.42*H, z0+0.56*H, 24):
        w,c=width_at(body0,z,0.018*H)
        if c is not None: hip_cands.append((z,w,c))
    hips_z, hips_w, hips_c = max(hip_cands, key=lambda t:t[1]) if hip_cands else (z0+0.52*H, 0.2, np.array([0,0,z0+0.52*H]))
    hips=hips_c.copy(); hips[2]=hips_z

    # --- Shoulders: find arm-separation height ---
    # At each z, compare full-mesh half-width vs body-column half-width; shoulders where difference grows AND z is high.
    # Also: take the 90th percentile Z of verts with |x| > body half-width*1.1 (upper arm verts) — their top cluster.
    body_hw_mid,_=width_at(body0, hips_z+0.15*H, 0.02*H)
    body_hw_mid=max(body_hw_mid/2, 0.08)
    # provisional body half-width at chest
    armish = (co[:,0] > body_hw_mid*1.15) | (co[:,0] < -body_hw_mid*1.15)
    armish &= co[:,2] > hips_z
    if armish.sum()>100:
        # shoulder height ≈ high percentile of armish verts that are near the body laterally (proximal)
        prox = armish & (np.abs(co[:,0]) < body_hw_mid*2.2)
        sh_z = float(np.percentile(co[prox,2], 88)) if prox.sum()>50 else float(np.percentile(co[armish,2], 90))
    else:
        sh_z = z0 + 0.82*H
    # clamp shoulders into a sane band
    sh_z = float(np.clip(sh_z, z0+0.72*H, z0+0.88*H))
    sh_c = body_centroid_at(co, body0, sh_z, 0.02*H)

    # body half-width at shoulders
    bw,_=width_at(body0, sh_z, 0.02*H)
    body_hw = max(bw/2.0, 0.09)

    # --- Neck: narrowest body column above shoulders, below head ---
    neck_cands=[]
    for z in np.linspace(sh_z+0.01*H, z0+0.95*H, 20):
        w,c=width_at(body0,z,0.012*H)
        if c is not None: neck_cands.append((z,w,c))
    if neck_cands:
        neck_z, _, neck_c = min(neck_cands, key=lambda t:t[1])
    else:
        neck_z=sh_z+0.04*H; neck_c=np.array([0.,sh_c[1],neck_z])
    neck_z=float(np.clip(neck_z, sh_z+0.015*H, z0+0.96*H))

    # --- Head ---
    head_start=np.array([0., float(neck_c[1]), neck_z+0.015*H])
    head_end=np.array([0., float(neck_c[1]), z1-0.02*H])

    # --- Spine: strictly increasing between hips and neck ---
    # Mixamo: Hips (short up), Spine, Spine1, Spine2 -> Neck
    neck_base=np.array([float(neck_c[0]), float(neck_c[1]), neck_z])
    # also put Spine2 end at shoulder height-ish (clavicle level), Neck starts there
    # Better Mixamo layout: Spine2 ends at base of neck / top of chest (= sh_z + small)
    chest_z = sh_z - 0.02*H
    def lerp_z(t):
        z=hips_z + t*(chest_z - hips_z)
        c=body_centroid_at(co, body0, z, 0.02*H); c[2]=z
        # stabilize lateral drift / breast-bias in Y: blend toward hips x/y
        c[0]=0.15*c[0]+0.85*hips[0]
        c[1]=0.35*c[1]+0.65*hips[1]
        return c
    # Hips bone tail = spine0 root
    s0=lerp_z(0.18); s1=lerp_z(0.45); s2=lerp_z(0.75)
    spine2_tail=body_centroid_at(co, body0, chest_z, 0.02*H); spine2_tail[2]=chest_z
    neck_start=spine2_tail.copy()
    neck_end=head_start.copy()

    # --- Legs ---
    def leg_joints(side):
        sign=1.0 if side=='L' else -1.0
        mask=(co[:,2] < hips_z-0.01*H) & (co[:,2] > z0+0.01*H) & (sign*co[:,0] > 0.025)
        if mask.sum()<80:
            x=sign*0.1*H
            return dict(
                upleg=np.array([x,hips[1],hips_z-0.02*H]),
                knee=np.array([x,hips[1],z0+0.28*H]),
                ankle=np.array([x,hips[1],z0+0.055*H]),
                toe=np.array([x,hips[1]+face_forward*0.07*H,z0+0.02*H]),
                toe_tip=np.array([x,hips[1]+face_forward*0.11*H,z0+0.012*H]))
        # knee = narrowest in 0.22..0.36 H
        kc=[]
        for z in np.linspace(z0+0.22*H, z0+0.36*H, 22):
            m=mask & (np.abs(co[:,2]-z)<0.014*H)
            if m.sum()<12: continue
            kc.append((z, float(co[m,0].max()-co[m,0].min()), co[m].mean(0)))
        if kc:
            knee_z,_,knee=min(kc,key=lambda t:t[1]); knee=knee.copy(); knee[2]=knee_z
        else:
            knee=co[mask].mean(0); knee[2]=z0+0.28*H
        # ankle
        ac=[]
        for z in np.linspace(z0+0.025*H, z0+0.12*H, 18):
            m=mask & (np.abs(co[:,2]-z)<0.01*H)
            if m.sum()<8: continue
            ac.append((z, float(co[m,0].max()-co[m,0].min()), co[m].mean(0)))
        if ac:
            ankle_z,_,ankle=min(ac,key=lambda t:t[1]); ankle=ankle.copy(); ankle[2]=ankle_z
        else:
            ankle=knee.copy(); ankle[2]=z0+0.05*H
        up_m=mask & (co[:,2] > hips_z-0.1*H)
        upleg=co[up_m].mean(0) if up_m.sum()>15 else knee.copy()
        upleg=upleg.copy(); upleg[2]=hips_z-0.015*H
        # keep under hip socket laterally ~ body
        upleg[0]=sign*min(abs(upleg[0]), body_hw*1.15) if abs(upleg[0])>body_hw*0.5 else sign*body_hw*0.85
        # foot
        foot_m=mask & (co[:,2] < ankle[2]+0.05*H)
        if foot_m.sum()>15:
            fv=co[foot_m]
            tip=fv[np.argmax(fv[:,1]*face_forward)].copy()
            toe=ankle + 0.55*(tip-ankle); toe[2]=z0+0.018*H
            tip[2]=z0+0.012*H
        else:
            toe=ankle+np.array([0,face_forward*0.06*H,-ankle[2]+0.018*H])
            tip=toe+np.array([0,face_forward*0.04*H,0])
        return dict(upleg=upleg,knee=knee,ankle=ankle,toe=toe,toe_tip=tip)

    L=leg_joints('L'); R=leg_joints('R')
    # symmetrize leg Z joints (average L/R heights) and mirror X
    for k in ('upleg','knee','ankle','toe','toe_tip'):
        z=0.5*(L[k][2]+R[k][2]); y=0.5*(L[k][1]+R[k][1]); x=0.5*(abs(L[k][0])+abs(R[k][0]))
        L[k]=np.array([x,y,z]); R[k]=np.array([-x,y,z])

    # --- Arms ---
    def arm_joints(side):
        sign=1.0 if side=='L' else -1.0
        mask=(co[:,2] > hips_z) & (sign*co[:,0] > body_hw*0.9)
        # shoulder joint near body
        clav_in=np.array([sign*body_hw*0.20, float(sh_c[1]), sh_z+0.005*H])
        sh_joint=np.array([sign*body_hw*1.05, float(sh_c[1]), sh_z])
        if mask.sum()<60:
            elbow=np.array([sign*(body_hw+0.07*H), sh_c[1], sh_z-0.20*H])
            wrist=np.array([sign*(body_hw+0.09*H), sh_c[1], sh_z-0.38*H])
            hand=wrist+np.array([sign*0.025*H, face_forward*0.01*H, -0.03*H])
            return dict(clav_in=clav_in, shoulder=sh_joint, arm=sh_joint, elbow=elbow, wrist=wrist, hand=hand)
        p=co[mask]
        mu=p.mean(0); X=p-mu; _,_,vh=np.linalg.svd(X,full_matrices=False)
        d=vh[0].copy()
        if d[2] > 0: d=-d
        if sign*d[0] < 0: d=-d
        t=(p-mu)@d
        tmin,tmax=float(t.min()),float(t.max())
        def at_frac(f):
            tt=tmin+f*(tmax-tmin)
            m=np.abs(t-tt)<max(0.02*(tmax-tmin),0.01)
            return p[m].mean(0) if m.sum()>=5 else (mu+d*tt)
        # elbow ≈ mid-length; refine by narrowest only in 0.40..0.55 band
        cands=[]
        for f in np.linspace(0.40,0.55,12):
            tt=tmin+f*(tmax-tmin); m=np.abs(t-tt)<0.04*(tmax-tmin)
            if m.sum()<8: continue
            q=p[m]-(mu+d*tt)
            rad=np.linalg.norm(q-np.outer(q@d,d),axis=1).mean()
            cands.append((f,rad,p[m].mean(0)))
        elbow=min(cands,key=lambda x:x[1])[2] if cands else at_frac(0.48)
        wrist=at_frac(0.84); hand=at_frac(0.97)
        arm_head=sh_joint.copy(); arm_head[2]=sh_z-0.005*H
        # ensure upper arm has meaningful length (at least 30% of arm span)
        if np.linalg.norm(elbow-arm_head) < 0.25*abs(tmax-tmin):
            elbow=at_frac(0.48)
        return dict(clav_in=clav_in, shoulder=sh_joint, arm=arm_head, elbow=elbow, wrist=wrist, hand=hand)

    LA=arm_joints('L'); RA=arm_joints('R')

    return dict(
        H=H, z0=z0, z1=z1, face_forward=face_forward, body_hw=float(body_hw),
        hips=hips,
        spine0=s0, spine1=s1, spine2=s2, spine2_tail=spine2_tail,
        neck=neck_start, neck_end=neck_end,
        head=head_start, head_end=head_end,
        L=L, R=R, LA=LA, RA=RA,
        sh_z=float(sh_z), hips_z=float(hips_z), neck_z=float(neck_z),
    )
