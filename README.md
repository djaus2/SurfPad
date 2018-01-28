# SurfPad
Turn your old Surface into into a large touchpad.

## Status: First release under developement. Watch this space.

Further Info: http://embedded101.com/Blogs/David-Jones/entryid/797/SurfPad-Your-old-Surface-as-a-Remote-App-Touchpad

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

May add Widgets later:
- Switch
- Slider
- Rotator
- LEDs

Also later on my add:
- Page to display XAML sent astext from remote app.

## Target for Remote app
The remote app platform can be any app on any system that supports Serial, Bluetooth Serial, or Sockets. Just neeeds to implement the interface. Some examples will be given eg for Windows 10 desktop, Windows 10 Mobile and Windows 10 IoT-Core. You are invited to add mote targets, once the interface is published here.
