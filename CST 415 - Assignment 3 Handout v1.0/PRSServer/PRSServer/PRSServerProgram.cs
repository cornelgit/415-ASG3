// PRSServerProgram.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using PRSLib;

namespace PRSServer
{
    class PRSServerProgram
    {
        class PRS
        {
            // represents a PRS Server, keeps all state and processes messages accordingly

            class PortReservation
            {
                private ushort port;
                private bool available;
                private string serviceName;
                private DateTime lastAlive;

                public PortReservation(ushort port)
                {
                    this.port = port;
                    available = true;
                }

                public string ServiceName { get { return serviceName; } }
                public ushort Port { get { return port; } }
                public bool Available { get { return available; } }

                public bool Expired(int timeout)
                {
                    // return true if timeout seconds have elapsed since lastAlive
                    // Know: lastAlive, DateTime.Now, timeout
                    return (DateTime.Now - lastAlive).TotalSeconds > timeout;
                }

                public void Reserve(string serviceName)
                {
                    // reserve this port for serviceName
                    this.serviceName = serviceName;
                    available = false;
                    lastAlive = DateTime.Now;
                }

                public void KeepAlive()
                {
                    // save current time in lastAlive
                    lastAlive = DateTime.Now;
                }

                public void Close()
                {
                    // make this reservation available
                    available = true;
                    serviceName = null;
                }
            }

            // server attribues
            private ushort startingClientPort;
            private ushort endingClientPort;
            private int keepAliveTimeout;
            private int numPorts;
            private PortReservation[] ports;
            private bool stopped;

            public PRS(ushort startingClientPort, ushort endingClientPort, int keepAliveTimeout)
            {               
                // save parameters
                this.startingClientPort = startingClientPort;
                this.endingClientPort = endingClientPort;
                this.keepAliveTimeout = keepAliveTimeout;

                // initialize to not stopped
                stopped = false;

                // initialize port reservations
                numPorts = endingClientPort - startingClientPort + 1;
                ports = new PortReservation[numPorts];
                for (int i = 0; i < numPorts; i++)
                {
                    ports[i] = new PortReservation((ushort)(startingClientPort + i));
                }
            }

            public bool Stopped { get { return stopped; } }

            private PortReservation FindPort(ushort port)
            {
                if (port >= startingClientPort && port <= endingClientPort)
                {
                    return ports[port - startingClientPort];
                }

                return null;
            }

            private PortReservation FindPort(string name)
            {
                //find the port # that is reserved by given name
                foreach (PortReservation reservation in ports)
                {
                    if (!reservation.Available && reservation.ServiceName == name)
                    {
                        return reservation;
                    }
                }

                return null;
            }

            private void CheckForExpiredPorts()
            {
                // expire any reserved ports that have not been kept alive, within the timeout range
                foreach (PortReservation reservation in ports)
                {
                    if (!reservation.Available && reservation.Expired(keepAliveTimeout))
                    {
                        reservation.Close();
                    }
                }
            }

            private PRSMessage RequestPort(string serviceName)
            {
                PRSMessage response = null;

                // client has requested the lowest available port, so find it!
                PortReservation availablePort = null;

                for (int i = 0; i < numPorts && availablePort == null; i++)
                {
                    if (ports[i].Available)
                    {
                        availablePort = ports[i];
                    }
                }
                
                // if found an available port, reserve it and send SUCCESS
                if (availablePort != null)
                {
                    availablePort.Reserve(serviceName);
                    response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, serviceName, availablePort.Port, PRSMessage.STATUS.SUCCESS);
                }

                // else, none available, send ALL_PORTS_BUSY
                else
                {
                    response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, serviceName, 0, PRSMessage.STATUS.ALL_PORTS_BUSY);
                }
                
                return response;
            }

            public PRSMessage HandleMessage(PRSMessage msg)
            {
                // handle one message and return a response

                PRSMessage response = null;

                switch (msg.MsgType)
                {
                    case PRSMessage.MESSAGE_TYPE.REQUEST_PORT:
                        {
                            // check for expired ports and send requested port back to the client
                            CheckForExpiredPorts();
                            response = RequestPort(msg.ServiceName);
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.KEEP_ALIVE:
                        {
                            // client has requested that we keep their port alive
                            // find the port
                            PortReservation reservation = FindPort(msg.Port);
                            
                            // if found, keep it alive and send SUCCESS
                            if (reservation != null)
                            {
                                reservation.KeepAlive();
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                            }
                            // else, SERVICE_NOT_FOUND
                            else
                            {
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }
                        }
                        break;
                        
                    case PRSMessage.MESSAGE_TYPE.CLOSE_PORT:
                        {
                            // client has requested that we close their port, and make it available for others!
                            // find the port
                            PortReservation reservation = FindPort(msg.Port);

                            // if found, close it and send SUCCESS
                            if (reservation != null)
                            {
                                reservation.Close();
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SUCCESS);
                            }
                            // else, SERVICE_NOT_FOUND
                            else
                            {
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }                            
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.LOOKUP_PORT:
                        {
                            // client wants to know the reserved port number for a named service
                            // find the port
                            PortReservation reservation = FindPort(msg.ServiceName);

                            // if found, send the corresponding port number and SUCCESS
                            if (reservation != null)
                            {
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, reservation.Port, PRSMessage.STATUS.SUCCESS);
                            }
                            // else, SERVICE_NOT_FOUND
                            else
                            {
                                response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, msg.ServiceName, msg.Port, PRSMessage.STATUS.SERVICE_NOT_FOUND);
                            }                            
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.STOP:
                        {
                            // client is telling us to close the application down
                            // stop the PRS and return SUCCESS
                            stopped = true;
                            response = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, "", 0, PRSMessage.STATUS.SUCCESS);
                        }
                        break;
                }

