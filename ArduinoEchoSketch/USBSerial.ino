/*
 Name:		ArduinoEchoSketch.ino
 Created:	1/31/2018 11:27:21 PM
 Author:	DavidJones
*/
//#include <Keypad.h>
//#include <Key.h>
//#include <SoftwareSerial.h>
#include <SPI.h>
//#include <Ethernet.h>
//#include <avr/pgmspoace>










void loopUSBSerial() {
	thisByte = Serial.peek();
	if (thisByte != -1)
	{
		thisByte = Serial.read();
		TerminalMode = USBSerial;
		////Debugging:
		////Serial.print(thisByte);
		////return;
		//Each char is interpretted as byte representing a keypress
		//The byte is the id of button pressed + ' ' (so are printable
		//Serial.print(thisByte);
		//Serial.print('#');
		switch (thisByte)
		{
		case '0':
			if (mode == Connected)
			{
				mode = ACK0;
				Serial.print('1');
			}
			break;
		case '2':
			if (mode == ACK0)
			{
				mode = ACK2;
				Serial.print('3');
			}
			break;
		case '4':
			if (mode == ACK2)
			{
				mode = ACK4;
				Serial.print('5');
			}
			break;
		case '!': //Get Exclamation mark as indicator of request for Json
			if (mode == ACK4)
			{
				mode = Ready;
				Serial.print('/'); //Send back / meaning it will follow
			}
			break;
		case '/':  //App sends this back
			if (mode == Ready)
			{
				mode = Json1;
				Serial.print(F("{\"Config\":[ [ { \"iWidth\": 120 },{ \"iHeight\": 100 },{ \"iSpace\": 5 },{ \"iCornerRadius\": 10 },{ \"iRows\": 2 },{ \"iColumns\": 5 },{ \"sComPortId\": \"\\\\\\\\?\\\\USB#VID_26BA&PID_0003#5543830353935161A112#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"sFTDIComPortId\": \"\\\\\\\\?\\\\FTDIBUS#VID_0403+PID_6001+FTG71BUIA#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"iComportConnectDeviceNo\": -1 },{ \"iFTDIComportConnectDeviceNo\": 1 },{ \"sUseSerial\": \"Serial\" } ] ] }~"));
			}
			break;
		case '~':  //Then when it gets above then sends this back as confirmation
			if (mode == Json1)
			{
				mode = Json2;
				Serial.print(F("{\"MainMenu\":[ [ \"Something else\", \"Unload\", \"Setup USB Serial\", \"Show full list\", \"The quick brown fox jumps over the lazy dog\" ],[ \"First\", \"Back\", \"Next\", \"Last\", \"Show All\" ] ] }~"));
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
				Serial.print(thisByte);
			}
		}
	}
}



