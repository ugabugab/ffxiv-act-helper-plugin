using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FFXIV_ACT_Helper_Plugin
{
    public class Buff
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string NameJa { get; set; }

        public BuffType Type { get; set; }

        public BuffTarget Target { get; set; }

        public int Duration { get; set; }

        public int DamageRate { get; set; }

        public int CriticalRate { get; set; }

        public int DirectHitRate { get; set; }

        public BuffGroup Group { get; set; }

        public Buff() {}

        public Buff(Buff source)
        {
            this.Id = source.Id;
            this.Name = source.Name;
            this.NameJa = source.NameJa;
            this.Type = source.Type;
            this.Target = source.Target;
            this.Duration = source.Duration;
            this.DamageRate = source.DamageRate;
            this.CriticalRate = source.CriticalRate;
            this.DirectHitRate = source.DirectHitRate;
            this.Group = source.Group;
        }

        public string[] NameList
        {
            get
            {
                return new string[] { Name, NameJa };
            }
        }

        public bool IsCard()
        {
            return Group == BuffGroup.CardForMeleeDPSOrTank || Group == BuffGroup.CardForRangedDPSOrHealer;
        }

        public bool IsConflict(Buff target)
        {
            return Id == target.Id
                || (this.IsCard() && target.IsCard());
        }
    }

    public enum BuffType
    {
        Buff = 1,
        Debuff
    }

    public enum BuffTarget
    {
        Solo = 1,
        Party
    }

    public enum BuffGroup
    {
        None = 0,
        Medicated = 1,
        CardForMeleeDPSOrTank = 2,
        CardForRangedDPSOrHealer = 3,
        Embolden = 4,
        Song = 5,
    }
}