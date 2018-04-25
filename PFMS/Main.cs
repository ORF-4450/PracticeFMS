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
        public const string version = "2018.1.0.0"; //Syntax: Year.Major.Minor.Revision

        static string[] defaultOptions = new string[] {
            "AutonomousTime:15",
            "TeleoperatedTime:135",
            "CountdownTime:3",
            "PauseTime:3",
            "GameStringOverride:-1"
        };

        static Dictionary<string, int> options = new Dictionary<string, int>();

        static IPAddress FMSIp = IPAddress.Parse("10.00.100.5");

        public enum AllianceStations { RED1, RED2, RED3, BLUE1, BLUE2, BLUE3 }

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

        //For the 2018 season, there are two different game strings sent, one for the Red alliance, and one for the Blue alliance.
        public static string redGameString;
        public static string blueGameString;

        //This will place the config file in the same directory as PFMS.exe
        static string configPath = "config.txt";

        static void generateGameString()
        {
            Dictionary<int, string[]> gameStrings = new Dictionary<int, string[]>();
            gameStrings.Add(0, new string[2] { "RLR", "RLR" });
            gameStrings.Add(1, new string[2] { "LRL", "LRL" });
            gameStrings.Add(2, new string[2] { "RRR", "LLL" });
            gameStrings.Add(3, new string[2] { "LLL", "RRR" });

            int selection = new Random().Next(0, 4);
            if (options["GameStringOverride"] == -1 && options["GameStringOverride"] < 4 && options["GameStringOverride"] >= 0) selection = options["GameStringOverride"];

            redGameString = gameStrings[selection][0];
            blueGameString = gameStrings[selection][1];

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
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                switch (keyInfo.Key)
                {
                    case ConsoleKey.Enter:
                        estop = true;
                        break;

                    case ConsoleKey.D1:
                        if (red1 != null) red1.estop = true;
                        break;

                    case ConsoleKey.D2:
                        if (red2 != null) red2.estop = true;
                        break;

                    case ConsoleKey.D3:
                        if (red3 != null) red3.estop = true;
                        break;

                    case ConsoleKey.D4:
                        if (blue1 != null) blue1.estop = true;
                        break;

                    case ConsoleKey.D5:
                        if (blue2 != null) blue2.estop = true;
                        break;

                    case ConsoleKey.D6:
                        if (blue3 != null) blue3.estop = true;
                        break;
                }
            }
        }

        static void Main(string[] args)
        {
            //Welcome Message
            Console.Clear();
            Console.WriteLine("Welcome to the unoffical practice FMS Version {0}", version);
            Console.WriteLine("Written by MoSadie for FRC Team 4450, the Olympia Robotics Federation.");
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
            red1 = new DriverStation(Console.ReadLine(), AllianceStations.RED1);
            Console.WriteLine();
            Console.Write("Enter a team number for the Red 2 driver station: ");
            red2 = new DriverStation(Console.ReadLine(), AllianceStations.RED2);
            Console.WriteLine();
            Console.Write("Enter a team number for the Red 3 driver station: ");
            red3 = new DriverStation(Console.ReadLine(), AllianceStations.RED3);
            Console.WriteLine();
            Console.Write("Enter a team number for the Blue 1 driver station: ");
            blue1 = new DriverStation(Console.ReadLine(), AllianceStations.BLUE1);
            Console.WriteLine();
            Console.Write("Enter a team number for the Blue 2 driver station: ");
            blue2 = new DriverStation(Console.ReadLine(), AllianceStations.BLUE2);
            Console.WriteLine();
            Console.Write("Enter a team number for the Blue 3 driver station: ");
            blue3 = new DriverStation(Console.ReadLine(), AllianceStations.BLUE3);

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
                Thread.Sleep(500);
            }

            Console.Clear();
            Console.WriteLine("Field Status: PreMatch (Ready)");
            Console.WriteLine("Teams Participating: " + red1.TeamNumber + ", " + red2.TeamNumber + ", " + red3.TeamNumber + ", " + blue1.TeamNumber + ", " + blue2.TeamNumber + ", and " + blue3.TeamNumber);
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
            Console.WriteLine("Match Begins in " + TimeLeftInPhase + " seconds.");
            Console.WriteLine();

            while (TimeLeftInPhase > 0 && currentGamePhase == GamePhase.PREMATCH)
            {
                Console.WriteLine(TimeLeftInPhase);
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

                if (!red1.estop) Console.WriteLine("Press 1 to EStop team {0}", red1.TeamNumber);
                else Console.WriteLine("Team {0} has been estopped.", red1.TeamNumber);

                if (!red2.estop) Console.WriteLine("Press 2 to EStop team {0}", red2.TeamNumber);
                else Console.WriteLine("Team {0} has been estopped.", red2.TeamNumber);

                if (!red3.estop) Console.WriteLine("Press 3 to EStop team {0}", red3.TeamNumber);
                else Console.WriteLine("Team {0} has been estopped.", red3.TeamNumber);

                if (!blue1.estop) Console.WriteLine("Press 4 to EStop team {0}", blue1.TeamNumber);
                else Console.WriteLine("Team {0} has been estopped.", blue1.TeamNumber);

                if (!blue2.estop) Console.WriteLine("Press 5 to EStop team {0}", blue2.TeamNumber);
                else Console.WriteLine("Team {0} has been estopped.", blue2.TeamNumber);

                if (!blue3.estop) Console.WriteLine("Press 6 to EStop team {0}", blue3.TeamNumber);
                else Console.WriteLine("Team {0} has been estopped.", blue3.TeamNumber);

                Thread.Sleep(1000);
                TimeLeftInPhase--;
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

                Thread.Sleep(1000);
                TimeLeftInPhase--;
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

                Thread.Sleep(1000);
                TimeLeftInPhase--;
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
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey(true);
            System.Environment.Exit(0);
        }

        static void dsConnectThread()
        {
            TcpListener dsListener = new TcpListener(FMSIp, 1750);

            try
            {
                dsListener.Start();
            } catch
            {
                Console.WriteLine();
                Console.WriteLine("Something went wrong while attempting to start connecting driver stations. Please check the network configuration and try again.");
                Console.WriteLine("Press any key to exit.");
                Console.ReadKey(true);
                System.Environment.Exit(0);
            }
            Console.WriteLine("Listening for driver stations on {0} on port {1}", FMSIp.ToString(), 1750);

            while (true)
            {
                TcpClient tcpClient = dsListener.AcceptTcpClient();

                byte[] buffer = new byte[5];
                tcpClient.GetStream().Read(buffer, 0, buffer.Length);

                if (!(buffer[0] == 0 && buffer[1] == 3 && buffer[2] == 24))
                {
                    tcpClient.Close();
                    Console.WriteLine("Bad connection");
                    continue;
                }

                int teamId_1 = (int)buffer[3] << 8;
                int teamId_2 = buffer[4];
                int teamId = teamId_1 | teamId_2;

                int allianceStation = -1;
                string ip = tcpClient.Client.RemoteEndPoint.ToString().Split(':')[0];
                IPAddress dsIp = IPAddress.Parse(ip);
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
}
