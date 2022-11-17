// PRSMessage.cs
//
// Pete Myers
// CST 415
// Fall 2019
// 

using System;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace PRSLib
{
    public class PRSMessage
    {
        public enum MESSAGE_TYPE
        {
            REQUEST_PORT = 0,
            LOOKUP_PORT = 1,
            KEEP_ALIVE = 2,
            CLOSE_PORT = 3,
            STOP = 4,
            RESPONSE = 5
        }

        public enum STATUS
        {
            SUCCESS = 0,
            SERVICE_IN_USE = 1,
            SERVICE_NOT_FOUND = 2,
            ALL_PORTS_BUSY = 3,
            INVALID_ARG = 4,
            UNDEFINED_ERROR = 5
        }

        private const int MSG_SIZE = 54;

        /*  Expected format for a message sent to the PRS server
            typedef struct
            {
                int8_t msg_type;            //  1 byte,  from 0 to 0
                char service_name[50];      // 50 bytes, from 1 to 50
                uint16_t port;              //  2 bytes, from 51 to 52
                int8_t status;              //  1 byte,  from 53 to 53
            } request_t;                    // 54 bytes total
        */

        private MESSAGE_TYPE msg_type;
        private string service_name;
        private ushort port;
        private STATUS status;

        public PRSMessage(MESSAGE_TYPE msg_type, string service_name, ushort port, STATUS status)
        {
            this.msg_type = msg_type;
            this.service_name = service_name;
            this.port = port;
            this.status = status;
        }

        public MESSAGE_TYPE MsgType { get { return msg_type; } }
        public string ServiceName { get { return service_name; } }
        public ushort Port { get { return port; } }
        public STATUS Status { get { return status; } }

        public override string ToString()
        {
            // e.g. {REQUEST_PORT, “SVC1”, 0, SUCCESS}

            string result = "";

            result += "{";
            result += msg_type.ToString();
            result += ", ";
            result += service_name;
            result += ", ";
            result += port.ToString();
            result += ", ";
            result += status.ToString();
            result += "}";

            return result;
        }

        public void SendMessage(Socket sock, EndPoint toEP)
        {
            int result = sock.SendTo(GetBytes(), toEP);
            Console.WriteLine("Sent " + result.ToString() + " bytes: " + ToString());
        }

        public static PRSMessage ReceiveMessage(Socket sock, ref EndPoint fromEP)
        {
            byte[] buffer = new byte[MSG_SIZE];
            int result = sock.ReceiveFrom(buffer, ref fromEP);
            PRSMessage msg = new PRSMessage(buffer);
            Console.WriteLine("Received " + result.ToString() + " bytes: " + msg.ToString());

            return msg;
        }

        #region implementation

        // these methods translate PRSMessage <--> byte[]
        // based on the expected format of a message
        // NOTE: there is a challenge with the port field...
        //     port is an unsigned short, but
        //     NetworkToHostOrder() and HostToNetworkOrder() only operate on SIGNED short, so
        //     casting between short and ushort is required in the following methods

        private PRSMessage(byte[] bytes)
        {
            this.msg_type = (MESSAGE_TYPE)bytes[0];
            this.service_name = ASCIIEncoding.UTF8.GetString(bytes, 1, 50).TrimEnd('\0');
            this.port = (ushort)IPAddress.NetworkToHostOrder(((short)BitConverter.ToInt16(bytes, 51)));
            this.status = (STATUS)bytes[53];
        }

        private byte[] GetBytes()
        {
            byte[] result = new byte[MSG_SIZE];

            // encode each field of the message into the resulting byte array
            result[0] = (byte)msg_type;
            ASCIIEncoding.UTF8.GetBytes(service_name).CopyTo(result, 1);
            System.BitConverter.GetBytes((ushort)(IPAddress.HostToNetworkOrder((short)port))).CopyTo(result, 51);
            result[53] = (byte)status;

            return result;
        }

        #endregion
    }
}
