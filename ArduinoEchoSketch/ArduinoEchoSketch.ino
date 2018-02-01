/*
 Name:		ArduinoEchoSketch.ino
 Created:	1/31/2018 11:27:21 PM
 Author:	DavidJones
*/

//// the setup function runs once when you press reset or power the board
//void setup() {
//
//}
//
//// the loop function runs over and over again until power down or reset
//void loop() {
//  
//}


#include <SoftwareSerial.h>

SoftwareSerial bt(2, 3); // RX, TX

char  thisByte;
bool useName = true;
enum Mode { ACK0, ACK1, ACK2, Running, GetString };
Mode mode = ACK1;
byte bytz;

void setup() {
	bt.begin(9600);
	Serial.begin(9600);
	Serial.print("Started");
	mode = ACK0;
	useName = false;
}

void loop() {
	bt.listen();
	thisByte = bt.read();
	if (useName)
	{
		// At start read messages as ending with last character #
		// Expect ACK0#
		// Send ACK1
		// Expect ACK1
		// Send Json config file
		// Expect ACK12#
		// Switch to single char mode
		String msg = "";
		if (thisByte != -1)
		{
			//Uses # terminated string
			msg += thisByte;

			while (thisByte != '#')
			{
				thisByte = bt.read();
				if (thisByte != -1)
					msg += thisByte;
			}
			//bt.write(thisByte);
			int j = 0;
			Serial.println(msg);
			//bt.print(msg);
			//if (mode == ACK0)
			//{
				if (msg == "ACK0#")
				{
					mode == ACK1;
					//Send Json file
					bt.print("ACK1#");
				}
			//}
			///else if (mode == ACK1)
			//{
				else if (msg == "ACK2#")
				{
					mode == ACK2;
					//Send Json file
					bt.print("[{ \"ElementConfig\": \"Config\" },{ \"MainMenu\": [ \"Setup BT\", \"Setup Serial\", \"Show sensor list\", \"Back to sensor list\", \" = Sensor\" ]}]#");
					bt.print("ACK3");
				}
			//}
			//else if (mode == ACK2)
			//{
				else if (msg == "ACK4#")
				{
					mode == Running;
					useName = false;
				}
			//}
		}
	}
	else
	{
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
				break;
			case '2':
				bt.print('3');
				break;
			case '4':
				bt.print('5');
				break;
			case '!':
				bytz = 200;
				bt.print('/');
				break;
			case '/':
				bt.print("Hello World~");
				mode == Running;
				break;
			default:
				bt.print(thisByte);
				break;
			}
		}
	}
	//  bt.write(thisByte);
	//
	//  bt.print(", dec: ");
	//  bt.print(thisByte);
	//  bt.print(", hex: ");
	//  bt.print(thisByte, HEX);
	//  bt.print(", oct: ");
	//  bt.print(thisByte, OCT);
	//  bt.print(", bin: ");
	//  bt.println(thisByte, BIN);
	//
	//  if(thisByte == 126) {
	//    thisByte = 32;
	//  }
	//  thisByte++;
}