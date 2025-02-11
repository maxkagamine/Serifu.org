// Copyright (c) Max Kagamine
//
// This program is free software: you can redistribute it and/or modify it under
// the terms of version 3 of the GNU Affero General Public License as published
// by the Free Software Foundation.
//
// This program is distributed in the hope that it will be useful, but WITHOUT
// ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
// FOR A PARTICULAR PURPOSE. See the GNU Affero General Public License for more
// details.
//
// You should have received a copy of the GNU Affero General Public License
// along with this program. If not, see https://www.gnu.org/licenses/.

using Mutagen.Bethesda.Plugins;

namespace Serifu.Importer.Skyrim;

// https://en.uesp.net/wiki/Skyrim_Mod:Mod_File_Format/DIAL
internal static class SubtypeName
{
    public static readonly RecordType ActorCollideWithActor = "ACAC";
    public static readonly RecordType AcceptYield = "ACYI";
    public static readonly RecordType Agree = "AGRE";
    public static readonly RecordType AlertIdle = "ALIL";
    public static readonly RecordType AllyKilled = "ALKL";
    public static readonly RecordType AlertToCombat = "ALTC";
    public static readonly RecordType AlertToNormal = "ALTN";
    public static readonly RecordType AskFavor = "ASKF";
    public static readonly RecordType AskGift = "ASKG";
    public static readonly RecordType AssaultNC = "ASNC";
    public static readonly RecordType Assault = "ASSA";
    public static readonly RecordType Attack = "ATCK";
    public static readonly RecordType AvoidThreat = "AVTH";
    public static readonly RecordType BarterExit = "BAEX";
    public static readonly RecordType Bash = "BASH";
    public static readonly RecordType BleedOut = "BLED";
    public static readonly RecordType Block = "BLOC";
    public static readonly RecordType EnterSprintBreath = "BREA";
    public static readonly RecordType Bribe = "BRIB";
    public static readonly RecordType CombatToLost = "COLO";
    public static readonly RecordType CombatToNormal = "COTN";
    public static readonly RecordType Custom = "CUST";
    public static readonly RecordType DestroyObject = "DEOB";
    public static readonly RecordType Death = "DETH";
    public static readonly RecordType DetectFriendDie = "DFDA";
    public static readonly RecordType EnterBowZoomBreath = "ENBZ";
    public static readonly RecordType ExitBowZoomBreath = "EXBZ";
    public static readonly RecordType Favor = "FAVO";
    public static readonly RecordType ExitFavorState = "FEXT";
    public static readonly RecordType ShootBow = "FIWE";
    public static readonly RecordType Flatter = "FLAT";
    public static readonly RecordType Flee = "FLEE";
    public static readonly RecordType FlyingMountAcceptTarget = "FMAT";
    public static readonly RecordType FlyingMountDestinationReached = "FMDR";
    public static readonly RecordType FlyingMountLand = "FMLX";
    public static readonly RecordType FlyingMountNoTarget = "FMNT";
    public static readonly RecordType FlyingMountRejectTarget = "FMRT";
    public static readonly RecordType FlyingMountCancelLand = "FMXL";
    public static readonly RecordType Follow = "FOLL";
    public static readonly RecordType Reject = "FRJT";
    public static readonly RecordType Goodbye = "GBYE";
    public static readonly RecordType Gift = "GIFF";
    public static readonly RecordType CombatGrunt = "GRNT";
    public static readonly RecordType GroupStrategy = "GRST";
    public static readonly RecordType Hello = "HELO";
    public static readonly RecordType Hit = "HIT_";
    public static readonly RecordType SharedInfo = "IDAT";
    public static readonly RecordType Idle = "IDLE";
    public static readonly RecordType Intimidate = "INTI";
    public static readonly RecordType Jump = "JUMP";
    public static readonly RecordType KnockOverObject = "KNOO";
    public static readonly RecordType LostIdle = "LOIL";
    public static readonly RecordType LockedObject = "LOOB";
    public static readonly RecordType LostToCombat = "LOTC";
    public static readonly RecordType LostToNormal = "LOTN";
    public static readonly RecordType LeaveWaterBreath = "LWBS";
    public static readonly RecordType MoralRefusal = "MREF";
    public static readonly RecordType MurderNC = "MUNC";
    public static readonly RecordType Murder = "MURD";
    public static readonly RecordType NormalToAlert = "NOTA";
    public static readonly RecordType NormalToCombat = "NOTC";
    public static readonly RecordType NoticeCorpse = "NOTI";
    public static readonly RecordType ObserveCombat = "OBCO";
    public static readonly RecordType OutOfBreath = "OUTB";
    public static readonly RecordType PlayerCastProjectileSpell = "PCPS";
    public static readonly RecordType PlayerShout = "PCSH";
    public static readonly RecordType PlayerCastSelfSpell = "PCSS";
    public static readonly RecordType ForceGreet = "PFGT";
    public static readonly RecordType PickpocketCombat = "PICC";
    public static readonly RecordType PickpocketNC = "PICN";
    public static readonly RecordType PickpocketTopic = "PICT";
    public static readonly RecordType PlayerInIronSights = "PIRN";
    public static readonly RecordType PowerAttack = "POAT";
    public static readonly RecordType PursueIdleTopic = "PURS";
    public static readonly RecordType RechargeExit = "RCEX";
    public static readonly RecordType Recharge = "RECH";
    public static readonly RecordType RepairExit = "REEX";
    public static readonly RecordType Refuse = "REFU";
    public static readonly RecordType Repair = "REPA";
    public static readonly RecordType Rumors = "RUMO";
    public static readonly RecordType SceneDialogueAction = "SCEN";
    public static readonly RecordType ServiceRefusal = "SERU";
    public static readonly RecordType Show = "SHOW";
    public static readonly RecordType ShowRelationships = "SHRE";
    public static readonly RecordType Steal = "STEA";
    public static readonly RecordType StealFromNC = "STFN";
    public static readonly RecordType StandOnFurniture = "STOF";
    public static readonly RecordType SwingMeleeWeapon = "SWMW";
    public static readonly RecordType Taunt = "TAUT";
    public static readonly RecordType TimeToGo = "TITG";
    public static readonly RecordType Training = "TRAI";
    public static readonly RecordType TrespassAgainstNC = "TRAN";
    public static readonly RecordType Travel = "TRAV";
    public static readonly RecordType Trespass = "TRES";
    public static readonly RecordType TrainingExit = "TREX";
    public static readonly RecordType VoicePowerEndLong = "VPEL";
    public static readonly RecordType VoicePowerEndShort = "VPES";
    public static readonly RecordType VoicePowerStartLong = "VPSL";
    public static readonly RecordType VoicePowerStartShort = "VPSS";
    public static readonly RecordType WereTransformCrime = "WTCR";
    public static readonly RecordType Yield = "YIEL";
    public static readonly RecordType ZKeyObject = "ZKEY";

