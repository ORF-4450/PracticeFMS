using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.IO;

namespace PFMS
{
    class Arena
    {
        const string version = "2018.0.0";
        static string[] defaultOptions = new string[] {
            "AutonomousTime:15",
            "TeleoperatedTime:135",
            "CountdownTime:3",
            "PauseTime:3",
            "GameStringOverride:-1"
        };

        static Dictionary<string, int> options = new Dictionary<string, int>();

        static IPAddress FMSIp = IPAddress.Parse("10.00.100.5");

        static DriverStation red1;
        static DriverStation red2;
        static DriverStation red3;
        static DriverStation blue1;
        static DriverStation blue2;
        static DriverStation blue3;

        public static bool estop = false;

        public enum GamePhase { PREMATCH, AUTO, PAUSE, TELEOP, POSTMATCH };
        public static GamePhase currentGamePhase = GamePhase.PREMATCH;

        public static int TimeLeftInPhase = 0;

        public static string redGameString;
        public static string blueGameString;

        static string configPath = "config.txt";

        static void generateGameString()
        {
            Dictionary<int, string[]> gameStrings = new Dictionary<int, string[]>();
            gameStrings.Add(0, new string[2] { "RLR", "RLR" });
            gameStrings.Add(1, new string[2] { "LRL", "LRL" });
            gameStrings.Add(0, new string[2] { "RRR", "LLL" });
            gameStrings.Add(0, new string[2] { "LLL", "RRR" });

            int selection = new Random().Next(0, 5);
            if (options["GameStringOverride"] == -1 && options["GameStringOverride"] <= 4 && options["GameStringOverride"] >= 0) selection = options["GameStringOverride"];

            redGameString = gameStrings[selection][0];
            redGameString = gameStrings[selection][1];

        }

        static bool readyForMatchStart()
        {
            if (!red1.readyForMatchStart() || !red2.readyForMatchStart() || !red3.readyForMatchStart()) { return false; }
            if (!blue1.readyForMatchStart() || !blue2.readyForMatchStart() || !blue3.readyForMatchStart()) { return false; }

            return true;
        }

        static void eStopThread()
        {
            while (currentGamePhase != GamePhase.POSTMATCH && !estop)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey();
                if (keyInfo.Key == ConsoleKey.Enter)
                {
                    estop = true;
                }
            }
        }

