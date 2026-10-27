import sys, os; sys.path.insert(0, os.path.dirname(__file__)); from au import *
os.chdir(os.environ.get('AUDIO_SRC', '.')); O='out/'
M='x_medieval/'; K='kenney_impact-sounds/Audio/'; KR='kenney_rpg-audio/Audio/'
C1='x_80-CC0-creature-SFX_0/'; C2='x_80-CC0-creature-sfx-2/'; R='x_80-CC0-RPG-SFX_0/'
B='x_sfx_breaking_and_falling/'; W='x_100-CC0-wood-metal-SFX/'; SC='x_sword_clash_-_starninjas_0/'; TS='x_weapons-apparel/sfx/'
def s(path, dur=None, rate=1.0, start=None, db=-30, fade=0.05):
    if path.startswith(M) and db==-30: db=-14
    return trim(load(path,1,rate), start=start, dur=dur, db=db, fade=fade)
def out(name, y, rms=-16, peak=-1.0, grit=0.0, hp=60):
    y=highpass(y,hp)
    if grit: y=comp(y,grit)
    y=norm_rms(tail_trim(y),rms,peak); write(y,O+name); print(name, round(len(y)/SR,2))

# --- squad shots ---
out('sfx_rowan_shot.ogg', mix([(s(M+'English Longbow Shoot.wav',0.45),0,1.0),(s(M+'Scythian Recurve Arrow Pass.wav',0.5),0.05,0.55)]), -17)
out('sfx_lyra_shot.ogg', mix([(s(M+'Spear Swing.wav',0.55,rate=0.75),0,0.9),(s(R+'spell_01.ogg',0.6,rate=0.8),0.02,0.5),
    (lowpass(s('boulder_drop_0.ogg',0.7,rate=0.5,fade=0.3),180),0.04,1.4)]), -17, grit=0.3)
