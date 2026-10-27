import sys, os; sys.path.insert(0, os.path.dirname(__file__)); from au import *
os.chdir(os.environ.get('AUDIO_SRC', '.')); O='out/'
def loud(y, target=-16, tp=-1.0):
    # integrated-ish loudness via RMS of active frames (K-weighting skipped), peak capped
    return norm_rms(y, target, tp)
# music: seamless CC0 march loop, used whole so its loop point stays intact
m=load('the_march_of_devils_dome_loop.wav',2)
m=norm_rms(m,-21.5,-1.0); write(m,O+'mus_runner_march_loop.ogg',q=0)
# win: cynicmusic victory fanfare short, natural ending
w=load('Heavy_ConceptB.wav',2); w=tail_trim(trim(w,db=-40,fade=0.4),-55); write(norm_rms(w,-14,-1.0),O+'jingle_win.ogg',q=3)
# lose: low war horn phrase pitched down + slow heavy drum hits
h=load('war_horns.wav',2,rate=0.84)
horn=trim(h,start=0.6/0.84,dur=4.2,fade=1.4)
d=lowpass(load('horde_war_drums_by_william_hector.wav',2,rate=0.7),900)
drum=trim(d,start=0.0,dur=1.2,fade=0.6)
l=mix([(horn,0.15,1.0),(drum,0.0,0.9),(drum,2.0,0.55)],2)
write(norm_rms(tail_trim(l,-55),-17,-1.0),O+'jingle_lose.ogg',q=3)
# boss incoming: war drums + horn blast
hh=load('war_horns.wav',2)
hb=trim(hh,start=22.8,dur=4.5,fade=1.5)
dr=trim(load('horde_war_drums_by_william_hector.wav',2),start=0.0,dur=4.0,fade=1.0)
st=mix([(dr,0,0.9),(hb,0.4,1.0)],2)
write(norm_rms(tail_trim(st,-55),-20,-1.0),O+'sting_boss_incoming.ogg',q=3)
for f in ['mus_runner_march_loop','jingle_win','jingle_lose','sting_boss_incoming']:
    print(f, os.path.getsize(O+f+'.ogg'))
