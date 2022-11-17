// FTConnectedClient.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.IO;

namespace FTServer
{
    class FTConnectedClient
    {
        // represents a single connected ft client, that wants directory contents from the server
        // each client will have its own socket and thread
        // client is given it's socket from the FTServer when the server accepts the connection
        // the client class creates it's own thread
        // the client's thread will process messages on the client's socket

        private Socket clientSocket;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread clientThread;

        public FTConnectedClient(Socket clientSocket)
        {
            // TODO: FTConnectedClient.FTConnectedClient()

            // save the client's socket
            
            // at this time, there is no stream, reader, write or thread
            
        }

        public void Start()
        {
            // TODO: FTConnectedClient.Start()

            // called by the main thread to start the clientThread and process messages for the client

            // create and start the clientThread, pass in a reference to this class instance as a parameter

        }

        private static void ThreadProc(Object param)
        {
            // TODO: FTConnectedClient.ThreadProc()

            // the procedure for the clientThread
            // when this method returns, the clientThread will exit

            // the param is a FTConnectedClient instance
            // start processing messages with the Run() method

        }

        private void Run()
        {
            // TODO: FTConnectedClient.Run()

            // this method is executed on the clientThread

            try
            {
                // create network stream, reader and writer over the socket
                
                // process client requests
                bool done = false;
                while (!done)
                {
                    // receive a message from the client
                    
                    // handle the message
                    // if get
                    {
                        // get directoryName
                                
                        // retrieve directory contents and sending all the files
                                
                        // if directory does not exist! send an error!
                                    
                        // if directory exists, send each file to the client
                        // for each file...
                        // get the file's name
                        // make sure it's a txt file
                        // get the file contents
                        // send a file to the client
                        // send done after last file
                                
                    }

                    // if exit
                    {
                        // client is done, close it's socket and quit the thread
                        
                    }
                    
                    // else invalid message
                    {
                        // error handling for an invalid message
                        
                        // this client is too broken to waste our time on!
                        // quite processing messages and disconnect
                        
                    }
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Error on client socket, closing connection: " + se.Message);
            }

            // close the client's writer, reader, network stream and socket
            
        }

        private void SendFileName(string fileName, int fileLength)
        {
            // TODO: FTConnectedClient.SendFileName()
            // send file name and file length message

        }

        private void SendFileContents(string fileContents)
        {
            // TODO: FTConnectedClient.SendFileContents()
            // send file contents only
            // NOTE: no \n at end of contents

        }

        private void SendDone()
        {
            // TODO: FTConnectedClient.SendDone()
            // send done message

        }

        private void SendError(string errorMessage)
        {
            // TODO: FTConnectedClient.SendError()
            // send error message

        }
    }
}
