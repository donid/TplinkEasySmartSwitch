using System.Diagnostics;

namespace TplinkEasySmartSwitch
{
	[DebuggerDisplay("{" + nameof(GetDebuggerDisplay) + "(),nq}")]
	public class PortSpeedInfo
	{
		public byte PortNumber { get; set; }
		public long IngressRateKbps { get; set; }
		public long EgressRateKbps { get; set; }

		private string GetDebuggerDisplay()
		{
			return $"Port: {PortNumber} IngressKbps: {IngressRateKbps} EgressKbps: {EgressRateKbps}";
		}
	}
}
