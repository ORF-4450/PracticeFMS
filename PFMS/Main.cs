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
        public const string version = "2018.1.1.0"; //Syntax: Year.Major.Minor.Revision

        static string[] defaultOptions = new string[] {
            "AutonomousTime:15",
            "TeleoperatedTime:135",
            "CountdownTime:3",
            "PauseTime:3",
            "GameStringOverride:-1",
            "RedAllianceCount:3",
            "BlueAllianceCount:3"
        };

        static Dictionary<string, int> options = new Dictionary<string, int>();

        static IPAddress FMSIp = IPAddress.Parse("10.00.100.5");

        public enum AllianceStations { RED1, RED2, RED3, BLUE1, BLUE2, BLUE3 }

        static DriverStation[] redAlliance;
        static DriverStation[] blueAlliance;

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
            if (options["GameStringOverride"] != -1 && options["GameStringOverride"] < 4 && options["GameStringOverride"] >= 0) selection = options["GameStringOverride"];

            redGameString = gameStrings[selection][0];
            blueGameString = gameStrings[selection][1];

        }

        static bool readyForMatchStart()
        {
            foreach(DriverStation ds in redAlliance) { if (!ds.readyForMatchStart()) return false; }
            foreach (DriverStation ds in blueAlliance) { if (!ds.readyForMatchStart()) return false; }

            return true;
        }

        public static List<ConsoleKey> estopkeyList = new List<ConsoleKey>() { ConsoleKey.D1, ConsoleKey.D2, ConsoleKey.D3, ConsoleKey.D4, ConsoleKey.D5, ConsoleKey.D6, ConsoleKey.D7, ConsoleKey.D8, ConsoleKey.D9, ConsoleKey.D0, ConsoleKey.Q, ConsoleKey.W, ConsoleKey.E, ConsoleKey.R, ConsoleKey.T, ConsoleKey.Y, ConsoleKey.U, ConsoleKey.I, ConsoleKey.O, ConsoleKey.P, ConsoleKey.A, ConsoleKey.S, ConsoleKey.D, ConsoleKey.F, ConsoleKey.G, ConsoleKey.H, ConsoleKey.J, ConsoleKey.K, ConsoleKey.L, ConsoleKey.Z, ConsoleKey.X, ConsoleKey.C, ConsoleKey.V, ConsoleKey.B, ConsoleKey.N, ConsoleKey.M };
        public static Dictionary<ConsoleKey, DriverStation> estopDsDict = new Dictionary<ConsoleKey, DriverStation>();
        static void eStopThread()
        {
            while (currentGamePhase != GamePhase.POSTMATCH && !estop)
            {
                ConsoleKeyInfo keyInfo = Console.ReadKey(true);
                if (keyInfo.Key == ConsoleKey.Enter) estop = true;
                else if (estopDsDict.ContainsKey(keyInfo.Key))
                {
                    estopDsDict[keyInfo.Key].estop = true;
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
            redAlliance = new DriverStation[options["RedAllianceCount"]];
            blueAlliance = new DriverStation[options["BlueAllianceCount"]];
            Console.WriteLine();
            Console.WriteLine();
            for (int i = 0; i < redAlliance.Length; i++)
            {
                Console.Write("Enter a team number for the Red " + (i+1) + " driver station: ");
                redAlliance[i] = new DriverStation(Console.ReadLine(), true, i+1);
                Console.WriteLine();
            }
            for (int i = 0; i < blueAlliance.Length; i++)
            {
                Console.Write("Enter a team number for the Blue " + (i + 1) + " driver station: ");
                blueAlliance[i] = new DriverStation(Console.ReadLine(), false, i + 1);
                Console.WriteLine();
            }

            //2018 Specific Game String Generation
            Console.WriteLine();
            generateGameString();
            Console.WriteLine("Game String for Red: " + redGameString);
            Console.WriteLine("Game String for Blue: " + blueGameString);
            Console.WriteLine();
            Console.WriteLine("Press Enter to start connecting Driver Stations.");
            Console.ReadLine();

            Console.WriteLine("Now waiting for driver stations to connect.");

            ThreadStart dsConnectThreadRef = new ThreadStart(dsConnectThread);
            Thread dsConnectThreadObj = new Thread(dsConnectThreadRef);
            dsConnectThreadObj.Start();

            Thread.Sleep(200);
            if (estop) return;
            
            while (!readyForMatchStart())
            {
                currentGamePhase = GamePhase.PREMATCH;
                Console.Clear();
                Console.WriteLine("Field Status: PreMatch (Waiting on driver stations and robots to connect)");
                Console.WriteLine();
                Console.WriteLine("Current Team Statuses:");
                foreach (DriverStation ds in redAlliance) if (ds.TeamNumber != 0) Console.WriteLine(ds.ToString());
                foreach (DriverStation ds in blueAlliance) if (ds.TeamNumber != 0) Console.WriteLine(ds.ToString());
                Thread.Sleep(500);
            }

            string teamsParticipatingString = "Teams Participating: ";
            List<DriverStation> nonZeroTeams = new List<DriverStation>();
            foreach (DriverStation ds in redAlliance) if (ds.TeamNumber != 0) nonZeroTeams.Add(ds);
            foreach (DriverStation ds in blueAlliance) if (ds.TeamNumber != 0) nonZeroTeams.Add(ds);

            for (int i = 0; i < nonZeroTeams.Count - 1; i++) teamsParticipatingString += blueAlliance[i].TeamNumber + ", ";
            teamsParticipatingString += " " + nonZeroTeams[nonZeroTeams.Count - 1].TeamNumber;

            Console.Clear();
            Console.WriteLine("Field Status: PreMatch (Ready)");
            Console.WriteLine(teamsParticipatingString);
            Console.WriteLine();
            Console.WriteLine("Press Enter to start the match.");
            Console.ReadLine();

            foreach (DriverStation ds in redAlliance) ds.sendGameStringPacket();
            foreach (DriverStation ds in blueAlliance) ds.sendGameStringPacket();

            Console.Clear();
            Console.WriteLine("Field Status: Countdown");
            Console.WriteLine(teamsParticipatingString);
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
                Console.WriteLine(teamsParticipatingString);
                Console.WriteLine("{0} seconds remain in Autonomous.", TimeLeftInPhase);
                Console.WriteLine();
                Console.WriteLine("Press Enter to EStop the match.");

                foreach (DriverStation ds in redAlliance)
                {
                    if (ds.TeamNumber == 0) continue;
                    if (!ds.estop) Console.WriteLine("Press {0} to EStop team {1}", ds.estopKey, ds.TeamNumber);
                    else Console.WriteLine("Team {0} has been estopped.", ds.TeamNumber);
                }

                foreach (DriverStation ds in blueAlliance)
                {
                    if (ds.TeamNumber == 0) continue;
                    if (!ds.estop) Console.WriteLine("Press {0} to EStop team {1}", ds.estopKey, ds.TeamNumber);
                    else Console.WriteLine("Team {0} has been estopped.", ds.TeamNumber);
                }
                
                Thread.Sleep(1000);
                TimeLeftInPhase--;
            }

            currentGamePhase = GamePhase.PAUSE;
            TimeLeftInPhase = options["PauseTime"];
            while (TimeLeftInPhase > 0 && currentGamePhase == GamePhase.PAUSE && !estop)
            {
                Console.Clear();
                Console.WriteLine("Field Status: Pause between Autonomous and Teloperated");
                Console.WriteLine(teamsParticipatingString);
                Console.WriteLine("{0} seconds remain in Pause.", TimeLeftInPhase);
                Console.WriteLine();
                Console.WriteLine("Press Enter to EStop the match.");

                foreach (DriverStation ds in redAlliance)
                {
                    if (ds.TeamNumber == 0) continue;
                    if (!ds.estop) Console.WriteLine("Press {0} to EStop team {1}", ds.estopKey, ds.TeamNumber);
                    else Console.WriteLine("Team {0} has been estopped.", ds.TeamNumber);
                }

                foreach (DriverStation ds in blueAlliance)
                {
                    if (ds.TeamNumber == 0) continue;
                    if (!ds.estop) Console.WriteLine("Press {0} to EStop team {1}", ds.estopKey, ds.TeamNumber);
                    else Console.WriteLine("Team {0} has been estopped.", ds.TeamNumber);
                }

                Thread.Sleep(1000);
                TimeLeftInPhase--;
            }

            currentGamePhase = GamePhase.TELEOP;
            TimeLeftInPhase = options["TeleoperatedTime"];
            while (TimeLeftInPhase > 0 && currentGamePhase == GamePhase.TELEOP && !estop)
            {
                Console.Clear();
                Console.WriteLine("Field Status: Teleoperated");
                Console.WriteLine(teamsParticipatingString);
                Console.WriteLine("{0} seconds remain in Teleoperated.", TimeLeftInPhase);
                Console.WriteLine("Game Strings: Red: {0} Blue: {1}", redGameString, blueGameString);
                Console.WriteLine();
                Console.WriteLine("Press Enter to EStop the match.");

                foreach (DriverStation ds in redAlliance)
                {
                    if (ds.TeamNumber == 0) continue;
                    if (!ds.estop) Console.WriteLine("Press {0} to EStop team {1}", ds.estopKey, ds.TeamNumber);
                    else Console.WriteLine("Team {0} has been estopped.", ds.TeamNumber);
                }

                foreach (DriverStation ds in blueAlliance)
                {
                    if (ds.TeamNumber == 0) continue;
                    if (!ds.estop) Console.WriteLine("Press {0} to EStop team {1}", ds.estopKey, ds.TeamNumber);
                    else Console.WriteLine("Team {0} has been estopped.", ds.TeamNumber);
                }

                Thread.Sleep(1000);
                TimeLeftInPhase--;
            }

            currentGamePhase = GamePhase.POSTMATCH;

            Console.Clear();
            Console.WriteLine("Field Status: Match Complete");
            Console.WriteLine();
            Console.WriteLine("Cleaning up.");
            foreach (DriverStation ds in redAlliance) ds.dispose();
            foreach (DriverStation ds in blueAlliance) ds.dispose();
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
                Arena.estop = true;
                Console.ReadKey(true);
                System.Environment.Exit(0);
            }
            Console.WriteLine("Listening for driver stations on {0} on port {1}", FMSIp.ToString(), 1750);

            while (Arena.currentGamePhase == GamePhase.PREMATCH)
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

                for (int i = 0; i < redAlliance.Length; i++)
                {
                    if (redAlliance[i].TeamNumber == teamId)
                    {
                        allianceStation = (int)DriverStation.IntToStation((i + 1), true);
                        redAlliance[i].setDsConnection(dsIp, tcpClient);
                        break;
                    }
                }

                for (int i = 0; i < blueAlliance.Length && allianceStation == -1; i++)
                {
                    if (blueAlliance[i].TeamNumber == teamId)
                    {
                        allianceStation = (int)DriverStation.IntToStation((i + 1), false);
                        blueAlliance[i].setDsConnection(dsIp, tcpClient);
                        break;
                    }
                }

                if (currentGamePhase != GamePhase.PREMATCH) break;

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
            dsListener.Stop();
        }
    }
}
