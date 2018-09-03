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

		public event Action<Security> OnSecurity;
		public event Action<MarketPrice> OnMarketPrice;

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

				//logon.SetField(new StringField(553, "FIXIT")); //username
				//logon.SetField(new StringField(554, "Q")); //username


				///   logon.SetField(new StringField(553, "FIXIT")); //username IT
				///    logon.SetField(new StringField(554, "Q")); //pass IT

				logon.SetField(new StringField(553, "FIXAGUS")); //username agus
				logon.SetField(new StringField(554, "QWER1234")); //pass agus

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
				securityDefinition.SecurityExchange = new SecurityExchange("ICDX");
				securityDefinition.SecurityRequestType = new SecurityRequestType(SecurityListRequestType.TRADINGSESSIONID);
				Session.SendToTarget(securityDefinition, _currentSessionId);
				progressHandler("Sent Security Definition Request");
			});
		}

		public Task SendMarketDataRequest(string symbol, string exchange, Action<string> progressHandler)
		{
			return Task.Run(() =>
			{
				//Create object of Security Definition
				QuickFix.FIX42.MarketDataRequest securityDefinition = new QuickFix.FIX42.MarketDataRequest
				{
					MDReqID = new MDReqID(Guid.NewGuid().ToString()),
					SubscriptionRequestType = new SubscriptionRequestType(SubscriptionRequestType.SNAPSHOT_PLUS_UPDATES),
					MarketDepth = new MarketDepth(1),
					MDUpdateType = new MDUpdateType(0)

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
				relatedSymbol.Set(new Symbol(symbol));
				relatedSymbol.Set(new SecurityExchange(exchange));
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
			MarketPrice marketPrice = new MarketPrice();
			marketPrice.Symbol = marketDataSnapshot.Symbol.getValue();
			var nomdentries = marketDataSnapshot.NoMDEntries;
			// message.GetGroup(1, noMdEntries);
			var grp = new QuickFix.FIX42.MarketDataSnapshotFullRefresh.NoMDEntriesGroup();

			for (int i = 1; i <= nomdentries.getValue(); i++)
			{
				grp = (QuickFix.FIX42.MarketDataSnapshotFullRefresh.NoMDEntriesGroup)marketDataSnapshot.GetGroup(i, grp);

			//	var grp = marketDataSnapshot.GetGroup(i, new QuickFix.FIX42.MarketDataSnapshotFullRefresh.NoMDEntriesGroup()) as QuickFix.FIX42.MarketDataSnapshotFullRefresh.NoMDEntriesGroup;
				MDEntryType priceType = grp.Get(new MDEntryType());
				MDEntryPx mdEntryPx = grp.Get(new MDEntryPx());
				if (priceType.getValue() == MDEntryType.BID)
					marketPrice.Bid = mdEntryPx.getValue();
				else if (priceType.getValue() == MDEntryType.OFFER)
					marketPrice.Offer = mdEntryPx.getValue();
				else if (priceType.getValue() == MDEntryType.TRADE)
					marketPrice.TradedPrice = mdEntryPx.getValue();
				else if (priceType.getValue() == MDEntryType.TRADING_SESSION_LOW_PRICE)
					marketPrice.LowPx = mdEntryPx.getValue();
				else if (priceType.getValue() == MDEntryType.TRADING_SESSION_HIGH_PRICE)
					marketPrice.HighPx = mdEntryPx.getValue();
			}

			if (OnMarketPrice != null)
				OnMarketPrice(marketPrice);
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
			var relatedsymbol = securityDefinition.SecurityID.getValue();

			var group = new QuickFix.FIX42.SecurityDefinition();
			if (Securities == null)
				Securities = new List<Security>();

			var sec = new Security
			{
				Symbol = securityDefinition.Symbol.getValue(),
				ContractMultiplier = securityDefinition.ContractMultiplier.getValue(),
				Currency = securityDefinition.Currency.getValue(),
				Exchange = securityDefinition.SecurityExchange.getValue(),
				MaturityDay = securityDefinition.MaturityDay.getValue(),
				MaturityMonthYear = securityDefinition.MaturityMonthYear.getValue()
			};

			Securities.Add(sec);

			if (OnSecurity != null)
				OnSecurity(sec);
		}
	}
}
