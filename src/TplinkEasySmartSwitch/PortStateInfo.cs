using System;

namespace TplinkEasySmartSwitch
{
	public class PortStateInfo
	{
		public byte PortNumber { get; set; }
		public bool IsEnabled { get; set; }
		public byte LinkStatus { get; set; }
		public long TxGoodPkt { get; set; }
		public long TxBadPkt { get; set; }
		public long RxGoodPkt { get; set; }
		public long RxBadPkt { get; set; }
		public string LinkStatusDisplayString { get { return LinkStatusToString(LinkStatus); } }

		public static string LinkStatusToString(byte value)
		{
			switch (value)
			{
				case 0:
					return "Link Down";
				case 2:
					return "10Half";
				case 5:
					return "100Full";
				case 6:
					return "1000Full";
				default:
					return value.ToString();
			}
		}
	}
}
