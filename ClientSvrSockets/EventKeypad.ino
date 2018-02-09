/* @file EventSerialKeypad.pde
 || @version 1.0
 || @author David Jones
 || Based upon original from Alexander Brevig
 || @contact alexanderbrevig@gmail.com
 ||
 || @description
 || | Demonstrates using the KeypadEvent.
 || | Captures keypressed, realwase and hold events
 || #
 */
#include <Keypad.h>
#include <SPI.h>
#include <Ethernet.h>

const byte ROWS = 4; //four rows
const byte COLS = 3; //three columns
char keys[ROWS][COLS] = {
    {'1','2','3'},
    {'4','5','6'},
    {'7','8','9'},
    {'*','0','#'}
};


byte rowPins[ROWS] = {7,10, 9, 8}; //connect to the row pinouts of the keypad
byte colPins[COLS] = { 4, 5, 6}; //connect to the column pinouts of the keypad

//Cols:
//4: KB Pin 3
//5: KB Pin 1
//6: KB Pin 5

//Rows:
//7: KB Pin 2
//8: KB Pin 4
//9: KB Pin 6
//10: KB Pin 7

/* KB Pins:
 *  From left at bottom from above
 *  1,2,3,4,5,6,7
*/
void keypadEvent(KeypadEvent key);
Keypad keypad = Keypad( makeKeymap(keys), rowPins, colPins, ROWS, COLS );
byte ledPin = 13; 

boolean blink = false;
boolean ledPin_state;
//EthernetServer server2;

void KPadsetup(EthernetServer svr){
    Serial.begin(9600);
    pinMode(ledPin, OUTPUT);              // Sets the digital pin as output.
    digitalWrite(ledPin, HIGH);           // Turn the LED on.
    ledPin_state = digitalRead(ledPin);   // Store initial LED state. HIGH when LED is on.
    keypad.addEventListener(keypadEvent); // Add an event listener for this keypad
    //server2 = svr;
}




void KPloop(){
    char key = keypad.getKey();
}

// Taking care of some special events.
void keypadEvent(KeypadEvent key){
    bool ignore=false;
    switch (keypad.getState()){
    case PRESSED:
//        if (key == '#') {
//            digitalWrite(ledPin,!digitalRead(ledPin));
//            ledPin_state = digitalRead(ledPin);        // Remember LED state, lit or unlit.
//        }
          Serial.print("+");
          server.write('+');
        break;

    case RELEASED:
//        if (key == '*') {
//            digitalWrite(ledPin,ledPin_state);    // Restore LED state from before it started blinking.
//            blink = false;
//        }
          Serial.print("-");
          server.write('-');
        break;
        

    case HOLD:
       // if (key == '*') {
      //      blink = true;    // Blink the LED when holding the * key.
      //  }
        Serial.print("@");
        server.write('@');
        break;
    default:
    //Will be zero
        ignore=true;
        //Serial.println(keypad.getState());
        break;
    }
    if (!ignore)
      Serial.println(key);
      server.write(key);
      server.write('\n');
}
