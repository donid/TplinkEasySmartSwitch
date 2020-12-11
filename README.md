# TplinkEasySmartSwitch
.Net Standard library to retrieve statistics from a TP-Link Easy Smart network switch

This library allows to retrieve (and reset) packet statistics from the Gigabit Switch TL-SG108E by some sort of Web-Scraping. This can be used for automatically monitoring if bad packets occur in your Ethernet network.

**NEW:** QoS / Bandwith Control => GetPortSpeeds() SetPortSpeeds(IngressRate, EgressRate) (thanks to @EXTREMEGABEL)

It might also work with the following models:

- TL-SG105E
- TL-SG108PE
- TL-SG1016DE
- TL-SG1024DE


#### Demo Sample output

    Port#	Status	Link Status	TxGoodPkt	TxBadPkt	RxGoodPkt	RxBadPkt
    1	Enabled	Link Down	    11070981	0	        68130043	0
    2	Enabled	Link Down	    0	        0	        0	        0
    3	Enabled	1000Full	    724438820	0	        2992039137	4
    4	Enabled	Link Down	    1429570820	0	        208170962	0
    5	Enabled	Link Down	    3566691176	0	        1834219074	0
    6	Enabled	1000Full	    2406747707	0	        2885793038	0
    7	Enabled	Link Down	    19260977	0	        49242424	0
    8	Enabled	1000Full	    164910192	0	        267304382	77


#### Other Projects

There is a more comprehensive project on GitHub which uses the native protocol of the switches, but it is written in Python:
https://github.com/pklaus/smrt


![.NET Core](https://github.com/donid/TplinkEasySmartSwitch/workflows/.NET%20Core/badge.svg)
