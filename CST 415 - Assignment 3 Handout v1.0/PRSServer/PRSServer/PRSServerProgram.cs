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
                    // TODO: PortReservation.Expired()
                    // return true if timeout secons have elapsed since lastAlive

                    return false;
                }

                public void Reserve(string serviceName)
                {
                    // TODO: PortReservation.Reserve()
                    // reserve this port for serviceName
                }

                public void KeepAlive()
                {
                    // TODO: PortReservation.KeepAlive()
                    // save current time in lastAlive
                }

                public void Close()
                {
                    // TODO: PortReservation.Close()
                    // make this reservation available
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
                // TODO: PRS.PRS()
                
                // save parameters
                
                // initialize to not stopped
                
                // initialize port reservations
                
            }

            public bool Stopped { get { return stopped; } }

            private void CheckForExpiredPorts()
            {
                // TODO: PRS.CheckForExpiredPorts()
                // expire any ports that have not been kept alive

            }

            private PRSMessage RequestPort(string serviceName)
            {
                // TODO: PRS.RequestPort()

                PRSMessage response = null;

                // client has requested the lowest available port, so find it!
                
                // if found an avialable port, reserve it and send SUCCESS
                // else, none available, send ALL_PORTS_BUSY
                
                return response;
            }

            public PRSMessage HandleMessage(PRSMessage msg)
            {
                // TODO: PRS.HandleMessage()

                // handle one message and return a response

                PRSMessage response = null;

                switch (msg.MsgType)
                {
                    case PRSMessage.MESSAGE_TYPE.REQUEST_PORT:
                        {
                            // check for expired ports and send requested report
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.KEEP_ALIVE:
                        {
                            // client has requested that we keep their port alive
                            // find the port
                            // if found, keep it alive and send SUCCESS
                            // else, SERVICE_NOT_FOUND
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.CLOSE_PORT:
                        {
                            // client has requested that we close their port, and make it available for others!
                            // find the port
                            // if found, close it and send SUCCESS
                            // else, SERVICE_NOT_FOUND
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.LOOKUP_PORT:
                        {
                            // client wants to know the reserved port number for a named service
                            // find the port
                            // if found, send port number back
                            // else, SERVICE_NOT_FOUND
                        }
                        break;

                    case PRSMessage.MESSAGE_TYPE.STOP:
                        {
                            // client is telling us to close the appliation down
                            // stop the PRS and return SUCCESS
                        }
                        break;
                }

                return response;
            }

        }

        static void Usage()
        {
            Console.WriteLine("usage: PRSServer [options]");
            Console.WriteLine("\t-p < service port >");
            Console.WriteLine("\t-s < starting client port number >");
            Console.WriteLine("\t-e < ending client port number >");
            Console.WriteLine("\t-t < keep alive time in seconds >");
        }

        static void Main(string[] args)
        {
            // TODO: PRSServerProgram.Main()

            // defaults
            ushort SERVER_PORT = 30000;
            ushort STARTING_CLIENT_PORT = 40000;
            ushort ENDING_CLIENT_PORT = 40099;
            int KEEP_ALIVE_TIMEOUT = 300;

            // process command options
            // -p < service port >
            // -s < starting client port number >
            // -e < ending client port number >
            // -t < keep alive time in seconds >

            // check for valid STARTING_CLIENT_PORT and ENDING_CLIENT_PORT
            
            // initialize the PRS server
            
            // create the socket for receiving messages at the server
            
            // bind the listening socket to the PRS server port
            
            //
            // Process client messages
            //

            // while (!prs.Stopped)
            {
                try
                {
                    // receive a message from a client
                    
                    // let the PRS handle the message
                    
                    // send response message back to client
                    
                }
                catch (Exception ex)
                {
                    // attempt to send a UNDEFINED_ERROR response to the client, if we know who that was

                }
            }

            // close the listening socket
            
            // wait for a keypress from the user before closing the console window
            Console.WriteLine("Press Enter to exit");
            Console.ReadKey();
        }
    }
}
