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
using TradingClientApp.Model;

namespace TradingClientApp
{
    public class CTSFixClient : MessageCracker, QuickFix.IApplication
    {

        IInitiator initiator;
        SessionID _currentSessionId;
        SessionID _markedDataSession;

        public List<Security> Securities { get; private set; }

        public event Action<string> OnProgress;
        /// <summary>
        /// InitializSession
        /// </summary>
        public Task Initialize(Action<string> progress, CancellationToken cancellationToken)
        {
            return Task.Run(() =>

            {
                var settings = new SessionSettings("cts.cfg");

                IMessageStoreFactory messageFactory = new FileStoreFactory(settings);

                ILogFactory logFactory = new FileLogFactory(settings);

                initiator = new SocketInitiator(this, messageFactory, settings, logFactory);

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
            //All Incoming Applications message trigger here
            Crack(message, sessionID);
        }

        public void OnCreate(SessionID sessionID)
        {
            OnProgress("Session created");
        }

        //if logon is successful
        public void OnLogon(SessionID sessionID)
        {
            _currentSessionId = sessionID;
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
            if (message is QuickFix.FIX42.Logon)
            {
                var logon = message as QuickFix.FIX42.Logon;
                logon.SetField(new StringField(553, "FIXIT")); //username
                logon.SetField(new StringField(554, "Q")); //username


                ///   logon.SetField(new StringField(553, "FIXIT")); //username IT
                ///    logon.SetField(new StringField(554, "Q")); //pass IT

                /// logon.SetField(new StringField(553, "FIXAGUS")); //username agus
                /// logon.SetField(new StringField(554, "QWER1234")); //pass agus

            }
        }

        public void ToApp(Message message, SessionID sessionId)
        {
            //  throw new NotImplementedException();
        }

        //Send Security Definitions Request
        public Task SendSecurityDefinitionRequest(Action<string> progressHandler)
        {
            return Task.Run(() =>
            {
                //Create object of Security Definition
                QuickFix.FIX42.SecurityDefinitionRequest securityDefinition = new QuickFix.FIX42.SecurityDefinitionRequest();
                securityDefinition.SecurityReqID = new SecurityReqID(Guid.NewGuid().ToString());
                securityDefinition.SecurityRequestType = new SecurityRequestType(3);
                // securityDefinition.Symbol = new Symbol("N/A");
                Session.SendToTarget(securityDefinition, _currentSessionId);
                progressHandler("Sent Security Definition Request");
            });
        }

        ///Response to Security Defintion will be triggered here
        public void OnMessage(QuickFix.FIX42.SecurityDefinition securityDefinition, SessionID session)
        {
            //Store Security Definitions
            Securities = new List<Security>();

            //Number of securities in one message
            int numOfSecurities = securityDefinition.TotalNumSecurities.getValue();
            var group = new QuickFix.FIX42.SecurityDefinition();


        }
    }
}
