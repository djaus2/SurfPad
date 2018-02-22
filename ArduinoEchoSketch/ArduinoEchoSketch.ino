/*
 Name:		ArduinoEchoSketch.ino
 Created:	1/31/2018 11:27:21 PM
 Author:	DavidJones
*/
#include <Keypad.h>
#include <Key.h>
#include <SoftwareSerial.h>
//#include <avr/pgmspoace>

void setupSocket();
void loopSocket();


const byte cFineStructureConstant = 137;
SoftwareSerial bt(2, 3); // RX, TX Pins

char  thisByte;
enum Mode { Disconnected, Connected, ACK0, ACK2,ACK4, Json1, Json2, Running };
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

void loopBT() {
	thisByte = bt.read();
	if (thisByte != -1)
	{
		Serial.print(thisByte);
		TerminalMode = BT;
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
				break;
			}
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
				bt.print('!');
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
		default:
			if (mode == Running)
			{
				//Do app functions here depending upon thisByte.
				//For now just echo it.
				DoApp(thisByte);
				bt.print(thisByte);
			}

		}
	}
}

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
				Serial.print('1');
				mode = ACK0;
			}
			break;
		case '2':
			if (mode == ACK0)
			{
				Serial.print('3');
				mode = ACK2;
			}
			break;
		case '4':
			if (mode == ACK2)
			{
				Serial.print('5');
				mode = ACK4;
			}
			break;
		case '!': //Get Exclamation mark as indicator of request for Json
			if (mode == ACK4)
			{
				Serial.print('/'); //Send back / meaning it will follow
				mode = Json1;
			}
			break;
		case '/':  //App sends this back
			if (mode == Json1)
			{
				Serial.print("{\"Config\":[ [ { \"iWidth\": 120 },{ \"iHeight\": 100 },{ \"iSpace\": 5 },{ \"iCornerRadius\": 10 },{ \"iRows\": 2 },{ \"iColumns\": 5 },{ \"sComPortId\": \"\\\\\\\\?\\\\USB#VID_26BA&PID_0003#5543830353935161A112#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"sFTDIComPortId\": \"\\\\\\\\?\\\\FTDIBUS#VID_0403+PID_6001+FTG71BUIA#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"iComportConnectDeviceNo\": -1 },{ \"iFTDIComportConnectDeviceNo\": 1 },{ \"sUseSerial\": \"Serial\" } ] ] }~");
				mode = Json2;
			}
			break;
		case '~':  //Then when it gets above then sends this back as confirmation
			if (mode == Json2)
			{
				Serial.print("{\"MainMenu\":[ [ \"Something else\", \"Unload\", \"Setup USB Serial\", \"Show full list\", \"The quick brown fox jumps over the lazy dog\" ],[ \"First\", \"Back\", \"Next\", \"Last\", \"Show All\" ] ] }~");
				mode = Running;
			}
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

void DoApp(char thisByte)
{

}

