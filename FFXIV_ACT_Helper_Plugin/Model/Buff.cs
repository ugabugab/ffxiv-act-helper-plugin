﻿using System;
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

        public bool IsContinuous { get; set; }

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
    }
}