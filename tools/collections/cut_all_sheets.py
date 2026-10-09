import json, glob, os, subprocess
A='/tmp/claude-0/anime4'; OUT='/tmp/claude-0/-home-claude-ashen-hollow/071def4a-521c-5c87-b501-31362956098d/scratchpad/packs'
FB=['male_front','male_back','female_front','female_back']; FFB=['female_front','female_back','male_front','male_back']
V4=lambda s:[s+'_front',s+'_back',s+'_side_left',s+'_side_right']
J=[]
def add(coll,cls,src,spec): J.append((f'{coll.capitalize()}_{cls}',src,spec))
L=A+'/lava/Lava_Anime_Fantasy/%s_Characters_Equipment.png'
add('lava','Druid',L%'Druid',{'figs':[30,0,960,400,FB],'items':{'helmet_antler_crown':[5,450,155,630],'helmet_hood':[160,450,265,630],'weapon_staff':[580,450,700,630],'weapon_sickle':[705,450,820,630],'weapon_shield':[825,450,990,630]}})
add('lava','Mage',L%'Mage',{'figs':[10,0,990,390,FB],'items':{'helmet_hood':[5,440,135,610],'weapon_staff':[600,450,700,620],'weapon_wand':[735,450,780,620],'weapon_grimoire':[800,450,990,610]}})
add('lava','Priest',L%'Priest',{'figs':[50,20,990,440,FB],'items':{'helmet_front':[10,480,130,620],'helmet_back':[140,480,250,620],'shoulders_front':[250,490,370,620],'shoulders_back':[370,490,495,620],'weapon_staff':[665,450,745,630],'weapon_mace':[770,480,850,630],'weapon_tome':[870,505,980,620]}})
add('lava','Ranger',L%'Ranger',{'figs':[20,20,990,390,FB],'items':{'weapon_longbow':[20,430,265,600],'weapon_quiver':[320,420,455,600],'weapon_short_sword':[530,440,610,630],'weapon_crossbow':[670,430,990,530],'weapon_crossbow_side':[660,540,990,600]}})
add('lava','Rogue',L%'Rogue',{'figs':[20,20,990,450,FB],'items':{'helmet':[10,490,130,630],'pauldron':[295,490,410,630],'weapon_dual_daggers':[650,490,780,630],'weapon_long_dagger':[790,480,860,630],'weapon_throwing_daggers':[870,495,990,620]}})
add('lava','Shaman',L%'Shaman',{'figs':[[40,10,450,400,['male_front','male_back']],[550,10,960,400,['female_front','female_back']]],'items':{'weapon_warhammer':[20,430,170,630],'weapon_warhammer_side':[170,430,250,630],'weapon_axe':[270,420,480,630],'weapon_orb_relic':[520,430,640,620],'weapon_spirit_totem':[760,420,870,620]}})
D=A+'/draconic/%s_Anime_Fantasy.png'
add('draconic','Druid',D%'Druid',{'figs':[15,30,650,450,FFB],'items':{'weapon_sickle':[660,70,765,370],'weapon_staff':[755,10,850,420],'weapon_shield':[830,180,990,380],'helmet_front':[15,475,150,625],'helmet_side':[155,475,265,625],'helmet_back':[270,475,385,625],'shoulder':[550,475,670,625]}})
add('draconic','Mage',D%'Mage',{'figs':[10,50,670,430,FFB],'items':{'weapon_staff':[680,55,792,440,[[773,290,800,440]]],'weapon_wand':[790,70,845,282],'weapon_spellbook':[850,100,990,230],'weapon_spellbook_open':[770,280,990,425],'helmet_front':[10,490,130,630],'helmet_side':[135,490,230,630],'helmet_back':[235,490,330,630],'pauldron':[340,490,460,630],'pauldron_inner':[465,490,610,630]}})
add('draconic','Priest',D%'Priest',{'figs':[[15,20,330,460,['female_front','female_back']],[660,20,990,460,['male_front','male_back']]],'items':{'weapon_mace':[330,130,420,420],'weapon_staff':[445,90,555,440],'weapon_tome':[555,250,670,430],'helmet_front':[10,480,145,630],'helmet_side':[150,480,290,630],'shoulder':[295,480,480,630]}})
add('draconic','Ranger',D%'Ranger',{'figs':[10,30,660,450,FFB],'items':{'weapon_bow':[675,25,770,370],'weapon_crossbow':[770,50,900,300],'weapon_quiver':[895,50,990,370],'weapon_short_sword':[760,300,840,490,[[800,300,850,380]]],'helmet_hood':[10,495,165,640],'shoulder':[175,495,320,640]}})
add('draconic','Rogue',D%'Rogue',{'figs':[[10,20,370,460,['female_front','female_back']],[640,20,990,460,['male_front','male_back']]],'items':{'weapon_pair_daggers':[385,75,520,270],'weapon_straight_dagger':[545,80,610,270],'weapon_ornamental_daggers':[395,290,615,460],'helmet_front':[15,495,150,630],'helmet_side':[150,495,290,630],'shoulder':[320,495,480,630],'gauntlet':[700,495,820,630]}})
add('draconic','Shaman',D%'Shaman',{'figs':[20,40,665,460,FFB],'items':{'weapon_axe':[675,55,815,300],'weapon_hammer':[815,55,975,300],'weapon_orb':[680,320,815,500],'weapon_totem':[830,320,935,500],'helmet':[25,500,140,640],'shoulder':[150,500,285,640]}})
M=A+'/demonic/%s/Demonic_%s_Anime_Fantasy.png'
add('demonic','Druid',M%('Druid','Druid'),{'figs':[[40,20,550,310,V4('male')],[40,330,550,620,V4('female')]],'items':{'helmet':[555,180,665,300],'shoulder':[665,180,750,300],'weapon_thorn_staff':[760,60,850,480],'weapon_sickle':[840,60,990,250],'weapon_shield':[825,270,990,460]}})
add('demonic','Mage',M%('Mage','Mage'),{'items':{'male_front':[20,40,195,300],'male_back':[195,40,350,300],'male_side_left':[350,40,468,300],'male_side_right':[466,30,562,300],'female_front':[20,355,195,620],'female_back':[195,330,350,620],'female_side_left':[350,330,468,620],'female_side_right':[466,330,562,620],'weapon_crystal_staff':[670,80,750,340],'weapon_wand':[785,90,815,340],'weapon_spellbook':[815,110,990,320],'helmet':[670,410,820,500],'shoulder':[825,380,990,500]}})
add('demonic','Priest',M%('Priest','Priest'),{'figs':[[110,0,650,270,V4('male')],[112,280,650,510,V4('female')]],'items':{'weapon_halo_staff':[665,85,770,490],'weapon_mace':[765,140,850,470],'weapon_grimoire':[850,220,990,450],'helmet_front':[10,525,150,640],'helmet_back':[160,525,295,640],'shoulder':[305,525,435,640]}})
add('demonic','Ranger',M%('Ranger','Ranger'),{'figs':[[50,0,650,285,V4('male')],[50,300,650,545,V4('female')]],'items':{'female_front':[50,300,200,545,[[0,295,105,342]]],'weapon_bow':[665,60,760,380],'weapon_quiver':[760,90,880,300],'weapon_arrows':[885,90,990,300],'weapon_crossbow':[662,328,905,478,[[655,320,742,392]]],'weapon_dagger':[905,310,990,480],'helmet':[40,560,150,630],'shoulder':[225,560,345,630]}})
add('demonic','Rogue',M%('Rogue','Rogue'),{'items':{'male_front':[10,0,200,320,[[0,0,75,35]]],'male_back':[200,0,370,320],'male_side_left':[370,0,490,320],'male_side_right':[490,0,610,320],'female_front':[10,340,200,650,[[0,335,75,372]]],'female_back':[200,340,370,650],'female_side_left':[370,340,490,650],'female_side_right':[490,340,610,650],'weapon_twin_daggers':[675,60,880,250],'weapon_curved_sword':[895,60,990,380],'weapon_throwing_knives':[700,270,890,400],'helmet':[640,435,730,540],'shoulder':[735,435,820,540]}})
add('demonic','Shaman',M%('Shaman','Shaman'),{'figs':[[80,20,690,250,V4('male')],[80,290,690,500,V4('female')]],'items':{'weapon_war_hammer':[710,40,860,300],'weapon_skull_axe':[865,40,990,300],'weapon_skull_totems':[720,290,990,500],'helmet_front':[5,535,150,640],'helmet_side':[155,535,265,640],'shoulder':[265,535,375,640]}})
F=A+'/fossil/Fossil_Anime/'
M6=['front','back','side_left','side_right','threeq_front','threeq_back']; FSB=['front','side_left','back','side_right','threeq_front','threeq_back']
fos={ # class: (male spec, female spec)
 'Druid':({'figs':[0,0,1000,540,M6],'items':{'helmet':[0,560,205,875],'shoulder':[415,560,615,875]}},{'figs':[0,10,1000,560,FSB],'items':{'helmet':[0,565,205,914],'shoulder':[435,565,615,914]}}),
 'Mage':({'figs':[0,40,1000,530,M6],'items':{'helmet':[0,555,195,880],'shoulder':[410,555,610,880]}},{'figs':[0,5,1000,580,FSB],'items':{'helmet':[5,590,200,914],'shoulder':[425,590,615,914]}}),
 'Priest':({'figs':[0,0,1000,460,M6],'items':{'helmet':[5,485,190,705],'shoulder':[435,485,620,705]}},{'figs':[[50,0,1000,415,M6[:4]],[150,415,800,730,M6[4:]]],'items':{'helmet':[5,740,190,914],'shoulder':[400,740,615,914]}}),
 'Ranger':({'figs':[0,0,1000,505,M6],'items':{'helmet':[5,530,165,700],'shoulder':[350,530,500,870]}},{'figs':[0,0,1000,640,M6],'items':{'helmet':[10,700,185,990],'shoulder':[375,700,540,980]}}),
 'Rogue':({'figs':[0,0,1000,500,M6],'items':{'helmet':[5,525,215,715],'shoulder':[450,525,620,860]}},{'figs':[0,0,1000,555,M6],'items':{'helmet':[5,580,160,915],'shoulder':[510,580,680,915]}}),
 'Shaman':({'figs':[[80,0,1000,340,M6[:3]],[80,340,1000,680,M6[3:]]],'items':{'helmet':[5,690,195,905],'shoulder':[415,690,605,905]}},{'figs':[0,0,1000,540,M6],'items':{'helmet':[0,545,195,914],'shoulder':[440,545,630,914]}}),
 'Warrior':({'figs':[0,0,1000,445,M6],'items':{'helmet':[5,455,205,740],'shoulder':[415,455,610,740]}},{'figs':[0,0,1000,580,M6],'items':{'helmet':[5,585,215,914],'shoulder':[425,585,615,914]}}),
}
FW={
 'Druid':{'branch_staff':{'weapon_staff':[0,40,125,700],'weapon_staff_side':[370,40,470,700]},'crescent_sickle':{'weapon_sickle':[0,30,215,380],'weapon_sickle_side':[420,20,470,380]},
          'petrified_wood_round_shield':{'weapon_shield_front':[0,40,310,370],'weapon_shield_back':[315,40,620,370],'weapon_shield_side':[630,40,710,370]}},
 'Mage':{'fossil_staff':{'weapon_staff':[0,60,130,700],'weapon_staff_side':[140,60,210,700]},'open_spellbook':{'weapon_spellbook':[20,20,505,320]},'wand':{'weapon_wand':[0,40,100,700],'weapon_wand_side':[250,40,310,700]}},
 'Priest':{'ceremonial_mace':{'weapon_mace':[0,20,150,670],'weapon_mace_side':[370,20,450,670]},'healing_staff':{'weapon_staff':[10,30,180,700],'weapon_staff_side':[400,30,470,700]},
          'prayer_tome':{'weapon_tome_front':[5,60,265,340],'weapon_tome_back':[270,60,520,340],'weapon_tome_side':[525,60,615,340]}},
 'Ranger':{'bow':{'weapon_bow':[10,30,170,700],'weapon_bow_side':[365,10,415,700]},'crossbow':{'weapon_crossbow':[0,10,620,300],'weapon_crossbow_side':[0,310,560,400],'weapon_crossbow_top':[615,10,1000,130]},
          'quiver_with_arrows':{'weapon_quiver_front':[20,40,200,420],'weapon_quiver_back':[195,40,355,420]},'short_sword':{'weapon_short_sword':[0,40,115,395],'weapon_short_sword_side':[255,25,300,395]}},
 'Rogue':{'paired_curved_daggers':{'weapon_curved_dagger':[0,60,125,345],'weapon_curved_dagger_side':[480,70,540,345]},'straight_dagger':{'weapon_straight_dagger':[5,65,135,585],'weapon_straight_dagger_side':[300,65,365,585]},
          'throwing_knives':{'weapon_throwing_knife':[10,30,100,560],'weapon_throwing_knife_side':[190,30,225,560]}},
 'Shaman':{'curved_axe':{'weapon_axe':[0,30,195,395],'weapon_axe_side':[395,30,470,395]},'hammer':{'weapon_hammer':[0,10,215,355],'weapon_hammer_side':[400,10,490,355]},
          'round_fossil_relic':{'weapon_relic':[0,30,180,425],'weapon_relic_side':[190,30,295,425]},'tall_fossil_totem':{'weapon_totem':[0,10,190,410],'weapon_totem_side':[370,30,480,410]}},
 'Warrior':{'battle_axe':{'weapon_battle_axe':[10,40,200,455],'weapon_battle_axe_side':[370,10,440,455]},'greatsword':{'weapon_greatsword':[80,0,200,510],'weapon_greatsword_side':[400,0,470,510]},
          'kite_shield':{'weapon_shield_front':[0,40,180,400],'weapon_shield_back':[175,40,345,400],'weapon_shield_side':[910,70,990,400]}},
}
def pre(spec,p):
    s={}
    f=spec['figs']; f=f if isinstance(f[0],list) else [f]
    s['figs']=[b[:4]+[[p+n for n in b[4]],0.045] for b in f]
    s['items']={p+k:v for k,v in spec['items'].items()}; return s
for c,(m,f) in fos.items():
    add('fossil',c,F+c+'/Male.png',pre(m,'male_')); add('fossil',c,F+c+'/Female.png',pre(f,'female_'))
    for w,its in FW.get(c,{}).items(): add('fossil',c,F+c+'/Weapon_'+w+'.png',{'items':its})
for name,src,spec in J:
    r=subprocess.run(['python3','-I','/tmp/claude-0/cut/cutter.py',src,OUT+'/'+name,json.dumps(spec)],capture_output=True,text=True)
    if r.returncode: print(name,src,r.stderr[-300:])
print(len(J),'jobs')
