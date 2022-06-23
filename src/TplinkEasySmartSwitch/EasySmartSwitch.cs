using HtmlAgilityPack;

using Jint;
using Jint.Native;
using Jint.Native.Array;
using Jint.Native.Object;

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;


namespace TplinkEasySmartSwitch
{
	public class EasySmartSwitch
	{
		private WebClient _client;
		private string _dnsNameOrIp;

		public EasySmartSwitch(string dnsNameOrIp)
		{
			_dnsNameOrIp = dnsNameOrIp;
			string baseUri = string.Format(@"http://{0}/", _dnsNameOrIp);
			_client = new WebClient();
			_client.BaseAddress = baseUri;
		}

		public void Logon(string username, string password)
		{
			var loginData = new NameValueCollection();
			loginData.Add("username", username);
			loginData.Add("password", password);
			loginData.Add("logon", "Login");
			//client.Headers.Add(HttpRequestHeader.Referer, baseUri+"/");
			//client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
			try
			{
				byte[] resp = _client.UploadValues("logon.cgi", "POST", loginData);
			}
			catch (WebException /*ex*/)
			{
				// old TP-Link switches (eg.g. 1.0.2 Build 20160526 Rel.34615) reply with '401 Access Denied', but login works
				// ex.Status==WebExceptionStatus.ProtocolError
				// confirmed with Fiddler that this also happens in the Web-Interface -> despite the error, the switch returns html code
			}
		}


		public void ClearPortStatistics()
		{
			// clears packet statistics and return the web-page with the new values
			string htmlClearStat = _client.DownloadString("port_statistics_set.cgi?clear=Clear");
		}

		/// <summary>
		/// Management Web-Interface: QoS / Bandwidth Control
		/// Sets the provided ports Ingress and Egress speeds to the values (the switch does some rounding though)
		/// 0 means unlimited - 1000000 is the maximum value
		/// </summary>
		/// <param name="port">The port to which to apply these settings</param>
		/// <param name="igrRate">Ingressrate in Kbps</param>
		/// <param name="egrRate">Egressrate in Kbps</param>
		public void SetPortSpeeds(int port, int igrRate, int egrRate)
		{
			if (port < 1 || port > 24)
			{
				throw new ArgumentException($"{nameof(port)} must be between 1 and 24.", nameof(port));
			}

			if (igrRate < 0 || igrRate > 1000000)
			{
				throw new ArgumentException($"{nameof(igrRate)} must be between 0 and 1000000.", nameof(igrRate));
			}

			if (egrRate < 0 || egrRate > 1000000)
			{
				throw new ArgumentException($"{nameof(egrRate)} must be between 0 and 1000000.", nameof(egrRate));
			}

			string html = _client.DownloadString("QosBandWidthControlRpm.htm");
			CheckLogin(html);

			var setBWData = new NameValueCollection();
			setBWData.Add("igrRate", igrRate.ToString());
			setBWData.Add("egrRate", egrRate.ToString());
			setBWData.Add("sel_" + port, "1");
			setBWData.Add("applay", "Apply");

			try
			{
				byte[] resp = _client.UploadValues("qos_bandwidth_set.cgi", "POST", setBWData);
			}
			catch (WebException /*ex*/)
			{
				//Console.WriteLine(ex);
				// old TP-Link switches (eg.g. 1.0.2 Build 20160526 Rel.34615) reply with '401 Access Denied', but login works
				// ex.Status==WebExceptionStatus.ProtocolError
				// confirmed with Fiddler that this also happens in the Web-Interface -> despite the error, the switch returns html code
			}

		}

		public IReadOnlyList<PortSpeedInfo> GetPortSpeeds()
		{
			string html = _client.DownloadString("QosBandWidthControlRpm.htm");
			CheckLogin(html);

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);
			List<HtmlNode> scriptNodes = doc.DocumentNode.Descendants("script").ToList();
			CheckScriptNodes(scriptNodes);
			HtmlNode script = scriptNodes.First();
			string scriptText = script.InnerText;

			Engine scriptingEngine = new Engine();
			Engine result = scriptingEngine.Execute(scriptText);
			JsValue r1 = result.GetValue("portNumber");
			JsValue r2 = result.GetValue("bcInfo");
			int max_port_num = (int)r1.AsNumber();
			long[] speedRateArray = AsLongArray(r2);

			List<PortSpeedInfo> ports = new List<PortSpeedInfo>();

			for (int i = 0; i < max_port_num; i++)
			{
				var psi = new PortSpeedInfo()
				{
					PortNumber = (byte)(i + 1),
					IngressRateKbps = speedRateArray[i * 3 + 0],
					EgressRateKbps = speedRateArray[i * 3 + 1],
				};
				ports.Add(psi);
			}
			return ports;
		}

		/// <summary>
		/// Management Web-Interface: Monitoring / Port Statistics
		/// </summary>
		/// <returns></returns>
		public IReadOnlyList<PortStateInfo> GetPortStatistics()
		{
			string html = _client.DownloadString("PortStatisticsRpm.htm");
			CheckLogin(html);

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);
			List<HtmlNode> scriptNodes = doc.DocumentNode.Descendants("script").ToList();
			CheckScriptNodes(scriptNodes);
			HtmlNode script = scriptNodes.First();
			string scriptText = script.InnerText;

			Engine scriptingEngine = new Engine();
			Engine result = scriptingEngine.Execute(scriptText);
			JsValue r1 = result.GetValue("max_port_num");
			int max_port_num = (int)r1.AsNumber();
			JsValue r2 = result.GetValue("all_info");
			ObjectInstance allInfoObj = r2.AsObject();
			long[] linkStatusArray = AsLongArray(allInfoObj.GetProperty("link_status").Value);
			long[] stateArray = AsLongArray(allInfoObj.GetProperty("state").Value);
			long[] packetCountArray = AsLongArray(allInfoObj.GetProperty("pkts").Value);

			List<PortStateInfo> ports = new List<PortStateInfo>();

			for (int i = 0; i < max_port_num; i++)
			{
				var psi = new PortStateInfo()
				{
					PortNumber = (byte)(i + 1),
					IsEnabled = stateArray[i] == 1,
					LinkStatus = (byte)linkStatusArray[i],
					TxGoodPkt = packetCountArray[i * 4 + 0],
					TxBadPkt = packetCountArray[i * 4 + 1],
					RxGoodPkt = packetCountArray[i * 4 + 2],
					RxBadPkt = packetCountArray[i * 4 + 3],
				};
				ports.Add(psi);
			}
			return ports;
		}

		private static void CheckScriptNodes(List<HtmlNode> scriptNodes)
		{
			if (!scriptNodes.Any())
			{
				throw new InvalidOperationException("no script nodes found");
			}
		}

		private static void CheckLogin(string html)
		{
			if (html.Contains("id=\"logon\""))
			{
				// not logged in!
				throw new InvalidOperationException("login failed");
			}
		}

		private static long[] AsLongArray(JsValue value)
		{
			ArrayInstance jarr = value.AsArray();
			long[] result = new long[jarr.GetLength()];
			for (int i = 0; i < result.Length; i++)
			{
				result[i] = (long)jarr.GetProperty(i.ToString()).Value.AsNumber();
			}
			return result;
		}
	}
}