    public static string GetLongName(RecordType subtype) => subtype.TypeInt switch
    {
        1128350529 /* ACAC */ => nameof(ActorCollideWithActor),
        1230586689 /* ACYI */ => nameof(AcceptYield),
        1163020097 /* AGRE */ => nameof(Agree),
        1279872065 /* ALIL */ => nameof(AlertIdle),
        1280003137 /* ALKL */ => nameof(AllyKilled),
        1129598017 /* ALTC */ => nameof(AlertToCombat),
        1314147393 /* ALTN */ => nameof(AlertToNormal),
        1179341633 /* ASKF */ => nameof(AskFavor),
        1196118849 /* ASKG */ => nameof(AskGift),
        1129206593 /* ASNC */ => nameof(AssaultNC),
        1095979841 /* ASSA */ => nameof(Assault),
        1262703681 /* ATCK */ => nameof(Attack),
        1213486657 /* AVTH */ => nameof(AvoidThreat),
        1480933698 /* BAEX */ => nameof(BarterExit),
        1213415746 /* BASH */ => nameof(Bash),
        1145392194 /* BLED */ => nameof(BleedOut),
        1129270338 /* BLOC */ => nameof(Block),
        1095062082 /* BREA */ => nameof(EnterSprintBreath),
        1112101442 /* BRIB */ => nameof(Bribe),
        1330401091 /* COLO */ => nameof(CombatToLost),
        1314148163 /* COTN */ => nameof(CombatToNormal),
        1414747459 /* CUST */ => nameof(Custom),
        1112491332 /* DEOB */ => nameof(DestroyObject),
        1213482308 /* DETH */ => nameof(Death),
        1094993476 /* DFDA */ => nameof(DetectFriendDie),
        1514294853 /* ENBZ */ => nameof(EnterBowZoomBreath),
        1514297413 /* EXBZ */ => nameof(ExitBowZoomBreath),
        1331052870 /* FAVO */ => nameof(Favor),
        1415071046 /* FEXT */ => nameof(ExitFavorState),
        1163348294 /* FIWE */ => nameof(ShootBow),
        1413565510 /* FLAT */ => nameof(Flatter),
        1162169414 /* FLEE */ => nameof(Flee),
        1413565766 /* FMAT */ => nameof(FlyingMountAcceptTarget),
        1380207942 /* FMDR */ => nameof(FlyingMountDestinationReached),
        1481395526 /* FMLX */ => nameof(FlyingMountLand),
        1414417734 /* FMNT */ => nameof(FlyingMountNoTarget),
        1414679878 /* FMRT */ => nameof(FlyingMountRejectTarget),
        1280855366 /* FMXL */ => nameof(FlyingMountCancelLand),
        1280069446 /* FOLL */ => nameof(Follow),
        1414156870 /* FRJT */ => nameof(Reject),
        1163477575 /* GBYE */ => nameof(Goodbye),
        1179011399 /* GIFF */ => nameof(Gift),
        1414419015 /* GRNT */ => nameof(CombatGrunt),
        1414746695 /* GRST */ => nameof(GroupStrategy),
        1330398536 /* HELO */ => nameof(Hello),
        1599359304 /* HIT_ */ => nameof(Hit),
        1413563465 /* IDAT */ => nameof(SharedInfo),
        1162626121 /* IDLE */ => nameof(Idle),
        1230261833 /* INTI */ => nameof(Intimidate),
        1347245386 /* JUMP */ => nameof(Jump),
        1330597451 /* KNOO */ => nameof(KnockOverObject),
        1279872844 /* LOIL */ => nameof(LostIdle),
        1112493900 /* LOOB */ => nameof(LockedObject),
        1129598796 /* LOTC */ => nameof(LostToCombat),
        1314148172 /* LOTN */ => nameof(LostToNormal),
        1396856652 /* LWBS */ => nameof(LeaveWaterBreath),
        1178948173 /* MREF */ => nameof(MoralRefusal),
        1129207117 /* MUNC */ => nameof(MurderNC),
        1146246477 /* MURD */ => nameof(Murder),
        1096044366 /* NOTA */ => nameof(NormalToAlert),
        1129598798 /* NOTC */ => nameof(NormalToCombat),
        1230262094 /* NOTI */ => nameof(NoticeCorpse),
        1329807951 /* OBCO */ => nameof(ObserveCombat),
        1112823119 /* OUTB */ => nameof(OutOfBreath),
        1397769040 /* PCPS */ => nameof(PlayerCastProjectileSpell),
        1213416272 /* PCSH */ => nameof(PlayerShout),
        1397965648 /* PCSS */ => nameof(PlayerCastSelfSpell),
        1413957200 /* PFGT */ => nameof(ForceGreet),
        1128483152 /* PICC */ => nameof(PickpocketCombat),
        1313032528 /* PICN */ => nameof(PickpocketNC),
        1413695824 /* PICT */ => nameof(PickpocketTopic),
        1314015568 /* PIRN */ => nameof(PlayerInIronSights),
        1413566288 /* POAT */ => nameof(PowerAttack),
        1397904720 /* PURS */ => nameof(PursueIdleTopic),
        1480934226 /* RCEX */ => nameof(RechargeExit),
        1212368210 /* RECH */ => nameof(Recharge),
        1480934738 /* REEX */ => nameof(RepairExit),
        1430668626 /* REFU */ => nameof(Refuse),
        1095779666 /* REPA */ => nameof(Repair),
        1330468178 /* RUMO */ => nameof(Rumors),
        1313162067 /* SCEN */ => nameof(SceneDialogueAction),
        1431455059 /* SERU */ => nameof(ServiceRefusal),
        1464813651 /* SHOW */ => nameof(Show),
        1163020371 /* SHRE */ => nameof(ShowRelationships),
        1095062611 /* STEA */ => nameof(Steal),
        1313231955 /* STFN */ => nameof(StealFromNC),
        1179604051 /* STOF */ => nameof(StandOnFurniture),
        1464686419 /* SWMW */ => nameof(SwingMeleeWeapon),
        1414873428 /* TAUT */ => nameof(Taunt),
        1196706132 /* TITG */ => nameof(TimeToGo),
        1229017684 /* TRAI */ => nameof(Training),
        1312903764 /* TRAN */ => nameof(TrespassAgainstNC),
        1447121492 /* TRAV */ => nameof(Travel),
        1397051988 /* TRES */ => nameof(Trespass),
        1480938068 /* TREX */ => nameof(TrainingExit),
        1279610966 /* VPEL */ => nameof(VoicePowerEndLong),
        1397051478 /* VPES */ => nameof(VoicePowerEndShort),
        1280528470 /* VPSL */ => nameof(VoicePowerStartLong),
        1397968982 /* VPSS */ => nameof(VoicePowerStartShort),
        1380144215 /* WTCR */ => nameof(WereTransformCrime),
        1279609177 /* YIEL */ => nameof(Yield),
        1497713498 /* ZKEY */ => nameof(ZKeyObject),
        _ => subtype.ToString()
    };
}
