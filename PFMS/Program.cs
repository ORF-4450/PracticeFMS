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
    class Program
    {
        const string version = "2018.0.0";
        static string[] defaultOptions = new string[] {
            "AutonomousTime:15",
            "TeleoperatedTime:135",
            "CountdownTime:3",
            "PauseTime:3"
        };

        static Dictionary<string, int> options = new Dictionary<string, int>();

        static DriverStation red1;
        static DriverStation red2;
        static DriverStation red3;
        static DriverStation blue1;
        static DriverStation blue2;
        static DriverStation blue3;

        static string configPath = "config.txt";

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
                    Console.WriteLine("{0}: {1} seconds", key, number);
                }
            }

            // Team Selection
            Console.WriteLine();
            Console.WriteLine();
            Console.Write("Enter a team number for the Red 1 driver station: ");
            red1 = new DriverStation(Console.ReadLine());
            Console.WriteLine();
            Console.Write("Enter a team number for the Red 2 driver station: ");
            red2 = new DriverStation(Console.ReadLine());
            Console.WriteLine();
            Console.Write("Enter a team number for the Red 3 driver station: ");
            red3 = new DriverStation(Console.ReadLine());
            Console.WriteLine();
            Console.Write("Enter a team number for the Blue 1 driver station: ");
            blue1 = new DriverStation(Console.ReadLine());
            Console.WriteLine();
            Console.Write("Enter a team number for the Blue 2 driver station: ");
            blue2 = new DriverStation(Console.ReadLine());
            Console.WriteLine();
            Console.Write("Enter a team number for the Blue 3 driver station: ");
            blue3 = new DriverStation(Console.ReadLine());

            //TODO THIS CHECK CHEESY ARENA
            TcpListener listener = new TcpListener(IPAddress.Any, 80);

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
        public DriverStation(string teamNumber)
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
        }

        int TeamNumber;
        ThreadStart pingThreadRef;
        Thread pingThread;
        IPAddress robotIp;
        IPAddress radioIp;
        IPAddress driverStationIp;
        bool isDSConnected = false;
        bool isRobotRadioConnected = false;
        bool isRoboRioConnected = false;

        public void robotPingThread()
        {
            Ping ping = new Ping();
            int timeout = 5;
            string data = "Hey thing! I want to know if you're alive!";
            byte[] buffer = Encoding.ASCII.GetBytes(data);
            while (true)
            {
                //Ping Robot Radio
                PingReply result = ping.Send(radioIp, timeout, buffer);
                isRobotRadioConnected = result.Status == IPStatus.Success;

                //Ping Robot
                result = ping.Send(robotIp, timeout, buffer);
                isRoboRioConnected = result.Status == IPStatus.Success;

                if (driverStationIp != null)
                {
                    result = ping.Send(driverStationIp, timeout, buffer);
                    isDSConnected = result.Status == IPStatus.Success;
                }
            }
        }
    }
}
