using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Linq;
using System.Text;
using HtmlAgilityPack;
using Jint;
using Jint.Native;
using Jint.Native.Array;


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
		/// Sets the Provided Ports Ingress and Egress Speeds to The values (the siwth does some rounding though) 0 means unlimited
		/// </summary>
		/// <param name="port">The port to which to aplly these settings</param>
		/// <param name="igrRate">Ingressrate in Kbps</param>
		/// <param name="egrRate">Egressrate in Kbps</param>
		public void SetPortSpeeds(int port, int igrRate, int egrRate){
			string html = _client.DownloadString("QosBandWidthControlRpm.htm");
			if (html.Contains("id=\"logon\""))
			{
				// not logged in!
				throw new Exception("login failed");
			}
			var setBWData = new NameValueCollection();
			
			setBWData.Add("igrRate", igrRate.ToString());
			setBWData.Add("egrRate", egrRate.ToString());
			setBWData.Add("sel_" + port, "1");
			setBWData.Add("applay", "Apply");

			try
			{
				byte[] resp = _client.UploadValues("qos_bandwidth_set.cgi", "POST", setBWData);
			}
			catch (WebException ex)
			{
				//Console.WriteLine(ex);
				// old TP-Link switches (eg.g. 1.0.2 Build 20160526 Rel.34615) reply with '401 Access Denied', but login works
				// ex.Status==WebExceptionStatus.ProtocolError
				// confirmed with Fiddler that this also happens in the Web-Interface -> despite the error, the switch returns html code
			}

		}

		public IReadOnlyList<PortStateInfo> GetPortStatistics()
		{
			string html = _client.DownloadString("PortStatisticsRpm.htm");
			if (html.Contains("id=\"logon\""))
			{
				// not logged in!
				throw new Exception("login failed");
			}

			HtmlDocument doc = new HtmlDocument();
			doc.LoadHtml(html);
			List<HtmlNode> scriptNodes = doc.DocumentNode.Descendants("script").ToList();
			if (!scriptNodes.Any())
			{
				throw new Exception("no script nodes found");
			}
			HtmlNode script = scriptNodes.First();
			string scriptText = script.InnerText;

			Engine scriptingEngine = new Engine();
			Engine result = scriptingEngine.Execute(scriptText);
			JsValue r1 = result.GetValue("max_port_num");
			JsValue r2 = result.GetValue("all_info");
			int max_port_num = (int)r1.AsNumber();
			long[] linkStatusArray = AsLongArray(r2.AsObject().GetProperty("link_status").Value);
			long[] stateArray = AsLongArray(r2.AsObject().GetProperty("state").Value);
			long[] packetCountArray = AsLongArray(r2.AsObject().GetProperty("pkts").Value);

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
