/*
 Name:		ArduinoEchoSketch.ino
 Created:	1/31/2018 11:27:21 PM
 Author:	DavidJones
*/
#include <Keypad.h>
#include <Key.h>
#include <SoftwareSerial.h>
#include <SPI.h>
#include <Ethernet.h>
//#include <avr/pgmspoace>

void setupSocket();
void loopSocket();
void loopUSBSerial();
void loopBT();


const byte cFineStructureConstant = 137;
SoftwareSerial bt(2, 3); // RX, TX Pins

char  thisByte;
enum Mode { Disconnected, Connected, ACK0, ACK2, ACK4, Ready, Json1, Json2, Running };
Mode mode = Disconnected;
int a0, a1, a2, a3;

enum TerminalModes { none, BT, USBSerial,Socket };
static TerminalModes TerminalMode = none;

void setup() {
	//Set the terminal mode by grounding either A0, A1 or A2  (only one thereof):
	// = BT, USB, Socket mode respectively
	TerminalMode = none;
	pinMode(A0, INPUT);
	digitalWrite(A0, INPUT_PULLUP);
	pinMode(A1, INPUT);
	digitalWrite(A1, INPUT_PULLUP);
	pinMode(A2, INPUT);
	digitalWrite(A2, INPUT_PULLUP);
	a0 = digitalRead(A0);
	a1 = digitalRead(A1);
	a2 = digitalRead(A2);
	if (a0 == 0)
		TerminalMode = BT; 
	else if (a1 == 0)
		TerminalMode = USBSerial;
	else  if (a2 == 0)
		TerminalMode = Socket;

	//a3 = a0 + a1 + a2;

	//Start sync
	if (TerminalMode == BT)
	{
		mode = ACK0;
		bt.begin(9600);
		Serial.begin(9600);
		while (!bt)
		{
		}
		bt.print((char)cFineStructureConstant);
		mode = Connected;
	}
	else if (TerminalMode == USBSerial)
	{
		mode = ACK0;
		Serial.begin(9600);
		while (!Serial)
		{
		}
		Serial.print((char)cFineStructureConstant);
		mode = Connected;
	}
	else if (TerminalMode == Socket)
	{
		setupSocket();
		
	}
}

void loop() {
	if (TerminalMode == BT)
		loopBT();
	else if (TerminalMode == USBSerial)
		loopUSBSerial();
	else if (TerminalMode == Socket)
		loopSocket();
}



void DoApp(char thisByte)
{
	if (TerminalMode == BT)
		bt.print(thisByte);
	else if (TerminalMode == USBSerial)
		Serial.print(thisByte);
}

void DoAppEther(char thisByte, EthernetClient client)
{
	if(TerminalMode == Socket)
		client.write(thisByte);
}



