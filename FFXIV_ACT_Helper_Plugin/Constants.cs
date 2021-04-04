using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Advanced_Combat_Tracker;

namespace FFXIV_ACT_Helper_Plugin
{
    public static class JobName
    {
        public static string Pld = "Pld";
        public static string War = "War";
        public static string Drk = "Drk";
        public static string Gnb = "Gnb";
        public static string Whm = "Whm";
        public static string Sch = "Sch";
        public static string Ast = "Ast";
        public static string Mnk = "Mnk";
        public static string Drg = "Drg";
        public static string Nin = "Nin";
        public static string Sam = "Sam";
        public static string Brd = "Brd";
        public static string Mch = "Mch";
        public static string Dnc = "Dnc";
        public static string Blm = "Blm";
        public static string Smn = "Smn";
        public static string Rdm = "Rdm";
        public static string Blu = "Blu";
        // TODO: Class support
    }

    public static class DamageTypeData
    {
        public static string OutgoingAutoAttack = "Auto-Attack (Out)";
        public static string OutgoingSkillAbility = "Skill/Ability (Out)";
        public static string OutgoingSilumatedDots = "Simulated DoTs (Out)";
        public static string OutgoingDamage = CombatantData.DamageTypeDataOutgoingDamage; //"Outgoing Damage";
        public static string OutgoingHealing = "Healed (Out)";
        public static string OutgoingSilumatedHots = "Simulated HoTs (Out)";
        public static string OutgoingPowerDrain = "Power Drain (Out)";
        public static string OutgoingPowerReplenish = "Power Replenish (Out)";
        public static string OutgoingCureDispel = "Cure/Dispel (Out)";
        public static string OutgoingBuffDebuff = "Buff/Debuff (Out)";
        public static string OutgoingThreat = "Threat (Out)";
        public static string OutgoingAll = "All Outgoing (Ref)";
        public static string IncomingDamage = CombatantData.DamageTypeDataIncomingDamage; //"Incoming Damage";
        public static string IncomingSilumatedDots = "Simulated DoTs (Inc)";
        public static string IncomingHealing = "Healed (Inc)";
        public static string IncomingSilumatedHots = "Simulated HoTs (Inc)";
        public static string IncomingPowerDrain = "Power Drain (Inc)";
        public static string IncomingPowerReplenish = "Power Replenish (Inc)";
        public static string IncomingCureDispel = "Cure/Dispel (Inc)";
        public static string IncomingBuffDebuff = "Buff/Debuff (Inc)";
        public static string IncomingThreat = "Threat (Inc)";
        public static string IncomingAll = "All Incoming (Ref)";
    }

    public static class AttackType
    {
        public static string All = "All";
        public static string WalkingDead = "Walking Dead";
    }

    public static class SwingType
    {
        public static int Attack = 1;
        public static int DamageSkill = 2;
        public static int HealSkill = 10;
        public static int Hot = 11;
        public static int Dot = 20;
        public static int Buff = 21;
    }

    public static class SwingTag
    {
        public static string Potency = "Potency";
        public static string Job = "Job";
        public static string ActorID = "ActorID";
        public static string TargetID = "TargetID";
        public static string SkillID = "SkillID";
        public static string BuffID = "BuffID";
        public static string BuffDuration = "BuffDuration";
        public static string BuffByte1 = "BuffByte1";
        public static string BuffByte2 = "BuffByte2";
        public static string BuffByte3 = "BuffByte3";
        public static string DirectHit = "DirectHit";
    }
}
