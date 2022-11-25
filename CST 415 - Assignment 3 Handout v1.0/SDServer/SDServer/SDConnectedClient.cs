// SDConnectedClient.cs
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

namespace SDServer
{
    class SDConnectedClient
    {
        // represents a single connected sd client
        // each client will have its own socket and thread while its connected
        // client is given it's socket from the SDServer when the server accepts the connection
        // this class creates it's own thread
        // the client's thread will process messages on the client's socket until it disconnects
        // NOTE: an sd client can connect/send messages/disconnect many times over it's lifetime

        private Socket clientSocket;
        private NetworkStream stream;
        private StreamReader reader;
        private StreamWriter writer;
        private Thread clientThread;
        private SessionTable sessionTable;      // server's session table
        private ulong sessionId;                // session id for this session, once opened or resumed

        public SDConnectedClient(Socket clientSocket, SessionTable sessionTable)
        {
            // save the client's socket
            this.clientSocket = clientSocket;

            // at this time, there is no stream, reader, write or thread
            stream = null;
            reader = null;
            writer = null;
            clientThread = null;

            // save the server's session table
            this.sessionTable = sessionTable;

            // at this time, there is no session open
            sessionId = 0;
        }

        public void Start()
        {
            // called by the main thread to start the clientThread and process messages for the client

            // create and start the clientThread, pass in a reference to this class instance as a parameter
            clientThread = new Thread(ThreadProc);
            clientThread.Start(this);
        }

        private static void ThreadProc(Object param)
        {
            // the procedure for the clientThread
            // when this method returns, the clientThread will exit

            // the param is a SDConnectedClient instance
            // start processing messages with the Run() method
            (param as SDConnectedClient).Run();
        }

        private void Run()
        {
            // this method is executed on the clientThread

            try
            {
                // create network stream, reader and writer over the socket
                stream = new NetworkStream(clientSocket);
                reader = new StreamReader(stream);
                writer = new StreamWriter(stream);

                // process client requests
                bool done = false;
                while (!done)
                {
                    // receive a message from the client
                    string msg = reader.ReadLine();
                    if (msg == null)
                    {
                        // no message means the client disconnected
                        // remember that the client will connect and disconnect as desired
                        Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Client disconnected");
                        done = true;
                    }
                    else
                    {
                        Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Client message: " + msg);

                        // handle the message
                        switch (msg)
                        {
                            case "open":
                                HandleOpen();
                                break;

                            case "resume":
                                HandleResume();
                                break;

                            case "close":
                                HandleClose();
                                break;

                            case "get":
                                HandleGet();
                                break;

                            case "post":
                                HandlePost();
                                break;

                            default:
                                {
                                    // error handling for an invalid message
                                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Invalid message!");
                                    SendError("Invalid message!");

                                    // this client is too broken to waste our time on!
                                    done = true;
                                }
                                break;
                        }
                    }
                }

            }
            catch (SocketException se)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Error on client socket, closing connection: " + se.Message);
            }
            catch (IOException ioe)
            {
                Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "IO Error on client socket, closing connection: " + ioe.Message);
            }

