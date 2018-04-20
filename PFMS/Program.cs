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
    class MainClass
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

        static enum GamePhase { PREMATCH, AUTO, PAUSE, TELEOP, POSTMATCH };
        static GamePhase currentGamePhase = GamePhase.PREMATCH;

        static string redGameString;
        static string blueGameString;

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

            //TODO THIS CHECK CHEESY ARENA
            TcpListener dsConnectListener = new TcpListener(FMSIp, 1750);

            listener.Start();

            while (true)
            {
                Console.WriteLine("Waiting for a connection...");
                TcpClient client = listener.AcceptTcpClient();
                Console.WriteLine("Connection Gotten!");

                byte[] bytes = new byte[1024];
                string data;
                int i;

                NetworkStream stream = client.GetStream();

                i = stream.Read(bytes, 0, bytes.Length);

                while (i != 0)
                {
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine(String.Format("Received: {0}", data));

                    data = data.ToUpper();

                    //Insert fms response here.
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                    stream.Write(msg, 0, msg.Length);
                    Console.WriteLine(String.Format("Sent: {0}", data));

                    i = stream.Read(bytes, 0, bytes.Length);
                }

                client.Close();
            }

            Console.WriteLine("Hit enter to continue...");
            Console.Read();
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

        int TeamNumber;
        bool closed = false;
        IPAddress robotIp;
        IPAddress radioIp;
        IPAddress driverStationIp;
        bool isDSConnected = false;
        bool isRobotRadioConnected = false;
        bool isRoboRioConnected = false;
        AllianceStation allianceStation;
        int packetCount = 0;

        ThreadStart pingThreadRef;
        Thread pingThread;

        ThreadStart recieveDataThreadRef;
        Thread recieveDataThread;

        ThreadStart sendDataThreadRef;
        Thread sendDataThread;

        UdpClient udpClient;
        TcpListener tcpListener;

        public bool isRedAlliance() { return (allianceStation == AllianceStation.RED1 || allianceStation == AllianceStation.RED2 || allianceStation == AllianceStation.RED3); }

        byte[] generateDriverStationControlPacket()
        {
            byte[] packet = new byte[22];

            //Packet defination from 254's Cheesy Arena.
            //Packet Count
            packet[0] = (byte) ((packetCount >> 8) & 0xff);
            packet[1] = (byte) (packetCount & 0xff);

            //Version
            packet[2] = 0;

            //Robot Status
            packet[3] = 0;
            if (MainClass.currentGameState == MainClass.GameState.AUTO) { packet[3] |= 0x02; }
            else if (MainClass.currentGameState == MainClass.GameState.TELEOP) { packet[3] |= 0x04; }

            //Unused
            packet[4] = 0;

            //Alliance Station
            packet[5] = (int)allianceStation;

            //Match Type
            packet[6] = 1; //Practice

            //Match Number
            packet[7] = 0;
            packet[8] = 1;

            //Repeat Number
            packet[9] = 1;

            //Current Time
            DateTime currentTime = DateTime.now;
            int nanoseconds = (currentTime.Ticks % TimeSpan.ticksPerMillasecond % 10) * 100;
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

            //TODO Match Time Remaining [20 and 21]
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
                TcpClient client = tcpListener.AcceptTcpClient();
                byte[] buffer = new byte[4096];


                i = client.GetStream().Read(bytes, 0, bytes.Length);

                while (i != 0)
                {
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine(String.Format("Received: {0}", data));

                    data = data.ToUpper();

                    //Insert fms response here.
                    byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                    stream.Write(msg, 0, msg.Length);
                    Console.WriteLine(String.Format("Sent: {0}", data));

                    i = client.GetStream().Read(bytes, 0, bytes.Length);
                }
            }
        }
    }

    enum AllianceStation { RED1, RED2, RED3, BLUE1, BLUE2, BLUE3 }

    class DriverStationStatusPacket
    {
        static enum PacketType { KEEP_ALIVE, ROBOT_CONTROL, BAD_PACKET };

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
