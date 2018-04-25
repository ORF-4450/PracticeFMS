using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static PFMS.Arena;

namespace PFMS
{
    class DriverStation
    {
        public DriverStation(string teamNumber, AllianceStations allianceStation)
        {
            if (int.TryParse(teamNumber, out TeamNumber))
            {
                switch (teamNumber.Length)
                {
                    case 1:
                    case 2:
                        robotIp = IPAddress.Parse("10.00." + teamNumber + ".2");
                        break;

                    case 3:
                        robotIp = IPAddress.Parse("10.0" + teamNumber[0] + "." + teamNumber[1] + teamNumber[2] + ".2");
                        break;

                    case 4:
                        robotIp = IPAddress.Parse("10." + teamNumber.Substring(0, 2) + "." + teamNumber.Substring(2) + ".2");
                        break;

                    default:
                        robotIp = IPAddress.Parse("10.0.0.2");
                        break;
                }
            }
            else
            {
                TeamNumber = 0;
                robotIp = IPAddress.Parse("10.0.0.2");
            }
            radioIp = IPAddress.Parse(robotIp.ToString().Substring(0, robotIp.ToString().Length - 1) + "1");
            Console.WriteLine("DriverStation created with robot IP of {0} and radio IP of {1}", robotIp.ToString(), radioIp.ToString());

            pingThreadRef = new ThreadStart(robotPingThread);
            pingThread = new Thread(pingThreadRef);
            pingThread.Start();

            recieveDataThreadRef = new ThreadStart(recieveStatusThread);
            recieveDataThread = new Thread(recieveDataThreadRef);
            recieveDataThread.Start();

            sendDataThreadRef = new ThreadStart(sendControlDataThread);
            sendDataThread = new Thread(sendDataThreadRef);
            sendDataThread.Start();

            this.allianceStation = allianceStation;
        }

        public int TeamNumber;
        public bool closed = false;
        public IPAddress robotIp;
        public IPAddress radioIp;
        public IPAddress driverStationIp;
        public bool isDSConnected = false;
        public bool isRobotRadioConnected = false;
        public bool isRoboRioConnected = false;
        public AllianceStations allianceStation;
        public int packetCount = 0;

        public bool estop = false;

        ThreadStart pingThreadRef;
        Thread pingThread;

        ThreadStart recieveDataThreadRef;
        Thread recieveDataThread;

        ThreadStart sendDataThreadRef;
        Thread sendDataThread;

        UdpClient udpClient;
        public TcpClient tcpClient;

        public void dispose()
        {
            if (udpClient != null) udpClient.Dispose();
            if (tcpClient != null) tcpClient.Dispose();
        }

        public void setDsConnection(IPAddress dsIp, TcpClient tcpConnection)
        {
            driverStationIp = dsIp;
            tcpClient = tcpConnection;
            udpClient = new UdpClient(dsIp.ToString(), 1121);
        }

        public override string ToString()
        {
            string stringToReturn;
            if (TeamNumber != 0) stringToReturn = allianceStation.ToString() + ": Team Number " + TeamNumber + " DS IP: " + ((driverStationIp == null) ? "Unregistered" : driverStationIp.ToString()) + " EStop: " + estop + " Connection status: DS: " + (isDSConnected ? "Connected" : "Disconnected") + " Radio: " + (isRobotRadioConnected ? "Connected" : "Disconnected") + " Robot: " + (isRoboRioConnected ? "Connected" : "Disconnected");
            else stringToReturn = allianceStation.ToString() + ": BYPASSED";
            return stringToReturn;
        }

        public bool isRedAlliance() { return (allianceStation == AllianceStations.RED1 || allianceStation == AllianceStations.RED2 || allianceStation == AllianceStations.RED3); }

        public bool readyForMatchStart()
        {
            return (driverStationIp != null && isDSConnected && isRoboRioConnected) || TeamNumber == 0;
        }

