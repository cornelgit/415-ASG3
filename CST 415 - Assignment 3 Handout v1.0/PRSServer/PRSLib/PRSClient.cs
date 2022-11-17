// PRSClient.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace PRSLib
{
    public class PRSClient
    {
        // used by a program that needs to talk to the PRS to either request or lookup a port

        private Socket clientSocket;
        private IPEndPoint endPt;
        private string serviceName;
        private ushort portNumber;
        private Thread keepAliveThread;
        private bool keepAliveRunning;
        private int keepAliveTimeout;

        public PRSClient(string prsAddress, ushort prsPort, string serviceName)
        {
            this.serviceName = serviceName;
            portNumber = 0;

            // create the socket for sending messages to the server
            clientSocket = new Socket(SocketType.Dgram, ProtocolType.Udp);
            
            // construct the server's address and port
            endPt = new IPEndPoint(IPAddress.Parse(prsAddress), prsPort);

            // no keep alive thread initially
            keepAliveThread = null;
            keepAliveRunning = false;
            keepAliveTimeout = 0;
        }

        public ushort RequestPort()
        {
            // send request to server for our service name
            SendMessage(new PRSMessage(PRSMessage.MESSAGE_TYPE.REQUEST_PORT, serviceName, 0, PRSMessage.STATUS.SUCCESS));

            // receive server's response and retrieve the port
            PRSMessage response = ReceiveResponse();
            if (response.Status == PRSMessage.STATUS.SUCCESS)
            {
                portNumber = response.Port;
                return response.Port;
            }
 
            throw new Exception("Failed to request port for service " + serviceName + ", error " + response.Status.ToString());
        }

        public void KeepPortAlive(int timeout = 300)
        {
            // create a thread to periodically send KEEP_ALIVE to the PRS for this port
            keepAliveTimeout = timeout;
            keepAliveThread = new Thread(KeepAliveThreadProc);
            keepAliveThread.Start(this);
        }

        private static void KeepAliveThreadProc(object param)
        {
            // this method is called on the KeepAliveThread
            PRSClient prs = (PRSClient)param;
            prs.keepAliveRunning = true;
            while (prs.keepAliveRunning)
            {
                // send the keep alive to the PRS for this client's port
                prs.SendMessage(new PRSMessage(PRSMessage.MESSAGE_TYPE.KEEP_ALIVE, prs.serviceName, prs.portNumber, PRSMessage.STATUS.SUCCESS));
                PRSMessage response = prs.ReceiveResponse();
                
                // if we receive a failure to keep this port alive, assume there's a serious failure and stop
                if (response.Status != PRSMessage.STATUS.SUCCESS)
                {
                    prs.keepAliveRunning = false;
                    prs.keepAliveThread = null;
                    prs.keepAliveTimeout = 0;
                    Console.WriteLine("Failed to keep port alive for service " + prs.serviceName + ", error " + response.Status.ToString());
                    return;
                }

                // sleep until half the timeout passes, before sending again
                try
                {
                    Thread.Sleep(prs.keepAliveTimeout * 1000 / 2);
                }
                catch (ThreadInterruptedException)
                {
                    // Note: Nothing to do here... we expect to receive this exception when ClosePort() is called
                }
            }
        }

        public void ClosePort()
        {
            // if the keep alive thread is running, then stop it before closing the port
            if (keepAliveRunning)
            {
                keepAliveRunning = false;
                keepAliveThread.Interrupt();
                keepAliveThread = null;
                keepAliveTimeout = 0;
            }

            // send close port to server for our service name and port number
            SendMessage(new PRSMessage(PRSMessage.MESSAGE_TYPE.CLOSE_PORT, serviceName, portNumber, PRSMessage.STATUS.SUCCESS));
            
            // expect success
            PRSMessage response = ReceiveResponse();
            if (response.Status != PRSMessage.STATUS.SUCCESS)
                throw new Exception("Failed to close port " + portNumber.ToString() + ", for service " + serviceName + ", error " + response.Status.ToString());
        }

        public ushort LookupPort()
        {
            // send lookup to server for our service name
            SendMessage(new PRSMessage(PRSMessage.MESSAGE_TYPE.LOOKUP_PORT, serviceName, 0, PRSMessage.STATUS.SUCCESS));

            // receive server's response and retrieve the port
            PRSMessage response = ReceiveResponse();
            if (response.Status == PRSMessage.STATUS.SUCCESS)
            {
                return response.Port;
            }

            throw new Exception("Failed to lookup port for service " + serviceName + ", error " + response.Status.ToString());
        }

        private void SendMessage(PRSMessage msg)
        {
            msg.SendMessage(clientSocket, endPt);
        }

        private PRSMessage ReceiveResponse()
        {
            // NOTE: ignoring server's end point because we know who we sent it to
            EndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
            return PRSMessage.ReceiveMessage(clientSocket, ref remoteEP);
        }
    }
}
