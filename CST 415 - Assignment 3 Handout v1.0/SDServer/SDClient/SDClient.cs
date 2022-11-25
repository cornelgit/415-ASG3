// SDClient.cs
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

namespace SDClient
{
    class SDClient
    {
        private string sdServerAddress;
        private ushort sdServerPort;
        private bool connected;
        private ulong sessionID;
        Socket clientSocket;
        NetworkStream stream;
        StreamReader reader;
        StreamWriter writer;

        public SDClient(string sdServerAddress, ushort sdServerPort)
        {
            // save server address/port
            this.sdServerAddress = sdServerAddress;
            this.sdServerPort = sdServerPort;

            // initialize to not connected to server
            clientSocket = null;
            stream = null;
            reader = null;
            writer = null;
            connected = false;

            // no session open at this time
            sessionID = 0;
        }

        public ulong SessionID { get { return sessionID; } set { sessionID = value; } }

        public void Connect()
        {
            ValidateDisconnected();

            // create a client socket and connect to the FT Server's IP address and port
            clientSocket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            clientSocket.Connect(new IPEndPoint(IPAddress.Parse(sdServerAddress), sdServerPort));

            // establish the network stream, reader and writer
            stream = new NetworkStream(clientSocket);
            reader = new StreamReader(stream);
            writer = new StreamWriter(stream);

            // now connected
            connected = true;
        }

        public void Disconnect()
        {
            ValidateConnected();

            // close writer, reader and stream
            writer.Close();
            reader.Close();
            stream.Close();

            // disconnect and close socket
            clientSocket.Disconnect(false);
            clientSocket.Close();

            // now disconnected
            connected = false;   
        }

        public void OpenSession()
        {
            ValidateConnected();

            // send open command to server
            SendOpen();

            // receive server's response, hopefully with a new session id
            sessionID = ReceiveSessionResponse();
        }

        public void ResumeSession(ulong trySessionID)
        {
            ValidateConnected();

            // send resume session to the server
            SendResume(trySessionID);
                        
            // receive server's response, hopefully confirming our sessionId
            ulong resumedSessionID = ReceiveSessionResponse();

            // verify that we received the same session ID that we requested
            if (trySessionID != resumedSessionID)
            {
                throw new Exception("Tried to resume session " + trySessionID.ToString() + ", but actually resumed " + resumedSessionID.ToString());
            }

            // save opened session
            sessionID = resumedSessionID;
        }

        public void CloseSession()
        {
            ValidateConnected();

            // send close session to the server
            SendClose(sessionID);

            // received closed response
            ulong wasCLosed = ReceiveSessionResponse();
            if (wasCLosed != sessionID)
            {
                throw new Exception("Server closed the wrong session! Requested " + sessionID.ToString() + ", Closed " + wasCLosed.ToString());
            }

            // no session open
            sessionID = 0;
        }

        public string GetDocument(string documentName)
        {
            ValidateConnected();

            // send get to the server
            SendGet(documentName);
            
            // get the server's response
            return ReceiveGetResponse();
        }

        public void PostDocument(string documentName, string documentContents)
        {
            ValidateConnected();

            // send the document to the server
            SendPost(documentName, documentContents);

            // get the server's response
            ReceivePostResponse();
        }

        private void ValidateConnected()
        {
            if (!connected)
                throw new Exception("Connot perform action. Not connected to server!");
        }

        private void ValidateDisconnected()
        {
            if (connected)
                throw new Exception("Connot perform action. Already connected to server!");
        }

        private void SendOpen()
        {
            // send open message to SD server
            writer.WriteLine("open");
            writer.Flush();
        }

        private void SendClose(ulong sessionId)
        {
            // send close message to SD server
            writer.WriteLine("close");
            writer.WriteLine(sessionId.ToString());
            writer.Flush();
        }

        private void SendResume(ulong sessionId)
        {
            // send resume message to SD server
            writer.WriteLine("resume");
            writer.WriteLine(sessionId.ToString());
            writer.Flush();
        }

        private ulong ReceiveSessionResponse()
        {
            // get SD server's response to our last session request (open, resume, or closed)
            string line = reader.ReadLine();
            if (line == "accepted")
            {
                // yay, server accepted our session!
                // get the sessionID
                return ulong.Parse(reader.ReadLine());
            }
            else if (line == "rejected")
            {
                // boo, server rejected us!
                string reason = reader.ReadLine();
                throw new Exception("Rejected session! " + reason);
            }
            else if (line == "closed")
            {
                // server happily closed our session as requested
                return ulong.Parse(reader.ReadLine());
            }
            else if (line == "error")
            {
                // boo, server sent us an error!
                string msg = reader.ReadLine();
                throw new Exception("Session error! " + msg);
            }
            else
            {
                throw new Exception("Expected to receive a valid session response, instead got... " + line);
            }
        }

        private void SendPost(string documentName, string documentContents)
        {
            // send post message to SD erer, including document name, length and contents
            // NOTE: no \n at the end of the contents
            writer.WriteLine("post");
            writer.WriteLine(documentName);
            writer.WriteLine(documentContents.Length.ToString());
            writer.WriteLine(documentContents);
            writer.Flush();
        }

        private void SendGet(string documentName)
        {
            // send get message to SD server
            writer.WriteLine("get");
            writer.WriteLine(documentName);
            writer.Flush();
        }

        private void ReceivePostResponse()
        {
            // get server's response to our last post request
            string line = reader.ReadLine();
            if (line == "success")
            {
                // yay, server accepted our request!                
            }
            else if (line == "error")
            {
                // boo, server sent us an error!
                throw new Exception(reader.ReadLine());
            }
            else
            {
                throw new Exception("Expected to receive a valid post response, instead got... " + line);
            }
        }

        private string ReceiveGetResponse()
        {
            // get server's response to our last get request and return the content received
            string line = reader.ReadLine();
            if (line == "success")
            {
                // yay, server accepted our request!
                
                // read the document name, content length and content
                string documentName = reader.ReadLine();
                int contentLength = int.Parse(reader.ReadLine());
                string documentContents = ReceiveDocumentContent(contentLength);

                // return the contents
                return documentContents;
            }
            else if (line == "error")
            {
                // boo, server sent us an error!
                throw new Exception(reader.ReadLine());
            }
            else
            {
                throw new Exception("Expected to receive a valid get response, instead got... " + line);
            }
        }

        private string ReceiveDocumentContent(int length)
        {
            // read from the reader until we've received the expected number of characters
            // accumulate the characters into a string and return those when we received enough

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
