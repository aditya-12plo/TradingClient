using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TradingClientApp.Model
{
	public class Security
	{
		public string Symbol { get; set; }
		public int SecurityId { get; set; }
		public string Exchange { get; set; }
		public string MaturityMonth { get; set; }
		public string MaturityDate { get; set; }
		public int ContractMultiplier { get; set; }
		public string Currency { get; set; }
	}
}