        static void Main(string[] args)
        {
            //Welcome Message
            Console.Clear();
            Console.WriteLine("Welcome to the unoffical practice FMS Version {0}", version);
            Console.WriteLine("Written by MoSadie.");
            Console.WriteLine("Robots are dangerous. Please be safe.");

            //Config Setup
            if (!File.Exists(configPath))
            {
                using (StreamWriter stream = File.CreateText(configPath))
                {
                    foreach (string defaultOption in defaultOptions)
                    {
                        stream.WriteLine(defaultOption);
                    }
                }
            }

            //Config Message
            Console.WriteLine();
            Console.WriteLine("Configuration can be changed using the config.txt file in the same directory as this executable.");
            Console.WriteLine("Current Configuration:");

            using (StreamReader stream = File.OpenText(configPath))
            {
                string line;
                while ((line = stream.ReadLine()) != null)
                {
                    string key = line.Split(':')[0];
                    int number = int.Parse(line.Split(':')[1]);
                    options.Add(key, number);
                    Console.WriteLine("{0}: {1}", key, number);
                }
            }

            // Team Selection
            Console.WriteLine();
            Console.WriteLine();
            Console.Write("Enter a team number for the Red 1 driver station: ");
            red1 = new DriverStation(Console.ReadLine(), AllianceStation.RED1);
            Console.WriteLine();
            Console.Write("Enter a team number for the Red 2 driver station: ");
            red2 = new DriverStation(Console.ReadLine(), AllianceStation.RED2);
            Console.WriteLine();
            Console.Write("Enter a team number for the Red 3 driver station: ");
            red3 = new DriverStation(Console.ReadLine(), AllianceStation.RED3);
            Console.WriteLine();
            Console.Write("Enter a team number for the Blue 1 driver station: ");
            blue1 = new DriverStation(Console.ReadLine(), AllianceStation.BLUE1);
            Console.WriteLine();
            Console.Write("Enter a team number for the Blue 2 driver station: ");
            blue2 = new DriverStation(Console.ReadLine(), AllianceStation.BLUE2);
            Console.WriteLine();
            Console.Write("Enter a team number for the Blue 3 driver station: ");
            blue3 = new DriverStation(Console.ReadLine(), AllianceStation.BLUE3);

            //2018 Specific Game String Generation
            Console.WriteLine();
            generateGameString();
            Console.WriteLine("Game String for Red: " + redGameString);
            Console.WriteLine("Game String for Blue: " + blueGameString);
            Console.WriteLine();
            Console.WriteLine("Press Enter to start connecting Driver Stations.");
            Console.ReadLine();

            Console.WriteLine("Now waiting for driver stations to connect.");
            //TODO THIS LOOP CHECK CHEESY ARENA
            ThreadStart dsConnectThreadRef = new ThreadStart(dsConnectThread);
            Thread dsConnectThreadObj = new Thread(dsConnectThreadRef);
            dsConnectThreadObj.Start();

            while (!readyForMatchStart())
            {
                currentGamePhase = GamePhase.PREMATCH;
                Console.Clear();
                Console.WriteLine("Field Status: PreMatch (Waiting on driver stations and robots to connect)");
                Console.WriteLine();
                Console.WriteLine("Current Team Statuses:");
                Console.WriteLine(red1.ToString());
                Console.WriteLine(red2.ToString());
                Console.WriteLine(red3.ToString());
                Console.WriteLine(blue1.ToString());
                Console.WriteLine(blue2.ToString());
                Console.WriteLine(blue3.ToString());
                Thread.Sleep(20);
            }

            Console.Clear();
            Console.WriteLine("Field Status: PreMatch (Ready)");
            Console.WriteLine();
            Console.WriteLine("Current Team Statuses:");
            Console.WriteLine(red1.ToString());
            Console.WriteLine(red2.ToString());
            Console.WriteLine(red3.ToString());
            Console.WriteLine(blue1.ToString());
            Console.WriteLine(blue2.ToString());
            Console.WriteLine(blue3.ToString());
            Console.WriteLine();
            Console.WriteLine("Press Enter to start the match.");
            Console.ReadLine();

            red1.sendGameStringPacket();
            red2.sendGameStringPacket();
            red3.sendGameStringPacket();
            blue1.sendGameStringPacket();
            blue2.sendGameStringPacket();
            blue3.sendGameStringPacket();
            
            Console.Clear();
            Console.WriteLine("Field Status: Countdown");
            Console.WriteLine("Teams Participating: " + red1.TeamNumber + ", " + red2.TeamNumber + ", " + red3.TeamNumber + ", " + blue1.TeamNumber + ", " + blue2.TeamNumber + ", and " + blue3.TeamNumber);
            TimeLeftInPhase = options["CountdownTime"];
            Console.WriteLine("Match Begins in " + TimeLeftInPhase);

            while (TimeLeftInPhase > 0 && currentGamePhase == GamePhase.PREMATCH)
            {
                Thread.Sleep(1000);
                TimeLeftInPhase--;
            }

            ThreadStart estopThreadRef = new ThreadStart(eStopThread);
            Thread estopThread = new Thread(estopThreadRef);
            estopThread.Start();

            currentGamePhase = GamePhase.AUTO;
            TimeLeftInPhase = options["AutonomousTime"];
            while (TimeLeftInPhase > 0 && currentGamePhase == GamePhase.AUTO && !estop)
            {
                Console.Clear();
                Console.WriteLine("Field Status: Autonomous");
                Console.WriteLine("Teams Participating: " + red1.TeamNumber + ", " + red2.TeamNumber + ", " + red3.TeamNumber + ", " + blue1.TeamNumber + ", " + blue2.TeamNumber + ", and " + blue3.TeamNumber);
                Console.WriteLine("{0} seconds remain in Autonomous.", TimeLeftInPhase);
                Console.WriteLine();
                Console.WriteLine("Press Enter to EStop the match.");
            }

            currentGamePhase = GamePhase.PAUSE;
            TimeLeftInPhase = options["PauseTime"];
            while (TimeLeftInPhase > 0 && currentGamePhase == GamePhase.PAUSE && !estop)
            {
                Console.Clear();
                Console.WriteLine("Field Status: Pause between Autonomous and Teloperated");
                Console.WriteLine("Teams Participating: " + red1.TeamNumber + ", " + red2.TeamNumber + ", " + red3.TeamNumber + ", " + blue1.TeamNumber + ", " + blue2.TeamNumber + ", and " + blue3.TeamNumber);
                Console.WriteLine("{0} seconds remain in Pause.", TimeLeftInPhase);
                Console.WriteLine();
                Console.WriteLine("Press Enter to EStop the match.");
            }

            currentGamePhase = GamePhase.TELEOP;
            TimeLeftInPhase = options["TeleoperatedTime"];
            while (TimeLeftInPhase > 0 && currentGamePhase == GamePhase.TELEOP && !estop)
            {
                Console.Clear();
                Console.WriteLine("Field Status: Teleoperated");
                Console.WriteLine("Teams Participating: " + red1.TeamNumber + ", " + red2.TeamNumber + ", " + red3.TeamNumber + ", " + blue1.TeamNumber + ", " + blue2.TeamNumber + ", and " + blue3.TeamNumber);
                Console.WriteLine("{0} seconds remain in Teleoperated.", TimeLeftInPhase);
                Console.WriteLine("Game Strings: Red: {0} Blue: {1}", redGameString, blueGameString);
                Console.WriteLine();
                Console.WriteLine("Press Enter to EStop the match.");
            }

            currentGamePhase = GamePhase.POSTMATCH;

            Console.Clear();
            Console.WriteLine("Field Status: Match Complete");
            Console.WriteLine();
            Console.WriteLine("Cleaning up.");
            red1.dispose();
            red2.dispose();
            red3.dispose();
            blue1.dispose();
            blue2.dispose();
            blue3.dispose();
            Console.WriteLine("All done! Thank you for using the PFMS by MoSadie.");
            Console.WriteLine("To start another match, just restart this program.");
            Console.WriteLine();
            Console.WriteLine("Hit any key to exit...");
            Console.Read();
        }

