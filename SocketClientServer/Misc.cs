using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocketClientServer
{
    public enum NotifyType
    {
        StatusMessage,
        WarningMessage,
        ErrorMessage
    };

    public enum AppMode
    {
        None,NewListner,ListenerStarted, NewClient, ClientSocketConnected, ClientSocketReadyToSend
    }

    
}