        byte[] generateDriverStationControlPacket()
        {
            byte[] packet = new byte[22];

            //Packet defination from 254's Cheesy Arena. This file to be exact: http://bit.ly/2HChGQ1
            //Packet Count
            packet[0] = (byte)((packetCount >> 8) & 0xff);
            packet[1] = (byte)(packetCount & 0xff);

            //Version
            packet[2] = 0;

            //Robot Status
            packet[3] = 0;
            if (Arena.estop || estop) { packet[3] |= 0x80; }
            if (Arena.currentGamePhase == Arena.GamePhase.AUTO || Arena.currentGamePhase == Arena.GamePhase.PREMATCH) { packet[3] |= 0x02; }
            if (Arena.currentGamePhase == Arena.GamePhase.TELEOP || Arena.currentGamePhase == Arena.GamePhase.AUTO
                ) { packet[3] |= 0x04; }

            //Unused
            packet[4] = 0;

            //Alliance Station
            packet[5] = (byte)(int)allianceStation;

            //Match Type
            packet[6] = 1; //Practice

            //Match Number
            packet[7] = 0;
            packet[8] = 1;

            //Repeat Number
            packet[9] = 1;

            //Current Time
            DateTime currentTime = DateTime.Now;
            int nanoseconds = (int)(currentTime.Ticks % TimeSpan.TicksPerMillisecond % 10) * 100;
            packet[10] = (byte)(((nanoseconds / 1000) >> 24) & 0xff);
            packet[11] = (byte)(((nanoseconds / 1000) >> 16) & 0xff);
            packet[12] = (byte)(((nanoseconds / 1000) >> 8) & 0xff);
            packet[13] = (byte)((nanoseconds / 1000) & 0xff);
            packet[14] = (byte)currentTime.Second;
            packet[15] = (byte)currentTime.Minute;
            packet[16] = (byte)currentTime.Hour;
            packet[17] = (byte)currentTime.Day;
            packet[18] = (byte)currentTime.Month;
            packet[19] = (byte)(currentTime.Year - 1900);

            //Match Time Remaining
            packet[20] = (byte)(Arena.TimeLeftInPhase >> 8 & 0xff);
            packet[21] = (byte)(Arena.TimeLeftInPhase & 0xff);

            return packet;
        }

        void sendControlDataThread()
        {
            while (!closed)
            {
                if (udpClient != null)
                {
                    byte[] packet = generateDriverStationControlPacket();
                    udpClient.Send(packet, packet.Length);
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
        }

        public byte[] generateGameStringPacket()
        {
            string gameString = "";
            if (allianceStation == AllianceStations.RED1 || allianceStation == AllianceStations.RED2 || allianceStation == AllianceStations.RED3) gameString = Arena.redGameString;
            else if (allianceStation == AllianceStations.BLUE1 || allianceStation == AllianceStations.BLUE2 || allianceStation == AllianceStations.BLUE3) gameString = Arena.blueGameString;

            byte[] stringBuffer = System.Text.Encoding.ASCII.GetBytes(gameString);

            byte[] packet = new byte[stringBuffer.Length + 4];
            packet[0] = 0; //Size
            packet[1] = (byte)(stringBuffer.Length + 2); //Size
            packet[2] = 28; //Type
            packet[3] = (byte)stringBuffer.Length;

            for (int i = 0; i < stringBuffer.Length; i++)
            {
                packet[i + 4] = stringBuffer[i];
            }

            return packet;
        }

        public void sendGameStringPacket()
        {
            if (tcpClient != null)
            {
                byte[] packet = generateGameStringPacket();
                tcpClient.GetStream().Write(packet, 0, packet.Length);
            }
        }

        public void robotPingThread()
        {
            Ping ping = new Ping();
            int timeout = 5;
            while (!closed)
            {
                //Ping Robot Radio
                PingReply result = ping.Send(radioIp, timeout);
                isRobotRadioConnected = result.Status == IPStatus.Success;

                //Ping Robot
                result = ping.Send(robotIp, timeout);
                isRoboRioConnected = result.Status == IPStatus.Success;

                if (driverStationIp != null)
                {
                    result = ping.Send(driverStationIp, timeout);
                    isDSConnected = result.Status == IPStatus.Success;
                }
            }
        }

        public void recieveStatusThread()
        {
            while (!closed)
            {
                if (tcpClient == null)
                {
                    Thread.Sleep(1000);
                    continue;
                }

                byte[] buffer = new byte[4096];

                int i = tcpClient.GetStream().Read(buffer, 0, buffer.Length);

                while (i != 0)
                {
                    //TODO Add some basic logging?
                    Thread.Sleep(20); //Don't want to kill the computer.
                    i = tcpClient.GetStream().Read(buffer, 0, buffer.Length);
                }
            }
        }
    }
}
