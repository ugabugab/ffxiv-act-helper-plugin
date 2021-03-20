using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FFXIV_ACT_Helper_Plugin
{
    public class Job
    {
        public static string[] tankJobNames = { 
            JobName.prd, JobName.war, JobName.drk, JobName.gnb 
        };
        
        public static string[] healerJobNames = {
            JobName.whm, JobName.sch, JobName.ast
        };
        
        public static string[] meleeDPSJobNames = {
            JobName.drg, JobName.mnk, JobName.nin, JobName.sam
        };
        
        public static string[] physicalRangedDPSJobNames = {
            JobName.brd, JobName.mch, JobName.dnc
        };

        public static string[] magicalRangedDPSJobNames = {
            JobName.blm, JobName.smn, JobName.rdm, JobName.blu
        };

        public string Name { get; set; }

        public Role Role
        {
            get
            {
                if (tankJobNames.Contains(Name)) return Role.Tank;
                else if (healerJobNames.Contains(Name)) return Role.Healer;
                else if (meleeDPSJobNames.Contains(Name)) return Role.MeleeDPS;
                else if (physicalRangedDPSJobNames.Contains(Name)) return Role.PhysicalRangedDPS;
                else if (magicalRangedDPSJobNames.Contains(Name)) return Role.MagicalRangedDPS;
                else return Role.Unknown;
            }
        }

        public Job(string name)
        {
            this.Name = name;
        }

        public bool IsMeleeDPSOrTank()
        {
            return Role == Role.Tank || Role == Role.MeleeDPS;
        }

        public bool IsRangedDPSOrHealer()
        {
            return Role == Role.Healer || Role == Role.PhysicalRangedDPS || Role == Role.MagicalRangedDPS;
        }
    }

    public enum Role
    {
        Tank = 1,
        Healer,
        MeleeDPS,
        PhysicalRangedDPS,
        MagicalRangedDPS,
        Unknown = 99,
    }
}