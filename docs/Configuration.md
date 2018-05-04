# Config Options
## AutonomousTime
The amount of time, in seconds, that the Autonomous period will last.

## TeleoperatedTime
The amount of time, in seconds, that the Teleoperated period will last.

## CountdownTime
The number of seconds of countdown given before the match starts.

## PauseTime
The amount of time, in seconds, between the Autonomous period ending and the Teleoperated period being enabled.

## GameStringOverride
See the table below for choosing a value:

| -1 | 0 | 1 | 2 | 3 |
|---|---|---|---|---|
| Override disabled, randomly chosen | Red: `RLR` Blue: `RLR` | Red: `LRL` Blue: `LRL` | Red: `RRR` Blue: `LLL` | Red: `LLL` Blue: `RRR` |

## RedAllianceCount
The number of robots on the red alliance.

## BlueAllianceCount
The number of robots on the blue alliance.
  
# Default Config
>AutonomousTime:15
TeleoperatedTime:135
CountdownTime:3
PauseTime:3
GameStringOverride:-1
RedAllianceCount:3
BlueAllianceCount:3 

[Return Home](index.md)