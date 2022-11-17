// SDServer.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Threading;
using System.Net.Sockets;
using System.Net;

namespace SDServer
{
    class SDServer
    {
        // represents the server and it's logic
        // the server uses the main program thread to listen and accept connections from client
        // when the server accepts a client connection, it will create the client's socket and thread

        private ushort listeningPort;
        private int clientBacklog;
        private SessionTable sessionTable;

        public SDServer(ushort listeningPort, int clientBacklog)
        {
            this.listeningPort = listeningPort;
            this.clientBacklog = clientBacklog;

            // initially empty session table
            sessionTable = new SessionTable();
        }

        public void Start()
        {
            // create a listening socket for clients to connect
            Socket listeningSocket= new Socket(SocketType.Stream, ProtocolType.Tcp);

            // bind to the SD Server port
            listeningSocket.Bind(new IPEndPoint(IPAddress.Any, listeningPort));

            // set the socket to listen
            listeningSocket.Listen(clientBacklog);

            bool done = false;
            while (!done)
            {
                try
                {
                    // accept a client connection
                    Socket clientSocket = listeningSocket.Accept();

                    // instantiate connected client to process messages
                    SDConnectedClient client = new SDConnectedClient(clientSocket, sessionTable);
                    client.Start();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error while accepting and starting client: " + ex.Message);
                    Console.WriteLine("Waiting for 5 seconds and trying again...");
                    Thread.Sleep(5000);
                }
            }

            // close socket and quit
            listeningSocket.Close();
        }
    }
}