                return response;
            }

        }

        static void Usage()
        {
            Console.WriteLine("\nUsage: PRSServer [options]");
            Console.WriteLine("\t-p < service port >");
            Console.WriteLine("\t-s < starting client port number >");
            Console.WriteLine("\t-e < ending client port number >");
            Console.WriteLine("\t-t < keep alive time in seconds >\n");
        }

        static void Main(string[] args)
        {
            // defaults
            ushort SERVER_PORT = 30000;
            ushort STARTING_CLIENT_PORT = 40000;
            ushort ENDING_CLIENT_PORT = 40100;
            int KEEP_ALIVE_TIMEOUT = 10;

            // process command options
            // -p < service port >
            // -s < starting client port number >
            // -e < ending client port number >
            // -t < keep alive time in seconds >

            // print usage
            Usage();

            try
            {

                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] == "-p")
                    {
                        if (i + 1 < args.Length)
                        {
                            SERVER_PORT = ushort.Parse(args[++i]);
                        }
                        else
                        {
                            throw new Exception("-p requires a value!");
                        }                        
                    }
                    else if (args[i] == "-s")
                    {
                        if (i + 1 < args.Length)
                        {
                            STARTING_CLIENT_PORT = ushort.Parse(args[++i]);
                        }
                        else
                        {
                            throw new Exception("-s requires a value!");
                        }
                    }
                    else if (args[i] == "-e")
                    {
                        if (i + 1 < args.Length)
                        {
                            ENDING_CLIENT_PORT = ushort.Parse(args[++i]);
                        }
                        else
                        {
                            throw new Exception("-e requires a value!");
                        }
                    }
                    else if (args[i] == "-t")
                    {
                        if (i + 1 < args.Length)
                        {
                            KEEP_ALIVE_TIMEOUT = ushort.Parse(args[++i]);
                        }
                        else
                        {
                            throw new Exception("-t requires a value!");
                        }
                    }
                    else
                    {
                        // error! unexpected cmd line arg
                        throw new Exception("Invalid cmd line arg: " + args[i]);
                    }
                }
            }

            catch (Exception ex)
            {
                Console.WriteLine("Error! "+ ex.Message);
                return;
            }

            // check for valid STARTING_CLIENT_PORT and ENDING_CLIENT_PORT
            if (STARTING_CLIENT_PORT > ENDING_CLIENT_PORT)
            {
                Console.WriteLine("Error! Invalid client port range!");
                return;
            }

            //print out the parameters
            Console.WriteLine("SERVER_PORT=" + SERVER_PORT.ToString());
            Console.WriteLine("STARTING_CLIENT_PORT=" + STARTING_CLIENT_PORT.ToString());
            Console.WriteLine("ENDING_CLIENT_PORT=" + ENDING_CLIENT_PORT.ToString());
            Console.WriteLine("KEEP_ALIVE_TIMEOUT=" + KEEP_ALIVE_TIMEOUT.ToString() + "\n");

            // initialize the PRS server
            PRS prs = new PRS(STARTING_CLIENT_PORT, ENDING_CLIENT_PORT, KEEP_ALIVE_TIMEOUT);

            // create the socket for receiving messages at the server
            Socket socket = new Socket(SocketType.Dgram, ProtocolType.Udp);

            // bind the listening socket to the PRS server port
            socket.Bind(new IPEndPoint(IPAddress.Any, SERVER_PORT));

            //
            // Process client messages
            //

            while (!prs.Stopped)
            {
                EndPoint clientEP = null;
                try
                {
                    // receive a message from a client
                    clientEP = new IPEndPoint(IPAddress.Any, 0);
                    PRSMessage msg = PRSMessage.ReceiveMessage(socket, ref clientEP);

                    // let the PRS handle the message
                    PRSMessage response = prs.HandleMessage(msg);

                    // send response message back to client
                    response.SendMessage(socket, clientEP);
                }
                catch (Exception ex)
                {
                    // attempt to send a UNDEFINED_ERROR response to the client, if we know who that was
                    Console.WriteLine("Unexpected error: " + ex.Message);
                    PRSMessage err = new PRSMessage(PRSMessage.MESSAGE_TYPE.RESPONSE, "", 0, PRSMessage.STATUS.UNDEFINED_ERROR);
                    err.SendMessage(socket, clientEP);
                }
            }

            // close the listening socket
            socket.Close();
            
            // wait for a keypress from the user before closing the console window
            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }
    }
}
