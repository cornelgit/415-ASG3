// PRSTestClientProgram.cs
//
// Pete Myers
// CST 415
// Fall 2019
//
// Connects to a PRSServer and runs TC1 through TC6
// Assumes the PRSServer is run with the following command line arguments:
//     PRSServer.exe -p 30000 -s 40000 -e 40100 -t 10
//

using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using PRSLib;
using System.Text.RegularExpressions;

namespace PRSTestClient
{
    class PRSTestClientProgram
    {
        static void Usage()
        {
            Console.WriteLine("\nUsage: PRSTestClient [options]");
            Console.WriteLine("\t-prs <serverIP>:<serverPort>\n");
        }

        static void Main(string[] args)
        {
            // defaults
            string SERVER_IP = "127.0.0.1";
            int SERVER_PORT = 30000;

            // print usage
            //Usage();

            //// process command options
            try
            {
                if (args[0] == "-prs")
                {
                    // X.X.X.X:X is minimum allowed for ip, colon, plus port length
                    if (args[1].Length >= 9)
                    {
                        if (args[1].Contains(".") && args[1].Contains(":"))
                        {
                            SERVER_PORT = int.Parse(args[1].Substring(args[1].IndexOf(':') + 1)); //get port number from cmd line string arg

                            //extract out ip address from string arg
                            string input = args[1];
                            string pattern = ":\\d{1,5}";
                            SERVER_IP = Regex.Replace(input, pattern, "");
                        }
                        else
                        {
                            throw new Exception("IP and Port need to be the right format: X.X.X.X:X. Some periods and a colon are required.");
                        }
                    }
                    else
                    {
                        throw new Exception("IP and Port combined length is too small. Expecting a proper format.");
                    }
                }
                else
                {
                    // error! unexpected cmd line arg
                    throw new Exception("Invalid cmd line arg: " + args[0] + "\t" + "-prs option must be specified when using cmd line arg");
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error! " + ex.Message);
                return;
            }

            // tell user what we're doing
            Console.WriteLine("Test Client started...");
            Console.WriteLine("  ServerIP = " + SERVER_IP.ToString());
            Console.WriteLine("  ServerPort = " + SERVER_PORT.ToString());

            // recommend proper PRSServer command line arguments
            Console.WriteLine();
            Console.WriteLine("Assumes the PRSServer is running with the following command line arguments:");
            Console.WriteLine("    PRSServer.exe -p 30000 -s 40000 -e 40100 -t 10");
            Console.WriteLine();

            // create the socket for sending messages to the server
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            // construct the server's address and port
            IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Parse(SERVER_IP), SERVER_PORT);

            //
            // Implement test cases
            //

            try
            {
                // call each test case method
                TestCase1(socket, serverEndPoint);
                TestCase2(socket, serverEndPoint);
                TestCase3(socket, serverEndPoint);
                TestCase4(socket, serverEndPoint);
                TestCase5(socket, serverEndPoint);
                TestCase6(socket, serverEndPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Exception: " + ex.Message);
                Console.WriteLine(ex.StackTrace);
            }

            // close the client socket and quit
            socket.Close();
            
            // wait for a keypress from the user before closing the console window
            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }

        private static void SendMessage(Socket clientSocket, IPEndPoint endPt, PRSMessage msg)
        {
            msg.SendMessage(clientSocket, endPt);
        }

        private static PRSMessage ExpectMessage(Socket clientSocket, string expectedMessage)
        {
            // receive message and validate that expected PRSMessage was received
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            PRSMessage msg = PRSMessage.ReceiveMessage(clientSocket, ref remoteEP);
            if (msg.ToString() != expectedMessage)
                throw new Exception("Test failed! Expected " + expectedMessage);

            return msg;
        }

        private static void TestCase1(Socket clientSocket, IPEndPoint endPt)
        {
            // Simulates a PRS client, SVC1, that requests a port, keeps it alive and then closes it.

            Console.WriteLine("TestCase 1 Started...");

            // See test cases doc
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "SVC1", 0, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.KEEP_ALIVE, "SVC1", 40000, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "SVC1", 40000, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");

            Console.WriteLine("TestCase 1 Passed!");
            Console.WriteLine();
        }

        private static void TestCase2(Socket clientSocket, IPEndPoint endPt)
        {
            // Simulates two PRS clients, SVC1 and C1, where SVC1 requests a port, and C1 looks up the port.

            Console.WriteLine("TestCase 2 Started...");

            // See test cases doc
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "SVC1", 0, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, "SVC1", 0, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "SVC1", 40000, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");

            Console.WriteLine("TestCase 2 Passed!");
            Console.WriteLine();
        }

        private static void TestCase3(Socket clientSocket, IPEndPoint endPt)
        {
            // Simulates two PRS clients, SVC1 and SVC2, where SVC1 requests a port, then SVC2 requests a port and receives its own port.

            Console.WriteLine("TestCase 3 Started...");

            // See test cases doc
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "SVC1", 0, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "SVC2", 0, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC2, 40001, SUCCESS}");

            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "SVC1", 40000, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "SVC2", 40001, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC2, 40001, SUCCESS}");