        static void dsConnectThread()
        {
            TcpListener dsListener = new TcpListener(FMSIp, 1750);
            Console.WriteLine("Listening for driver stations on {0} on port {1}", FMSIp.ToString(), 1750);

            while (true)
            {
                TcpClient tcpClient = dsListener.AcceptTcpClient();

                byte[] buffer = new byte[5];
                tcpClient.GetStream().Read(buffer, 0, buffer.Length);

                if (!(buffer[0] == 0 && buffer[1] == 3 && buffer[2] == 24))
                {
                    tcpClient.Close();
                    continue;
                }

                int teamId = ((int)buffer[3]) << 8 + ((int)buffer[4]);

                int allianceStation = -1;
                IPAddress dsIp = IPAddress.Parse(tcpClient.Client.RemoteEndPoint.ToString());
                if (red1.TeamNumber == teamId) { allianceStation = 0; red1.setDsConnection(dsIp, tcpClient); }
                else if (red2.TeamNumber == teamId) { allianceStation = 1; red2.setDsConnection(dsIp, tcpClient); }
                else if (red3.TeamNumber == teamId) { allianceStation = 2; red3.setDsConnection(dsIp, tcpClient); }
                else if (blue1.TeamNumber == teamId) { allianceStation = 3; blue1.setDsConnection(dsIp, tcpClient); }
                else if (blue2.TeamNumber == teamId) { allianceStation = 4; blue2.setDsConnection(dsIp, tcpClient); }
                else if (blue3.TeamNumber == teamId) { allianceStation = 5; blue3.setDsConnection(dsIp, tcpClient); }

                if (allianceStation == -1)
                {
                    Console.WriteLine("Driver Station from team {0} attempted to connect, but they are not in this match!", teamId);
                    tcpClient.Close();
                    continue;
                }

                Console.WriteLine("Team {0} has connected their driver station!", teamId);

                //byte knowledge from Team 254's Cheesy Arena.
                byte[] assignmentPacket = new byte[5];
                assignmentPacket[0] = 0; //Size
                assignmentPacket[1] = 3; //Size
                assignmentPacket[2] = 25; //Type
                assignmentPacket[3] = (byte)allianceStation; //allianceStation
                assignmentPacket[4] = 0; //Station Status, I currently do not have checks for correct station, since there is no vlan.

                tcpClient.GetStream().Write(assignmentPacket, 0, assignmentPacket.Length);
            }
        }
    }

    class DriverStation
    {
        public DriverStation(string teamNumber, AllianceStation allianceStation)
        {
            if (int.TryParse(teamNumber, out TeamNumber))
            {
                switch(teamNumber.Length)
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
            } else
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
        }

        public int TeamNumber;
        public bool closed = false;
        public IPAddress robotIp;
        public IPAddress radioIp;
        public IPAddress driverStationIp;
        public bool isDSConnected = false;
        public bool isRobotRadioConnected = false;
        public bool isRoboRioConnected = false;
        public AllianceStation allianceStation;
        public int packetCount = 0;

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
            if (udpClient != null ) udpClient.Dispose();
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
            return allianceStation.ToString() + ": Team Number " + TeamNumber + " DS IP: " + ((driverStationIp == null) ? "Unregistered" : driverStationIp.ToString()) + " Connection status: DS: " + (isDSConnected ? "Connected" : "Disconnected") + " Radio: " + (isRobotRadioConnected ? "Connected" : "Disconnected") + " Robot: " + (isRoboRioConnected ? "Connected" : "Disconnected");
        }

        public bool isRedAlliance() { return (allianceStation == AllianceStation.RED1 || allianceStation == AllianceStation.RED2 || allianceStation == AllianceStation.RED3); }

        public bool readyForMatchStart()
        {
            return (driverStationIp != null && isDSConnected && isRoboRioConnected) || TeamNumber == 0;
        }

        byte[] generateDriverStationControlPacket()
        {
            byte[] packet = new byte[22];

            //Packet defination from 254's Cheesy Arena. This file to be exact: http://bit.ly/2HChGQ1
            //Packet Count
            packet[0] = (byte) ((packetCount >> 8) & 0xff);
            packet[1] = (byte) (packetCount & 0xff);

            //Version
            packet[2] = 0;

            //Robot Status
            packet[3] = 0;
            if (Arena.estop) { packet[3] |= 0x80; }
            else if (Arena.currentGamePhase.Equals(Arena.GamePhase.AUTO)) { packet[3] |= 0x02; }
            else if (Arena.currentGamePhase == Arena.GamePhase.TELEOP) { packet[3] |= 0x04; }

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
            int nanoseconds = (int) (currentTime.Ticks % TimeSpan.TicksPerMillisecond % 10) * 100;
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

        public byte[] generateGameStringPacket()
        {
            string gameString = "";
            if (allianceStation == AllianceStation.RED1 || allianceStation == AllianceStation.RED2 || allianceStation == AllianceStation.RED3) gameString = Arena.redGameString;
            else if (allianceStation == AllianceStation.BLUE1 || allianceStation == AllianceStation.BLUE2 || allianceStation == AllianceStation.BLUE3) gameString = Arena.blueGameString;

            byte[] stringBuffer = System.Text.Encoding.ASCII.GetBytes(gameString);

            byte[] packet = new byte[stringBuffer.Length + 4];
            packet[0] = 0; //Size
            packet[1] = (byte)(stringBuffer.Length + 2); //Size
            packet[2] = 28; //Type
            packet[3] = (byte)stringBuffer.Length;

            for(int i = 0; i < stringBuffer.Length; i++)
            {
                packet[i + 4] = stringBuffer[i];
            }

            return packet;
        }

        public void sendGameStringPacket()
        {
            byte[] packet = generateGameStringPacket();
            tcpClient.GetStream().Write(packet, 0, packet.Length);
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

    enum AllianceStation { RED1, RED2, RED3, BLUE1, BLUE2, BLUE3 }

    class DriverStationStatusPacket
    {
        public enum PacketType { KEEP_ALIVE, ROBOT_CONTROL, BAD_PACKET };

        static DriverStationStatusPacket decodeDriverStationStatusPacket(byte[] data)
        {
            DriverStationStatusPacket packet = new DriverStationStatusPacket();
			if ((int) data[2] == 28) {
                packet.packetType = PacketType.KEEP_ALIVE;
                return packet;
			} else if ((int) data[2] != 22) {
                packet.packetType = PacketType.BAD_PACKET;
				return packet;
			}

            packet.packetType = PacketType. ROBOT_CONTROL;

            return packet;
        }

        public DriverStationStatusPacket()
        {

        }
		
		public PacketType packetType;
		
    }
}
