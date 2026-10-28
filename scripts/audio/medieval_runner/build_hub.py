import sys, os; sys.path.insert(0, os.path.dirname(__file__)); from au import *
os.chdir(os.environ.get('AUDIO_SRC', '.')); O='out/'
K='kenney_impact-sounds/Audio/'; KR='kenney_rpg-audio/Audio/'; M='x_medieval/'
C1='x_80-CC0-creature-SFX_0/'; C2='x_80-CC0-creature-sfx-2/'; R='x_80-CC0-RPG-SFX_0/'
B='x_sfx_breaking_and_falling/'; W='x_100-CC0-wood-metal-SFX/'; SC='x_sword_clash_-_starninjas_0/'; TS='x_weapons-apparel/sfx/'
def s(path, dur=None, rate=1.0, start=None, db=-30, fade=0.05):
    if path.startswith(M) and db==-30: db=-14
    return trim(load(path,1,rate), start=start, dur=dur, db=db, fade=fade)
def out(name, y, rms=-17, peak=-1.0, grit=0.0, hp=60):
    y=highpass(y,hp)
    if grit: y=comp(y,grit)
    y=norm_rms(tail_trim(y),rms,peak); write(y,O+name); print(name, round(len(y)/SR,2))
# --- UI ---
out('ui_tap.ogg', mix([(s(K+'impactWood_light_002.ogg',0.12),0,1.0),(s(KR+'bookOpen.ogg',0.1),0,0.35)]), -17, hp=150)
out('ui_deny.ogg', mix([(lowpass(s(K+'impactWood_medium_003.ogg',0.25,rate=0.8),1400),0,1.0),(lowpass(s(KR+'dropLeather.ogg',0.3),2500),0.0,0.7)]), -14)
out('ui_store_open.ogg', mix([(s(KR+'metalLatch.ogg',0.3),0,0.7),(s(KR+'doorOpen_2.ogg',1.4,fade=0.3),0.12,1.0)]), -18)
out('ui_gift_celebrate.ogg', mix([(s(K+'impactBell_heavy_001.ogg',1.7,rate=1.5,fade=0.6),0,0.9),(s(K+'impactBell_heavy_000.ogg',1.5,rate=2.0,fade=0.6),0.18,0.6),
    (s('victory.wav',1.9,start=0.0,fade=0.8),0.05,0.8),(s(KR+'handleCoins.ogg',0.85),0.3,0.6)]), -16)
out('purchase_success.ogg', mix([(s(KR+'beltHandle1.ogg',0.4),0,0.6),(s(KR+'handleCoins.ogg',0.85),0.05,1.0),(s(R+'item_coins_01.ogg',0.4),0.35,0.8)]), -16)
# --- economy ---
out('collect_food.ogg', mix([(s(K+'footstep_grass_002.ogg',0.3),0,0.8),(s(KR+'cloth2.ogg',0.42),0.02,0.9),(s(R+'item_wood_02.ogg',0.25,rate=0.9),0.0,0.35)]), -18)
out('collect_gold.ogg', mix([(s(R+'item_coins_03.ogg',0.48),0,1.0),(s(KR+'handleCoins2.ogg',0.34),0.08,0.6)]), -17)
out('collect_resource.ogg', mix([(s(R+'item_wood_01.ogg',0.4),0,1.0),(s(R+'item_stone_03.ogg',0.35),0.07,0.7)]), -16)
out('upgrade_start.ogg', mix([(s(W+'wood_hammer_01.ogg',0.3),0,1.0),(s(W+'hammer_02.ogg',0.24),0.22,0.7),(s(W+'metal_hit_02.ogg',0.43),0.22,0.6),(s(W+'wood_hammer_02.ogg',0.23),0.45,0.8)]), -18)
out('build_complete.ogg', mix([(s(W+'wood_hammer_01.ogg',0.3),0,0.8),(s(W+'wood_hammer_02.ogg',0.23),0.18,0.8),
    (s(W+'hammer_02.ogg',0.24),0.38,1.0),(s(W+'metal_hit_02.ogg',0.43),0.38,0.8),(s(K+'impactBell_heavy_001.ogg',1.7,rate=1.2,fade=0.7),0.42,0.7),
    (s('war_horns.wav',1.5,start=2.4,fade=0.7),0.55,0.55)]), -19)
# --- battle-only impacts ---
out('sfx_sword_hit.ogg', mix([(s(TS+'sword-knife-clash-07.wav',0.35),0,1.0),(s(SC+'sword_clash.1.ogg',0.3),0,0.5),(s(K+'impactPunch_medium_001.ogg',0.2),0,0.6)]), -17)
out('sfx_arrow_hit.ogg', mix([(s(K+'impactWood_medium_002.ogg',0.18),0,1.0),(s(K+'impactPunch_medium_003.ogg',0.18),0,0.7)]), -17)
out('sfx_magic_impact.ogg', mix([(s(R+'spell_02.ogg',0.6),0,1.0),(s(R+'spell_fire_01.ogg',0.5),0,0.4),(lowpass(s('boulder_drop_0.ogg',0.6,rate=0.5,fade=0.3),200),0.0,1.2)]), -17, grit=0.25)
# --- boss (battle-only attacks) ---
out('sfx_ironhowl_punch.ogg', mix([(s(K+'impactPunch_heavy_001.ogg',0.35,rate=0.7),0,1.0),(s(K+'impactPlate_heavy_001.ogg',0.35),0,0.6),(s(C2+'attack_02.ogg',0.5,rate=0.75),0,0.5)]), -16, grit=0.3)
out('sfx_ironhowl_sweep.ogg', mix([(s(M+'Axe Swing.wav',0.7,rate=0.6),0,1.0),(s(C2+'attack_04.ogg',0.6,rate=0.7),0.0,0.5),(s(K+'impactPunch_heavy_004.ogg',0.3,rate=0.7),0.3,0.7)]), -16, grit=0.2)
out('sfx_ironhowl_charge.ogg', mix([(s(C2+'stomp_01.ogg',0.4,rate=0.7),0,0.9),(s(K+'impactPunch_heavy_002.ogg',0.3,rate=0.5),0.22,0.9),(s(K+'impactPunch_heavy_003.ogg',0.3,rate=0.5),0.40,1.0),
    (s(C2+'roar_04.ogg',0.8,rate=0.8,fade=0.3),0.1,0.6)]), -15, grit=0.3)