            Console.WriteLine("TestCase 3 Passed!");
            Console.WriteLine();
        }

        private static void TestCase4(Socket clientSocket, IPEndPoint endPt)
        {
            // Simulates two PRS clients, SVC1 and SVC2, where SVC1 requests a port, SVC1 fails to keep the port alive, then SVC2 requests a port and receives SVC1’s expired port.

            Console.WriteLine("TestCase 4 Started...");

            // See test cases doc
            // use Thread.Sleep();
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "SVC1", 0, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");

            // sleep for 15 seconds to give SV1's port time to expire
            Console.WriteLine("Sleeping for 15 seconds...");
            Thread.Sleep(15000);

            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "SVC2", 0, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC2, 40000, SUCCESS}");
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "SVC2", 40000, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC2, 40000, SUCCESS}");

            Console.WriteLine("TestCase 4 Passed!");
            Console.WriteLine();
        }

        private static void TestCase5(Socket clientSocket, IPEndPoint endPt)
        {
            // Simulates two PRS clients, SVC1 and SVC2, where SVC1 requests a port, SVC1 keeps the port alive, then SVC2 requests a port and receives its own port.

            Console.WriteLine("TestCase 5 Started...");

            // See test cases doc
            // use Thread.Sleep();
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "SVC1", 0, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");

            // sleep for 8 seconds before SV1 keeps its port alive
            Console.WriteLine("Sleeping for 8 seconds...");
            Thread.Sleep(8000);
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.KEEP_ALIVE, "SVC1", 40000, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");

            // sleep for another 8 seconds before SV2 requests its port
            Console.WriteLine("Sleeping for 8 seconds...");
            Thread.Sleep(8000);
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, "SVC2", 0, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC2, 40001, SUCCESS}");

            // both clients can close their ports
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "SVC1", 40000, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC1, 40000, SUCCESS}");
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, "SVC2", 40001, 0));
            ExpectMessage(clientSocket, "{RESPONSE, SVC2, 40001, SUCCESS}");

            Console.WriteLine("TestCase 5 Passed!");
            Console.WriteLine();
        }

        private static void TestCase6(Socket clientSocket, IPEndPoint endPt)
        {
            // Simulates a PRS client, M, that tells the PRS to stop

            Console.WriteLine("TestCase 6 Started...");

            // See test cases doc
            SendMessage(clientSocket, endPt, new PRSMessage(PRSMessage.MESSAGE_TYPE.STOP, "", 0, 0));
            ExpectMessage(clientSocket, "{RESPONSE, , 0, SUCCESS}");

            Console.WriteLine("TestCase 6 Passed!");
            Console.WriteLine();
        }
    }
}