out('sfx_oakenshield_throw.ogg', mix([(s(M+'Spear Swing.wav',0.45),0,1.0),(s(M+'Crossbow Bolt Pass.wav',0.45,rate=0.8),0.06,0.4)]), -17)
out('sfx_enemy_projectile.ogg', mix([(s(M+'Crossbow Shoot.wav',0.45),0,1.0),(s(M+'Crossbow Bolt Passby.wav',0.4),0.05,0.4)]), -18)
# --- hits / deaths ---
out('sfx_enemy_hit.ogg', mix([(s(SC+'sword_clash.3.ogg',0.3),0,0.8),(s(K+'impactPunch_heavy_001.ogg',0.25),0,0.9)]), -15)
out('sfx_crate_hit.ogg', mix([(s(K+'impactWood_heavy_002.ogg',0.35),0,1.0),(s(W+'wood_hit_03.ogg',0.3),0.0,0.5)]), -16)
out('sfx_enemy_death.ogg', mix([(s(C2+'die_02.ogg',0.7,rate=0.85),0,1.0),(s(K+'impactPunch_heavy_003.ogg',0.3),0,0.6)]), -16)
out('sfx_enemy_death_big.ogg', mix([(s(C2+'roar_05.ogg',1.2,rate=0.75),0,1.0),(lowpass(s('boulder_drop_0.ogg',0.8,rate=0.6),400),0.35,1.0),(s(K+'impactPunch_heavy_000.ogg',0.3,rate=0.8),0.35,0.6)]), -15, grit=0.2)
out('sfx_hero_hurt.ogg', mix([(s(K+'impactPlate_medium_001.ogg',0.3),0,1.0),(s(K+'impactPunch_medium_002.ogg',0.25),0,0.6),(s(C2+'human_02.ogg',0.5),0.02,0.35)]), -15)
out('sfx_hero_death.ogg', mix([(s(K+'impactPlate_heavy_002.ogg',0.4),0,1.0),(s(B+'bfh1_metal_falling_02.ogg',1.2),0.12,0.7),(s(C2+'human_06.ogg',0.7,rate=0.9),0,0.4)]), -16)
out('sfx_follower_down.ogg', mix([(s(K+'impactPlate_heavy_000.ogg',0.35),0,0.9),(s(B+'bfh1_metal_falling_01.ogg',0.9),0.08,0.6)]), -17)
out('sfx_follower_join.ogg', mix([(s(TS+'seax-unsheathe-01.wav',0.6),0,1.0),(s(K+'impactPlate_light_001.ogg',0.25),0.0,0.35)]), -17)
# --- traps ---
out('sfx_hazard_spikes.ogg', mix([(s(K+'impactMetal_heavy_001.ogg',0.35),0,1.0),(s(K+'impactPunch_heavy_002.ogg',0.25),0,0.7),(s(R+'blade_02.ogg',0.3),0.0,0.5)]), -15)
out('sfx_hazard_log.ogg', mix([(s(K+'impactWood_heavy_000.ogg',0.4,rate=0.8),0,1.0),(s(W+'wood_falling_02.ogg',0.8),0.05,0.6),(s(K+'impactPunch_heavy_004.ogg',0.3),0,0.5)]), -15)
out('sfx_hazard_fire.ogg', mix([(s(R+'spell_fire_04.ogg',1.0),0,1.0),(s('fire-1.wav',0.9,start=0.3,fade=0.4),0.05,0.6),(s(K+'impactPunch_medium_000.ogg',0.2),0,0.4)]), -16)
# --- barriers (new) ---
out('sfx_barrier_wood_hit.ogg', mix([(s(W+'wood_cracking_02.ogg',0.4),0,1.0),(s(K+'impactWood_medium_001.ogg',0.3),0,0.6)]), -16)
out('sfx_barrier_wood_break.ogg', mix([(s(B+'bfh1_wood_breaking_02.ogg',1.2),0,1.0),(s(W+'wood_breaking_01.ogg',1.0),0.05,0.7),(s(W+'wood_falling_04.ogg',0.9),0.3,0.5)]), -14, grit=0.15)
out('sfx_barrier_stone_hit.ogg', mix([(s(K+'impactMining_002.ogg',0.35),0,1.0),(s(B+'bfh1_rock_hit_01.ogg',0.4),0,0.6)]), -16)
out('sfx_barrier_stone_break.ogg', mix([(s(B+'bfh1_rock_breaking_01.ogg',1.2),0,1.0),(s('rock_break_0.ogg',0.45),0.02,0.7),(s(B+'bfh1_rock_falling_03.ogg',1.2),0.25,0.7),(lowpass(s('boulder_drop_0.ogg',0.8,rate=0.7),500),0.05,0.8)]), -14, grit=0.15)
out('sfx_barrier_spike_hit.ogg', mix([(s(K+'impactWood_heavy_003.ogg',0.3),0,0.9),(s(K+'impactMetal_medium_002.ogg',0.3),0,0.6)]), -16)
out('sfx_barrier_spike_break.ogg', mix([(s(B+'bfh1_breaking_02.ogg',1.0),0,1.0),(s(B+'bfh1_wood_breaking_03.ogg',1.0),0.03,0.7),(s(B+'bfh1_metal_falling_03.ogg',1.0),0.2,0.6)]), -14, grit=0.15)
# --- boss (runner + battle) ---
out('sfx_ironhowl_growl.ogg', mix([(s(C2+'roar_04.ogg',1.0,rate=0.7),0,1.0),(s(C1+'grunt_01.ogg',1.0,rate=0.7),0.1,0.5)]), -15, grit=0.2)
out('sfx_ironhowl_ground_pound.ogg', mix([(lowpass(s('boulder_drop_0.ogg',0.9,rate=0.5),600),0,1.2),(s(K+'impactPunch_heavy_000.ogg',0.35,rate=0.6),0,0.9),(s(B+'bfh1_rock_falling_05.ogg',1.0),0.08,0.5)]), -13, grit=0.3)
out('sfx_boss_dodge_warning.ogg', mix([(s(C2+'roar_06.ogg',1.0,rate=0.65),0,1.0),(lowpass(s('horde_war_drums_by_william_hector.wav',0.6,start=0.0),300),0,0.8)]), -15, grit=0.2)
out('sfx_boss_quake_rumble.ogg', mix([(lowpass(s(B+'bfh1_rock_falling_01.ogg',1.6,rate=0.5),900),0,1.0),(lowpass(s(B+'bfh1_rock_falling_07.ogg',1.6,rate=0.6),900),0.2,0.8),(lowpass(s('boulder_drop_0.ogg',1.0,rate=0.4),300),0.0,1.0)]), -15, grit=0.3, hp=30)
