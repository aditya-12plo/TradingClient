using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using QuickFix;
using QuickFix.Fields;
using QuickFix.Transport;

namespace TradingClientApp
{
    public class CTSFixClient : QuickFix.IApplication
    {

        IInitiator initiator;

        public event Action<string> OnProgress;
        /// <summary>
        /// InitializSession
        /// </summary>
        public Task Initialize(Action<string> progress,CancellationToken cancellationToken)
        {
            return Task.Run(() =>

            {
                var settings = new SessionSettings("cts.cfg");

                IMessageStoreFactory messageFactory = new FileStoreFactory(settings);

                ILogFactory logFactory = new FileLogFactory(settings);

                initiator = new SocketInitiator(this, messageFactory, settings);

                progress("Initialization done");

            }, cancellationToken);
        }

        public void Connect()
        {
            initiator.Start();
        }


        public void Disconnect()
        {
            initiator.Stop();
        }

        public void FromAdmin(Message message, SessionID sessionID)
        {
            if (message is QuickFix.FIX42.Heartbeat)
            {
                OnProgress("Heartbeat");
            }
        }

        public void FromApp(Message message, SessionID sessionID)
        {
            throw new NotImplementedException();
        }

        public void OnCreate(SessionID sessionID)
        {
            OnProgress("Session created");
        }

        //if logon is successful
        public void OnLogon(SessionID sessionID)
        {
            OnProgress("Connection is successful");
           // throw new NotImplementedException();
        }

        //if logon is failed
        public void OnLogout(SessionID sessionID)
        {
            OnProgress("Connection is loggedout");
        }

        //hook admin messages before calling server
        public void ToAdmin(Message message, SessionID sessionID)
        {
            if(message is QuickFix.FIX42.Logon)
            {
                var logon = message as QuickFix.FIX42.Logon;
                logon.SetField(new StringField(553, "FIXAGUS")); //username
                logon.SetField(new StringField(554, "QWER1234")); //username

            }
        }

        public void ToApp(Message message, SessionID sessionId)
        {
          //  throw new NotImplementedException();
        }
    }
}
