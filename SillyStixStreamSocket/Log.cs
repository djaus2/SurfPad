using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SoxStreams
{
    public static class Util
    {
        public static void Log(string msg, NotifyType typ)
        {
            MainPage.Current.Log = msg;
            System.Diagnostics.Debug.WriteLine(msg, typ);
        }

        public static void Log(object omsg, NotifyType typ)
        {
            string msg = omsg.ToString();
            MainPage.Current.Log = msg;
            System.Diagnostics.Debug.WriteLine(msg, typ);
        }

        public static void Log(string msg)
        {
            Log(msg, NotifyType.StatusMessage);
        }

        public static bool IsNumeric_uint(string val)
        {
            uint value;
            bool res = uint.TryParse(val, out value);
            return res;
        }
    }
}
