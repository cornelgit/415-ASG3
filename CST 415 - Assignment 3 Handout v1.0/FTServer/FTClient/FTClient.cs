// FTClient.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace FTClient
{
    class FTClient
    {
        private string ftServerAddress;
        private ushort ftServerPort;
        bool connected;
        Socket clientSocket;
        NetworkStream stream;
        StreamReader reader;
        StreamWriter writer;

        public FTClient(string ftServerAddress, ushort ftServerPort)
        {
            // TODO: FTClient.FTClient()

            // save server address/port


            // initialize to not connected to server

        }

        public void Connect()
        {
            // TODO: FTClient.Connect()

            if (!connected)
            {
                // create a client socket and connect to the FT Server's IP address and port
                
                // establish the network stream, reader and writer
                
                // now connected
                
            }
        }

        public void Disconnect()
        {
            // TODO: FTClient.Disconnect()

            if (connected)
            {
                // send exit to FT server
                
                // close writer, reader and stream
                
                // disconnect and close socket
                
                // now disconnected
                
            }
        }

        public void GetDirectory(string directoryName)
        {
            // TODO: FTClient.GetDirectory()

            // send get to the server for the specified directory and receive files
            if (connected)
            {
                // send get command for the directory
                
                // receive and process files
                
            }
        }

        #region implementation

        private void SendGet(string directoryName)
        {
            // TODO: FTClient.SendGet()
            // send get message for the directory

        }

        private void SendExit()
        {
            // TODO: FTClient.SendExit()
            // send exit message

        }

        private void SendInvalidMessage()
        {
            // TODO: FTClient.SendInvalidMessage()
            // allows for testing of server's error handling code

        }

        private bool ReceiveFile(string directoryName)
        {
            // TODO: FTClient.ReceiveFile()
            // receive a single file from the server and save it locally in the specified directory

            // expect file name from server

            // when the server sends "done", then there are no more files!

            // handle error messages from the server

            // received a file name

            // receive file length from server

            // receive file contents

            // loop until all of the file contenst are received
            //while (charsToRead > 0)
            {
                // receive as many characters from the server as available

                // accumulate bytes read into the contents

            }

            // create the local directory if needed
            
            // save the file locally on the disk
            
            return true;
        }

        #endregion
    }
}
