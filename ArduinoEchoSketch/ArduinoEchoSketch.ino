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

void setup() {
	bt.begin(9600);
	Serial.begin(9600);
	Serial.print("AAAA");
}

void loop() {
	bt.listen();
	thisByte = bt.read();
	String msg = "";
	if (thisByte != -1)
	{
		
		msg += thisByte;

		while (thisByte != '#')
		{
			thisByte = bt.read();
			if (thisByte != -1)
				msg += thisByte;
		}
		//bt.write(thisByte);
		Serial.println(msg);
		bt.print(msg);
		int i = 0;
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