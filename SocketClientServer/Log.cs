using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Networking.Sockets;

namespace SocketClientServer
{
    public class Sox : INotifyPropertyChanged
    {
        public static CancellationToken CancelToken;
        private static  CancellationTokenSource CancelTokenSource = null;

        public static void StartToken()
        {
            CancelActions();
            CancelTokenSource = new CancellationTokenSource();
            CancelToken = CancelTokenSource.Token;
        }

        public static void CancelActions()
        {
            if (CancelTokenSource != null)
            {
                CancelTokenSource.Cancel();
            }
            CancelTokenSource = null;
        }


        public static /*async*/ void WaitForToken()
        {
            if (CancelTokenSource != null)
            {
                ManualResetEventSlim mrs = new ManualResetEventSlim();
                mrs.Wait(CancelToken);
            }
        }

        
        public static AppMode AppMode { get; set; }  = AppMode.None;
        public static void Start()
        {
            var log = new Sox();
            AppMode = AppMode.None;
        }

        public static Sox Instance = null;
        public Sox()
        {
            Instance = this;
            IsPairing = false;
        }

        public string _logMsg="";
        public string LogMsg
        {
            get
            {
                return _logMsg;
            }
            set
            {
                if (_logMsg != value)
                {
                    _logMsg = value;
                    RaisePropertyChanged("_logMsg");
                }
            }
        }

        public bool IsPairing { get; set; } = false;
        public string Passkey { get; set; } = "";

        public event PropertyChangedEventHandler PropertyChanged;
        protected void RaisePropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }

        public  void Log(string msg, NotifyType typ)
        {
            LogMsg = msg;
            System.Diagnostics.Debug.WriteLine(msg, typ);
        }

        public  void Log(object omsg, NotifyType typ)
        {
            string msg = omsg.ToString();
            LogMsg = msg;
            System.Diagnostics.Debug.WriteLine(msg, typ);
        }

        public  void Log(string msg)
        {
            Log(msg, NotifyType.StatusMessage);
        }

        public  static bool IsNumeric_uint(string val)
        {
            uint value;
            bool res = uint.TryParse(val, out value);
            return res;
        }
    }
}
