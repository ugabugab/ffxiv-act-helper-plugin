using Advanced_Combat_Tracker;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;

namespace FFXIV_ACT_Helper_Plugin
{
    public static class DnumExtension
    {
        public static void SetNum(this Dnum dnum, long num)
        {
            try
            {
                var fieldInfo = dnum.GetType().GetField("num", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (fieldInfo != null)
                {
                    fieldInfo.SetValue(dnum, num);
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }
        }
    }
}
