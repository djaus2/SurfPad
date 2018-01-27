
#SurfPad
Turn your old Surface into into a large touchpad.

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
