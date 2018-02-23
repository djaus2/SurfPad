/*
 Name:		ArduinoEchoSketch.ino
 Created:	1/31/2018 11:27:21 PM
 Author:	DavidJones
*/
#include <SPI.h>
#include <SoftwareSerial.h>

void loopBT() {
	thisByte = bt.read();
	if (thisByte != -1)
	{
		////Debugging:
		//Serial.print(thisByte);
		//return;
		//Each char is interpretted as byte representing a keypress
		//The byte is the id of button pressed + ' ' (so are printable
		//bt.print(thisByte);
		//bt.print('#');
		switch (thisByte)
		{
		case '0':
			if (mode == Connected)
			{
				bt.print('1');
				mode = ACK0;
			}
			break;
		case '2':
			if (mode == ACK0)
			{
				bt.print('3');
				mode = ACK2;
			}
			break;
		case '4':
			if (mode == ACK2)
			{
				bt.print('5');
				mode = ACK4;
			}
			break;
		case '!':
			if (mode == ACK4)
			{
				bt.print('/');
				mode = Json1;
			}
			break;
		case '/':  //App sends this back
			if (mode = Json1)
			{
				bt.print(F("{\"Config\":[ [ { \"iWidth\": 120 },{ \"iHeight\": 100 },{ \"iSpace\": 5 },{ \"iCornerRadius\": 10 },{ \"iRows\": 2 },{ \"iColumns\": 5 },{ \"sComPortId\": \"\\\\\\\\?\\\\USB#VID_26BA&PID_0003#5543830353935161A112#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"sFTDIComPortId\": \"\\\\\\\\?\\\\FTDIBUS#VID_0403+PID_6001+FTG71BUIA#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"iComportConnectDeviceNo\": -1 },{ \"iFTDIComportConnectDeviceNo\": 1 },{ \"sUseSerial\": \"BT\" } ] ] }~"));
				mode = Json2;
			}
			break;
		 case '~':  //Then when it gets above then sends this back as confirmation
			if (mode = Json2)
			{
				bt.print(F("{\"MainMenu\":[ [ \"Set up BT Serial\", \"Unload\", \"Something else\", \"Show full list\", \"The quick brown fox jumps over the lazy dog\" ],[ \"First\", \"Back\", \"Next\", \"Last\", \"Show All\" ] ] }~"));
				mode = Running;
			}
			break;
		case '^':  //Retart
			mode = Connected;
			break;
		default:
			if (mode == Running)
			{
				//Do app functions here depending upon thisByte.
				//For now just echo it.
				DoApp(thisByte);
			}

		}
	}
}




