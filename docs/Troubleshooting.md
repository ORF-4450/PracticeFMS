## Driver stations won't connect to the robot!
Things to check:
- The driver station is connected to the network. (Indicated by the "Connected to FMS" message appearing if appropriate)
- The robot is on.
- The robot's radio has been configured. (See [here](Troubleshooting.md#robots-wont-connect-to-the-router) for info on checking this.)

## Robots won't connect to the router!
First, look at the Wifi light on the robot radio. (See [this](http://wpilib.screenstepslive.com/s/currentCS/m/troubleshooting/l/599674-status-light-quick-reference#OpenMeshRadio) picture for what lights are which)

| Robot Radio Wifi Light | Status
|---|---|
| Off | Either: Non-FRC firmware (Radio needs reprogramming) or not connected to the field network. |
| Green | Radio is connected to field network. Something else is wrong. Check the field network configuration. |
| Red/Yellow/Orange | Not programmed in FMS Offseason mode. See [here](Quickstart.md#step-3-program-robot-radios) for instructions on how to program the radio in FMS Offseason mode. |

### Common problems that cause the Wifi light to be off:
* The WPA key or SSID for the field network was typed in incorrectly.
* Attempting to connect the radios to a 2.4Ghz network, you must use a 5Ghz network. (It's usually a different SSID)

## The driver stations aren't connecting to the FMS!
Things to check:
* Has the PracticeFMS program been told to connect driver stations yet? After typing all team number in, there should be a prompt to "Press Enter to start connecting Driver Stations."
* Are the correct team numbers entered for the practice match? If not, there should be messages that clear every half-second that say which team number is attempting to connect. The program just needs to be closed and reopened if there is an incorrect team number.
* Are the driver stations actually connected to the field network? It depends on the individual network setup, but the most realistic setup is to use ethernet cables connecting driver stations to the field network. Also, check to see if the correct network adapters are enabled.

[Return Home](index.md)