            // close the client's writer, reader, network stream and socket
            writer.Close();
            reader.Close();
            stream.Close();
            clientSocket.Disconnect(false);
            clientSocket.Close();
        }

        private void HandleOpen()
        {
            // handle an "open" request from the client

            // if no session currently open, then...
            if (sessionId == 0)
            {
                try
                {
                    // ask the SessionTable to open a new session and save the session ID
                    sessionId = sessionTable.OpenSession();

                    // send accepted message, with the new session's ID, to the client
                    SendAccepted(sessionId);
                }
                catch (SessionException se)
                {
                    SendError(se.Message);
                }
                catch (Exception ex)
                {
                    SendError(ex.Message);
                }
            }
            else
            {
                // error!  the client already has a session open!
                SendError("Session already open!");
            }
        }

        private void HandleResume()
        {
            // handle a "resume" request from the client
            ulong resumeSessionId = ulong.Parse(reader.ReadLine());

            // get the sessionId that the client just asked us to resume
            
            try
            {
                // if we don't have a session open currently for this client...
                if (sessionId == 0)
                {
                    // try to resume the session in the session table
                    
                    if (sessionTable.ResumeSession(resumeSessionId))
                    {
                        // if success, remember the session that we're now using and send accepted to client
                        sessionId = resumeSessionId;
                        SendAccepted(sessionId);
                    }
                    else
                    {
                        // if failed to resume session, send rejectetd to client
                        SendRejected("Can't resume this session!");
                    }
                }
                else
                {
                    // error! we already have a session open
                    SendError("Session already open, cannot resume!");
                }
            }
            catch (SessionException se)
            {
                SendError(se.Message);
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
            }
        }

        private void HandleClose()
        {
            // handle a "close" request from the client

            // get the sessionId that the client just asked us to close
            ulong closeThis = ulong.Parse(reader.ReadLine());
            
            try
            {
                // close the session in the session table
                sessionTable.CloseSession(closeThis);

                // send closed message back to client
                SendClosed(closeThis);

                // record that this client no longer has an open session
                sessionId = 0;
            }
            catch (SessionException se)
            {
                SendError(se.Message);
            }
            catch (Exception ex)
            {
                SendError(ex.Message);
            }
        }

        private void HandleGet()
        {
            // handle a "get" request from the client

            // if the client has a session open
            if (sessionId != 0)
            {
                try
                {
                    // get the document name from the client
                    string documentName = reader.ReadLine();
                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Receiving get for " + documentName);

                    // get the document content from the session table
                    string documentContents = sessionTable.GetSessionValue(sessionId, documentName);
                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Got contents " + documentContents);

                    // send success and document to the client
                    SendSuccess(documentName, documentContents);
                }
                catch (SessionException se)
                {
                    SendError(se.Message);
                }
                catch (Exception ex)
                {
                    SendError(ex.Message);
                }
            }
            else
            {
                // error, cannot get without a session
                SendError("No session open, cannot get!");
            }
        }

        private void HandlePost()
        {
            // handle a "post" request from the client

            // if the client has a session open
            if (sessionId != 0)
            {
                try
                {
                    // get the document name, content length and contents from the client
                    string documentName = reader.ReadLine();
                    int documentLength = int.Parse(reader.ReadLine());
                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Receiving post for " + documentName);
                    string documentContents = ReceiveDocument(documentLength);
                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Received contents: " + documentContents);

                    // put the document into the session
                    sessionTable.PutSessionValue(sessionId, documentName, documentContents);
                    Console.WriteLine("[" + clientThread.ManagedThreadId.ToString() + "] " + "Put contents in session table ");

                    // send success to the client
                    SendSuccess();
                }
                catch (SessionException se)
                {
                    SendError(se.Message);
                }
                catch (Exception ex)
                {
                    SendError(ex.Message);
                }
            }
            else
            {
                // error, cannot post without a session
                SendError("No session open, cannot post!");
            }
        }

        private void SendAccepted(ulong sessionId)
        {
            // send accepted message to SD client, including session id of now open session
            writer.WriteLine("accepted");
            writer.WriteLine(sessionId.ToString());
            writer.Flush();
        }

        private void SendRejected(string reason)
        {
            // send rejected message to SD client, including reason for rejection
            writer.WriteLine("rejected");
            writer.WriteLine(reason);
            writer.Flush();
        }

        private void SendClosed(ulong sessionId)
        {
            // send closed message to SD client, including session id that was just closed
            writer.WriteLine("closed");
            writer.WriteLine(sessionId.ToString());
            writer.Flush();
        }

        private void SendSuccess()
        {
            // send sucess message to SD client, with no further info
            // NOTE: in response to a post request
            writer.WriteLine("success");
            writer.Flush();
        }

        private void SendSuccess(string documentName, string documentContent)
        {
            // send success message to SD client, including retrieved document name, length and content
            // NOTE: in response to a get request
            writer.WriteLine("success");
            writer.WriteLine(documentName);
            writer.WriteLine(documentContent.Length.ToString());
            writer.Write(documentContent);
            writer.Flush();
        }

        private void SendError(string errorString)
        {
            // send error message to SD client, including error string
            writer.WriteLine("error");
            writer.WriteLine(errorString);
            writer.Flush();
        }

        private string ReceiveDocument(int length)
        {
            // receive a document from the SD client, of expected length
            // NOTE: as part of processing a post request

            // read from the reader until we've received the expected number of characters
            // accumulate the characters into a string and return those when we've got enough

            // receive file contents
            int charsToRead = length;
            string contents = "";

            // loop until all of the file contents are received
            while (charsToRead > 0)
            {
                // receive as many characters from the server as available
                char[] buffer = new char[charsToRead];
                int charsRead = reader.Read(buffer, 0, charsToRead);
                string stringRead = new string(buffer);

                // accumulate bytes read into the contents
                charsToRead -= charsToRead;
                contents += stringRead;
            }
            
            return contents;
        }
    }
}
