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
				logon.SetField(new StringField(553, "FIXAGUS")); //username
				logon.SetField(new StringField(554, "QWER1234")); //username

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
				securityDefinition.SecurityReqID = new SecurityReqID("1");
				securityDefinition.SecurityRequestType = new SecurityRequestType(SecurityListRequestType.TRADINGSESSIONID);
				Session.SendToTarget(securityDefinition, _currentSessionId);
				progressHandler("Sent Security Definition Request");
			});
		}

		public Task SendMarketDataRequest(Action<string> progressHandler)
		{
			return Task.Run(() =>
			{
				//Create object of Security Definition
				QuickFix.FIX42.MarketDataRequest securityDefinition = new QuickFix.FIX42.MarketDataRequest
				{
					MDReqID = new MDReqID(Guid.NewGuid().ToString()),
					SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES),
					MarketDepth = new MarketDepth(1)
				};

				var noMDEntryTypes = new QuickFix.FIX42.MarketDataRequest.NoMDEntryTypesGroup();
				var mdEntryType_bid = new MDEntryType(MDEntryType.BID);
				noMDEntryTypes.Set(mdEntryType_bid);
				securityDefinition.AddGroup(noMDEntryTypes);
				noMDEntryTypes.Set(new MDEntryType(MDEntryType.OFFER)); securityDefinition.AddGroup(noMDEntryTypes);
				noMDEntryTypes.Set(new MDEntryType(MDEntryType.TRADE)); securityDefinition.AddGroup(noMDEntryTypes);
				noMDEntryTypes.Set(new MDEntryType(MDEntryType.OPENING_PRICE)); securityDefinition.AddGroup(noMDEntryTypes);
				noMDEntryTypes.Set(new MDEntryType(MDEntryType.SETTLEMENT_PRICE)); securityDefinition.AddGroup(noMDEntryTypes);
				noMDEntryTypes.Set(new MDEntryType(MDEntryType.TRADING_SESSION_HIGH_PRICE)); securityDefinition.AddGroup(noMDEntryTypes);
				noMDEntryTypes.Set(new MDEntryType(MDEntryType.TRADING_SESSION_LOW_PRICE)); securityDefinition.AddGroup(noMDEntryTypes);
				noMDEntryTypes.Set(new MDEntryType(MDEntryType.TRADE_VOLUME)); securityDefinition.AddGroup(noMDEntryTypes);
				noMDEntryTypes.Set(new MDEntryType(MDEntryType.OPEN_INTEREST)); securityDefinition.AddGroup(noMDEntryTypes);

				securityDefinition.NoRelatedSym = new NoRelatedSym(1);

				var relatedSymbol = new QuickFix.FIX42.MarketDataRequest.NoRelatedSymGroup();
				relatedSymbol.Set(new Symbol("IF1509"));
				relatedSymbol.Set(new SecurityExchange("CFFEX"));
				securityDefinition.AddGroup(relatedSymbol);
				Session.SendToTarget(securityDefinition, _currentSessionId);
				progressHandler("Sent MarketData Request");
			});
		}

		public void OnMessage(QuickFix.FIX42.MarketDataRequestReject marketDataSnapshot, SessionID session)
		{
			Debug.WriteLine(marketDataSnapshot.ToString());
		}

		public void OnMessage(QuickFix.FIX42.MarketDataSnapshotFullRefresh marketDataSnapshot, SessionID session)
		{
			Debug.WriteLine(marketDataSnapshot.ToString());
		}
		public void OnMessage(QuickFix.FIX42.MarketDataIncrementalRefresh marketDataSnapshot, SessionID session)
		{

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
