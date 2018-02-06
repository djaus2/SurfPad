# SurfPad
Turn your old Surface into into a large touchpad.

## Wiki: 
Now has links to articles about this project

## Status:
- Fourth version (now available): Adds Arduino app as the remote app. Adds an Arduino app into the solution (needs VMicro Arduino Vs 2017 plugin.). Currently UWP app does some initial handshaking with remote app. Only. Keypresses are echoed (code sent to remote app and then its Text is looked up upon return. Also if second key is pressed, ie [Set up Serial] then get Hello World" message sent from remote app! [Setup BT] goes to BT setup page. Just double click on the BT device. (*More on this in my blog (see Wiki)*): https://github.com/djaus2/SurfPad/tree/2254af7aad5777ac74d87f13f7c349bc78d1e40e
- Third release now available: Implements a ListView on right. The text of button taps are listed.
https://github.com/djaus2/SurfPad/tree/9cc2bc99bcd19f9ee547906375b6ea28d3fe3279
- Second release: Reads button and app metainfo from a Json file: https://github.com/djaus2/SurfPad/tree/0507ef48136b1e9a5d707ba4d7a3b5706111a23b
- First release: As specified below:
https://github.com/djaus2/SurfPad/tree/38e4b990b59c7f5e5efcbf02d2c02670b423e370

## Articles:
- Coming: Bluetooth Serial Connectivity.
- [SurfPad - Your old Surface as a Remote App Touchpad: Text Output](http://embedded101.com/Blogs/David-Jones/entryid/799/SurfPad-Your-old-Surface-as-a-Remote-App-Touchpad-Text-Output)
- [SurfPad -  Your old Surface as a Remote App Touchpad: The UI](http://embedded101.com/Blogs/David-Jones/entryid/797/SurfPad-Your-old-Surface-as-a-Remote-App-Touchpad)
- [SurfPad - Your old Surface as a Remote App Touchpad: Json Configuration](http://embedded101.com/Blogs/David-Jones/entryid/798/SurfPad-Your-old-Surface-as-a-Remote-App-Touchpad-Json-Configuration)

Have you got an old RT Surface 1 or 2 gathering dust and don't know what to do with it?

With this app:
- Create a UWP app to run on a Suface (ARM or x86), or any touchscreen Windows laptop.
- Uses rounded boxes a touch keys
- Keys are placed in a grid
  - Grid parameters (specified in app): 
      - Cell lenngth and Width, 
      - Grid Spacing, 
      - Rounded box radius
- Communicates with an app running remotes via serial, Bluetooth(serial) or over Ethernet (eg WiFi)
- App sends info as to what keys to display 
  - Each key info:
    - Text
    - Id
    - Grid location (x,y)
  - Optional key specific info:
    - Background color, 
    - Multiple span of key across grid and down
- When a key is pressed its ID is sent to the app for action
- Scrolling textbox to display remote app info

May add Widgets later on:
- Switch
- Slider
- Rotator
- LEDs

Also later on:
- Page to display XAML sent as text from remote app.
- Surface as X-Y large mouse pad.

## Target for Remote app
The remote app platform can be any app on any system that supports Serial, Bluetooth Serial, or Sockets. Just neeeds to implement the interface. Some examples will be given eg for Windows 10 desktop, Windows 10 Mobile and Windows 10 IoT-Core. You are invited to add mote targets, once the interface is published here.
