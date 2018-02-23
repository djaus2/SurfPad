
/*
Chat  Server

A simple server that distributes any incoming messages to all
connected clients.  To use telnet to  your device's IP address and type.
You can see the client's input in the serial monitor as well.
Using an Arduino Wiznet Ethernet shield.

Circuit:
* Ethernet shield attached to pins 10, 11, 12, 13
* Analog inputs attached to pins A0 through A5 (optional)

created 18 Dec 2009
by David A. Mellis
modified 9 Apr 2012
by Tom Igoe

*/

#include <SPI.h>
#include <Ethernet.h>

#define PORT 1234


void DoAppEther(char ch);




// Enter a MAC address and IP address for your controller below.
// The IP address will be dependent on your local network.
// gateway and subnet are optional:
byte mac[] = {
	0xDE, 0xAD, 0xBE, 0xEF, 0xFE, 0xED
};
IPAddress ip(192, 168, 0, 137);
IPAddress gateway(192, 168, 0, 1);
IPAddress subnet(255, 255, 0, 0);


// Needs to match the client
EthernetServer server(PORT);
boolean alreadyConnected = false; // whether or not the client was connected previously

void setupSocket() {
	// initialize the ethernet device
	Ethernet.begin(mac, ip, gateway, subnet);
	// start listening for clients
	server.begin();
	// Open serial communications and wait for port to open:
	Serial.begin(9600);
	while (!Serial) {
		; // wait for serial port to connect. Needed for Leonardo only
	}


	Serial.print(F("Server address:"));
	Serial.println(Ethernet.localIP());

}



void loopSocket() {
	// wait for a new client:
	EthernetClient client = server.available();

	// when the client sends the first byte, say hello:
	if (client) {
		if (!alreadyConnected) {
			// clear out the input buffer:
			client.flush();
			Serial.println(F("We have a new client"));
			client.println(F("Hello, client!"));
			char ch = '@'; //StartMsg
			client.write(ch);
			alreadyConnected = true;
			mode = Connected;
			return;
		}

		if (client.available() > 0) {
			// read the bytes incoming from the client:
			thisByte = client.read();
		}

		if (thisByte != -1)
		{
			Serial.println(thisByte);
			////Debugging:
			//Serial.print(thisByte);
			//return;
			//Each char is interpretted as byte representing a keypress
			//The byte is the id of button pressed + ' ' (so are printable
			if (mode != Running)
			{
				switch (thisByte)
				{
				case '0':
					if (mode == Connected)
					{
						mode = ACK0;
						client.write('1');
					}
					break;
				case '2':
					if (mode == ACK0)
					{
						mode = ACK2;
						client.write('3');
					}
					break;
				case '4':
					if (mode == ACK2)
					{
						mode = ACK4;
						client.write('5');
					}
					break;
				case '!': //Get Exclamation mark as indicator of request for Json
					if (mode == ACK4)
					{
						mode = Json1;
						client.write('/'); //Send back / meaning it will follow
					}
					break;
				case '/':  //App sends this back
					if (mode = Json1)
					{
						mode = Json2;
						client.println(F("{\"Config\":[ [ { \"iWidth\": 120 },{ \"iHeight\": 100 },{ \"iSpace\": 5 },{ \"iCornerRadius\": 10 },{ \"iRows\": 2 },{ \"iColumns\": 5 },{ \"sComPortId\": \"\\\\\\\\?\\\\USB#VID_26BA&PID_0003#5543830353935161A112#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"sFTDIComPortId\": \"\\\\\\\\?\\\\FTDIBUS#VID_0403+PID_6001+FTG71BUIA#0000#{86e0d1e0-8089-11d0-9ce4-08003e301f73}\" },{ \"iComportConnectDeviceNo\": -1 },{ \"iFTDIComportConnectDeviceNo\": 1 },{ \"sUseSerial\": \"BT\" } ] ] }~"));
					}
					break;
				case '~':  //Then when it gets above then sends this back as confirmation
					if (mode = Json2)
					{
						mode = Running;
						Serial.println("Now run");
						client.println(F("{\"MainMenu\":[ [ \"Something else\", \"Unload\", \"Show full list\", \"Setup Sockets\", \"The quick brown fox jumps over the lazy dog\" ],[ \"First\", \"Back\", \"Next\", \"Last\", \"Show All\" ] ] }~"));
					}
					break;
				case '^':  //Retart
					mode = Connected;
					break;
				}
			}
			else
			{
				//Do app functions here depending upon thisByte.
				//For now just echo it.
				DoAppEther(thisByte,client);
			}

		}
	}
}



