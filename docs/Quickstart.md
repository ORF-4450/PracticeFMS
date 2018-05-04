# Materials

## Materials Required (For the PFMS setup)
* A computer with the PracticeFMS executable.
* A router that can be configured and can broadcast in the 5Ghz range.

## Additional Materials Required (For using PFMS)
* At least one FRC Driver Station. (Up to 6)
* At least one FRC Robot with the 2018 FRC Control System. (Up to 6)

## Optional Extra Materials
* An additional computer for programming robot radios.
* Ethernet cables for driver station computers to plug into. (To mimic how the real field appears to drivers)
* Network switch for those ethernet cables. (If needed)
* A field (or practice field)

# Setup

## Step 1: Download software.
Download the PracticeFMS program from [here](https://github.com/MoSadie/PracticeFMS/releases/latest) onto the computer that will serve as the server.

## Step 2: Configure Router and PFMS Computer
Configure your router so the DHCP server serves IP addresses in the range of 10.0.100.6 to 10.0.100.254 with a subnet mask of 255.0.0.0. You can also set whatever SSID and WPA key you want, just make sure you remember it.
On the computer with the PracticeFMS program, set a static IP of 10.0.100.5.

## Step 3: Program Robot Radios
Download and install the latest version of the FRC Radio Configuration Utility from [here](http://wpilib.screenstepslive.com/s/currentCS/m/getting_started/l/144986-programming-your-radio#download_the_software).
Then in the utility, select Tools and then select "FMS Offseason Mode."
Follow the instructions onscreen to enter the SSID (For the 5Ghz network) and WPA key, and then you can program robot radios to connect to your field network.

**NOTE:** When the robots are on after being configured, the radio _will_ connect automatically to the field network. If too many teams are connected to the router, it may slow down. You may want to have teams only use their radios on the field.

# Usage

## Step 1: Start PracticeFMS.
Start the PracticeFMS program on the server computer.

## Step 2: Configure Match.
When prompted, enter the team number for each alliance station, or leave it blank if there is no team there.

## Step 3: Connect Driver Stations.
Make sure every Driver Station is connected to the network, and press enter when prompted to start connecting Driver Stations. This process will run until all Driver Stations are connected with robots. You can cancel this process by closing the program.

## Step 4: Start the Match
When prompted, press Enter to start a match countdown and to trigger sending the Game Specific String to the Driver Stations. The match will then run like normal, with the defaults of a 3-second countdown, followed by a 15 second Autonomous period, followed by a 3-second pause, followed by 135 seconds of Teleoperated period.

## Step 5: (Optional) E-stop a robot or the whole match.
If the need arises that a robot needs to be E-stopped, there is a designated keyboard key for E-stopping each robot on the field. (These are all in the number row, and based off the default configuration, check the console window during a match to see the keys for your specific configuration)

| Red 1 | Red 2 | Red 3 | Blue 1 | Blue 2 | Blue 3 |
|---|---|---|---|---|---|
| 1 | 2 | 3 | 4 | 5 | 6 |

If something has gone terribly wrong and the whole match, including the timer and robots, needs to be E-stopped immediately, just press Enter in the PracticeFMS program while the match is running. This will immediately E-stop every robot and end the match.

[Return Home](index.md)