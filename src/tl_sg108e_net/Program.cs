﻿using System;
using System.Collections.Generic;
using TplinkEasySmartSwitch;


namespace tl_sg108e_net
{
	class Program
	{
		static void Main(string[] args)
		{
			string dnsNameOrIp = "tl-sg108e";
			EasySmartSwitch easySmartSwitch = new EasySmartSwitch(dnsNameOrIp);
			easySmartSwitch.Logon("admin", "admin");
			//easySmartSwitch.ClearPortStatistics();
			IReadOnlyList<PortStateInfo> ports= easySmartSwitch.GetPortStatistics();

			Console.WriteLine("Port#	Status	Link Status	TxGoodPkt	TxBadPkt	RxGoodPkt	RxBadPkt");
			foreach (PortStateInfo psi in ports)
			{
				Console.WriteLine($"{psi.PortNumber}\t{(psi.IsEnabled ? "Enabled" : "Disabled")}\t{psi.LinkStatusDisplayString}\t{psi.TxGoodPkt}\t{psi.TxBadPkt}\t{psi.RxGoodPkt}\t{psi.RxBadPkt}");
			}
		}


	}

	
}
