/*
 Name:		ArduinoEchoSketch.ino
 Created:	1/31/2018 11:27:21 PM
 Author:	DavidJones
*/
#include <SoftwareSerial.h>

SoftwareSerial bt(2, 3); // RX, TX Pins

char  thisByte;
enum Mode { ACK0, ACK1, ACK2,ACK4, Running, GetJson, GetString };
Mode mode = ACK0;

void setup() {
	bt.begin(9600);
	Serial.begin(9600);
	Serial.print("Started");
	mode = ACK0;
}

void loop() {
	bt.listen();
	thisByte = bt.read();
	// Read BT Serial char by char
	// At start read messages to sync
	// Expect '0'
	// Send '1' as ack
	// Expect '2'
	// Send '3' as ack
	// Expect '4'
	// Send '5 as ack
	// If char = '!' 
	//   Send back '/' as ack ready to send json
	//   Expect '/'
	//   Send first json string (Config)
	//   Expect '~'
	//   Send back single line json string (MainMenu)
	// Default: Just echo the character back
	if (thisByte != -1)
	{
		//Each char is interpretted as byte representing a keypress
		//The byte is the id of button pressed + ' ' (so are printable
		//bt.print(thisByte);
		//bt.print('#');
		switch (thisByte)
		{
		case '0':
			bt.print('1');
			mode = ACK1;
			break;
		case '2':
			bt.print('3');
			mode = ACK2;
			break;
		case '4':
			bt.print('5');
			mode = ACK4;
			break;
		case '!': //Get Exclamation mark as indicator of request for Json
			bt.print('/'); //Send back / meaning it will follow
			mode = GetJson;
			break;
		default:
			if (mode==Running)
				bt.print(thisByte);
			else if (mode = GetJson)
			{
				switch (thisByte)
				{
					case '/':  //App sends this back
						bt.print("{\"Config\":[ [ { \"iWidth\": 120 },{ \"iHeight\": 100 },{ \"iSpace\": 5 },{ \"iCornerRadius\": 10 },{ \"iRows\": 2 },{ \"iColumns\": 5 },{ \"sComPortId\": \"\\\\\\\\?\\\\USB#VID_26BA&PID_0003#5543830353935161A112#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"sFTDIComPortId\": \"\\\\\\\\?\\\\FTDIBUS#VID_0403+PID_6001+FTG71BUIA#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"iComportConnectDeviceNo\": -1 },{ \"iFTDIComportConnectDeviceNo\": 1 },{ \"sUseSerial\": \"BT\" } ] ] }~");
						break;
					case '~':  //Then when it gets above then sends this back as confirmation
							   //bt.print("Hello World~");
						bt.print("{\"MainMenu\":[ [ \"Setup BT Serial\", \"Load App Menu\", \"Setup USB Serial\", \"Show full list\", \"The quick brown fox jumps over the lazy dog\" ],[ \"First\", \"Back\", \"Next\", \"Last\", \"Show All\" ] ] }~");
						mode = Running;
						break;
					default:
						mode = Running;
				}
			}
			break;
		}
	}
}